using System.Text;

namespace pop3.Tests;

public class Pop3PlainSaslServerTests
{
    [Test]
    public void InitialResponseDecodesUtf8CredentialsAndCallsProviderInOrder()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3ServerConnection connection = Connection(new MemoryStream(), true);
        using CancellationTokenSource source = new CancellationTokenSource();
        Pop3PlainSaslServer session = new Pop3PlainSaslServer(
            provider,
            connection,
            source.Token);
        byte[] payload = Encoding.UTF8.GetBytes(
            "requested-user\0álîçé😀\0pä ss🔑");

        Pop3SaslStep step = session.BeginAuthentication(payload, true);
        Pop3AuthenticationResult result = session.GetAuthenticationResult();

        Assert.That(step.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(step.HasData, Is.False);
        Assert.That(step.Data, Is.Empty);
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("canonical-user"));
        Assert.That(result.MaildropKey, Is.EqualTo("maildrop"));
        Assert.That(provider.Events, Is.EqualTo(new[] { "delay", "plain" }));
        Assert.That(provider.DelayUsername, Is.EqualTo("álîçé😀"));
        Assert.That(provider.Credentials!.AuthorizationIdentity,
            Is.EqualTo("requested-user"));
        Assert.That(provider.Credentials.Username, Is.EqualTo("álîçé😀"));
        Assert.That(provider.Credentials.Password, Is.EqualTo("pä ss🔑"));
        Assert.That(provider.Connection, Is.SameAs(connection));
        Assert.That(provider.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(payload, Is.All.EqualTo(0));

        MemoryStream stream = new MemoryStream();
        Assert.That(session.ApplySecurityLayer(stream), Is.SameAs(stream));
        stream.Dispose();
    }

    [Test]
    public void MissingInitialResponseRequestsAnEmptyChallengeThenAuthenticates()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3PlainSaslServer session = new Pop3PlainSaslServer(
            provider,
            Connection(new MemoryStream(), true),
            CancellationToken.None);

        Pop3SaslStep challenge = session.BeginAuthentication(
            Array.Empty<byte>(),
            false);
        byte[] payload = Encoding.UTF8.GetBytes("\0alice\0secret");
        Pop3SaslStep completion = session.ContinueAuthentication(payload);

        Assert.That(challenge.Status, Is.EqualTo(Pop3SaslStepStatus.Continue));
        Assert.That(challenge.HasData, Is.True);
        Assert.That(challenge.Data, Is.Empty);
        Assert.That(completion.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(provider.Credentials!.AuthorizationIdentity, Is.Null);
        Assert.That(provider.Credentials.Username, Is.EqualTo("alice"));
        Assert.That(provider.Credentials.Password, Is.EqualTo("secret"));
        Assert.That(payload, Is.All.EqualTo(0));
    }

    [TestCaseSource(nameof(MalformedPlainPayloads))]
    public void MalformedFieldsAndUtf8AreRejectedBeforeProviderCalls(
        byte[] payload)
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3PlainSaslServer session = new Pop3PlainSaslServer(
            provider,
            Connection(new MemoryStream(), true),
            CancellationToken.None);

        Pop3SaslStep step = session.BeginAuthentication(payload, true);
        Pop3AuthenticationResult result = session.GetAuthenticationResult();

