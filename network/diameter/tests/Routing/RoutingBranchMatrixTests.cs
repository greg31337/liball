namespace diameter.Tests;

public class RoutingBranchMatrixTests
{
    [TestCase(
        DiameterRouteAction.Local,
        DiameterAgentRole.None,
        true,
        false,
        "route-local")]
    [TestCase(
        DiameterRouteAction.Relay,
        DiameterAgentRole.Relay,
        true,
        false,
        "route-selected")]
    [TestCase(
        DiameterRouteAction.Proxy,
        DiameterAgentRole.Proxy,
        true,
        false,
        "route-selected")]
    [TestCase(
        DiameterRouteAction.Redirect,
        DiameterAgentRole.Redirect,
        true,
        false,
        "route-selected")]
    [TestCase(
        DiameterRouteAction.Translate,
        DiameterAgentRole.Translation,
        true,
        false,
        "matrix-request-translated")]
    [TestCase(
        DiameterRouteAction.Reject,
        DiameterAgentRole.None,
        false,
        true,
        "route-rejected")]
    public void EveryConfiguredActionProducesAnExplicitOwnedDecision(
        DiameterRouteAction action,
        DiameterAgentRole role,
        bool routed,
        bool rejected,
        string code)
    {
        MatrixTranslationHandler handler = new MatrixTranslationHandler();
        DiameterNodeConfiguration configuration =
            CreateMatrixConfiguration(
                DiameterNodeRole.Server |
                DiameterNodeRole.Relay |
                DiameterNodeRole.Proxy |
                DiameterNodeRole.Redirect |
                DiameterNodeRole.Translation,
                handler);
        RouteTable table = CreateMatrixRouteTable();
        table.AddOrReplaceStaticRoute(CreateMatrixActionRoute(
            action,
            role));
        RouteSelector selector = new RouteSelector(configuration, table);
        DiameterRouteRequest request = CreateMatrixRequest(
            "remote.example",
            string.Empty,
            4U);
        request.SourcePeer = CreateMatrixIdentity("ingress.example");
        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        if (action == DiameterRouteAction.Relay ||
            action == DiameterRouteAction.Proxy)
        {
            peers.Add(CreateMatrixPeer("peer.example", true, 4U));
        }

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            peers);
        int routeRecords = decision.RoutedMessage == null ? 0 :
            CountMatrixAvps(
                decision.RoutedMessage,
                DiameterAvpCodes.RouteRecord);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(action));
            Assert.That(decision.AgentRole, Is.EqualTo(role));
            Assert.That(decision.Routed, Is.EqualTo(routed));
            Assert.That(decision.Rejected, Is.EqualTo(rejected));
            Assert.That(decision.Code, Is.EqualTo(code));
            Assert.That(decision.SelectedCandidate, Is.Not.Null);
            Assert.That(decision.SelectedCandidate!.Action,
                Is.EqualTo(action));
            Assert.That(decision.Request, Is.Not.SameAs(request));
            Assert.That(routeRecords, Is.EqualTo(
                action == DiameterRouteAction.Relay ||
                action == DiameterRouteAction.Proxy ? 1 : 0));
        });

        if (action == DiameterRouteAction.Translate)
        {
            Assert.Multiple(() =>
            {
                Assert.That(handler.RequestCount, Is.EqualTo(1));
                Assert.That(decision.RoutedMessage!.ApplicationId,
                    Is.EqualTo(9U));
                Assert.That(decision.TranslationResult!.Disposition,
                    Is.EqualTo(
                        DiameterTranslationDisposition.Translated));
            });
        }
    }

    [TestCase("host", "host")]
    [TestCase("realm", "realm")]
    [TestCase("default", "default")]
    public void HostRealmAndDefaultPrecedenceIsDeterministic(
        string requestKind,
        string expectedRoute)
    {
        DiameterNodeConfiguration configuration =
            CreateMatrixConfiguration(
                DiameterNodeRole.Relay,
                new MatrixTranslationHandler());
        RouteTable table = CreateMatrixRouteTable();
        DiameterRouteConfiguration host = CreateMatrixPeerRoute(
            "host",
            "remote.example",
            "host-peer.example");
        host.DestinationHost = CreateMatrixIdentity(
            "service.remote.example");
        host.Priority = 100;
        table.AddOrReplaceStaticRoute(host);
        DiameterRouteConfiguration realm = CreateMatrixPeerRoute(
            "realm",
            "remote.example",
            "realm-peer.example");
        realm.Priority = 0;
        table.AddOrReplaceStaticRoute(realm);
        DiameterRouteConfiguration fallback = CreateMatrixPeerRoute(
            "default",
            string.Empty,
            "default-peer.example");
        fallback.DefaultRoute = true;
        fallback.AnyApplication = true;
        table.AddOrReplaceStaticRoute(fallback);
        RouteSelector selector = new RouteSelector(configuration, table);
        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        peers.Add(CreateMatrixPeer("host-peer.example", true, 4U));
        peers.Add(CreateMatrixPeer("realm-peer.example", true, 4U));
        peers.Add(CreateMatrixPeer("default-peer.example", true, 4U));
        string realmValue = requestKind == "default" ?
            "elsewhere.example" : "remote.example";
        string hostValue = requestKind == "host" ?
            "service.remote.example" : string.Empty;

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateMatrixRequest(realmValue, hostValue, 4U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.SelectedCandidate!.Configuration
                .RouteIdentifier, Is.EqualTo(expectedRoute));
            Assert.That(decision.SelectedCandidate.Source,
                Is.EqualTo(requestKind == "default" ?
                    DiameterRouteSource.Default :
                    DiameterRouteSource.Static));
        });
    }

    [TestCase(true, 4U, false, true, "first")]
    [TestCase(true, uint.MaxValue, false, true, "first")]
    [TestCase(false, 4U, false, true, "second")]
    [TestCase(true, 5U, false, true, "second")]
    [TestCase(true, 4U, true, true, "second")]
    [TestCase(false, 4U, false, false, "")]
    public void CapabilityHealthAndExclusionDriveFailover(
        bool firstAvailable,
        uint firstApplication,
        bool excludeFirst,
        bool secondAvailable,
        string expectedRoute)
    {
        DiameterNodeConfiguration configuration =
            CreateMatrixConfiguration(
                DiameterNodeRole.Relay,
                new MatrixTranslationHandler());
        RouteTable table = CreateMatrixRouteTable();
        table.AddOrReplaceStaticRoute(CreateMatrixPeerRoute(
            "first",
            "remote.example",
            "first-peer.example"));
        table.AddOrReplaceStaticRoute(CreateMatrixPeerRoute(
            "second",
            "remote.example",
            "second-peer.example"));
        RouteSelector selector = new RouteSelector(configuration, table);
        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        peers.Add(CreateMatrixPeer(
            "first-peer.example",
            firstAvailable,
            firstApplication));
        peers.Add(CreateMatrixPeer(
            "second-peer.example",
            secondAvailable,
            4U));
        DiameterRouteRequest request = CreateMatrixRequest(
            "remote.example",
            string.Empty,
            4U);
        if (excludeFirst)
        {
            request.ExcludedPeers.Add(CreateMatrixIdentity(
                "first-peer.example"));
        }

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed,
                Is.EqualTo(expectedRoute.Length > 0));
            Assert.That(decision.SelectedCandidate == null ? string.Empty :
                decision.SelectedCandidate.Configuration.RouteIdentifier,
                Is.EqualTo(expectedRoute));
            Assert.That(decision.ResultCode,
                Is.EqualTo(expectedRoute.Length == 0 ?
                    DiameterResultCodes.UnableToDeliver : 0U));
            Assert.That(decision.Code,
                Is.EqualTo(expectedRoute.Length == 0 ?
                    "route-unable-to-deliver" : "route-selected"));
        });
    }

    [TestCase(DiameterNodeRole.Client, 4U)]
    [TestCase(DiameterNodeRole.Server, 5U)]
    public void StaticLocalRoutesRequireServerRoleAndRegisteredApplication(
        DiameterNodeRole roles,
        uint requestApplication)
    {
        DiameterNodeConfiguration configuration =
            CreateMatrixConfiguration(
                roles,
                new MatrixTranslationHandler());
        RouteTable table = CreateMatrixRouteTable();
        DiameterRouteConfiguration route = new DiameterRouteConfiguration();
        route.RouteIdentifier = "local";
        route.DestinationRealm.Value = "remote.example";
        route.ApplicationId = requestApplication;
        route.Action = DiameterRouteAction.Local;
        route.AgentRole = DiameterAgentRole.None;
        table.AddOrReplaceStaticRoute(route);
        RouteSelector selector = new RouteSelector(configuration, table);

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateMatrixRequest(
                "remote.example",
                string.Empty,
                requestApplication),
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Action,
                Is.EqualTo(DiameterRouteAction.NoRoute));
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.ApplicationUnsupported));
            Assert.That(decision.Code,
                Is.EqualTo("route-local-application-unsupported"));
            Assert.That(decision.Candidates, Has.Count.EqualTo(1));
        });
    }

    [TestCase("source", "route-unable-to-deliver", false)]
    [TestCase("excluded", "route-unable-to-deliver", false)]
    [TestCase("route-record", "route-loop-detected", true)]
    [TestCase("missing", "route-unable-to-deliver", false)]
    public void LoopAndUnableToDeliverOutcomesAreExplicit(
        string condition,
        string expectedCode,
        bool rejected)
    {
        DiameterNodeConfiguration configuration =
            CreateMatrixConfiguration(
                DiameterNodeRole.Relay,
                new MatrixTranslationHandler());
        RouteTable table = CreateMatrixRouteTable();
        if (condition != "missing")
        {
            table.AddOrReplaceStaticRoute(CreateMatrixPeerRoute(
                "only",
                "remote.example",
                "peer.example"));
        }

        RouteSelector selector = new RouteSelector(configuration, table);
        DiameterRouteRequest request = CreateMatrixRequest(
            "remote.example",
            string.Empty,
            4U);
        if (condition == "source")
        {
            request.SourcePeer = CreateMatrixIdentity("peer.example");
        }
        else if (condition == "excluded")
        {
            request.ExcludedPeers.Add(CreateMatrixIdentity("peer.example"));
        }
        else if (condition == "route-record")
        {
            request.Message.Avps.Add(
                new DiameterBaseAvpFactory().CreateRouteRecordAvp(
                    configuration.OriginHost));
        }

        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        peers.Add(CreateMatrixPeer("peer.example", true, 4U));
        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Rejected, Is.EqualTo(rejected));
            Assert.That(decision.Code, Is.EqualTo(expectedCode));
            Assert.That(decision.ResultCode,
                Is.EqualTo(rejected ?
                    DiameterResultCodes.LoopDetected :
                    DiameterResultCodes.UnableToDeliver));
            Assert.That(decision.RoutedMessage == null,
                Is.EqualTo(!rejected));
        });
    }

    private static DiameterNodeConfiguration CreateMatrixConfiguration(
        DiameterNodeRole roles,
        MatrixTranslationHandler handler)
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "routing-matrix-tests";
        result.Roles = roles;
        result.Transport.EnableListener =
            (roles & DiameterNodeRole.Server) != DiameterNodeRole.None;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId source = new DiameterApplicationId();
        source.Id = 4U;
        result.Applications.Add(source);
        DiameterApplicationId target = new DiameterApplicationId();
        target.Id = 9U;
        result.Applications.Add(target);
        DiameterTranslationHandlerConfiguration registration =
            new DiameterTranslationHandlerConfiguration();
        registration.SourceApplicationId = 4U;
        registration.TargetApplicationId = 9U;
        registration.Handler = handler;
        result.TranslationHandlers.Add(registration);
        return result;
    }

    private static RouteTable CreateMatrixRouteTable()
    {
        return new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(new long[] { 0L }));
    }

    private static DiameterRouteConfiguration CreateMatrixActionRoute(
        DiameterRouteAction action,
        DiameterAgentRole role)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = "matrix-action";
        result.DestinationRealm.Value = "remote.example";
        result.ApplicationId = 4U;
        result.Action = action;
        result.AgentRole = role;
        if (action == DiameterRouteAction.Relay ||
            action == DiameterRouteAction.Proxy)
        {
            result.PeerIdentity = CreateMatrixIdentity("peer.example");
        }
        else if (action == DiameterRouteAction.Redirect)
        {
            DiameterUri target = new DiameterUri();
            target.Host = CreateMatrixIdentity("redirect.example");
            result.RedirectHosts.Add(target);
        }
        else if (action == DiameterRouteAction.Translate)
        {
            result.TargetApplicationId = 9U;
        }

        return result;
    }

    private static DiameterRouteConfiguration CreateMatrixPeerRoute(
        string identifier,
        string realm,
        string peer)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateMatrixIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterRouteRequest CreateMatrixRequest(
        string realm,
        string host,
        uint applicationId)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = applicationId;
        result.Message.HopByHopIdentifier = 100U;
        result.Message.EndToEndIdentifier = 200U;
        result.ApplicationId = applicationId;
        result.DestinationRealm = CreateMatrixIdentity(realm);
        if (host.Length > 0)
        {
            result.DestinationHost = CreateMatrixIdentity(host);
        }

        return result;
    }

    private static DiameterPeerSnapshot CreateMatrixPeer(
        string identity,
        bool available,
        uint applicationId)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateMatrixIdentity(identity);
        result.Available = available;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        result.Capabilities.CommonApplications.Add(application);
        return result;
    }

    private static DiameterIdentity CreateMatrixIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static int CountMatrixAvps(
        DiameterMessage message,
        uint code)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == code)
            {
                result++;
            }

            index++;
        }

        return result;
    }
}

