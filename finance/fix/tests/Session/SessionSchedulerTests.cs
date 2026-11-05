using System;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SessionSchedulerTests
{
    [Test]
    public void NonStopScheduleRemainsActiveWithoutSyntheticBoundaries()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 8, 31, 8, 0));
        SessionScheduler scheduler = new SessionScheduler(new SessionSchedule(), clock);

        SessionScheduleEvaluation first = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromDays(10));
        SessionScheduleEvaluation later = scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(first.Active, Is.True);
            Assert.That(first.BoundaryStarted, Is.False);
            Assert.That(later.Active, Is.True);
            Assert.That(later.BoundaryStarted, Is.False);
            Assert.That(later.BoundaryEnded, Is.False);
        });
    }

    [Test]
    public void DailyWindowUsesHalfOpenStartAndEndBoundaries()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 8, 31, 8, 59));
        SessionSchedule schedule = Scheduled("UTC", Window(DayOfWeek.Monday, 9, 0, DayOfWeek.Monday, 17, 0, true));
        SessionScheduler scheduler = new SessionScheduler(schedule, clock);

        SessionScheduleEvaluation before = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromMinutes(1));
        SessionScheduleEvaluation start = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromHours(8));
        SessionScheduleEvaluation end = scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(before.Active, Is.False);
            Assert.That(start.Active, Is.True);
            Assert.That(start.BoundaryStarted, Is.True);
            Assert.That(end.Active, Is.False);
            Assert.That(end.BoundaryEnded, Is.True);
        });
    }

    [Test]
    public void SameDayOvernightWindowContinuesIntoFollowingDay()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 8, 31, 21, 59));
        SessionSchedule schedule = Scheduled("UTC", Window(DayOfWeek.Monday, 22, 0, DayOfWeek.Monday, 5, 0, true));
        SessionScheduler scheduler = new SessionScheduler(schedule, clock);

        Assert.That(scheduler.EvaluateSchedule().Active, Is.False);
        clock.AdvanceClock(TimeSpan.FromMinutes(1));
        Assert.That(scheduler.EvaluateSchedule().BoundaryStarted, Is.True);
        clock.AdvanceClock(TimeSpan.FromHours(7));
        SessionScheduleEvaluation ended = scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(ended.LocalNow.DayOfWeek, Is.EqualTo(DayOfWeek.Tuesday));
            Assert.That(ended.Active, Is.False);
            Assert.That(ended.BoundaryEnded, Is.True);
        });
    }

    [Test]
    public void WeeklyWindowWrapsAcrossSundayAndDisabledWindowsAreIgnored()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 9, 6, 12, 0));
        SessionSchedule schedule = Scheduled("UTC", Window(DayOfWeek.Friday, 22, 0, DayOfWeek.Monday, 5, 0, true));
        schedule.Windows.Add(Window(DayOfWeek.Sunday, 0, 0, DayOfWeek.Sunday, 23, 59, false));
        SessionScheduler scheduler = new SessionScheduler(schedule, clock);

        SessionScheduleEvaluation sunday = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromHours(17));
        SessionScheduleEvaluation mondayEnd = scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(sunday.LocalNow.DayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
            Assert.That(sunday.Active, Is.True);
            Assert.That(mondayEnd.LocalNow.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(mondayEnd.LocalNow.TimeOfDay, Is.EqualTo(new TimeSpan(5, 0, 0)));
            Assert.That(mondayEnd.BoundaryEnded, Is.True);
        });
    }

    [Test]
    public void TimeZoneConversionTracksSpringDaylightSavingTransition()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 3, 8, 6, 45));
        SessionSchedule schedule = Scheduled("America/New_York", Window(DayOfWeek.Sunday, 1, 30, DayOfWeek.Sunday, 3, 30, true));
        SessionScheduler scheduler = new SessionScheduler(schedule, clock);

        SessionScheduleEvaluation beforeJump = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromMinutes(30));
        SessionScheduleEvaluation afterJump = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromMinutes(15));
        SessionScheduleEvaluation ended = scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(beforeJump.LocalNow.Hour, Is.EqualTo(1));
            Assert.That(beforeJump.Active, Is.True);
            Assert.That(afterJump.LocalNow.Hour, Is.EqualTo(3));
            Assert.That(afterJump.Active, Is.True);
            Assert.That(ended.BoundaryEnded, Is.True);
        });
    }

    [Test]
    public void TimeZoneConversionKeepsBothOccurrencesOfRepeatedFallHourActive()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 11, 1, 5, 30));
        SessionSchedule schedule = Scheduled("America/New_York", Window(DayOfWeek.Sunday, 1, 15, DayOfWeek.Sunday, 1, 45, true));
        SessionScheduler scheduler = new SessionScheduler(schedule, clock);

        SessionScheduleEvaluation daylightOccurrence = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromHours(1));
        SessionScheduleEvaluation standardOccurrence = scheduler.EvaluateSchedule();
        clock.AdvanceClock(TimeSpan.FromMinutes(15));
        SessionScheduleEvaluation ended = scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(daylightOccurrence.LocalNow.TimeOfDay, Is.EqualTo(new TimeSpan(1, 30, 0)));
            Assert.That(daylightOccurrence.Active, Is.True);
            Assert.That(standardOccurrence.LocalNow.TimeOfDay, Is.EqualTo(new TimeSpan(1, 30, 0)));
            Assert.That(standardOccurrence.Active, Is.True);
            Assert.That(standardOccurrence.BoundaryStarted, Is.False);
            Assert.That(standardOccurrence.BoundaryEnded, Is.False);
            Assert.That(ended.LocalNow.TimeOfDay, Is.EqualTo(new TimeSpan(1, 45, 0)));
            Assert.That(ended.BoundaryEnded, Is.True);
        });
    }

    [Test]
    public void InvalidScheduleInputsFailBeforeSessionMutation()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 8, 31, 8, 0));
        SessionSchedule noWindows = new SessionSchedule { NonStop = false, TimeZoneId = "UTC" };
        SessionSchedule badTime = Scheduled("UTC", Window(DayOfWeek.Monday, 24, 0, DayOfWeek.Monday, 17, 0, true));
        SessionSchedule badZone = Scheduled("Not/A_Real_Zone", Window(DayOfWeek.Monday, 9, 0, DayOfWeek.Monday, 17, 0, true));

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new SessionScheduler(noWindows, clock));
            Assert.Throws<FixSessionException>(() => new SessionScheduler(badTime, clock));
            Assert.Throws<FixSessionException>(() => new SessionScheduler(badZone, clock));
            Assert.Throws<FixSessionException>(() => new SessionScheduler(null!, clock));
        });
    }

    [Test]
    public void InactiveSessionWaitsForBoundaryThenConnectsAndResetsSequenceState()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 8, 31, 8, 59));
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Schedule = Scheduled("UTC", Window(DayOfWeek.Monday, 9, 0, DayOfWeek.Monday, 17, 0, true));
        settings.ResetMode = SessionResetMode.OnSessionBoundary;
        FixSessionTests.FakeSessionTransport transport = new FixSessionTests.FakeSessionTransport();
        MemoryMessageStore store = new MemoryMessageStore(0);
        Assert.That(store.SaveSequenceState(7, 9).Success, Is.True);
        FixSession session = CreateSession(settings, clock, transport, store);

        SessionPollResult waiting = session.StartSession();
        clock.AdvanceClock(TimeSpan.FromMinutes(1));
        SessionPollResult opened = session.PollSession();
        StoreRangeResult reset = store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(waiting.Events.Exists(item => item.Code == "session-schedule-inactive"), Is.True);
            Assert.That(transport.OpenCount, Is.EqualTo(1));
            Assert.That(opened.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(opened.Events.Exists(item => item.Code == "session-window-started"), Is.True);
            Assert.That(opened.Events.Exists(item => item.Code == "boundary-sequence-reset"), Is.True);
            Assert.That(reset.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(reset.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(opened.Snapshot.OutboundQueueCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void WindowEndDisconnectsAndAppliesBoundaryResetOnce()
    {
        FakeSessionClock clock = new FakeSessionClock(Utc(2026, 8, 31, 9, 0));
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Schedule = Scheduled("UTC", Window(DayOfWeek.Monday, 9, 0, DayOfWeek.Monday, 17, 0, true));
        settings.ResetMode = SessionResetMode.OnSessionBoundary | SessionResetMode.OnDisconnect;
        FixSessionTests.FakeSessionTransport transport = new FixSessionTests.FakeSessionTransport();
        MemoryMessageStore store = new MemoryMessageStore(0);
        FixSession session = CreateSession(settings, clock, transport, store);
        session.StartSession();
        session.PollSession();

        clock.AdvanceClock(TimeSpan.FromHours(8));
        SessionPollResult ended = session.PollSession();
        StoreRangeResult state = store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(ended.Events.Exists(item => item.Code == "session-window-ended"), Is.True);
            Assert.That(ended.Events.FindAll(item => item.Code == "boundary-sequence-reset"), Has.Count.EqualTo(1));
            Assert.That(ended.Events.Exists(item => item.Code == "disconnect-sequence-reset"), Is.False);
            Assert.That(transport.CloseCount, Is.EqualTo(1));
            Assert.That(state.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(state.NextTargetSeqNum, Is.EqualTo(1));
        });
    }

    [Test]
    public void LogonLogoutAndDisconnectResetModesApplyAtTheirOwnBoundaries()
    {
        SessionSettings logonSettings = FixSessionTests.Settings(false);
        logonSettings.ResetMode = SessionResetMode.OnLogon;
        FakeSessionClock logonClock = new FakeSessionClock(Utc(2026, 8, 31, 10, 0));
        FixSessionTests.FakeSessionTransport logonTransport = new FixSessionTests.FakeSessionTransport();
        MemoryMessageStore logonStore = new MemoryMessageStore(0);
        Assert.That(logonStore.SaveSequenceState(7, 9).Success, Is.True);
        FixSession logonSession = CreateSession(logonSettings, logonClock, logonTransport, logonStore);

        SessionPollResult logon = logonSession.StartSession();
        StoreRangeResult logonState = logonStore.RefreshStore();

        SessionSettings disconnectSettings = FixSessionTests.Settings(false);
        disconnectSettings.ResetMode = SessionResetMode.OnDisconnect;
        FakeSessionClock disconnectClock = new FakeSessionClock(Utc(2026, 8, 31, 10, 0));
        FixSessionTests.FakeSessionTransport disconnectTransport = new FixSessionTests.FakeSessionTransport();
        MemoryMessageStore disconnectStore = new MemoryMessageStore(0);
        Assert.That(disconnectStore.SaveSequenceState(4, 6).Success, Is.True);
        FixSession disconnectSession = CreateSession(disconnectSettings, disconnectClock, disconnectTransport, disconnectStore);
        disconnectSession.StartSession();
        SessionPollResult stopped = disconnectSession.StopSession();
        StoreRangeResult disconnectState = disconnectStore.RefreshStore();

        FixSessionTests.SessionFixture logoutFixture = new FixSessionTests.SessionFixture(false);
        logoutFixture.Settings.ResetMode = SessionResetMode.OnLogout;
        logoutFixture.Activate();
        logoutFixture.Session.LogoutSession("reset");
        logoutFixture.Session.PollSession();
        logoutFixture.Transport.AddInbound(logoutFixture.Encode(logoutFixture.PeerFactory.CreateLogoutMessage(2, "reset")));
        SessionPollResult loggedOut = logoutFixture.Session.PollSession();
        StoreRangeResult logoutState = logoutFixture.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(logon.Events.Exists(item => item.Code == "logon-sequence-reset"), Is.True);
            Assert.That(logonState.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(logonState.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(stopped.Events.Exists(item => item.Code == "disconnect-sequence-reset"), Is.True);
            Assert.That(disconnectState.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(disconnectState.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(loggedOut.Events.Exists(item => item.Code == "logout-sequence-reset"), Is.True);
            Assert.That(logoutState.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(logoutState.NextTargetSeqNum, Is.EqualTo(1));
        });
    }

    internal static SessionSchedule Scheduled(string timeZoneId, SessionTimeWindow window)
    {
        SessionSchedule schedule = new SessionSchedule { NonStop = false, TimeZoneId = timeZoneId };
        schedule.Windows.Add(window);
        return schedule;
    }

    internal static SessionTimeWindow Window(DayOfWeek startDay, int startHour, int startMinute, DayOfWeek endDay, int endHour, int endMinute, bool enabled)
    {
        return new SessionTimeWindow
        {
            StartDay = startDay,
            StartTime = new TimeSpan(startHour, startMinute, 0),
            EndDay = endDay,
            EndTime = new TimeSpan(endHour, endMinute, 0),
            Enabled = enabled
        };
    }

    internal static DateTime Utc(int year, int month, int day, int hour, int minute)
    {
        return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    }

    internal static FixSession CreateSession(SessionSettings settings, FakeSessionClock clock, FixSessionTests.FakeSessionTransport transport, MemoryMessageStore store)
    {
        FixSessionTests.EmptyDictionaryProvider provider = new FixSessionTests.EmptyDictionaryProvider();
        return new FixSession(settings, transport, store, new FixSessionTests.RecordingFixLog(), clock, new FixSessionTests.RecordingThrottle(), new FixSessionTests.RecordingSessionApplication(), new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixSessionTests.AcceptingMessageValidator(), provider, new FixSessionTests.AcceptingAuthenticator());
    }
}
