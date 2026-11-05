using System.Text;

namespace diameter.Tests;

public class RoutingTopologyEndToEndTests
{
    [Test]
    public void RelayProxyPriorityAndLoopBehaveAcrossThreeNodeTopology()
    {
        RoutingWireTopology topology = new RoutingWireTopology();
        try
        {
            DiameterMessage relaySource = CreateRequest(
                "source.example;relay;1",
                "remote.example",
                101U,
                201U);
            AddInteger32(
                relaySource,
                DiameterAvpCodes.Drmp,
                (int)DiameterRoutingPriority.Priority2);
            topology.Send(topology.SourceToAgent, relaySource);
            DiameterMessage relayIngress = topology.Receive(
                topology.AgentFromSource);
            DiameterRouteDecision relayDecision = SelectPeerRoute(
                topology,
                relayIngress,
                DiameterRouteAction.Relay,
                DiameterAgentRole.Relay);
            RelayAgent relay = new RelayAgent(
                new RoutingTopologyIdentifierSource(700U),
                8);
            RelayRequestProcessingResult relayForwarded =
                relay.ForwardRelayRequest(
                    relayDecision,
                    topology.AgentFromSource
                        .ReadDiameterConnectionMetadata());
            topology.Send(
                topology.AgentToDestination,
                relayForwarded.Message!);
            DiameterMessage relayAtDestination = topology.Receive(
                topology.DestinationFromAgent);
            DiameterMessage relayDestinationAnswer = CreateAnswer(
                relayAtDestination,
                DiameterResultCodes.Success,
                false);
            topology.Send(
                topology.DestinationFromAgent,
                relayDestinationAnswer);
            RelayAnswerProcessingResult relayAnswered =
                relay.ForwardRelayAnswer(
                    topology.AgentToDestination
                        .ReadDiameterConnectionMetadata(),
                    topology.Receive(topology.AgentToDestination));
            topology.Send(
                topology.AgentFromSource,
                relayAnswered.Message!);
            DiameterMessage relayAtSource = topology.Receive(
                topology.SourceToAgent);

            DiameterMessage proxySource = CreateRequest(
                "source.example;proxy;1",
                "remote.example",
                301U,
                401U);
            topology.Send(topology.SourceToAgent, proxySource);
            DiameterMessage proxyIngress = topology.Receive(
                topology.AgentFromSource);
            DiameterRouteDecision proxyDecision = SelectPeerRoute(
                topology,
                proxyIngress,
                DiameterRouteAction.Proxy,
                DiameterAgentRole.Proxy);
            ProxyAgent proxy = new ProxyAgent(
                CreateIdentity("agent.example"),
                CreateProtectionKey(),
                new RoutingTopologyIdentifierSource(800U),
                8);
            ProxyRequestProcessingResult proxyForwarded =
                proxy.ForwardProxyRequest(
                    proxyDecision,
                    topology.AgentFromSource
                        .ReadDiameterConnectionMetadata());
            topology.Send(
                topology.AgentToDestination,
                proxyForwarded.Message!);
            DiameterMessage proxyAtDestination = topology.Receive(
                topology.DestinationFromAgent);
            DiameterMessage proxyDestinationAnswer = CreateAnswer(
                proxyAtDestination,
                DiameterResultCodes.Success,
                true);
            topology.Send(
                topology.DestinationFromAgent,
                proxyDestinationAnswer);
            ProxyAnswerProcessingResult proxyAnswered =
                proxy.ForwardProxyAnswer(
                    topology.AgentToDestination
                        .ReadDiameterConnectionMetadata(),
                    topology.Receive(topology.AgentToDestination));
            topology.Send(
                topology.AgentFromSource,
                proxyAnswered.Message!);
            DiameterMessage proxyAtSource = topology.Receive(
                topology.SourceToAgent);

            DiameterMessage loopMessage = CreateRequest(
                "source.example;loop;1",
                "remote.example",
                501U,
                601U);
            loopMessage.Avps.Add(CreateTextAvp(
                DiameterAvpCodes.RouteRecord,
                "agent.example"));
            DiameterRouteDecision loop = SelectPeerRoute(
                topology,
                loopMessage,
                DiameterRouteAction.Relay,
                DiameterAgentRole.Relay);

            Assert.Multiple(() =>
            {
                Assert.That(relayDecision.Routed, Is.True);
                Assert.That(relayDecision.Request.RoutingPriority,
                    Is.EqualTo(DiameterRoutingPriority.Priority2));
                Assert.That(relayForwarded.RoutingPriority,
                    Is.EqualTo(DiameterRoutingPriority.Priority2));
                Assert.That(relayAtDestination.HopByHopIdentifier,
                    Is.Not.EqualTo(relaySource.HopByHopIdentifier));
                Assert.That(relayAtDestination.EndToEndIdentifier,
                    Is.EqualTo(relaySource.EndToEndIdentifier));
                Assert.That(relayAtSource.HopByHopIdentifier,
                    Is.EqualTo(relaySource.HopByHopIdentifier));
                Assert.That(relayAnswered.Forwarded, Is.True);
                Assert.That(proxyForwarded.Accepted, Is.True);
                Assert.That(CountAvps(
                    proxyAtDestination,
                    DiameterAvpCodes.ProxyInfo), Is.EqualTo(1));
                Assert.That(proxyAnswered.Forwarded, Is.True);
                Assert.That(CountAvps(
                    proxyAtSource,
                    DiameterAvpCodes.ProxyInfo), Is.Zero);
                Assert.That(proxyAtSource.HopByHopIdentifier,
                    Is.EqualTo(proxySource.HopByHopIdentifier));
                Assert.That(loop.Routed, Is.False);
                Assert.That(loop.Rejected, Is.True);
                Assert.That(loop.ResultCode,
                    Is.EqualTo(DiameterResultCodes.LoopDetected));
                Assert.That(loop.Code, Is.EqualTo("route-loop-detected"));
            });
        }
        finally
        {
            topology.Close();
        }
    }