internal class MatrixTranslationHandler : ITranslationHandler
{
    internal int RequestCount;

    public DiameterTranslationResult TranslateDiameterRequest(
        DiameterRouteRequest? request,
        uint targetApplicationId)
    {
        RequestCount++;
        DiameterTranslationResult result =
            new DiameterTranslationResult();
        if (request == null)
        {
            result.Disposition = DiameterTranslationDisposition.Error;
            result.Code = "matrix-request-missing";
            result.Message = "The matrix request was missing.";
            return result;
        }

        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceApplicationId = request.ApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.TranslatedMessage = CloneMatrixMessage(request.Message);
        result.TranslatedMessage.ApplicationId = targetApplicationId;
        result.Code = "matrix-request-translated";
        result.Message = "The matrix request was translated.";
        return result;
    }

    public DiameterTranslationResult TranslateDiameterAnswer(
        DiameterTranslationResult? requestTranslation,
        DiameterMessage? answer)
    {
        throw new NotSupportedException();
    }

    internal DiameterMessage CloneMatrixMessage(DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp avp = new DiameterAvp();
            avp.Code = source.Avps[index].Code;
            avp.Flags = source.Avps[index].Flags;
            avp.VendorId = source.Avps[index].VendorId;
            avp.Data = source.Avps[index].Data.ToArray();
            result.Avps.Add(avp);
            index++;
        }

        return result;
    }
}
