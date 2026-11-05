using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class RequestDataTests
{
    [Test]
    public void RequestDataDefaultsRepresentUncompletedOperations()
    {
        DiameterSendRequest request = new DiameterSendRequest();
        DiameterSendResult send = new DiameterSendResult();
        DiameterAnswerResult answer = new DiameterAnswerResult();
        DiameterPendingRequestSnapshot pending =
            new DiameterPendingRequestSnapshot();
        DiameterDuplicateResult duplicate = new DiameterDuplicateResult();

        Assert.Multiple(() =>
        {
            Assert.That(request.Request, Is.Not.Null);
            Assert.That(request.PreferredPeer, Is.Null);
            Assert.That(request.TimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(request.AllowFailover, Is.True);
            Assert.That(request.AllowRetransmission, Is.True);
            Assert.That(request.ReportLateAnswers, Is.True);
            Assert.That(request.CorrelationText, Is.Empty);
            Assert.That(send.Status, Is.EqualTo(DiameterSendStatus.Accepted));
            Assert.That(send.PendingRequest, Is.Null);
            Assert.That(send.PeerIdentity, Is.Null);
            Assert.That(send.Accepted, Is.False);
            Assert.That(send.Delivered, Is.False);
            Assert.That(answer.RequestIdentifier, Is.Zero);
            Assert.That(answer.PendingRequest, Is.Null);
            Assert.That(answer.Answer, Is.Null);
            Assert.That(answer.PeerIdentity, Is.Null);
            Assert.That(answer.AnswerReceived, Is.False);
            Assert.That(answer.Correlated, Is.False);
            Assert.That(answer.Successful, Is.False);
            Assert.That(answer.ErrorAnswer, Is.False);
            Assert.That(answer.TimedOut, Is.False);
            Assert.That(answer.Cancelled, Is.False);
            Assert.That(answer.PeerFailed, Is.False);
            Assert.That(answer.Late, Is.False);
            Assert.That(answer.ResultCode, Is.Zero);
            Assert.That(answer.ExperimentalResult, Is.Null);
            Assert.That(answer.ProtocolError, Is.False);
            Assert.That(answer.ApplicationError, Is.False);
            Assert.That(pending.SendRequest, Is.Not.Null);
            Assert.That(pending.OutboundMessage, Is.Not.Null);
            Assert.That(pending.PeerIdentity, Is.Not.Null);
            Assert.That(pending.RequestIdentifier, Is.Zero);
            Assert.That(pending.AttemptCount, Is.Zero);
            Assert.That(pending.Delivered, Is.False);
            Assert.That(pending.FailoverEligible, Is.False);
            Assert.That(pending.RetransmissionEligible, Is.False);
            Assert.That(duplicate.OriginHost, Is.Not.Null);
            Assert.That(duplicate.Request, Is.Null);
            Assert.That(duplicate.CachedAnswer, Is.Null);
            Assert.That(duplicate.Duplicate, Is.False);
            Assert.That(duplicate.Retransmission, Is.False);
            Assert.That(duplicate.CachedAnswerAvailable, Is.False);
            Assert.That(duplicate.ReplaySafe, Is.False);
            Assert.That(duplicate.ObservationCount, Is.Zero);
        });
    }

    [Test]
    public void SendRequestValidationAcceptsRoutedAndPreferredPeerRequests()
    {
        DiameterSendRequest routed = CreateValidSendRequest();
        DiameterSendRequest preferred = CreateValidSendRequest();
        preferred.PreferredPeer = new DiameterIdentity();
        preferred.PreferredPeer.Value = "peer.example";
        preferred.TimeoutMilliseconds = 1;
        preferred.CorrelationText = "operation-42";

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(routed.ValidateSendRequest);
            Assert.DoesNotThrow(preferred.ValidateSendRequest);
        });
    }

    [Test]
    public void SendRequestValidationRejectsInvalidMessagesAndPolicy()
    {
        DiameterSendRequest missingMessage = CreateValidSendRequest();
        missingMessage.Request = null!;
        DiameterSendRequest missingAvps = CreateValidSendRequest();
        missingAvps.Request.Avps = null!;
        DiameterSendRequest answer = CreateValidSendRequest();
        answer.Request.Flags = DiameterMessageFlags.None;
        DiameterSendRequest errorRequest = CreateValidSendRequest();
        errorRequest.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Error;
        DiameterSendRequest retransmittedRequest = CreateValidSendRequest();
        retransmittedRequest.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.PotentiallyRetransmitted;
        DiameterSendRequest wrongVersion = CreateValidSendRequest();
        wrongVersion.Request.Version = 2;
        DiameterSendRequest zeroCommand = CreateValidSendRequest();
        zeroCommand.Request.CommandCode = 0;
        DiameterSendRequest largeCommand = CreateValidSendRequest();
        largeCommand.Request.CommandCode =
            DiameterProtocolConstants.MaximumCommandCode + 1;
        DiameterSendRequest invalidPeer = CreateValidSendRequest();
        invalidPeer.PreferredPeer = new DiameterIdentity();
        invalidPeer.PreferredPeer.Value = "not a diameter identity";
        DiameterSendRequest invalidTimeout = CreateValidSendRequest();
        invalidTimeout.TimeoutMilliseconds = 0;
        DiameterSendRequest nullCorrelation = CreateValidSendRequest();
        nullCorrelation.CorrelationText = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(missingMessage.ValidateSendRequest);
            Assert.Throws<ArgumentException>(missingAvps.ValidateSendRequest);
            Assert.Throws<ArgumentException>(answer.ValidateSendRequest);
            Assert.Throws<ArgumentException>(errorRequest.ValidateSendRequest);
            Assert.Throws<ArgumentException>(
                retransmittedRequest.ValidateSendRequest);
            Assert.Throws<ArgumentException>(wrongVersion.ValidateSendRequest);
            Assert.Throws<ArgumentException>(zeroCommand.ValidateSendRequest);
            Assert.Throws<ArgumentException>(largeCommand.ValidateSendRequest);
            Assert.Throws<ArgumentException>(invalidPeer.ValidateSendRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalidTimeout.ValidateSendRequest);
            Assert.Throws<ArgumentException>(nullCorrelation.ValidateSendRequest);
        });
    }

    [Test]
    public void RequestClientContractCarriesSendCancelAndPendingOutcomes()
    {
        RequestClientContractProbe probe = new RequestClientContractProbe();
        AnswerHandlerContractProbe handler = new AnswerHandlerContractProbe();
        DiameterSendRequest request = CreateValidSendRequest();
        probe.SendOutcome.Accepted = true;
        probe.SendOutcome.Status = DiameterSendStatus.Accepted;
        probe.SendOutcome.PendingRequest = probe.Pending;
        probe.Pending.RequestIdentifier = 71;
        probe.Pending.SendRequest = request;
        probe.Pending.OutboundMessage = request.Request;
        probe.Pending.AttemptCount = 1;
        probe.Pending.FailoverEligible = true;
        probe.Pending.RetransmissionEligible = true;
        probe.PendingItems.Add(probe.Pending);
        probe.CancelOutcome.RequestIdentifier = 71;
        probe.CancelOutcome.PendingRequest = probe.Pending;
        probe.CancelOutcome.Cancelled = true;
        probe.CancelOutcome.Code = "request-cancelled";
        IDiameterRequestClient client = probe;

        DiameterSendResult send = client.SendDiameterRequest(request, handler);
        List<DiameterPendingRequestSnapshot> snapshots =
            client.ReadPendingDiameterRequests();
        DiameterAnswerResult cancelled = client.CancelDiameterRequest(71);

        Assert.Multiple(() =>
        {
            Assert.That(probe.ObservedRequest, Is.SameAs(request));
            Assert.That(probe.ObservedHandler, Is.SameAs(handler));
            Assert.That(send, Is.SameAs(probe.SendOutcome));
            Assert.That(send.PendingRequest, Is.SameAs(probe.Pending));
            Assert.That(snapshots, Is.SameAs(probe.PendingItems));
            Assert.That(cancelled, Is.SameAs(probe.CancelOutcome));
            Assert.That(probe.CancelledIdentifier, Is.EqualTo(71));
            Assert.That(handler.Outcome, Is.SameAs(cancelled));
            Assert.That(cancelled.PendingRequest!.SendRequest,
                Is.SameAs(request));
        });
    }

    [Test]
    public void DuplicateStoreContractCarriesDetectionAndReplayMetadata()
    {
        DuplicateStoreContractProbe probe = new DuplicateStoreContractProbe();
        DiameterMessage request = new DiameterMessage();
        request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.PotentiallyRetransmitted;
        request.CommandCode = 300;
        request.ApplicationId = 4;
        request.EndToEndIdentifier = 99;
        DiameterIdentity origin = new DiameterIdentity();
        origin.Value = "origin.example";
        DateTimeOffset receivedAt = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
        probe.Observation.OriginHost = origin;
        probe.Observation.EndToEndIdentifier = 99;
        probe.Observation.CommandCode = 300;
        probe.Observation.ApplicationId = 4;
        probe.Observation.Request = request;
        probe.Observation.Duplicate = true;
        probe.Observation.Retransmission = true;
        probe.Observation.ObservationCount = 2;
        probe.Observation.FirstSeenAt = receivedAt.AddSeconds(-1);
        probe.Observation.LastSeenAt = receivedAt;
        probe.Observation.ExpiresAt = receivedAt.AddMinutes(4);
        IDuplicateRequestStore store = probe;

        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(request, origin, receivedAt);
        DiameterMessage cachedAnswer = new DiameterMessage();
        cachedAnswer.CommandCode = request.CommandCode;
        cachedAnswer.ApplicationId = request.ApplicationId;
        cachedAnswer.EndToEndIdentifier = request.EndToEndIdentifier;
        observed.CachedAnswer = cachedAnswer;
        observed.CachedAnswerAvailable = true;
        observed.ReplaySafe = true;
        store.CacheDiameterDuplicateAnswer(observed);

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.SameAs(probe.Observation));
            Assert.That(probe.ObservedRequest, Is.SameAs(request));
            Assert.That(probe.ObservedOriginHost, Is.SameAs(origin));
            Assert.That(probe.ObservedAt, Is.EqualTo(receivedAt));
            Assert.That(observed.Duplicate, Is.True);
            Assert.That(observed.Retransmission, Is.True);
            Assert.That(observed.ObservationCount, Is.EqualTo(2));
            Assert.That(probe.CachedResult, Is.SameAs(observed));
            Assert.That(probe.CachedResult!.CachedAnswer,
                Is.SameAs(cachedAnswer));
            Assert.That(probe.CachedResult.ReplaySafe, Is.True);
        });
    }

    private static DiameterSendRequest CreateValidSendRequest()
    {
        DiameterSendRequest result = new DiameterSendRequest();
        result.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Request.CommandCode = 300;
        result.Request.ApplicationId = 4;
        return result;
    }
}

