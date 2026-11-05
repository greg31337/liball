using System;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class RequestFailoverControllerTests
{
    [Test]
    public void EligibleRequestRetransmitsToAlternateWithNewHopIdentifier()
    {
        RequestFailoverTestContext context = CreateRequestFailoverTestContext(
            100,
            true,
            true,
            true,
            null);
        context.IdentifierSource.HopByHopIdentifier = 199;
        DateTimeOffset originalDeadline =
            context.Registration.PendingRequest!.DeadlineAt;

        RequestFailoverResult result =
            context.Controller.FailoverPendingRequest(
                context.RequestIdentifier,
                CreateRequestFailoverIdentity("ALT.EXAMPLE"),
                new RequestFailoverPolicy());
        result.Message!.CommandCode = 999;

        DiameterPendingRequestSnapshot current =
            context.Table.ReadPendingRequestSnapshots()[0];
        PendingRequestTableResult originalAttempt =
            context.Table.ReadPendingRequest("peer.example", 100);
        PendingRequestTableResult alternateAttempt =
            context.Table.ReadPendingRequest("alt.example", 200);

        Assert.Multiple(() =>
        {
            Assert.That(result.Eligible, Is.True);
            Assert.That(result.Retried, Is.True);
            Assert.That(result.AlternateRoute, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("request-retransmitted-to-alternate"));
            Assert.That(result.PeerIdentity!.Value,
                Is.EqualTo("alt.example"));
            Assert.That(current.OriginalHopByHopIdentifier, Is.EqualTo(100));
            Assert.That(current.HopByHopIdentifier, Is.EqualTo(200));
            Assert.That(current.EndToEndIdentifier, Is.EqualTo(1100));
            Assert.That(current.OutboundMessage.EndToEndIdentifier,
                Is.EqualTo(1100));
            Assert.That((current.OutboundMessage.Flags &
                DiameterMessageFlags.PotentiallyRetransmitted),
                Is.EqualTo(DiameterMessageFlags.PotentiallyRetransmitted));
            Assert.That((current.OutboundMessage.Flags &
                DiameterMessageFlags.Request),
                Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That((current.OutboundMessage.Flags &
                DiameterMessageFlags.Proxyable),
                Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That(current.AttemptCount, Is.EqualTo(2));
            Assert.That(current.DeadlineAt, Is.EqualTo(originalDeadline));
            Assert.That(current.OutboundMessage.CommandCode, Is.EqualTo(300));
            Assert.That(context.Request.HopByHopIdentifier, Is.EqualTo(100));
            Assert.That((context.Request.Flags &
                DiameterMessageFlags.PotentiallyRetransmitted),
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(originalAttempt.Found, Is.True);
            Assert.That(originalAttempt.PendingRequest!.HopByHopIdentifier,
                Is.EqualTo(100));
            Assert.That(alternateAttempt.Found, Is.True);
            Assert.That(alternateAttempt.PendingRequest!.HopByHopIdentifier,
                Is.EqualTo(200));
            Assert.That(context.Table.EntriesByKey.Count, Is.EqualTo(2));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AnswerFromEitherAttemptCompletesOnceAndOtherAnswerIsLate()
    {
        RequestFailoverTestContext context = CreateRequestFailoverTestContext(
            101,
            true,
            true,
            true,
            null);
        context.IdentifierSource.HopByHopIdentifier = 200;
        RequestFailoverResult retry =
            context.Controller.FailoverPendingRequest(
                context.RequestIdentifier,
                CreateRequestFailoverIdentity("alternate.example"),
                new RequestFailoverPolicy());
        AnswerCorrelator correlator = new AnswerCorrelator(context.Table);
        DiameterMessage originalAnswer = CreateRequestFailoverAnswer(
            context.Request);
        DiameterMessage alternateAnswer = CreateRequestFailoverAnswer(
            retry.PendingRequest!.OutboundMessage);

        AnswerCorrelationResult completed =
            correlator.CorrelateDiameterAnswer(
                "peer.example",
                originalAnswer);
        AnswerCorrelationResult late =
            correlator.CorrelateDiameterAnswer(
                "alternate.example",
                alternateAnswer);

        Assert.Multiple(() =>
        {
            Assert.That(completed.Completed, Is.True);
            Assert.That(completed.Late, Is.False);
            Assert.That(completed.AnswerResult!.PeerIdentity!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(completed.PendingRequest!.AttemptCount, Is.EqualTo(2));
            Assert.That(late.Completed, Is.False);
            Assert.That(late.Late, Is.True);
            Assert.That(late.AnswerResult!.PeerIdentity!.Value,
                Is.EqualTo("alternate.example"));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.Zero);
            Assert.That(context.Table.EntriesByKey, Is.Empty);
            Assert.That(context.Table.ReadPendingRequestTombstoneCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void CallerApplicationAndSessionPoliciesIndependentlyDenyFailover()
    {
        RequestFailoverTestContext caller = CreateRequestFailoverTestContext(
            102,
            false,
            true,
            true,
            null);
        RequestFailoverTestContext retransmission =
            CreateRequestFailoverTestContext(
                103,
                true,
                false,
                true,
                null);
        RequestFailoverTestContext application =
            CreateRequestFailoverTestContext(
                104,
                true,
                true,
                true,
                null);
        RequestFailoverPolicy applicationPolicy =
            new RequestFailoverPolicy();
        applicationPolicy.ApplicationAllowsFailover = false;
        RequestFailoverTestContext session = CreateRequestFailoverTestContext(
            105,
            true,
            true,
            true,
            null);
        RequestFailoverPolicy sessionPolicy = new RequestFailoverPolicy();
        sessionPolicy.SessionAllowsFailover = false;

        RequestFailoverResult callerResult = FailoverRequestForPolicyTest(
            caller,
            new RequestFailoverPolicy());
        RequestFailoverResult retransmissionResult =
            FailoverRequestForPolicyTest(
                retransmission,
                new RequestFailoverPolicy());
        RequestFailoverResult applicationResult =
            FailoverRequestForPolicyTest(application, applicationPolicy);
        RequestFailoverResult sessionResult =
            FailoverRequestForPolicyTest(session, sessionPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(callerResult.Code,
                Is.EqualTo("request-failover-disabled"));
            Assert.That(retransmissionResult.Code,
                Is.EqualTo("request-retransmission-disabled"));
            Assert.That(applicationResult.Code,
                Is.EqualTo("application-failover-denied"));
            Assert.That(sessionResult.Code,
                Is.EqualTo("session-failover-denied"));
            Assert.That(caller.Table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(retransmission.Table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(application.Table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(session.Table.EntriesByKey.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void RetransmissionRequiresUncertainDeliveryAndDistinctValidPeer()
    {
        RequestFailoverTestContext undelivered =
            CreateRequestFailoverTestContext(
                106,
                true,
                true,
                false,
                null);
        RequestFailoverTestContext certain = CreateRequestFailoverTestContext(
            107,
            true,
            true,
            true,
            null);
        RequestFailoverPolicy certainPolicy = new RequestFailoverPolicy();
        certainPolicy.DeliveryUncertain = false;
        RequestFailoverTestContext peer = CreateRequestFailoverTestContext(
            108,
            true,
            true,
            true,
            null);

        RequestFailoverResult notDelivered = FailoverRequestForPolicyTest(
            undelivered,
            new RequestFailoverPolicy());
        RequestFailoverResult notUncertain = FailoverRequestForPolicyTest(
            certain,
            certainPolicy);
        RequestFailoverResult samePeer =
            peer.Controller.FailoverPendingRequest(
                peer.RequestIdentifier,
                CreateRequestFailoverIdentity("PEER.EXAMPLE"),
                new RequestFailoverPolicy());
        RequestFailoverResult invalidPeer =
            peer.Controller.FailoverPendingRequest(
                peer.RequestIdentifier,
                CreateRequestFailoverIdentity("not a peer"),
                new RequestFailoverPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(notDelivered.Code,
                Is.EqualTo("request-not-retransmittable"));
            Assert.That(notUncertain.Code,
                Is.EqualTo("request-not-retransmittable"));
            Assert.That(samePeer.Code,
                Is.EqualTo("alternate-peer-not-distinct"));
            Assert.That(invalidPeer.Code,
                Is.EqualTo("invalid-alternate-peer"));
            Assert.That(peer.Table.ReadPendingRequestCount(), Is.EqualTo(1));
            Assert.That(peer.Table.EntriesByKey.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void FixedDestinationRequiresSessionPolicyRemoval()
    {
        RequestFailoverTestContext denied = CreateRequestFailoverTestContext(
            109,
            true,
            true,
            true,
            "peer.example");
        RequestFailoverTestContext allowed = CreateRequestFailoverTestContext(
            110,
            true,
            true,
            true,
            "peer.example");
        allowed.IdentifierSource.HopByHopIdentifier = 209;
        RequestFailoverPolicy removal = new RequestFailoverPolicy();
        removal.RemoveDestinationHost = true;
        RequestFailoverTestContext routed = CreateRequestFailoverTestContext(
            111,
            true,
            true,
            true,
            "server.example");
        routed.IdentifierSource.HopByHopIdentifier = 210;

        RequestFailoverResult deniedResult = FailoverRequestForPolicyTest(
            denied,
            new RequestFailoverPolicy());
        RequestFailoverResult allowedResult =
            allowed.Controller.FailoverPendingRequest(
                allowed.RequestIdentifier,
                CreateRequestFailoverIdentity("alternate.example"),
                removal);
        RequestFailoverResult routedResult =
            routed.Controller.FailoverPendingRequest(
                routed.RequestIdentifier,
                CreateRequestFailoverIdentity("agent.example"),
                new RequestFailoverPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(deniedResult.Code,
                Is.EqualTo("fixed-destination-failover-denied"));
            Assert.That(allowedResult.Retried, Is.True);
            Assert.That(CountRequestFailoverAvps(
                allowedResult.Message!,
                DiameterAvpCodes.DestinationHost),
                Is.Zero);
            Assert.That(routedResult.Retried, Is.True);
            Assert.That(CountRequestFailoverAvps(
                routedResult.Message!,
                DiameterAvpCodes.DestinationHost),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void IdentifierCollisionAndExpiredDeadlineDoNotMutatePendingState()
    {
        RequestFailoverTestContext sameIdentifier =
            CreateRequestFailoverTestContext(
                112,
                true,
                true,
                true,
                null);
        sameIdentifier.IdentifierSource.HopByHopIdentifier = 111;
        RequestFailoverResult same = FailoverRequestForPolicyTest(
            sameIdentifier,
            new RequestFailoverPolicy());
        RequestFailoverTestContext expired = CreateRequestFailoverTestContext(
            113,
            true,
            true,
            true,
            null);
        expired.Clock.AdvanceDiscoveryTime(5);
        expired.IdentifierSource.HopByHopIdentifier = 212;
        RequestFailoverResult deadline = FailoverRequestForPolicyTest(
            expired,
            new RequestFailoverPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(same.Code,
                Is.EqualTo("hop-by-hop-identifier-not-new"));
            Assert.That(sameIdentifier.Table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(deadline.Eligible, Is.True);
            Assert.That(deadline.Retried, Is.False);
            Assert.That(deadline.Code,
                Is.EqualTo("pending-request-deadline-expired"));
            Assert.That(expired.Table.EntriesByKey.Count, Is.EqualTo(1));
            Assert.That(expired.Table.ReadPendingRequestSnapshots()[0]
                .AttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CancellationAfterRetransmissionCleansEveryAttemptKey()
    {
        RequestFailoverTestContext context = CreateRequestFailoverTestContext(
            115,
            true,
            true,
            true,
            null);
        context.IdentifierSource.HopByHopIdentifier = 214;
        RequestFailoverResult retry =
            context.Controller.FailoverPendingRequest(
                context.RequestIdentifier,
                CreateRequestFailoverIdentity("alternate.example"),
                new RequestFailoverPolicy());

        PendingRequestTableResult cancelled =
            context.Table.CancelPendingRequest(context.RequestIdentifier);
        PendingRequestTableResult original = context.Table.ReadPendingRequest(
            "peer.example",
            115);
        PendingRequestTableResult alternate = context.Table.ReadPendingRequest(
            "alternate.example",
            retry.PendingRequest!.HopByHopIdentifier);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Found, Is.True);
            Assert.That(cancelled.AnswerResult!.Cancelled, Is.True);
            Assert.That(cancelled.PendingRequest!.AttemptCount, Is.EqualTo(2));
            Assert.That(context.Table.EntriesByKey, Is.Empty);
            Assert.That(context.Table.ReadPendingRequestCount(), Is.Zero);
            Assert.That(original.Found, Is.True);
            Assert.That(original.Late, Is.True);
            Assert.That(alternate.Found, Is.True);
            Assert.That(alternate.Late, Is.True);
        });
    }

    [Test]
    public void ConcurrentFailoverFromOneAttemptHasOneAtomicWinner()
    {
        RequestFailoverTestContext context = CreateRequestFailoverTestContext(
            114,
            true,
            true,
            true,
            null);
        Barrier barrier = new Barrier(3);
        BarrierFailoverIdentifierSource identifiers =
            new BarrierFailoverIdentifierSource(barrier, 300);
        context.Controller = new RequestFailoverController(
            context.Table,
            identifiers);
        Task<RequestFailoverResult> first = Task.Run(() =>
            context.Controller.FailoverPendingRequest(
                context.RequestIdentifier,
                CreateRequestFailoverIdentity("alternate-one.example"),
                new RequestFailoverPolicy()));
        Task<RequestFailoverResult> second = Task.Run(() =>
            context.Controller.FailoverPendingRequest(
                context.RequestIdentifier,
                CreateRequestFailoverIdentity("alternate-two.example"),
                new RequestFailoverPolicy()));
        barrier.SignalAndWait();
        Task.WaitAll(first, second);
        int retried = (first.Result.Retried ? 1 : 0) +
            (second.Result.Retried ? 1 : 0);
        int changed = (first.Result.Code ==
            "pending-request-attempt-changed" ? 1 : 0) +
            (second.Result.Code ==
            "pending-request-attempt-changed" ? 1 : 0);

        Assert.Multiple(() =>
        {
            Assert.That(retried, Is.EqualTo(1));
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(context.Table.ReadPendingRequestCount(), Is.EqualTo(1));
            Assert.That(context.Table.EntriesByKey.Count, Is.EqualTo(2));
            Assert.That(context.Table.ReadPendingRequestSnapshots()[0]
                .AttemptCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void RoutedRetransmissionIsOwnedAndRetainsRouteTransformations()
    {
        RequestFailoverTestContext context = CreateRequestFailoverTestContext(
            116,
            true,
            true,
            true,
            null);
        context.IdentifierSource.HopByHopIdentifier = 215;
        DiameterMessage routed = context.Table.ClonePendingMessage(
            context.Request);
        DiameterAvp routeMarker = new DiameterAvp();
        routeMarker.Code = 999998U;
        routeMarker.Data = new byte[] { 1, 2, 3 };
        routed.Avps.Add(routeMarker);
        RequestFailoverPolicy policy = new RequestFailoverPolicy();
        policy.RoutedMessage = routed;

        RequestFailoverResult result =
            context.Controller.FailoverPendingRequest(
                context.RequestIdentifier,
                CreateRequestFailoverIdentity("alternate.example"),
                policy);
        routed.Avps[0].Data[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Retried, Is.True);
            Assert.That(result.Message!.Avps, Has.Count.EqualTo(1));
            Assert.That(result.Message.Avps[0].Code, Is.EqualTo(999998U));
            Assert.That(result.Message.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Message.HopByHopIdentifier, Is.EqualTo(216U));
            Assert.That(result.Message.EndToEndIdentifier,
                Is.EqualTo(context.Request.EndToEndIdentifier));
            Assert.That(result.Message.Flags.HasFlag(
                DiameterMessageFlags.PotentiallyRetransmitted), Is.True);
        });
    }

    private static RequestFailoverTestContext CreateRequestFailoverTestContext(
        uint hopByHopIdentifier,
        bool allowFailover,
        bool allowRetransmission,
        bool delivered,
        string? destinationHost)
    {
        RequestFailoverTestContext result =
            new RequestFailoverTestContext();
        result.Table = new PendingRequestTable(result.Clock, 8, 5000);
        result.Controller = new RequestFailoverController(
            result.Table,
            result.IdentifierSource);
        result.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Request.CommandCode = 300;
        result.Request.ApplicationId = 4;
        result.Request.HopByHopIdentifier = hopByHopIdentifier;
        result.Request.EndToEndIdentifier = hopByHopIdentifier + 1000;
        if (destinationHost != null)
        {
            result.Request.Avps.Add(
                new DiameterBaseAvpFactory().CreateDestinationHostAvp(
                    CreateRequestFailoverIdentity(destinationHost)));
        }

        DiameterSendRequest sendRequest = new DiameterSendRequest();
        sendRequest.Request = result.Request;
        sendRequest.TimeoutMilliseconds = 5000;
        sendRequest.AllowFailover = allowFailover;
        sendRequest.AllowRetransmission = allowRetransmission;
        result.Registration = result.Table.RegisterPendingRequest(
            sendRequest,
            CreateRequestFailoverIdentity("peer.example"),
            result.Request,
            result.Handler,
            delivered);
        result.RequestIdentifier =
            result.Registration.PendingRequest!.RequestIdentifier;
        return result;
    }

    private static RequestFailoverResult FailoverRequestForPolicyTest(
        RequestFailoverTestContext context,
        RequestFailoverPolicy policy)
    {
        return context.Controller.FailoverPendingRequest(
            context.RequestIdentifier,
            CreateRequestFailoverIdentity("alternate.example"),
            policy);
    }

    private static DiameterIdentity CreateRequestFailoverIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterMessage CreateRequestFailoverAnswer(
        DiameterMessage request)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        DiameterAvp resultCode = new DiameterAvp();
        resultCode.Code = DiameterAvpCodes.ResultCode;
        resultCode.Flags = DiameterAvpFlags.Mandatory;
        resultCode.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(
            resultCode.Data,
            DiameterResultCodes.Success);
        result.Avps.Add(resultCode);
        return result;
    }

    private static int CountRequestFailoverAvps(
        DiameterMessage message,
        uint code)
    {
        int count = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}

internal class RequestFailoverTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal PendingRequestTable Table = null!;
    internal SequenceFailoverIdentifierSource IdentifierSource =
        new SequenceFailoverIdentifierSource();
    internal RequestFailoverController Controller = null!;
    internal PendingRequestTestHandler Handler = new PendingRequestTestHandler();
    internal DiameterMessage Request = new DiameterMessage();
    internal PendingRequestTableResult Registration =
        new PendingRequestTableResult();
    internal long RequestIdentifier;
}

internal class SequenceFailoverIdentifierSource : IDiameterIdentifierSource
{
    internal uint HopByHopIdentifier;

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier = HopByHopIdentifier + 1U;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        throw new NotSupportedException();
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        throw new NotSupportedException();
    }

    public uint CreateOriginStateIdentifier()
    {
        throw new NotSupportedException();
    }
}

internal class BarrierFailoverIdentifierSource : IDiameterIdentifierSource
{
    internal Barrier Barrier;
    internal int HopByHopIdentifier;

    internal BarrierFailoverIdentifierSource(
        Barrier barrier,
        int hopByHopIdentifier)
    {
        Barrier = barrier;
        HopByHopIdentifier = hopByHopIdentifier;
    }

    public uint CreateHopByHopIdentifier()
    {
        int result = Interlocked.Increment(ref HopByHopIdentifier);
        Barrier.SignalAndWait();
        return (uint)result;
    }

    public uint CreateEndToEndIdentifier()
    {
        throw new NotSupportedException();
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        throw new NotSupportedException();
    }

    public uint CreateOriginStateIdentifier()
    {
        throw new NotSupportedException();
    }
}
