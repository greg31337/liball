namespace diameter.Tests;

public class DiameterOverloadManagerTests
{
    [Test]
    public void ConstructorRejectsMissingDependenciesAndInvalidNodeScope()
    {
        DiameterNodeConfiguration configuration = CreateConfiguration(
            DiameterNodeRole.Client);
        ManagerOverloadSource source = new ManagerOverloadSource();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterNodeConfiguration invalid = CreateConfiguration(
            DiameterNodeRole.None);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(null, source, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(configuration, null, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(configuration, source, null));
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(invalid, source, clock));
        });
    }

    [Test]
    public void LocalPublicationNormalizesOwnsAndTimestampsSourceReports()
    {
        DiameterNodeConfiguration configuration = CreateConfiguration(
            DiameterNodeRole.Client);
        configuration.Overload.EnableLoadReports = true;
        configuration.Overload.DefaultReportValiditySeconds = 20U;
        configuration.Overload.MaximumReportValiditySeconds = 30U;
        configuration.Overload.LoadReportIntervalMilliseconds = 2000;
        ManagerOverloadSource source = new ManagerOverloadSource();
        source.OverloadReport = CreateLossReport(
            "NODE.EXAMPLE",
            4U,
            1UL,
            25U,
            31U);
        source.OverloadReport.ExtensionAvps.Add(
            CreateAvp(900U, new byte[] { 1, 2 }));
        source.LoadReport = CreateLoadReport(
            DiameterLoadSource.Host,
            "NODE.EXAMPLE",
            50000UL);
        source.LoadReport.ExtensionAvps.Add(
            CreateAvp(901U, new byte[] { 3, 4 }));
        OverloadTestClock clock = new OverloadTestClock();
        IDiameterOverloadManager manager = new DiameterOverloadManager(
            configuration,
            source,
            clock);
        DiameterAdmissionRequest request = CreateAdmissionRequest(
            clock,
            "node.example",
            "peer.example");

        DiameterOverloadReport overload =
            manager.PublishDiameterOverloadReport(request);
        DiameterLoadReport load = manager.PublishDiameterLoadReport(
            DiameterLoadSource.Host);
        source.OverloadReport.Host!.Value = "mutated.example";
        source.OverloadReport.ExtensionAvps[0].Data[0] = 9;
        source.LoadReport.SourceId.Value = "mutated.example";
        source.LoadReport.ExtensionAvps[0].Data[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(overload.Host!.Value, Is.EqualTo("node.example"));
            Assert.That(overload.ValidityDurationSeconds, Is.EqualTo(20U));
            Assert.That(overload.CreatedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(overload.ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(20)));
            Assert.That(overload.Cached, Is.False);
            Assert.That(overload.ExtensionAvps[0].Data,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(load.SourceId.Value, Is.EqualTo("node.example"));
            Assert.That(load.CreatedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(load.ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddMilliseconds(2000)));
            Assert.That(load.Cached, Is.False);
            Assert.That(load.ExtensionAvps[0].Data,
                Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(source.LastAdmissionRequest, Is.Not.SameAs(request));
        });
    }

    [Test]
    public void ReceivedReportsAreCachedAndReturnedAsOwnedSnapshots()
    {
        DiameterNodeConfiguration configuration = CreateConfiguration(
            DiameterNodeRole.Client);
        configuration.Overload.EnablePeerReports = true;
        configuration.Overload.EnableLoadReports = true;
        ManagerOverloadSource source = new ManagerOverloadSource();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadManager manager = new DiameterOverloadManager(
            configuration,
            source,
            clock);
        DiameterOverloadReport overload = CreateLossReport(
            "server.example",
            4U,
            1UL,
            50U,
            30U);
        DiameterLoadReport load = CreateLoadReport(
            DiameterLoadSource.Host,
            "server.example",
            30000UL);
        DiameterOverloadReport peerOverload = CreatePeerReport(
            "peer.example",
            4U,
            1UL,
            20U);

        DiameterOverloadReport processedOverload =
            manager.ProcessDiameterOverloadReport(
                overload,
                CreateIdentity("peer.example"),
                clock.UtcTime);
        DiameterLoadReport processedLoad = manager.ProcessDiameterLoadReport(
            load,
            CreateIdentity("peer.example"),
            clock.UtcTime);
        DiameterOverloadReport processedPeer =
            manager.ProcessDiameterOverloadReport(
                peerOverload,
                CreateIdentity("PEER.EXAMPLE"),
                clock.UtcTime);
        processedOverload.Host!.Value = "mutated.example";
        processedPeer.SourceId!.Value = "mutated.example";
        processedLoad.SourceId.Value = "mutated.example";
        List<DiameterOverloadReport> overloadReports =
            manager.ReadDiameterOverloadReports();
        List<DiameterLoadReport> loadReports =
            manager.ReadDiameterLoadReports();
        overloadReports[0].Host!.Value = "changed.example";
        loadReports[0].SourceId.Value = "changed.example";

        Assert.Multiple(() =>
        {
            Assert.That(manager.ReadDiameterOverloadReports(),
                Has.Count.EqualTo(2));
            Assert.That(manager.ReadDiameterOverloadReports()[0].Host!.Value,
                Is.EqualTo("server.example"));
            Assert.That(manager.ReadDiameterOverloadReports()[1].SourceId!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(manager.ReadDiameterLoadReports()[0].SourceId.Value,
                Is.EqualTo("server.example"));
            Assert.That(overloadReports[0].Cached, Is.True);
            Assert.That(loadReports[0].Cached, Is.True);
        });
    }

    [Test]
    public void ServerOnlyManagerIgnoresHostLoadButAcceptsAdjacentPeerLoad()
    {
        DiameterNodeConfiguration configuration = CreateConfiguration(
            DiameterNodeRole.Server);
        configuration.Overload.EnableLoadReports = true;
        ManagerOverloadSource source = new ManagerOverloadSource();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadManager manager = new DiameterOverloadManager(
            configuration,
            source,
            clock);

        manager.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Host,
                "remote.example",
                10000UL),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        manager.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "peer.example",
                20000UL),
            CreateIdentity("PEER.EXAMPLE"),
            clock.UtcTime);
        List<DiameterLoadReport> reports =
            manager.ReadDiameterLoadReports();

        Assert.Multiple(() =>
        {
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(reports[0].Source,
                Is.EqualTo(DiameterLoadSource.Peer));
            Assert.That(reports[0].SourceId.Value,
                Is.EqualTo("peer.example"));
        });
    }

    [Test]
    public void PublicAdmissionCombinesLossRateLoadAndWatchdogBehavior()
    {
        DiameterNodeConfiguration lossConfiguration = CreateConfiguration(
            DiameterNodeRole.Client);
        lossConfiguration.Overload.EnableLoadReports = true;
        ManagerOverloadSource source = new ManagerOverloadSource();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadManager lossManager = new DiameterOverloadManager(
            lossConfiguration,
            source,
            clock);
        lossManager.ProcessDiameterOverloadReport(
            CreateLossReport(
                "server.example",
                4U,
                1UL,
                100U,
                30U),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        lossManager.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "peer.example",
                40000UL),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        DiameterAdmissionRequest lossRequest = CreateAdmissionRequest(
            clock,
            "server.example",
            "peer.example");
        lossRequest.AllowDiversion = false;

        DiameterAdmissionDecision loss =
            lossManager.EvaluateDiameterAdmission(lossRequest);

        DiameterNodeConfiguration rateConfiguration = CreateConfiguration(
            DiameterNodeRole.Client);
        rateConfiguration.Overload.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        rateConfiguration.Overload.RateToleranceMilliseconds = 0;
        DiameterOverloadManager rateManager = new DiameterOverloadManager(
            rateConfiguration,
            source,
            clock);
        rateManager.ProcessDiameterOverloadReport(
            CreateRateReport("server.example", 4U, 1UL, 1U),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        DiameterAdmissionRequest rateRequest = CreateAdmissionRequest(
            clock,
            "server.example",
            "peer.example");
        rateRequest.AllowDiversion = false;
        DiameterAdmissionDecision firstRate =
            rateManager.EvaluateDiameterAdmission(rateRequest);
        DiameterAdmissionDecision secondRate =
            rateManager.EvaluateDiameterAdmission(rateRequest);
        DiameterAdmissionRequest watchdog = CreateAdmissionRequest(
            clock,
            "server.example",
            "peer.example");
        watchdog.Request.Message.CommandCode =
            DiameterCommandCodes.DeviceWatchdog;
        watchdog.Request.Message.ApplicationId =
            DiameterApplicationIds.Common;
        watchdog.Request.ApplicationId = DiameterApplicationIds.Common;
        DiameterAdmissionDecision watchdogDecision =
            lossManager.EvaluateDiameterAdmission(watchdog);

        Assert.Multiple(() =>
        {
            Assert.That(loss.Action,
                Is.EqualTo(DiameterAdmissionAction.Throttle));
            Assert.That(loss.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(loss.LoadReport, Is.Not.Null);
            Assert.That(loss.LoadReport!.LoadValue, Is.EqualTo(40000UL));
            Assert.That(firstRate.Admitted, Is.True);
            Assert.That(secondRate.Action,
                Is.EqualTo(DiameterAdmissionAction.Throttle));
            Assert.That(secondRate.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(watchdogDecision.Admitted, Is.True);
            Assert.That(watchdogDecision.Code,
                Is.EqualTo("overload-watchdog-bypass"));
            Assert.DoesNotThrow(loss.ValidateAdmissionDecision);
            Assert.DoesNotThrow(secondRate.ValidateAdmissionDecision);
            Assert.DoesNotThrow(watchdogDecision.ValidateAdmissionDecision);
        });
    }

    [Test]
    public void ManagerRejectsAllInvalidDependenciesAndSourceOutcomes()
    {
        ManagerOverloadSource source = new ManagerOverloadSource();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterNodeConfiguration missingOverload = CreateConfiguration(
            DiameterNodeRole.Client);
        missingOverload.Overload = null!;
        DiameterNodeConfiguration invalidHost = CreateConfiguration(
            DiameterNodeRole.Client);
        invalidHost.OriginHost.Value = "";
        DiameterNodeConfiguration invalidRealm = CreateConfiguration(
            DiameterNodeRole.Client);
        invalidRealm.OriginRealm.Value = "";
        DiameterNodeConfiguration invalidRole = CreateConfiguration(
            (DiameterNodeRole)64);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(missingOverload, source, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(invalidHost, source, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(invalidRealm, source, clock));
            Assert.Throws<ArgumentException>(() =>
                new DiameterOverloadManager(invalidRole, source, clock));
        });

        DiameterNodeConfiguration configuration = CreateConfiguration(
            DiameterNodeRole.Client);
        DiameterOverloadManager manager = new DiameterOverloadManager(
            configuration,
            source,
            clock);
        Assert.Throws<ArgumentException>(() =>
            manager.PublishDiameterOverloadReport(null));
        Assert.Throws<ArgumentException>(() =>
            manager.ProcessDiameterOverloadReport(
                null,
                CreateIdentity("peer.example"),
                clock.UtcTime));
        Assert.Throws<ArgumentException>(() =>
            manager.ProcessDiameterOverloadReport(
                CreateLossReport("node.example", 4U, 1UL, 10U, 10U),
                null,
                clock.UtcTime));
        Assert.Throws<ArgumentException>(() =>
            manager.ProcessDiameterOverloadReport(
                CreateLossReport("node.example", 4U, 1UL, 10U, 10U),
                CreateIdentity(""),
                clock.UtcTime));
        Assert.Throws<ArgumentException>(() =>
            manager.EvaluateDiameterAdmission(null));

        source.OverloadReport = null!;
        Assert.Throws<DiameterValidationException>(() =>
            manager.PublishDiameterOverloadReport(
                CreateAdmissionRequest(clock, "node.example", "peer.example")));
        source.OverloadReport = CreateRateReport(
            "node.example", 4U, 1UL, 1U);
        Assert.Throws<DiameterValidationException>(() =>
            manager.PublishDiameterOverloadReport(
                CreateAdmissionRequest(clock, "node.example", "peer.example")));
        source.OverloadReport = CreatePeerReport(
            "node.example", 4U, 1UL, 10U);
        Assert.Throws<DiameterValidationException>(() =>
            manager.PublishDiameterOverloadReport(
                CreateAdmissionRequest(clock, "node.example", "peer.example")));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            manager.PublishDiameterLoadReport((DiameterLoadSource)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            manager.PublishDiameterLoadReport((DiameterLoadSource)2));
        source.LoadReport = null!;
        Assert.Throws<DiameterValidationException>(() =>
            manager.PublishDiameterLoadReport(DiameterLoadSource.Host));
        source.LoadReport = CreateLoadReport(
            DiameterLoadSource.Peer,
            "node.example",
            1UL);
        Assert.Throws<DiameterValidationException>(() =>
            manager.PublishDiameterLoadReport(DiameterLoadSource.Host));
        source.LoadReport = CreateLoadReport(
            DiameterLoadSource.Host,
            "other.example",
            1UL);
        Assert.Throws<DiameterValidationException>(() =>
            manager.PublishDiameterLoadReport(DiameterLoadSource.Host));
    }

    [Test]
    public void LocalScopeAndLoadAttachmentCoverHostRealmPeerAndEmptyCandidates()
    {
        ManagerOverloadSource source = new ManagerOverloadSource();
        OverloadTestClock clock = new OverloadTestClock();
        DiameterNodeConfiguration configuration = CreateConfiguration(
            DiameterNodeRole.Client);
        configuration.Overload.EnablePeerReports = true;
        configuration.Overload.EnableLoadReports = true;
        DiameterOverloadManager manager = new DiameterOverloadManager(
            configuration,
            source,
            clock);
        DiameterOverloadReport realm = new DiameterOverloadReport();
        realm.ReportType = DiameterOverloadReportType.Realm;
        realm.Realm = CreateIdentity("example");
        DiameterOverloadReport peer = CreatePeerReport(
            "node.example", 4U, 1UL, 1U);
        DiameterOverloadReport foreign = CreateLossReport(
            "foreign.example", 4U, 1UL, 1U, 10U);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                manager.ValidateLocalDiameterOverloadScope(realm));
            Assert.DoesNotThrow(() =>
                manager.ValidateLocalDiameterOverloadScope(peer));
            Assert.Throws<DiameterValidationException>(() =>
                manager.ValidateLocalDiameterOverloadScope(foreign));
        });

        DiameterAdmissionDecision empty = new DiameterAdmissionDecision();
        manager.AttachDiameterAdmissionLoadReport(empty);
        DiameterAdmissionDecision withoutDestination =
            new DiameterAdmissionDecision();
        withoutDestination.Request.Candidate = new DiameterRouteCandidate();
        withoutDestination.Request.Candidate.PeerIdentity =
            CreateIdentity("peer.example");
        withoutDestination.Request.Candidate.Configuration.DestinationHost =
            null;
        manager.AttachDiameterAdmissionLoadReport(withoutDestination);
        DiameterOverloadManager serverManager = new DiameterOverloadManager(
            CreateConfiguration(DiameterNodeRole.Server),
            source,
            clock);
        serverManager.AttachDiameterAdmissionLoadReport(withoutDestination);
        Assert.That(empty.LoadReport, Is.Null);
        Assert.That(withoutDestination.LoadReport, Is.Null);
    }

    private static DiameterNodeConfiguration CreateConfiguration(
        DiameterNodeRole role)
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example";
        result.Roles = role;
        result.Overload.Enabled = true;
        return result;
    }

    private static DiameterAdmissionRequest CreateAdmissionRequest(
        OverloadTestClock clock,
        string destinationHost,
        string peer)
    {
        DiameterAdmissionRequest result = new DiameterAdmissionRequest();
        result.Request.Message.Flags = DiameterMessageFlags.Request;
        result.Request.Message.CommandCode = 300U;
        result.Request.Message.ApplicationId = 4U;
        result.Request.ApplicationId = 4U;
        result.Request.DestinationHost = CreateIdentity(destinationHost);
        result.Request.DestinationRealm = CreateIdentity("example");
        result.Request.ReceivedAt = clock.UtcTime;
        result.Candidate = new DiameterRouteCandidate();
        result.Candidate.PeerIdentity = CreateIdentity(peer);
        result.Candidate.Configuration.DestinationHost =
            CreateIdentity(destinationHost);
        result.Candidate.Weight = 1;
        result.Scope = DiameterOverloadScope.Application;
        result.EvaluatedAt = clock.UtcTime;
        return result;
    }

    private static DiameterOverloadReport CreateLossReport(
        string host,
        uint applicationId,
        ulong sequence,
        uint reduction,
        uint validity)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Host;
        result.Host = CreateIdentity(host);
        result.ApplicationId = applicationId;
        result.SequenceNumber = sequence;
        result.ReductionPercentage = reduction;
        result.ValidityDurationSeconds = validity;
        return result;
    }

    private static DiameterOverloadReport CreateRateReport(
        string host,
        uint applicationId,
        ulong sequence,
        uint maximumRate)
    {
        DiameterOverloadReport result = CreateLossReport(
            host,
            applicationId,
            sequence,
            0U,
            30U);
        result.Algorithm = DiameterOverloadAlgorithm.Rate;
        result.ReductionPercentagePresent = false;
        result.MaximumRatePresent = true;
        result.MaximumRate = maximumRate;
        return result;
    }

    private static DiameterOverloadReport CreatePeerReport(
        string peer,
        uint applicationId,
        ulong sequence,
        uint reduction)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Peer;
        result.SourceId = CreateIdentity(peer);
        result.ApplicationId = applicationId;
        result.SequenceNumber = sequence;
        result.ReductionPercentage = reduction;
        result.ValidityDurationSeconds = 30U;
        return result;
    }

    private static DiameterLoadReport CreateLoadReport(
        DiameterLoadSource source,
        string identity,
        ulong loadValue)
    {
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = source;
        result.SourceId = CreateIdentity(identity);
        result.LoadValue = loadValue;
        return result;
    }

    private static DiameterAvp CreateAvp(uint code, byte[] data)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = data;
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class ManagerOverloadSource : IOverloadSource
{
    internal DiameterOverloadReport OverloadReport =
        new DiameterOverloadReport();
    internal DiameterLoadReport LoadReport = new DiameterLoadReport();
    internal DiameterAdmissionRequest? LastAdmissionRequest;

    public DiameterOverloadReport ReadLocalDiameterOverloadReport(
        DiameterAdmissionRequest? request)
    {
        LastAdmissionRequest = request;
        return OverloadReport;
    }

    public DiameterLoadReport ReadLocalDiameterLoadReport(
        DiameterLoadSource source)
    {
        return LoadReport;
    }
}