internal class RequestClientContractProbe : IDiameterRequestClient
{
    internal DiameterSendResult SendOutcome = new DiameterSendResult();
    internal DiameterAnswerResult CancelOutcome = new DiameterAnswerResult();
    internal DiameterPendingRequestSnapshot Pending =
        new DiameterPendingRequestSnapshot();
    internal List<DiameterPendingRequestSnapshot> PendingItems =
        new List<DiameterPendingRequestSnapshot>();
    internal DiameterSendRequest? ObservedRequest;
    internal IDiameterAnswerHandler? ObservedHandler;
    internal long CancelledIdentifier;

    public DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        ObservedRequest = request;
        ObservedHandler = handler;
        return SendOutcome;
    }

    public DiameterAnswerResult CancelDiameterRequest(long requestIdentifier)
    {
        CancelledIdentifier = requestIdentifier;
        if (ObservedHandler != null)
        {
            ObservedHandler.HandleDiameterAnswer(CancelOutcome);
        }

        return CancelOutcome;
    }

    public List<DiameterPendingRequestSnapshot> ReadPendingDiameterRequests()
    {
        return PendingItems;
    }
}

internal class AnswerHandlerContractProbe : IDiameterAnswerHandler
{
    internal DiameterAnswerResult? Outcome;

    public void HandleDiameterAnswer(DiameterAnswerResult result)
    {
        Outcome = result;
    }
}

internal class DuplicateStoreContractProbe : IDuplicateRequestStore
{
    internal DiameterDuplicateResult Observation = new DiameterDuplicateResult();
    internal DiameterMessage? ObservedRequest;
    internal DiameterIdentity? ObservedOriginHost;
    internal DateTimeOffset ObservedAt;
    internal DiameterDuplicateResult? CachedResult;

    public DiameterDuplicateResult ObserveDiameterDuplicateRequest(
        DiameterMessage? request,
        DiameterIdentity? originHost,
        DateTimeOffset receivedAt)
    {
        ObservedRequest = request;
        ObservedOriginHost = originHost;
        ObservedAt = receivedAt;
        return Observation;
    }

    public void CacheDiameterDuplicateAnswer(DiameterDuplicateResult? result)
    {
        CachedResult = result;
    }
}
