using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SessionRoleContractTests
{
    [TestCase(SessionRole.Initiator)]
    [TestCase(SessionRole.Acceptor)]
    public void PublicSessionRolesShareStartPollSnapshotAndStopContracts(SessionRole role)
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Role = role;
        FakeSessionClock clock = new FakeSessionClock(FixSessionTests.Now);
        FixSessionTests.FakeSessionTransport transport = new FixSessionTests.FakeSessionTransport();
        IFixSession session = new FixSession(
            settings,
            transport,
            new MemoryMessageStore(0),
            new FixSessionTests.RecordingFixLog(),
            clock,
            new FixSessionTests.RecordingThrottle(),
            new FixSessionTests.RecordingSessionApplication(),
            new TagValueMessageFramer(),
            new TagValueMessageDecoder(),
            new TagValueMessageEncoder(),
            new FixSessionTests.AcceptingMessageValidator(),
            new FixSessionTests.EmptyDictionaryProvider(),
            new FixSessionTests.AcceptingAuthenticator());

        SessionPollResult started = session.StartSession();
        SessionSnapshot firstSnapshot = session.GetSessionSnapshot();
        SessionPollResult polled = session.PollSession();
        SessionPollResult stopped = session.StopSession();
        SessionSnapshot stoppedSnapshot = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(started.Events.ConvertAll(item => item.Kind), Is.EqualTo(new[] { SessionEventKind.Started, SessionEventKind.Connected }));
            Assert.That(firstSnapshot.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(polled.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(stopped.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(stoppedSnapshot.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(firstSnapshot, Is.Not.SameAs(started.Snapshot));
            Assert.That(stoppedSnapshot, Is.Not.SameAs(stopped.Snapshot));
            Assert.That(transport.OpenCount, Is.EqualTo(1));
            Assert.That(transport.CloseCount, Is.EqualTo(1));
        });
    }
}
