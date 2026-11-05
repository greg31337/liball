using System;
using System.Globalization;

namespace fix;

internal class SessionTimers
{
    internal ISessionClock Clock;
    internal bool Running;
    internal bool TimedOut;
    internal int HeartbeatIntervalSeconds;
    internal long HeartbeatIntervalTicks;
    internal DateTime LastObservedUtc;
    internal DateTime LastInboundUtc;
    internal DateTime LastOutboundUtc;
    internal bool TestRequestPending;
    internal string PendingTestRequestId = "";
    internal DateTime TestRequestDeadlineUtc;
    internal long NextTestRequestNumber = 1;

    internal SessionTimers(ISessionClock clock)
    {
        if (clock == null)
        {
            throw new FixSessionException("The session timer clock is missing.");
        }
        Clock = clock;
    }

    internal SessionTimerActionResult StartTimers(int heartbeatIntervalSeconds)
    {
        lock (this)
        {
            if (heartbeatIntervalSeconds < 0)
            {
                throw new FixSessionException("The heartbeat interval cannot be negative.");
            }
            DateTime now = ReadUtcNow();
            long intervalTicks = (long)heartbeatIntervalSeconds * TimeSpan.TicksPerSecond;
            if (intervalTicks > 0 && now.Ticks > DateTime.MaxValue.Ticks - intervalTicks)
            {
                throw new FixSessionException("The heartbeat interval exceeds the supported UTC range.");
            }
            Running = true;
            TimedOut = false;
            HeartbeatIntervalSeconds = heartbeatIntervalSeconds;
            HeartbeatIntervalTicks = intervalTicks;
            LastObservedUtc = now;
            LastInboundUtc = now;
            LastOutboundUtc = now;
            ClearPendingTestRequest();
            return CreateAction(true, SessionEventKind.None, "session-timers-started", "Session inactivity timers started.");
        }
    }

    internal SessionTimerActionResult StopTimers()
    {
        lock (this)
        {
            Running = false;
            TimedOut = false;
            HeartbeatIntervalSeconds = 0;
            HeartbeatIntervalTicks = 0;
            ClearPendingTestRequest();
            return CreateAction(true, SessionEventKind.None, "session-timers-stopped", "Session inactivity timers stopped.");
        }
    }

    internal SessionTimerActionResult PollTimers()
    {
        lock (this)
        {
            if (!Running)
            {
                return CreateAction(false, SessionEventKind.None, "session-timers-not-running", "Session inactivity timers are not running.");
            }
            DateTime now = ReadUtcNow();
            if (ObserveClock(now))
            {
                return PendingAction("session-clock-moved-backward", "The session clock moved backward and timer baselines were reset.");
            }
            if (HeartbeatIntervalTicks == 0)
            {
                return PendingAction("heartbeat-timers-disabled", "Heartbeat inactivity timers are disabled.");
            }
            if (TestRequestPending && now >= TestRequestDeadlineUtc)
            {
                Running = false;
                TimedOut = true;
                SessionTimerActionResult timeout = CreateAction(true, SessionEventKind.Disconnected, "test-request-timeout", "The peer did not return the matching TestReqID before the timeout.");
                timeout.Disconnect = true;
                timeout.TestRequestId = PendingTestRequestId;
                timeout.TestRequestPending = true;
                return timeout;
            }
            if (!TestRequestPending && IsDue(now, LastInboundUtc))
            {
                string testRequestId = CreateTestRequestId();
                TestRequestPending = true;
                PendingTestRequestId = testRequestId;
                TestRequestDeadlineUtc = AddInterval(now);
                LastOutboundUtc = now;
                SessionTimerActionResult request = CreateAction(true, SessionEventKind.TestRequest, "test-request-required", "Inbound inactivity requires a TestRequest.");
                request.SendTestRequest = true;
                request.TestRequestPending = true;
                request.TestRequestId = testRequestId;
                return request;
            }
            if (IsDue(now, LastOutboundUtc))
            {
                LastOutboundUtc = now;
                SessionTimerActionResult heartbeat = CreateAction(true, SessionEventKind.Heartbeat, "heartbeat-required", "Outbound inactivity requires a Heartbeat.");
                heartbeat.SendHeartbeat = true;
                heartbeat.TestRequestPending = TestRequestPending;
                heartbeat.TestRequestId = PendingTestRequestId;
                return heartbeat;
            }
            return PendingAction("session-timers-pending", "No session inactivity action is due.");
        }
    }

    internal SessionTimerActionResult RecordInboundActivity()
    {
        lock (this)
        {
            if (!Running)
            {
                return CreateAction(false, SessionEventKind.None, "session-timers-not-running", "Session inactivity timers are not running.");
            }
            DateTime now = ReadUtcNow();
            bool movedBackward = ObserveClock(now);
            LastInboundUtc = now;
            string code = movedBackward ? "inbound-activity-clock-reset" : "inbound-activity-recorded";
            string text = movedBackward ? "Inbound activity and the backward clock adjustment reset timer baselines." : "Inbound session activity was recorded.";
            return PendingAction(code, text);
        }
    }

