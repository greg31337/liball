using System.Text;

namespace pop3.Tests;

public class Pop3ServerEngineStartupTests
{
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void PlaintextStartupWritesGreetingAndHonorsAcceptedStreamOwnership(
        bool leaveOpen,
        int expectedDisposeCount)
    {
        StartupStream stream = new StartupStream();
        Pop3ServerConnection connection = CreateConnection(stream, leaveOpen, false);
        connection.LocalEndPoint = "127.0.0.1:110";
        connection.RemoteEndPoint = "192.0.2.1:40000";
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.Plaintext);
        options.GreetingText = "server ready";
        Pop3ServerEngine engine = new Pop3ServerEngine();

        Pop3Response greeting = engine.StartSession(
            connection,
            options,
            CancellationToken.None);

        Assert.That(greeting.Text, Is.EqualTo("server ready"));
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK server ready\r\n"));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.False);
        Assert.That(engine.GetStateMachine().GetTransportStream(), Is.SameAs(stream));
        Assert.That(engine.GetWireReader(), Is.Not.Null);
        Assert.That(engine.GetWireWriter(), Is.Not.Null);
        Assert.That(engine.GetOptions(), Is.SameAs(options));
        Assert.That(engine.GetStateMachine().GetConnectionContext().LocalEndPoint,
            Is.EqualTo("127.0.0.1:110"));

        engine.CloseSession();
        engine.CloseSession();

        Assert.That(stream.DisposeCount, Is.EqualTo(expectedDisposeCount));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.Throws<Pop3StateException>(() => engine.GetWireReader());
        Assert.Throws<Pop3StateException>(() => engine.GetOptions());
        if (leaveOpen)
        {
            stream.Dispose();
        }
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void ImplicitTlsHandshakePrecedesGreetingAndPreservesRootOwnership(
        bool leaveOpen,
        int expectedRootDisposeCount)
    {
        StartupStream stream = new StartupStream();
        RecordingTlsProvider provider = new RecordingTlsProvider();
        Pop3ServerConnection connection = CreateConnection(stream, leaveOpen, false);
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.ImplicitTls);
        options.GreetingText = "protected ready";
        options.TlsProvider = provider;
        options.Tls.RequireClientCertificate = true;
        options.Tls.CheckCertificateRevocation = false;
        Pop3ServerEngine engine = new Pop3ServerEngine();

