using System.Security.Cryptography;
using System.Text;

namespace pop3.Tests;

public class Pop3ClientApopTests
{
    [Test]
    public void ExplicitApopUsesTheGreetingChallengeAndDoesNotRequireAutomaticOptIn()
    {
        const string timestamp = "<1896.697170952@dbc.mtview.ca.us>";
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = SuccessfulStream(timestamp, "USER");
        Pop3ClientOptions options = Options(logger);
        Assert.That(options.Authentication.AllowApop, Is.False);
        Pop3ClientEngine engine = Connect(stream, options);

        Pop3AuthenticationResult result = engine.AuthenticateApop(
            Credentials("mrose", "tanstaaf"),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("mrose"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAPOP mrose c4c9334bac560ecc979e58001b3e22fb\r\nCAPA\r\n"));
        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "APOP [redacted]",
            "+OK maildrop ready"
        }));
        Assert.That(logger.Entries[0].IsRedacted, Is.True);
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("mrose"));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("tanstaaf"));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("c4c9334bac560ecc979e58001b3e22fb"));

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.AuthenticatedIdentity, Is.EqualTo("mrose"));
        Assert.That(snapshot.Greeting, Does.Contain(timestamp));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("transaction capabilities"));
        string snapshotText = snapshot.Greeting + "|" + snapshot.LastResponse.Text;
        Assert.That(snapshotText, Does.Not.Contain("tanstaaf"));
        Assert.That(snapshotText, Does.Not.Contain("c4c9334bac560ecc979e58001b3e22fb"));
    }

    [Test]
    public void RejectedApopCanBeRetriedWithoutLeavingAuthorization()
    {
        const string timestamp = "<retry@example.test>";
        string digest = Digest(timestamp, "secret");
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "-ERR invalid credentials\r\n" +
            "+OK maildrop ready\r\n" +
            "+OK transaction capabilities\r\n.\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(logger));
        Pop3Credentials credentials = Credentials("alice", "secret");

        Pop3AuthenticationException rejected = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateApop(credentials, CancellationToken.None))!;

        Assert.That(rejected.Method, Is.EqualTo(Pop3AuthenticationMethod.Apop));
        Assert.That(rejected.Result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(rejected.Result.FailureText, Is.EqualTo("invalid credentials"));
        Assert.That(rejected.Response.Text, Is.EqualTo("invalid credentials"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "APOP [redacted]",
            "-ERR invalid credentials"
        }));
        string failureText = rejected.Message + "|" + rejected.Result.FailureText + "|" +
            rejected.Response.Text + "|" + engine.GetSnapshot().Greeting + "|" +
            engine.GetSnapshot().LastResponse!.Text + "|" + string.Join("|", logger.Texts());
        Assert.That(failureText, Does.Not.Contain("secret"));
        Assert.That(failureText, Does.Not.Contain(digest));

        Pop3AuthenticationResult retried = engine.AuthenticateApop(
            credentials,
            CancellationToken.None);

        Assert.That(retried.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAPOP alice " + digest + "\r\nAPOP alice " + digest + "\r\nCAPA\r\n"));
        Assert.That(logger.Texts().Count(text => text == "APOP [redacted]"), Is.EqualTo(2));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("secret"));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain(digest));
    }

    [TestCase("AUTH/account", Pop3AuthenticationStatus.Rejected, -1)]
    [TestCase("LOGIN-DELAY/vendor", Pop3AuthenticationStatus.LoginDelay, 45)]
    [TestCase("IN-USE/maildrop", Pop3AuthenticationStatus.InUse, -1)]
    [TestCase("SYS/TEMP/backend", Pop3AuthenticationStatus.TemporaryFailure, -1)]
    [TestCase("SYS/PERM/storage", Pop3AuthenticationStatus.PermanentFailure, -1)]
    public void NegativeResponsesAreClassifiedAndRemainRetryable(
        string responseCode,
        Pop3AuthenticationStatus expectedStatus,
        int expectedRetryAfterSeconds)
    {
        const string timestamp = "<negative@example.test>";
        string responseText = "[" + responseCode + "] authentication detail";
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nLOGIN-DELAY 45\r\n.\r\n" +
            "-ERR " + responseText + "\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(null));

        Pop3AuthenticationException exception = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateApop(Credentials("alice", "secret"), CancellationToken.None))!;

        Assert.That(exception.Method, Is.EqualTo(Pop3AuthenticationMethod.Apop));
        Assert.That(exception.Result.Status, Is.EqualTo(expectedStatus));
        Assert.That(exception.Result.RetryAfterSeconds, Is.EqualTo(expectedRetryAfterSeconds));
        Assert.That(exception.Result.FailureText, Is.EqualTo(responseText));
        Assert.That(exception.Response.ResponseCodeLevels, Is.EqualTo(responseCode.Split('/')));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo(responseText));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAPOP alice " + Digest(timestamp, "secret") + "\r\n"));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void Utf8CredentialsRequireEnabledUtf8UserMode()
    {
        const string timestamp = "<utf8@example.test>";
        const string digest = "a6a0df8315ce4cf868ef9502380d8e0b";
        Pop3Credentials credentials = Credentials("álîçé😀", "päss🔑");
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream allowedStream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nUTF8 USER\r\n.\r\n" +
            "+OK UTF8 enabled\r\n" +
            "+OK maildrop ready\r\n" +
            "+OK transaction capabilities\r\nUIDL\r\n.\r\n");
        Pop3ClientEngine allowedEngine = Connect(allowedStream, Options(logger));

        Assert.Throws<InvalidOperationException>(() => allowedEngine.AuthenticateApop(
            credentials,
            CancellationToken.None));
        allowedEngine.EnableUtf8(CancellationToken.None);

        Pop3AuthenticationResult result = allowedEngine.AuthenticateApop(
            credentials,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(allowedStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUTF8\r\nAPOP álîçé😀 " + digest + "\r\nCAPA\r\n"));
        string loggedText = string.Join("|", logger.Texts());
        Assert.That(logger.Texts(), Does.Contain("APOP [redacted]"));
        Assert.That(loggedText, Does.Not.Contain(credentials.Username));
        Assert.That(loggedText, Does.Not.Contain(credentials.Password));
        Assert.That(loggedText, Does.Not.Contain(digest));
        string snapshotText = allowedEngine.GetSnapshot().Greeting + "|" +
            allowedEngine.GetSnapshot().LastResponse!.Text;
        Assert.That(snapshotText, Does.Not.Contain(credentials.Password));
        Assert.That(snapshotText, Does.Not.Contain(digest));

        AuthenticationSequenceStream deniedStream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nUTF8\r\n.\r\n" +
            "+OK UTF8 enabled\r\n");
        Pop3ClientEngine deniedEngine = Connect(deniedStream, Options(null));
        deniedEngine.EnableUtf8(CancellationToken.None);

        Assert.Throws<InvalidOperationException>(() => deniedEngine.AuthenticateApop(
            credentials,
            CancellationToken.None));
        Assert.That(deniedStream.WrittenText(), Is.EqualTo("CAPA\r\nUTF8\r\n"));
        Assert.That(deniedEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
    }

    [Test]
    public void MissingChallengeAndInvalidRequestsAreRejectedBeforeApopIsWritten()
    {
        AuthenticationSequenceStream noChallengeStream = SuccessfulStream(null, "USER");
        Pop3ClientEngine noChallengeEngine = Connect(noChallengeStream, Options(null));

        Assert.Throws<InvalidOperationException>(() => noChallengeEngine.AuthenticateApop(
            Credentials("alice", "secret"),
            CancellationToken.None));
        Assert.That(noChallengeStream.WrittenText(), Is.EqualTo("CAPA\r\n"));

        AuthenticationSequenceStream validationStream = SuccessfulStream(
            "<validation@example.test>",
            "UTF8 USER");
        Pop3ClientEngine validationEngine = Connect(validationStream, Options(null));

        Assert.Throws<ArgumentNullException>(() => validationEngine.AuthenticateApop(
            null!,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => validationEngine.AuthenticateApop(
            Credentials("alice smith", "secret"),
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => validationEngine.AuthenticateApop(
            Credentials(new string('x', 216), "secret"),
            CancellationToken.None));
        Assert.That(validationStream.WrittenText(), Is.EqualTo("CAPA\r\n"));
    }

    [TestCase("+OK ready <missing-at>\r\n")]
    [TestCase("+OK ready <@domain>\r\n")]
    [TestCase("+OK ready <local@>\r\n")]
    [TestCase("+OK ready <a b@domain>\r\n")]
    [TestCase("+OK ready <a@b> and <c@d>\r\n")]
    [TestCase("+OK ready <a@b\r\n")]
    public void MalformedGreetingChallengesCloseBeforeCapaOrApop(string greeting)
    {
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(greeting);
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.ConnectStream(stream, Options(null), CancellationToken.None))!;

        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(exception.Context, Is.EqualTo("greeting"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.Empty);
        stream.Dispose();
    }

    [Test]
    public void UnexpectedContinuationClosesTheDesynchronizedSession()
    {
        const string timestamp = "<continuation@example.test>";
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nUSER\r\n.\r\n" +
            "+ unexpected\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(null));

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.AuthenticateApop(Credentials("alice", "secret"), CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("APOP response"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Does.StartWith("CAPA\r\nAPOP alice "));
    }

    [Test]
    public void PreCancelledApopDoesNotWriteOrChangeState()
    {
        AuthenticationSequenceStream stream = SuccessfulStream("<cancel@example.test>", "USER");
        Pop3ClientEngine engine = Connect(stream, Options(null));
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => engine.AuthenticateApop(
                Credentials("alice", "secret"),
                cancellation.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
    }

    private static AuthenticationSequenceStream SuccessfulStream(string? timestamp, string capabilities)
    {
        string greeting = "+OK ready";
        if (timestamp != null)
        {
            greeting += " " + timestamp;
        }
        return new AuthenticationSequenceStream(
            greeting + "\r\n" +
            "+OK capabilities\r\n" + capabilities + "\r\n.\r\n" +
            "+OK maildrop ready\r\n" +
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

    private static Pop3ClientOptions Options(IPop3ProtocolLogger? logger)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 128;
        options.LeaveStreamOpen = true;
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

    private static string Digest(string timestamp, string password)
    {
        return Convert.ToHexString(
            MD5.HashData(Encoding.UTF8.GetBytes(timestamp + password))).ToLowerInvariant();
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
