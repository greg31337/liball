using System.Text;

namespace diameter.Tests;

public class DiameterRequestClientTests
{
    [Test]
    public void PreferredPeerRequestIsTrackedSentAndCorrelated()
    {
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("peer.example", 4U);
        RecordingRequestRouter router = new RecordingRequestRouter();
        RequestClientAnswerHandler handler =
            new RequestClientAnswerHandler();
        DiameterMessageCodec codec = new DiameterMessageCodec();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DiameterRequestClient client = CreateRequestClient(
            router,
            peerManager,
            new RequestClientIdentifierSource(),
            clock);
        DiameterSendRequest request = CreateSendRequest(
            "peer.example",
            5000);

        DiameterSendResult sent = client.SendDiameterRequest(
            request,
            handler);
        DiameterDecodeResult decoded = codec.DecodeMessage(
            peerManager.SentMessages[0],
            false,
            new DiameterLimitConfiguration());
        DiameterAnswerResult answer =
            client.HandleReceivedDiameterAnswer(
                CreateIdentity("peer.example"),
                CreateAnswer(sent.PendingRequest!.OutboundMessage));

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(DiameterSendStatus.Accepted));
            Assert.That(sent.Accepted, Is.True);
            Assert.That(sent.Delivered, Is.True);
            Assert.That(sent.PendingRequest, Is.Not.Null);
            Assert.That(sent.PendingRequest!.HopByHopIdentifier,
                Is.EqualTo(101U));
            Assert.That(sent.PendingRequest.EndToEndIdentifier,
                Is.EqualTo(201U));
            Assert.That(sent.PendingRequest.LastSentAt,
                Is.EqualTo(clock.UtcTime));
            Assert.That(request.Request.HopByHopIdentifier, Is.EqualTo(9U));
            Assert.That(request.Request.EndToEndIdentifier, Is.EqualTo(10U));
            Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoded.Message!.HopByHopIdentifier,
                Is.EqualTo(101U));
            Assert.That(decoded.Message.EndToEndIdentifier,
                Is.EqualTo(201U));
            Assert.That(answer.Correlated, Is.True);
            Assert.That(answer.Successful, Is.True);
            Assert.That(answer.ResultCode,
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(handler.Results, Has.Count.EqualTo(1));
            Assert.That(handler.Results[0], Is.Not.SameAs(answer));
            Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
            Assert.That(router.Requests, Is.Empty);
        });
    }

    [Test]
    public void RequestWithoutPreferredPeerUsesOwnedRoutingContext()
    {
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("routed.example", 4U);
        RecordingRequestRouter router = new RecordingRequestRouter();
        router.RoutePeer = "routed.example";
        DiameterRequestClient client = CreateRequestClient(
            router,
            peerManager,
            new RequestClientIdentifierSource(),
            new FakeDiscoveryClock());
        DiameterSendRequest request = CreateSendRequest(null, 5000);

        DiameterSendResult result = client.SendDiameterRequest(
            request,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PeerIdentity!.Value,
                Is.EqualTo("routed.example"));
            Assert.That(router.Requests, Has.Count.EqualTo(1));
            Assert.That(router.Requests[0].ApplicationId, Is.EqualTo(4U));
            Assert.That(router.Requests[0].DestinationRealm!.Value,
                Is.EqualTo("example"));
            Assert.That(router.Requests[0].OriginHost!.Value,
                Is.EqualTo("client.example"));
            Assert.That(router.Requests[0].SessionId,
                Is.EqualTo("client.example;1"));
            Assert.That(router.Requests[0].UserName,
                Is.EqualTo("alice@example"));
            Assert.That(peerManager.SentPeers,
                Is.EqualTo(new[] { "routed.example" }));
        });
    }

    [Test]
    public void CancellationAndExpiryDispatchTerminalApplicationOutcomes()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("peer.example", 4U);
        DiameterRequestClient client = CreateRequestClient(
            new RecordingRequestRouter(),
            peerManager,
            new RequestClientIdentifierSource(),
            clock);
        RequestClientAnswerHandler cancelledHandler =
            new RequestClientAnswerHandler();
        RequestClientAnswerHandler expiredHandler =
            new RequestClientAnswerHandler();
        DiameterSendResult cancelledSend = client.SendDiameterRequest(
            CreateSendRequest("peer.example", 1000),
            cancelledHandler);
        DiameterSendResult expiredSend = client.SendDiameterRequest(
            CreateSendRequest("peer.example", 1000),
            expiredHandler);

        DiameterAnswerResult cancelled = client.CancelDiameterRequest(
            cancelledSend.PendingRequest!.RequestIdentifier);
        clock.AdvanceDiscoveryTime(2);
        List<DiameterAnswerResult> expired =
            client.ExpireDiameterRequests();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Cancelled, Is.True);
            Assert.That(cancelledHandler.Results, Has.Count.EqualTo(1));
            Assert.That(cancelledHandler.Results[0].Cancelled, Is.True);
            Assert.That(expired, Has.Count.EqualTo(1));
            Assert.That(expired[0].RequestIdentifier,
                Is.EqualTo(expiredSend.PendingRequest!.RequestIdentifier));
            Assert.That(expired[0].TimedOut, Is.True);
            Assert.That(expiredHandler.Results, Has.Count.EqualTo(1));
            Assert.That(expiredHandler.Results[0].TimedOut, Is.True);
            Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
        });
    }

    [Test]
    public void PeerFailureRetransmitsThroughRoutingAndCorrelatesAnswer()
    {
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("first.example", 4U);
        peerManager.AddAvailablePeer("second.example", 4U);
        RecordingRequestRouter router = new RecordingRequestRouter();
        router.RoutePeer = "second.example";
        router.AddedRouteAvpCode = 999999U;
        RequestClientAnswerHandler handler =
            new RequestClientAnswerHandler();
        DiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterRequestClient client = CreateRequestClient(
            router,
            peerManager,
            new RequestClientIdentifierSource(),
            new FakeDiscoveryClock());
        DiameterSendResult first = client.SendDiameterRequest(
            CreateSendRequest("first.example", 5000),
            handler);

        peerManager.PublishTransportFailure("first.example");
        DiameterPendingRequestSnapshot pending =
            client.ReadPendingDiameterRequests()[0];
        DiameterDecodeResult retransmission = codec.DecodeMessage(
            peerManager.SentMessages[1],
            false,
            new DiameterLimitConfiguration());
        DiameterAnswerResult answer =
            client.HandleReceivedDiameterAnswer(
                CreateIdentity("second.example"),
                CreateAnswer(pending.OutboundMessage));

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(peerManager.SentPeers,
                Is.EqualTo(new[] { "first.example", "second.example" }));
            Assert.That(pending.AttemptCount, Is.EqualTo(2));
            Assert.That(pending.PeerIdentity.Value,
                Is.EqualTo("second.example"));
            Assert.That(pending.EndToEndIdentifier,
                Is.EqualTo(first.PendingRequest!.EndToEndIdentifier));
            Assert.That(pending.HopByHopIdentifier,
                Is.Not.EqualTo(first.PendingRequest.HopByHopIdentifier));
            Assert.That(retransmission.Message!.Flags.HasFlag(
                DiameterMessageFlags.PotentiallyRetransmitted), Is.True);
            Assert.That(retransmission.Message.Avps.Any(
                avp => avp.Code == 999999U), Is.True);
            Assert.That(router.Requests[0].ExcludedPeers[0].Value,
                Is.EqualTo("first.example"));
            Assert.That(router.Requests[0].DestinationHost, Is.Null);
            Assert.That(answer.Correlated, Is.True);
            Assert.That(handler.Results, Has.Count.EqualTo(1));
            Assert.That(handler.Results[0].Successful, Is.True);
        });
    }

    [Test]
    public void MismatchedAnswerDoesNotConsumeRequestAndCorrectAnswerCompletes()
    {
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("peer.example", 4U);
        RequestClientAnswerHandler handler =
            new RequestClientAnswerHandler();
        DiameterRequestClient client = CreateRequestClient(
            new RecordingRequestRouter(),
            peerManager,
            new RequestClientIdentifierSource(),
            new FakeDiscoveryClock());
        DiameterSendResult sent = client.SendDiameterRequest(
            CreateSendRequest("peer.example", 5000),
            handler);
        DiameterMessage mismatched = CreateAnswer(
            sent.PendingRequest!.OutboundMessage);
        mismatched.EndToEndIdentifier++;

        DiameterAnswerResult rejected =
            client.HandleReceivedDiameterAnswer(
                CreateIdentity("peer.example"),
                mismatched);
        DiameterAnswerResult completed =
            client.HandleReceivedDiameterAnswer(
                CreateIdentity("peer.example"),
                CreateAnswer(sent.PendingRequest.OutboundMessage));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Correlated, Is.False);
            Assert.That(rejected.Code,
                Is.EqualTo("answer-end-to-end-mismatch"));
            Assert.That(completed.Correlated, Is.True);
            Assert.That(completed.Successful, Is.True);
            Assert.That(handler.Results, Has.Count.EqualTo(1));
            Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
        });
    }

    [Test]
    public void CancelledRequestCanReportOwnedLateAnswer()
    {
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("peer.example", 4U);
        RequestClientAnswerHandler handler =
            new RequestClientAnswerHandler();
        DiameterRequestClient client = CreateRequestClient(
            new RecordingRequestRouter(),
            peerManager,
            new RequestClientIdentifierSource(),
            new FakeDiscoveryClock());
        DiameterSendResult sent = client.SendDiameterRequest(
            CreateSendRequest("peer.example", 5000),
            handler);
        DiameterMessage answer = CreateAnswer(
            sent.PendingRequest!.OutboundMessage);

        DiameterAnswerResult cancelled = client.CancelDiameterRequest(
            sent.PendingRequest.RequestIdentifier);
        DiameterAnswerResult late = client.HandleReceivedDiameterAnswer(
            CreateIdentity("peer.example"),
            answer);
        answer.CommandCode = 999U;

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Cancelled, Is.True);
            Assert.That(late.Correlated, Is.True);
            Assert.That(late.Late, Is.True);
            Assert.That(late.Successful, Is.True);
            Assert.That(handler.Results, Has.Count.EqualTo(2));
            Assert.That(handler.Results[0].Cancelled, Is.True);
            Assert.That(handler.Results[1].Late, Is.True);
            Assert.That(handler.Results[1].Answer!.CommandCode,
                Is.EqualTo(300U));
            Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
        });
    }

    [Test]
    public void PeerLossWithoutAlternateRouteIsTerminalAndCleansPendingState()
    {
        RequestClientPeerManager peerManager =
            new RequestClientPeerManager();
        peerManager.AddAvailablePeer("peer.example", 4U);
        RequestClientAnswerHandler handler =
            new RequestClientAnswerHandler();
        DiameterRequestClient client = CreateRequestClient(
            new RecordingRequestRouter(),
            peerManager,
            new RequestClientIdentifierSource(),
            new FakeDiscoveryClock());
        DiameterSendResult sent = client.SendDiameterRequest(
            CreateSendRequest("peer.example", 5000),
            handler);

        peerManager.PublishTransportFailure("peer.example");
        DiameterAnswerResult repeatedCancellation =
            client.CancelDiameterRequest(
                sent.PendingRequest!.RequestIdentifier);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Results, Has.Count.EqualTo(1));
            Assert.That(handler.Results[0].PeerFailed, Is.True);
            Assert.That(handler.Results[0].Code,
                Is.EqualTo("request-peer-failed"));
            Assert.That(repeatedCancellation.Code,
                Is.EqualTo("request-not-pending"));
            Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
        });
    }

    [Test]
    public void ClientRejectsEveryMissingDependencyAndIgnoresIncompletePeerEvents()
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "client.example";
        configuration.OriginRealm.Value = "example";
        configuration.ProductName = "request-client-tests";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Security.AllowUnsecuredTransport = true;
        RecordingRequestRouter router = new RecordingRequestRouter();
        RequestClientPeerManager peers = new RequestClientPeerManager();
        DiameterMessageCodec codec = new DiameterMessageCodec();
        RequestClientIdentifierSource identifiers =
            new RequestClientIdentifierSource();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new DiameterRequestClient(
                null, router, peers, codec, identifiers, clock));
            Assert.Throws<ArgumentException>(() => new DiameterRequestClient(
                configuration, null, peers, codec, identifiers, clock));
            Assert.Throws<ArgumentException>(() => new DiameterRequestClient(
                configuration, router, null, codec, identifiers, clock));
            Assert.Throws<ArgumentException>(() => new DiameterRequestClient(
                configuration, router, peers, null, identifiers, clock));
            Assert.Throws<ArgumentException>(() => new DiameterRequestClient(
                configuration, router, peers, codec, null, clock));
            Assert.Throws<ArgumentException>(() => new DiameterRequestClient(
                configuration, router, peers, codec, identifiers, null));
        });

        DiameterRequestClient client = new DiameterRequestClient(
            configuration, router, peers, codec, identifiers, clock);
        Assert.Throws<ArgumentException>(() =>
            client.SendDiameterRequest(null, null));
        client.HandlePeerEvent(null!);
        DiameterPeerEvent missingPeer = new DiameterPeerEvent();
        missingPeer.Peer = null!;
        client.HandlePeerEvent(missingPeer);
        DiameterPeerEvent missingConfiguration = new DiameterPeerEvent();
        missingConfiguration.Peer.Configuration = null!;
        client.HandlePeerEvent(missingConfiguration);
        DiameterPeerEvent missingIdentity = new DiameterPeerEvent();
        missingIdentity.Peer.Configuration.Identity = null!;
        client.HandlePeerEvent(missingIdentity);
        DiameterPeerEvent ignored = new DiameterPeerEvent();
        ignored.Peer.Configuration.Identity.Value = "peer.example";
        ignored.Kind = DiameterPeerEventKind.StateChanged;
        ignored.CurrentState = DiameterPeerState.InitiatorOpen;
        client.HandlePeerEvent(ignored);
        Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
    }

    private static DiameterRequestClient CreateRequestClient(
        RecordingRequestRouter router,
        RequestClientPeerManager peerManager,
        RequestClientIdentifierSource identifiers,
        FakeDiscoveryClock clock)
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "client.example";
        configuration.OriginRealm.Value = "example";
        configuration.ProductName = "request-client-tests";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Security.AllowUnsecuredTransport = true;
        return new DiameterRequestClient(
            configuration,
            router,
            peerManager,
            new DiameterMessageCodec(),
            identifiers,
            clock);
    }

    private static DiameterSendRequest CreateSendRequest(
        string? preferredPeer,
        int timeoutMilliseconds)
    {
        DiameterSendRequest result = new DiameterSendRequest();
        result.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Request.CommandCode = 300U;
        result.Request.ApplicationId = 4U;
        result.Request.HopByHopIdentifier = 9U;
        result.Request.EndToEndIdentifier = 10U;
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            "example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationHost,
            "server.example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.OriginHost,
            "client.example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            "client.example;1"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            "alice@example"));
        result.TimeoutMilliseconds = timeoutMilliseconds;
        if (preferredPeer != null)
        {
            result.PreferredPeer = CreateIdentity(preferredPeer);
        }

        return result;
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static DiameterMessage CreateAnswer(DiameterMessage request)
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

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class RecordingRequestRouter : IDiameterRouter
{
    internal string RoutePeer = string.Empty;
    internal uint AddedRouteAvpCode;
    internal List<DiameterRouteRequest> Requests =
        new List<DiameterRouteRequest>();

    public DiameterRouteDecision SelectDiameterRoute(
        DiameterRouteRequest? request)
    {
        if (request == null)
        {
            return new DiameterRouteDecision();
        }

        Requests.Add(request);
        DiameterRouteDecision result = new DiameterRouteDecision();
        result.Request = request;
        if (RoutePeer.Length == 0)
        {
            result.Code = "request-test-no-route";
            return result;
        }

        result.Routed = true;
        result.Code = "request-test-routed";
        if (AddedRouteAvpCode != 0U)
        {
            DiameterAvp added = new DiameterAvp();
            added.Code = AddedRouteAvpCode;
            added.Data = new byte[] { 1 };
            request.Message.Avps.Add(added);
        }

        result.RoutedMessage = request.Message;
        result.SelectedCandidate = new DiameterRouteCandidate();
        result.SelectedCandidate.PeerIdentity = new DiameterIdentity();
        result.SelectedCandidate.PeerIdentity.Value = RoutePeer;
        return result;
    }

    public List<DiameterRedirectEntry> ProcessDiameterRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        return new List<DiameterRedirectEntry>();
    }

    public List<DiameterRouteSnapshot> ReadDiameterRouteSnapshots()
    {
        return new List<DiameterRouteSnapshot>();
    }
}

