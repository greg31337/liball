namespace diameter.Tests;

public class DiameterOverloadRfcExampleMatrixTests
{
    [Test]
    public void RfcCapabilityBitsComposeLossRateAndAdjacentPeerSupport()
    {
        DiameterOverloadConfiguration configuration =
            CreateOverloadConfiguration();
        configuration.EnablePeerReports = true;
        configuration.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        configuration.ReportingAlgorithm = DiameterOverloadAlgorithm.Rate;
        DiameterMessage request = CreateMessage(300U, 4U);

        DiameterDoicNegotiationResult doic =
            DiameterDoicProcessor.PrepareDiameterOverloadRequest(
                request,
                configuration,
                DiameterAgentRole.None);
        DiameterPeerOverloadMessageResult peer =
            DiameterPeerOverloadProcessor.PrepareDiameterPeerRequest(
                doic.Message,
                configuration,
                DiameterAgentRole.Relay,
                CreateIdentity("agent.example"));
        DiameterPeerFeatureInspection inspection =
            DiameterPeerOverloadProcessor.InspectDiameterPeerFeatures(
                peer.Message,
                CreateIdentity("AGENT.EXAMPLE"),
                false);

        Assert.Multiple(() =>
        {
            Assert.That(doic.FeatureVector, Is.EqualTo(5UL));
            Assert.That(peer.PeerSupported, Is.True);
            Assert.That(inspection.Valid, Is.True);
            Assert.That(inspection.Supported, Is.True);
            Assert.That(inspection.FeatureVector, Is.EqualTo(21UL));
            Assert.That(inspection.SourceId!.Value,
                Is.EqualTo("agent.example"));
        });
    }