    [Test]
    public void HostRealmRedirectAndTranslationProduceReroutableWireOutcomes()
    {
        InProcessHarnessClock clock = new InProcessHarnessClock();
        DiameterMessage requestMessage = CreateRequest(
            "client.example;redirect;1",
            "original.example",
            11U,
            12U);
        DiameterRouteRequest request = CreateRouteRequest(
            requestMessage,
            "client.example",
            "original.example");

        RouteTable hostTable = new RouteTable(
            clock,
            new SequenceRouteRandom(0L));
        DiameterRouteConfiguration hostRedirect =
            CreateRoute(
                "host-redirect",
                "original.example",
                null,
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        hostRedirect.RedirectHosts.Add(
            CreateUri("redirect-target.example"));
        hostRedirect.RedirectUsage =
            DiameterRedirectHostUsage.AllSession;
        hostRedirect.RedirectMaximumCacheTimeSeconds = 30U;
        hostTable.AddOrReplaceStaticRoute(hostRedirect);
        RouteSelector hostSelector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Redirect),
            hostTable);
        DiameterRouteDecision hostDecision = hostSelector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        RedirectAnswerGenerationResult hostAnswer = new RedirectAgent(
            CreateIdentity("redirector.example"),
            CreateIdentity("example"))
            .GenerateRedirectAnswer(hostDecision);
        DiameterMessage hostWireAnswer = WireRoundTrip(
            hostAnswer.Answer!);
        List<DiameterPeerSnapshot> redirectPeers =
            new List<DiameterPeerSnapshot>();
        redirectPeers.Add(CreatePeer(
            "redirect-target.example",
            null,
            4U));
        RedirectProcessingResult hostProcessed =
            hostSelector.Redirects.ProcessRedirectAnswer(
                request,
                hostWireAnswer,
                CreateIdentity("redirector.example"),
                true,
                clock.ReadUtcTime());
        RedirectRouteSelectionResult hostSelected =
            hostSelector.Redirects.SelectRedirectRoute(
                request,
                redirectPeers);