        Assert.That(step.Status, Is.EqualTo(Pop3SaslStepStatus.Rejected));
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.PlainCallCount, Is.Zero);
        Assert.That(payload, Is.All.EqualTo(0));
    }

    [TestCase(Pop3AuthenticationStatus.Rejected)]
    [TestCase(Pop3AuthenticationStatus.LoginDelay)]
    [TestCase(Pop3AuthenticationStatus.InUse)]
    [TestCase(Pop3AuthenticationStatus.TemporaryFailure)]
    [TestCase(Pop3AuthenticationStatus.PermanentFailure)]
    public void LoginDelayFailureSkipsPlainAuthentication(
        Pop3AuthenticationStatus status)
    {
        AuthenticationProvider provider = SuccessfulProvider();
        provider.DelayResult = Result(status, null, null, "delay detail", 15);
        Pop3PlainSaslServer session = new Pop3PlainSaslServer(
            provider,
            Connection(new MemoryStream(), true),
            CancellationToken.None);

        Pop3SaslStep step = session.BeginAuthentication(
            Encoding.UTF8.GetBytes("\0alice\0secret"),
            true);
        Pop3AuthenticationResult result = session.GetAuthenticationResult();

        Assert.That(step.Status, Is.EqualTo(Pop3SaslStepStatus.Rejected));
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.FailureText, Is.EqualTo("delay detail"));
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(15));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.PlainCallCount, Is.Zero);
    }

    [TestCase(Pop3AuthenticationStatus.Rejected)]
    [TestCase(Pop3AuthenticationStatus.LoginDelay)]
    [TestCase(Pop3AuthenticationStatus.InUse)]
    [TestCase(Pop3AuthenticationStatus.TemporaryFailure)]
    [TestCase(Pop3AuthenticationStatus.PermanentFailure)]
    public void PlainProviderFailuresRemainClassified(
        Pop3AuthenticationStatus status)
    {
        AuthenticationProvider provider = SuccessfulProvider();
        provider.PlainResult = Result(
            status,
            null,
            null,
            "authentication detail",
            20);
        Pop3PlainSaslServer session = new Pop3PlainSaslServer(
            provider,
            Connection(new MemoryStream(), true),
            CancellationToken.None);

        Pop3SaslStep step = session.BeginAuthentication(
            Encoding.UTF8.GetBytes("\0alice\0secret"),
            true);
        Pop3AuthenticationResult result = session.GetAuthenticationResult();

        Assert.That(step.Status, Is.EqualTo(Pop3SaslStepStatus.Rejected));
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.FailureText, Is.EqualTo("authentication detail"));
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(20));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.PlainCallCount, Is.EqualTo(1));
    }

    [Test]
    public void CapabilityAdvertisesBuiltInPlainOnlyWhenPolicyAllowsCredentials()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();
        state.OpenConnection(
            Connection(new MemoryStream(), false),
            options,
            1);
        Pop3CapabilityBuilder builder = new Pop3CapabilityBuilder();

        Assert.That(builder.BuildCapabilities(options, state).SaslMechanisms,
            Is.Empty);

        options.AllowUnprotectedCredentials = true;
        Assert.That(builder.BuildCapabilities(options, state).SaslMechanisms,
            Is.EqualTo(new[] { "PLAIN" }));

        options.AllowUnprotectedCredentials = false;
        state.ReplaceTransportStream(state.GetTransportStream(), true);
        Assert.That(builder.BuildCapabilities(options, state).SaslMechanisms,
            Is.EqualTo(new[] { "PLAIN" }));

        state.CloseConnection();
    }

    [Test]
    public void EngineAuthenticatesWithBuiltInPlainOnAProtectedConnection()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        string token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("delegate\0alice\0secret"));
        DuplexStream stream = new DuplexStream(
            "AUTH plain " + token + "\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, true);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.PlainCallCount, Is.EqualTo(1));
        Assert.That(provider.Credentials!.Username, Is.EqualTo("alice"));
        Assert.That(provider.Credentials.AuthorizationIdentity,
            Is.EqualTo("delegate"));
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void EngineRejectsBuiltInPlainOnAnUnprotectedConnection()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        string token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("\0alice\0secret"));
        DuplexStream stream = new DuplexStream(
            "AUTH PLAIN " + token + "\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.PlainCallCount, Is.Zero);
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR [AUTH] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void ExplicitCleartextOptInAllowsDeferredPlainAuthentication()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        options.AllowUnprotectedCredentials = true;
        string token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("\0alice\0secret"));
        DuplexStream stream = new DuplexStream(
            "AUTH PLAIN\r\n" + token + "\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(provider.PlainCallCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+ \r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void CallerPlainFactoryTakesPrecedenceOverBuiltInPlain()
    {
        AuthenticationProvider provider = SuccessfulProvider();
        CustomPlainSession customSession = new CustomPlainSession();
        Pop3ServerOptions options = CreateOptions(provider);
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new CustomPlainFactory(customSession)
        };
        string token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("not-a-plain-message"));
        DuplexStream stream = new DuplexStream(
            "AUTH PLAIN " + token + "\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, true);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("custom-user"));
        Assert.That(customSession.BeginCount, Is.EqualTo(1));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.PlainCallCount, Is.Zero);

        engine.CloseSession();
        stream.Dispose();
    }

    static IEnumerable<TestCaseData> MalformedPlainPayloads()
    {
        yield return new TestCaseData(Array.Empty<byte>())
            .SetName("PlainRejectsEmptyPayload");
        yield return new TestCaseData(Encoding.UTF8.GetBytes("alice"))
            .SetName("PlainRejectsMissingSeparators");
        yield return new TestCaseData(Encoding.UTF8.GetBytes("alice\0secret"))
            .SetName("PlainRejectsOneSeparator");
        yield return new TestCaseData(Encoding.UTF8.GetBytes("\0\0secret"))
            .SetName("PlainRejectsEmptyAuthenticationIdentity");
        yield return new TestCaseData(Encoding.UTF8.GetBytes("\0alice\0"))
            .SetName("PlainRejectsEmptyPassword");
        yield return new TestCaseData(Encoding.UTF8.GetBytes(
            "delegate\0alice\0secret\0extra"))
            .SetName("PlainRejectsAdditionalSeparators");
        yield return new TestCaseData(new byte[]
        {
            0, 97, 108, 105, 99, 101, 0, 192, 175
        }).SetName("PlainRejectsInvalidPasswordUtf8");
        yield return new TestCaseData(new byte[]
        {
            237, 160, 128, 0, 97, 108, 105, 99, 101, 0, 120
        }).SetName("PlainRejectsInvalidAuthorizationIdentityUtf8");
        yield return new TestCaseData(new byte[]
        {
            0, 244, 144, 128, 128, 0, 120
        }).SetName("PlainRejectsInvalidAuthenticationIdentityUtf8");
    }

    static AuthenticationProvider SuccessfulProvider()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.PlainResult = Result(
            Pop3AuthenticationStatus.Success,
            "canonical-user",
            "maildrop",
            null,
            -1);
        return provider;
    }

    static Pop3AuthenticationResult Result(
        Pop3AuthenticationStatus status,
        string? identity,
        string? maildrop,
        string? failureText,
        int retryAfterSeconds)
    {
        Pop3AuthenticationResult result = new Pop3AuthenticationResult();
        result.Status = status;
        result.AuthenticatedIdentity = identity;
        result.MaildropKey = maildrop;
        result.FailureText = failureText;
        result.RetryAfterSeconds = retryAfterSeconds;
        return result;
    }

    static Pop3ServerOptions CreateOptions(
        IPop3AuthenticationProvider provider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AuthenticationProvider = provider;
        return options;
    }

    static Pop3ServerConnection Connection(Stream stream, bool secure)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.IsSecure = secure;
        connection.LeaveOpen = true;
        connection.LocalEndPoint = "local";
        connection.RemoteEndPoint = "remote";
        return connection;
    }

    static Pop3ServerEngine StartEngine(
        DuplexStream stream,
        Pop3ServerOptions options,
        bool secure)
    {
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(
            Connection(stream, secure),
            options,
            CancellationToken.None);
        return engine;
    }

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
    {
        internal Pop3AuthenticationResult DelayResult = Result(
            Pop3AuthenticationStatus.Success,
            null,
            null,
            null,
            -1);
        internal Pop3AuthenticationResult PlainResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            null,
            -1);
        internal int DelayCallCount;
        internal int PlainCallCount;
        internal string? DelayUsername;
        internal Pop3Credentials? Credentials;
        internal Pop3ServerConnection? Connection;
        internal CancellationToken CancellationToken;
        internal List<string> Events = new List<string>();

        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            DelayCallCount++;
            DelayUsername = username;
            Connection = connection;
            CancellationToken = cancellationToken;
            Events.Add("delay");
            return DelayResult;
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
            PlainCallCount++;
            Credentials = credentials;
            Connection = connection;
            CancellationToken = cancellationToken;
            Events.Add("plain");
            return PlainResult;
        }
    }

    sealed class CustomPlainFactory : IPop3SaslServerFactory
    {
        CustomPlainSession Session;

        internal CustomPlainFactory(CustomPlainSession session)
        {
            Session = session;
        }

        public string GetMechanism()
        {
            return "plain";
        }

        public IPop3SaslServerSession CreateSession(
            Pop3ServerConnection connection)
        {
            return Session;
        }
    }

    sealed class CustomPlainSession : IPop3SaslServerSession
    {
        internal int BeginCount;

        public Pop3SaslStep BeginAuthentication(
            byte[] initialResponse,
            bool hasInitialResponse)
        {
            BeginCount++;
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
            return Result(
                Pop3AuthenticationStatus.Success,
                "custom-user",
                "custom-maildrop",
                null,
                -1);
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            return stream;
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

        internal string WrittenText()
        {
            return Encoding.ASCII.GetString(Output.ToArray());
        }

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
    }
}
