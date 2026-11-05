namespace diameter.Tests;

public class RouteSelectorTests
{
    [Test]
    public void ExplicitLocalHostUsesLocalApplicationBeforeStaticRoutes()
    {
        RouteTable table = CreateRouteTable(0);
        DiameterRouteConfiguration hostRoute = CreatePeerRoute(
            "configured-host",
            "local.example",
            "remote-peer.example",
            4U);
        hostRoute.DestinationHost = CreateIdentity("node.local.example");
        table.AddOrReplaceStaticRoute(hostRoute);
        table.AddOrReplaceStaticRoute(CreateDefaultPeerRoute(
            "default",
            "default-peer.example"));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U),
            table);
        DiameterRouteRequest request = CreateRequest(
            "local.example",
            "NODE.LOCAL.EXAMPLE",
            4U);
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 1U;
        avp.Data = new byte[] { 1, 2, 3 };
        request.Message.Avps.Add(avp);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("remote-peer.example", true, 4U));
        peers.Add(CreatePeer("default-peer.example", true, 4U));

        DiameterRouteDecision decision = selector.SelectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(decision.Code, Is.EqualTo("route-local"));
            Assert.That(decision.ResultCode, Is.Zero);
            Assert.That(decision.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("local-application-4"));
            Assert.That(decision.SelectedCandidate.ApplicationSupported, Is.True);
            Assert.That(decision.Candidates, Has.Count.EqualTo(1));
            Assert.That(decision.RoutedMessage, Is.Not.Null);
            Assert.That(decision.RoutedMessage, Is.Not.SameAs(request.Message));
            Assert.That(decision.RoutedMessage!.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        request.DestinationRealm!.Value = "changed.example";
        request.Message.Avps[0].Data[0] = 9;
        Assert.That(decision.Request.DestinationRealm!.Value,
            Is.EqualTo("local.example"));
        Assert.That(decision.Request.Message.Avps[0].Data[0], Is.EqualTo(1));
        Assert.That(decision.RoutedMessage!.Avps[0].Data[0], Is.EqualTo(1));
    }

    [Test]
    public void ExplicitLocalHostDoesNotLeakUnsupportedApplicationToDefaultRoute()
    {
        RouteTable table = CreateRouteTable(0);
        table.AddOrReplaceStaticRoute(CreateDefaultPeerRoute(
            "default",
            "default-peer.example"));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U),
            table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("default-peer.example", true, 5U));

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateRequest(
                "local.example",
                "node.local.example",
                5U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.NoRoute));
            Assert.That(decision.SelectedCandidate, Is.Null);
            Assert.That(decision.Candidates, Is.Empty);
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.ApplicationUnsupported));
            Assert.That(decision.Code,
                Is.EqualTo("route-local-application-unsupported"));
        });
    }

    [Test]
    public void LocalRealmWithoutDestinationHostUsesLocalApplicationFirst()
    {
        RouteTable table = CreateRouteTable(0);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "realm",
            "local.example",
            "realm-peer.example",
            4U));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U),
            table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("realm-peer.example", true, 4U));

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateRequest("LOCAL.EXAMPLE", string.Empty, 4U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(decision.SelectedCandidate!.Source,
                Is.EqualTo(DiameterRouteSource.Static));
            Assert.That(table.ReadStaticRouteSnapshot("realm")!.SelectionCount,
                Is.Zero);
        });
    }

    [Test]
    public void ExplicitRemoteHostPrecedesLocalRealmAndRealmRoute()
    {
        RouteTable table = CreateRouteTable(0);
        DiameterRouteConfiguration host = CreatePeerRoute(
            "host",
            "local.example",
            "host-peer.example",
            4U);
        host.DestinationHost = CreateIdentity("service.local.example");
        host.Priority = 10;
        table.AddOrReplaceStaticRoute(host);
        DiameterRouteConfiguration realm = CreatePeerRoute(
            "realm",
            "local.example",
            "realm-peer.example",
            4U);
        realm.Priority = 0;
        table.AddOrReplaceStaticRoute(realm);
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U),
            table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("host-peer.example", true, 4U));
        peers.Add(CreatePeer("realm-peer.example", true, 4U));

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateRequest(
                "local.example",
                "service.local.example",
                4U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Relay));
            Assert.That(decision.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("host"));
            Assert.That(decision.Candidates, Has.Count.EqualTo(2));
            Assert.That(decision.RoutedMessage, Is.Not.Null);
            Assert.That(table.ReadStaticRouteSnapshot("host")!.SelectionCount,
                Is.EqualTo(1));
            Assert.That(table.ReadStaticRouteSnapshot("realm")!.SelectionCount,
                Is.Zero);
        });
    }

    [Test]
    public void UnsupportedLocalApplicationUsesRealmRouteAndDistinguishesFailure()
    {
        RouteTable table = CreateRouteTable(0, 0);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "application-five",
            "local.example",
            "application-peer.example",
            5U));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U),
            table);
        List<DiameterPeerSnapshot> availablePeers =
            new List<DiameterPeerSnapshot>();
        availablePeers.Add(CreatePeer("application-peer.example", true, 5U));
        DiameterRouteRequest request = CreateRequest(
            "local.example",
            string.Empty,
            5U);

        DiameterRouteDecision routed = selector.SelectRoute(
            request,
            availablePeers);
        List<DiameterPeerSnapshot> unavailablePeers =
            new List<DiameterPeerSnapshot>();
        unavailablePeers.Add(CreatePeer("application-peer.example", false, 5U));
        DiameterRouteDecision unsupported = selector.SelectRoute(
            request,
            unavailablePeers);
        table.RemoveStaticRoute("application-five");
        DiameterRouteDecision unregistered = selector.SelectRoute(
            request,
            unavailablePeers);

        Assert.Multiple(() =>
        {
            Assert.That(routed.Routed, Is.True);
            Assert.That(routed.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("application-five"));
            Assert.That(unsupported.Routed, Is.False);
            Assert.That(unsupported.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToDeliver));
            Assert.That(unsupported.Candidates, Has.Count.EqualTo(1));
            Assert.That(unsupported.Candidates[0].Code,
                Is.EqualTo("route-peer-unhealthy"));
            Assert.That(unregistered.ResultCode,
                Is.EqualTo(DiameterResultCodes.ApplicationUnsupported));
            Assert.That(unregistered.Candidates, Is.Empty);
        });
    }

    [Test]
    public void NonLocalRealmUsesRealmThenDefaultAndReportsUnableToDeliver()
    {
        RouteTable table = CreateRouteTable(0, 0);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "realm",
            "remote.example",
            "realm-peer.example",
            4U));
        table.AddOrReplaceStaticRoute(CreateDefaultPeerRoute(
            "default",
            "default-peer.example"));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U),
            table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("realm-peer.example", true, 4U));
        peers.Add(CreatePeer("default-peer.example", true, 4U));

        DiameterRouteDecision realm = selector.SelectRoute(
            CreateRequest("remote.example", string.Empty, 4U),
            peers);
        DiameterRouteDecision fallback = selector.SelectRoute(
            CreateRequest("elsewhere.example", string.Empty, 4U),
            peers);
        peers[1].Available = false;
        DiameterRouteDecision missing = selector.SelectRoute(
            CreateRequest("missing.example", string.Empty, 4U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(realm.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("realm"));
            Assert.That(fallback.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("default"));
            Assert.That(fallback.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.Default));
            Assert.That(missing.Routed, Is.False);
            Assert.That(missing.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToDeliver));
            Assert.That(missing.Code, Is.EqualTo("route-unable-to-deliver"));
        });
    }

    [Test]
    public void NodeWithoutServerRoleDoesNotClaimLocalDelivery()
    {
        RouteTable table = CreateRouteTable(0);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "realm",
            "local.example",
            "realm-peer.example",
            4U));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(
                DiameterNodeRole.Client | DiameterNodeRole.Relay,
                4U),
            table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("realm-peer.example", true, 4U));

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateRequest("local.example", string.Empty, 4U),
            peers);

        Assert.That(decision.SelectedCandidate!.Configuration.RouteIdentifier,
            Is.EqualTo("realm"));
    }

    [Test]
    public void RoutingNormalizesDrmpAndRejectsInvalidPriorityValues()
    {
        RouteTable table = CreateRouteTable(0);
        DiameterNodeConfiguration configuration =
            CreateNodeConfiguration(DiameterNodeRole.Server, 4U);
        configuration.DefaultRoutingPriority =
            DiameterRoutingPriority.Priority7;
        RouteSelector selector = new RouteSelector(configuration, table);
        DiameterRouteRequest implicitRequest = CreateRequest(
            "local.example",
            "node.local.example",
            4U);
        DiameterRouteRequest explicitRequest = CreateRequest(
            "local.example",
            "node.local.example",
            4U);
        DiameterAvp explicitDrmp = new DiameterAvp();
        explicitDrmp.Code = DiameterAvpCodes.Drmp;
        explicitDrmp.Data = new byte[4];
        NetworkByteOrder.WriteInt32(
            explicitDrmp.Data,
            (int)DiameterRoutingPriority.Priority2);
        explicitRequest.Message.Avps.Add(explicitDrmp);
        DiameterRouteRequest invalidRequest = CreateRequest(
            "local.example",
            "node.local.example",
            4U);
        DiameterAvp invalidDrmp = new DiameterAvp();
        invalidDrmp.Code = DiameterAvpCodes.Drmp;
        invalidDrmp.Data = new byte[4];
        NetworkByteOrder.WriteInt32(invalidDrmp.Data, 16);
        invalidRequest.Message.Avps.Add(invalidDrmp);

        DiameterRouteDecision implicitDecision = selector.SelectRoute(
            implicitRequest,
            new List<DiameterPeerSnapshot>());
        DiameterRouteDecision explicitDecision = selector.SelectRoute(
            explicitRequest,
            new List<DiameterPeerSnapshot>());
        DiameterRouteDecision invalidDecision = selector.SelectRoute(
            invalidRequest,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(implicitDecision.Routed, Is.True);
            Assert.That(implicitDecision.Request.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority7));
            Assert.That(implicitDecision.Request.RoutingPriorityPresent,
                Is.False);
            Assert.That(implicitDecision.Request.Message.Avps, Is.Empty);
            Assert.That(explicitDecision.Request.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority2));
            Assert.That(explicitDecision.Request.RoutingPriorityPresent,
                Is.True);
            Assert.That(explicitDecision.RoutedMessage!.Avps,
                Has.Count.EqualTo(1));
            Assert.That(invalidDecision.Routed, Is.False);
            Assert.That(invalidDecision.Rejected, Is.True);
            Assert.That(invalidDecision.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(invalidDecision.Code,
                Is.EqualTo("drmp-value-invalid"));
        });
    }

    private static RouteTable CreateRouteTable(params long[] values)
    {
        return new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(values));
    }

    private static DiameterNodeConfiguration CreateNodeConfiguration(
        DiameterNodeRole roles,
        params uint[] applicationIds)
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "diameter.Tests";
        result.Roles = roles;
        result.Transport.EnableListener =
            (roles & DiameterNodeRole.Server) != DiameterNodeRole.None;
        result.Security.AllowUnsecuredTransport = true;
        int index = 0;
        while (index < applicationIds.Length)
        {
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = applicationIds[index];
            result.Applications.Add(application);
            index = index + 1;
        }

        return result;
    }

    private static DiameterRouteConfiguration CreatePeerRoute(
        string identifier,
        string realm,
        string peer,
        uint applicationId)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = applicationId;
        result.PeerIdentity = CreateIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterRouteConfiguration CreateDefaultPeerRoute(
        string identifier,
        string peer)
    {
        DiameterRouteConfiguration result = CreatePeerRoute(
            identifier,
            "temporary.example",
            peer,
            0U);
        result.DestinationRealm.Value = string.Empty;
        result.AnyApplication = true;
        result.DefaultRoute = true;
        return result;
    }

    private static DiameterRouteRequest CreateRequest(
        string realm,
        string host,
        uint applicationId)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = applicationId;
        result.ApplicationId = applicationId;
        result.DestinationRealm = CreateIdentity(realm);
        if (host.Length > 0)
        {
            result.DestinationHost = CreateIdentity(host);
        }

        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(
        string identity,
        bool available,
        params uint[] applicationIds)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = available;
        int index = 0;
        while (index < applicationIds.Length)
        {
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = applicationIds[index];
            result.Capabilities.CommonApplications.Add(application);
            index = index + 1;
        }

        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}