    [Test]
    public void ReportScopesHonorRolloverAndExpireAtTheExactBoundary()
    {
        DiameterOverloadConfiguration configuration =
            CreateOverloadConfiguration();
        configuration.EnablePeerReports = true;
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = new DiameterOverloadState(
            configuration,
            clock,
            new SequenceOverloadRandom());
        ulong onePercent = ulong.MaxValue / 100UL;
        state.ProcessDiameterOverloadReport(
            CreateHostLossReport(
                "server.example",
                ulong.MaxValue - onePercent,
                0U,
                1U),
            clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            CreateHostLossReport(
                "server.example",
                onePercent,
                0U,
                1U),
            clock.UtcTime);
        DiameterOverloadReportUpdate repeated =
            state.ProcessDiameterOverloadReport(
                CreateHostLossReport(
                    "server.example",
                    onePercent,
                    100U,
                    1U),
                clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            CreateRealmLossReport("example", 1UL, 0U, 1U),
            clock.UtcTime);
        state.ProcessDiameterPeerOverloadReport(
            CreatePeerLossReport("peer.example", 1UL, 0U, 1U),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(
            clock,
            true);
        DiameterAdmissionDecision active =
            state.EvaluateDiameterLossAdmission(request);
        clock.Advance(1000L);
        request.EvaluatedAt = clock.UtcTime;
        DiameterAdmissionDecision expired =
            state.EvaluateDiameterLossAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Updated, Is.False);
            Assert.That(repeated.Report.SequenceNumber,
                Is.EqualTo(onePercent));
            Assert.That(repeated.Report.ReductionPercentage, Is.Zero);
            Assert.That(active.Reports, Has.Count.EqualTo(3));
            Assert.That(active.Reports.Select(report => report.ReportType),
                Is.EqualTo(new[]
                {
                    DiameterOverloadReportType.Host,
                    DiameterOverloadReportType.Realm,
                    DiameterOverloadReportType.Peer
                }));
            Assert.That(expired.Reports, Is.Empty);
            Assert.That(expired.Code, Is.EqualTo("overload-not-applicable"));
        });
    }

    [Test]
    public void RfcLossExampleAbatesTenOfOneHundredRequests()
    {
        int[] percentages = new int[100];
        int index = 0;
        while (index < percentages.Length)
        {
            percentages[index] = index + 1;
            index = index + 1;
        }

        DiameterOverloadConfiguration configuration =
            CreateOverloadConfiguration();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = new DiameterOverloadState(
            configuration,
            clock,
            new SequenceOverloadRandom(percentages));
        state.ProcessDiameterOverloadReport(
            CreateHostLossReport("server.example", 1UL, 10U, 30U),
            clock.UtcTime);
        int admitted = 0;
        int abated = 0;
        index = 0;
        while (index < 100)
        {
            DiameterAdmissionRequest request = CreateAdmissionRequest(
                clock,
                false);
            DiameterAdmissionDecision decision =
                state.EvaluateDiameterLossAdmission(request);
            if (decision.Admitted)
            {
                admitted = admitted + 1;
            }
            else
            {
                abated = abated + 1;
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(admitted, Is.EqualTo(90));
            Assert.That(abated, Is.EqualTo(10));
        });
    }

    [Test]
    public void RfcRateExampleCapsAThousandRequestSpikeAtNinetyPerSecond()
    {
        DiameterOverloadConfiguration configuration =
            CreateOverloadConfiguration();
        configuration.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        configuration.RateToleranceMilliseconds = 0;
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = new DiameterOverloadState(
            configuration,
            clock,
            new SequenceOverloadRandom());
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport("server.example", 1UL, 90U, 2U),
            clock.UtcTime);
        int admitted = 0;
        int index = 0;
        while (index < 1000)
        {
            clock.MonotonicMilliseconds = index;
            DiameterAdmissionRequest request = CreateAdmissionRequest(
                clock,
                false);
            DiameterAdmissionDecision decision =
                state.EvaluateDiameterRateAdmission(request);
            if (decision.Admitted)
            {
                admitted = admitted + 1;
            }

            index = index + 1;
        }

        clock.MonotonicMilliseconds = 2000L;
        clock.UtcTime = clock.UtcTime.AddSeconds(2);
        DiameterAdmissionRequest probe = CreateAdmissionRequest(clock, false);
        probe.Probe = true;
        DiameterAdmissionDecision expired =
            state.EvaluateDiameterRateAdmission(probe);

        Assert.Multiple(() =>
        {
            Assert.That(admitted, Is.InRange(80, 90));
            Assert.That(expired.Admitted, Is.True);
            Assert.That(expired.Code,
                Is.EqualTo("overload-rate-probe-admitted"));
        });
    }

    [Test]
    public void RfcLoadBoundariesDriveRoutingAndFallBackAfterExpiry()
    {
        DiameterOverloadConfiguration configuration =
            CreateOverloadConfiguration();
        configuration.EnableLoadReports = true;
        configuration.LoadReportIntervalMilliseconds = 1000;
        OverloadTestClock clock = new OverloadTestClock();
        DiameterLoadState load = new DiameterLoadState(
            configuration,
            clock);
        load.ProcessDiameterLoadReport(
            CreateLoadReport("available.example", 65535UL),
            CreateIdentity("available.example"),
            true,
            clock.UtcTime);
        load.ProcessDiameterLoadReport(
            CreateLoadReport("loaded.example", 0UL),
            CreateIdentity("loaded.example"),
            true,
            clock.UtcTime);
        SequenceRouteRandom random =
            new SequenceRouteRandom(65534L, 1L);
        RouteTable table = new RouteTable(clock, random);
        table.ConfigureDiameterLoadRouting(load);
        table.AddOrReplaceStaticRoute(
            CreateRoute("available", "available.example"));
        DiameterRouteConfiguration loadedRoute =
            CreateRoute("loaded", "loaded.example");
        loadedRoute.Weight = 2;
        table.AddOrReplaceStaticRoute(loadedRoute);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("available.example"));
        peers.Add(CreatePeer("loaded.example"));

        RouteSelectionResult fresh = table.SelectStaticRoute(
            CreateRouteRequest(clock),
            peers);
        clock.Advance(1000L);
        RouteSelectionResult expired = table.SelectStaticRoute(
            CreateRouteRequest(clock),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(fresh.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("available.example"));
            Assert.That(expired.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("loaded.example"));
            Assert.That(random.Index, Is.EqualTo(2));
            Assert.That(load.ReadActiveDiameterLoadReports(), Is.Empty);
        });
    }

    [Test]
    public void PublicAdmissionExposesAdmitDivertAndTooBusyThrottle()
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "node.example";
        configuration.OriginRealm.Value = "example";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Overload.Enabled = true;
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadManager manager = new DiameterOverloadManager(
            configuration,
            new RfcExampleOverloadSource(),
            clock);
        DiameterAdmissionRequest request = CreateAdmissionRequest(
            clock,
            true);

        DiameterAdmissionDecision admitted =
            manager.EvaluateDiameterAdmission(request);
        manager.ProcessDiameterOverloadReport(
            CreateHostLossReport("server.example", 1UL, 100U, 30U),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        DiameterAdmissionDecision diverted =
            manager.EvaluateDiameterAdmission(request);
        request.Candidate!.PeerIdentity!.Value = "mutated.example";
        DiameterAdmissionRequest throttleRequest = CreateAdmissionRequest(
            clock,
            false);
        DiameterAdmissionDecision throttled =
            manager.EvaluateDiameterAdmission(throttleRequest);

        Assert.Multiple(() =>
        {
            Assert.That(admitted.Action,
                Is.EqualTo(DiameterAdmissionAction.Admit));
            Assert.That(admitted.Admitted, Is.True);
            Assert.That(diverted.Action,
                Is.EqualTo(DiameterAdmissionAction.Divert));
            Assert.That(diverted.DiversionCandidate!.PeerIdentity!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(throttled.Action,
                Is.EqualTo(DiameterAdmissionAction.Throttle));
            Assert.That(throttled.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.DoesNotThrow(admitted.ValidateAdmissionDecision);
            Assert.DoesNotThrow(diverted.ValidateAdmissionDecision);
            Assert.DoesNotThrow(throttled.ValidateAdmissionDecision);
        });
    }

    private static DiameterOverloadConfiguration CreateOverloadConfiguration()
    {
        DiameterOverloadConfiguration result =
            new DiameterOverloadConfiguration();
        result.Enabled = true;
        return result;
    }

    private static DiameterMessage CreateMessage(
        uint commandCode,
        uint applicationId)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = commandCode;
        result.ApplicationId = applicationId;
        return result;
    }

    private static DiameterAdmissionRequest CreateAdmissionRequest(
        OverloadTestClock clock,
        bool allowDiversion)
    {
        DiameterAdmissionRequest result = new DiameterAdmissionRequest();
        result.Request.Message = CreateMessage(300U, 4U);
        result.Request.ApplicationId = 4U;
        result.Request.DestinationHost = CreateIdentity("server.example");
        result.Request.DestinationRealm = CreateIdentity("example");
        result.Request.ReceivedAt = clock.UtcTime;
        result.Candidate = new DiameterRouteCandidate();
        result.Candidate.PeerIdentity = CreateIdentity("peer.example");
        result.Candidate.Configuration.DestinationHost =
            CreateIdentity("server.example");
        result.Scope = DiameterOverloadScope.Application;
        result.AllowDiversion = allowDiversion;
        result.EvaluatedAt = clock.UtcTime;
        return result;
    }

    private static DiameterOverloadReport CreateHostLossReport(
        string host,
        ulong sequence,
        uint reduction,
        uint validity)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Host;
        result.Host = CreateIdentity(host);
        result.ApplicationId = 4U;
        result.SequenceNumber = sequence;
        result.ReductionPercentage = reduction;
        result.ValidityDurationSeconds = validity;
        return result;
    }

    private static DiameterOverloadReport CreateRealmLossReport(
        string realm,
        ulong sequence,
        uint reduction,
        uint validity)
    {
        DiameterOverloadReport result = CreateHostLossReport(
            "unused.example",
            sequence,
            reduction,
            validity);
        result.ReportType = DiameterOverloadReportType.Realm;
        result.Host = null;
        result.Realm = CreateIdentity(realm);
        return result;
    }

    private static DiameterOverloadReport CreatePeerLossReport(
        string peer,
        ulong sequence,
        uint reduction,
        uint validity)
    {
        DiameterOverloadReport result = CreateHostLossReport(
            "unused.example",
            sequence,
            reduction,
            validity);
        result.ReportType = DiameterOverloadReportType.Peer;
        result.Host = null;
        result.SourceId = CreateIdentity(peer);
        return result;
    }

    private static DiameterOverloadReport CreateHostRateReport(
        string host,
        ulong sequence,
        uint maximumRate,
        uint validity)
    {
        DiameterOverloadReport result = CreateHostLossReport(
            host,
            sequence,
            0U,
            validity);
        result.Algorithm = DiameterOverloadAlgorithm.Rate;
        result.ReductionPercentagePresent = false;
        result.MaximumRate = maximumRate;
        result.MaximumRatePresent = true;
        return result;
    }

    private static DiameterLoadReport CreateLoadReport(
        string peer,
        ulong value)
    {
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = DiameterLoadSource.Peer;
        result.SourceId = CreateIdentity(peer);
        result.LoadValue = value;
        return result;
    }

    private static DiameterRouteConfiguration CreateRoute(
        string routeIdentifier,
        string peer)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = routeIdentifier;
        result.DestinationRealm.Value = "example";
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(string identity)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Capabilities.CommonApplications.Add(application);
        return result;
    }

    private static DiameterRouteRequest CreateRouteRequest(
        OverloadTestClock clock)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message = CreateMessage(300U, 4U);
        result.ApplicationId = 4U;
        result.DestinationRealm = CreateIdentity("example");
        result.ReceivedAt = clock.UtcTime;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class RfcExampleOverloadSource : IOverloadSource
{
    public DiameterOverloadReport ReadLocalDiameterOverloadReport(
        DiameterAdmissionRequest? request)
    {
        return new DiameterOverloadReport();
    }

    public DiameterLoadReport ReadLocalDiameterLoadReport(
        DiameterLoadSource source)
    {
        return new DiameterLoadReport();
    }
}
