using System.Text;

namespace pop3.Tests;

public class Pop3ClientSaslSecurityLayerTests
{
    [Test]
    public void NewSecurityStreamActivatesAtSuccessBoundaryAndRefreshesCapabilities()
    {
        SaslBoundaryStream stream = new SaslBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nSASL TEST\r\nUSER\r\nX-OLD value\r\n.\r\n",
            "+OK authenticated\r\nPLAINTEXT-INJECTION",
            "+OK protected capabilities\r\nUIDL\r\nX-NEW value\r\n.\r\n");
        BoundarySaslClient client = new BoundarySaslClient(stream, true, false, null);
        Pop3ClientEngine engine = Connect(stream, Options());
        client.Engine = engine;

        Pop3AuthenticationResult result = engine.AuthenticateSasl(
            client,
            CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(client.ApplyCount, Is.EqualTo(1));
        Assert.That(client.SuppliedStream, Is.SameAs(stream));
        Assert.That(client.PreLayerChunkConsumed, Is.True);
        Assert.That(client.CapabilitiesEmptyAtActivation, Is.True);
        Assert.That(client.ReaderUnavailableAtActivation, Is.True);
        Assert.That(client.WriterUnavailableAtActivation, Is.True);
        Assert.That(client.CommandsAtActivation, Is.EqualTo(
            "CAPA\r\nAUTH TEST cHJvb2Y=\r\n"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUidl, Is.True);
        Assert.That(snapshot.Capabilities.Entries.Select(entry => entry.Name),
            Is.EqualTo(new[] { "UIDL", "X-NEW" }));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("protected capabilities"));
        Assert.That(engine.GetTransportStream(), Is.SameAs(client.SecurityStream));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH TEST cHJvb2Y=\r\nCAPA\r\n"));

        LayerStream layer = client.SecurityStream!;
        engine.CloseConnection();
        Assert.That(layer.IsDisposed, Is.True);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void SameStreamResultRepresentsNoSecurityLayerAndPreservesBufferedInput()
    {
        SaslBoundaryStream stream = new SaslBoundaryStream(
            "+OK ready\r\n",
            "+OK initial capabilities\r\nSASL TEST\r\nX-OLD value\r\n.\r\n",
            "+OK authenticated\r\n",
            "+OK transaction capabilities\r\nUIDL\r\n.\r\n");
        BoundarySaslClient client = new BoundarySaslClient(stream, false, false, null);
        Pop3ClientEngine engine = Connect(stream, Options());
        client.Engine = engine;

        Pop3AuthenticationResult result = engine.AuthenticateSasl(
            client,
            CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(client.ApplyCount, Is.EqualTo(1));
        Assert.That(client.CapabilitiesEmptyAtActivation, Is.True);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsSecure, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUidl, Is.True);
        Assert.That(engine.GetTransportStream(), Is.SameAs(stream));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH TEST cHJvb2Y=\r\nCAPA\r\n"));

        engine.CloseConnection();
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void SecurityLayerFailureClosesTheAuthenticatedConnection()
    {
        SaslBoundaryStream stream = new SaslBoundaryStream(
            "+OK ready\r\n",
            "+OK capabilities\r\nSASL TEST\r\n.\r\n",
            "+OK authenticated\r\n",
            "+OK unused\r\n.\r\n");
        BoundarySaslClient client = new BoundarySaslClient(stream, true, true, null);
        Pop3ClientEngine engine = Connect(stream, Options());
        client.Engine = engine;

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.AuthenticateSasl(client, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("ApplySecurityLayer"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(client.ApplyCount, Is.EqualTo(1));
        Assert.That(client.PreLayerChunkConsumed, Is.True);
        Assert.That(client.CapabilitiesEmptyAtActivation, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH TEST cHJvb2Y=\r\n"));
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void NullSecurityStreamIsRejectedAndClosesTheConnection()
    {
        SaslBoundaryStream stream = new SaslBoundaryStream(
            "+OK ready\r\n",
            "+OK capabilities\r\nSASL TEST\r\n.\r\n",
            "+OK authenticated\r\n",
            "+OK unused\r\n.\r\n");
        BoundarySaslClient client = new BoundarySaslClient(stream, true, false, null);
        client.ReturnNull = true;
        Pop3ClientEngine engine = Connect(stream, Options());
        client.Engine = engine;

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.AuthenticateSasl(client, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("ApplySecurityLayer"));
        Assert.That(exception.InnerException, Is.Null);
        Assert.That(client.ApplyCount, Is.EqualTo(1));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH TEST cHJvb2Y=\r\n"));
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void CancellationAfterSaslSuccessWaitsForLayerActivationThenCloses()
    {
        SaslBoundaryStream stream = new SaslBoundaryStream(
            "+OK ready\r\n",
            "+OK capabilities\r\nSASL TEST\r\n.\r\n",
            "+OK authenticated\r\n",
            "+OK unused\r\n.\r\n");
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            BoundarySaslClient client = new BoundarySaslClient(
                stream,
                false,
                false,
                cancellation);
            Pop3ClientEngine engine = Connect(stream, Options());
            client.Engine = engine;

            Assert.Throws<OperationCanceledException>(() => engine.AuthenticateSasl(
                client,
                cancellation.Token));

            Assert.That(client.ApplyCount, Is.EqualTo(1));
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(stream.WrittenText(), Is.EqualTo(
                "CAPA\r\nAUTH TEST cHJvb2Y=\r\n"));
        }
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void CancellationDuringSaslResponseReadCancelsTheMechanismAndCloses()
    {
        BlockingSaslStream stream = new BlockingSaslStream();
        Pop3ClientOptions options = Options();
        options.AuthenticationTimeoutMilliseconds = 2000;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        CleanupSaslClient client = new CleanupSaslClient();
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.CancelAfter(50);

            Assert.Throws<OperationCanceledException>(() => engine.AuthenticateSasl(
                client,
                cancellation.Token));
        }

        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(client.Response, Is.All.Zero);
        Assert.That(client.ApplyCount, Is.Zero);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH TEST c2VjcmV0LXJlc3BvbnNl\r\n"));
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void SaslResponseTimeoutCancelsTheMechanismAndCloses()
    {
        BlockingSaslStream stream = new BlockingSaslStream();
        Pop3ClientOptions options = Options();
        options.AuthenticationTimeoutMilliseconds = 50;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        CleanupSaslClient client = new CleanupSaslClient();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.AuthenticateSasl(client, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Auth));
        Assert.That(exception.IsTimeout, Is.True);
        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(client.Response, Is.All.Zero);
        Assert.That(client.ApplyCount, Is.Zero);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    private static Pop3ClientEngine Connect(
        SaslBoundaryStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        return engine;
    }

    private static Pop3ClientOptions Options()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 128;
        options.LeaveStreamOpen = true;
        return options;
    }

    private class BoundarySaslClient : IPop3SaslClient
    {
        SaslBoundaryStream BoundaryStream;
        bool ReturnNewStream;
        bool FailActivation;
        CancellationTokenSource? Cancellation;
        public Pop3ClientEngine? Engine;
        public int ApplyCount;
        public bool PreLayerChunkConsumed;
        public bool CapabilitiesEmptyAtActivation;
        public bool ReaderUnavailableAtActivation;
        public bool WriterUnavailableAtActivation;
        public string? CommandsAtActivation;
        public Stream? SuppliedStream;
        public LayerStream? SecurityStream;
        public bool ReturnNull;

        public BoundarySaslClient(
            SaslBoundaryStream boundaryStream,
            bool returnNewStream,
            bool failActivation,
            CancellationTokenSource? cancellation)
        {
            BoundaryStream = boundaryStream;
            ReturnNewStream = returnNewStream;
            FailActivation = failActivation;
            Cancellation = cancellation;
        }

        public string GetMechanism()
        {
            return "TEST";
        }

        public Pop3SaslStep BeginAuthentication()
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Status = Pop3SaslStepStatus.Success;
            step.Data = Encoding.ASCII.GetBytes("proof");
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
            ApplyCount++;
            SuppliedStream = stream;
            PreLayerChunkConsumed = BoundaryStream.ActiveChunkIsConsumed();
            CapabilitiesEmptyAtActivation = Engine!.GetSnapshot().Capabilities.Entries.Length == 0;
            ReaderUnavailableAtActivation = IsReaderUnavailable();
            WriterUnavailableAtActivation = IsWriterUnavailable();
            CommandsAtActivation = BoundaryStream.WrittenText();
            BoundaryStream.ActivateSecurityLayer();
            if (FailActivation)
            {
                throw new InvalidOperationException("Injected SASL security-layer failure.");
            }
            if (Cancellation != null)
            {
                Cancellation.Cancel();
            }
            if (ReturnNull)
            {
                return null!;
            }
            if (!ReturnNewStream)
            {
                return stream;
            }
            SecurityStream = new LayerStream(stream, true);
            return SecurityStream;
        }

        bool IsReaderUnavailable()
        {
            try
            {
                Engine!.GetWireReader();
            }
            catch (Pop3StateException)
            {
                return true;
            }
            return false;
        }

        bool IsWriterUnavailable()
        {
            try
            {
                Engine!.GetWireWriter();
            }
            catch (Pop3StateException)
            {
                return true;
            }
            return false;
        }

    }

    private class CleanupSaslClient : IPop3SaslClient
    {
        public byte[] Response = Encoding.ASCII.GetBytes("secret-response");
        public int CancelCount;
        public int ApplyCount;

        public string GetMechanism()
        {
            return "TEST";
        }

        public Pop3SaslStep BeginAuthentication()
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Status = Pop3SaslStepStatus.Success;
            step.Data = Response;
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
            CancelCount++;
            Array.Clear(Response, 0, Response.Length);
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            ApplyCount++;
            return stream;
        }
    }

    private class BlockingSaslStream : Stream
    {
        byte[] Prefix = Encoding.ASCII.GetBytes(
            "+OK ready\r\n+OK capabilities\r\nSASL TEST\r\n.\r\n");
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

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
            get { return Prefix.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
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
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int remaining = Prefix.Length - ReadPosition;
            if (remaining > 0)
            {
                int readCount = Math.Min(count, remaining);
                Array.Copy(Prefix, ReadPosition, buffer, offset, readCount);
                ReadPosition += readCount;
                return readCount;
            }
            await Task.Delay(Timeout.Infinite, cancellationToken);
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
            Written.Write(buffer, offset, count);
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
    }

    private class SaslBoundaryStream : Stream
    {
        byte[] InitialCapabilities;
        byte[] AuthenticationResponse;
        byte[] ProtectedCapabilities;
        byte[] ActiveReadData;
        int ActiveReadPosition;
        MemoryStream Written = new MemoryStream();
        bool SecurityLayerActive;
        public bool IsDisposed;

        public SaslBoundaryStream(
            string greeting,
            string initialCapabilities,
            string authenticationResponse,
            string protectedCapabilities)
        {
            ActiveReadData = Encoding.ASCII.GetBytes(greeting);
            InitialCapabilities = Encoding.ASCII.GetBytes(initialCapabilities);
            AuthenticationResponse = Encoding.ASCII.GetBytes(authenticationResponse);
            ProtectedCapabilities = Encoding.ASCII.GetBytes(protectedCapabilities);
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

        public void ActivateSecurityLayer()
        {
            SecurityLayerActive = true;
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
            if (command == "CAPA\r\n" && SecurityLayerActive)
            {
                SetActiveReadData(ProtectedCapabilities);
            }
            else if (command == "CAPA\r\n")
            {
                SetActiveReadData(InitialCapabilities);
            }
            else if (command.StartsWith("AUTH TEST ", StringComparison.Ordinal))
            {
                SetActiveReadData(AuthenticationResponse);
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
