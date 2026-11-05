using System.Security.Authentication;
using System.Text;

namespace pop3.Tests;

public class Pop3ClientStlsTests
{
    [Test]
    public void PositiveStlsResponseFormsBarrierAndRefreshesProtectedCapabilities()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nSTLS\r\nPIPELINING\r\nUSER\r\nX-OLD value\r\n.\r\n",
            "+OK begin TLS\r\nPLAINTEXT-INJECTION",
            "+OK protected capabilities\r\nSASL PLAIN\r\nX-NEW value\r\n.\r\n");
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientOptions options = CreateOptions(provider, true);
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        options.EnablePipelining = true;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;

        engine.ConnectStream(stream, options, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(provider.PlaintextChunkConsumedAtHandshake, Is.True);
        Assert.That(provider.CapabilitiesEmptyAtHandshake, Is.True);
        Assert.That(provider.CommandsAtHandshake, Is.EqualTo("CAPA\r\nSTLS\r\n"));
        Assert.That(provider.Options!.TargetHost, Is.EqualTo("mail.example.test"));
        Assert.That(provider.Options.LeaveInnerStreamOpen, Is.True);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.False);
        Assert.That(snapshot.Capabilities.SaslMechanisms, Is.EqualTo(new[] { "PLAIN" }));
        Assert.That(snapshot.Capabilities.Entries, Has.Length.EqualTo(2));
        Assert.That(snapshot.Capabilities.Entries[1].Name, Is.EqualTo("X-NEW"));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("protected capabilities"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nSTLS\r\nCAPA\r\n"));

        LayerStream securedStream = provider.SecuredStream!;
        engine.CloseConnection();
        Assert.That(securedStream.IsDisposed, Is.True);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void AutomaticAuthenticationSelectsOnlyPostTlsCapabilities()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK plaintext capabilities\r\nSTLS\r\nSASL STALE\r\nUSER\r\n.\r\n",
            "+OK begin TLS\r\n",
            "+OK protected capabilities\r\nSASL CURRENT\r\n.\r\n" +
                "+OK authenticated\r\n");
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientOptions options = CreateOptions(provider, true);
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;
        engine.ConnectStream(stream, options, CancellationToken.None);
        SelectionSaslClient stale = new SelectionSaslClient("STALE", "stale");
        SelectionSaslClient current = new SelectionSaslClient("CURRENT", "current");
        Pop3AuthenticationRequest request = new Pop3AuthenticationRequest();
        request.Credentials.Username = "alice";
        request.Credentials.Password = "secret";
        request.SaslClients = new IPop3SaslClient[] { stale, current };
        request.AllowUnprotectedCredentials = true;

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(stale.BeginCount, Is.Zero);
        Assert.That(current.BeginCount, Is.EqualTo(1));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetSnapshot().IsSecure, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nSTLS\r\nCAPA\r\nAUTH CURRENT Y3VycmVudA==\r\nCAPA\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("AUTH STALE"));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void AuthenticatedSessionRejectsLateStlsWithoutWriting()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n",
            "+OK unused\r\n",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;
        engine.ConnectStream(stream, CreateOptions(provider, true), CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        string commandsBeforeUpgrade = stream.WrittenText();

        Pop3StateException exception = Assert.Throws<Pop3StateException>(() =>
            engine.UpgradeToTls(CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Stls));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo(commandsBeforeUpgrade));
        Assert.That(commandsBeforeUpgrade, Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void NegativeStlsResponseDoesNotStartTlsOrDiscardCapabilities()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nSTLS\r\nUSER\r\n.\r\n",
            "-ERR STLS unavailable\r\n",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;
        Pop3ClientOptions options = CreateOptions(provider, true);
        engine.ConnectStream(stream, options, CancellationToken.None);

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.UpgradeToTls(CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Stls));
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsSecure, Is.False);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.True);
        Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(snapshot.LastResponse.Text, Is.EqualTo("STLS unavailable"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nSTLS\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void FailedTlsHandshakeAfterPositiveStlsClosesTheSession(
        bool leaveOpen,
        bool expectedDisposed)
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n",
            "+OK begin TLS\r\nPLAINTEXT-INJECTION",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, true);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;
        engine.ConnectStream(stream, CreateOptions(provider, leaveOpen), CancellationToken.None);

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.UpgradeToTls(CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Operation, Is.EqualTo("SecureClientStream"));
        Assert.That(exception.InnerException, Is.TypeOf<AuthenticationException>());
        Assert.That(provider.PlaintextChunkConsumedAtHandshake, Is.True);
        Assert.That(provider.CapabilitiesEmptyAtHandshake, Is.True);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(snapshot.LastResponse.Text, Is.EqualTo("begin TLS"));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        Assert.Throws<Pop3StateException>(() => engine.GetTransportStream());
        stream.Dispose();
    }

    [Test]
    public void NegativePostTlsCapaLeavesAConnectedSecureSessionWithNoCapabilities()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n",
            "+OK begin TLS\r\n",
            "-ERR CAPA unavailable\r\n");
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;
        engine.ConnectStream(stream, CreateOptions(provider, true), CancellationToken.None);

        engine.UpgradeToTls(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.Capabilities.Entries, Is.Empty);
        Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(snapshot.LastResponse.Text, Is.EqualTo("CAPA unavailable"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nSTLS\r\nCAPA\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void UpgradeRejectsCancellationAndRepeatedTlsBeforeWritingStls()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n",
            "+OK begin TLS\r\n",
            "+OK protected capabilities\r\n.\r\n");
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;
        engine.ConnectStream(stream, CreateOptions(provider, true), CancellationToken.None);
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.UpgradeToTls(cancellation.Token));
        }
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));

        engine.UpgradeToTls(CancellationToken.None);
        string commandsAfterUpgrade = stream.WrittenText();

        Pop3StateException exception = Assert.Throws<Pop3StateException>(() =>
            engine.UpgradeToTls(CancellationToken.None))!;
        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Stls));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo(commandsAfterUpgrade));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void OptionalStlsModeRemainsPlaintextWhenCapabilityIsAbsent()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nUSER\r\n.\r\n",
            "-ERR unused\r\n",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientOptions options = CreateOptions(provider, true);
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;

        engine.ConnectStream(stream, options, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsSecure, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.True);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void RequiredStlsModeClosesWhenCapabilityIsAbsent()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nUSER\r\n.\r\n",
            "-ERR unused\r\n",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientOptions options = CreateOptions(provider, true);
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Stls));
        Assert.That(exception.SecurityMode, Is.EqualTo(Pop3SecurityMode.StlsRequired));
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        stream.Dispose();
    }

    [Test]
    public void OptionalStlsModeRemainsPlaintextWhenAdvertisedUpgradeIsRejected()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nSTLS\r\n.\r\n",
            "-ERR temporarily unavailable\r\n",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientOptions options = CreateOptions(provider, true);
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;

        engine.ConnectStream(stream, options, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsSecure, Is.False);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.True);
        Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nSTLS\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void RequiredStlsModeClosesWhenAdvertisedUpgradeIsRejected()
    {
        StlsBoundaryStream stream = new StlsBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nSTLS\r\n.\r\n",
            "-ERR temporarily unavailable\r\n",
            null);
        BoundaryTlsProvider provider = new BoundaryTlsProvider(stream, false);
        Pop3ClientOptions options = CreateOptions(provider, true);
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        provider.Engine = engine;

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Stls));
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nSTLS\r\n"));
        stream.Dispose();
    }

    private static Pop3ClientOptions CreateOptions(
        IPop3ClientTlsProvider provider,
        bool leaveOpen)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 128;
        options.LeaveStreamOpen = leaveOpen;
        options.TlsProvider = provider;
        return options;
    }

    private class BoundaryTlsProvider : IPop3ClientTlsProvider
    {
        StlsBoundaryStream Stream;
        bool FailHandshake;
        public Pop3ClientEngine? Engine;
        public int CallCount;
        public bool PlaintextChunkConsumedAtHandshake;
        public bool CapabilitiesEmptyAtHandshake;
        public string? CommandsAtHandshake;
        public Pop3TlsClientOptions? Options;
        public LayerStream? SecuredStream;

        public BoundaryTlsProvider(StlsBoundaryStream stream, bool failHandshake)
        {
            Stream = stream;
            FailHandshake = failHandshake;
        }

        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            PlaintextChunkConsumedAtHandshake = Stream.ActiveChunkIsConsumed();
            CapabilitiesEmptyAtHandshake = Engine!.GetSnapshot().Capabilities.Entries.Length == 0;
            CommandsAtHandshake = Stream.WrittenText();
            Options = options;
            if (FailHandshake)
            {
                throw new AuthenticationException("Injected STLS handshake failure.");
            }

            Stream.ActivateTls();
            SecuredStream = new LayerStream(stream, options.LeaveInnerStreamOpen);
            return SecuredStream;
        }
    }

    private class SelectionSaslClient : IPop3SaslClient
    {
        string Mechanism;
        string Response;
        public int BeginCount;

        public SelectionSaslClient(string mechanism, string response)
        {
            Mechanism = mechanism;
            Response = response;
        }

        public string GetMechanism()
        {
            return Mechanism;
        }

        public Pop3SaslStep BeginAuthentication()
        {
            BeginCount++;
            Pop3SaslStep step = new Pop3SaslStep();
            step.Status = Pop3SaslStepStatus.Success;
            step.Data = Encoding.ASCII.GetBytes(Response);
            step.HasData = true;
            step.AuthenticatedIdentity = "alice";
            return step;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] challenge)
        {
            throw new InvalidOperationException("No challenge was expected.");
        }

        public void CancelAuthentication()
        {
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            return stream;
        }
    }

    private class StlsBoundaryStream : Stream
    {
        byte[] InitialCapabilities;
        byte[] StlsResponse;
        byte[] ProtectedCapabilities;
        byte[] ActiveReadData;
        int ActiveReadPosition;
        MemoryStream Written = new MemoryStream();
        int PlaintextCapaCount;
        public bool TlsActive;
        public bool IsDisposed;

        public StlsBoundaryStream(
            string greeting,
            string? initialCapabilities,
            string stlsResponse,
            string? protectedCapabilities)
        {
            ActiveReadData = Encoding.ASCII.GetBytes(greeting);
            InitialCapabilities = EncodeOptional(initialCapabilities);
            StlsResponse = Encoding.ASCII.GetBytes(stlsResponse);
            ProtectedCapabilities = EncodeOptional(protectedCapabilities);
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
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public void ActivateTls()
        {
            TlsActive = true;
        }

        public bool ActiveChunkIsConsumed()
        {
            return ActiveReadPosition == ActiveReadData.Length;
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
            Written.Write(buffer, offset, count);
            string command = Encoding.ASCII.GetString(buffer, offset, count);
            if (command == "STLS\r\n")
            {
                SetActiveReadData(StlsResponse);
            }
            else if (command == "CAPA\r\n" && TlsActive)
            {
                SetActiveReadData(ProtectedCapabilities);
            }
            else if (command == "CAPA\r\n")
            {
                PlaintextCapaCount++;
                SetActiveReadData(InitialCapabilities);
            }
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing)
            {
                Written.Dispose();
            }
            base.Dispose(disposing);
        }

        int ReadCore(byte[] buffer, int offset, int count)
        {
            int remaining = ActiveReadData.Length - ActiveReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(ActiveReadData, ActiveReadPosition, buffer, offset, readCount);
            ActiveReadPosition += readCount;
            return readCount;
        }

        void SetActiveReadData(byte[] value)
        {
            ActiveReadData = value;
            ActiveReadPosition = 0;
        }

        static byte[] EncodeOptional(string? value)
        {
            if (value == null)
            {
                return new byte[0];
            }
            return Encoding.ASCII.GetBytes(value);
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
}
