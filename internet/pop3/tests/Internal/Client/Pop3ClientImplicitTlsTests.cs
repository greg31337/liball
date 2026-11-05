using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace pop3.Tests;

public class Pop3ClientImplicitTlsTests
{
    [Test]
    public void AttachedStreamIsSecuredBeforeTheGreetingIsRead()
    {
        TrackingDuplexStream stream = CreateGreetingStream("+OK secured ready\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider();
        Pop3ClientOptions options = CreateOptions(true, provider);
        options.Host = "mail.example.test";
        options.Tls.TargetHost = "";
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, options, CancellationToken.None);

        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(provider.ReadCallsAtHandshake, Is.Zero);
        Assert.That(provider.Options, Is.Not.Null);
        Assert.That(provider.Options, Is.Not.SameAs(options.Tls));
        Assert.That(provider.Options!.TargetHost, Is.EqualTo("mail.example.test"));
        Assert.That(provider.Options.ClientCertificates, Is.Not.SameAs(options.Tls.ClientCertificates));
        Assert.That(provider.Options.LeaveInnerStreamOpen, Is.True);
        Assert.That(engine.GetTransportStream(), Is.SameAs(provider.SecuredStream));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().IsSecure, Is.True);
        Assert.That(engine.GetSnapshot().Greeting, Is.EqualTo("secured ready"));

        LayerStream securedStream = provider.SecuredStream!;
        engine.CloseConnection();
        Assert.That(securedStream.IsDisposed, Is.True);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void HostConnectionCompletesTlsBeforeTheServerGreeting()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using (ManualResetEventSlim handshakeStarted = new ManualResetEventSlim(false))
        {
            Task<bool> serverTask = Task.Run(() => ServeAfterHandshake(listener, handshakeStarted));
            GateTlsProvider provider = new GateTlsProvider(handshakeStarted);
            Pop3ClientOptions options = CreateOptions(false, provider);
            options.Host = "127.0.0.1";
            options.Port = port;
            options.Tls.TargetHost = "";
            Pop3ClientEngine engine = new Pop3ClientEngine();

            try
            {
                engine.ConnectHost(options, CancellationToken.None);

                Assert.That(provider.CallCount, Is.EqualTo(1));
                Assert.That(provider.Options!.TargetHost, Is.EqualTo("127.0.0.1"));
                Assert.That(provider.Options.LeaveInnerStreamOpen, Is.False);
                Assert.That(engine.GetSnapshot().IsSecure, Is.True);
                Assert.That(engine.GetSnapshot().Greeting, Is.EqualTo("TLS first"));

                engine.CloseConnection();
                Assert.That(serverTask.GetAwaiter().GetResult(), Is.True);
            }
            finally
            {
                engine.CloseConnection();
                listener.Stop();
            }
        }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void FailedHandshakeDoesNotConsumePlaintextGreeting(
        bool leaveOpen,
        bool expectedDisposed)
    {
        TrackingDuplexStream stream = CreateGreetingStream("+OK must not be read\r\n");
        ThrowingTlsProvider provider = new ThrowingTlsProvider();
        Pop3ClientOptions options = CreateOptions(leaveOpen, provider);
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.SecurityMode, Is.EqualTo(Pop3SecurityMode.ImplicitTls));
        Assert.That(exception.Operation, Is.EqualTo("SecureClientStream"));
        Assert.That(exception.InnerException, Is.TypeOf<AuthenticationException>());
        Assert.That(stream.ReadCallCount, Is.Zero);
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [Test]
    public void TimedOutHandshakeDoesNotConsumePlaintextOrCallerOwnedStream()
    {
        TrackingDuplexStream stream = CreateGreetingStream("+OK must not be read\r\n");
        BlockingTlsProvider provider = new BlockingTlsProvider();
        Pop3ClientOptions options = CreateOptions(true, provider);
        options.AuthenticationTimeoutMilliseconds = 50;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.Message, Does.Contain("timed out"));
        Assert.That(exception.Operation, Is.EqualTo("SecureClientStream"));
        Assert.That(stream.ReadCallCount, Is.Zero);
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [Test]
    public void CanceledHandshakeDoesNotConsumePlaintextOrCallerOwnedStream()
    {
        TrackingDuplexStream stream = CreateGreetingStream("+OK must not be read\r\n");
        BlockingTlsProvider provider = new BlockingTlsProvider();
        Pop3ClientOptions options = CreateOptions(true, provider);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.CancelAfter(50);

            Assert.Throws<OperationCanceledException>(() =>
                engine.ConnectStream(stream, options, cancellation.Token));
        }

