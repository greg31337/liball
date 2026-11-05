using System.Text;

namespace pop3.Tests;

public class Pop3ClientUserPasswordTests
{
    [Test]
    public void SuccessfulAuthenticationReturnsAndSnapshotsTheAuthenticatedIdentity()
    {
        AuthenticationSequenceStream stream = SuccessfulStream("USER");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));

        Pop3AuthenticationResult result = engine.AuthenticateUserPassword(
            Credentials("alice", "secret"),
            CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(result.FailureText, Is.Null);
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(-1));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsAuthenticated, Is.True);
        Assert.That(snapshot.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(snapshot.Capabilities.SupportsUidl, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.False);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("transaction capabilities"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void UserRejectionDoesNotSendThePasswordAndCanBeRetried()
    {
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "-ERR unknown user\r\n" +
            "+OK user accepted\r\n" +
            "+OK password accepted\r\n" +
            "+OK transaction capabilities\r\nUIDL\r\n.\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));
        Pop3Credentials credentials = Credentials("alice", "secret");

        Pop3AuthenticationException rejected = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateUserPassword(credentials, CancellationToken.None))!;

        Assert.That(rejected.Method, Is.EqualTo(Pop3AuthenticationMethod.UserPassword));
        Assert.That(rejected.Result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(rejected.Result.FailureText, Is.EqualTo("unknown user"));
        Assert.That(rejected.Response.Text, Is.EqualTo("unknown user"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("unknown user"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nUSER alice\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("PASS"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("secret"));

        Pop3AuthenticationResult retried = engine.AuthenticateUserPassword(
            credentials,
            CancellationToken.None);

        Assert.That(retried.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(retried.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
    }

    [Test]
    public void PasswordRejectionLeavesAuthorizationStateForRetry()
    {
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "+OK user accepted\r\n" +
            "-ERR invalid credentials\r\n" +
            "+OK user accepted again\r\n" +
            "+OK password accepted\r\n" +
            "+OK transaction capabilities\r\n.\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));
        Pop3Credentials credentials = Credentials("alice", "secret");

        Pop3AuthenticationException rejected = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateUserPassword(credentials, CancellationToken.None))!;

        Assert.That(rejected.Result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(rejected.Result.FailureText, Is.EqualTo("invalid credentials"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        Pop3AuthenticationResult retried = engine.AuthenticateUserPassword(
            credentials,
            CancellationToken.None);

        Assert.That(retried.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\n" +
            "USER alice\r\nPASS secret\r\nCAPA\r\n"));
    }

    [Test]
    public void PasswordIsRedactedFromProtocolLogs()
    {
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = SuccessfulStream("USER");
        Pop3ClientEngine engine = Connect(stream, Options(true, logger));

        Pop3AuthenticationResult result = engine.AuthenticateUserPassword(
            Credentials("alice", "swordfish"),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "USER alice",
            "+OK user accepted",
            "PASS [redacted]",
            "+OK password accepted"
        }));
        Assert.That(logger.Entries[0].IsRedacted, Is.False);
        Assert.That(logger.Entries[2].IsRedacted, Is.True);
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("swordfish"));
        Assert.That(logger.Entries.Select(entry => entry.SessionState), Is.All.EqualTo(Pop3SessionState.Authorization));
    }

    [Test]
    public void RejectedPasswordRemainsRedactedFromProtocolLogsAndFailureObjects()
    {
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "+OK user accepted\r\n" +
            "-ERR [AUTH] invalid credentials\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(true, logger));

        Pop3AuthenticationException exception = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateUserPassword(
                Credentials("alice", "swordfish"),
                CancellationToken.None))!;

        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "USER alice",
            "+OK user accepted",
            "PASS [redacted]",
            "-ERR [AUTH] invalid credentials"
        }));
        Assert.That(logger.Entries[2].IsRedacted, Is.True);
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("swordfish"));
        Assert.That(exception.Message, Does.Not.Contain("swordfish"));
        Assert.That(exception.Result.FailureText, Does.Not.Contain("swordfish"));
        Assert.That(exception.Response.Text, Does.Not.Contain("swordfish"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase("USER", "AUTH/account", Pop3AuthenticationStatus.Rejected, -1)]
    [TestCase("USER", "LOGIN-DELAY/vendor", Pop3AuthenticationStatus.LoginDelay, 75)]
    [TestCase("USER", "IN-USE/maildrop", Pop3AuthenticationStatus.InUse, -1)]
    [TestCase("USER", "SYS/TEMP/backend", Pop3AuthenticationStatus.TemporaryFailure, -1)]
    [TestCase("USER", "SYS/PERM/storage", Pop3AuthenticationStatus.PermanentFailure, -1)]
    [TestCase("PASS", "AUTH/account", Pop3AuthenticationStatus.Rejected, -1)]
    [TestCase("PASS", "LOGIN-DELAY/vendor", Pop3AuthenticationStatus.LoginDelay, 75)]
    [TestCase("PASS", "IN-USE/maildrop", Pop3AuthenticationStatus.InUse, -1)]
    [TestCase("PASS", "SYS/TEMP/backend", Pop3AuthenticationStatus.TemporaryFailure, -1)]
    [TestCase("PASS", "SYS/PERM/storage", Pop3AuthenticationStatus.PermanentFailure, -1)]
    public void UserAndPasswordResponseCodesAreClassifiedAndRemainRetryable(
        string rejectingCommand,
        string responseCode,
        Pop3AuthenticationStatus expectedStatus,
        int expectedRetryAfterSeconds)
    {
        string rejection = "-ERR [" + responseCode + "] authentication detail\r\n";
        string authenticationResponses = rejectingCommand == Pop3Commands.Pass ?
            "+OK user accepted\r\n" + rejection :
            rejection;
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\nLOGIN-DELAY 75\r\n.\r\n" +
            authenticationResponses);
        Pop3ClientEngine engine = Connect(stream, Options(true, null));

        Pop3AuthenticationException exception = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateUserPassword(
                Credentials("alice", "secret"),
                CancellationToken.None))!;

        string expectedCommands = rejectingCommand == Pop3Commands.Pass ?
            "CAPA\r\nUSER alice\r\nPASS secret\r\n" :
            "CAPA\r\nUSER alice\r\n";
        Assert.That(exception.Method, Is.EqualTo(Pop3AuthenticationMethod.UserPassword));
        Assert.That(exception.Result.Status, Is.EqualTo(expectedStatus));
        Assert.That(exception.Result.RetryAfterSeconds, Is.EqualTo(expectedRetryAfterSeconds));
        Assert.That(exception.Result.FailureText, Is.EqualTo(
            "[" + responseCode + "] authentication detail"));
        Assert.That(exception.Response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(exception.Response.ResponseCodeLevels, Is.EqualTo(responseCode.Split('/')));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo(exception.Response.Text));
        Assert.That(stream.WrittenText(), Is.EqualTo(expectedCommands));
        if (rejectingCommand == Pop3Commands.User)
        {
            Assert.That(stream.WrittenText(), Does.Not.Contain(Pop3Commands.Pass));
            Assert.That(stream.WrittenText(), Does.Not.Contain("secret"));
        }
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void CleartextCredentialsAreDeniedByDefaultBeforeUserIsWritten()
    {
        AuthenticationSequenceStream stream = SuccessfulStream("USER");
        Pop3ClientEngine engine = Connect(stream, Options(false, null));

        Assert.Throws<InvalidOperationException>(() => engine.AuthenticateUserPassword(
            Credentials("alice", "secret"),
            CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
    }

    [Test]
    public void ProtectedTransportDoesNotRequireTheCleartextOverride()
    {
        AuthenticationSequenceStream stream = SuccessfulStream("USER");
        Pop3ClientOptions options = Options(false, null);
        options.SecurityMode = Pop3SecurityMode.ImplicitTls;
        options.TlsProvider = new IdentityTlsProvider();
        Pop3ClientEngine engine = Connect(stream, options);

        Pop3AuthenticationResult result = engine.AuthenticateUserPassword(
            Credentials("alice", "secret"),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(engine.GetSnapshot().IsSecure, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
    }

    [Test]
    public void CapaProbingPolicyControlsUserWhenCapaIsUnavailable()
    {
        AuthenticationSequenceStream deniedStream = new AuthenticationSequenceStream(
            "+OK ready\r\n-ERR CAPA unavailable\r\n");
        Pop3ClientOptions deniedOptions = Options(true, null);
        Pop3ClientEngine deniedEngine = Connect(deniedStream, deniedOptions);

        Assert.Throws<InvalidOperationException>(() => deniedEngine.AuthenticateUserPassword(
            Credentials("alice", "secret"),
            CancellationToken.None));
        Assert.That(deniedStream.WrittenText(), Is.EqualTo("CAPA\r\n"));

        AuthenticationSequenceStream allowedStream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "+OK user accepted\r\n" +
            "+OK password accepted\r\n" +
            "-ERR CAPA unavailable\r\n");
        Pop3ClientOptions allowedOptions = Options(true, null);
        allowedOptions.ProbeUserWhenCapaUnavailable = true;
        Pop3ClientEngine allowedEngine = Connect(allowedStream, allowedOptions);

        Pop3AuthenticationResult result = allowedEngine.AuthenticateUserPassword(
            Credentials("alice", "secret"),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(allowedStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
    }

    [Test]
    public void Utf8CredentialsRequireEnabledUtf8UserMode()
    {
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream allowedStream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\nUTF8 USER\r\n.\r\n" +
            "+OK UTF8 enabled\r\n" +
            "+OK user accepted\r\n" +
            "+OK password accepted\r\n" +
            "+OK transaction capabilities\r\nUIDL\r\n.\r\n");
        Pop3ClientEngine allowedEngine = Connect(allowedStream, Options(true, logger));

        Assert.Throws<InvalidOperationException>(() => allowedEngine.AuthenticateUserPassword(
            Credentials("álîçé😀", "päss🔑"),
            CancellationToken.None));
        allowedEngine.EnableUtf8(CancellationToken.None);

        Pop3AuthenticationResult result = allowedEngine.AuthenticateUserPassword(
            Credentials("álîçé😀", "päss🔑"),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(allowedStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUTF8\r\nUSER álîçé😀\r\nPASS päss🔑\r\nCAPA\r\n"));
        Assert.That(logger.Texts(), Does.Contain("PASS [redacted]"));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("päss🔑"));

        AuthenticationSequenceStream deniedStream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\nUTF8\r\n.\r\n" +
            "+OK UTF8 enabled\r\n");
        Pop3ClientEngine deniedEngine = Connect(deniedStream, Options(true, null));
        deniedEngine.EnableUtf8(CancellationToken.None);

        Assert.Throws<InvalidOperationException>(() => deniedEngine.AuthenticateUserPassword(
            Credentials("álîçé", "secret"),
            CancellationToken.None));
        Assert.That(deniedStream.WrittenText(), Is.EqualTo("CAPA\r\nUTF8\r\n"));
    }

    [Test]
    public void InvalidCredentialsAndOversizedCommandsAreRejectedBeforeWriting()
    {
        AuthenticationSequenceStream stream = SuccessfulStream("USER\r\nUTF8 USER");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));

        Assert.Throws<ArgumentNullException>(() => engine.AuthenticateUserPassword(
            null!,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateUserPassword(
            Credentials("alice smith", "secret"),
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateUserPassword(
            Credentials("alice", "sec\rret"),
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateUserPassword(
            Credentials("alice", new string('x', 249)),
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateUserPassword(
            Credentials("ali\uD800ce", "secret"),
            CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
    }

    [Test]
    public void UnexpectedUserContinuationClosesTheDesynchronizedSession()
    {
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "+ unexpected\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.AuthenticateUserPassword(Credentials("alice", "secret"), CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("USER response"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nUSER alice\r\n"));
    }

    [Test]
    public void UnexpectedPasswordContinuationClosesTheDesynchronizedSession()
    {
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "+OK user accepted\r\n" +
            "+ unexpected\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.AuthenticateUserPassword(Credentials("alice", "secret"), CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("PASS response"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\n"));
        stream.Dispose();
    }

    [Test]
    public void PreCancelledAuthenticationDoesNotWriteUserOrChangeState()
    {
        AuthenticationSequenceStream stream = SuccessfulStream("USER");
        Pop3ClientEngine engine = Connect(stream, Options(true, null));
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => engine.AuthenticateUserPassword(
                Credentials("alice", "secret"),
                cancellation.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
    }

    private static AuthenticationSequenceStream SuccessfulStream(string capabilities)
    {
        return new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\n" + capabilities + "\r\n.\r\n" +
            "+OK user accepted\r\n" +
            "+OK password accepted\r\n" +
            "+OK transaction capabilities\r\nUIDL\r\n.\r\n");
    }

    private static Pop3ClientEngine Connect(
        AuthenticationSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        return engine;
    }

    private static Pop3ClientOptions Options(bool allowUnprotectedCredentials, IPop3ProtocolLogger? logger)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 128;
        options.LeaveStreamOpen = true;
        options.Authentication.AllowUnprotectedCredentials = allowUnprotectedCredentials;
        options.Logger = logger;
        return options;
    }

    private static Pop3Credentials Credentials(string username, string password)
    {
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = username;
        credentials.Password = password;
        return credentials;
    }

    private class RecordingLogger : IPop3ProtocolLogger
    {
        public List<Pop3LogEntry> Entries = new List<Pop3LogEntry>();

        public void Log(Pop3LogEntry entry)
        {
            Entries.Add(entry);
        }

        public string[] Texts()
        {
            return Entries.Select(entry => entry.Text).ToArray();
        }
    }

    private class IdentityTlsProvider : IPop3ClientTlsProvider
    {
        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return stream;
        }
    }

    private class AuthenticationSequenceStream : Stream
    {
        byte[] ReadData;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public AuthenticationSequenceStream(string responses)
        {
            ReadData = Encoding.UTF8.GetBytes(responses);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
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

        public string WrittenText()
        {
            return Encoding.UTF8.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = ReadData.Length - ReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(ReadData, ReadPosition, buffer, offset, readCount);
            ReadPosition += readCount;
            return readCount;
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
    }
}
