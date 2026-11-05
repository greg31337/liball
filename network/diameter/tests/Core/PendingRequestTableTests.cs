using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class PendingRequestTableTests
{
    [Test]
    public void RegistrationOwnsInputAndIndexesNormalizedPeerAndHopIdentifier()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 5000);
        PendingRequestTestInput input = CreatePendingRequestTestInput(
            41,
            2000,
            true);
        input.PeerIdentity.Value = "PEER.Example";
        input.SendRequest.CorrelationText = "original";
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 1;
        avp.Data = new byte[] { 1, 2, 3 };
        input.OutboundMessage.Avps.Add(avp);

        PendingRequestTableResult registered = table.RegisterPendingRequest(
            input.SendRequest,
            input.PeerIdentity,
            input.OutboundMessage,
            input.Handler,
            true);
        input.PeerIdentity.Value = "changed.example";
        input.SendRequest.CorrelationText = "changed";
        input.OutboundMessage.CommandCode = 999;
        input.OutboundMessage.Avps[0].Data[0] = 9;
        PendingRequestTableResult found = table.ReadPendingRequest(
            "peer.example",
            41);

        Assert.Multiple(() =>
        {
            Assert.That(registered.Accepted, Is.True);
            Assert.That(registered.Code, Is.EqualTo("pending-request-registered"));
            Assert.That(registered.PendingRequest, Is.Not.Null);
            Assert.That(registered.PendingRequest!.RequestIdentifier,
                Is.GreaterThan(0));
            Assert.That(registered.PendingRequest.CreatedAt,
                Is.EqualTo(clock.UtcTime));
            Assert.That(registered.PendingRequest.DeadlineAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(2)));
            Assert.That(registered.PendingRequest.AttemptCount, Is.EqualTo(1));
            Assert.That(registered.PendingRequest.Delivered, Is.True);
            Assert.That(found.Found, Is.True);
            Assert.That(found.PendingRequest!.PeerIdentity.Value,
                Is.EqualTo("PEER.Example"));
            Assert.That(found.PendingRequest.SendRequest.CorrelationText,
                Is.EqualTo("original"));
            Assert.That(found.PendingRequest.OutboundMessage.CommandCode,
                Is.EqualTo(300));
            Assert.That(found.PendingRequest.OutboundMessage.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(table.EntriesByIdentifier.Count, Is.EqualTo(1));
            Assert.That(table.RequestOrder.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void CapacityAndKeyCollisionsDoNotPartiallyRegisterRequests()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 5000);
        PendingRequestTestInput first = CreatePendingRequestTestInput(
            1,
            1000,
            true);
        PendingRequestTestInput second = CreatePendingRequestTestInput(
            2,
            1000,
            true);
        PendingRequestTestInput third = CreatePendingRequestTestInput(
            3,
            1000,
            true);

        PendingRequestTableResult addedFirst = RegisterPendingRequestTestInput(
            table,
            first);
        PendingRequestTableResult collision = RegisterPendingRequestTestInput(
            table,
            first);
        PendingRequestTableResult addedSecond = RegisterPendingRequestTestInput(
            table,
            second);
        PendingRequestTableResult full = RegisterPendingRequestTestInput(
            table,
            third);
        table.CancelPendingRequest(
            addedFirst.PendingRequest!.RequestIdentifier);
        PendingRequestTableResult afterCleanup = RegisterPendingRequestTestInput(
            table,
            third);

        Assert.Multiple(() =>
        {
            Assert.That(addedFirst.Accepted, Is.True);
            Assert.That(collision.Accepted, Is.False);
            Assert.That(collision.Code,
                Is.EqualTo("pending-request-key-collision"));
            Assert.That(addedSecond.Accepted, Is.True);
            Assert.That(full.Accepted, Is.False);
            Assert.That(full.CapacityReached, Is.True);
            Assert.That(full.Code,
                Is.EqualTo("pending-request-limit-reached"));
            Assert.That(afterCleanup.Accepted, Is.True);
            Assert.That(table.ReadPendingRequestCount(), Is.EqualTo(2));
            Assert.That(table.EntriesByKey.Count,
                Is.EqualTo(table.EntriesByIdentifier.Count));
            Assert.That(table.RequestOrder.Count,
                Is.EqualTo(table.EntriesByIdentifier.Count));
        });
    }

    [Test]
    public void CompletionRecognizesLateAnswersUntilBoundedTombstoneExpiry()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 5000);
        PendingRequestTestInput reported = CreatePendingRequestTestInput(
            11,
            1000,
            true);
        PendingRequestTestInput silent = CreatePendingRequestTestInput(
            12,
            1000,
            false);
        RegisterPendingRequestTestInput(table, reported);
        RegisterPendingRequestTestInput(table, silent);
        DiameterMessage reportedAnswer = CreatePendingRequestTestAnswer(
            reported.OutboundMessage);
        DiameterMessage silentAnswer = CreatePendingRequestTestAnswer(
            silent.OutboundMessage);

        PendingRequestTableResult completed = table.CompletePendingRequest(
            "peer.example",
            11,
            reportedAnswer);
        PendingRequestTableResult late = table.CompletePendingRequest(
            "PEER.EXAMPLE",
            11,
            reportedAnswer);
        PendingRequestTableResult silentCompleted = table.CompletePendingRequest(
            "peer.example",
            12,
            silentAnswer);
        PendingRequestTableResult silentLate = table.CompletePendingRequest(
            "peer.example",
            12,
            silentAnswer);
        reportedAnswer.CommandCode = 999;

        Assert.Multiple(() =>
        {
            Assert.That(completed.Found, Is.True);
            Assert.That(completed.Late, Is.False);
            Assert.That(completed.Handler, Is.SameAs(reported.Handler));
            Assert.That(completed.AnswerResult!.AnswerReceived, Is.True);
            Assert.That(completed.AnswerResult.Correlated, Is.True);
            Assert.That(completed.AnswerResult.Late, Is.False);
            Assert.That(completed.AnswerResult.Answer!.CommandCode,
                Is.EqualTo(300));
            Assert.That(late.Found, Is.True);
            Assert.That(late.Late, Is.True);
            Assert.That(late.Handler, Is.SameAs(reported.Handler));
            Assert.That(late.AnswerResult!.Late, Is.True);
            Assert.That(silentCompleted.Handler, Is.SameAs(silent.Handler));
            Assert.That(silentLate.Found, Is.True);
            Assert.That(silentLate.Late, Is.True);
            Assert.That(silentLate.Handler, Is.Null);
            Assert.That(silentLate.AnswerResult, Is.Null);
            Assert.That(table.ReadPendingRequestCount(), Is.Zero);
            Assert.That(table.ReadPendingRequestTombstoneCount(), Is.EqualTo(2));
        });

        clock.AdvanceDiscoveryTime(5);
        PendingRequestTableResult expired = table.ReadPendingRequest(
            "peer.example",
            11);

        Assert.Multiple(() =>
        {
            Assert.That(expired.Found, Is.False);
            Assert.That(expired.Code, Is.EqualTo("pending-request-not-found"));
            Assert.That(table.ReadPendingRequestTombstoneCount(), Is.Zero);
        });
    }

    [Test]
    public void CancellationAndTimeoutRemoveEveryActiveIndexExactlyOnce()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 3, 10000);
        PendingRequestTestInput first = CreatePendingRequestTestInput(
            21,
            1000,
            true);
        PendingRequestTestInput second = CreatePendingRequestTestInput(
            22,
            3000,
            true);
        PendingRequestTableResult registeredFirst =
            RegisterPendingRequestTestInput(table, first);
        PendingRequestTableResult registeredSecond =
            RegisterPendingRequestTestInput(table, second);
        clock.AdvanceDiscoveryTime(1);

        List<PendingRequestTableResult> expired =
            table.ExpirePendingRequests();
        List<PendingRequestTableResult> repeatedExpiry =
            table.ExpirePendingRequests();
        PendingRequestTableResult cancelled = table.CancelPendingRequest(
            registeredSecond.PendingRequest!.RequestIdentifier);
        PendingRequestTableResult repeatedCancellation =
            table.CancelPendingRequest(
                registeredSecond.PendingRequest.RequestIdentifier);
        PendingRequestTableResult late = table.CompletePendingRequest(
            "peer.example",
            21,
            CreatePendingRequestTestAnswer(first.OutboundMessage));

        Assert.Multiple(() =>
        {
            Assert.That(expired, Has.Count.EqualTo(1));
            Assert.That(expired[0].PendingRequest!.RequestIdentifier,
                Is.EqualTo(registeredFirst.PendingRequest!.RequestIdentifier));
            Assert.That(expired[0].Handler, Is.SameAs(first.Handler));
            Assert.That(expired[0].AnswerResult!.TimedOut, Is.True);
            Assert.That(expired[0].AnswerResult!.Cancelled, Is.False);
            Assert.That(repeatedExpiry, Is.Empty);
            Assert.That(cancelled.Found, Is.True);
            Assert.That(cancelled.Handler, Is.SameAs(second.Handler));
            Assert.That(cancelled.AnswerResult!.Cancelled, Is.True);
            Assert.That(cancelled.AnswerResult!.TimedOut, Is.False);
            Assert.That(repeatedCancellation.Found, Is.False);
            Assert.That(late.Found, Is.True);
            Assert.That(late.Late, Is.True);
            Assert.That(table.ReadPendingRequestCount(), Is.Zero);
            Assert.That(table.EntriesByKey, Is.Empty);
            Assert.That(table.EntriesByIdentifier, Is.Empty);
            Assert.That(table.RequestOrder, Is.Empty);
        });
    }

    [Test]
    public void PendingSnapshotsAreIndependentAndRemainInCreationOrder()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 5000);
        RegisterPendingRequestTestInput(
            table,
            CreatePendingRequestTestInput(72, 1000, true));
        RegisterPendingRequestTestInput(
            table,
            CreatePendingRequestTestInput(71, 1000, true));

        List<DiameterPendingRequestSnapshot> first =
            table.ReadPendingRequestSnapshots();
        first[0].PeerIdentity.Value = "mutated.example";
        first[0].OutboundMessage.CommandCode = 999;
        first.RemoveAt(1);
        List<DiameterPendingRequestSnapshot> second =
            table.ReadPendingRequestSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(second, Has.Count.EqualTo(2));
            Assert.That(second[0].HopByHopIdentifier, Is.EqualTo(72));
            Assert.That(second[1].HopByHopIdentifier, Is.EqualTo(71));
            Assert.That(second[0].RequestIdentifier,
                Is.LessThan(second[1].RequestIdentifier));
            Assert.That(second[0].PeerIdentity.Value,
                Is.EqualTo("peer.example"));
            Assert.That(second[0].OutboundMessage.CommandCode,
                Is.EqualTo(300));
        });
    }

    [Test]
    public void InvalidStoredMessagesAreRejectedWithoutPartialMutation()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 5000);
        PendingRequestTestInput mismatch = CreatePendingRequestTestInput(
            81,
            1000,
            true);
        DiameterMessage originalRequest = new DiameterMessage();
        originalRequest.Flags = DiameterMessageFlags.Request;
        originalRequest.CommandCode = 300;
        originalRequest.ApplicationId = 4;
        mismatch.SendRequest.Request = originalRequest;
        mismatch.OutboundMessage.ApplicationId = 5;
        PendingRequestTestInput invalidAvp = CreatePendingRequestTestInput(
            82,
            1000,
            true);
        DiameterAvp avp = new DiameterAvp();
        avp.Data = null!;
        invalidAvp.OutboundMessage.Avps.Add(avp);
        PendingRequestTestInput valid = CreatePendingRequestTestInput(
            83,
            1000,
            true);
        RegisterPendingRequestTestInput(table, valid);
        DiameterMessage invalidAnswer = CreatePendingRequestTestAnswer(
            valid.OutboundMessage);
        invalidAnswer.Avps = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                RegisterPendingRequestTestInput(table, mismatch));
            Assert.Throws<ArgumentException>(() =>
                RegisterPendingRequestTestInput(table, invalidAvp));
            Assert.Throws<ArgumentException>(() =>
                table.CompletePendingRequest(
                    "peer.example",
                    83,
                    invalidAnswer));
            Assert.That(table.ReadPendingRequestCount(), Is.EqualTo(1));
            Assert.That(table.ReadPendingRequest("peer.example", 83).Found,
                Is.True);
            Assert.That(table.ReadPendingRequestTombstoneCount(), Is.Zero);
        });
    }

    [Test]
    public void ConcurrentRegistrationForOneCorrelationKeyHasOneWinner()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 1, 5000);
        PendingRequestTestInput input = CreatePendingRequestTestInput(
            88,
            1000,
            true);
        Barrier barrier = new Barrier(17);
        List<Task<PendingRequestTableResult>> tasks =
            new List<Task<PendingRequestTableResult>>();
        int index = 0;
        while (index < 16)
        {
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                return RegisterPendingRequestTestInput(table, input);
            }));
            index = index + 1;
        }

        barrier.SignalAndWait();
        Task.WaitAll(tasks.ToArray());
        int accepted = 0;
        int collisions = 0;
        index = 0;
        while (index < tasks.Count)
        {
            PendingRequestTableResult result = tasks[index].Result;
            if (result.Accepted)
            {
                accepted = accepted + 1;
            }
            else if (result.Code == "pending-request-key-collision")
            {
                collisions = collisions + 1;
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo(1));
            Assert.That(collisions, Is.EqualTo(15));
            Assert.That(table.ReadPendingRequestCount(), Is.EqualTo(1));
            Assert.That(table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(table.EntriesByIdentifier.Count, Is.EqualTo(1));
            Assert.That(table.RequestOrder.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void TombstoneRetentionIsBoundedByPendingCapacity()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 10000);
        int identifier = 1;
        while (identifier <= 3)
        {
            PendingRequestTestInput input = CreatePendingRequestTestInput(
                (uint)identifier,
                1000,
                true);
            PendingRequestTableResult registered =
                RegisterPendingRequestTestInput(table, input);
            table.CancelPendingRequest(
                registered.PendingRequest!.RequestIdentifier);
            identifier = identifier + 1;
        }

        PendingRequestTableResult evicted = table.ReadPendingRequest(
            "peer.example",
            1);
        PendingRequestTableResult retained = table.ReadPendingRequest(
            "peer.example",
            3);

        Assert.Multiple(() =>
        {
            Assert.That(table.ReadPendingRequestTombstoneCount(), Is.EqualTo(2));
            Assert.That(evicted.Found, Is.False);
            Assert.That(retained.Found, Is.True);
            Assert.That(retained.Late, Is.True);
        });
    }

    [Test]
    public void DeliveryRecordingAndTerminalFailureCleanEveryActiveIndex()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        PendingRequestTable table = new PendingRequestTable(clock, 2, 5000);
        PendingRequestTestInput input = CreatePendingRequestTestInput(
            91,
            5000,
            true);
        PendingRequestTableResult registered = table.RegisterPendingRequest(
            input.SendRequest,
            input.PeerIdentity,
            input.OutboundMessage,
            input.Handler,
            false);

        PendingRequestTableResult stale =
            table.RecordPendingRequestDelivery(
                registered.PendingRequest!.RequestIdentifier,
                92,
                true);
        PendingRequestTableResult delivered =
            table.RecordPendingRequestDelivery(
                registered.PendingRequest.RequestIdentifier,
                91,
                true);
        PendingRequestTableResult failed = table.FailPendingRequest(
            registered.PendingRequest.RequestIdentifier,
            true,
            "request-transport-failed");
        PendingRequestTableResult repeated = table.FailPendingRequest(
            registered.PendingRequest.RequestIdentifier,
            true,
            "request-transport-failed");
        PendingRequestTableResult tombstone = table.ReadPendingRequest(
            "peer.example",
            91);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Found, Is.False);
            Assert.That(stale.Code,
                Is.EqualTo("pending-request-attempt-changed"));
            Assert.That(delivered.Found, Is.True);
            Assert.That(delivered.PendingRequest!.Delivered, Is.True);
            Assert.That(failed.Found, Is.True);
            Assert.That(failed.Handler, Is.SameAs(input.Handler));
            Assert.That(failed.AnswerResult!.PeerFailed, Is.True);
            Assert.That(failed.AnswerResult.Code,
                Is.EqualTo("request-transport-failed"));
            Assert.That(repeated.Found, Is.False);
            Assert.That(tombstone.Found, Is.True);
            Assert.That(tombstone.Late, Is.True);
            Assert.That(table.EntriesByKey, Is.Empty);
            Assert.That(table.EntriesByIdentifier, Is.Empty);
            Assert.That(table.RequestOrder, Is.Empty);
            Assert.That(table.ReadPendingRequestTombstoneCount(),
                Is.EqualTo(1));
        });
    }

    private static PendingRequestTestInput CreatePendingRequestTestInput(
        uint hopByHopIdentifier,
        int timeoutMilliseconds,
        bool reportLateAnswers)
    {
        PendingRequestTestInput result = new PendingRequestTestInput();
        result.PeerIdentity.Value = "peer.example";
        result.OutboundMessage.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.OutboundMessage.CommandCode = 300;
        result.OutboundMessage.ApplicationId = 4;
        result.OutboundMessage.HopByHopIdentifier = hopByHopIdentifier;
        result.OutboundMessage.EndToEndIdentifier =
            hopByHopIdentifier + 1000;
        result.SendRequest.Request = result.OutboundMessage;
        result.SendRequest.TimeoutMilliseconds = timeoutMilliseconds;
        result.SendRequest.ReportLateAnswers = reportLateAnswers;
        return result;
    }

    private static PendingRequestTableResult RegisterPendingRequestTestInput(
        PendingRequestTable table,
        PendingRequestTestInput input)
    {
        return table.RegisterPendingRequest(
            input.SendRequest,
            input.PeerIdentity,
            input.OutboundMessage,
            input.Handler,
            true);
    }

    private static DiameterMessage CreatePendingRequestTestAnswer(
        DiameterMessage request)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        return result;
    }
}

internal class PendingRequestTestInput
{
    internal DiameterSendRequest SendRequest = new DiameterSendRequest();
    internal DiameterIdentity PeerIdentity = new DiameterIdentity();
    internal DiameterMessage OutboundMessage = new DiameterMessage();
    internal PendingRequestTestHandler Handler = new PendingRequestTestHandler();
}

internal class PendingRequestTestHandler : IDiameterAnswerHandler
{
    internal List<DiameterAnswerResult> Results =
        new List<DiameterAnswerResult>();

    public void HandleDiameterAnswer(DiameterAnswerResult result)
    {
        Results.Add(result);
    }
}
