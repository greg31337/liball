using System.Text;

namespace pop3.Tests;

public class Pop3ServerSaslSecurityLayerTests
{
    [Test]
    public void NewLayerActivatesAfterSuccessAndDiscardsPreLayerInput()
    {
        BoundaryStream root = new BoundaryStream(
            "AUTH TEST cHJvb2Y=\r\nPLAINTEXT INJECTION\r\n");
        LayerStream saslLayer = new LayerStream(root, "CAPA\r\n", false);
        SaslSession session = new SaslSession(root, saslLayer);
        Pop3ServerOptions options = Options(new SaslFactory(session));
        Pop3ServerEngine engine = StartEngine(root, options, false);
        session.Engine = engine;
        Pop3WireReader plaintextReader = engine.GetWireReader();

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.WriteAuthenticationSuccessResponse(
            Positive("authenticated"),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(session.ApplyCount, Is.EqualTo(1));
        Assert.That(session.SuppliedStream, Is.SameAs(root));
        Assert.That(session.OutputAtApply, Is.EqualTo(
            "+OK ready\r\n+OK authenticated\r\n"));
        Assert.That(session.ReaderUnavailableAtApply, Is.True);
        Assert.That(session.WriterUnavailableAtApply, Is.True);
        Assert.That(engine.GetStateMachine().GetTransportStream(),
            Is.SameAs(saslLayer));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.True);
        Assert.Throws<EndOfStreamException>(() =>
            plaintextReader.ReadCommand(
                Pop3Defaults.MaximumCommandLineOctets,
                CancellationToken.None));

        Pop3Command protectedCommand = engine.ReadNextCommand(
            CancellationToken.None);
        Assert.That(protectedCommand.Keyword, Is.EqualTo(Pop3Commands.Capa));
        engine.WriteNextResponse(Positive("protected"), CancellationToken.None);
        Assert.That(saslLayer.WriteCount, Is.GreaterThan(0));
        Assert.That(root.WrittenText(), Does.EndWith(
            "+OK protected\r\n"));

        engine.CloseSession();
        Assert.That(saslLayer.DisposeCount, Is.EqualTo(1));
        Assert.That(root.DisposeCount, Is.Zero);
        root.Dispose();
    }

