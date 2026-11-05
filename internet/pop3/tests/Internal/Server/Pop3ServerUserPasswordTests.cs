using System.Text;

namespace pop3.Tests;

public class Pop3ServerUserPasswordTests
{
    [Test]
    public void UserAndPassCallProvidersInOrderAndReturnSuccessfulIdentity()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Success,
            "canonical-alice",
            "maildrop-42",
            null,
            -1);
        FakeProtocolLogger logger = new FakeProtocolLogger();
        Pop3ServerOptions options = CreateOptions(provider);
        options.Logger = logger;
        DuplexStream stream = new DuplexStream(
            "uSeR alice\r\npAsS secret\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        Pop3Command user = engine.ReadNextCommand(CancellationToken.None);
        Pop3Response userResponse = engine.HandleUserCommand(
            user,
            CancellationToken.None);
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.AuthenticationCallCount, Is.Zero);

        Pop3Command pass = engine.ReadNextCommand(CancellationToken.None);
        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            pass,
            CancellationToken.None);

        Assert.That(userResponse.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("canonical-alice"));
        Assert.That(result.MaildropKey, Is.EqualTo("maildrop-42"));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.AuthenticationCallCount, Is.EqualTo(1));
        Assert.That(provider.Events, Is.EqualTo(new[] { "delay", "authenticate" }));
        Assert.That(provider.DelayUsername, Is.EqualTo("alice"));
        Assert.That(provider.Credentials!.Username, Is.EqualTo("alice"));
        Assert.That(provider.Credentials.Password, Is.EqualTo("secret"));
        Assert.That(provider.Credentials.AuthorizationIdentity, Is.Null);
        Assert.That(provider.Connection!.IsSecure, Is.False);
        Assert.That(provider.Connection.LocalEndPoint, Is.EqualTo("local"));
        Assert.That(provider.Connection.RemoteEndPoint, Is.EqualTo("remote"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Send password.\r\n"));
        Assert.That(logger.Entries.Select(entry => entry.Text), Is.EqualTo(new[]
        {
            "USER alice",
            "+OK Send password.",
            "PASS [redacted]"
        }));
        Assert.That(logger.Entries.Select(entry => entry.IsRedacted),
            Is.EqualTo(new[] { false, false, true }));
        Assert.That(string.Join("|", logger.Entries.Select(entry => entry.Text)),
            Does.Not.Contain("secret"));
        Assert.Throws<Pop3StateException>(() =>
            engine.ReadNextCommand(CancellationToken.None));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void PassWithoutUserIsUniformlyRejectedWithoutProviderCalls()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream("PASS secret\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        Pop3Command pass = engine.ReadNextCommand(CancellationToken.None);
        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            pass,
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.AuthenticationCallCount, Is.Zero);
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR Authentication failed.\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void ANewUserReplacesThePreviousClaimWithoutAccountLookup()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Success,
            "bob",
            "bob-mail",
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream(
            "USER nonexistent\r\nUSER bob\r\nPASS correct\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.DelayUsername, Is.EqualTo("bob"));
        Assert.That(provider.Credentials!.Username, Is.EqualTo("bob"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "+OK Send password.\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(Pop3AuthenticationStatus.Rejected, false, "[AUTH]", 1)]
    [TestCase(Pop3AuthenticationStatus.LoginDelay, true, "[LOGIN-DELAY]", 0)]
    [TestCase(Pop3AuthenticationStatus.InUse, true, "[IN-USE]", 0)]
    [TestCase(Pop3AuthenticationStatus.TemporaryFailure, true, "[SYS/TEMP]", 0)]
    [TestCase(Pop3AuthenticationStatus.PermanentFailure, true, "[SYS/PERM]", 0)]
    public void FailuresUseUniformTextAndMachineReadableClassification(
        Pop3AuthenticationStatus status,
        bool failDelayCheck,
        string expectedCode,
        int expectedAuthenticationCalls)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        Pop3AuthenticationResult failure = Result(
            status,
            null,
            null,
            "provider detail for alice",
            status == Pop3AuthenticationStatus.LoginDelay ? 90 : -1);
        if (failDelayCheck)
        {
            provider.DelayResult = failure;
        }
        else
        {
            provider.AuthenticationResult = failure;
        }
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS wrong\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.FailureText, Is.EqualTo("provider detail for alice"));
        Assert.That(result.RetryAfterSeconds,
            Is.EqualTo(status == Pop3AuthenticationStatus.LoginDelay ? 90 : -1));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.AuthenticationCallCount,
            Is.EqualTo(expectedAuthenticationCalls));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "-ERR " + expectedCode + " Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("alice"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("wrong"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("provider detail"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void DisabledResponseCodesMakeCredentialFailuresIndistinguishable()
    {
        string first = RunRejectedAuthentication("unknown", "no such account");
        string second = RunRejectedAuthentication("known", "wrong password");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "-ERR Authentication failed.\r\n"));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void UserPolicyRejectsBeforeProviderWork(
        bool enabled,
        bool hasProvider)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        Pop3ServerOptions options = CreateOptions(
            hasProvider ? provider : null);
        options.EnableUserPassword = enabled;
        options.AllowUnprotectedCredentials = false;
        DuplexStream stream = new DuplexStream("USER alice\r\nPASS secret\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        Pop3Response userResponse = engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult passResult = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(userResponse.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(userResponse.ResponseCodeLevels, Is.Empty);
        Assert.That(passResult.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.AuthenticationCallCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR Authentication failed.\r\n" +
            "-ERR Authentication failed.\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void SecureTransportAllowsCredentialsWithoutCleartextOptIn()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Success,
            "alice",
            "mail",
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        options.AllowUnprotectedCredentials = false;
        DuplexStream stream = new DuplexStream("USER alice\r\nPASS secret\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, true);

        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(provider.AuthenticationCallCount, Is.EqualTo(1));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void FailedPassClearsUserAndRequiresAReplacementUserCommand()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS first\r\nPASS second\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);

        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult first = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult second = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(first.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(second.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.AuthenticationCallCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR [AUTH] Authentication failed.\r\n" +
            "-ERR Authentication failed.\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void PreCanceledPassPreservesPendingUserForRetry()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Success,
            "alice",
            "mail",
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream("USER alice\r\nPASS secret\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command pass = engine.ReadNextCommand(CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            engine.HandlePasswordCommand(pass, source.Token));
        Assert.That(provider.DelayCallCount, Is.Zero);

        Pop3AuthenticationResult result = engine.HandlePasswordCommand(
            pass,
            CancellationToken.None);
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void UserBeginsAuthenticationAndMakesStlsTooLate()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        CountingTlsProvider tlsProvider = new CountingTlsProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        options.TlsProvider = tlsProvider;
        DuplexStream stream = new DuplexStream("USER alice\r\nSTLS\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Pop3Command stls = engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet? result = engine.UpgradeTransportToTls(
            stls,
            CancellationToken.None);

        Assert.That(result, Is.Null);
        Assert.That(tlsProvider.CallCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetIsSecure(), Is.False);
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR TLS upgrade is not available.\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void InvalidSuccessfulProviderResultIsRejectedAndClearsTheExchange()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Success,
            null,
            null,
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\nPASS again\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command pass = engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            engine.HandlePasswordCommand(
                pass,
                CancellationToken.None));
        Pop3AuthenticationResult retry = engine.HandlePasswordCommand(
            pass,
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(retry.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.AuthenticationCallCount, Is.EqualTo(1));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Pass));

        engine.CloseSession();
        stream.Dispose();
    }

    static string RunRejectedAuthentication(string username, string failureText)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            failureText,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        options.EnableResponseCodes = false;
        DuplexStream stream = new DuplexStream(
            "USER " + username + "\r\nPASS bad\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options, false);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        string output = stream.WrittenText();
        engine.CloseSession();
        stream.Dispose();
        return output;
    }

    static Pop3ServerEngine StartEngine(
        DuplexStream stream,
        Pop3ServerOptions options,
        bool secure)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.IsSecure = secure;
        connection.LeaveOpen = true;
        connection.LocalEndPoint = "local";
        connection.RemoteEndPoint = "remote";
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    static Pop3ServerOptions CreateOptions(
        IPop3AuthenticationProvider? provider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AuthenticationProvider = provider;
        options.AllowUnprotectedCredentials = true;
        return options;
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

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
    {
        internal Pop3AuthenticationResult DelayResult = Result(
            Pop3AuthenticationStatus.Success,
            null,
            null,
            null,
            -1);
        internal Pop3AuthenticationResult AuthenticationResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            null,
            -1);
        internal int DelayCallCount;
        internal int AuthenticationCallCount;
        internal string? DelayUsername;
        internal Pop3Credentials? Credentials;
        internal Pop3ServerConnection? Connection;
        internal List<string> Events = new List<string>();

        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DelayCallCount++;
            DelayUsername = username;
            Connection = connection;
            Events.Add("delay");
            return DelayResult;
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AuthenticationCallCount++;
            Credentials = credentials;
            Connection = connection;
            Events.Add("authenticate");
            return AuthenticationResult;
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

    sealed class CountingTlsProvider : IPop3ServerTlsProvider
    {
        internal int CallCount;

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            CallCount++;
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
