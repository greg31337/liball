using System.Text;

namespace pop3.Tests;

public class Pop3ServerTlsUpgradeTests
{
    [TestCase(Pop3SecurityMode.StlsIfAvailable)]
    [TestCase(Pop3SecurityMode.StlsRequired)]
    public void StlsWritesAcceptanceDiscardsPlaintextAndRebuildsCapabilities(
        Pop3SecurityMode securityMode)
    {
        DuplexStream root = new DuplexStream("STLS\r\nUSER injected\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("CAPA\r\n");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        options.SecurityMode = securityMode;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory("PLAIN")
        };
        options.Tls.EnabledProtocols =
            System.Security.Authentication.SslProtocols.Tls12;
        options.Tls.RequireClientCertificate = true;
        options.Tls.CheckCertificateRevocation = false;
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3CapabilitySet before = new Pop3CapabilityBuilder().BuildCapabilities(
            options,
            engine.GetStateMachine());
        Pop3WireReader plaintextReader = engine.GetWireReader();

        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet? after = engine.UpgradeTransportToTls(
            stls,
            CancellationToken.None);

        Assert.That(before.SupportsStls, Is.True);
        Assert.That(before.SupportsUser, Is.False);
        Assert.That(before.SaslMechanisms, Is.Empty);
        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(provider.Options, Is.Not.SameAs(options.Tls));
        Assert.That(provider.Options!.EnabledProtocols,
            Is.EqualTo(System.Security.Authentication.SslProtocols.Tls12));
        Assert.That(provider.Options.RequireClientCertificate, Is.True);
        Assert.That(provider.Options.CheckCertificateRevocation, Is.False);
        Assert.That(provider.Options.LeaveInnerStreamOpen, Is.True);
        Assert.That(provider.OutputAtCall, Is.EqualTo(
            "+OK ready\r\n+OK Begin TLS negotiation\r\n"));
        Assert.That(root.BytesRead, Is.EqualTo(
            Encoding.ASCII.GetByteCount("STLS\r\nUSER injected\r\n")));
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.True);
        Assert.That(engine.GetStateMachine().GetTransportStream(),
            Is.SameAs(provider.Layer));
        Assert.That(after, Is.Not.Null);
        Assert.That(after!.SupportsStls, Is.False);
        Assert.That(after.SupportsUser, Is.True);
        Assert.That(after.SaslMechanisms, Is.EqualTo(new[] { "PLAIN" }));
        Assert.Throws<EndOfStreamException>(() =>
            plaintextReader.ReadCommand(
                Pop3Defaults.MaximumCommandLineOctets,
                CancellationToken.None));

        Pop3Command protectedCommand = engine.ReadNextCommand(
            CancellationToken.None);
        Assert.That(protectedCommand.Keyword, Is.EqualTo(Pop3Commands.Capa));
        engine.WriteCapabilitiesResponse(CancellationToken.None);
        Assert.That(root.WrittenText(), Does.Not.Contain("STLS\r\n"));
        Assert.That(root.WrittenText(), Does.Contain("USER\r\n"));
        Assert.That(root.WrittenText(), Does.Contain("SASL PLAIN\r\n"));

