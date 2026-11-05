using System;
using System.Collections.Generic;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class MultiSessionIntegrationTests
{
    private static readonly DateTime Start = new DateTime(2026, 8, 30, 18, 0, 0, DateTimeKind.Utc);

    [Test]
    public void RemovingAndAddingLiveSessionsDoesNotInterruptRemainingSessions()
    {
        EngineFixture engine = new EngineFixture();
        ProfileSession first = engine.CreateProfileSession("FIRST", false);
        ProfileSession second = engine.CreateProfileSession("SECOND", true);
        first.Activate();
        second.Activate();
        engine.Register(first);
        engine.Register(second);

        SessionPollResult removed = engine.Engine.RemoveSession(first.Settings.Id);
        ProfileSession third = engine.CreateProfileSession("THIRD", false);
        third.Activate();
        engine.Register(third);
        second.QueueInbound(second.PeerFactory.CreateHeartbeatMessage(2, ""));
        third.QueueInbound(third.PeerFactory.CreateHeartbeatMessage(2, ""));

        SessionPollResult aggregate = engine.Engine.PollSessions();

        Assert.Multiple(() =>
        {
            Assert.That(removed.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(first.Transport.CloseCount, Is.EqualTo(1));
            Assert.That(engine.Engine.Sessions, Has.Count.EqualTo(2));
            Assert.That(engine.Engine.Sessions.ConvertAll(item => item.Id.Qualifier), Is.EqualTo(new[] { "SECOND", "THIRD" }));
            Assert.That(engine.Engine.ResolveSessionSnapshot(second.Settings.Id).State, Is.EqualTo(SessionState.Active));
            Assert.That(engine.Engine.ResolveSessionSnapshot(second.Settings.Id).NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(engine.Engine.ResolveSessionSnapshot(third.Settings.Id).NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(aggregate.Events.FindAll(item => item.Code == "heartbeat-processed"), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Fix4AndFixtSessionsExchangeTrafficThroughOneEngine()
    {
        EngineFixture engine = new EngineFixture();
        ProfileSession fix4 = engine.CreateProfileSession("FIX4", false);
        ProfileSession fixt = engine.CreateProfileSession("FIXT", true);
        fix4.Activate();
        fixt.Activate();
        engine.Register(fix4);
        engine.Register(fixt);
        fix4.QueueInbound(fix4.PeerFactory.CreateHeartbeatMessage(2, ""));
        fixt.QueueInbound(fixt.PeerFactory.CreateStandardMessage("D", 2));

        SessionPollResult aggregate = engine.Engine.PollSessions();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Engine.ResolveSessionSnapshot(fix4.Settings.Id).NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(engine.Engine.ResolveSessionSnapshot(fixt.Settings.Id).NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(fix4.Session.Core.TransportDictionary.Identity.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(fixt.Session.Core.TransportDictionary.Identity.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(fixt.Session.Core.ApplicationDictionary.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(engine.Application.ApplicationContexts, Has.Count.EqualTo(1));
            Assert.That(engine.Application.ApplicationContexts[0].Id.Qualifier, Is.EqualTo("FIXT"));
            Assert.That(aggregate.Events.Exists(item => item.Id.Qualifier == "FIX4" && item.Code == "heartbeat-processed"), Is.True);
            Assert.That(aggregate.Events.Exists(item => item.Id.Qualifier == "FIXT" && item.Message != null && item.Message.MsgType == "D"), Is.True);
        });
    }

    [Test]
    public void StoresDictionariesAndSchedulesRemainSessionLocal()
    {
        EngineFixture engine = new EngineFixture();
        SessionSchedule utcSchedule = WindowSchedule("UTC", new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0));
        SessionSchedule newYorkSchedule = WindowSchedule("America/New_York", new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0));
        ProfileSession fix4 = engine.CreateProfileSession("UTC", false, utcSchedule);
        ProfileSession fixt = engine.CreateProfileSession("NEW-YORK", true, newYorkSchedule);
        engine.Register(fix4);
        engine.Register(fixt);

        Assert.That(fix4.Store.SaveSequenceState(8, 9).Success, Is.True);
        StoreRangeResult firstStore = fix4.Store.RefreshStore();
        StoreRangeResult secondStore = fixt.Store.RefreshStore();
        engine.Clock.SetUtcNow(new DateTime(2026, 8, 31, 9, 30, 0, DateTimeKind.Utc));
        SessionScheduleEvaluation utcMorning = fix4.Session.Core.Scheduler.EvaluateSchedule();
        SessionScheduleEvaluation newYorkEarly = fixt.Session.Core.Scheduler.EvaluateSchedule();
        engine.Clock.SetUtcNow(new DateTime(2026, 8, 31, 13, 30, 0, DateTimeKind.Utc));
        SessionScheduleEvaluation utcAfternoon = fix4.Session.Core.Scheduler.EvaluateSchedule();
        SessionScheduleEvaluation newYorkMorning = fixt.Session.Core.Scheduler.EvaluateSchedule();

        Assert.Multiple(() =>
        {
            Assert.That(fix4.Store, Is.Not.SameAs(fixt.Store));
            Assert.That(firstStore.NextSenderSeqNum, Is.EqualTo(8));
            Assert.That(firstStore.NextTargetSeqNum, Is.EqualTo(9));
            Assert.That(secondStore.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(secondStore.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(fix4.Session.Core.TransportDictionary, Is.Not.SameAs(fixt.Session.Core.TransportDictionary));
            Assert.That(fix4.Session.Core.ApplicationDictionary, Is.SameAs(fix4.Session.Core.TransportDictionary));
            Assert.That(fixt.Session.Core.ApplicationDictionary, Is.Not.SameAs(fixt.Session.Core.TransportDictionary));
            Assert.That(utcMorning.Active, Is.True);
            Assert.That(newYorkEarly.Active, Is.False);
            Assert.That(utcAfternoon.Active, Is.False);
            Assert.That(newYorkMorning.Active, Is.True);
        });
    }

    [Test]
    public void OneFaultedSessionDoesNotPreventOtherSessionsFromProgressing()
    {
        EngineFixture engine = new EngineFixture();
        ProfileSession failing = engine.CreateProfileSession("FAILING", false);
        ProfileSession healthy = engine.CreateProfileSession("HEALTHY", true);
        failing.Activate();
        healthy.Activate();
        engine.Register(failing);
        engine.Register(healthy);
        failing.QueueInbound(failing.PeerFactory.CreateLogonMessage(2, 1, false));
        healthy.QueueInbound(healthy.PeerFactory.CreateHeartbeatMessage(2, ""));

        SessionPollResult firstPoll = engine.Engine.PollSessions();
        healthy.QueueInbound(healthy.PeerFactory.CreateHeartbeatMessage(3, ""));
        SessionPollResult secondPoll = engine.Engine.PollSessions();

        Assert.Multiple(() =>
        {
            Assert.That(engine.Engine.ResolveSessionSnapshot(failing.Settings.Id).State, Is.EqualTo(SessionState.Faulted));
            Assert.That(failing.Transport.CloseCount, Is.EqualTo(1));
            Assert.That(engine.Engine.ResolveSessionSnapshot(healthy.Settings.Id).State, Is.EqualTo(SessionState.Active));
            Assert.That(engine.Engine.ResolveSessionSnapshot(healthy.Settings.Id).NextTargetSeqNum, Is.EqualTo(4));
            Assert.That(firstPoll.Events.Exists(item => item.Id.Qualifier == "FAILING" && item.Code == "unexpected-logon"), Is.True);
            Assert.That(firstPoll.Events.Exists(item => item.Id.Qualifier == "HEALTHY" && item.Code == "heartbeat-processed"), Is.True);
            Assert.That(secondPoll.Events.Exists(item => item.Id.Qualifier == "HEALTHY" && item.SequenceNumber == 3), Is.True);
        });
    }

    [Test]
    public void MixedProfileSessionsRecoverIndependentGapsSimultaneously()
    {
        EngineFixture engine = new EngineFixture();
        ProfileSession fix4 = engine.CreateProfileSession("RECOVER-FIX4", false);
        ProfileSession fixt = engine.CreateProfileSession("RECOVER-FIXT", true);
        fix4.Activate();
        fixt.Activate();
        engine.Register(fix4);
        engine.Register(fixt);
        fix4.QueueInbound(fix4.PeerFactory.CreateStandardMessage("D", 4));
        fixt.QueueInbound(fixt.PeerFactory.CreateStandardMessage("D", 4));

        SessionPollResult gaps = engine.Engine.PollSessions();
        SessionSnapshot fix4Gap = engine.Engine.ResolveSessionSnapshot(fix4.Settings.Id);
        SessionSnapshot fixtGap = engine.Engine.ResolveSessionSnapshot(fixt.Settings.Id);
        fix4.FlushOutbound();
        fixt.FlushOutbound();
        fix4.QueueInbound(fix4.PeerFactory.CreateSequenceResetMessage(2, 4, true));
        fixt.QueueInbound(fixt.PeerFactory.CreateSequenceResetMessage(2, 4, true));

        SessionPollResult recovered = engine.Engine.PollSessions();

        Assert.Multiple(() =>
        {
            Assert.That(fix4Gap.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(fixtGap.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(fix4Gap.InboundQueueCount, Is.EqualTo(1));
            Assert.That(fixtGap.InboundQueueCount, Is.EqualTo(1));
            Assert.That(gaps.Events.FindAll(item => item.Kind == SessionEventKind.SequenceGap), Has.Count.EqualTo(2));
            Assert.That(engine.Engine.ResolveSessionSnapshot(fix4.Settings.Id).State, Is.EqualTo(SessionState.Active));
            Assert.That(engine.Engine.ResolveSessionSnapshot(fixt.Settings.Id).State, Is.EqualTo(SessionState.Active));
            Assert.That(engine.Engine.ResolveSessionSnapshot(fix4.Settings.Id).NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(engine.Engine.ResolveSessionSnapshot(fixt.Settings.Id).NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(recovered.Events.FindAll(item => item.Kind == SessionEventKind.RecoveryCompleted), Has.Count.EqualTo(2));
            Assert.That(CountApplicationContexts(engine.Application.ApplicationContexts, "RECOVER-FIX4"), Is.EqualTo(1));
            Assert.That(CountApplicationContexts(engine.Application.ApplicationContexts, "RECOVER-FIXT"), Is.EqualTo(1));
        });
    }

    private static int CountApplicationContexts(List<ApplicationMessageContext> contexts, string qualifier)
    {
        int count = 0;
        for (int index = 0; index < contexts.Count; index++)
        {
            if (contexts[index].Id.Qualifier == qualifier) count++;
        }
        return count;
    }

    private static SessionSchedule WindowSchedule(string timeZoneId, TimeSpan start, TimeSpan end)
    {
        SessionSchedule schedule = new SessionSchedule();
        schedule.NonStop = false;
        schedule.TimeZoneId = timeZoneId;
        schedule.Windows.Add(new SessionTimeWindow
        {
            StartDay = DayOfWeek.Monday,
            StartTime = start,
            EndDay = DayOfWeek.Monday,
            EndTime = end
        });
        return schedule;
    }

    private class EngineFixture
    {
        internal FakeSessionClock Clock = new FakeSessionClock(Start);
        internal FixSessionTests.RecordingSessionApplication Application = new FixSessionTests.RecordingSessionApplication();
        internal EmbeddedDictionaryProvider Provider = new EmbeddedDictionaryProvider(null);
        internal FixSessionEngine Engine;

        internal EngineFixture()
        {
            Engine = new FixSessionEngine(Clock, Application, Provider, new FixSessionTests.AcceptingAuthenticator());
        }

        internal ProfileSession CreateProfileSession(string qualifier, bool fixt)
        {
            return new ProfileSession(Clock, Application, Provider, qualifier, fixt, null);
        }

        internal ProfileSession CreateProfileSession(string qualifier, bool fixt, SessionSchedule schedule)
        {
            return new ProfileSession(Clock, Application, Provider, qualifier, fixt, schedule);
        }

        internal void Register(ProfileSession fixture)
        {
            Engine.Sessions.Add(new FixSessionEngineEntry(FixSessionEngine.CloneEngineId(fixture.Settings.Id), fixture.Session));
        }
    }

    private class ProfileSession
    {
        internal SessionSettings Settings;
        internal FakeSessionClock Clock;
        internal FixSessionTests.FakeSessionTransport Transport = new FixSessionTests.FakeSessionTransport();
        internal MemoryMessageStore Store = new MemoryMessageStore(0);
        internal EmbeddedDictionaryProvider Provider;
        internal FixDictionary WireDictionary;
        internal AdminMessageFactory PeerFactory;
        internal FixSession Session;

        internal ProfileSession(FakeSessionClock clock, FixSessionTests.RecordingSessionApplication application, EmbeddedDictionaryProvider provider, string qualifier, bool fixt, SessionSchedule? schedule)
        {
            Clock = clock;
            Provider = provider;
            Settings = CreateSettings(qualifier, fixt, schedule);
            WireDictionary = provider.ResolveDictionary(Settings.TransportDictionary).Dictionary!;
            PeerFactory = new AdminMessageFactory(CreatePeerSettings(Settings), clock);
            Session = new FixSession(Settings, Transport, Store, new FixSessionTests.RecordingFixLog(), clock, new FixSessionTests.RecordingThrottle(), application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixSessionTests.AcceptingMessageValidator(), provider, new FixSessionTests.AcceptingAuthenticator());
        }

        internal void Activate()
        {
            SessionPollResult started = Session.StartSession();
            Assert.That(started.State, Is.EqualTo(SessionState.LogonPending));
            FlushOutbound();
            QueueInbound(PeerFactory.CreateLogonMessage(1, 1, false));
            SessionPollResult active = Session.PollSession();
            Assert.That(active.State, Is.EqualTo(SessionState.Active), EventText(active));
            Transport.OutboundBytes.Clear();
        }

        internal void QueueInbound(FixMessage message)
        {
            Transport.AddInbound(Encode(message));
        }

        internal void FlushOutbound()
        {
            int polls = 0;
            while (Session.GetSessionSnapshot().OutboundQueueCount != 0 && polls < 20)
            {
                Session.PollSession();
                polls++;
            }
            Assert.That(Session.GetSessionSnapshot().OutboundQueueCount, Is.Zero);
            Transport.OutboundBytes.Clear();
        }

        internal byte[] Encode(FixMessage message)
        {
            byte[] destination = new byte[FixDefaults.MaxMessageSize];
            EncodeResult encoded = new TagValueMessageEncoder().EncodeMessage(message, WireDictionary, destination, new EncodeOptions());
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode + ":" + encoded.ErrorMessage);
            byte[] bytes = new byte[encoded.BytesWritten];
            Array.Copy(destination, bytes, encoded.BytesWritten);
            return bytes;
        }

        internal static SessionSettings CreateSettings(string qualifier, bool fixt, SessionSchedule? schedule)
        {
            SessionSettings settings = FixSessionTests.Settings(false);
            settings.Id.SenderCompID = "LOCAL-" + qualifier;
            settings.Id.TargetCompID = "REMOTE-" + qualifier;
            settings.Id.Qualifier = qualifier;
            settings.HeartbeatIntervalSeconds = 30;
            if (schedule != null) settings.Schedule = schedule;
            if (fixt)
            {
                settings.Id.BeginString = FixBeginStrings.Fixt11;
                settings.TransportDictionary.BeginString = FixBeginStrings.Fixt11;
                settings.ApplicationDictionary.BeginString = "";
                settings.ApplicationDictionary.ApplicationVersion = FixApplicationVersions.Fix50Sp2;
                settings.DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2;
            }
            return settings;
        }

        internal static SessionSettings CreatePeerSettings(SessionSettings local)
        {
            bool fixt = local.Id.BeginString == FixBeginStrings.Fixt11;
            SessionSettings peer = CreateSettings(local.Id.Qualifier, fixt, null);
            peer.Role = SessionRole.Acceptor;
            peer.Id.SenderCompID = local.Id.TargetCompID;
            peer.Id.TargetCompID = local.Id.SenderCompID;
            return peer;
        }

        internal static string EventText(SessionPollResult result)
        {
            return string.Join(",", result.Events.ConvertAll(item => item.Code + ":" + item.Text));
        }
    }
}
