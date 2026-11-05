using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class PublicExceptionBehaviorTests
{
    [Test]
    public void StoreMisusePathsThrowFixStoreExceptionWithStableMessages()
    {
        string temporaryDirectory = Path.Combine(Path.GetTempPath(), "fix-exception-tests-" + Guid.NewGuid().ToString("N"));

        Assert.Multiple(() =>
        {
            AssertException<FixStoreException>(() => new FileMessageStore(null!, "store", 0), "The file store directory path is missing.");
            AssertException<FixStoreException>(() => new FileMessageStore(temporaryDirectory, "../store", 0), "The file store name contains invalid characters.");
            AssertException<FixStoreException>(() => new FileMessageStore(temporaryDirectory, "store", -1), "The maximum retained message count cannot be negative.");
            AssertException<FixStoreException>(() => new MemoryMessageStore(-1), "The maximum retained message count cannot be negative.");
            AssertException<FixStoreException>(() => new FileFixLog("", true), "The FIX audit log path is missing.");
            FileFixLog log = new FileFixLog(Path.Combine(temporaryDirectory, "session.log"), true);
            AssertException<FixStoreException>(() => log.WriteLog(null!), "The FIX audit log entry is missing.");
        });

        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, true);
        }
    }

    [Test]
    public void TransportMisusePathsThrowFixTransportExceptionWithStableMessages()
    {
        ReadOnlyStream readOnly = new ReadOnlyStream();
        MemoryStream invalidRoleStream = new MemoryStream();
        SessionSettings invalidSettings = new SessionSettings { ConnectTimeoutMilliseconds = 0 };

        Assert.Multiple(() =>
        {
            AssertException<FixTransportException>(() => new StreamSessionTransport(null!), "The connected transport stream is missing.");
            AssertException<FixTransportException>(() => new StreamSessionTransport(readOnly), "The connected transport stream must be readable and writable.");
            AssertException<FixTransportException>(() => new TlsSessionTransport(null!, SessionRole.Initiator), "The connected TLS stream is missing.");
            AssertException<FixTransportException>(() => new TlsSessionTransport(readOnly, SessionRole.Initiator), "The connected TLS stream must be readable and writable.");
            AssertException<FixTransportException>(() => new TlsSessionTransport(invalidRoleStream, (SessionRole)99), "The TLS transport role is invalid.");
            AssertException<FixTransportException>(() => new TcpInitiatorTransport(null!), "The TCP initiator settings are missing.");
            AssertException<FixTransportException>(() => new TcpInitiatorTransport(invalidSettings), "The TCP connection timeout must be positive.");
            AssertException<FixTransportException>(() => new TcpAcceptorTransport(null!), "The TCP acceptor settings are missing.");
            AssertException<FixTransportException>(() => new TcpAcceptorTransport(invalidSettings), "The TCP accept timeout must be positive.");
        });

        Assert.That(readOnly.DisposeCount, Is.Zero);
        Assert.That(invalidRoleStream.CanRead, Is.True);
    }

    [Test]
    public void SessionAndRuleMisusePathsThrowTheirPublishedExceptionTypes()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(new FixDictionary());
        FixMemberDefinition member = new FixMemberDefinition();
        FixMessage message = new FixMessage();
        ValidationContext context = new ValidationContext();

        Assert.Multiple(() =>
        {
            AssertException<FixSessionException>(() => new BasicSessionAuthenticator(null!, null), "The basic authentication settings are missing.");
            AssertException<FixSessionException>(() => new TokenBucketMessageThrottle(null!), "The message throttle clock is missing.");
            AssertException<FixSessionException>(() => new FixSessionEngine(null!, fixture.Application, fixture.Provider, null), "A FixSessionEngine dependency is missing.");
            AssertException<FixSessionException>(() => new FixSession(null!, fixture.Transport, fixture.Store, fixture.Log, fixture.Clock, fixture.Throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, fixture.Provider, fixture.Authenticator), "FixSession settings are missing.");
            AssertArgumentNull(() => new OrchestraRuleEvaluator(null!), "dictionary");
            AssertArgumentNull(() => evaluator.EvaluateRule(null!, message, context), "member");
            AssertArgumentNull(() => evaluator.EvaluateRule(member, null!, context), "message");
            AssertArgumentNull(() => evaluator.EvaluateRule(member, message, null!), "context");
        });
    }

    [Test]
    public void EngineMethodMisusePathsThrowWithoutCorruptingRegistrationState()
    {
        FixSessionEngine engine = MultiSessionTests.CreateEngine();
        SessionSettings settings = MultiSessionTests.InactiveSettings("EXCEPTIONS", "REMOTE", "PRIMARY");
        SessionSettings missingPersistence = MultiSessionTests.InactiveSettings("MISSING-PERSISTENCE", "REMOTE", "PRIMARY");
        missingPersistence.StoreType = null!;
        SessionSettings incompleteIdentity = MultiSessionTests.InactiveSettings("INCOMPLETE", "REMOTE", "PRIMARY");
        incompleteIdentity.Id.SenderSubID = null!;
        SessionSettings unsupportedStore = MultiSessionTests.InactiveSettings("BAD-STORE", "REMOTE", "PRIMARY");
        unsupportedStore.StoreType = "unsupported";
        SessionSettings unsupportedLog = MultiSessionTests.InactiveSettings("BAD-LOG", "REMOTE", "PRIMARY");
        unsupportedLog.LogType = "unsupported";

        AssertException<FixSessionException>(() => engine.AddSession(null!), "The session engine settings are missing.");
        AssertException<FixSessionException>(() => engine.AddSession(missingPersistence), "The session persistence configuration is missing.");
        AssertException<FixSessionException>(() => engine.AddSession(incompleteIdentity), "The complete session identity is missing.");
        AssertException<FixSessionException>(() => engine.AddSession(unsupportedStore), "The configured message store type is unsupported: unsupported");
        AssertException<FixSessionException>(() => engine.AddSession(unsupportedLog), "The configured FIX log type is unsupported: unsupported");
        engine.AddSession(settings);
        AssertException<FixSessionException>(() => engine.AddSession(settings), "A session with the same complete identity is already registered.");
        AssertException<FixSessionException>(() => engine.RemoveSession(new SessionId()), "The requested session is not registered.");
        AssertException<FixSessionException>(() => engine.ResolveSessionSnapshot(new SessionId()), "The requested session is not registered.");

        Assert.That(engine.Sessions, Has.Count.EqualTo(1));
        Assert.That(engine.ResolveSessionSnapshot(settings.Id).Id.SenderCompID, Is.EqualTo("EXCEPTIONS"));
    }

    [Test]
    public void InvalidSessionImplementationResultsUseExceptionsOnlyForRequiredEngineOutcomes()
    {
        FixSessionEngine engine = MultiSessionTests.CreateEngine();
        SessionId id = new SessionId
        {
            BeginString = FixBeginStrings.Fix44,
            SenderCompID = "BROKEN",
            TargetCompID = "REMOTE",
            Qualifier = "PRIMARY"
        };
        engine.Sessions.Add(new FixSessionEngineEntry(id, new MissingOutcomeSession()));

        SessionPollResult poll = engine.PollSessions();
        AssertException<FixSessionException>(() => engine.ResolveSessionSnapshot(id), "The requested session did not return a snapshot.");
        AssertException<FixSessionException>(() => engine.RemoveSession(id), "The removed session did not return a stop result.");

        Assert.Multiple(() =>
        {
            Assert.That(poll.Events, Has.Count.EqualTo(1));
            Assert.That(poll.Events[0].Code, Is.EqualTo("missing-session-poll-result"));
            Assert.That(poll.Events[0].Kind, Is.EqualTo(SessionEventKind.Error));
            Assert.That(engine.Sessions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void EmbeddedDictionaryCorruptionPathsThrowFixDictionaryException()
    {
        using MemoryStream invalidFormat = new MemoryStream();
        using (GZipStream compression = new GZipStream(invalidFormat, CompressionLevel.SmallestSize, true))
        using (BinaryWriter writer = new BinaryWriter(compression, Encoding.UTF8, true))
        {
            writer.Write("FIXDICT");
            writer.Write(2);
        }
        invalidFormat.Position = 0;

        using MemoryStream invalidCount = new MemoryStream();
        using BinaryWriter countWriter = new BinaryWriter(invalidCount, Encoding.UTF8, true);
        countWriter.Write(-1);
        countWriter.Flush();
        invalidCount.Position = 0;
        using BinaryReader countReader = new BinaryReader(invalidCount, Encoding.UTF8, true);

        Assert.Multiple(() =>
        {
            AssertException<FixDictionaryException>(() => EmbeddedDictionaryProvider.ResolveEmbeddedSnapshot("missing-resource.fixdict.gz"), "The embedded dictionary resource is missing: missing-resource.fixdict.gz");
            AssertException<FixDictionaryException>(() => EmbeddedDictionaryFormat.ReadResource(invalidFormat), "The embedded dictionary resource has an unsupported format.");
            AssertException<FixDictionaryException>(() => EmbeddedDictionaryFormat.ReadDefinitionCount(countReader), "The embedded dictionary resource contains an invalid definition count.");
        });
    }

    [Test]
    public void ExpectedProtocolValidationAndContentFailuresRemainStructuredResults()
    {
        byte[] malformed = Encoding.ASCII.GetBytes("not-a-fix-message");
        FixDictionary dictionary = new FixDictionary();
        FrameResult frame = new TagValueMessageFramer().ReadFrame(malformed, new FrameOptions());
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(malformed, dictionary, new DecodeOptions());
        EncodeResult encoded = new TagValueMessageEncoder().EncodeMessage(new FixMessage(), dictionary, new byte[32], new EncodeOptions());
        FieldParseResult field = new FixFieldValueCodec().DecodeFieldValue(Encoding.ASCII.GetBytes("not-an-int"), 38, FixFieldType.Int, new DecodeOptions());
        ValidationResult validation = new FixMessageValidator().ValidateMessage(new FixMessage(), dictionary, new ValidationOptions(), new ValidationContext());
        using MemoryStream quickFixSource = new MemoryStream(Encoding.UTF8.GetBytes("<broken"));
        using MemoryStream orchestraSource = new MemoryStream(Encoding.UTF8.GetBytes("<broken"));
        using MemoryStream repositorySource = new MemoryStream(Encoding.UTF8.GetBytes("<broken"));
        DictionaryLoadResult quickFix = new QuickFixDictionaryLoader().LoadDictionary(quickFixSource, new FixDictionaryOptions());
        DictionaryLoadResult orchestraDictionary = new OrchestraDictionaryLoader().LoadDictionary(orchestraSource, new FixDictionaryOptions());
        OrchestraReadResult repository = new OrchestraReader().ReadRepository(repositorySource, new OrchestraReadOptions { ValidateSchema = false });
        OrchestraReadResult semantic = new OrchestraValidator().ValidateRepository(null!);
        GenerationResult models = new CSharpModelGenerator().GenerateModels(null!, new ModelGenerationOptions());
        GenerationResult schema = new SbeSchemaGenerator().GenerateSbeSchema(null!, new SbeGenerationOptions());
        StoreRangeResult store = new MemoryMessageStore(0).StoreMessage(new StoredMessage());
        AuthenticationResult authentication = new BasicSessionAuthenticator(FixSessionTests.Settings(false), null).AuthenticateSession(null!);
        ApplicationDecision throttle = new TokenBucketMessageThrottle(new FakeSessionClock(FixSessionTests.Now)).CheckMessageThrottle(new ApplicationMessageContext(), null!);
        SessionSendResult send = new FixSessionTests.SessionFixture(false).Session.SendMessage(null!);
        StreamSessionTransport transport = new StreamSessionTransport(new MemoryStream());
        transport.CloseTransport();
        TransportWriteResult write = transport.WriteTransport([1]);
        FixMemberDefinition rule = new FixMemberDefinition { PresenceRule = "$market.missing == 1", Presence = FixPresence.Required };
        RuleEvaluationResult evaluation = new OrchestraRuleEvaluator(dictionary).EvaluateRule(rule, new FixMessage(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(frame.Status, Is.Not.EqualTo(FrameStatus.Complete));
            Assert.That(decoded.Status, Is.Not.EqualTo(DecodeStatus.Success));
            Assert.That(encoded.Status, Is.Not.EqualTo(EncodeStatus.Success));
            Assert.That(field.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(validation.IsValid, Is.False);
            Assert.That(quickFix.Success, Is.False);
            Assert.That(quickFix.Diagnostics, Is.Not.Empty);
            Assert.That(orchestraDictionary.Success, Is.False);
            Assert.That(orchestraDictionary.Diagnostics, Is.Not.Empty);
            Assert.That(repository.Success, Is.False);
            Assert.That(repository.Diagnostics, Is.Not.Empty);
            Assert.That(semantic.Success, Is.False);
            Assert.That(semantic.Diagnostics[0].Code, Is.EqualTo("missing-orchestra-repository"));
            Assert.That(models.Success, Is.False);
            Assert.That(models.Diagnostics[0].Code, Is.EqualTo("missing-generation-dictionary"));
            Assert.That(schema.Success, Is.False);
            Assert.That(schema.Diagnostics[0].Code, Is.EqualTo("missing-sbe-dictionary"));
            Assert.That(store.Success, Is.False);
            Assert.That(store.Code, Is.EqualTo("invalid-message-sequence-number"));
            Assert.That(authentication.Authenticated, Is.False);
            Assert.That(authentication.Code, Is.EqualTo("missing-authentication-request"));
            Assert.That(throttle.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(throttle.Code, Is.EqualTo("missing-throttle-settings"));
            Assert.That(send.Accepted, Is.False);
            Assert.That(send.Code, Is.EqualTo("missing-send-message"));
            Assert.That(write.ErrorCode, Is.EqualTo("transport-closed"));
            Assert.That(evaluation.WasEvaluated, Is.False);
            Assert.That(evaluation.ErrorCode, Is.EqualTo("external-state-unavailable"));
        });
    }

    private static void AssertException<TException>(TestDelegate action, string message) where TException : Exception
    {
        TException exception = Assert.Throws<TException>(action)!;
        Assert.That(exception.Message, Is.EqualTo(message));
    }

    private static void AssertArgumentNull(TestDelegate action, string parameterName)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action)!;
        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    private class MissingOutcomeSession : IFixSession
    {
        public SessionPollResult StartSession()
        {
            return null!;
        }

        public SessionPollResult PollSession()
        {
            return null!;
        }

        public SessionSendResult SendMessage(FixMessage message)
        {
            return new SessionSendResult();
        }

        public SessionPollResult LogoutSession(string text)
        {
            return null!;
        }

        public SessionPollResult StopSession()
        {
            return null!;
        }

        public SessionSnapshot GetSessionSnapshot()
        {
            return null!;
        }
    }

    private class ReadOnlyStream : MemoryStream
    {
        internal int DisposeCount;

        public override bool CanWrite => false;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }
            base.Dispose(disposing);
        }
    }
}
