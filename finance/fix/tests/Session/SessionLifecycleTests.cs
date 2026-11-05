using System;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SessionLifecycleTests
{
    [Test]
    public void InitiatorAndAcceptorCompleteLogonAndLogoutWithOrderedCallbacks()
    {
        SessionPair pair = new SessionPair(5);

        pair.Activate();
        SessionPollResult logoutStarted = pair.InitiatorSession.LogoutSession("finished");
        pair.InitiatorSession.PollSession();
        pair.TransferInitiatorToAcceptor();
        SessionPollResult peerLogout = pair.AcceptorSession.PollSession();
        SessionPollResult peerResponse = pair.AcceptorSession.PollSession();
        pair.TransferAcceptorToInitiator();
        SessionPollResult completed = pair.InitiatorSession.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(logoutStarted.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(peerLogout.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(peerResponse.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(completed.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(pair.InitiatorApplication.CallbackOrder, Is.EqualTo(new[] { "created", "administrative", "logon", "administrative", "logout" }));
            Assert.That(pair.AcceptorApplication.CallbackOrder, Is.EqualTo(new[] { "created", "administrative", "logon", "administrative", "logout" }));
            Assert.That(pair.InitiatorTransport.CloseCount, Is.EqualTo(1));
            Assert.That(pair.AcceptorTransport.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void TestRequestRoundTripReturnsMatchingHeartbeatWithoutDisconnecting()
    {
        SessionPair pair = new SessionPair(5);
        pair.Activate();
        pair.Clock.AdvanceClock(TimeSpan.FromSeconds(5));

        SessionPollResult requested = pair.InitiatorSession.PollSession();
        pair.InitiatorSession.PollSession();
        pair.TransferInitiatorToAcceptor();
        SessionPollResult receivedRequest = pair.AcceptorSession.PollSession();
        pair.AcceptorSession.PollSession();
        pair.TransferAcceptorToInitiator();
        SessionPollResult receivedHeartbeat = pair.InitiatorSession.PollSession();

        SessionEvent requestEvent = requested.Events.Single(item => item.Kind == SessionEventKind.TestRequest);
        SessionEvent responseEvent = receivedHeartbeat.Events.Single(item => item.Kind == SessionEventKind.Heartbeat);
        Assert.Multiple(() =>
        {
            Assert.That(Text(requestEvent.Message!, FixTags.TestReqID), Is.EqualTo("TEST-1"));
            Assert.That(receivedRequest.Events.Exists(item => item.Kind == SessionEventKind.TestRequest), Is.True);
            Assert.That(Text(responseEvent.Message!, FixTags.TestReqID), Is.EqualTo("TEST-1"));
            Assert.That(responseEvent.Code, Is.EqualTo("heartbeat-test-request-matched"));
            Assert.That(pair.InitiatorSession.GetSessionSnapshot().State, Is.EqualTo(SessionState.Active));
            Assert.That(pair.AcceptorSession.GetSessionSnapshot().State, Is.EqualTo(SessionState.Active));
        });
    }

    [Test]
    public void UnansweredTestRequestDisconnectsExactlyAtItsDeadline()
    {
        SessionPair pair = new SessionPair(5);
        pair.Activate();
        pair.Clock.AdvanceClock(TimeSpan.FromSeconds(5));
        pair.InitiatorSession.PollSession();
        pair.InitiatorSession.PollSession();

        pair.Clock.AdvanceClock(TimeSpan.FromSeconds(5).Subtract(TimeSpan.FromTicks(1)));
        SessionPollResult beforeDeadline = pair.InitiatorSession.PollSession();
        pair.Clock.AdvanceClock(TimeSpan.FromTicks(1));
        SessionPollResult atDeadline = pair.InitiatorSession.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(beforeDeadline.State, Is.EqualTo(SessionState.Active));
            Assert.That(beforeDeadline.Events.Exists(item => item.Code == "test-request-timeout"), Is.False);
            Assert.That(atDeadline.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(atDeadline.Events.Exists(item => item.Code == "test-request-timeout"), Is.True);
            Assert.That(pair.InitiatorTransport.CloseCount, Is.EqualTo(1));
        });
    }

    [TestCase(SessionRole.Initiator)]
    [TestCase(SessionRole.Acceptor)]
    public void LogonTimeoutIsDeterministicForBothSessionRoles(SessionRole role)
    {
        FakeSessionClock clock = new FakeSessionClock(FixSessionTests.Now);
        SessionSettings settings = Settings(role, role == SessionRole.Initiator ? "LOCAL" : "REMOTE", role == SessionRole.Initiator ? "REMOTE" : "LOCAL", 5);
        settings.LogonTimeoutMilliseconds = 25;
        settings.ReconnectDelayMilliseconds = 10;
        settings.MaxReconnectDelayMilliseconds = 10;
        FixSessionTests.FakeSessionTransport transport = new FixSessionTests.FakeSessionTransport();
        FixSession session = CreateSession(settings, transport, clock, new FixSessionTests.RecordingSessionApplication());
        session.StartSession();
        clock.AdvanceClock(TimeSpan.FromMilliseconds(25));

        SessionPollResult timeout = session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(timeout.State, Is.EqualTo(role == SessionRole.Initiator ? SessionState.Reconnecting : SessionState.Disconnected));
            Assert.That(timeout.Events.Exists(item => item.Code == "logon-timeout"), Is.True);
            Assert.That(timeout.Events.Exists(item => item.Code == "transport-retry-scheduled"), Is.True);
            Assert.That(transport.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidAcceptorLogonQueuesLogoutWithoutLifecycleCallbacks()
    {
        SessionPair pair = new SessionPair(5);
        pair.AcceptorSession.StartSession();
        AdminMessageFactory factory = new AdminMessageFactory(pair.InitiatorSettings, pair.Clock);
        FixMessage invalidLogon = factory.CreateLogonMessage(1, 1, false);
        invalidLogon.BodyFields.RemoveAll(field => field.Tag == FixTags.HeartBtInt);
        pair.AcceptorTransport.AddInbound(pair.Encode(invalidLogon));

        SessionPollResult rejected = pair.AcceptorSession.PollSession();
        pair.AcceptorTransport.MaximumWrite = 10;
        SessionPollResult partialLogout = pair.AcceptorSession.PollSession();
        pair.Clock.AdvanceClock(TimeSpan.FromMilliseconds(pair.AcceptorSettings.LogonTimeoutMilliseconds));
        SessionPollResult logoutSent = partialLogout;
        int polls = 1;
        while (logoutSent.State != SessionState.Disconnected && polls < 100)
        {
            logoutSent = pair.AcceptorSession.PollSession();
            polls++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(rejected.Events.Exists(item => item.Code == "missing-heart-bt-int"), Is.True);
            Assert.That(rejected.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(partialLogout.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(partialLogout.Events.Exists(item => item.Kind == SessionEventKind.Backpressure), Is.True);
            Assert.That(logoutSent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message!.MsgType == FixAdminMessageTypes.Logout), Is.True);
            Assert.That(logoutSent.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(polls, Is.GreaterThan(1));
            Assert.That(pair.AcceptorApplication.CallbackOrder, Is.EqualTo(new[] { "created" }));
            Assert.That(pair.AcceptorSession.GetSessionSnapshot().NextTargetSeqNum, Is.EqualTo(1));
        });
    }

    private static SessionSettings Settings(SessionRole role, string sender, string target, int heartbeatSeconds)
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Role = role;
        settings.Id.SenderCompID = sender;
        settings.Id.TargetCompID = target;
        settings.HeartbeatIntervalSeconds = heartbeatSeconds;
        return settings;
    }

    private static FixSession CreateSession(SessionSettings settings, FixSessionTests.FakeSessionTransport transport, FakeSessionClock clock, FixSessionTests.RecordingSessionApplication application)
    {
        return new FixSession(settings, transport, new MemoryMessageStore(0), new FixSessionTests.RecordingFixLog(), clock, new FixSessionTests.RecordingThrottle(), application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixSessionTests.AcceptingMessageValidator(), new FixSessionTests.EmptyDictionaryProvider(), new FixSessionTests.AcceptingAuthenticator());
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField field = EnvelopeValidator.FindFirstMessageField(message, tag)!;
        return Encoding.Latin1.GetString(field.Value);
    }

    private class SessionPair
    {
        internal FakeSessionClock Clock = new FakeSessionClock(FixSessionTests.Now);
        internal SessionSettings InitiatorSettings;
        internal SessionSettings AcceptorSettings;
        internal FixSessionTests.FakeSessionTransport InitiatorTransport = new FixSessionTests.FakeSessionTransport();
        internal FixSessionTests.FakeSessionTransport AcceptorTransport = new FixSessionTests.FakeSessionTransport();
        internal FixSessionTests.RecordingSessionApplication InitiatorApplication = new FixSessionTests.RecordingSessionApplication();
        internal FixSessionTests.RecordingSessionApplication AcceptorApplication = new FixSessionTests.RecordingSessionApplication();
        internal FixSessionTests.EmptyDictionaryProvider DictionaryProvider = new FixSessionTests.EmptyDictionaryProvider();
        internal FixSession InitiatorSession;
        internal FixSession AcceptorSession;

        internal SessionPair(int heartbeatSeconds)
        {
            InitiatorSettings = Settings(SessionRole.Initiator, "LOCAL", "REMOTE", heartbeatSeconds);
            AcceptorSettings = Settings(SessionRole.Acceptor, "REMOTE", "LOCAL", heartbeatSeconds);
            InitiatorSession = CreateSession(InitiatorSettings, InitiatorTransport, Clock, InitiatorApplication);
            AcceptorSession = CreateSession(AcceptorSettings, AcceptorTransport, Clock, AcceptorApplication);
        }

        internal void Activate()
        {
            SessionPollResult acceptorStarted = AcceptorSession.StartSession();
            SessionPollResult initiatorStarted = InitiatorSession.StartSession();
            InitiatorSession.PollSession();
            TransferInitiatorToAcceptor();
            SessionPollResult acceptorActive = AcceptorSession.PollSession();
            AcceptorSession.PollSession();
            TransferAcceptorToInitiator();
            SessionPollResult initiatorActive = InitiatorSession.PollSession();

            Assert.Multiple(() =>
            {
                Assert.That(acceptorStarted.State, Is.EqualTo(SessionState.LogonPending));
                Assert.That(initiatorStarted.State, Is.EqualTo(SessionState.LogonPending));
                Assert.That(acceptorActive.State, Is.EqualTo(SessionState.Active), EventText(acceptorActive));
                Assert.That(initiatorActive.State, Is.EqualTo(SessionState.Active), EventText(initiatorActive));
                Assert.That(InitiatorApplication.CallbackOrder, Is.EqualTo(new[] { "created", "administrative", "logon" }));
                Assert.That(AcceptorApplication.CallbackOrder, Is.EqualTo(new[] { "created", "administrative", "logon" }));
            });
        }

        internal byte[] Encode(FixMessage message)
        {
            byte[] destination = new byte[FixDefaults.MaxMessageSize];
            EncodeResult encoded = new TagValueMessageEncoder().EncodeMessage(message, DictionaryProvider.Dictionary, destination, new EncodeOptions());
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode + ": " + encoded.ErrorMessage);
            byte[] bytes = new byte[encoded.BytesWritten];
            Array.Copy(destination, bytes, encoded.BytesWritten);
            return bytes;
        }

        internal void TransferInitiatorToAcceptor()
        {
            Transfer(InitiatorTransport, AcceptorTransport);
        }

        internal void TransferAcceptorToInitiator()
        {
            Transfer(AcceptorTransport, InitiatorTransport);
        }

        private static void Transfer(FixSessionTests.FakeSessionTransport source, FixSessionTests.FakeSessionTransport destination)
        {
            destination.AddInbound(source.OutboundBytes.ToArray());
            source.OutboundBytes.Clear();
        }

        private static string EventText(SessionPollResult result)
        {
            return string.Join(",", result.Events.ConvertAll(item => item.Code + ":" + item.Text));
        }
    }
}