internal class RequestClientPeerManager : IDiameterPeerManager
{
    internal Dictionary<string, DiameterPeerSnapshot> Peers =
        new Dictionary<string, DiameterPeerSnapshot>();
    internal List<IPeerEventHandler> Handlers =
        new List<IPeerEventHandler>();
    internal List<byte[]> SentMessages = new List<byte[]>();
    internal List<string> SentPeers = new List<string>();

    internal void AddAvailablePeer(string identity, uint applicationId)
    {
        DiameterPeerSnapshot peer = new DiameterPeerSnapshot();
        peer.Configuration.Identity.Value = identity;
        peer.Available = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        peer.Capabilities.CommonApplications.Add(application);
        Peers[identity] = peer;
    }

    internal void PublishTransportFailure(string identity)
    {
        DiameterPeerEvent peerEvent = new DiameterPeerEvent();
        peerEvent.Kind = DiameterPeerEventKind.TransportFailed;
        peerEvent.Peer.Configuration.Identity.Value = identity;
        int index = 0;
        while (index < Handlers.Count)
        {
            Handlers[index].HandlePeerEvent(peerEvent);
            index++;
        }
    }

    public DiameterPeerSnapshot AddDiameterPeer(
        DiameterPeerConfiguration? configuration)
    {
        throw new NotSupportedException();
    }