    internal SessionTimerActionResult RecordOutboundActivity()
    {
        lock (this)
        {
            if (!Running)
            {
                return CreateAction(false, SessionEventKind.None, "session-timers-not-running", "Session inactivity timers are not running.");
            }
            DateTime now = ReadUtcNow();
            bool movedBackward = ObserveClock(now);
            LastOutboundUtc = now;
            string code = movedBackward ? "outbound-activity-clock-reset" : "outbound-activity-recorded";
            string text = movedBackward ? "Outbound activity and the backward clock adjustment reset timer baselines." : "Outbound session activity was recorded.";
            return PendingAction(code, text);
        }
    }

    internal SessionTimerActionResult HandleHeartbeat(string testRequestId)
    {
        lock (this)
        {
            if (!Running)
            {
                return CreateAction(false, SessionEventKind.Heartbeat, "session-timers-not-running", "Session inactivity timers are not running.");
            }
            DateTime now = ReadUtcNow();
            ObserveClock(now);
            LastInboundUtc = now;
            string actualId = testRequestId == null ? "" : testRequestId;
            if (TestRequestPending)
            {
                if (string.Equals(PendingTestRequestId, actualId, StringComparison.Ordinal))
                {
                    string matchedId = PendingTestRequestId;
                    ClearPendingTestRequest();
                    SessionTimerActionResult matched = CreateAction(true, SessionEventKind.Heartbeat, "test-request-heartbeat-matched", "The Heartbeat matched the pending TestReqID.");
                    matched.MatchedTestRequest = true;
                    matched.TestRequestId = matchedId;
                    return matched;
                }
                SessionTimerActionResult mismatch = CreateAction(true, SessionEventKind.Heartbeat, "test-request-heartbeat-mismatch", "The Heartbeat did not match the pending TestReqID.");
                mismatch.TestRequestPending = true;
                mismatch.TestRequestId = actualId;
                return mismatch;
            }
            SessionTimerActionResult received = CreateAction(true, SessionEventKind.Heartbeat, "heartbeat-received", "A Heartbeat was received.");
            received.TestRequestId = actualId;
            return received;
        }
    }

    internal SessionTimerActionResult HandleTestRequest(string testRequestId)
    {
        lock (this)
        {
            if (!Running)
            {
                return CreateAction(false, SessionEventKind.TestRequest, "session-timers-not-running", "Session inactivity timers are not running.");
            }
            string actualId = testRequestId == null ? "" : testRequestId;
            if (actualId.Length == 0)
            {
                return CreateAction(false, SessionEventKind.TestRequest, "missing-test-request-id", "A TestRequest must contain TestReqID.");
            }
            DateTime now = ReadUtcNow();
            ObserveClock(now);
            LastInboundUtc = now;
            LastOutboundUtc = now;
            SessionTimerActionResult response = CreateAction(true, SessionEventKind.Heartbeat, "test-request-heartbeat-required", "The inbound TestRequest requires a matching Heartbeat.");
            response.SendHeartbeat = true;
            response.TestRequestId = actualId;
            response.TestRequestPending = TestRequestPending;
            return response;
        }
    }

    internal bool ObserveClock(DateTime now)
    {
        bool movedBackward = now < LastObservedUtc;
        LastObservedUtc = now;
        if (movedBackward)
        {
            LastInboundUtc = now;
            LastOutboundUtc = now;
            if (TestRequestPending)
            {
                TestRequestDeadlineUtc = AddInterval(now);
            }
        }
        return movedBackward;
    }

    internal bool IsDue(DateTime now, DateTime lastActivity)
    {
        return now.Ticks - lastActivity.Ticks >= HeartbeatIntervalTicks;
    }

    internal DateTime AddInterval(DateTime start)
    {
        if (start.Ticks > DateTime.MaxValue.Ticks - HeartbeatIntervalTicks)
        {
            throw new FixSessionException("The session timer deadline exceeds the supported UTC range.");
        }
        return start.AddTicks(HeartbeatIntervalTicks);
    }

    internal string CreateTestRequestId()
    {
        string value = "TEST-" + NextTestRequestNumber.ToString(CultureInfo.InvariantCulture);
        if (NextTestRequestNumber == long.MaxValue)
        {
            NextTestRequestNumber = 1;
        }
        else
        {
            NextTestRequestNumber++;
        }
        return value;
    }

    internal void ClearPendingTestRequest()
    {
        TestRequestPending = false;
        PendingTestRequestId = "";
        TestRequestDeadlineUtc = default(DateTime);
    }

    internal DateTime ReadUtcNow()
    {
        DateTime now = Clock.GetUtcNow();
        if (now.Kind != DateTimeKind.Utc)
        {
            throw new FixSessionException("The session clock returned a non-UTC value.");
        }
        return now;
    }

    internal SessionTimerActionResult PendingAction(string code, string text)
    {
        SessionTimerActionResult result = CreateAction(true, SessionEventKind.None, code, text);
        result.TestRequestPending = TestRequestPending;
        result.TestRequestId = PendingTestRequestId;
        return result;
    }

    internal static SessionTimerActionResult CreateAction(bool accepted, SessionEventKind eventKind, string code, string text)
    {
        SessionTimerActionResult result = new SessionTimerActionResult();
        result.Accepted = accepted;
        result.EventKind = eventKind;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
