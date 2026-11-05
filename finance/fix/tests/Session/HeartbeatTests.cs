using System;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class HeartbeatTests
{
    internal static readonly DateTime Start = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void OutboundInactivityGeneratesHeartbeatAtExactInterval()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(10);
        clock.AdvanceClock(TimeSpan.FromSeconds(9));
        timers.RecordInboundActivity();

        SessionTimerActionResult early = timers.PollTimers();
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult due = timers.PollTimers();
        SessionTimerActionResult repeated = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(early.SendHeartbeat, Is.False);
            Assert.That(early.Code, Is.EqualTo("session-timers-pending"));
            Assert.That(due.SendHeartbeat, Is.True);
            Assert.That(due.SendTestRequest, Is.False);
            Assert.That(due.EventKind, Is.EqualTo(SessionEventKind.Heartbeat));
            Assert.That(due.Code, Is.EqualTo("heartbeat-required"));
            Assert.That(timers.LastOutboundUtc, Is.EqualTo(Start.AddSeconds(10)));
            Assert.That(repeated.SendHeartbeat, Is.False);
            Assert.That(repeated.Code, Is.EqualTo("session-timers-pending"));
        });
    }

    [Test]
    public void AnyOutboundActivityPostponesHeartbeatWithoutChangingInboundDeadline()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(10);
        clock.AdvanceClock(TimeSpan.FromSeconds(9));
        SessionTimerActionResult recorded = timers.RecordOutboundActivity();
        timers.RecordInboundActivity();
        clock.AdvanceClock(TimeSpan.FromSeconds(9));

        SessionTimerActionResult early = timers.PollTimers();
        timers.RecordInboundActivity();
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult due = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(recorded.Code, Is.EqualTo("outbound-activity-recorded"));
            Assert.That(early.SendHeartbeat, Is.False);
            Assert.That(due.SendHeartbeat, Is.True);
            Assert.That(due.Code, Is.EqualTo("heartbeat-required"));
        });
    }

    [Test]
    public void InboundInactivityGeneratesTestRequestBeforeHeartbeatAndStartsTimeout()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(10);
        clock.AdvanceClock(TimeSpan.FromSeconds(9));
        timers.RecordOutboundActivity();
        clock.AdvanceClock(TimeSpan.FromSeconds(1));

        SessionTimerActionResult result = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(result.SendTestRequest, Is.True);
            Assert.That(result.SendHeartbeat, Is.False);
            Assert.That(result.Disconnect, Is.False);
            Assert.That(result.EventKind, Is.EqualTo(SessionEventKind.TestRequest));
            Assert.That(result.Code, Is.EqualTo("test-request-required"));
            Assert.That(result.TestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(result.TestRequestPending, Is.True);
            Assert.That(timers.PendingTestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(timers.TestRequestDeadlineUtc, Is.EqualTo(Start.AddSeconds(20)));
            Assert.That(timers.LastOutboundUtc, Is.EqualTo(Start.AddSeconds(10)));
        });
    }

    [Test]
    public void UnmatchedHeartbeatDoesNotClearPendingRequestAndTimeoutDisconnectsExactlyAtDeadline()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(5);
        clock.AdvanceClock(TimeSpan.FromSeconds(5));
        SessionTimerActionResult request = timers.PollTimers();
        clock.AdvanceClock(TimeSpan.FromSeconds(4));

        SessionTimerActionResult mismatch = timers.HandleHeartbeat("OTHER");
        SessionTimerActionResult early = timers.PollTimers();
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult timeout = timers.PollTimers();
        SessionTimerActionResult repeated = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(request.TestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(mismatch.Accepted, Is.True);
            Assert.That(mismatch.MatchedTestRequest, Is.False);
            Assert.That(mismatch.Code, Is.EqualTo("test-request-heartbeat-mismatch"));
            Assert.That(timers.PendingTestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(early.Disconnect, Is.False);
            Assert.That(timeout.Disconnect, Is.True);
            Assert.That(timeout.EventKind, Is.EqualTo(SessionEventKind.Disconnected));
            Assert.That(timeout.Code, Is.EqualTo("test-request-timeout"));
            Assert.That(timeout.TestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(timers.TimedOut, Is.True);
            Assert.That(timers.Running, Is.False);
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.Code, Is.EqualTo("session-timers-not-running"));
        });
    }

    [Test]
    public void MatchingHeartbeatClearsRequestAndResetsInboundInactivity()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(5);
        clock.AdvanceClock(TimeSpan.FromSeconds(5));
        SessionTimerActionResult request = timers.PollTimers();
        clock.AdvanceClock(TimeSpan.FromSeconds(4));

        SessionTimerActionResult matched = timers.HandleHeartbeat(request.TestRequestId);
        bool pendingAfterMatch = timers.TestRequestPending;
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult heartbeat = timers.PollTimers();
        clock.AdvanceClock(TimeSpan.FromSeconds(4));
        SessionTimerActionResult nextRequest = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(matched.MatchedTestRequest, Is.True);
            Assert.That(matched.Code, Is.EqualTo("test-request-heartbeat-matched"));
            Assert.That(matched.TestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(pendingAfterMatch, Is.False);
            Assert.That(heartbeat.SendHeartbeat, Is.True);
            Assert.That(nextRequest.SendTestRequest, Is.True);
            Assert.That(nextRequest.TestRequestId, Is.EqualTo("TEST-2"));
            Assert.That(nextRequest.Disconnect, Is.False);
            Assert.That(timers.TestRequestPending, Is.True);
        });
    }

    [Test]
    public void OrdinaryInboundActivityResetsInactivityButCannotSatisfyPendingTestRequest()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(5);
        clock.AdvanceClock(TimeSpan.FromSeconds(5));
        timers.PollTimers();
        clock.AdvanceClock(TimeSpan.FromSeconds(4));

        SessionTimerActionResult activity = timers.RecordInboundActivity();
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult timeout = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(activity.Code, Is.EqualTo("inbound-activity-recorded"));
            Assert.That(activity.TestRequestPending, Is.True);
            Assert.That(activity.TestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(timeout.Disconnect, Is.True);
            Assert.That(timeout.Code, Is.EqualTo("test-request-timeout"));
        });
    }

    [Test]
    public void InboundTestRequestRequiresHeartbeatWithExactIdentifierAndResetsActivity()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(10);
        clock.AdvanceClock(TimeSpan.FromSeconds(4));

        SessionTimerActionResult response = timers.HandleTestRequest("VENUE-42");
        DateTime inbound = timers.LastInboundUtc;
        DateTime outbound = timers.LastOutboundUtc;
        clock.AdvanceClock(TimeSpan.FromSeconds(9));
        SessionTimerActionResult early = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(response.Accepted, Is.True);
            Assert.That(response.SendHeartbeat, Is.True);
            Assert.That(response.SendTestRequest, Is.False);
            Assert.That(response.EventKind, Is.EqualTo(SessionEventKind.Heartbeat));
            Assert.That(response.Code, Is.EqualTo("test-request-heartbeat-required"));
            Assert.That(response.TestRequestId, Is.EqualTo("VENUE-42"));
            Assert.That(inbound, Is.EqualTo(Start.AddSeconds(4)));
            Assert.That(outbound, Is.EqualTo(Start.AddSeconds(4)));
            Assert.That(early.Code, Is.EqualTo("session-timers-pending"));
        });
    }

    [Test]
    public void MissingInboundTestRequestIdIsRejectedWithoutResettingTimers()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(10);
        clock.AdvanceClock(TimeSpan.FromSeconds(4));

        SessionTimerActionResult missing = timers.HandleTestRequest("");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.SendHeartbeat, Is.False);
            Assert.That(missing.Code, Is.EqualTo("missing-test-request-id"));
            Assert.That(timers.LastInboundUtc, Is.EqualTo(Start));
            Assert.That(timers.LastOutboundUtc, Is.EqualTo(Start));
            Assert.That(clock.ReadCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroIntervalDisablesHeartbeatAndTestRequestActions()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        SessionTimerActionResult started = timers.StartTimers(0);
        clock.AdvanceClock(TimeSpan.FromDays(365));

        SessionTimerActionResult result = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(started.Accepted, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("heartbeat-timers-disabled"));
            Assert.That(result.SendHeartbeat, Is.False);
            Assert.That(result.SendTestRequest, Is.False);
            Assert.That(result.Disconnect, Is.False);
        });
    }

    [Test]
    public void StopAndRestartClearPendingTimeoutAndResetBothActivityBaselines()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(5);
        clock.AdvanceClock(TimeSpan.FromSeconds(5));
        timers.PollTimers();

        SessionTimerActionResult stopped = timers.StopTimers();
        clock.AdvanceClock(TimeSpan.FromHours(1));
        SessionTimerActionResult stoppedPoll = timers.PollTimers();
        SessionTimerActionResult restarted = timers.StartTimers(10);
        SessionTimerActionResult restartedPoll = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.Code, Is.EqualTo("session-timers-stopped"));
            Assert.That(timers.TestRequestPending, Is.False);
            Assert.That(stoppedPoll.Accepted, Is.False);
            Assert.That(restarted.Code, Is.EqualTo("session-timers-started"));
            Assert.That(timers.LastInboundUtc, Is.EqualTo(Start.AddHours(1).AddSeconds(5)));
            Assert.That(timers.LastOutboundUtc, Is.EqualTo(Start.AddHours(1).AddSeconds(5)));
            Assert.That(restartedPoll.Code, Is.EqualTo("session-timers-pending"));
            Assert.That(timers.TimedOut, Is.False);
        });
    }

    [Test]
    public void BackwardClockMovementResetsBaselinesAndPendingDeadline()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.StartTimers(10);
        clock.AdvanceClock(TimeSpan.FromSeconds(10));
        timers.PollTimers();
        DateTime movedBack = Start.AddSeconds(3);
        clock.SetUtcNow(movedBack);

        SessionTimerActionResult reset = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(reset.Code, Is.EqualTo("session-clock-moved-backward"));
            Assert.That(reset.Disconnect, Is.False);
            Assert.That(reset.TestRequestPending, Is.True);
            Assert.That(timers.LastInboundUtc, Is.EqualTo(movedBack));
            Assert.That(timers.LastOutboundUtc, Is.EqualTo(movedBack));
            Assert.That(timers.TestRequestDeadlineUtc, Is.EqualTo(movedBack.AddSeconds(10)));
        });
    }

    [Test]
    public void TestRequestIdentifiersRemainDeterministicAcrossRolloverAndRestart()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        timers.NextTestRequestNumber = long.MaxValue;
        timers.StartTimers(1);
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult maximum = timers.PollTimers();
        timers.HandleHeartbeat(maximum.TestRequestId);
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult rolled = timers.PollTimers();
        timers.StopTimers();
        timers.StartTimers(1);
        clock.AdvanceClock(TimeSpan.FromSeconds(1));
        SessionTimerActionResult afterRestart = timers.PollTimers();

        Assert.Multiple(() =>
        {
            Assert.That(maximum.TestRequestId, Is.EqualTo("TEST-9223372036854775807"));
            Assert.That(rolled.TestRequestId, Is.EqualTo("TEST-1"));
            Assert.That(afterRestart.TestRequestId, Is.EqualTo("TEST-2"));
        });
    }

    [Test]
    public void ConstructorStartAndClockContractRejectInvalidInputsWithoutStarting()
    {
        FakeSessionClock clock = new FakeSessionClock(Start);
        SessionTimers timers = new SessionTimers(clock);
        FakeSessionClock localClock = new FakeSessionClock(DateTime.SpecifyKind(Start, DateTimeKind.Local));
        SessionTimers localTimers = new SessionTimers(localClock);
        FakeSessionClock nearMaximum = new FakeSessionClock(DateTime.SpecifyKind(DateTime.MaxValue.AddSeconds(-1), DateTimeKind.Utc));
        SessionTimers overflowing = new SessionTimers(nearMaximum);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new SessionTimers(null!));
            Assert.Throws<FixSessionException>(() => timers.StartTimers(-1));
            Assert.Throws<FixSessionException>(() => localTimers.StartTimers(10));
            Assert.Throws<FixSessionException>(() => overflowing.StartTimers(2));
            Assert.That(timers.Running, Is.False);
            Assert.That(localTimers.Running, Is.False);
            Assert.That(overflowing.Running, Is.False);
        });
    }
}