        RouteTable realmTable = new RouteTable(
            clock,
            new SequenceRouteRandom(0L));
        realmTable.AddOrReplaceStaticRoute(CreateRoute(
            "alternate-route",
            "alternate.example",
            "alternate-peer.example",
            DiameterRouteAction.Relay,
            DiameterAgentRole.Relay));
        DiameterNodeConfiguration realmClientConfiguration =
            CreateNodeConfiguration(
                DiameterNodeRole.Client | DiameterNodeRole.Relay);
        RouteSelector realmClient = new RouteSelector(
            realmClientConfiguration,
            realmTable);
        RouteTable realmServerTable = new RouteTable(
            clock,
            new SequenceRouteRandom(0L));
        DiameterRouteRequest realmRequest = CreateRouteRequest(
            requestMessage,
            "client.example",
            "example");
        DiameterRouteConfiguration realmRedirect = CreateRoute(
            "realm-redirect",
            "example",
            null,
            DiameterRouteAction.Redirect,
            DiameterAgentRole.Redirect);
        realmRedirect.RedirectRealms.Add(
            CreateIdentity("alternate.example"));
        realmRedirect.RedirectUsage =
            DiameterRedirectHostUsage.RealmAndApplication;
        realmRedirect.RedirectMaximumCacheTimeSeconds = 30U;
        realmServerTable.AddOrReplaceStaticRoute(realmRedirect);
        RouteSelector realmServer = new RouteSelector(
            CreateNodeConfiguration(
                DiameterNodeRole.Server | DiameterNodeRole.Redirect),
            realmServerTable);
        DiameterRouteDecision realmDecision = realmServer.SelectRoute(
            realmRequest,
            new List<DiameterPeerSnapshot>());
        RedirectAnswerGenerationResult realmGenerated = realmServer
            .RealmRedirects.GenerateRealmRedirectAnswer(realmDecision);
        Assert.That(
            realmGenerated.Generated,
            Is.True,
            realmDecision.Code + ": " + realmGenerated.Code);
        DiameterMessage realmWireAnswer = realmGenerated.Answer!;
        List<DiameterPeerSnapshot> realmPeers =
            new List<DiameterPeerSnapshot>();
        realmPeers.Add(CreatePeer(
            "alternate-peer.example",
            null,
            4U));
        RealmRedirectProcessingResult realmProcessed =
            realmClient.ProcessRealmRedirectAnswer(
                realmRequest,
                realmWireAnswer,
                CreateIdentity("redirector.example"),
                true,
                clock.ReadUtcTime(),
                realmPeers);

        RoutingTopologyTranslationHandler translationHandler =
            new RoutingTopologyTranslationHandler();
        DiameterNodeConfiguration translationConfiguration =
            CreateNodeConfiguration(DiameterNodeRole.Translation);
        DiameterTranslationHandlerConfiguration handler =
            new DiameterTranslationHandlerConfiguration();
        handler.SourceApplicationId = 4U;
        handler.TargetApplicationId = 9U;
        handler.Handler = translationHandler;
        translationConfiguration.TranslationHandlers.Add(handler);
        DiameterApplicationId targetApplication =
            new DiameterApplicationId();
        targetApplication.Id = 9U;
        translationConfiguration.Applications.Add(targetApplication);
        RouteTable translationTable = new RouteTable(
            clock,
            new SequenceRouteRandom(0L));
        DiameterRouteConfiguration translationRoute = CreateRoute(
            "translation",
            "original.example",
            null,
            DiameterRouteAction.Translate,
            DiameterAgentRole.Translation);
        translationRoute.TargetApplicationId = 9U;
        translationTable.AddOrReplaceStaticRoute(translationRoute);
        RouteSelector translationSelector = new RouteSelector(
            translationConfiguration,
            translationTable);
        DiameterRouteDecision translated = translationSelector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        DiameterMessage targetAnswer = CreateAnswer(
            WireRoundTrip(translated.RoutedMessage!),
            DiameterResultCodes.Success,
            false);
        DiameterTranslationResult translatedAnswer =
            translationSelector.Translations.TranslateDispatchedAnswer(
                translated.TranslationResult,
                WireRoundTrip(targetAnswer));