        Assert.That(stream.ReadCallCount, Is.Zero);
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [Test]
    public void InvalidSecuredStreamIsRejectedAndDisposed()
    {
        TrackingDuplexStream stream = CreateGreetingStream("+OK must not be read\r\n");
        InvalidTlsProvider provider = new InvalidTlsProvider();
        Pop3ClientOptions options = CreateOptions(true, provider);
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.Message, Does.Contain("invalid secured stream"));
        Assert.That(provider.InvalidStream.IsDisposed, Is.True);
        Assert.That(stream.ReadCallCount, Is.Zero);
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [Test]
    public void ImplicitTlsConfigurationIsValidatedBeforeTheStreamIsTouched()
    {
        TrackingDuplexStream stream = CreateGreetingStream("+OK must not be read\r\n");
        Pop3ClientOptions options = CreateOptions(true, new RecordingTlsProvider());
        options.Host = "";
        options.Tls.TargetHost = "";
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Assert.Throws<ArgumentException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None));

        Assert.That(stream.ReadCallCount, Is.Zero);
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Disconnected));
        stream.Dispose();
    }

    private static Pop3ClientOptions CreateOptions(bool leaveOpen, IPop3ClientTlsProvider provider)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.Port = Pop3Defaults.ImplicitTlsPort;
        options.SecurityMode = Pop3SecurityMode.ImplicitTls;
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 8;
        options.LeaveStreamOpen = leaveOpen;
        options.TlsProvider = provider;
        return options;
    }

    private static TrackingDuplexStream CreateGreetingStream(string greeting)
    {
        return new TrackingDuplexStream(Encoding.ASCII.GetBytes(
            greeting + "-ERR CAPA unavailable\r\n"));
    }

    private static bool ServeAfterHandshake(
        TcpListener listener,
        ManualResetEventSlim handshakeStarted)
    {
        using (TcpClient client = listener.AcceptTcpClient())
        {
            if (!handshakeStarted.Wait(5000))
            {
                return false;
            }

            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = 5000;
            byte[] greeting = Encoding.ASCII.GetBytes("+OK TLS first\r\n");
            stream.Write(greeting, 0, greeting.Length);
            stream.Flush();
            byte[] command = new byte[6];
            int commandCount = 0;
            while (commandCount < command.Length)
            {
                int count = stream.Read(command, commandCount, command.Length - commandCount);
                if (count == 0)
                {
                    return false;
                }
                commandCount += count;
            }
            if (Encoding.ASCII.GetString(command) != "CAPA\r\n")
            {
                return false;
            }
            byte[] response = Encoding.ASCII.GetBytes("-ERR CAPA unavailable\r\n");
            stream.Write(response, 0, response.Length);
            stream.Flush();
            return stream.Read(command, 0, 1) == 0;
        }
    }

    private class RecordingTlsProvider : IPop3ClientTlsProvider
    {
        public int CallCount;
        public int ReadCallsAtHandshake;
        public Pop3TlsClientOptions? Options;
        public LayerStream? SecuredStream;

        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            TrackingDuplexStream tracking = (TrackingDuplexStream)stream;
            ReadCallsAtHandshake = tracking.ReadCallCount;
            Options = options;
            SecuredStream = new LayerStream(stream, options.LeaveInnerStreamOpen);
            return SecuredStream;
        }
    }

    private class GateTlsProvider : IPop3ClientTlsProvider
    {
        ManualResetEventSlim HandshakeStarted;
        public int CallCount;
        public Pop3TlsClientOptions? Options;

        public GateTlsProvider(ManualResetEventSlim handshakeStarted)
        {
            HandshakeStarted = handshakeStarted;
        }

        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            Options = options;
            HandshakeStarted.Set();
            return stream;
        }
    }

    private class ThrowingTlsProvider : IPop3ClientTlsProvider
    {
        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            throw new AuthenticationException("TLS failed before POP3 framing.");
        }
    }

    private class BlockingTlsProvider : IPop3ClientTlsProvider
    {
        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.WaitHandle.WaitOne();
            cancellationToken.ThrowIfCancellationRequested();
            return stream;
        }
    }

    private class InvalidTlsProvider : IPop3ClientTlsProvider
    {
        public CapabilityStream InvalidStream = new CapabilityStream(true, false);

        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            return InvalidStream;
        }
    }

    private class TrackingDuplexStream : Stream
    {
        byte[] ReadData;
        int ReadPosition;
        public int ReadCallCount;
        public bool IsDisposed;

        public TrackingDuplexStream(byte[] readData)
        {
            ReadData = readData;
        }

        public override bool CanRead
        {
            get { return !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !IsDisposed; }
        }

        public override long Length
        {
            get { return ReadData.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadCore(buffer, offset, count);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadCore(buffer, offset, count));
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
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        int ReadCore(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;
            int remaining = ReadData.Length - ReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(ReadData, ReadPosition, buffer, offset, readCount);
            ReadPosition += readCount;
            return readCount;
        }
    }

    private class LayerStream : Stream
    {
        Stream Inner;
        bool LeaveInnerOpen;
        public bool IsDisposed;

        public LayerStream(Stream inner, bool leaveInnerOpen)
        {
            Inner = inner;
            LeaveInnerOpen = leaveInnerOpen;
        }

        public override bool CanRead
        {
            get { return !IsDisposed && Inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !IsDisposed && Inner.CanWrite; }
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

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            return Inner.ReadAsync(buffer, offset, count, cancellationToken);
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
            IsDisposed = true;
            if (disposing && !LeaveInnerOpen)
            {
                Inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private class CapabilityStream : Stream
    {
        bool Readable;
        bool Writable;
        public bool IsDisposed;

        public CapabilityStream(bool readable, bool writable)
        {
            Readable = readable;
            Writable = writable;
        }

        public override bool CanRead
        {
            get { return Readable && !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return Writable && !IsDisposed; }
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
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
