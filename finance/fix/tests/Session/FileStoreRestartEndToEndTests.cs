using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class FileStoreRestartEndToEndTests
{
    private string storeDirectory = "";

    [SetUp]
    public void CreateStoreDirectory()
    {
        storeDirectory = Path.Combine(Path.GetTempPath(), "fix-restart-end-to-end-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storeDirectory);
    }

    [TearDown]
    public void RemoveStoreDirectory()
    {
        if (Directory.Exists(storeDirectory)) Directory.Delete(storeDirectory, true);
    }

    [Test]
    public void DurableSequencesAndEarlierApplicationReplaysSpanThreePhysicalConnections()
    {
        InMemoryDuplexTransportPair transport = new InMemoryDuplexTransportPair();
        FakeSessionClock clock = new FakeSessionClock(FixSessionTests.Now);
        SessionSettings initiatorSettings = Settings(SessionRole.Initiator, "INITIATOR", "ACCEPTOR");
        SessionSettings acceptorSettings = Settings(SessionRole.Acceptor, "ACCEPTOR", "INITIATOR");

        RestartConnection first = Connection(transport, clock, initiatorSettings, acceptorSettings);
        first.StartAndActivate();
        AssertAccepted(first.Initiator.SendMessage(Order("ORDER-1")));
        AssertAccepted(first.Initiator.SendMessage(Order("ORDER-2")));
        first.PumpUntil(() => first.AcceptorApplication.ApplicationContexts.Count == 2);
        SessionSnapshot firstInitiator = first.Initiator.GetSessionSnapshot();
        SessionSnapshot firstAcceptor = first.Acceptor.GetSessionSnapshot();

        transport.Disconnect();
        first.PollInitiator();
        first.PollAcceptor();
        transport.Reconnect();

        RestartConnection second = Connection(transport, clock, initiatorSettings, acceptorSettings);
        second.StartAndActivate();
        SessionSnapshot secondInitiator = second.Initiator.GetSessionSnapshot();
        SessionSnapshot secondAcceptor = second.Acceptor.GetSessionSnapshot();
        AssertAccepted(second.Acceptor.SendMessage(ResendRequest(2, 3)));
        second.PumpUntil(() => second.InitiatorApplication.ReplayContexts.Count == 2 && second.AcceptorEvents.Count(item => item.Code == "possible-duplicate-ignored") == 2);
        AssertAccepted(second.Initiator.SendMessage(Order("ORDER-3")));
        second.PumpUntil(() => second.AcceptorApplication.ApplicationContexts.Count == 1);

        transport.Disconnect();
        second.PollInitiator();
        second.PollAcceptor();
        transport.Reconnect();

        RestartConnection third = Connection(transport, clock, initiatorSettings, acceptorSettings);
        third.StartAndActivate();
        SessionSnapshot thirdInitiator = third.Initiator.GetSessionSnapshot();
        SessionSnapshot thirdAcceptor = third.Acceptor.GetSessionSnapshot();
        SessionPollResult logout = third.Initiator.LogoutSession("restart-continuity-complete");
        third.InitiatorEvents.AddRange(logout.Events);
        third.PumpUntil(() => third.Initiator.GetSessionSnapshot().State == SessionState.Disconnected && third.Acceptor.GetSessionSnapshot().State == SessionState.Disconnected);

        FileMessageStore durableInitiator = new FileMessageStore(storeDirectory, "initiator", 0);
        FileMessageStore durableAcceptor = new FileMessageStore(storeDirectory, "acceptor", 0);
        StoreRangeResult initiatorState = durableInitiator.RefreshStore();
        StoreRangeResult acceptorState = durableAcceptor.RefreshStore();
        StoreRangeResult initiatorMessages = durableInitiator.ReadMessageRange(1, 0);
        StoreRangeResult acceptorMessages = durableAcceptor.ReadMessageRange(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(transport.ConnectionGeneration, Is.EqualTo(3));
            Assert.That(firstInitiator.NextSenderSeqNum, Is.EqualTo(4));
            Assert.That(firstInitiator.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(firstAcceptor.NextSenderSeqNum, Is.EqualTo(2));
            Assert.That(firstAcceptor.NextTargetSeqNum, Is.EqualTo(4));
            Assert.That(secondInitiator.NextSenderSeqNum, Is.EqualTo(5));
            Assert.That(secondInitiator.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(secondAcceptor.NextSenderSeqNum, Is.EqualTo(3));
            Assert.That(secondAcceptor.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(second.InitiatorEvents.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Logon && item.SequenceNumber == 4), Is.True);
            Assert.That(second.AcceptorEvents.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Logon && item.SequenceNumber == 2), Is.True);
            Assert.That(second.InitiatorApplication.ReplayContexts.Select(item => item.SequenceNumber), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(second.InitiatorApplication.ReplayContexts.Select(item => Text(item.Message, 11)), Is.EqualTo(new[] { "ORDER-1", "ORDER-2" }));
            Assert.That(second.InitiatorApplication.ReplayContexts.TrueForAll(item => item.IsReplay && item.IsPossibleDuplicate), Is.True);
            Assert.That(second.AcceptorApplication.ApplicationContexts.Select(item => item.SequenceNumber), Is.EqualTo(new[] { 5 }));
            Assert.That(thirdInitiator.NextSenderSeqNum, Is.EqualTo(7));
            Assert.That(thirdInitiator.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(thirdAcceptor.NextSenderSeqNum, Is.EqualTo(5));
            Assert.That(thirdAcceptor.NextTargetSeqNum, Is.EqualTo(7));
            Assert.That(third.InitiatorEvents.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Logon && item.SequenceNumber == 6), Is.True);
            Assert.That(third.AcceptorEvents.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message != null && item.Message.MsgType == FixAdminMessageTypes.Logon && item.SequenceNumber == 4), Is.True);
            AssertStoreState(initiatorState, 8, 6);
            AssertStoreState(acceptorState, 6, 8);
            Assert.That(initiatorMessages.Success, Is.True, initiatorMessages.Code + ": " + initiatorMessages.Text);
            Assert.That(acceptorMessages.Success, Is.True, acceptorMessages.Code + ": " + acceptorMessages.Text);
            Assert.That(initiatorMessages.Messages.Select(item => item.SequenceNumber), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(acceptorMessages.Messages.Select(item => item.SequenceNumber), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(initiatorMessages.HasGap, Is.False);
            Assert.That(acceptorMessages.HasGap, Is.False);
        });
    }

    private RestartConnection Connection(InMemoryDuplexTransportPair transport, FakeSessionClock clock, SessionSettings initiatorSettings, SessionSettings acceptorSettings)
    {
        return new RestartConnection(transport, clock, initiatorSettings, acceptorSettings, new FileMessageStore(storeDirectory, "initiator", 0), new FileMessageStore(storeDirectory, "acceptor", 0));
    }

    private SessionSettings Settings(SessionRole role, string sender, string target)
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.Role = role;
        settings.Id.SenderCompID = sender;
        settings.Id.TargetCompID = target;
        settings.CheckLatency = false;
        settings.StoreType = "File";
        settings.StorePath = storeDirectory;
        return settings;
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

    private static FixMessage ResendRequest(int beginSequenceNumber, int endSequenceNumber)
    {
        return new FixMessage
        {
            MsgType = FixAdminMessageTypes.ResendRequest,
            BodyFields =
            [
                FixSessionTests.Field(FixTags.BeginSeqNo, beginSequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                FixSessionTests.Field(FixTags.EndSeqNo, endSequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ]
        };
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField? field = EnvelopeValidator.FindFirstMessageField(message, tag);
        return field == null ? "" : Encoding.Latin1.GetString(field.Value);
    }

    private static void AssertAccepted(SessionSendResult result)
    {
        Assert.That(result.Accepted, Is.True, result.Code + ": " + result.Text);
    }

    private static void AssertStoreState(StoreRangeResult result, int senderSequenceNumber, int targetSequenceNumber)
    {
        Assert.That(result.Success, Is.True, result.Code + ": " + result.Text);
        Assert.That(result.HasSequenceState, Is.True);
        Assert.That(result.NextSenderSeqNum, Is.EqualTo(senderSequenceNumber));
        Assert.That(result.NextTargetSeqNum, Is.EqualTo(targetSequenceNumber));
    }

    private class RestartConnection
    {
        internal FixSession Initiator;
        internal FixSession Acceptor;
        internal RestartApplication InitiatorApplication = new RestartApplication();
        internal RestartApplication AcceptorApplication = new RestartApplication();
        internal List<SessionEvent> InitiatorEvents = [];
        internal List<SessionEvent> AcceptorEvents = [];

        internal RestartConnection(InMemoryDuplexTransportPair transport, FakeSessionClock clock, SessionSettings initiatorSettings, SessionSettings acceptorSettings, FileMessageStore initiatorStore, FileMessageStore acceptorStore)
        {
            EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
            Initiator = CreateSession(initiatorSettings, transport.First, initiatorStore, clock, InitiatorApplication, provider);
            Acceptor = CreateSession(acceptorSettings, transport.Second, acceptorStore, clock, AcceptorApplication, provider);
        }

        internal void StartAndActivate()
        {
            AcceptorEvents.AddRange(Acceptor.StartSession().Events);
            InitiatorEvents.AddRange(Initiator.StartSession().Events);
            PumpUntil(() => Initiator.GetSessionSnapshot().State == SessionState.Active && Acceptor.GetSessionSnapshot().State == SessionState.Active);
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

        private static FixSession CreateSession(SessionSettings settings, ISessionTransport transport, IMessageStore store, FakeSessionClock clock, RestartApplication application, EmbeddedDictionaryProvider provider)
        {
            return new FixSession(settings, transport, store, new FixSessionTests.RecordingFixLog(), clock, new TokenBucketMessageThrottle(clock), application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageValidator(), provider, new BasicSessionAuthenticator(settings, null));
        }
    }

    private class RestartApplication : ISessionApplication
    {
        internal List<ApplicationMessageContext> ApplicationContexts = [];
        internal List<ApplicationMessageContext> ReplayContexts = [];

        public ApplicationDecision OnSessionCreated(SessionId id)
        {
            return Accepted();
        }

        public ApplicationDecision OnSessionLogon(ApplicationMessageContext context)
        {
            return Accepted();
        }

        public ApplicationDecision OnSessionLogout(ApplicationMessageContext context)
        {
            return Accepted();
        }

        public ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context)
        {
            return Accepted();
        }

        public ApplicationDecision OnApplicationMessage(ApplicationMessageContext context)
        {
            ApplicationContexts.Add(context);
            return Accepted();
        }

        public ApplicationDecision OnReplayMessage(ApplicationMessageContext context)
        {
            ReplayContexts.Add(context);
            return Accepted();
        }

        private static ApplicationDecision Accepted()
        {
            return new ApplicationDecision { Disposition = MessageDisposition.Accept, Code = "accepted" };
        }
    }
}