        engine.CloseSession();
        Assert.That(provider.Layer!.DisposeCount, Is.EqualTo(1));
        Assert.That(root.DisposeCount, Is.Zero);
        root.Dispose();
    }

    [Test]
    public void RequiredStlsOverridesCleartextCredentialOptInUntilUpgrade()
    {
        DuplexStream root = new DuplexStream(
            "USER alice\r\nAUTH PLAIN =\r\nSTLS\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("CAPA\r\n");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        options.SecurityMode = Pop3SecurityMode.StlsRequired;
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory("PLAIN")
        };
        Pop3ServerEngine engine = StartEngine(root, options, false);

        Pop3CapabilitySet before = new Pop3CapabilityBuilder().BuildCapabilities(
            options,
            engine.GetStateMachine());
        Pop3Response userResponse = engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult authResult = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3CapabilitySet? after = engine.UpgradeTransportToTls(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(before.SupportsStls, Is.True);
        Assert.That(before.SupportsUser, Is.False);
        Assert.That(before.SaslMechanisms, Is.Empty);
        Assert.That(userResponse.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(authResult.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(after, Is.Not.Null);
        Assert.That(after!.SupportsStls, Is.False);
        Assert.That(after.SupportsUser, Is.True);
        Assert.That(after.SaslMechanisms, Is.EqualTo(new[] { "PLAIN" }));
        Assert.That(root.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR Authentication failed.\r\n" +
            "-ERR [AUTH] Authentication failed.\r\n" +
            "+OK Begin TLS negotiation\r\n"));
        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.Capa));

        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void RepeatedStlsIsRejectedWithoutAnotherHandshake()
    {
        DuplexStream root = new DuplexStream("STLS\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider(
            "STLS\r\nCAPA\r\n");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        Pop3ServerEngine engine = StartEngine(root, options, false);

        Pop3Command first = engine.ReadNextCommand(CancellationToken.None);
        Assert.That(engine.UpgradeTransportToTls(first, CancellationToken.None),
            Is.Not.Null);
        Pop3Command repeated = engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet? rejected = engine.UpgradeTransportToTls(
            repeated,
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(rejected, Is.Null);
        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(root.WrittenText(), Does.EndWith(
            "-ERR TLS upgrade is not available.\r\n"));
        engine.CloseSession();
        root.Dispose();
    }

    [TestCase(Pop3SecurityMode.Plaintext, false)]
    [TestCase(Pop3SecurityMode.StlsIfAvailable, true)]
    public void UnavailableOrAlreadySecureStlsIsRejected(
        Pop3SecurityMode securityMode,
        bool alreadySecure)
    {
        DuplexStream root = new DuplexStream("STLS\r\nCAPA\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        options.SecurityMode = securityMode;
        Pop3ServerEngine engine = StartEngine(root, options, alreadySecure);

        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet? result = engine.UpgradeTransportToTls(
            stls,
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result, Is.Null);
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetIsSecure(),
            Is.EqualTo(alreadySecure));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(root.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR TLS upgrade is not available.\r\n"));
        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void StlsAfterUtf8IsRejectedWithoutHandshake()
    {
        DuplexStream root = new DuplexStream("STLS\r\nCAPA\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        Pop3ServerEngine engine = StartEngine(root, options, false);
        engine.GetStateMachine().EnableUtf8Mode();

        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet? result = engine.UpgradeTransportToTls(
            stls,
            CancellationToken.None);

        Assert.That(result, Is.Null);
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.False);
        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.Capa));
        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void TransactionStateRejectsStlsBeforeItReachesTlsHandler()
    {
        DuplexStream root = new DuplexStream("STLS\r\nCAPA\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        Pop3ServerEngine engine = StartEngine(root, options, false);
        engine.GetStateMachine().EnterTransaction();

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(root.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR Command is not valid in the current state.\r\n"));
        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void FailedHandshakeOccursAfterAcceptanceAndClosesTheSession()
    {
        DuplexStream root = new DuplexStream("STLS\r\n");
        FailingTlsProvider provider = new FailingTlsProvider();
        Pop3ServerOptions options = CreateStlsOptions(provider);
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.UpgradeTransportToTls(stls, CancellationToken.None))!;

        Assert.That(exception.SecurityMode,
            Is.EqualTo(Pop3SecurityMode.StlsIfAvailable));
        Assert.That(provider.OutputAtCall, Is.EqualTo(
            "+OK ready\r\n+OK Begin TLS negotiation\r\n"));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(root.DisposeCount, Is.Zero);
        root.Dispose();
    }

    [Test]
    public void HandshakeTimeoutOccursAfterAcceptanceAndClosesTheSession()
    {
        DuplexStream root = new DuplexStream("STLS\r\n");
        BlockingTlsProvider provider = new BlockingTlsProvider();
        Pop3ServerOptions options = CreateStlsOptions(provider);
        options.AuthenticationTimeoutMilliseconds = 50;
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);

        Pop3TlsException exception = Assert.Throws<Pop3TlsException>(() =>
            engine.UpgradeTransportToTls(stls, CancellationToken.None))!;

        Assert.That(exception.Message, Does.Contain("timed out"));
        Assert.That(provider.OutputAtCall, Is.EqualTo(
            "+OK ready\r\n+OK Begin TLS negotiation\r\n"));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(root.DisposeCount, Is.Zero);
        root.Dispose();
    }

    [Test]
    public void PreCanceledUpgradeWritesNothingAndCanBeRetried()
    {
        DuplexStream root = new DuplexStream("STLS\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            engine.UpgradeTransportToTls(stls, source.Token));
        Assert.That(root.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        Assert.That(provider.CallCount, Is.Zero);

        Assert.That(engine.UpgradeTransportToTls(
            stls,
            CancellationToken.None), Is.Not.Null);
        Assert.That(provider.CallCount, Is.EqualTo(1));
        engine.CloseSession();
        root.Dispose();
    }

    [Test]
    public void StlsHandlerRequiresThePendingStlsCommand()
    {
        DuplexStream root = new DuplexStream("CAPA\r\n");
        RecordingTlsProvider provider = new RecordingTlsProvider("");
        Pop3ServerOptions options = CreateStlsOptions(provider);
        Pop3ServerEngine engine = StartEngine(root, options, false);
        Pop3Command capa = engine.ReadNextCommand(CancellationToken.None);
        Pop3Command fakeStls = new Pop3Command();
        fakeStls.Keyword = Pop3Commands.Stls;

        Assert.Throws<Pop3StateException>(() =>
            engine.UpgradeTransportToTls(fakeStls, CancellationToken.None));
        Assert.That(provider.CallCount, Is.Zero);
        Assert.That(root.WrittenText(), Is.EqualTo("+OK ready\r\n"));

        engine.WriteCapabilitiesResponse(CancellationToken.None);
        Assert.That(capa.Keyword, Is.EqualTo(Pop3Commands.Capa));
        engine.CloseSession();
        root.Dispose();
    }

    static Pop3ServerEngine StartEngine(
        DuplexStream stream,
        Pop3ServerOptions options,
        bool isSecure)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.IsSecure = isSecure;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    static Pop3ServerOptions CreateStlsOptions(IPop3ServerTlsProvider provider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        options.TlsProvider = provider;
        return options;
    }

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
    {
        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
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

    sealed class SaslFactory : IPop3SaslServerFactory
    {
        string Mechanism;

        internal SaslFactory(string mechanism)
        {
            Mechanism = mechanism;
        }

        public string GetMechanism()
        {
            return Mechanism;
        }

        public IPop3SaslServerSession CreateSession(Pop3ServerConnection connection)
        {
            return null!;
        }
    }

    sealed class RecordingTlsProvider : IPop3ServerTlsProvider
    {
        string ProtectedInput;
        internal int CallCount;
        internal string OutputAtCall = "";
        internal Pop3TlsServerOptions? Options;
        internal LayerStream? Layer;

        internal RecordingTlsProvider(string protectedInput)
        {
            ProtectedInput = protectedInput;
        }

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            CallCount++;
            DuplexStream root = (DuplexStream)stream;
            OutputAtCall = root.WrittenText();
            Options = options;
            Layer = new LayerStream(root, ProtectedInput);
            return Layer;
        }
    }

    sealed class FailingTlsProvider : IPop3ServerTlsProvider
    {
        internal string OutputAtCall = "";

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            OutputAtCall = ((DuplexStream)stream).WrittenText();
            throw new InvalidDataException("handshake failed");
        }
    }

    sealed class BlockingTlsProvider : IPop3ServerTlsProvider
    {
        internal string OutputAtCall = "";

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            OutputAtCall = ((DuplexStream)stream).WrittenText();
            cancellationToken.WaitHandle.WaitOne();
            cancellationToken.ThrowIfCancellationRequested();
            return stream;
        }
    }

    sealed class LayerStream : Stream
    {
        DuplexStream Root;
        byte[] Input;
        int InputOffset;

        internal LayerStream(DuplexStream root, string input)
        {
            Root = root;
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal int DisposeCount;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            Root.Flush();
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
            Root.Write(buffer, offset, count);
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

    sealed class DuplexStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();

        internal DuplexStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal int BytesRead;
        internal int DisposeCount;

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
                BytesRead += copied;
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
