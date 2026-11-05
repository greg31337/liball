using System;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class DiameterRateSchedulerTests
{
    [Test]
    public void StrictBucketAdmitsAtTheConfiguredInterRequestInterval()
    {
        DiameterRateBucketState bucket =
            DiameterRateScheduler.CreateDiameterRateBucket(
                CreateHostRateReport(2U, 1UL, 30U),
                0L);

        DiameterRateScheduleDecision first =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                0,
                0L);
        DiameterRateScheduler.CommitDiameterRateBucket(bucket, first);
        DiameterRateScheduleDecision immediate =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                0,
                0L);
        DiameterRateScheduleDecision early =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                0,
                499L);
        DiameterRateScheduleDecision onTime =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                0,
                500L);

        Assert.Multiple(() =>
        {
            Assert.That(first.Admitted, Is.True);
            Assert.That(immediate.Excess, Is.True);
            Assert.That(immediate.RetryAfterMilliseconds, Is.EqualTo(500));
            Assert.That(early.Excess, Is.True);
            Assert.That(early.RetryAfterMilliseconds, Is.EqualTo(1));
            Assert.That(onTime.Admitted, Is.True);
            Assert.That(bucket.BucketContentScaled, Is.EqualTo(1000M));
            Assert.That(bucket.LastConformingMonotonicMilliseconds,
                Is.Zero);
        });
    }

    [Test]
    public void ToleranceAllowsTheConfiguredBurstWithoutChangingLongTermRate()
    {
        DiameterRateBucketState bucket =
            DiameterRateScheduler.CreateDiameterRateBucket(
                CreateHostRateReport(10U, 1UL, 30U),
                0L);
        DiameterRateScheduleDecision first = EvaluateAndCommit(bucket, 200, 0L);
        DiameterRateScheduleDecision second = EvaluateAndCommit(bucket, 200, 0L);
        DiameterRateScheduleDecision third = EvaluateAndCommit(bucket, 200, 0L);
        DiameterRateScheduleDecision fourth =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                200,
                0L);
        DiameterRateScheduleDecision recovered =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                200,
                100L);

        Assert.Multiple(() =>
        {
            Assert.That(first.Admitted, Is.True);
            Assert.That(second.Admitted, Is.True);
            Assert.That(third.Admitted, Is.True);
            Assert.That(fourth.Excess, Is.True);
            Assert.That(fourth.RetryAfterMilliseconds, Is.EqualTo(100));
            Assert.That(recovered.Admitted, Is.True);
            Assert.That(bucket.BucketContentScaled, Is.EqualTo(3000M));
        });
    }

    [Test]
    public void ReportUpdatePreservesScheduleAndTerminationRemovesIt()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 0);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(2U, 1UL, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.AllowDiversion = false;

        DiameterAdmissionDecision first =
            state.EvaluateDiameterRateAdmission(request);
        DiameterAdmissionDecision excess =
            state.EvaluateDiameterRateAdmission(request);
        clock.Advance(500L);
        DiameterAdmissionDecision recovered =
            state.EvaluateDiameterRateAdmission(request);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(2U, 2UL, 30U),
            clock.UtcTime);
        DiameterAdmissionDecision preserved =
            state.EvaluateDiameterRateAdmission(request);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(2U, 3UL, 0U),
            clock.UtcTime);
        DiameterAdmissionDecision ended =
            state.EvaluateDiameterRateAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo("overload-rate-admitted"));
            Assert.That(excess.Code,
                Is.EqualTo("overload-rate-excess-throttle"));
            Assert.That(excess.RetryAfterMilliseconds, Is.EqualTo(500));
            Assert.That(recovered.Admitted, Is.True);
            Assert.That(preserved.Admitted, Is.False);
            Assert.That(preserved.RetryAfterMilliseconds, Is.EqualTo(500));
            Assert.That(ended.Code,
                Is.EqualTo("overload-rate-not-applicable"));
            Assert.That(state.RateBuckets, Is.Empty);
            Assert.DoesNotThrow(excess.ValidateAdmissionDecision);
        });
    }

    [Test]
    public void RateDrainUsesMonotonicTimeWhenWallClockMovesBackward()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 0);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(2U, 1UL, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.AllowDiversion = false;
        state.EvaluateDiameterRateAdmission(request);
        clock.MonotonicMilliseconds = 500L;
        clock.UtcTime = clock.UtcTime.AddHours(-1);
        request.EvaluatedAt = clock.UtcTime;

        DiameterAdmissionDecision decision =
            state.EvaluateDiameterRateAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Admitted, Is.True);
            Assert.That(decision.Code,
                Is.EqualTo("overload-rate-admitted"));
        });
    }

    [Test]
    public void ZeroRateAbatesUntilTheReportExpires()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 0);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(0U, 1UL, 2U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.AllowDiversion = false;

        DiameterAdmissionDecision blocked =
            state.EvaluateDiameterRateAdmission(request);
        clock.Advance(2000L);
        request.EvaluatedAt = clock.UtcTime;
        request.Probe = true;
        DiameterAdmissionDecision expired =
            state.EvaluateDiameterRateAdmission(request);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Admitted, Is.False);
            Assert.That(blocked.Action,
                Is.EqualTo(DiameterAdmissionAction.Throttle));
            Assert.That(blocked.RetryAfterMilliseconds, Is.EqualTo(2000));
            Assert.That(expired.Admitted, Is.True);
            Assert.That(expired.Code,
                Is.EqualTo("overload-rate-probe-admitted"));
            Assert.That(state.RateBuckets, Is.Empty);
        });
    }

    [Test]
    public void ExcessRequestPrefersDiversionAndOwnsTheCandidate()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 0);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(1U, 1UL, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
        request.Candidate = CreateCandidate("alternate.example");
        state.EvaluateDiameterRateAdmission(request);

        DiameterAdmissionDecision diverted =
            state.EvaluateDiameterRateAdmission(request);
        request.Candidate.PeerIdentity!.Value = "mutated.example";

        Assert.Multiple(() =>
        {
            Assert.That(diverted.Action,
                Is.EqualTo(DiameterAdmissionAction.Divert));
            Assert.That(diverted.OverloadApplied, Is.True);
            Assert.That(diverted.Code,
                Is.EqualTo("overload-rate-excess-divert"));
            Assert.That(diverted.DiversionCandidate!.PeerIdentity!.Value,
                Is.EqualTo("alternate.example"));
            Assert.DoesNotThrow(diverted.ValidateAdmissionDecision);
        });
    }

    [Test]
    public void LayeredScopesCommitOnlyWhenEveryRateAllowsTheRequest()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 0);
        state.ProcessDiameterPeerOverloadReport(
            CreatePeerRateReport("peer.example", 2U, 1UL),
            CreateIdentity("peer.example"),
            clock.UtcTime);
        DiameterAdmissionRequest peerRequest = CreateAdmissionRequest(clock);
        peerRequest.Scope = DiameterOverloadScope.Peer;
        peerRequest.Candidate = CreateCandidate("peer.example");
        peerRequest.AllowDiversion = false;
        state.EvaluateDiameterRateAdmission(peerRequest);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(1U, 1UL, 30U),
            clock.UtcTime);
        DiameterAdmissionRequest application = CreateAdmissionRequest(clock);
        application.Scope = DiameterOverloadScope.Application;
        application.Candidate = CreateCandidate("peer.example");
        application.AllowDiversion = false;

        DiameterAdmissionDecision peerBlocked =
            state.EvaluateDiameterRateAdmission(application);
        clock.Advance(500L);
        application.EvaluatedAt = clock.UtcTime;
        DiameterAdmissionDecision jointlyAdmitted =
            state.EvaluateDiameterRateAdmission(application);
        DiameterAdmissionDecision bothBlocked =
            state.EvaluateDiameterRateAdmission(application);

        Assert.Multiple(() =>
        {
            Assert.That(peerBlocked.Admitted, Is.False);
            Assert.That(peerBlocked.Reports, Has.Count.EqualTo(2));
            Assert.That(peerBlocked.RetryAfterMilliseconds, Is.EqualTo(500));
            Assert.That(jointlyAdmitted.Admitted, Is.True);
            Assert.That(bothBlocked.Admitted, Is.False);
            Assert.That(bothBlocked.RetryAfterMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void ConcurrentArrivalsCannotConsumeTheSameStrictRateSlot()
    {
        OverloadTestClock clock = new OverloadTestClock();
        DiameterOverloadState state = CreateState(clock, 0);
        state.ProcessDiameterOverloadReport(
            CreateHostRateReport(1U, 1UL, 30U),
            clock.UtcTime);
        int admitted = 0;
        int excess = 0;

        Parallel.For(0, 32, index =>
        {
            DiameterAdmissionRequest request = CreateAdmissionRequest(clock);
            request.AllowDiversion = false;
            DiameterAdmissionDecision decision =
                state.EvaluateDiameterRateAdmission(request);
            if (decision.Admitted)
            {
                Interlocked.Increment(ref admitted);
            }
            else
            {
                Interlocked.Increment(ref excess);
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(admitted, Is.EqualTo(1));
            Assert.That(excess, Is.EqualTo(31));
        });
    }

    private static DiameterRateScheduleDecision EvaluateAndCommit(
        DiameterRateBucketState bucket,
        int toleranceMilliseconds,
        long arrivalMonotonicMilliseconds)
    {
        DiameterRateScheduleDecision result =
            DiameterRateScheduler.EvaluateDiameterRateBucket(
                bucket,
                toleranceMilliseconds,
                arrivalMonotonicMilliseconds);
        DiameterRateScheduler.CommitDiameterRateBucket(bucket, result);
        return result;
    }

    private static DiameterOverloadState CreateState(
        OverloadTestClock clock,
        int toleranceMilliseconds)
    {
        DiameterOverloadConfiguration configuration =
            new DiameterOverloadConfiguration();
        configuration.Enabled = true;
        configuration.EnablePeerReports = true;
        configuration.SupportedAlgorithms.Add(
            DiameterOverloadAlgorithm.Rate);
        configuration.RateToleranceMilliseconds = toleranceMilliseconds;
        return new DiameterOverloadState(
            configuration,
            clock,
            new SequenceOverloadRandom());
    }

    private static DiameterOverloadReport CreateHostRateReport(
        uint maximumRate,
        ulong sequence,
        uint validitySeconds)
    {
        DiameterOverloadReport result = new DiameterOverloadReport();
        result.ReportType = DiameterOverloadReportType.Host;
        result.Algorithm = DiameterOverloadAlgorithm.Rate;
        result.Host = CreateIdentity("server.example");
        result.ApplicationId = 4U;
        result.SequenceNumber = sequence;
        result.ValidityDurationSeconds = validitySeconds;
        result.ReductionPercentagePresent = false;
        result.MaximumRate = maximumRate;
        result.MaximumRatePresent = true;
        return result;
    }

    private static DiameterOverloadReport CreatePeerRateReport(
        string peer,
        uint maximumRate,
        ulong sequence)
    {
        DiameterOverloadReport result = CreateHostRateReport(
            maximumRate,
            sequence,
            30U);
        result.ReportType = DiameterOverloadReportType.Peer;
        result.Host = null;
        result.SourceId = CreateIdentity(peer);
        return result;
    }

    private static DiameterAdmissionRequest CreateAdmissionRequest(
        OverloadTestClock clock)
    {
        DiameterAdmissionRequest result = new DiameterAdmissionRequest();
        result.Request.Message.Flags = DiameterMessageFlags.Request;
        result.Request.Message.CommandCode = 300U;
        result.Request.Message.ApplicationId = 4U;
        result.Request.ApplicationId = 4U;
        result.Request.DestinationRealm = CreateIdentity("example");
        result.Request.DestinationHost = CreateIdentity("server.example");
        result.Request.ReceivedAt = clock.UtcTime;
        result.Scope = DiameterOverloadScope.Host;
        result.EvaluatedAt = clock.UtcTime;
        result.CorrelationText = "rate-admission";
        return result;
    }

    private static DiameterRouteCandidate CreateCandidate(string peer)
    {
        DiameterRouteCandidate result = new DiameterRouteCandidate();
        result.PeerIdentity = CreateIdentity(peer);
        result.Configuration.PeerIdentity = CreateIdentity(peer);
        result.Configuration.RouteIdentifier = "rate-route";
        result.Configuration.DefaultRoute = true;
        result.Configuration.Action = DiameterRouteAction.Relay;
        result.Configuration.AgentRole = DiameterAgentRole.Relay;
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
}
