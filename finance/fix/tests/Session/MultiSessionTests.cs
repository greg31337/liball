using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class MultiSessionTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 18, 0, 0, DateTimeKind.Utc);

    [Test]
    public void AddFindAndRemoveManageIndependentSessionInstances()
    {
        FixSessionEngine engine = CreateEngine();
        SessionSettings firstSettings = InactiveSettings("FIRST", "REMOTE", "ONE");
        SessionSettings secondSettings = InactiveSettings("SECOND", "REMOTE", "TWO");

        SessionPollResult first = engine.AddSession(firstSettings);
        SessionPollResult second = engine.AddSession(secondSettings);
        SessionSnapshot found = engine.ResolveSessionSnapshot(CopyId(firstSettings.Id));
        FixSession firstSession = (FixSession)engine.Sessions[0].Session;
        FixSession secondSession = (FixSession)engine.Sessions[1].Session;
        found.Id.SenderCompID = "MUTATED";
        SessionSnapshot detached = engine.ResolveSessionSnapshot(firstSettings.Id);
        SessionPollResult removed = engine.RemoveSession(firstSettings.Id);

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(second.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(first.Events.Exists(item => item.Code == "session-schedule-inactive"), Is.True);
            Assert.That(detached.Id.SenderCompID, Is.EqualTo("FIRST"));
            Assert.That(removed.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(engine.Sessions, Has.Count.EqualTo(1));
            Assert.That(engine.Sessions[0].Id.SenderCompID, Is.EqualTo("SECOND"));
            Assert.That(firstSession.Core.Transport, Is.Not.SameAs(secondSession.Core.Transport));
            Assert.That(firstSession.Core.Store, Is.Not.SameAs(secondSession.Core.Store));
            Assert.That(firstSession.Core.Throttle, Is.Not.SameAs(secondSession.Core.Throttle));
            Assert.Throws<FixSessionException>(() => engine.ResolveSessionSnapshot(firstSettings.Id));
        });
    }

    [Test]
    public void DuplicateDetectionUsesTheCompleteIdentityAndPreservesRegistration()
    {
        FixSessionEngine engine = CreateEngine();
        SessionSettings original = InactiveSettings("LOCAL", "REMOTE", "PRIMARY");
        SessionSettings duplicate = InactiveSettings("LOCAL", "REMOTE", "PRIMARY");
        SessionSettings qualified = InactiveSettings("LOCAL", "REMOTE", "SECONDARY");
        duplicate.Id.SenderSubID = "SUB";
        original.Id.SenderSubID = "SUB";
        qualified.Id.SenderSubID = "SUB";
        engine.AddSession(original);

        Assert.Throws<FixSessionException>(() => engine.AddSession(duplicate));
        Assert.DoesNotThrow(() => engine.AddSession(qualified));

        Assert.Multiple(() =>
        {
            Assert.That(engine.Sessions, Has.Count.EqualTo(2));
            Assert.That(engine.ResolveSessionSnapshot(original.Id).Id.Qualifier, Is.EqualTo("PRIMARY"));
            Assert.That(engine.ResolveSessionSnapshot(qualified.Id).Id.Qualifier, Is.EqualTo("SECONDARY"));
        });
    }

    [Test]
    public void PollAggregatesInInsertionOrderAndIsolatesEventsAndFailures()
    {
        FixSessionEngine engine = CreateEngine();
        SessionSettings firstSettings = InactiveSettings("FIRST", "REMOTE", "ONE");
        SessionSettings secondSettings = InactiveSettings("SECOND", "REMOTE", "TWO");
        engine.AddSession(firstSettings);
        engine.AddSession(secondSettings);
        SessionPollResult firstResult = PollResult(firstSettings.Id, SessionState.Faulted, true, "first-event");
        SessionPollResult secondResult = PollResult(secondSettings.Id, SessionState.Active, false, "second-event");
        ScriptedFixSession first = new ScriptedFixSession(firstResult);
        ScriptedFixSession second = new ScriptedFixSession(secondResult);
        engine.Sessions[0].Session = first;
        engine.Sessions[1].Session = second;

        SessionPollResult aggregate = engine.PollSessions();
        firstResult.Events[0].Code = "mutated";
        firstResult.Events[0].Id.SenderCompID = "mutated";
        firstResult.Events[0].Message!.MsgType = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(first.PollCount, Is.EqualTo(1));
            Assert.That(second.PollCount, Is.EqualTo(1));
            Assert.That(aggregate.State, Is.EqualTo(SessionState.Active));
            Assert.That(aggregate.WorkRemaining, Is.True);
            Assert.That(aggregate.Snapshot.Id.SenderCompID, Is.EqualTo("SECOND"));
            Assert.That(aggregate.Events.ConvertAll(item => item.Code), Is.EqualTo(new[] { "first-event", "second-event" }));
            Assert.That(aggregate.Events.ConvertAll(item => item.Id.SenderCompID), Is.EqualTo(new[] { "FIRST", "SECOND" }));
            Assert.That(aggregate.Events[0].Message!.MsgType, Is.EqualTo("D"));
            Assert.That(aggregate.Events[0], Is.Not.SameAs(firstResult.Events[0]));
            Assert.That(aggregate.Events[0].Message, Is.Not.SameAs(firstResult.Events[0].Message));
        });
    }

    [Test]
    public void EmptyPollingAndInvalidConfigurationHaveExplicitOutcomes()
    {
        FixSessionEngine engine = CreateEngine();
        SessionPollResult empty = engine.PollSessions();
        SessionSettings badStore = InactiveSettings("BAD-STORE", "REMOTE", "ONE");
        badStore.StoreType = "Unknown";
        SessionSettings badLog = InactiveSettings("BAD-LOG", "REMOTE", "TWO");
        badLog.LogType = "Unknown";
        FixSessionTests.RecordingSessionApplication rejectingApplication = new FixSessionTests.RecordingSessionApplication();
        rejectingApplication.Decision = FixSessionTests.Decision(MessageDisposition.Reject, "creation-rejected", "rejected");
        FixSessionEngine rejectingEngine = new FixSessionEngine(new FakeSessionClock(Now), rejectingApplication, new FixSessionTests.EmptyDictionaryProvider(), null);
        SessionPollResult rejected = rejectingEngine.AddSession(InactiveSettings("REJECTED", "REMOTE", "THREE"));

        Assert.Multiple(() =>
        {
            Assert.That(empty.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(empty.Events, Is.Empty);
            Assert.That(empty.WorkRemaining, Is.False);
            Assert.Throws<FixSessionException>(() => engine.AddSession(badStore));
            Assert.Throws<FixSessionException>(() => engine.AddSession(badLog));
            Assert.Throws<FixSessionException>(() => engine.RemoveSession(new SessionId()));
            Assert.Throws<FixSessionException>(() => new FixSessionEngine(null!, new FixSessionTests.RecordingSessionApplication(), new FixSessionTests.EmptyDictionaryProvider(), null));
            Assert.That(engine.Sessions, Is.Empty);
            Assert.That(rejected.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(rejected.Events.Exists(item => item.Code == "creation-rejected"), Is.True);
            Assert.That(rejectingEngine.Sessions, Is.Empty);
        });
    }

    [Test]
    public void FilePersistenceConfigurationCreatesIdentityScopedStoreAndLog()
    {
        string directory = Path.Combine(Path.GetTempPath(), "fix-engine-files-" + Guid.NewGuid().ToString("N"));
        try
        {
            SessionSettings settings = InactiveSettings("FILE", "REMOTE", "PRIMARY");
            settings.StoreType = "file";
            settings.StorePath = directory;
            settings.LogType = "file";
            settings.LogPath = Path.Combine(directory, "session.log");
            FixSessionEngine engine = CreateEngine();

            SessionPollResult added = engine.AddSession(settings);
            FixSession session = (FixSession)engine.Sessions.Single().Session;
            FileMessageStore store = (FileMessageStore)session.Core.Store;

            Assert.Multiple(() =>
            {
                Assert.That(added.State, Is.EqualTo(SessionState.Disconnected));
                Assert.That(session.Core.Store, Is.TypeOf<FileMessageStore>());
                Assert.That(session.Core.Log, Is.TypeOf<FileFixLog>());
                Assert.That(Path.GetFileName(store.AppendPath), Does.StartWith("session-"));
                Assert.That(Path.GetFileName(store.AppendPath), Does.EndWith(".messages"));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    internal static FixSessionEngine CreateEngine()
    {
        return new FixSessionEngine(new FakeSessionClock(Now), new FixSessionTests.RecordingSessionApplication(), new FixSessionTests.EmptyDictionaryProvider(), new FixSessionTests.AcceptingAuthenticator());
    }

    internal static SessionSettings InactiveSettings(string sender, string target, string qualifier)
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Id.SenderCompID = sender;
        settings.Id.TargetCompID = target;
        settings.Id.Qualifier = qualifier;
        settings.Schedule.NonStop = false;
        settings.Schedule.TimeZoneId = "UTC";
        settings.Schedule.Windows.Add(new SessionTimeWindow
        {
            StartDay = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndDay = DayOfWeek.Monday,
            EndTime = new TimeSpan(17, 0, 0)
        });
        return settings;
    }

    internal static SessionId CopyId(SessionId source)
    {
        return new SessionId
        {
            BeginString = source.BeginString,
            SenderCompID = source.SenderCompID,
            SenderSubID = source.SenderSubID,
            SenderLocationID = source.SenderLocationID,
            TargetCompID = source.TargetCompID,
            TargetSubID = source.TargetSubID,
            TargetLocationID = source.TargetLocationID,
            Qualifier = source.Qualifier
        };
    }

    internal static SessionPollResult PollResult(SessionId id, SessionState state, bool workRemaining, string code)
    {
        SessionPollResult result = new SessionPollResult();
        result.State = state;
        result.WorkRemaining = workRemaining;
        result.Snapshot.Id = CopyId(id);
        result.Snapshot.State = state;
        SessionEvent sessionEvent = new SessionEvent();
        sessionEvent.Id = CopyId(id);
        sessionEvent.Kind = state == SessionState.Faulted ? SessionEventKind.Error : SessionEventKind.MessageReceived;
        sessionEvent.TimestampUtc = Now;
        sessionEvent.Code = code;
        sessionEvent.Text = code;
        sessionEvent.Message = new FixMessage { MsgType = "D", BodyFields = [new FixField { Tag = 11, Value = Encoding.Latin1.GetBytes(code) }] };
        result.Events.Add(sessionEvent);
        return result;
    }

    internal class ScriptedFixSession : IFixSession
    {
        internal SessionPollResult Result;
        internal int PollCount;

        internal ScriptedFixSession(SessionPollResult result)
        {
            Result = result;
        }

        public SessionPollResult StartSession()
        {
            return Result;
        }

        public SessionPollResult PollSession()
        {
            PollCount++;
            return Result;
        }

        public SessionSendResult SendMessage(FixMessage message)
        {
            return new SessionSendResult();
        }

        public SessionPollResult LogoutSession(string text)
        {
            return Result;
        }

        public SessionPollResult StopSession()
        {
            return Result;
        }

        public SessionSnapshot GetSessionSnapshot()
        {
            return Result.Snapshot;
        }
    }
}