        engine.StartSession(connection, options, CancellationToken.None);

        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(provider.WrittenOctetsAtCall, Is.Zero);
        Assert.That(provider.Options, Is.Not.Null);
        Assert.That(provider.Options, Is.Not.SameAs(options.Tls));
        Assert.That(provider.Options!.RequireClientCertificate, Is.True);
        Assert.That(provider.Options.CheckCertificateRevocation, Is.False);
        Assert.That(provider.Options.LeaveInnerStreamOpen, Is.True);
        Assert.That(provider.Layer, Is.Not.Null);
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK protected ready\r\n"));
        Assert.That(engine.GetStateMachine().GetTransportStream(),
            Is.SameAs(provider.Layer));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.True);
        Pop3ServerConnection context =
            engine.GetStateMachine().GetConnectionContext();
        Assert.That(context.Stream, Is.SameAs(provider.Layer));
        Assert.That(context.IsSecure, Is.True);
        Assert.That(context.LeaveOpen, Is.EqualTo(leaveOpen));

        engine.CloseSession();

        Assert.That(provider.Layer!.DisposeCount, Is.EqualTo(1));
        Assert.That(stream.DisposeCount, Is.EqualTo(expectedRootDisposeCount));
        if (leaveOpen)
        {
            Assert.That(stream.CanWrite, Is.True);
            stream.Dispose();
        }
    }

    [Test]
    public void AlreadySecureAcceptedStreamSkipsImplicitHandshake()
    {
        StartupStream stream = new StartupStream();
        ThrowingTlsProvider provider = new ThrowingTlsProvider();
        Pop3ServerConnection connection = CreateConnection(stream, true, true);
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.ImplicitTls);
        options.TlsProvider = provider;
        options.Tls = null!;
        Pop3ServerEngine engine = new Pop3ServerEngine();

        engine.StartSession(connection, options, CancellationToken.None);

        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.True);
        Assert.That(engine.GetStateMachine().GetTransportStream(), Is.SameAs(stream));

        engine.CloseSession();
        Assert.That(stream.DisposeCount, Is.Zero);
        stream.Dispose();
    }

    [TestCase(Pop3SecurityMode.StlsIfAvailable)]
    [TestCase(Pop3SecurityMode.StlsRequired)]
    public void StlsPoliciesStartInPlaintextUntilUpgradeIsRequested(
        Pop3SecurityMode securityMode)
    {
        StartupStream stream = new StartupStream();
        RecordingTlsProvider provider = new RecordingTlsProvider();
        Pop3ServerOptions options = CreateOptions(securityMode);
        options.TlsProvider = provider;
        Pop3ServerEngine engine = new Pop3ServerEngine();

        engine.StartSession(
            CreateConnection(stream, true, false),
            options,
            CancellationToken.None);

        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.False);
        Assert.That(engine.GetStateMachine().GetTransportStream(), Is.SameAs(stream));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void FailedImplicitHandshakeWritesNoGreetingAndClosesSession(
        bool leaveOpen,
        int expectedDisposeCount)
    {
        StartupStream stream = new StartupStream();
        ThrowingTlsProvider provider = new ThrowingTlsProvider();
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.ImplicitTls);
        options.TlsProvider = provider;
        Pop3ServerEngine engine = new Pop3ServerEngine();

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.StartSession(
                CreateConnection(stream, leaveOpen, false),
                options,
                CancellationToken.None))!;

        Assert.That(exception.SecurityMode, Is.EqualTo(Pop3SecurityMode.ImplicitTls));
        Assert.That(exception.Operation, Is.EqualTo("SecureServerStream"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(provider.WrittenOctetsAtCall, Is.Zero);
        Assert.That(stream.WrittenText(), Is.Empty);
        Assert.That(stream.DisposeCount, Is.EqualTo(expectedDisposeCount));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        if (leaveOpen)
        {
            stream.Dispose();
        }
    }

    [Test]
    public void ImplicitHandshakeTimeoutWritesNoGreetingAndLeavesCallerStreamOpen()
    {
        StartupStream stream = new StartupStream();
        BlockingTlsProvider provider = new BlockingTlsProvider();
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.ImplicitTls);
        options.AuthenticationTimeoutMilliseconds = 50;
        options.TlsProvider = provider;
        Pop3ServerEngine engine = new Pop3ServerEngine();

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.StartSession(
                CreateConnection(stream, true, false),
                options,
                CancellationToken.None))!;

        Assert.That(exception.Message, Does.Contain("timed out"));
        Assert.That(stream.WrittenText(), Is.Empty);
        Assert.That(stream.DisposeCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));

        stream.Dispose();
    }

    [Test]
    public void GreetingWriteFailureClosesOwnedSession()
    {
        StartupStream stream = new StartupStream();
        stream.ThrowOnWrite = true;
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.Plaintext);
        Pop3ServerEngine engine = new Pop3ServerEngine();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.StartSession(
                CreateConnection(stream, false, false),
                options,
                CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("Greeting"));
        Assert.That(exception.SessionState,
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.IsTimeout, Is.False);
        Assert.That(stream.WriteCallCount, Is.EqualTo(1));
        Assert.That(stream.DisposeCount, Is.EqualTo(1));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
    }

    [Test]
    public void PlainGreetingHonorsTheExactResponseLineLimit()
    {
        StartupStream exactStream = new StartupStream();
        Pop3ServerOptions exactOptions = CreateOptions(Pop3SecurityMode.Plaintext);
        exactOptions.EnableApop = false;
        exactOptions.GreetingText = new string('g', 506);
        Pop3ServerEngine exactEngine = new Pop3ServerEngine();

        exactEngine.StartSession(
            CreateConnection(exactStream, true, false),
            exactOptions,
            CancellationToken.None);

        Assert.That(exactStream.WrittenOctets(),
            Is.EqualTo(Pop3Defaults.MaximumResponseLineOctets));
        Assert.That(exactStream.WrittenText(),
            Is.EqualTo("+OK " + exactOptions.GreetingText + "\r\n"));
        exactEngine.CloseSession();
        exactStream.Dispose();

        StartupStream overflowStream = new StartupStream();
        Pop3ServerOptions overflowOptions = CreateOptions(Pop3SecurityMode.Plaintext);
        overflowOptions.EnableApop = false;
        overflowOptions.GreetingText = new string('g', 507);
        Pop3ServerEngine overflowEngine = new Pop3ServerEngine();

        Assert.Throws<InvalidDataException>(() =>
            overflowEngine.StartSession(
                CreateConnection(overflowStream, true, false),
                overflowOptions,
                CancellationToken.None));

        Assert.That(overflowStream.WrittenOctets(), Is.Zero);
        Assert.That(overflowEngine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Disconnected));
        overflowStream.Dispose();
    }

    [Test]
    public void StartupValidatesConfigurationAndCancellationBeforeNetworkUse()
    {
        StartupStream stream = new StartupStream();
        Pop3ServerConnection connection = CreateConnection(stream, true, false);
        Pop3ServerOptions options = CreateOptions(Pop3SecurityMode.Plaintext);
        Pop3ServerEngine engine = new Pop3ServerEngine();

        options.ReadTimeoutMilliseconds = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.StartSession(connection, options, CancellationToken.None));
        options.ReadTimeoutMilliseconds = 2000;
        options.BufferSize = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.StartSession(connection, options, CancellationToken.None));
        options.BufferSize = 16;
        options.SecurityMode = (Pop3SecurityMode)99;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.StartSession(connection, options, CancellationToken.None));
        options.SecurityMode = Pop3SecurityMode.ImplicitTls;
        Assert.Throws<ArgumentException>(() =>
            engine.StartSession(connection, options, CancellationToken.None));
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() =>
                engine.StartSession(connection, options, cancellation.Token));
        }

        Assert.That(stream.WriteCallCount, Is.Zero);
        Assert.That(stream.DisposeCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Disconnected));

        stream.Dispose();
    }

    static Pop3ServerConnection CreateConnection(
        StartupStream stream,
        bool leaveOpen,
        bool isSecure)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = leaveOpen;
        connection.IsSecure = isSecure;
        return connection;
    }

    static Pop3ServerOptions CreateOptions(Pop3SecurityMode securityMode)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = securityMode;
        options.InactivityTimeoutMilliseconds = 600000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.MaximumMessageLineOctets = 1024;
        options.MaximumSaslLineOctets = 1024;
        return options;
    }

    class RecordingTlsProvider : IPop3ServerTlsProvider
    {
        public int CallCount;
        public int WrittenOctetsAtCall;
        public Pop3TlsServerOptions? Options;
        public LayerStream? Layer;

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            WrittenOctetsAtCall = ((StartupStream)stream).WrittenOctets();
            Options = options;
            Layer = new LayerStream(stream, options.LeaveInnerStreamOpen);
            return Layer;
        }
    }

    class ThrowingTlsProvider : IPop3ServerTlsProvider
    {
        public int CallCount;
        public int WrittenOctetsAtCall;

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            WrittenOctetsAtCall = ((StartupStream)stream).WrittenOctets();
            throw new InvalidDataException("handshake failed");
        }
    }

    class BlockingTlsProvider : IPop3ServerTlsProvider
    {
        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            Task.Delay(Timeout.Infinite, cancellationToken).GetAwaiter().GetResult();
            return stream;
        }
    }

    class LayerStream : Stream
    {
        Stream Inner;
        bool LeaveInnerOpen;
        public int DisposeCount;

        public LayerStream(Stream inner, bool leaveInnerOpen)
        {
            Inner = inner;
            LeaveInnerOpen = leaveInnerOpen;
        }

        public override bool CanRead
        {
            get { return Inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return Inner.CanWrite; }
        }

        public override long Length
        {
            get { return Inner.Length; }
        }

        public override long Position
        {
            get { return Inner.Position; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            Inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Inner.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            if (!LeaveInnerOpen)
            {
                Inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    class StartupStream : Stream
    {
        MemoryStream Written = new MemoryStream();
        public bool ThrowOnWrite;
        public int WriteCallCount;
        public int DisposeCount;
        bool Disposed;

        public override bool CanRead
        {
            get { return !Disposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !Disposed; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public int WrittenOctets()
        {
            return (int)Written.Length;
        }

        public string WrittenText()
        {
            return Encoding.ASCII.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCallCount++;
            if (ThrowOnWrite)
            {
                throw new IOException("write failed");
            }
            Written.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