        Assert.That(
            realmProcessed.Rerouted,
            Is.True,
            realmProcessed.Code + ": " + realmProcessed.Message);
        Assert.Multiple(() =>
        {
            Assert.That(hostAnswer.Generated, Is.True);
            Assert.That(hostProcessed.Accepted, Is.True);
            Assert.That(hostSelected.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("redirect-target.example"));
            Assert.That(realmProcessed.Request!.DestinationRealm!.Value,
                Is.EqualTo("alternate.example"));
            Assert.That(realmProcessed.Candidate!.PeerIdentity!.Value,
                Is.EqualTo("alternate-peer.example"));
            Assert.That(translated.Routed, Is.True);
            Assert.That(translated.RoutedMessage!.ApplicationId,
                Is.EqualTo(9U));
            Assert.That(translatedAnswer.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Translated));
            Assert.That(translatedAnswer.TranslatedMessage!.ApplicationId,
                Is.EqualTo(4U));
            Assert.That(translationHandler.RequestCount, Is.EqualTo(1));
            Assert.That(translationHandler.AnswerCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void OverloadDiversionAndLoadReportsDriveRouteSelection()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterNodeConfiguration configuration =
            CreateNodeConfiguration(DiameterNodeRole.Client);
        configuration.Overload.Enabled = true;
        configuration.Overload.EnableLoadReports = true;
        configuration.Overload.LoadReportIntervalMilliseconds = 1000;
        DiameterOverloadManager overload = new DiameterOverloadManager(
            configuration,
            new ManagerOverloadSource(),
            clock);
        overload.ProcessDiameterOverloadReport(
            CreateOverloadReport("overloaded.example"),
            CreateIdentity("upstream.example"),
            clock.UtcTime);
        overload.ProcessDiameterLoadReport(
            CreateLoadReport("overloaded.example", 100UL),
            CreateIdentity("upstream.example"),
            clock.UtcTime);
        overload.ProcessDiameterLoadReport(
            CreateLoadReport("available.example", 900UL),
            CreateIdentity("upstream.example"),
            clock.UtcTime);

        RouteTable table = new RouteTable(
            clock,
            new SequenceRouteRandom(100L));
        table.ConfigureDiameterLoadRouting(overload.LoadState);
        table.AddOrReplaceStaticRoute(CreateRoute(
            "overloaded",
            "remote.example",
            "overloaded.example",
            DiameterRouteAction.Relay,
            DiameterAgentRole.Relay));
        table.AddOrReplaceStaticRoute(CreateRoute(
            "available",
            "remote.example",
            "available.example",
            DiameterRouteAction.Relay,
            DiameterAgentRole.Relay));
        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("overloaded.example", null, 4U));
        peers.Add(CreatePeer("available.example", null, 4U));
        DiameterRouteRequest routeRequest = CreateRouteRequest(
            CreateRequest(
                "client.example;load;1",
                "remote.example",
                71U,
                72U),
            "client.example",
            "remote.example");
        RouteSelectionResult selected = table.SelectStaticRoute(
            routeRequest,
            peers);

        DiameterAdmissionRequest admission =
            new DiameterAdmissionRequest();
        admission.Request = routeRequest;
        admission.Candidate = CreateAdmissionCandidate(
            "overloaded.example");
        admission.Scope = DiameterOverloadScope.Application;
        admission.EvaluatedAt = clock.UtcTime;
        DiameterAdmissionDecision diverted =
            overload.EvaluateDiameterAdmission(admission);

        Assert.Multiple(() =>
        {
            Assert.That(selected.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("available.example"));
            Assert.That(diverted.Action,
                Is.EqualTo(DiameterAdmissionAction.Divert));
            Assert.That(diverted.Admitted, Is.False);
            Assert.That(diverted.Reports, Has.Count.EqualTo(1));
            Assert.That(overload.ReadDiameterLoadReports(),
                Has.Count.EqualTo(2));
        });
    }

