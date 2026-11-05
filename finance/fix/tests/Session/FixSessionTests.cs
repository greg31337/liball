using System;
using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FixSessionTests
{
    internal static readonly DateTime Now = new DateTime(2026, 8, 30, 18, 0, 0, DateTimeKind.Utc);

    [Test]
    public void StartLoadsStateOpensTransportAndQueuesInitiatorLogon()
    {
        SessionFixture fixture = new SessionFixture(false);

        SessionPollResult started = fixture.Session.StartSession();

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SessionState.LogonPending), string.Join(",", started.Events.ConvertAll(item => item.Code + ":" + item.Text)));
            Assert.That(started.Events.ConvertAll(item => item.Kind), Is.EqualTo(new[] { SessionEventKind.Started, SessionEventKind.Connected }));
            Assert.That(started.WorkRemaining, Is.True);
            Assert.That(started.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(started.Snapshot.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(fixture.Application.CreatedCount, Is.EqualTo(1));
            Assert.That(fixture.Transport.OpenCount, Is.EqualTo(1));
            Assert.That(fixture.Store.ReadMessageRange(1, 1).Messages, Has.Count.EqualTo(1));
            Assert.That(fixture.Log.Entries.Exists(item => item.EventKind == SessionEventKind.Started), Is.True);
        });
    }

    [Test]
    public void PartialWritesDoNotConsumeSequenceUntilCompleteFrameIsWritten()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.Transport.MaximumWrite = 12;
        fixture.Session.StartSession();

        SessionPollResult first = fixture.Session.PollSession();
        int polls = 1;
        while (fixture.Session.GetSessionSnapshot().OutboundQueueCount != 0 && polls < 100)
        {
            fixture.Session.PollSession();
            polls++;
        }

        DecodeResult sent = fixture.DecodeSingle(fixture.Transport.OutboundBytes.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(first.Events.Exists(item => item.Kind == SessionEventKind.Backpressure), Is.True);
            Assert.That(polls, Is.GreaterThan(1));
            Assert.That(fixture.Session.GetSessionSnapshot().NextSenderSeqNum, Is.EqualTo(2));
            Assert.That(sent.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(sent.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.Logon));
            Assert.That(fixture.Log.Entries.Exists(item => item.Direction == LogDirection.Outgoing), Is.True);
        });
    }

    [Test]
    public void InboundLogonActivatesTimersAndOrdersAdministrativeBeforeLifecycleCallback()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.StartAndSendLogon();

        fixture.Transport.AddInbound(fixture.CreatePeerLogon(1));
        SessionPollResult active = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(active.State, Is.EqualTo(SessionState.Active), string.Join(",", active.Events.ConvertAll(item => item.Code + ":" + item.Text)));
            Assert.That(active.Events.ConvertAll(item => item.Kind), Does.Contain(SessionEventKind.Logon));
            Assert.That(active.Snapshot.IsLoggedOn, Is.True);
            Assert.That(active.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(active.Snapshot.HasLastInboundTime, Is.True);
            Assert.That(fixture.Application.CallbackOrder, Is.EqualTo(new[] { "created", "administrative", "logon" }));
            Assert.That(fixture.Log.Entries.Exists(item => item.Direction == LogDirection.Incoming), Is.True);
        });
    }

    [Test]
    public void SendValidatesThrottlesStoresAndQueuesApplicationMessage()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.Activate();
        FixMessage order = new FixMessage { MsgType = "D" };
        order.BodyFields.Add(Field(11, "ORDER-1"));

        SessionSendResult queued = fixture.Session.SendMessage(order);
        StoreRangeResult stored = fixture.Store.ReadMessageRange(2, 2);
        SessionPollResult sent = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(queued.Accepted, Is.True);
            Assert.That(queued.SequenceNumber, Is.EqualTo(2));
            Assert.That(queued.QueueCount, Is.EqualTo(1));
            Assert.That(fixture.Throttle.Contexts, Has.Count.EqualTo(1));
            Assert.That(fixture.Validator.Messages.Exists(item => item.MsgType == "D"), Is.True);
            Assert.That(stored.Messages, Has.Count.EqualTo(1));
            Assert.That(stored.Messages[0].IsAdministrative, Is.False);
            Assert.That(sent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent), Is.True);
            Assert.That(sent.Snapshot.NextSenderSeqNum, Is.EqualTo(3));
            Assert.That(order.HeaderFields, Is.Empty);
        });
    }

    [Test]
    public void PreLogonQueueWaitsForActivationAndThrottleRejectionDoesNotQueue()
    {
        SessionFixture fixture = new SessionFixture(true);
        SessionPollResult started = fixture.Session.StartSession();
        FixMessage early = new FixMessage { MsgType = "D" };

        SessionSendResult waiting = fixture.Session.SendMessage(early);
        fixture.Session.PollSession();
        fixture.Transport.AddInbound(fixture.CreatePeerLogon(1));
        fixture.Session.PollSession();
        SessionPollResult sent = fixture.Session.PollSession();
        fixture.Throttle.Decision = Decision(MessageDisposition.Reject, "throttle-stop", "full");
        SessionSendResult rejected = fixture.Session.SendMessage(new FixMessage { MsgType = "F" });

        Assert.Multiple(() =>
        {
            Assert.That(started.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(waiting.Accepted, Is.True);
            Assert.That(waiting.SequenceNumber, Is.Zero);
            Assert.That(waiting.Code, Is.EqualTo("message-queued-before-logon"));
            Assert.That(sent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent && item.Message!.MsgType == "D"), Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Code, Is.EqualTo("throttle-stop"));
            Assert.That(fixture.Session.GetSessionSnapshot().OutboundQueueCount, Is.Zero);
        });
    }

    [Test]
    public void BuiltInThrottleBoundsPreLogonApplicationQueueWithoutCountingAdministrativeMessages()
    {
        SessionFixture fixture = new SessionFixture(true);
        fixture.Settings.Throttle.MaxMessages = 10;
        fixture.Settings.Throttle.IntervalMilliseconds = 1000;
        fixture.Settings.Throttle.BurstMessages = 0;
        fixture.Settings.Throttle.QueueCapacity = 1;
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(fixture.Clock);
        fixture.Session = new FixSession(fixture.Settings, fixture.Transport, fixture.Store, fixture.Log, fixture.Clock, throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, fixture.Provider, fixture.Authenticator);
        fixture.Session.StartSession();

        SessionSendResult first = fixture.Session.SendMessage(new FixMessage { MsgType = "D" });
        SessionSendResult full = fixture.Session.SendMessage(new FixMessage { MsgType = "F" });
        SessionSnapshot snapshot = fixture.Session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(first.Code, Is.EqualTo("message-queued-before-logon"));
            Assert.That(full.Accepted, Is.False);
            Assert.That(full.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(full.Code, Is.EqualTo("message-queue-full"));
            Assert.That(snapshot.OutboundQueueCount, Is.EqualTo(2));
            Assert.That(throttle.AvailableTokens, Is.EqualTo(9m));
        });
    }

    [Test]
    public void InboundApplicationMessageConsumesSequenceAndHonorsApplicationDecision()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.Activate();
        FixMessage execution = fixture.PeerFactory.CreateStandardMessage("8", 2);
        execution.BodyFields.Add(Field(17, "EXEC-1"));
        fixture.Transport.AddInbound(fixture.Encode(execution));

        SessionPollResult received = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(received.Events.Exists(item => item.Kind == SessionEventKind.MessageReceived && item.Message!.MsgType == "8"), Is.True);
            Assert.That(received.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(fixture.Application.ApplicationContexts, Has.Count.EqualTo(1));
            Assert.That(fixture.Application.ApplicationContexts[0].SequenceNumber, Is.EqualTo(2));
            Assert.That(fixture.Application.ApplicationContexts[0].IsAdministrative, Is.False);
        });
    }

    [Test]
    public void PollGeneratesTestRequestAndGracefulLogoutClosesAfterPeerResponse()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.Activate();
        fixture.Clock.AdvanceClock(TimeSpan.FromSeconds(30));

        SessionPollResult timer = fixture.Session.PollSession();
        fixture.Session.PollSession();
        SessionPollResult logout = fixture.Session.LogoutSession("finished");
        fixture.Session.PollSession();
        FixMessage peerLogout = fixture.PeerFactory.CreateLogoutMessage(2, "finished");
        fixture.Transport.AddInbound(fixture.Encode(peerLogout));
        SessionPollResult completed = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(timer.Events.Exists(item => item.Kind == SessionEventKind.TestRequest), Is.True);
            Assert.That(timer.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(logout.State, Is.EqualTo(SessionState.LogoutPending));
            Assert.That(completed.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(completed.Snapshot.IsLoggedOn, Is.False);
            Assert.That(fixture.Transport.CloseCount, Is.EqualTo(1));
            Assert.That(fixture.Application.LogoutCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SequenceGapQueuesResendAndGapFillReleasesApplicationMessage()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.Activate();
        FixMessage high = fixture.PeerFactory.CreateStandardMessage("8", 4);
        high.BodyFields.Add(Field(17, "QUEUED"));
        fixture.Transport.AddInbound(fixture.Encode(high));

        SessionPollResult gap = fixture.Session.PollSession();
        fixture.Session.PollSession();
        FixMessage fill = fixture.PeerFactory.CreateSequenceResetMessage(2, 4, true);
        fixture.Transport.AddInbound(fixture.Encode(fill));
        SessionPollResult recovered = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(gap.State, Is.EqualTo(SessionState.Recovering), string.Join(",", gap.Events.ConvertAll(item => item.Code + ":" + item.Text)));
            Assert.That(gap.Events.Exists(item => item.Kind == SessionEventKind.SequenceGap), Is.True);
            Assert.That(gap.Snapshot.InboundQueueCount, Is.EqualTo(1));
            Assert.That(gap.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(recovered.State, Is.EqualTo(SessionState.Active));
            Assert.That(recovered.Events.Exists(item => item.Kind == SessionEventKind.RecoveryCompleted), Is.True);
            Assert.That(recovered.Snapshot.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(recovered.Snapshot.InboundQueueCount, Is.Zero);
            Assert.That(fixture.Application.ApplicationContexts, Has.Count.EqualTo(1));
            Assert.That(fixture.Application.ApplicationContexts[0].SequenceNumber, Is.EqualTo(4));
        });
    }

    [Test]
    public void StopClosesTransportClearsQueuesAndConstructorRejectsMissingInputs()
    {
        SessionFixture fixture = new SessionFixture(false);
        fixture.Session.StartSession();

        SessionPollResult stopped = fixture.Session.StopSession();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(stopped.Snapshot.OutboundQueueCount, Is.Zero);
            Assert.That(fixture.Transport.CloseCount, Is.EqualTo(1));
            Assert.That(stopped.Events.Exists(item => item.Kind == SessionEventKind.Stopped), Is.True);
            Assert.Throws<FixSessionException>(() => new FixSession(fixture.Settings, null!, fixture.Store, fixture.Log, fixture.Clock, fixture.Throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, fixture.Provider, fixture.Authenticator));
            SessionSettings noEndpoint = Settings(false);
            noEndpoint.Endpoints.Clear();
            Assert.Throws<FixSessionException>(() => new FixSession(noEndpoint, fixture.Transport, fixture.Store, fixture.Log, fixture.Clock, fixture.Throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, fixture.Provider, fixture.Authenticator));
        });
    }

    [Test]
    public void BuiltInDictionaryValidatorAndThrottleAcceptGeneratedLogon()
    {
        SessionSettings settings = Settings(false);
        FakeSessionTransport transport = new FakeSessionTransport();
        MemoryMessageStore store = new MemoryMessageStore(0);
        RecordingFixLog log = new RecordingFixLog();
        FakeSessionClock clock = new FakeSessionClock(Now);
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        FixSession session = new FixSession(settings, transport, store, log, clock, new TokenBucketMessageThrottle(clock), new RecordingSessionApplication(), new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageValidator(), provider, new BasicSessionAuthenticator(settings, null));

        SessionPollResult started = session.StartSession();
        SessionPollResult sent = session.PollSession();
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(transport.OutboundBytes.ToArray(), provider.ResolveDictionary(settings.TransportDictionary).Dictionary!, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SessionState.LogonPending), string.Join(",", started.Events.ConvertAll(item => item.Code + ":" + item.Text)));
            Assert.That(sent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent), Is.True);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(decoded.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.Logon));
        });
    }

    [Test]
    public void BuiltInFixtProfileResolvesTransportAndApplicationDictionaries()
    {
        SessionSettings settings = Settings(false);
        settings.Id.BeginString = FixBeginStrings.Fixt11;
        settings.TransportDictionary.BeginString = FixBeginStrings.Fixt11;
        settings.ApplicationDictionary.BeginString = "";
        settings.ApplicationDictionary.ApplicationVersion = FixApplicationVersions.Fix50Sp2;
        settings.DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2;
        FakeSessionTransport transport = new FakeSessionTransport();
        MemoryMessageStore store = new MemoryMessageStore(0);
        RecordingFixLog log = new RecordingFixLog();
        FakeSessionClock clock = new FakeSessionClock(Now);
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        FixSession session = new FixSession(settings, transport, store, log, clock, new TokenBucketMessageThrottle(clock), new RecordingSessionApplication(), new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageValidator(), provider, new BasicSessionAuthenticator(settings, null));

        SessionPollResult started = session.StartSession();
        SessionPollResult sent = session.PollSession();
        FixDictionary transportDictionary = provider.ResolveDictionary(settings.TransportDictionary).Dictionary!;
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(transport.OutboundBytes.ToArray(), transportDictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(started.State, Is.EqualTo(SessionState.LogonPending));
            Assert.That(sent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent), Is.True);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(decoded.Message!.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(Encoding.Latin1.GetString(EnvelopeValidator.FindFirstMessageField(decoded.Message, FixTags.DefaultApplVerID)!.Value), Is.EqualTo(FixApplicationVersions.Fix50Sp2));
        });
    }

    internal static SessionSettings Settings(bool allowBeforeLogon)
    {
        SessionSettings settings = new SessionSettings();
        settings.Id.BeginString = FixBeginStrings.Fix44;
        settings.Id.SenderCompID = "LOCAL";
        settings.Id.TargetCompID = "REMOTE";
        settings.Role = SessionRole.Initiator;
        settings.AllowSendBeforeLogon = allowBeforeLogon;
        settings.HeartbeatIntervalSeconds = 30;
        settings.CheckLatency = true;
        settings.MaxLatencySeconds = 120;
        settings.TransportDictionary.BeginString = FixBeginStrings.Fix44;
        settings.ApplicationDictionary.BeginString = FixBeginStrings.Fix44;
        settings.Endpoints.Add(new SessionEndpoint { Host = "peer", Port = 9876 });
        return settings;
    }

    internal static SessionSettings PeerSettings()
    {
        SessionSettings settings = Settings(false);
        settings.Id.SenderCompID = "REMOTE";
        settings.Id.TargetCompID = "LOCAL";
        settings.Role = SessionRole.Acceptor;
        return settings;
    }

    internal static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    internal static ApplicationDecision Decision(MessageDisposition disposition, string code, string text)
    {
        return new ApplicationDecision { Disposition = disposition, Code = code, Text = text };
    }

    internal class SessionFixture
    {
        internal SessionSettings Settings;
        internal FakeSessionClock Clock = new FakeSessionClock(Now);
        internal FakeSessionTransport Transport = new FakeSessionTransport();
        internal MemoryMessageStore Store = new MemoryMessageStore(0);
        internal RecordingFixLog Log = new RecordingFixLog();
        internal RecordingThrottle Throttle = new RecordingThrottle();
        internal RecordingSessionApplication Application = new RecordingSessionApplication();
        internal TagValueMessageFramer Framer = new TagValueMessageFramer();
        internal TagValueMessageDecoder Decoder = new TagValueMessageDecoder();
        internal TagValueMessageEncoder Encoder = new TagValueMessageEncoder();
        internal AcceptingMessageValidator Validator = new AcceptingMessageValidator();
        internal EmptyDictionaryProvider Provider = new EmptyDictionaryProvider();
        internal AcceptingAuthenticator Authenticator = new AcceptingAuthenticator();
        internal AdminMessageFactory PeerFactory;
        internal FixSession Session;

        internal SessionFixture(bool allowBeforeLogon)
        {
            Settings = FixSessionTests.Settings(allowBeforeLogon);
            PeerFactory = new AdminMessageFactory(PeerSettings(), Clock);
            Session = new FixSession(Settings, Transport, Store, Log, Clock, Throttle, Application, Framer, Decoder, Encoder, Validator, Provider, Authenticator);
        }

        internal void StartAndSendLogon()
        {
            Session.StartSession();
            Session.PollSession();
        }

        internal void Activate()
        {
            StartAndSendLogon();
            Transport.AddInbound(CreatePeerLogon(1));
            SessionPollResult result = Session.PollSession();
            Assert.That(result.State, Is.EqualTo(SessionState.Active), string.Join(",", result.Events.ConvertAll(item => item.Code + ":" + item.Text)));
            Transport.OutboundBytes.Clear();
        }

        internal byte[] CreatePeerLogon(int sequenceNumber)
        {
            byte[] bytes = Encode(PeerFactory.CreateLogonMessage(sequenceNumber, 2, false));
            DecodeResult decoded = DecodeSingle(bytes);
            Assert.That(Encoding.Latin1.GetString(EnvelopeValidator.FindFirstMessageField(decoded.Message!, FixTags.SenderCompID)!.Value), Is.EqualTo("REMOTE"));
            return bytes;
        }

        internal byte[] Encode(FixMessage message)
        {
            byte[] destination = new byte[FixDefaults.MaxMessageSize];
            EncodeResult result = Encoder.EncodeMessage(message, Provider.Dictionary, destination, new EncodeOptions());
            Assert.That(result.Status, Is.EqualTo(EncodeStatus.Success));
            byte[] bytes = new byte[result.BytesWritten];
            Array.Copy(destination, bytes, result.BytesWritten);
            return bytes;
        }

        internal DecodeResult DecodeSingle(byte[] bytes)
        {
            return Decoder.DecodeMessage(bytes, Provider.Dictionary, new DecodeOptions());
        }
    }

    internal class FakeSessionTransport : ISessionTransport
    {
        internal List<byte> InboundBytes = [];
        internal List<byte> OutboundBytes = [];
        internal int MaximumWrite = int.MaxValue;
        internal int OpenCount;
        internal int CloseCount;
        internal TransportState State = TransportState.Disconnected;

        public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
        {
            OpenCount++;
            State = TransportState.Connected;
            return new TransportWriteResult { State = State };
        }

        public TransportReadResult ReadTransport(Span<byte> destination)
        {
            TransportReadResult result = new TransportReadResult { State = State };
            if (InboundBytes.Count == 0)
            {
                result.WouldBlock = true;
                return result;
            }
            int count = InboundBytes.Count < destination.Length ? InboundBytes.Count : destination.Length;
            for (int index = 0; index < count; index++) destination[index] = InboundBytes[index];
            InboundBytes.RemoveRange(0, count);
            result.BytesRead = count;
            return result;
        }

        public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
        {
            int count = source.Length < MaximumWrite ? source.Length : MaximumWrite;
            for (int index = 0; index < count; index++) OutboundBytes.Add(source[index]);
            return new TransportWriteResult { State = State, BytesWritten = count, WouldBlock = count < source.Length };
        }

        public TransportWriteResult CloseTransport()
        {
            CloseCount++;
            State = TransportState.Closed;
            return new TransportWriteResult { State = State };
        }

        internal void AddInbound(byte[] bytes)
        {
            InboundBytes.AddRange(bytes);
        }
    }

    internal class EmptyDictionaryProvider : IDictionaryProvider
    {
        internal FixDictionary Dictionary;

        internal EmptyDictionaryProvider()
        {
            DictionaryLoadResult loaded = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 });
            Dictionary = loaded.Dictionary!;
        }

        public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
        {
            return new DictionaryLoadResult { Success = true, Dictionary = Dictionary };
        }
    }

    internal class AcceptingMessageValidator : IMessageValidator
    {
        internal List<FixMessage> Messages = [];

        public ValidationResult ValidateMessage(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
        {
            Messages.Add(message);
            return new ValidationResult { IsValid = true };
        }
    }

    internal class AcceptingAuthenticator : ISessionAuthenticator
    {
        public AuthenticationResult AuthenticateSession(AuthenticationRequest request)
        {
            return new AuthenticationResult { Authenticated = true, Code = "accepted" };
        }
    }

    internal class RecordingThrottle : IMessageThrottle
    {
        internal List<ApplicationMessageContext> Contexts = [];
        internal ApplicationDecision Decision = FixSessionTests.Decision(MessageDisposition.Accept, "accepted", "");

        public ApplicationDecision CheckMessageThrottle(ApplicationMessageContext context, SessionThrottleSettings settings)
        {
            Contexts.Add(context);
            return Decision;
        }
    }

    internal class RecordingFixLog : IFixLog
    {
        internal List<LogEntry> Entries = [];

        public void WriteLog(LogEntry entry)
        {
            Entries.Add(entry);
        }
    }

    internal class RecordingSessionApplication : ISessionApplication
    {
        internal List<string> CallbackOrder = [];
        internal List<ApplicationMessageContext> ApplicationContexts = [];
        internal int CreatedCount;
        internal int LogoutCount;
        internal ApplicationDecision Decision = FixSessionTests.Decision(MessageDisposition.Accept, "accepted", "");

        public ApplicationDecision OnSessionCreated(SessionId id)
        {
            CreatedCount++;
            CallbackOrder.Add("created");
            return Decision;
        }

        public ApplicationDecision OnSessionLogon(ApplicationMessageContext context)
        {
            CallbackOrder.Add("logon");
            return Decision;
        }

        public ApplicationDecision OnSessionLogout(ApplicationMessageContext context)
        {
            LogoutCount++;
            CallbackOrder.Add("logout");
            return Decision;
        }

        public ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context)
        {
            CallbackOrder.Add("administrative");
            return Decision;
        }

        public ApplicationDecision OnApplicationMessage(ApplicationMessageContext context)
        {
            ApplicationContexts.Add(context);
            CallbackOrder.Add("application");
            return Decision;
        }

        public ApplicationDecision OnReplayMessage(ApplicationMessageContext context)
        {
            CallbackOrder.Add("replay");
            return Decision;
        }
    }
}
