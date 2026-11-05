namespace diameter.Tests;

public class DiameterRouterTests
{
    [Test]
    public void RouterOwnsConfiguredRoutesAndAppliesPoliciesToLivePeers()
    {
        DiameterNodeConfiguration configuration =
            CreateRouterConfiguration();
        ScoringRoutePolicy policy = new ScoringRoutePolicy();
        policy.PreferredRoute = "preferred";
        policy.PreferredAdjustment = 20;
        policy.OtherAdjustment = 1;
        policy.MutateInputs = true;
        configuration.RoutePolicies.Add(policy);
        DiameterRouteConfiguration first = CreateRouterRoute(
            "first",
            "first.example");
        DiameterRouteConfiguration preferred = CreateRouterRoute(
            "preferred",
            "preferred.example");
        configuration.Routes.Add(first);
        configuration.Routes.Add(preferred);
        RouterPeerManagerProbe peers = new RouterPeerManagerProbe();
        peers.Peers.Add(CreateRouterPeer("first.example"));
        peers.Peers.Add(CreateRouterPeer("preferred.example"));
        IDiameterRouter router = new DiameterRouter(
            configuration,
            peers,
            new FakeDiscoveryClock());
        first.RouteIdentifier = "mutated-input";
        DiameterRouteRequest request = CreateRouterRequest();

        DiameterRouteDecision decision = router.SelectDiameterRoute(request);
        List<DiameterRouteSnapshot> snapshots =
            router.ReadDiameterRouteSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("preferred"));
            Assert.That(decision.SelectedCandidate.PolicyScore, Is.EqualTo(20));
            Assert.That(decision.Request.SessionId, Is.EqualTo("session-one"));
            Assert.That(request.SessionId, Is.EqualTo("session-one"));
            Assert.That(peers.ReadCount, Is.EqualTo(1));
            Assert.That(snapshots, Has.Count.EqualTo(2));
            Assert.That(snapshots[0].Configuration.RouteIdentifier,
                Is.EqualTo("first"));
            Assert.That(snapshots[1].SelectionCount, Is.EqualTo(1));
        });

        snapshots[0].Configuration.RouteIdentifier = "mutated-snapshot";
        Assert.That(router.ReadDiameterRouteSnapshots()[0]
            .Configuration.RouteIdentifier, Is.EqualTo("first"));
    }

    [Test]
    public void RouterRejectsMissingDependenciesAndNullRequests()
    {
        DiameterNodeConfiguration configuration =
            CreateRouterConfiguration();
        RouterPeerManagerProbe peers = new RouterPeerManagerProbe();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterRouter(null, peers, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterRouter(configuration, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterRouter(configuration, peers, null));
        });

        DiameterRouter router = new DiameterRouter(
            configuration,
            peers,
            clock);
        Assert.Throws<ArgumentException>(() =>
            router.SelectDiameterRoute(null));
        Assert.That(router.ProcessDiameterRedirectAnswer(
            null,
            null,
            null,
            clock.UtcTime), Is.Empty);
    }

    [Test]
    public void RedirectHelpersClassifyRealmAvpsAndSourceSecurity()
    {
        RouterPeerManagerProbe peers = new RouterPeerManagerProbe();
        DiameterRouter router = new DiameterRouter(
            CreateRouterConfiguration(),
            peers,
            new FakeDiscoveryClock());
        DiameterIdentity source = new DiameterIdentity();
        source.Value = "source.example";

        DiameterPeerSnapshot invalidPeer = CreateRouterPeer("bad peer");
        DiameterPeerSnapshot connectionSecure =
            CreateRouterPeer("source.example");
        connectionSecure.Connection = new DiameterConnectionMetadata();
        connectionSecure.Connection.Security.Mode =
            DiameterSecurityMode.Tls;
        DiameterPeerSnapshot connectionUnsecured =
            CreateRouterPeer("source.example");
        connectionUnsecured.Connection = new DiameterConnectionMetadata();
        DiameterPeerSnapshot endpointSecure =
            CreateRouterPeer("source.example");
        endpointSecure.CurrentEndpoint = new DiameterEndpoint();
        endpointSecure.CurrentEndpoint.Security =
            DiameterSecurityMode.Ipsec;
        DiameterPeerSnapshot endpointUnsecured =
            CreateRouterPeer("source.example");
        endpointUnsecured.CurrentEndpoint = new DiameterEndpoint();

        DiameterMessage missingAvps = new DiameterMessage();
        missingAvps.Avps = null!;
        DiameterMessage variants = new DiameterMessage();
        variants.Avps.Add(null!);
        DiameterAvp missingData = new DiameterAvp();
        missingData.Code = DiameterAvpCodes.RedirectRealm;
        missingData.Data = null!;
        variants.Avps.Add(missingData);
        DiameterAvp wrongVendor = CreateRealmAvp("alternate.example");
        wrongVendor.VendorId = 9U;
        variants.Avps.Add(wrongVendor);
        DiameterAvp wrongCode = CreateRealmAvp("alternate.example");
        wrongCode.Code = DiameterAvpCodes.DestinationRealm;
        variants.Avps.Add(wrongCode);
        DiameterAvp valid = CreateRealmAvp("alternate.example");
        variants.Avps.Add(valid);

        Assert.Multiple(() =>
        {
            Assert.That(router.ReadRedirectSourceSecurity(
                CreateRouterIdentity("bad peer"),
                new List<DiameterPeerSnapshot>()), Is.False);
            Assert.That(router.ReadRedirectSourceSecurity(
                source,
                new List<DiameterPeerSnapshot>()), Is.False);
            Assert.That(router.ReadRedirectSourceSecurity(
                source,
                new List<DiameterPeerSnapshot> { invalidPeer }), Is.False);
            Assert.That(router.ReadRedirectSourceSecurity(
                source,
                new List<DiameterPeerSnapshot> { connectionSecure }), Is.True);
            Assert.That(router.ReadRedirectSourceSecurity(
                source,
                new List<DiameterPeerSnapshot> { connectionUnsecured }), Is.False);
            Assert.That(router.ReadRedirectSourceSecurity(
                source,
                new List<DiameterPeerSnapshot> { endpointSecure }), Is.True);
            Assert.That(router.ReadRedirectSourceSecurity(
                source,
                new List<DiameterPeerSnapshot> { endpointUnsecured }), Is.False);
            Assert.That(
                router.ContainsRealmRedirectInformation(missingAvps),
                Is.False);
            Assert.That(
                router.ContainsRealmRedirectInformation(
                    new DiameterMessage()),
                Is.False);
            Assert.That(
                router.ContainsRealmRedirectInformation(variants),
                Is.True);
        });
    }

    [Test]
    public void PublicRedirectProcessingReturnsOwnedHostRedirectEntries()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RouterPeerManagerProbe peers = new RouterPeerManagerProbe();
        DiameterRouter router = new DiameterRouter(
            CreateRouterConfiguration(),
            peers,
            clock);
        DiameterRouteRequest request = CreateRouterRequest();
        DiameterMessage answer = new DiameterMessage();
        answer.Flags = DiameterMessageFlags.Error;
        answer.CommandCode = request.Message.CommandCode;
        answer.ApplicationId = request.Message.ApplicationId;
        answer.HopByHopIdentifier = request.Message.HopByHopIdentifier;
        answer.EndToEndIdentifier = request.Message.EndToEndIdentifier;
        DiameterUri uri = new DiameterUri();
        uri.Host.Value = "redirect.example";
        DiameterBaseAvpFactory avps = new DiameterBaseAvpFactory();
        DiameterAvp resultCode = new DiameterAvp();
        resultCode.Code = DiameterAvpCodes.ResultCode;
        resultCode.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(
            resultCode.Data,
            DiameterResultCodes.RedirectIndication);
        answer.Avps.Add(resultCode);
        answer.Avps.Add(avps.CreateRedirectHostAvp(uri));
        answer.Avps.Add(avps.CreateRedirectHostUsageAvp(
            DiameterRedirectHostUsage.DoNotCache));

        List<DiameterRedirectEntry> entries =
            router.ProcessDiameterRedirectAnswer(
                request,
                answer,
                CreateRouterIdentity("source.example"),
                clock.UtcTime);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Hosts[0].Host.Value,
            Is.EqualTo("redirect.example"));
    }

    private static DiameterNodeConfiguration CreateRouterConfiguration()
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example.net";
        result.ProductName = "diameter.Tests";
        result.Roles = DiameterNodeRole.Relay;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
        return result;
    }

    private static DiameterRouteConfiguration CreateRouterRoute(
        string identifier,
        string peer)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = "remote.example";
        result.ApplicationId = 4U;
        result.PeerIdentity = new DiameterIdentity();
        result.PeerIdentity.Value = peer;
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterPeerSnapshot CreateRouterPeer(string identity)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity.Value = identity;
        result.Available = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Capabilities.CommonApplications.Add(application);
        return result;
    }

    private static DiameterIdentity CreateRouterIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterAvp CreateRealmAvp(string realm)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.RedirectRealm;
        result.Data = System.Text.Encoding.UTF8.GetBytes(realm);
        return result;
    }

    private static DiameterRouteRequest CreateRouterRequest()
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = 4U;
        result.ApplicationId = 4U;
        result.DestinationRealm = new DiameterIdentity();
        result.DestinationRealm.Value = "remote.example";
        result.SessionId = "session-one";
        return result;
    }
}

internal class RouterPeerManagerProbe : IDiameterPeerManager
{
    internal List<DiameterPeerSnapshot> Peers =
        new List<DiameterPeerSnapshot>();
    internal int ReadCount;

    public DiameterPeerSnapshot AddDiameterPeer(
        DiameterPeerConfiguration? configuration)
    {
        throw new NotSupportedException();
    }

    public bool RemoveDiameterPeer(DiameterIdentity? identity)
    {
        throw new NotSupportedException();
    }

    public DiameterPeerSnapshot ConnectDiameterPeer(
        DiameterIdentity? identity)
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
        throw new NotSupportedException();
    }

    public List<DiameterPeerSnapshot> ReadDiameterPeerSnapshots()
    {
        ReadCount = ReadCount + 1;
        return Peers;
    }

    public DiameterTransportIoResult SendDiameterPeerBytes(
        DiameterIdentity? identity,
        byte[]? source,
        int timeoutMilliseconds)
    {
        throw new NotSupportedException();
    }

    public void RegisterPeerEventHandler(IPeerEventHandler? handler)
    {
        throw new NotSupportedException();
    }

    public void UnregisterPeerEventHandler(IPeerEventHandler? handler)
    {
        throw new NotSupportedException();
    }
}