    public bool RemoveDiameterPeer(DiameterIdentity? identity)
    {
        throw new NotSupportedException();
    }

    public DiameterPeerSnapshot ConnectDiameterPeer(DiameterIdentity? identity)
    {
        throw new NotSupportedException();
    }

    public DiameterPeerSnapshot DisconnectDiameterPeer(
        DiameterDisconnectRequest? request)
    {
        throw new NotSupportedException();
    }

    public DiameterPeerSnapshot? ReadDiameterPeerSnapshot(
        DiameterIdentity? identity)
    {
        if (identity == null || !Peers.ContainsKey(identity.Value))
        {
            return null;
        }

        return Peers[identity.Value];
    }

    public List<DiameterPeerSnapshot> ReadDiameterPeerSnapshots()
    {
        return Peers.Values.ToList();
    }

    public DiameterTransportIoResult SendDiameterPeerBytes(
        DiameterIdentity? identity,
        byte[]? source,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        if (identity == null || source == null)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            return result;
        }

        SentPeers.Add(identity.Value);
        SentMessages.Add(source.ToArray());
        result.TransferredCount = source.Length;
        return result;
    }

    public void RegisterPeerEventHandler(IPeerEventHandler? handler)
    {
        if (handler != null && !Handlers.Contains(handler))
        {
            Handlers.Add(handler);
        }
    }

    public void UnregisterPeerEventHandler(IPeerEventHandler? handler)
    {
        if (handler != null)
        {
            Handlers.Remove(handler);
        }
    }
}

internal class RequestClientIdentifierSource : IDiameterIdentifierSource
{
    internal uint HopByHopIdentifier = 100U;
    internal uint EndToEndIdentifier = 200U;

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier++;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        EndToEndIdentifier++;
        return EndToEndIdentifier;
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        return "request-client-session";
    }

    public uint CreateOriginStateIdentifier()
    {
        return 1U;
    }
}

internal class RequestClientAnswerHandler : IDiameterAnswerHandler
{
    internal List<DiameterAnswerResult> Results =
        new List<DiameterAnswerResult>();

    public void HandleDiameterAnswer(DiameterAnswerResult result)
    {
        Results.Add(result);
    }
}