    private static DiameterRouteDecision SelectPeerRoute(
        RoutingWireTopology topology,
        DiameterMessage message,
        DiameterRouteAction action,
        DiameterAgentRole role)
    {
        RouteTable table = new RouteTable(
            topology.Clock,
            new SequenceRouteRandom(0L));
        table.AddOrReplaceStaticRoute(CreateRoute(
            "wire-route",
            "remote.example",
            "destination.example",
            action,
            role));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(
                action == DiameterRouteAction.Proxy ?
                    DiameterNodeRole.Proxy : DiameterNodeRole.Relay),
            table);
        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer(
            "destination.example",
            topology.AgentToDestination
                .ReadDiameterConnectionMetadata(),
            4U));
        return selector.SelectRoute(
            CreateRouteRequest(
                message,
                "source.example",
                "remote.example"),
            peers);
    }

    private static DiameterNodeConfiguration CreateNodeConfiguration(
        DiameterNodeRole role)
    {
        DiameterNodeConfiguration result =
            new DiameterNodeConfiguration();
        result.OriginHost.Value = role == DiameterNodeRole.Redirect ?
            "redirector.example" : "agent.example";
        result.OriginRealm.Value = "example";
        result.ProductName = "routing-topology-tests";
        result.Roles = role;
        result.Transport.EnableListener =
            (role & DiameterNodeRole.Server) != DiameterNodeRole.None;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
        return result;
    }

    private static DiameterRouteConfiguration CreateRoute(
        string identifier,
        string realm,
        string? peer,
        DiameterRouteAction action,
        DiameterAgentRole role)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = 4U;
        if (peer != null)
        {
            result.PeerIdentity = CreateIdentity(peer);
        }

        result.Action = action;
        result.AgentRole = role;
        return result;
    }

    private static DiameterRouteRequest CreateRouteRequest(
        DiameterMessage message,
        string sourcePeer,
        string realm)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message = message;
        result.SourcePeer = CreateIdentity(sourcePeer);
        result.OriginHost = CreateIdentity(sourcePeer);
        result.DestinationRealm = CreateIdentity(realm);
        result.ApplicationId = message.ApplicationId;
        result.SessionId = ReadTextAvp(
            message,
            DiameterAvpCodes.SessionId);
        result.ReceivedAt = new DateTimeOffset(
            2026,
            8,
            29,
            20,
            0,
            0,
            TimeSpan.Zero);
        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(
        string identity,
        DiameterConnectionMetadata? connection,
        uint applicationId)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        result.Capabilities.CommonApplications.Add(application);
        result.Connection = connection;
        return result;
    }

    private static DiameterMessage CreateRequest(
        string sessionId,
        string realm,
        uint hopByHopIdentifier,
        uint endToEndIdentifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        result.HopByHopIdentifier = hopByHopIdentifier;
        result.EndToEndIdentifier = endToEndIdentifier;
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            sessionId));
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.OriginHost,
            "source.example"));
        result.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            realm));
        return result;
    }

    private static DiameterMessage CreateAnswer(
        DiameterMessage request,
        uint resultCode,
        bool copyProxyInformation)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        AddUnsigned32(result, DiameterAvpCodes.ResultCode, resultCode);
        if (copyProxyInformation)
        {
            int index = 0;
            while (index < request.Avps.Count)
            {
                DiameterAvp source = request.Avps[index];
                if (source.Code == DiameterAvpCodes.ProxyInfo &&
                    source.VendorId == DiameterVendorIds.Ietf)
                {
                    DiameterAvp copied = new DiameterAvp();
                    copied.Code = source.Code;
                    copied.Flags = source.Flags;
                    copied.VendorId = source.VendorId;
                    copied.Data = source.Data.ToArray();
                    result.Avps.Add(copied);
                }

                index = index + 1;
            }
        }

        return result;
    }

    private static DiameterMessage WireRoundTrip(DiameterMessage message)
    {
        DiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterEncodeResult encoded = codec.EncodeMessageToArray(message);
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        DiameterDecodeResult decoded = codec.DecodeMessage(
            encoded.Data,
            false,
            new DiameterLimitConfiguration());
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Message!;
    }

    private static DiameterUri CreateUri(string host)
    {
        DiameterUri result = new DiameterUri();
        result.Host.Value = host;
        result.Protocol = "diameter";
        result.Transport = "tcp";
        result.Port = DiameterProtocolConstants.Port;
        return result;
    }

    private static DiameterOverloadReport CreateOverloadReport(string host)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Host;
        result.Host = CreateIdentity(host);
        result.ApplicationId = 4U;
        result.SequenceNumber = 1UL;
        result.ReductionPercentage = 100U;
        result.ValidityDurationSeconds = 30U;
        return result;
    }

    private static DiameterLoadReport CreateLoadReport(
        string host,
        ulong load)
    {
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = DiameterLoadSource.Host;
        result.SourceId = CreateIdentity(host);
        result.LoadValue = load;
        return result;
    }

    private static DiameterRouteCandidate CreateAdmissionCandidate(
        string host)
    {
        DiameterRouteCandidate result = new DiameterRouteCandidate();
        result.Configuration.DestinationHost = CreateIdentity(host);
        result.PeerIdentity = CreateIdentity(host);
        result.Weight = 1;
        return result;
    }

    private static byte[] CreateProtectionKey()
    {
        byte[] result = new byte[32];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(index + 1);
            index = index + 1;
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

    private static void AddUnsigned32(
        DiameterMessage message,
        uint code,
        uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static void AddInteger32(
        DiameterMessage message,
        uint code,
        int value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteInt32(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static int CountAvps(DiameterMessage message, uint code)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static string ReadTextAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code &&
                message.Avps[index].VendorId == DiameterVendorIds.Ietf)
            {
                return Encoding.UTF8.GetString(message.Avps[index].Data);
            }

            index = index + 1;
        }

        return string.Empty;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class RoutingWireTopology
{
    internal InProcessHarnessClock Clock = new InProcessHarnessClock();
    internal InProcessHarnessNetwork Network;
    internal IDiameterListener AgentListener;
    internal IDiameterListener DestinationListener;
    internal IDiameterConnection SourceToAgent;
    internal IDiameterConnection AgentFromSource;
    internal IDiameterConnection AgentToDestination;
    internal IDiameterConnection DestinationFromAgent;
    internal DiameterMessageCodec Codec = new DiameterMessageCodec();

    internal RoutingWireTopology()
    {
        Network = new InProcessHarnessNetwork(Clock);
        InProcessHarnessTransportFactory source =
            new InProcessHarnessTransportFactory(Network, "source.example");
        InProcessHarnessTransportFactory agent =
            new InProcessHarnessTransportFactory(Network, "agent.example");
        InProcessHarnessTransportFactory destination =
            new InProcessHarnessTransportFactory(
                Network,
                "destination.example");
        DiameterSecurityConfiguration security =
            new DiameterSecurityConfiguration();
        security.AllowUnsecuredTransport = true;
        DiameterTransportConfiguration agentTransport =
            CreateTransport(42001, true);
        DiameterTransportConfiguration destinationTransport =
            CreateTransport(42002, true);
        AgentListener = agent.CreateDiameterListener(
            agentTransport,
            security).Listener!;
        DestinationListener = destination.CreateDiameterListener(
            destinationTransport,
            security).Listener!;
        SourceToAgent = source.ConnectDiameterEndpoint(
            CreateEndpoint("agent.example", 42001),
            CreateTransport(42000, false),
            security,
            1000).Connection!;
        AgentFromSource = AgentListener.AcceptDiameterConnection(1000)
            .Connection!;
        AgentToDestination = agent.ConnectDiameterEndpoint(
            CreateEndpoint("destination.example", 42002),
            CreateTransport(42001, false),
            security,
            1000).Connection!;
        DestinationFromAgent = DestinationListener
            .AcceptDiameterConnection(1000).Connection!;
    }

    internal void Send(IDiameterConnection connection, DiameterMessage message)
    {
        DiameterEncodeResult encoded = Codec.EncodeMessageToArray(message);
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        DiameterTransportIoResult sent = connection
            .SendDiameterConnectionBytes(encoded.Data, 1000);
        Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
    }

    internal DiameterMessage Receive(IDiameterConnection connection)
    {
        byte[] buffer = new byte[65536];
        DiameterTransportIoResult received = connection
            .ReceiveDiameterConnectionBytes(buffer, 1000);
        Assert.That(received.Status,
            Is.EqualTo(DiameterTransportStatus.Success));
        DiameterDecodeResult decoded = Codec.DecodeMessage(
            buffer.AsSpan(0, received.TransferredCount),
            false,
            new DiameterLimitConfiguration());
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Message!;
    }

    internal void Close()
    {
        SourceToAgent.CloseDiameterConnection();
        AgentFromSource.CloseDiameterConnection();
        AgentToDestination.CloseDiameterConnection();
        DestinationFromAgent.CloseDiameterConnection();
        AgentListener.CloseDiameterListener();
        DestinationListener.CloseDiameterListener();
    }

    internal static DiameterTransportConfiguration CreateTransport(
        int port,
        bool listener)
    {
        DiameterTransportConfiguration result =
            new DiameterTransportConfiguration();
        result.Host = "127.0.0.1";
        result.Port = port;
        result.EnableListener = listener;
        result.EnableActiveConnections = !listener;
        return result;
    }

    internal static DiameterEndpoint CreateEndpoint(
        string identity,
        int port)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = identity;
        result.Host = identity;
        result.Address = System.Net.IPAddress.Loopback;
        result.Port = port;
        result.Transport = DiameterTransportKind.Tcp;
        return result;
    }
}

internal class RoutingTopologyIdentifierSource : IDiameterIdentifierSource
{
    internal uint HopByHopIdentifier;

    internal RoutingTopologyIdentifierSource(uint hopByHopIdentifier)
    {
        HopByHopIdentifier = hopByHopIdentifier;
    }

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier = HopByHopIdentifier + 1U;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        return 1U;
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        return originHost.Value + ";routing";
    }

    public uint CreateOriginStateIdentifier()
    {
        return 1U;
    }
}

