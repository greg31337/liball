using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class InitiatorAcceptorEndToEndTests
{
    [Test]
    public void LoadedFix44PeersCompleteFramedRecoveryValidationHeartbeatAndLogoutLifecycle()
    {
        EndToEndPair pair = new EndToEndPair();

        pair.Start();
        pair.Transport.First.FragmentNextWrite(1, 2, 5, 8);
        pair.PumpUntil(() => pair.Initiator.GetSessionSnapshot().State == SessionState.Active && pair.Acceptor.GetSessionSnapshot().State == SessionState.Active);

        pair.Transport.First.CoalesceNextWrites(2);
        AssertAccepted(pair.Initiator.SendMessage(Order("ORDER-1")));
        AssertAccepted(pair.Initiator.SendMessage(Order("ORDER-2")));
        pair.PumpUntil(() => pair.AcceptorApplication.ApplicationContexts.Count == 2);

        pair.Transport.First.DropNextWrite();
        AssertAccepted(pair.Initiator.SendMessage(Order("ORDER-3")));
        pair.PollInitiator();
        AssertAccepted(pair.Initiator.SendMessage(Order("ORDER-4")));
        pair.PumpUntil(() => pair.AcceptorApplication.ApplicationContexts.Count == 4 && pair.Acceptor.GetSessionSnapshot().State == SessionState.Active);

        AssertAccepted(pair.Initiator.SendMessage(InvalidOrder("INVALID-ORDER")));
        pair.PumpUntil(() => pair.InitiatorEvents.Exists(item => item.Kind == SessionEventKind.MessageReceived && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Reject));

        pair.Clock.AdvanceClock(TimeSpan.FromSeconds(4));
        AssertAccepted(pair.Initiator.SendMessage(Order("ORDER-5")));
        pair.PumpUntil(() => pair.AcceptorApplication.ApplicationContexts.Count == 5);
        pair.Clock.AdvanceClock(TimeSpan.FromSeconds(1));
        pair.PollAcceptor();
        pair.PollAcceptor();
        pair.PollInitiator();

        SessionPollResult logout = pair.Initiator.LogoutSession("end-to-end-complete");
        pair.InitiatorEvents.AddRange(logout.Events);
        pair.PumpUntil(() => pair.Initiator.GetSessionSnapshot().State == SessionState.Disconnected && pair.Acceptor.GetSessionSnapshot().State == SessionState.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(pair.LoadedSettings, Has.Length.EqualTo(2));
            Assert.That(pair.LoadedSettings[0].TransportDictionary.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(pair.LoadedSettings[1].TransportDictionary.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(pair.LoadedSettings[0].ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix44));
            Assert.That(pair.LoadedSettings[1].ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix44));
            Assert.That(pair.InitiatorDictionary.Messages.Exists(item => item.MsgType == "D"), Is.True);
            Assert.That(pair.AcceptorDictionary.Messages.Exists(item => item.MsgType == "D"), Is.True);
            Assert.That(pair.InitiatorApplication.CallbackOrder.Take(3), Is.EqualTo(new[] { "created", "administrative", "logon" }));
            Assert.That(pair.AcceptorApplication.CallbackOrder.Take(3), Is.EqualTo(new[] { "created", "administrative", "logon" }));
            Assert.That(pair.AcceptorApplication.ApplicationContexts.Select(item => item.SequenceNumber), Is.EqualTo(new[] { 2, 3, 4, 5, 7 }));
            Assert.That(pair.AcceptorApplication.ApplicationContexts.Count(item => item.IsPossibleDuplicate), Is.EqualTo(1));
            Assert.That(pair.InitiatorApplication.CallbackOrder, Does.Contain("replay"));
            Assert.That(pair.AcceptorEvents.Exists(item => item.Kind == SessionEventKind.SequenceGap), Is.True);
            Assert.That(pair.AcceptorEvents.Exists(item => item.Kind == SessionEventKind.RecoveryStarted), Is.True);
            Assert.That(pair.AcceptorEvents.Exists(item => item.Kind == SessionEventKind.RecoveryCompleted), Is.True);
            Assert.That(pair.AcceptorEvents.Exists(item => item.Code == "required-field-missing" && item.Message != null && Text(item.Message, 11) == "INVALID-ORDER"), Is.True);
            Assert.That(pair.AcceptorEvents.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Reject), Is.True);
            Assert.That(pair.InitiatorEvents.Exists(item => item.Kind == SessionEventKind.Heartbeat && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Heartbeat), Is.True);
            Assert.That(pair.InitiatorApplication.LogoutCount, Is.EqualTo(1));
            Assert.That(pair.AcceptorApplication.LogoutCount, Is.EqualTo(1));
            Assert.That(pair.InitiatorLog.Entries.Exists(item => item.Direction == LogDirection.Incoming), Is.True);
            Assert.That(pair.AcceptorLog.Entries.Exists(item => item.Direction == LogDirection.Outgoing), Is.True);
        });
    }

    private static FixMessage Order(string id)
    {
        return new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                FixSessionTests.Field(11, id),
                FixSessionTests.Field(21, "1"),
                FixSessionTests.Field(55, "IBM"),
                FixSessionTests.Field(54, "1"),
                FixSessionTests.Field(60, "20260830-18:00:00.000"),
                FixSessionTests.Field(40, "1")
            ]
        };
    }

    private static FixMessage InvalidOrder(string id)
    {
        return new FixMessage { MsgType = "D", BodyFields = [FixSessionTests.Field(11, id)] };
    }

    private static void AssertAccepted(SessionSendResult result)
    {
        Assert.That(result.Accepted, Is.True, result.Code + ": " + result.Text);
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    private class EndToEndPair
    {
        internal SessionSettings[] LoadedSettings;
        internal InMemoryDuplexTransportPair Transport = new InMemoryDuplexTransportPair();
        internal FakeSessionClock Clock = new FakeSessionClock(FixSessionTests.Now);
        internal FixSessionTests.RecordingSessionApplication InitiatorApplication = new FixSessionTests.RecordingSessionApplication();
        internal FixSessionTests.RecordingSessionApplication AcceptorApplication = new FixSessionTests.RecordingSessionApplication();
        internal FixSessionTests.RecordingFixLog InitiatorLog = new FixSessionTests.RecordingFixLog();
        internal FixSessionTests.RecordingFixLog AcceptorLog = new FixSessionTests.RecordingFixLog();
        internal List<SessionEvent> InitiatorEvents = [];
        internal List<SessionEvent> AcceptorEvents = [];
        internal FixDictionary InitiatorDictionary;
        internal FixDictionary AcceptorDictionary;
        internal FixSession Initiator;
        internal FixSession Acceptor;

        internal EndToEndPair()
        {
            LoadedSettings = LoadSettings();
            EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
            InitiatorDictionary = provider.ResolveDictionary(LoadedSettings[0].TransportDictionary).Dictionary!;
            AcceptorDictionary = provider.ResolveDictionary(LoadedSettings[1].TransportDictionary).Dictionary!;
            Initiator = CreateSession(LoadedSettings[0], Transport.First, InitiatorApplication, InitiatorLog, new FixSessionTests.AcceptingMessageValidator(), provider);
            Acceptor = CreateSession(LoadedSettings[1], Transport.Second, AcceptorApplication, AcceptorLog, new FixMessageValidator(), provider);
        }

        internal void Start()
        {
            SessionPollResult acceptor = Acceptor.StartSession();
            SessionPollResult initiator = Initiator.StartSession();
            AcceptorEvents.AddRange(acceptor.Events);
            InitiatorEvents.AddRange(initiator.Events);
        }

        internal void PumpUntil(Func<bool> completed)
        {
            int polls = 0;
            while (!completed() && polls < 500)
            {
                PollInitiator();
                PollAcceptor();
                polls++;
            }
            Assert.That(completed(), Is.True, StateText());
        }

        internal void PollInitiator()
        {
            InitiatorEvents.AddRange(Initiator.PollSession().Events);
        }

        internal void PollAcceptor()
        {
            AcceptorEvents.AddRange(Acceptor.PollSession().Events);
        }

        internal string StateText()
        {
            SessionSnapshot initiator = Initiator.GetSessionSnapshot();
            SessionSnapshot acceptor = Acceptor.GetSessionSnapshot();
            return "initiator=" + initiator.State + "/" + initiator.NextSenderSeqNum + "/" + initiator.NextTargetSeqNum + ", acceptor=" + acceptor.State + "/" + acceptor.NextSenderSeqNum + "/" + acceptor.NextTargetSeqNum + ", initiator-events=" + string.Join(",", InitiatorEvents.ConvertAll(item => item.Code)) + ", acceptor-events=" + string.Join(",", AcceptorEvents.ConvertAll(item => item.Code));
        }

        private FixSession CreateSession(SessionSettings settings, ISessionTransport transport, FixSessionTests.RecordingSessionApplication application, FixSessionTests.RecordingFixLog log, IMessageValidator validator, EmbeddedDictionaryProvider provider)
        {
            return new FixSession(settings, transport, new MemoryMessageStore(0), log, Clock, new TokenBucketMessageThrottle(Clock), application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), validator, provider, new BasicSessionAuthenticator(settings, null));
        }

        private static SessionSettings[] LoadSettings()
        {
            string text = """
                [DEFAULT]
                BeginString=FIX.4.4
                HeartBtInt=5
                CheckLatency=N
                SocketConnectPort=9876

                [SESSION]
                ConnectionType=initiator
                SenderCompID=INITIATOR
                TargetCompID=ACCEPTOR
                SocketConnectHost=peer

                [SESSION]
                ConnectionType=acceptor
                SenderCompID=ACCEPTOR
                TargetCompID=INITIATOR
                SocketAcceptAddress=peer
                SocketAcceptPort=9876
                """;
            using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return new TextSessionSettingsLoader().LoadSessionSettings(source);
        }
    }
}
