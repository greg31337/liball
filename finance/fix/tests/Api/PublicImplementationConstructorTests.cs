using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class PublicImplementationConstructorTests
{
    private string TemporaryDirectory = "";

    [SetUp]
    public void CreateTemporaryDirectory()
    {
        TemporaryDirectory = Path.Combine(Path.GetTempPath(), "fix-constructor-tests-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void DeleteTemporaryDirectory()
    {
        if (Directory.Exists(TemporaryDirectory))
        {
            Directory.Delete(TemporaryDirectory, true);
        }
    }

    [Test]
    public void EveryPublicImplementationConstructorAcceptsValidInputs()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        MemoryStream stream = new MemoryStream();
        MemoryStream tlsStream = new MemoryStream();
        StreamSessionTransport streamTransport = new StreamSessionTransport(stream);
        TlsSessionTransport tlsTransport = new TlsSessionTransport(tlsStream, SessionRole.Initiator);
        SessionSettings transportSettings = FixSessionTests.Settings(false);
        List<object> implementations =
        [
            new BasicSessionAuthenticator(fixture.Settings, fixture.Authenticator),
            new CSharpModelGenerator(),
            new EmbeddedDictionaryProvider(fixture.Provider),
            new FileFixLog(Path.Combine(TemporaryDirectory, "audit", "session.log"), true),
            new FileMessageStore(Path.Combine(TemporaryDirectory, "store"), "session", 10),
            new FixDictionaryComposer(),
            new FixDictionaryQuery(),
            new FixFieldValueCodec(),
            new FixMessageCracker(),
            new FixMessageOperations(),
            new FixMessageValidator(),
            new FixSession(fixture.Settings, fixture.Transport, fixture.Store, fixture.Log, fixture.Clock, fixture.Throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, fixture.Provider, fixture.Authenticator),
            new FixSessionEngine(fixture.Clock, fixture.Application, fixture.Provider, fixture.Authenticator),
            new MemoryMessageStore(10),
            new NullFixLog(),
            new OrchestraDictionaryLoader(),
            new OrchestraDiffer(),
            new OrchestraMapper(),
            new OrchestraReader(),
            new OrchestraRuleEvaluator(fixture.Provider.Dictionary),
            new OrchestraSlicer(),
            new OrchestraValidator(),
            new OrchestraWriter(),
            new QuickFixDictionaryLoader(),
            new QuickFixDictionaryWriter(),
            new SbeSchemaGenerator(),
            new SofhMessageFramer(),
            streamTransport,
            new SystemSessionClock(),
            new TagValueMessageDecoder(),
            new TagValueMessageEncoder(),
            new TagValueMessageFramer(),
            new TcpAcceptorTransport(transportSettings),
            new TcpInitiatorTransport(transportSettings),
            new TextSessionSettingsLoader(),
            tlsTransport,
            new TokenBucketMessageThrottle(fixture.Clock)
        ];

        Assembly assembly = typeof(FixSession).Assembly;
        Type[] expected = assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.GetInterfaces().Any(contract => contract.Assembly == assembly))
            .OrderBy(type => type.FullName)
            .ToArray();
        Type[] actual = implementations.Select(instance => instance.GetType()).OrderBy(type => type.FullName).ToArray();

        Assert.That(actual, Is.EqualTo(expected));

        streamTransport.CloseTransport();
        tlsTransport.CloseTransport();
    }

    [Test]
    public void OptionalAndAlternativeConstructorInputsAreAcceptedAndCopied()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        BasicSessionAuthenticator withoutCustomAuthentication = new BasicSessionAuthenticator(fixture.Settings, null);
        BasicSessionAuthenticator withCustomAuthentication = new BasicSessionAuthenticator(fixture.Settings, fixture.Authenticator);
        EmbeddedDictionaryProvider embeddedOnly = new EmbeddedDictionaryProvider(null);
        EmbeddedDictionaryProvider withFallback = new EmbeddedDictionaryProvider(fixture.Provider);
        FixSessionEngine engineWithoutCustomAuthentication = new FixSessionEngine(fixture.Clock, fixture.Application, fixture.Provider, null);
        FixSessionEngine engineWithCustomAuthentication = new FixSessionEngine(fixture.Clock, fixture.Application, fixture.Provider, fixture.Authenticator);
        MemoryMessageStore unlimitedMemoryStore = new MemoryMessageStore(0);
        MemoryMessageStore boundedMemoryStore = new MemoryMessageStore(5);
        FileMessageStore unlimitedFileStore = new FileMessageStore(Path.Combine(TemporaryDirectory, "unlimited"), "store", 0);
        FileMessageStore boundedFileStore = new FileMessageStore(Path.Combine(TemporaryDirectory, "bounded"), "store", 5);
        FileFixLog redactingLog = new FileFixLog(Path.Combine(TemporaryDirectory, "redacting.log"), true);
        FileFixLog plainLog = new FileFixLog(Path.Combine(TemporaryDirectory, "plain.log"), false);
        TlsSessionTransport initiator = new TlsSessionTransport(new MemoryStream(), SessionRole.Initiator);
        TlsSessionTransport acceptor = new TlsSessionTransport(new MemoryStream(), SessionRole.Acceptor);

        Assert.Multiple(() =>
        {
            Assert.That(withoutCustomAuthentication.CustomAuthenticator, Is.Null);
            Assert.That(withCustomAuthentication.CustomAuthenticator, Is.SameAs(fixture.Authenticator));
            Assert.That(embeddedOnly.RuntimeProvider, Is.Null);
            Assert.That(withFallback.RuntimeProvider, Is.SameAs(fixture.Provider));
            Assert.That(engineWithoutCustomAuthentication.CustomAuthenticator, Is.Null);
            Assert.That(engineWithCustomAuthentication.CustomAuthenticator, Is.SameAs(fixture.Authenticator));
            Assert.That(unlimitedMemoryStore.MaximumMessages, Is.Zero);
            Assert.That(boundedMemoryStore.MaximumMessages, Is.EqualTo(5));
            Assert.That(unlimitedFileStore.MaximumMessages, Is.Zero);
            Assert.That(boundedFileStore.MaximumMessages, Is.EqualTo(5));
            Assert.That(redactingLog.RedactSensitiveFields, Is.True);
            Assert.That(plainLog.RedactSensitiveFields, Is.False);
            Assert.That(initiator.Role, Is.EqualTo(SessionRole.Initiator));
            Assert.That(acceptor.Role, Is.EqualTo(SessionRole.Acceptor));
        });

        initiator.CloseTransport();
        acceptor.CloseTransport();
    }

    [Test]
    public void TransportConstructorsCopySettingsWithoutMutatingThem()
    {
        SessionSettings settings = FixSessionTests.Settings(false);
        settings.ConnectTimeoutMilliseconds = 4321;
        settings.Endpoints[0].Priority = 7;
        string originalHost = settings.Endpoints[0].Host;
        int originalPort = settings.Endpoints[0].Port;

        TcpInitiatorTransport initiator = new TcpInitiatorTransport(settings);
        TcpAcceptorTransport acceptor = new TcpAcceptorTransport(settings);
        settings.ConnectTimeoutMilliseconds = 1;
        settings.Endpoints[0].Host = "changed";
        settings.Endpoints[0].Port = 1;
        settings.Endpoints[0].Priority = 1;

        Assert.Multiple(() =>
        {
            Assert.That(initiator.ConnectTimeoutMilliseconds, Is.EqualTo(4321));
            Assert.That(acceptor.ConnectTimeoutMilliseconds, Is.EqualTo(4321));
            Assert.That(initiator.ConfiguredEndpoints[0].Host, Is.EqualTo(originalHost));
            Assert.That(initiator.ConfiguredEndpoints[0].Port, Is.EqualTo(originalPort));
            Assert.That(initiator.ConfiguredEndpoints[0].Priority, Is.EqualTo(7));
            Assert.That(initiator.ConfiguredEndpoints[0], Is.Not.SameAs(settings.Endpoints[0]));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    public void FixSessionRejectsEachMissingRequiredInputAtomically(int missingInput)
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);

        Assert.Throws<FixSessionException>(() => ConstructSession(fixture, missingInput));

        AssertOperationalDependenciesWereNotUsed(fixture);
    }

    [TestCase("missing-id")]
    [TestCase("missing-validation")]
    [TestCase("missing-throttle")]
    [TestCase("missing-tls")]
    [TestCase("missing-schedule")]
    [TestCase("missing-transport-dictionary")]
    [TestCase("missing-application-dictionary")]
    [TestCase("missing-endpoints")]
    [TestCase("empty-endpoints")]
    [TestCase("invalid-connect-timeout")]
    [TestCase("invalid-logon-timeout")]
    [TestCase("invalid-logout-timeout")]
    [TestCase("invalid-role")]
    [TestCase("invalid-heartbeat")]
    [TestCase("invalid-latency")]
    [TestCase("invalid-reconnect-delay")]
    [TestCase("missing-begin-string")]
    [TestCase("missing-sender")]
    [TestCase("missing-target")]
    [TestCase("unsupported-begin-string")]
    public void FixSessionRejectsInvalidConfigurationAtomically(string invalidConfiguration)
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        ApplyInvalidConfiguration(fixture.Settings, invalidConfiguration);

        Assert.Throws<FixSessionException>(() => ConstructSession(fixture, -1));

        AssertOperationalDependenciesWereNotUsed(fixture);
    }

    [Test]
    public void InvalidSimpleConstructorInputsHaveNoExternalEffects()
    {
        string absentStoreDirectory = Path.Combine(TemporaryDirectory, "absent-store");
        string absentLogDirectory = Path.Combine(TemporaryDirectory, "absent-log");
        TrackingStream unreadable = new TrackingStream(false, true);
        TrackingStream unwritable = new TrackingStream(true, false);
        TrackingStream invalidRoleStream = new TrackingStream(true, true);
        FixSessionTests.RecordingSessionApplication application = new FixSessionTests.RecordingSessionApplication();
        FixSessionTests.EmptyDictionaryProvider provider = new FixSessionTests.EmptyDictionaryProvider();
        FakeSessionClock clock = new FakeSessionClock(FixSessionTests.Now);
        SessionSettings invalidTransportSettings = FixSessionTests.Settings(false);
        invalidTransportSettings.ConnectTimeoutMilliseconds = 0;

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new BasicSessionAuthenticator(null!, null));
            Assert.Throws<ArgumentNullException>(() => new OrchestraRuleEvaluator(null!));
            Assert.Throws<FixStoreException>(() => new MemoryMessageStore(-1));
            Assert.Throws<FixSessionException>(() => new TokenBucketMessageThrottle(null!));
            Assert.Throws<FixTransportException>(() => new StreamSessionTransport(unreadable));
            Assert.Throws<FixTransportException>(() => new StreamSessionTransport(unwritable));
            Assert.Throws<FixTransportException>(() => new TlsSessionTransport(unreadable, SessionRole.Initiator));
            Assert.Throws<FixTransportException>(() => new TlsSessionTransport(invalidRoleStream, (SessionRole)99));
            Assert.Throws<FixTransportException>(() => new TcpInitiatorTransport(invalidTransportSettings));
            Assert.Throws<FixTransportException>(() => new TcpAcceptorTransport(invalidTransportSettings));
            Assert.Throws<FixSessionException>(() => new FixSessionEngine(null!, application, provider, null));
            Assert.Throws<FixSessionException>(() => new FixSessionEngine(clock, null!, provider, null));
            Assert.Throws<FixSessionException>(() => new FixSessionEngine(clock, application, null!, null));
            Assert.Throws<FixStoreException>(() => new FileMessageStore(absentStoreDirectory, "../escape", 0));
            Assert.Throws<FixStoreException>(() => new FileMessageStore(absentStoreDirectory, "valid", -1));
            Assert.Throws<FixStoreException>(() => new FileFixLog("", true));
            Assert.That(Directory.Exists(absentStoreDirectory), Is.False);
            Assert.That(Directory.Exists(absentLogDirectory), Is.False);
            Assert.That(unreadable.DisposeCount, Is.Zero);
            Assert.That(unwritable.DisposeCount, Is.Zero);
            Assert.That(invalidRoleStream.DisposeCount, Is.Zero);
            Assert.That(application.CallbackOrder, Is.Empty);
            Assert.That(clock.ReadCount, Is.Zero);
        });
    }

    [Test]
    public void DictionaryResolutionFailureDoesNotStartOrMutateSessionDependencies()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        FailingDictionaryProvider provider = new FailingDictionaryProvider();

        Assert.Throws<FixSessionException>(() => new FixSession(fixture.Settings, fixture.Transport, fixture.Store, fixture.Log, fixture.Clock, fixture.Throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, provider, fixture.Authenticator));

        Assert.Multiple(() =>
        {
            Assert.That(provider.ResolveCount, Is.EqualTo(1));
            AssertOperationalDependenciesWereNotUsed(fixture);
        });
    }

    private static FixSession ConstructSession(FixSessionTests.SessionFixture fixture, int missingInput)
    {
        return new FixSession(
            missingInput == 0 ? null! : fixture.Settings,
            missingInput == 1 ? null! : fixture.Transport,
            missingInput == 2 ? null! : fixture.Store,
            missingInput == 3 ? null! : fixture.Log,
            missingInput == 4 ? null! : fixture.Clock,
            missingInput == 5 ? null! : fixture.Throttle,
            missingInput == 6 ? null! : fixture.Application,
            missingInput == 7 ? null! : fixture.Framer,
            missingInput == 8 ? null! : fixture.Decoder,
            missingInput == 9 ? null! : fixture.Encoder,
            missingInput == 10 ? null! : fixture.Validator,
            missingInput == 11 ? null! : fixture.Provider,
            missingInput == 12 ? null! : fixture.Authenticator);
    }

    private static void ApplyInvalidConfiguration(SessionSettings settings, string invalidConfiguration)
    {
        switch (invalidConfiguration)
        {
            case "missing-id": settings.Id = null!; break;
            case "missing-validation": settings.Validation = null!; break;
            case "missing-throttle": settings.Throttle = null!; break;
            case "missing-tls": settings.Tls = null!; break;
            case "missing-schedule": settings.Schedule = null!; break;
            case "missing-transport-dictionary": settings.TransportDictionary = null!; break;
            case "missing-application-dictionary": settings.ApplicationDictionary = null!; break;
            case "missing-endpoints": settings.Endpoints = null!; break;
            case "empty-endpoints": settings.Endpoints.Clear(); break;
            case "invalid-connect-timeout": settings.ConnectTimeoutMilliseconds = 0; break;
            case "invalid-logon-timeout": settings.LogonTimeoutMilliseconds = 0; break;
            case "invalid-logout-timeout": settings.LogoutTimeoutMilliseconds = 0; break;
            case "invalid-role": settings.Role = (SessionRole)99; break;
            case "invalid-heartbeat": settings.HeartbeatIntervalSeconds = -1; break;
            case "invalid-latency": settings.MaxLatencySeconds = 0; break;
            case "invalid-reconnect-delay": settings.MaxReconnectDelayMilliseconds = settings.ReconnectDelayMilliseconds - 1; break;
            case "missing-begin-string": settings.Id.BeginString = ""; break;
            case "missing-sender": settings.Id.SenderCompID = ""; break;
            case "missing-target": settings.Id.TargetCompID = ""; break;
            case "unsupported-begin-string": settings.Id.BeginString = "FIX.9.9"; break;
            default: throw new InvalidOperationException("Unknown invalid constructor configuration: " + invalidConfiguration);
        }
    }

    private static void AssertOperationalDependenciesWereNotUsed(FixSessionTests.SessionFixture fixture)
    {
        StoreRangeResult state = fixture.Store.RefreshStore();
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Transport.OpenCount, Is.Zero);
            Assert.That(fixture.Transport.CloseCount, Is.Zero);
            Assert.That(fixture.Application.CallbackOrder, Is.Empty);
            Assert.That(fixture.Application.ApplicationContexts, Is.Empty);
            Assert.That(fixture.Log.Entries, Is.Empty);
            Assert.That(fixture.Throttle.Contexts, Is.Empty);
            Assert.That(fixture.Validator.Messages, Is.Empty);
            Assert.That(state.NextSenderSeqNum, Is.EqualTo(FixDefaults.InitialSequenceNumber));
            Assert.That(state.NextTargetSeqNum, Is.EqualTo(FixDefaults.InitialSequenceNumber));
        });
    }

    private class FailingDictionaryProvider : IDictionaryProvider
    {
        internal int ResolveCount;

        public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
        {
            ResolveCount++;
            return new DictionaryLoadResult();
        }
    }

    private class TrackingStream : Stream
    {
        private readonly bool Readable;
        private readonly bool Writable;
        internal int DisposeCount;

        internal TrackingStream(bool readable, bool writable)
        {
            Readable = readable;
            Writable = writable;
        }

        public override bool CanRead => Readable;
        public override bool CanSeek => false;
        public override bool CanWrite => Writable;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { return 0; }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { }

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
