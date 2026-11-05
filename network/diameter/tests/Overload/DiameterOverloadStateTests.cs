using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class DiameterOverloadStateTests
{
    [Test]
    public void CacheSeparatesHostRealmAndApplicationAndOwnsSnapshots()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 10, 1);
        DiameterOverloadReport host = CreateHostReport(
            "SERVER.EXAMPLE",
            4U,
            1UL,
            20U,
            30U);
        host.ExtensionAvps.Add(CreateAvp(900U, new byte[] { 1, 2 }));
        DiameterOverloadReport realm = CreateRealmReport(
            "example",
            4U,
            1UL,
            30U,
            30U);
        DiameterOverloadReport otherApplication = CreateHostReport(
            "server.example",
            5U,
            1UL,
            40U,
            30U);

        state.ProcessDiameterOverloadReport(host, clock.UtcTime);
        state.ProcessDiameterOverloadReport(realm, clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            otherApplication,
            clock.UtcTime);
        host.Host!.Value = "mutated.example";
        host.ExtensionAvps[0].Data[0] = 9;
        List<DiameterOverloadReport> reports =
            state.ReadActiveDiameterOverloadReports();

        Assert.Multiple(() =>
        {
            Assert.That(reports, Has.Count.EqualTo(3));
            Assert.That(reports[0].ReportType,
                Is.EqualTo(DiameterOverloadReportType.Host));
            Assert.That(reports[0].Host!.Value,
                Is.EqualTo("server.example"));
            Assert.That(reports[0].ApplicationId, Is.EqualTo(4U));
            Assert.That(reports[0].ExtensionAvps[0].Data,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(reports[1].ReportType,
                Is.EqualTo(DiameterOverloadReportType.Realm));
            Assert.That(reports[2].ApplicationId, Is.EqualTo(5U));
        });

        reports[0].Host!.Value = "changed.example";
        reports[0].ExtensionAvps[0].Data[0] = 8;
        DiameterOverloadReport reread =
            state.ReadActiveDiameterOverloadReports()[0];
        Assert.Multiple(() =>
        {
            Assert.That(reread.Host!.Value, Is.EqualTo("server.example"));
            Assert.That(reread.ExtensionAvps[0].Data,
                Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void SequenceUpdatesIgnoreRetransmissionsAndRecognizeRollover()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 10, 1);
        DiameterOverloadReport first = CreateHostReport(
            "server.example",
            4U,
            50UL,
            10U,
            30U);
        DiameterOverloadReport equal = CreateHostReport(
            "server.example",
            4U,
            50UL,
            90U,
            30U);
        DiameterOverloadReport older = CreateHostReport(
            "server.example",
            4U,
            49UL,
            80U,
            30U);
        DiameterOverloadReport newer = CreateHostReport(
            "server.example",
            4U,
            51UL,
            40U,
            30U);

        DiameterOverloadReportUpdate created =
            state.ProcessDiameterOverloadReport(first, clock.UtcTime);
        DiameterOverloadReportUpdate repeated =
            state.ProcessDiameterOverloadReport(equal, clock.UtcTime);
        DiameterOverloadReportUpdate stale =
            state.ProcessDiameterOverloadReport(older, clock.UtcTime);
        DiameterOverloadReportUpdate updated =
            state.ProcessDiameterOverloadReport(newer, clock.UtcTime);
        ulong onePercent = ulong.MaxValue / 100UL;
        DiameterOverloadReport high = CreateHostReport(
            "server.example",
            4U,
            ulong.MaxValue - onePercent,
            60U,
            30U);
        state.ProcessDiameterOverloadReport(high, clock.UtcTime);
        DiameterOverloadReport rollover = CreateHostReport(
            "server.example",
            4U,
            onePercent,
            70U,
            30U);
        DiameterOverloadReportUpdate rolled =
            state.ProcessDiameterOverloadReport(rollover, clock.UtcTime);
        DiameterOverloadReport retained =
            state.ReadActiveDiameterOverloadReports()[0];

        Assert.Multiple(() =>
        {
            Assert.That(created.Updated, Is.True);
            Assert.That(repeated.Updated, Is.False);
            Assert.That(repeated.Code, Is.EqualTo("overload-report-ignored"));
            Assert.That(repeated.Report.ReductionPercentage, Is.EqualTo(10U));
            Assert.That(stale.Updated, Is.False);
            Assert.That(updated.Updated, Is.True);
            Assert.That(rolled.Updated, Is.True);
            Assert.That(retained.SequenceNumber, Is.EqualTo(onePercent));
            Assert.That(retained.ReductionPercentage, Is.EqualTo(70U));
        });
    }

    [Test]
    public void ValidityUsesDefaultCapZeroTerminationAndMonotonicExpiry()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadConfiguration configuration =
            CreateConfiguration(10);
        configuration.DefaultReportValiditySeconds = 30U;
        configuration.MaximumReportValiditySeconds = 60U;
        DiameterOverloadState state = new DiameterOverloadState(
            configuration,
            clock,
            new SequenceOverloadRandom(1));
        DiameterOverloadReport oversized = CreateHostReport(
            "server.example",
            4U,
            1UL,
            100U,
            61U);

        DiameterOverloadReportUpdate cached =
            state.ProcessDiameterOverloadReport(oversized, clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(cached.Report.ValidityDurationSeconds,
                Is.EqualTo(30U));
            Assert.That(cached.Report.ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(30)));
            Assert.That(cached.Report.Cached, Is.True);
        });

        clock.MonotonicMilliseconds = 30001L;
        clock.UtcTime = clock.UtcTime.AddHours(-1);
        Assert.That(state.ReadActiveDiameterOverloadReports(), Is.Empty);

        clock.UtcTime = CreateTime();
        clock.MonotonicMilliseconds = 0L;
        DiameterOverloadReport active = CreateHostReport(
            "server.example",
            4U,
            2UL,
            100U,
            30U);
        state.ProcessDiameterOverloadReport(active, clock.UtcTime);
        DiameterOverloadReport ended = CreateHostReport(
            "server.example",
            4U,
            3UL,
            0U,
            0U);
        DiameterOverloadReportUpdate termination =
            state.ProcessDiameterOverloadReport(ended, clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(termination.Updated, Is.True);
            Assert.That(termination.Active, Is.False);
            Assert.That(termination.Code, Is.EqualTo("overload-report-ended"));
            Assert.That(state.ReadActiveDiameterOverloadReports(), Is.Empty);
        });
    }

    [Test]
    public void CapacityEvictsOldestScopeInDeterministicOrder()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 2, 1);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("one.example", 4U, 1UL, 10U, 30U),
            clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("two.example", 4U, 1UL, 20U, 30U),
            clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("three.example", 4U, 1UL, 30U, 30U),
            clock.UtcTime);

        List<DiameterOverloadReport> reports =
            state.ReadActiveDiameterOverloadReports();

        Assert.Multiple(() =>
        {
            Assert.That(reports, Has.Count.EqualTo(2));
            Assert.That(reports[0].Host!.Value, Is.EqualTo("two.example"));
            Assert.That(reports[1].Host!.Value, Is.EqualTo("three.example"));
        });
    }

    [Test]
    public void AdmissionScopeMatchesHostRealmAndApplicationIndependently()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 10, 100);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("server.example", 4U, 1UL, 0U, 30U),
            clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            CreateRealmReport("example", 4U, 1UL, 0U, 30U),
            clock.UtcTime);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("server.example", 5U, 1UL, 0U, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);

        request.Scope = DiameterOverloadScope.Host;
        DiameterAdmissionDecision host =
            state.EvaluateDiameterLossAdmission(request);
        request.Scope = DiameterOverloadScope.Realm;
        DiameterAdmissionDecision realm =
            state.EvaluateDiameterLossAdmission(request);
        request.Scope = DiameterOverloadScope.Application;
        DiameterAdmissionDecision application =
            state.EvaluateDiameterLossAdmission(request);
        request.Request.ApplicationId = 6U;
        request.Request.Message.ApplicationId = 6U;
        DiameterAdmissionDecision unmatched =
            state.EvaluateDiameterLossAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(host.Reports, Has.Count.EqualTo(1));
            Assert.That(host.Reports[0].ReportType,
                Is.EqualTo(DiameterOverloadReportType.Host));
            Assert.That(realm.Reports, Has.Count.EqualTo(1));
            Assert.That(realm.Reports[0].ReportType,
                Is.EqualTo(DiameterOverloadReportType.Realm));
            Assert.That(application.Reports, Has.Count.EqualTo(2));
            Assert.That(unmatched.Reports, Is.Empty);
            Assert.That(unmatched.Code, Is.EqualTo("overload-not-applicable"));
        });
    }

    [Test]
    public void LossPercentageUsesInclusiveBoundaryForThrottleAndAdmission()
    {
        OverloadTestClock clock = new OverloadTestClock();
        SequenceOverloadRandom random =
            new SequenceOverloadRandom(25, 26);
        DiameterOverloadState state = new DiameterOverloadState(
            CreateConfiguration(10),
            clock,
            random);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("server.example", 4U, 1UL, 25U, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.AllowDiversion = false;

        DiameterAdmissionDecision throttle =
            state.EvaluateDiameterLossAdmission(request);
        DiameterAdmissionDecision admit =
            state.EvaluateDiameterLossAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(throttle.Action,
                Is.EqualTo(DiameterAdmissionAction.Throttle));
            Assert.That(throttle.Admitted, Is.False);
            Assert.That(throttle.OverloadApplied, Is.True);
            Assert.That(throttle.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(throttle.RetryAfterMilliseconds, Is.EqualTo(30000));
            Assert.That(throttle.Reports, Has.Count.EqualTo(1));
            Assert.DoesNotThrow(throttle.ValidateAdmissionDecision);
            Assert.That(admit.Action,
                Is.EqualTo(DiameterAdmissionAction.Admit));
            Assert.That(admit.Admitted, Is.True);
            Assert.That(admit.Code, Is.EqualTo("overload-loss-admitted"));
            Assert.DoesNotThrow(admit.ValidateAdmissionDecision);
            Assert.That(random.ReadCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void LossAbatementPrefersDiversionAndReturnsOwnedCandidate()
    {
        OverloadTestClock clock = new OverloadTestClock();
        SequenceOverloadRandom random = new SequenceOverloadRandom(50);
        DiameterOverloadState state = new DiameterOverloadState(
            CreateConfiguration(10),
            clock,
            random);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("server.example", 4U, 1UL, 100U, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.Candidate = CreateCandidate("alternate.example");

        DiameterAdmissionDecision decision =
            state.EvaluateDiameterLossAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action,
                Is.EqualTo(DiameterAdmissionAction.Divert));
            Assert.That(decision.DiversionCandidate, Is.Not.Null);
            Assert.That(decision.DiversionCandidate,
                Is.Not.SameAs(request.Candidate));
            Assert.That(decision.DiversionCandidate!.PeerIdentity!.Value,
                Is.EqualTo("alternate.example"));
            Assert.That(random.ReadCount, Is.Zero);
            Assert.DoesNotThrow(decision.ValidateAdmissionDecision);
        });

        request.Candidate!.PeerIdentity!.Value = "mutated.example";
        Assert.That(decision.DiversionCandidate!.PeerIdentity!.Value,
            Is.EqualTo("alternate.example"));
    }

    [Test]
    public void ExpiredStateAdmitsConservativeProbe()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 10, 1);
        state.ProcessDiameterOverloadReport(
            CreateHostReport("server.example", 4U, 1UL, 100U, 1U),
            clock.UtcTime);
        clock.Advance(1000L);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.Probe = true;

        DiameterAdmissionDecision decision =
            state.EvaluateDiameterLossAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Admitted, Is.True);
            Assert.That(decision.OverloadApplied, Is.False);
            Assert.That(decision.Reports, Is.Empty);
            Assert.That(decision.Code, Is.EqualTo("overload-probe-admitted"));
        });
    }

    [Test]
    public void DisabledStateAndInvalidRfcValidityConfigurationAreRejectedSafely()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadConfiguration disabled =
            new DiameterOverloadConfiguration();
        DiameterOverloadState state = new DiameterOverloadState(
            disabled,
            clock,
            new SequenceOverloadRandom(1));
        DiameterOverloadReportUpdate ignored =
            state.ProcessDiameterOverloadReport(
                CreateHostReport("server.example", 4U, 1UL, 100U, 30U),
                clock.UtcTime);
        DiameterAdmissionDecision admitted =
            state.EvaluateDiameterLossAdmission(
                CreateAdmissionRequest(clock));
        DiameterOverloadConfiguration invalid =
            new DiameterOverloadConfiguration();
        invalid.MaximumReportValiditySeconds = 86401U;
        DiameterOverloadReport rate = CreateHostReport(
            "server.example",
            4U,
            1UL,
            0U,
            30U);
        rate.Algorithm = DiameterOverloadAlgorithm.Rate;
        rate.ReductionPercentagePresent = false;
        rate.MaximumRatePresent = true;
        rate.MaximumRate = 10U;

        Assert.Multiple(() =>
        {
            Assert.That(ignored.Updated, Is.False);
            Assert.That(ignored.Code, Is.EqualTo("overload-cache-disabled"));
            Assert.That(state.ReadActiveDiameterOverloadReports(), Is.Empty);
            Assert.That(admitted.Admitted, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(
                invalid.ValidateOverloadConfiguration);
            Assert.Throws<ArgumentException>(() =>
                state.ProcessDiameterOverloadReport(
                    CreateHostReport(
                        "server.example",
                        4U,
                        2UL,
                        10U,
                        30U),
                    default(DateTimeOffset)));
            Assert.Throws<ArgumentException>(() =>
                state.ProcessDiameterOverloadReport(rate, clock.UtcTime));
        });
    }

    private static DiameterOverloadState CreateState(
        OverloadTestClock clock,
        int capacity,
        params int[] randomValues)
    {
        return new DiameterOverloadState(
            CreateConfiguration(capacity),
            clock,
            new SequenceOverloadRandom(randomValues));
    }

    private static DiameterOverloadConfiguration CreateConfiguration(
        int capacity)
    {
        DiameterOverloadConfiguration result =
            new DiameterOverloadConfiguration();
        result.Enabled = true;
        result.MaximumOverloadReports = capacity;
        return result;
    }

    private static DiameterOverloadReport CreateHostReport(
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
        result.ValidityDurationSeconds = validity;
        result.ReductionPercentage = reduction;
        return result;
    }

    private static DiameterOverloadReport CreateRealmReport(
        string realm,
        uint applicationId,
        ulong sequence,
        uint reduction,
        uint validity)
    {
        DiameterOverloadReport result = CreateHostReport(
            "unused.example",
            applicationId,
            sequence,
            reduction,
            validity);
        result.ReportType = DiameterOverloadReportType.Realm;
        result.Host = null;
        result.Realm = CreateIdentity(realm);
        return result;
    }

    private static DiameterAdmissionRequest CreateAdmissionRequest(
        OverloadTestClock clock)
    {
        DiameterAdmissionRequest result = new DiameterAdmissionRequest();
        result.Request.Message.Version = DiameterProtocolConstants.Version;
        result.Request.Message.Flags = DiameterMessageFlags.Request;
        result.Request.Message.CommandCode = DiameterCommandCodes.CreditControl;
        result.Request.Message.ApplicationId = 4U;
        result.Request.ApplicationId = 4U;
        result.Request.DestinationRealm = CreateIdentity("example");
        result.Request.DestinationHost = CreateIdentity("server.example");
        result.Request.ReceivedAt = clock.UtcTime;
        result.Scope = DiameterOverloadScope.Host;
        result.EvaluatedAt = clock.UtcTime;
        result.CorrelationText = "overload-admission";
        return result;
    }

    private static DiameterRouteCandidate CreateCandidate(string peer)
    {
        DiameterRouteCandidate result = new DiameterRouteCandidate();
        result.PeerIdentity = CreateIdentity(peer);
        result.Configuration.RouteIdentifier = "alternate";
        result.Configuration.DefaultRoute = true;
        result.Configuration.Action = DiameterRouteAction.Relay;
        result.Configuration.AgentRole = DiameterAgentRole.Relay;
        result.Configuration.PeerIdentity = CreateIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        result.Available = true;
        result.ApplicationSupported = true;
        result.LoopFree = true;
        result.Secure = true;
        result.PolicyAccepted = true;
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

    private static DateTimeOffset CreateTime()
    {
        return new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}

internal class OverloadTestClock : IDiameterClock
{
    internal DateTimeOffset UtcTime = CreateOverloadTime();
    internal long MonotonicMilliseconds;

    public DateTimeOffset ReadUtcTime()
    {
        return UtcTime;
    }

    public long ReadMonotonicMilliseconds()
    {
        return MonotonicMilliseconds;
    }

    internal void Advance(long milliseconds)
    {
        UtcTime = UtcTime.AddMilliseconds(milliseconds);
        MonotonicMilliseconds = MonotonicMilliseconds + milliseconds;
    }

    internal static DateTimeOffset CreateOverloadTime()
    {
        return new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}

internal class SequenceOverloadRandom : IOverloadRandom
{
    internal List<int> Values = new List<int>();
    internal int ReadCount;

    internal SequenceOverloadRandom(params int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            Values.Add(values[index]);
            index = index + 1;
        }
    }

    public int ReadOverloadRandomPercentage()
    {
        if (ReadCount >= Values.Count)
        {
            throw new InvalidOperationException(
                "No overload random value remains.");
        }

        int result = Values[ReadCount];
        ReadCount = ReadCount + 1;
        return result;
    }
}