    [Test]
    public void SameStreamResultPreservesExistingWireComponentsAndBufferedInput()
    {
        BoundaryStream root = new BoundaryStream(
            "AUTH TEST cHJvb2Y=\r\nCAPA\r\n");
        SaslSession session = new SaslSession(root, root);
        Pop3ServerOptions options = Options(new SaslFactory(session));
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3WireReader reader = engine.GetWireReader();
        Pop3WireWriter writer = engine.GetWireWriter();
        engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        engine.WriteAuthenticationSuccessResponse(
            Positive("authenticated"),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(session.ApplyCount, Is.EqualTo(1));
        Assert.That(engine.GetStateMachine().GetTransportStream(),
            Is.SameAs(root));
        Assert.That(engine.GetWireReader(), Is.SameAs(reader));
        Assert.That(engine.GetWireWriter(), Is.SameAs(writer));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.False);
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(root.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK authenticated\r\n"));

        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void SaslLayerWrapsTheCurrentTlsLayer()
    {
        BoundaryStream root = new BoundaryStream("STLS\r\n");
        LayerStream tlsLayer = new LayerStream(
            root,
            "AUTH TEST cHJvb2Y=\r\nPLAINTEXT INJECTION\r\n",
            false);
        LayerStream saslLayer = new LayerStream(
            tlsLayer,
            "CAPA\r\n",
            true);
        SaslSession session = new SaslSession(root, saslLayer);
        RecordingTlsProvider tlsProvider = new RecordingTlsProvider(tlsLayer);
        Pop3ServerOptions options = Options(new SaslFactory(session));
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        options.TlsProvider = tlsProvider;
        Pop3ServerEngine engine = StartEngine(root, options, false);

        engine.UpgradeTransportToTls(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.WriteAuthenticationSuccessResponse(
            Positive("authenticated"),
            CancellationToken.None);

        Assert.That(tlsProvider.CallCount, Is.EqualTo(1));
        Assert.That(session.SuppliedStream, Is.SameAs(tlsLayer));
        Assert.That(engine.GetStateMachine().GetTransportStream(),
            Is.SameAs(saslLayer));
        Assert.That(root.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Begin TLS negotiation\r\n" +
            "+OK authenticated\r\n"));

        Pop3Command protectedCommand = engine.ReadNextCommand(
            CancellationToken.None);
        Assert.That(protectedCommand.Keyword, Is.EqualTo(Pop3Commands.Capa));
        engine.WriteNextResponse(Positive("protected"), CancellationToken.None);
        Assert.That(saslLayer.WriteCount, Is.GreaterThan(0));
        Assert.That(tlsLayer.WriteCount, Is.GreaterThan(0));

        engine.CloseSession();
        Assert.That(saslLayer.DisposeCount, Is.EqualTo(1));
        Assert.That(tlsLayer.DisposeCount, Is.EqualTo(1));
        Assert.That(root.DisposeCount, Is.Zero);
        root.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void InvalidSecurityStreamsAreRejectedAfterTheSuccessResponse(
        int scenario)
    {
        BoundaryStream root = new BoundaryStream("AUTH TEST cHJvb2Y=\r\n");
        InvalidStream? invalid = null;
        Stream? returned;
        if (scenario == 0)
        {
            returned = null;
        }
        else
        {
            invalid = new InvalidStream(scenario == 1, scenario == 2);
            returned = invalid;
        }
        SaslSession session = new SaslSession(root, returned);
        Pop3ServerEngine engine = StartEngine(
            root,
            Options(new SaslFactory(session)),
            false);
        engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            engine.WriteAuthenticationSuccessResponse(
                Positive("authenticated"),
                CancellationToken.None));

        Assert.That(session.ApplyCount, Is.EqualTo(1));
        Assert.That(root.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK authenticated\r\n"));
        if (invalid != null)
        {
            Assert.That(invalid.DisposeCount, Is.EqualTo(1));
        }
        Assert.Throws<Pop3StateException>(() => engine.GetWireReader());
        Assert.Throws<Pop3StateException>(() => engine.GetWireWriter());

        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void NonSaslAuthenticationCompletionDoesNotReplaceTheTransport()
    {
        PasswordProvider provider = new PasswordProvider();
        BoundaryStream root = new BoundaryStream(
            "USER alice\r\nPASS secret\r\nCAPA\r\n");
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = provider;
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3WireReader reader = engine.GetWireReader();

        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.WriteAuthenticationSuccessResponse(
            Positive("authenticated"),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(engine.GetStateMachine().GetTransportStream(), Is.SameAs(root));
        Assert.That(engine.GetWireReader(), Is.SameAs(reader));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));

        engine.CloseSession();
        root.Dispose();
    }

    static Pop3Response Positive(string text)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = text;
        return response;
    }

    static Pop3ServerOptions Options(IPop3SaslServerFactory factory)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.SaslFactories = new IPop3SaslServerFactory[] { factory };
        options.BufferSize = 128;
        return options;
    }

    static Pop3ServerEngine StartEngine(
        BoundaryStream root,
        Pop3ServerOptions options,
        bool secure)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = root;
        connection.IsSecure = secure;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    sealed class SaslFactory : IPop3SaslServerFactory
    {
        SaslSession Session;

        internal SaslFactory(SaslSession session)
        {
            Session = session;
        }

        public string GetMechanism()
        {
            return "TEST";
        }

        public IPop3SaslServerSession CreateSession(
            Pop3ServerConnection connection)
        {
            return Session;
        }
    }

    sealed class SaslSession : IPop3SaslServerSession
    {
        BoundaryStream Root;
        Stream? ReturnedStream;
        internal int ApplyCount;
        internal Stream? SuppliedStream;
        internal string? OutputAtApply;
        internal Pop3ServerEngine? Engine;
        internal bool ReaderUnavailableAtApply;
        internal bool WriterUnavailableAtApply;

        internal SaslSession(BoundaryStream root, Stream? returnedStream)
        {
            Root = root;
            ReturnedStream = returnedStream;
        }

        public Pop3SaslStep BeginAuthentication(
            byte[] initialResponse,
            bool hasInitialResponse)
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Status = Pop3SaslStepStatus.Success;
            return step;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] response)
        {
            throw new InvalidOperationException();
        }

