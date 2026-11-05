using System;
using System.Collections.Generic;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ReconnectFailoverTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 18, 0, 0, DateTimeKind.Utc);

    [Test]
    public void PlannerUsesStablePriorityOrderAndCappedExponentialBackoff()
    {
        SessionEndpoint last = new SessionEndpoint { Host = "last", Priority = 5 };
        SessionEndpoint first = new SessionEndpoint { Host = "first", Priority = 0 };
        SessionEndpoint second = new SessionEndpoint { Host = "second", Priority = 0 };
        ReconnectPlanner planner = new ReconnectPlanner([last, first, second], 100, 250);

        Assert.That(planner.GetCurrentEndpoint(), Is.SameAs(first));
        planner.ScheduleNextAttempt(Now);
        Assert.Multiple(() =>
        {
            Assert.That(planner.GetCurrentEndpoint(), Is.SameAs(second));
            Assert.That(planner.IsAttemptDue(Now), Is.True);
        });
        planner.MarkAttemptStarted();
        planner.ScheduleNextAttempt(Now);
        Assert.That(planner.GetCurrentEndpoint(), Is.SameAs(last));
        planner.MarkAttemptStarted();
        planner.ScheduleNextAttempt(Now);

        Assert.Multiple(() =>
        {
            Assert.That(planner.GetCurrentEndpoint(), Is.SameAs(first));
            Assert.That(planner.IsAttemptDue(Now.AddMilliseconds(99)), Is.False);
            Assert.That(planner.IsAttemptDue(Now.AddMilliseconds(100)), Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(ReconnectPlanner.DoubleDelay(100, 250), Is.EqualTo(200));
            Assert.That(ReconnectPlanner.DoubleDelay(200, 250), Is.EqualTo(250));
            Assert.That(ReconnectPlanner.DoubleDelay(250, 250), Is.EqualTo(250));
        });
    }

    [Test]
    public void RetryableOpenRotatesEndpointsAndWaitsAfterAFullRound()
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Endpoints.Clear();
        settings.Endpoints.Add(new SessionEndpoint { Host = "backup", Port = 9002, Priority = 1 });
        settings.Endpoints.Add(new SessionEndpoint { Host = "primary", Port = 9001, Priority = 0 });
        settings.ReconnectDelayMilliseconds = 100;
        settings.MaxReconnectDelayMilliseconds = 400;
        FakeSessionClock clock = new FakeSessionClock(Now);
        ScriptedTransport transport = new ScriptedTransport();
        transport.OpenResults.Add(Failure(TransportState.Faulted, "tcp-connect-failed"));
        transport.OpenResults.Add(Failure(TransportState.Faulted, "tcp-connect-timeout"));
        transport.OpenResults.Add(Success(TransportState.Connected));
        FixSession session = CreateSession(settings, transport, clock);

        SessionPollResult first = session.StartSession();
        SessionPollResult second = session.PollSession();
        clock.AdvanceClock(TimeSpan.FromMilliseconds(99));
        SessionPollResult waiting = session.PollSession();
        clock.AdvanceClock(TimeSpan.FromMilliseconds(1));
        SessionPollResult connected = session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SessionState.Reconnecting));
            Assert.That(first.Events.Exists(item => item.Code == "transport-retry-scheduled"), Is.True);
            Assert.That(second.State, Is.EqualTo(SessionState.Reconnecting));
            Assert.That(waiting.State, Is.EqualTo(SessionState.Reconnecting));
            Assert.That(connected.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(transport.OpenedHosts, Is.EqualTo(new[] { "primary", "backup", "primary" }));
        });
    }

    [Test]
    public void ConnectAndLogonDeadlinesScheduleRetryableOutcomes()
    {
        SessionSettings connectSettings = FixSessionTests.Settings(false);
        connectSettings.ConnectTimeoutMilliseconds = 50;
        connectSettings.ReconnectDelayMilliseconds = 10;
        connectSettings.MaxReconnectDelayMilliseconds = 10;
        FakeSessionClock connectClock = new FakeSessionClock(Now);
        ScriptedTransport connectingTransport = new ScriptedTransport();
        connectingTransport.OpenResults.Add(Success(TransportState.Connecting));
        FixSession connectingSession = CreateSession(connectSettings, connectingTransport, connectClock);
        connectingSession.StartSession();
        connectClock.AdvanceClock(TimeSpan.FromMilliseconds(50));
        SessionPollResult connectTimeout = connectingSession.PollSession();

        SessionSettings logonSettings = FixSessionTests.Settings(false);
        logonSettings.LogonTimeoutMilliseconds = 75;
        logonSettings.ReconnectDelayMilliseconds = 10;
        logonSettings.MaxReconnectDelayMilliseconds = 10;
        FakeSessionClock logonClock = new FakeSessionClock(Now);
        ScriptedTransport connectedTransport = new ScriptedTransport();
        connectedTransport.OpenResults.Add(Success(TransportState.Connected));
        FixSession logonSession = CreateSession(logonSettings, connectedTransport, logonClock);
        logonSession.StartSession();
        logonClock.AdvanceClock(TimeSpan.FromMilliseconds(75));
        SessionPollResult logonTimeout = logonSession.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(connectTimeout.State, Is.EqualTo(SessionState.Reconnecting));
            Assert.That(connectTimeout.Events.Exists(item => item.Code == "connect-timeout"), Is.True);
            Assert.That(connectTimeout.Events.Exists(item => item.Code == "transport-retry-scheduled"), Is.True);
            Assert.That(logonTimeout.State, Is.EqualTo(SessionState.Reconnecting));
            Assert.That(logonTimeout.Events.Exists(item => item.Code == "logon-timeout"), Is.True);
            Assert.That(logonTimeout.Snapshot.OutboundQueueCount, Is.Zero);
        });
    }

    [Test]
    public void LogoutDeadlineAppliesEvenWhileTransportWriteIsBlocked()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        fixture.Transport.MaximumWrite = 0;
        fixture.Session.LogoutSession("blocked");
        fixture.Clock.AdvanceClock(TimeSpan.FromMilliseconds(fixture.Settings.LogoutTimeoutMilliseconds));

        SessionPollResult timeout = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(timeout.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(timeout.Events.Exists(item => item.Code == "logout-timeout"), Is.True);
            Assert.That(fixture.Transport.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InactiveAndDisabledIntervalsSuppressPendingReconnects()
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.ReconnectDelayMilliseconds = 100;
        settings.MaxReconnectDelayMilliseconds = 100;
        settings.Schedule.NonStop = false;
        settings.Schedule.TimeZoneId = "UTC";
        settings.Schedule.Windows.Add(new SessionTimeWindow
        {
            StartDay = DayOfWeek.Sunday,
            StartTime = new TimeSpan(18, 0, 0),
            EndDay = DayOfWeek.Sunday,
            EndTime = new TimeSpan(18, 1, 0)
        });
        settings.Schedule.Windows.Add(new SessionTimeWindow
        {
            StartDay = DayOfWeek.Sunday,
            StartTime = new TimeSpan(18, 1, 0),
            EndDay = DayOfWeek.Sunday,
            EndTime = new TimeSpan(23, 0, 0),
            Enabled = false
        });
        FakeSessionClock clock = new FakeSessionClock(Now);
        ScriptedTransport transport = new ScriptedTransport();
        transport.OpenResults.Add(Failure(TransportState.Faulted, "tcp-connect-failed"));
        transport.OpenResults.Add(Success(TransportState.Connected));
        FixSession session = CreateSession(settings, transport, clock);
        session.StartSession();

        clock.AdvanceClock(TimeSpan.FromMinutes(1));
        SessionPollResult ended = session.PollSession();
        clock.AdvanceClock(TimeSpan.FromMinutes(10));
        SessionPollResult inactive = session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(ended.Events.Exists(item => item.Code == "session-window-ended"), Is.True);
            Assert.That(inactive.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(transport.OpenedHosts, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UnknownFaultIsTerminalAndIsNotScheduledForRetry()
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        FakeSessionClock clock = new FakeSessionClock(Now);
        ScriptedTransport transport = new ScriptedTransport();
        transport.OpenResults.Add(Failure(TransportState.Faulted, "custom-terminal-failure"));
        FixSession session = CreateSession(settings, transport, clock);

        SessionPollResult result = session.StartSession();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Faulted));
            Assert.That(result.Events.Exists(item => item.Code == "custom-terminal-failure"), Is.True);
            Assert.That(result.Events.Exists(item => item.Code == "transport-retry-scheduled"), Is.False);
        });
    }

    internal static FixSession CreateSession(SessionSettings settings, ISessionTransport transport, FakeSessionClock clock)
    {
        FixSessionTests.EmptyDictionaryProvider provider = new FixSessionTests.EmptyDictionaryProvider();
        return new FixSession(settings, transport, new MemoryMessageStore(0), new FixSessionTests.RecordingFixLog(), clock, new FixSessionTests.RecordingThrottle(), new FixSessionTests.RecordingSessionApplication(), new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixSessionTests.AcceptingMessageValidator(), provider, new FixSessionTests.AcceptingAuthenticator());
    }

    internal static TransportWriteResult Success(TransportState state)
    {
        return new TransportWriteResult { State = state };
    }

    internal static TransportWriteResult Failure(TransportState state, string code)
    {
        return new TransportWriteResult { State = state, ErrorCode = code, ErrorMessage = code };
    }

    internal class ScriptedTransport : ISessionTransport
    {
        internal List<TransportWriteResult> OpenResults = [];
        internal List<string> OpenedHosts = [];
        internal int OpenIndex;
        internal TransportState State = TransportState.Disconnected;

        public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
        {
            OpenedHosts.Add(endpoint.Host);
            TransportWriteResult result = OpenIndex < OpenResults.Count ? OpenResults[OpenIndex] : Success(TransportState.Connecting);
            OpenIndex++;
            State = result.State;
            return result;
        }

        public TransportReadResult ReadTransport(Span<byte> destination)
        {
            return new TransportReadResult { State = State, WouldBlock = true };
        }

        public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
        {
            return new TransportWriteResult { State = State, BytesWritten = source.Length };
        }

        public TransportWriteResult CloseTransport()
        {
            State = TransportState.Closed;
            return new TransportWriteResult { State = State };
        }
    }
}