internal class RoutingTopologyTranslationHandler : ITranslationHandler
{
    internal int RequestCount;
    internal int AnswerCount;

    public DiameterTranslationResult TranslateDiameterRequest(
        DiameterRouteRequest? request,
        uint targetApplicationId)
    {
        RequestCount = RequestCount + 1;
        DiameterTranslationResult result =
            new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceApplicationId = request!.ApplicationId;
        result.TargetApplicationId = targetApplicationId;
        result.TranslatedMessage = RouteTableCloner.CloneRouteMessage(
            request.Message);
        result.TranslatedMessage.ApplicationId = targetApplicationId;
        result.TranslationState = new byte[] { 1, 2, 3 };
        result.Code = "topology-request-translated";
        result.Message = "The topology request was translated.";
        return result;
    }

    public DiameterTranslationResult TranslateDiameterAnswer(
        DiameterTranslationResult? requestTranslation,
        DiameterMessage? answer)
    {
        AnswerCount = AnswerCount + 1;
        DiameterTranslationResult result =
            new DiameterTranslationResult();
        result.Disposition = DiameterTranslationDisposition.Translated;
        result.SourceApplicationId =
            requestTranslation!.SourceApplicationId;
        result.TargetApplicationId =
            requestTranslation.TargetApplicationId;
        result.TranslatedMessage = RouteTableCloner.CloneRouteMessage(
            answer!);
        result.TranslatedMessage.ApplicationId =
            requestTranslation.SourceApplicationId;
        result.TranslationState = requestTranslation.TranslationState;
        result.Code = "topology-answer-translated";
        result.Message = "The topology answer was translated.";
        return result;
    }
}