        public void CancelAuthentication()
        {
        }

        public Pop3AuthenticationResult GetAuthenticationResult()
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.AuthenticatedIdentity = "alice";
            result.MaildropKey = "mail";
            return result;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            ApplyCount++;
            SuppliedStream = stream;
            OutputAtApply = Root.WrittenText();
            if (Engine != null)
            {
                ReaderUnavailableAtApply = ReaderIsUnavailable();
                WriterUnavailableAtApply = WriterIsUnavailable();
            }
            return ReturnedStream!;
        }

        bool ReaderIsUnavailable()
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

        bool WriterIsUnavailable()
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

    sealed class RecordingTlsProvider : IPop3ServerTlsProvider
    {
        LayerStream Layer;
        internal int CallCount;

        internal RecordingTlsProvider(LayerStream layer)
        {
            Layer = layer;
        }

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Layer;
        }
    }

    sealed class PasswordProvider : IPop3AuthenticationProvider
    {
        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            return result;
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.AuthenticatedIdentity = credentials.Username;
            result.MaildropKey = "mail";
            return result;
        }

        public Pop3AuthenticationResult AuthenticateApop(
            Pop3ApopRequest request,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }

        public Pop3AuthenticationResult AuthenticateSaslPlain(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }
    }

    sealed class InvalidStream : Stream
    {
        bool Readable;
        bool Writable;
        internal int DisposeCount;

        internal InvalidStream(bool readable, bool writable)
        {
            Readable = readable;
            Writable = writable;
        }

        public override bool CanRead => Readable;
        public override bool CanSeek => false;
        public override bool CanWrite => Writable;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            base.Dispose(disposing);
        }
    }

    sealed class LayerStream : Stream
    {
        Stream Inner;
        byte[] Input;
        int InputOffset;
        bool DisposeInner;
        internal int WriteCount;
        internal int DisposeCount;

        internal LayerStream(Stream inner, string input, bool disposeInner)
        {
            Inner = inner;
            Input = Encoding.ASCII.GetBytes(input);
            DisposeInner = disposeInner;
        }

        public override bool CanRead => DisposeCount == 0;
        public override bool CanSeek => false;
        public override bool CanWrite => DisposeCount == 0;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            Inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = Input.Length - InputOffset;
            int copied = Math.Min(count, available);
            if (copied > 0)
            {
                Array.Copy(Input, InputOffset, buffer, offset, copied);
                InputOffset += copied;
            }
            return copied;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCount++;
            Inner.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            if (disposing && DisposeInner)
            {
                Inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    sealed class BoundaryStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();
        internal int DisposeCount;

        internal BoundaryStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal string WrittenText()
        {
            return Encoding.ASCII.GetString(Output.ToArray());
        }

        public override bool CanRead => DisposeCount == 0;
        public override bool CanSeek => false;
        public override bool CanWrite => DisposeCount == 0;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = Input.Length - InputOffset;
            int copied = Math.Min(count, available);
            if (copied > 0)
            {
                Array.Copy(Input, InputOffset, buffer, offset, copied);
                InputOffset += copied;
            }
            return copied;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Output.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            base.Dispose(disposing);
        }
    }
}
