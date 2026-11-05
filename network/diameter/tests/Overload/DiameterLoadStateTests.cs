using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class DiameterLoadStateTests
{
    [Test]
    public void CreatedLoadReportEncodesRequiredFieldsAndOwnsExtensions()
    {
        DateTimeOffset createdAt = OverloadTestClock.CreateOverloadTime();
        DiameterLoadReport report =
            DiameterLoadProcessor.CreateDiameterLoadReport(
                DiameterLoadSource.Host,
                CreateIdentity("SERVER.EXAMPLE"),
                50000UL,
                createdAt,
                1000);
        DiameterAvp extension = CreateAvp(900U, new byte[] { 7, 8 });
        report.ExtensionAvps.Add(extension);

        DiameterAvp encoded = DiameterLoadProcessor.CreateDiameterLoadAvp(
            report);
        extension.Data[0] = 1;
        List<DiameterAvp> children = DecodeChildren(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(report.SourceId.Value,
                Is.EqualTo("server.example"));
            Assert.That(report.ExpiresAt,
                Is.EqualTo(createdAt.AddMilliseconds(1000)));
            Assert.That(ReadInteger32Child(
                children,
                DiameterAvpCodes.LoadType),
                Is.EqualTo((int)DiameterLoadSource.Host));
            Assert.That(ReadUnsigned64Child(
                children,
                DiameterAvpCodes.LoadValue), Is.EqualTo(50000UL));
            Assert.That(ReadTextChild(children, DiameterAvpCodes.SourceId),
                Is.EqualTo("server.example"));
            Assert.That(FindChild(children, 900U).Data,
                Is.EqualTo(new byte[] { 7, 8 }));
        });
    }

    [Test]
    public void AgentReplacesPeerReportAndPreservesHostReport()
    {
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        DiameterMessage answer = CreateAnswer();
        DiameterLoadReport host = CreateLoadReport(
            DiameterLoadSource.Host,
            "server.example",
            40000UL);
        DiameterLoadReport downstreamPeer = CreateLoadReport(
            DiameterLoadSource.Peer,
            "downstream.example",
            30000UL);
        DiameterLoadReport localPeer = CreateLoadReport(
            DiameterLoadSource.Peer,
            "agent.example",
            50000UL);
        answer.Avps.Add(DiameterLoadProcessor.CreateDiameterLoadAvp(host));
        answer.Avps.Add(
            DiameterLoadProcessor.CreateDiameterLoadAvp(downstreamPeer));

        DiameterLoadMessageResult result =
            DiameterLoadProcessor.PrepareDiameterLoadAnswer(
                answer,
                configuration,
                DiameterAgentRole.Relay,
                CreateIdentity("agent.example"),
                localPeer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Modified, Is.True);
            Assert.That(CountLoadReports(
                result.Message,
                DiameterLoadSource.Host), Is.EqualTo(1));
            Assert.That(CountLoadReports(
                result.Message,
                DiameterLoadSource.Peer), Is.EqualTo(1));
            Assert.That(ReadLoadSourceIdentity(
                result.Message,
                DiameterLoadSource.Peer), Is.EqualTo("agent.example"));
            Assert.That(CountLoadReports(
                answer,
                DiameterLoadSource.Peer), Is.EqualTo(1));
        });
    }

    [Test]
    public void CacheEnforcesPeerAdjacencyAndServerSelectionResponsibility()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterLoadState state = new DiameterLoadState(
            CreateConfiguration(),
            clock);
        DiameterLoadReport peer = CreateLoadReport(
            DiameterLoadSource.Peer,
            "peer.example",
            10000UL);
        DiameterLoadReport host = CreateLoadReport(
            DiameterLoadSource.Host,
            "server.example",
            20000UL);

        DiameterLoadReportUpdate spoofed = state.ProcessDiameterLoadReport(
            peer,
            CreateIdentity("other.example"),
            true,
            clock.UtcTime);
        DiameterLoadReportUpdate ignoredHost =
            state.ProcessDiameterLoadReport(
                host,
                CreateIdentity("peer.example"),
                false,
                clock.UtcTime);
        DiameterLoadReportUpdate acceptedPeer =
            state.ProcessDiameterLoadReport(
                peer,
                CreateIdentity("PEER.EXAMPLE"),
                true,
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(spoofed.Updated, Is.False);
            Assert.That(spoofed.Code,
                Is.EqualTo("load-peer-source-ignored"));
            Assert.That(ignoredHost.Updated, Is.False);
            Assert.That(ignoredHost.Code,
                Is.EqualTo("load-host-report-ignored"));
            Assert.That(acceptedPeer.Updated, Is.True);
            Assert.That(state.ReadActiveDiameterLoadReports(),
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CacheSeparatesSourcesOwnsSnapshotsAndEvictsOldest()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        configuration.MaximumLoadReports = 2;
        DiameterLoadState state = new DiameterLoadState(
            configuration,
            clock);
        DiameterLoadReport host = CreateLoadReport(
            DiameterLoadSource.Host,
            "same.example",
            100UL);
        host.ExtensionAvps.Add(CreateAvp(901U, new byte[] { 1, 2 }));
        DiameterLoadReport peer = CreateLoadReport(
            DiameterLoadSource.Peer,
            "same.example",
            200UL);
        peer.ExtensionAvps.Add(CreateAvp(902U, new byte[] { 3, 4 }));
        state.ProcessDiameterLoadReport(
            host,
            CreateIdentity("upstream.example"),
            true,
            clock.UtcTime);
        state.ProcessDiameterLoadReport(
            peer,
            CreateIdentity("same.example"),
            true,
            clock.UtcTime);
        List<DiameterLoadReport> separated =
            state.ReadActiveDiameterLoadReports();
        host.SourceId.Value = "mutated.example";
        host.ExtensionAvps[0].Data[0] = 9;
        peer.SourceId.Value = "mutated.example";
        peer.ExtensionAvps[0].Data[0] = 9;
        state.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "new.example",
                300UL),
            CreateIdentity("new.example"),
            true,
            clock.UtcTime);
        List<DiameterLoadReport> reports =
            state.ReadActiveDiameterLoadReports();

        Assert.Multiple(() =>
        {
            Assert.That(separated, Has.Count.EqualTo(2));
            Assert.That(separated[0].Source,
                Is.EqualTo(DiameterLoadSource.Host));
            Assert.That(separated[1].Source,
                Is.EqualTo(DiameterLoadSource.Peer));
            Assert.That(reports, Has.Count.EqualTo(2));
            Assert.That(reports[0].SourceId.Value,
                Is.EqualTo("same.example"));
            Assert.That(reports[0].Source,
                Is.EqualTo(DiameterLoadSource.Peer));
            Assert.That(reports[0].ExtensionAvps[0].Data,
                Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(reports[1].SourceId.Value,
                Is.EqualTo("new.example"));
        });
    }

    [Test]
    public void CacheExpiryUsesMonotonicTimeWhenWallClockMovesBackward()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        configuration.LoadReportIntervalMilliseconds = 1000;
        DiameterLoadState state = new DiameterLoadState(
            configuration,
            clock);
        state.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Host,
                "server.example",
                100UL),
            CreateIdentity("upstream.example"),
            true,
            clock.UtcTime);
        clock.MonotonicMilliseconds = 1000L;
        clock.UtcTime = clock.UtcTime.AddHours(-1);

        List<DiameterLoadReport> reports =
            state.ReadActiveDiameterLoadReports();

        Assert.Multiple(() =>
        {
            Assert.That(reports, Is.Empty);
            Assert.That(state.Entries, Is.Empty);
        });
    }

    [Test]
    public void CompleteScoresCombineConfiguredWeightAndAvailableCapacity()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterLoadState state = new DiameterLoadState(
            CreateConfiguration(),
            clock);
        state.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Host,
                "one.example",
                100UL),
            CreateIdentity("upstream.example"),
            true,
            clock.UtcTime);
        state.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Host,
                "two.example",
                200UL),
            CreateIdentity("upstream.example"),
            true,
            clock.UtcTime);
        state.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "one.example",
                65000UL),
            CreateIdentity("one.example"),
            true,
            clock.UtcTime);
        List<DiameterRouteCandidate> candidates =
            new List<DiameterRouteCandidate>();
        candidates.Add(CreateCandidate("one", "one.example", 2));
        candidates.Add(CreateCandidate("two", "two.example", 3));

        DiameterLoadScoringResult result =
            state.ScoreDiameterRouteCandidates(candidates, true);
        candidates[0].PeerIdentity!.Value = "mutated.example";

        Assert.Multiple(() =>
        {
            Assert.That(result.LoadApplied, Is.True);
            Assert.That(result.TotalSelectionWeight, Is.EqualTo(800L));
            Assert.That(result.Scores[0].SelectionWeight, Is.EqualTo(200L));
            Assert.That(result.Scores[1].SelectionWeight, Is.EqualTo(600L));
            Assert.That(result.Scores[0].LoadReport!.Source,
                Is.EqualTo(DiameterLoadSource.Host));
            Assert.That(result.Scores[0].Candidate.PeerIdentity!.Value,
                Is.EqualTo("one.example"));
        });
    }

    [Test]
    public void IncompleteAndAllZeroLoadFallBackToConfiguredWeights()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterLoadState incompleteState = new DiameterLoadState(
            CreateConfiguration(),
            clock);
        incompleteState.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "one.example",
                100UL),
            CreateIdentity("one.example"),
            true,
            clock.UtcTime);
        List<DiameterRouteCandidate> candidates =
            new List<DiameterRouteCandidate>();
        candidates.Add(CreateCandidate("one", "one.example", 2));
        candidates.Add(CreateCandidate("two", "two.example", 3));
        DiameterLoadScoringResult incomplete =
            incompleteState.ScoreDiameterRouteCandidates(candidates, false);
        DiameterLoadState zeroState = new DiameterLoadState(
            CreateConfiguration(),
            clock);
        zeroState.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "one.example",
                0UL),
            CreateIdentity("one.example"),
            true,
            clock.UtcTime);
        zeroState.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "two.example",
                0UL),
            CreateIdentity("two.example"),
            true,
            clock.UtcTime);
        DiameterLoadScoringResult zero =
            zeroState.ScoreDiameterRouteCandidates(candidates, false);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.LoadApplied, Is.False);
            Assert.That(incomplete.TotalSelectionWeight, Is.EqualTo(5L));
            Assert.That(incomplete.Code,
                Is.EqualTo("load-scores-incomplete-fallback"));
            Assert.That(zero.LoadApplied, Is.False);
            Assert.That(zero.TotalSelectionWeight, Is.EqualTo(5L));
            Assert.That(zero.Code,
                Is.EqualTo("load-scores-zero-fallback"));
        });
    }

    [Test]
    public void RouteTableUsesFreshLoadAndFallsBackAfterExpiry()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadConfiguration configuration = CreateConfiguration();
        configuration.LoadReportIntervalMilliseconds = 1000;
        DiameterLoadState loadState = new DiameterLoadState(
            configuration,
            clock);
        loadState.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "one.example",
                100UL),
            CreateIdentity("one.example"),
            true,
            clock.UtcTime);
        loadState.ProcessDiameterLoadReport(
            CreateLoadReport(
                DiameterLoadSource.Peer,
                "two.example",
                900UL),
            CreateIdentity("two.example"),
            true,
            clock.UtcTime);
        SequenceRouteRandom random = new SequenceRouteRandom(100L);
        RouteTable table = new RouteTable(clock, random);
        table.ConfigureDiameterLoadRouting(loadState);
        table.AddOrReplaceStaticRoute(CreateRoute("one", "one.example"));
        table.AddOrReplaceStaticRoute(CreateRoute("two", "two.example"));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("one.example"));
        peers.Add(CreatePeer("two.example"));

        RouteSelectionResult loaded = table.SelectStaticRoute(
            CreateRouteRequest("loaded-session"),
            peers);
        clock.Advance(1000L);
        RouteSelectionResult expired = table.SelectStaticRoute(
            CreateRouteRequest("expired-session"),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("two"));
            Assert.That(expired.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("one"));
            Assert.That(random.Index, Is.EqualTo(1));
        });
    }

    private static DiameterOverloadConfiguration CreateConfiguration()
    {
        DiameterOverloadConfiguration result =
            new DiameterOverloadConfiguration();
        result.EnableLoadReports = true;
        result.LoadReportIntervalMilliseconds = 1000;
        return result;
    }

    private static DiameterLoadReport CreateLoadReport(
        DiameterLoadSource source,
        string identity,
        ulong value)
    {
        DiameterLoadReport result = new DiameterLoadReport();
        result.Source = source;
        result.SourceId = CreateIdentity(identity);
        result.LoadValue = value;
        return result;
    }

    private static DiameterMessage CreateAnswer()
    {
        DiameterMessage result = new DiameterMessage();
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        return result;
    }

    private static DiameterRouteCandidate CreateCandidate(
        string identifier,
        string peer,
        int weight)
    {
        DiameterRouteCandidate result = new DiameterRouteCandidate();
        result.Configuration.RouteIdentifier = identifier;
        result.Configuration.PeerIdentity = CreateIdentity(peer);
        result.PeerIdentity = CreateIdentity(peer);
        result.Weight = weight;
        return result;
    }

    private static DiameterRouteConfiguration CreateRoute(
        string identifier,
        string peer)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = "example";
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterRouteRequest CreateRouteRequest(string session)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = 4U;
        result.ApplicationId = 4U;
        result.DestinationRealm = CreateIdentity("example");
        result.SessionId = session;
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

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterAvp CreateAvp(uint code, byte[] data)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Data = data;
        return result;
    }

    private static List<DiameterAvp> DecodeChildren(DiameterAvp avp)
    {
        DiameterValueResult decoded =
            DiameterLoadProcessor.DecodeDiameterLoadChildren(avp);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Avps;
    }

    private static DiameterAvp FindChild(
        List<DiameterAvp> children,
        uint code)
    {
        int index = 0;
        while (index < children.Count)
        {
            DiameterAvp child = children[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(child, code))
            {
                return child;
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Expected Load child was not found.");
    }

    private static int CountLoadReports(
        DiameterMessage message,
        DiameterLoadSource source)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                    avp,
                    DiameterAvpCodes.Load) &&
                DiameterLoadProcessor.ReadDiameterLoadSource(avp) ==
                    (int)source)
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    private static string ReadLoadSourceIdentity(
        DiameterMessage message,
        DiameterLoadSource source)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (DiameterDoicProcessor.IsDiameterIetfAvp(
                    avp,
                    DiameterAvpCodes.Load) &&
                DiameterLoadProcessor.ReadDiameterLoadSource(avp) ==
                    (int)source)
            {
                return ReadTextChild(
                    DecodeChildren(avp),
                    DiameterAvpCodes.SourceId);
            }

            index = index + 1;
        }

        throw new InvalidOperationException("Expected Load report was not found.");
    }

    private static int ReadInteger32Child(
        List<DiameterAvp> children,
        uint code)
    {
        return NetworkByteOrder.ReadInt32(FindChild(children, code).Data);
    }

    private static ulong ReadUnsigned64Child(
        List<DiameterAvp> children,
        uint code)
    {
        return NetworkByteOrder.ReadUInt64(FindChild(children, code).Data);
    }

    private static string ReadTextChild(
        List<DiameterAvp> children,
        uint code)
    {
        return Encoding.UTF8.GetString(FindChild(children, code).Data);
    }
}
