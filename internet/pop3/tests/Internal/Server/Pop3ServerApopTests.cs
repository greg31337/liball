using System.Text;
using System.Text.RegularExpressions;

namespace pop3.Tests;

public class Pop3ServerApopTests
{
    [TestCase("0123456789abcdef0123456789abcdef")]
    [TestCase("0123456789ABCDEF0123456789ABCDEF")]
    [TestCase("0123456789aBcDeF0123456789AbCdEf")]
    public void ValidHexDigestsReachProviderWithTheConnectionChallenge(
        string digest)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.ApopResult = Result(
            Pop3AuthenticationStatus.Success,
            "canonical-alice",
            "maildrop-7",
            null,
            -1);
        FakeProtocolLogger logger = new FakeProtocolLogger();
        Pop3ServerOptions options = CreateOptions(provider);
        options.Logger = logger;
        DuplexStream stream = new DuplexStream(
            "aPoP alice " + digest + "\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);
        string? challenge = engine.GetStateMachine()
            .GetGreeting().GreetingTimestamp;

        Pop3AuthenticationResult result = engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("canonical-alice"));
        Assert.That(result.MaildropKey, Is.EqualTo("maildrop-7"));
        Assert.That(challenge, Does.Match(
            new Regex("^<[0-9]+\\.[0-9a-f]{32}@localhost>$")));
        Assert.That(provider.Events, Is.EqualTo(new[] { "delay", "apop" }));
        Assert.That(provider.DelayUsername, Is.EqualTo("alice"));
        Assert.That(provider.Request!.Username, Is.EqualTo("alice"));
        Assert.That(provider.Request.Timestamp, Is.EqualTo(challenge));
        Assert.That(provider.Request.Digest, Is.EqualTo(digest));
        Assert.That(provider.Connection!.IsSecure, Is.False);
        Assert.That(provider.Connection.LocalEndPoint, Is.EqualTo("local"));
        Assert.That(provider.Connection.RemoteEndPoint, Is.EqualTo("remote"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready " + challenge + "\r\n"));
        Assert.That(logger.Entries.Select(entry => entry.Text),
            Is.EqualTo(new[] { "APOP [redacted]" }));
        Assert.That(logger.Entries[0].IsRedacted, Is.True);
        Assert.That(logger.Entries[0].Text, Does.Not.Contain("alice"));
        Assert.That(logger.Entries[0].Text, Does.Not.Contain(digest));
        Assert.Throws<Pop3StateException>(() =>
            engine.ReadNextCommand(CancellationToken.None));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void IndependentSessionsSupplyDistinctChallengesToTheProvider()
    {
        const string digest = "0123456789abcdef0123456789abcdef";
        AuthenticationProvider firstProvider = SuccessfulProvider();
        AuthenticationProvider secondProvider = SuccessfulProvider();
        Pop3ServerOptions firstOptions = CreateOptions(firstProvider);
        Pop3ServerOptions secondOptions = CreateOptions(secondProvider);
        DuplexStream firstStream = new DuplexStream(
            "APOP alice " + digest + "\r\n");
        DuplexStream secondStream = new DuplexStream(
            "APOP alice " + digest + "\r\n");
        Pop3ServerEngine first = StartEngine(firstStream, firstOptions);
        Pop3ServerEngine second = StartEngine(secondStream, secondOptions);

        first.HandleApopCommand(
            first.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        second.HandleApopCommand(
            second.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(firstProvider.Request!.Timestamp,
            Is.Not.EqualTo(secondProvider.Request!.Timestamp));
        Assert.That(firstProvider.Request.Timestamp,
            Is.EqualTo(first.GetStateMachine().GetGreeting().GreetingTimestamp));
        Assert.That(secondProvider.Request.Timestamp,
            Is.EqualTo(second.GetStateMachine().GetGreeting().GreetingTimestamp));

        first.CloseSession();
        second.CloseSession();
        firstStream.Dispose();
        secondStream.Dispose();
    }

    [TestCase("0123456789abcdef0123456789abcde")]
    [TestCase("0123456789abcdef0123456789abcdef0")]
    [TestCase("0123456789abcdef0123456789abcdeg")]
    [TestCase("0123456789abcdef0123456789abcde-")]
    [TestCase("0x0123456789abcdef0123456789abcdef")]
    public void MalformedDigestsAreUniformlyRejectedBeforeProviderWork(
        string digest)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream(
            "APOP alice " + digest + "\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.ApopCallCount, Is.Zero);
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR [AUTH] Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain(digest));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void UnavailableApopIsRejectedBeforeProviderWork(int scenario)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        Pop3ServerOptions options = CreateOptions(provider);
        if (scenario == 0)
        {
            options.EnableApop = false;
        }
        else if (scenario == 1)
        {
            options.AuthenticationProvider = null;
        }
        else
        {
            options.EnableApop = false;
        }
        DuplexStream stream = new DuplexStream(
            "APOP alice 0123456789abcdef0123456789abcdef\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);
        if (scenario == 2)
        {
            options.EnableApop = true;
        }

        Pop3AuthenticationResult result = engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.Zero);
        Assert.That(provider.ApopCallCount, Is.Zero);
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR [AUTH] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(Pop3AuthenticationStatus.Rejected, false, "[AUTH]", 1)]
    [TestCase(Pop3AuthenticationStatus.LoginDelay, true, "[LOGIN-DELAY]", 0)]
    [TestCase(Pop3AuthenticationStatus.InUse, true, "[IN-USE]", 0)]
    [TestCase(Pop3AuthenticationStatus.TemporaryFailure, true, "[SYS/TEMP]", 0)]
    [TestCase(Pop3AuthenticationStatus.PermanentFailure, true, "[SYS/PERM]", 0)]
    public void ProviderFailuresUseUniformTextAndClassification(
        Pop3AuthenticationStatus status,
        bool failDelayCheck,
        string expectedCode,
        int expectedApopCalls)
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        Pop3AuthenticationResult failure = Result(
            status,
            null,
            null,
            "provider detail for alice and digest",
            status == Pop3AuthenticationStatus.LoginDelay ? 75 : -1);
        if (failDelayCheck)
        {
            provider.DelayResult = failure;
        }
        else
        {
            provider.ApopResult = failure;
        }
        Pop3ServerOptions options = CreateOptions(provider);
        DuplexStream stream = new DuplexStream(
            "APOP alice 0123456789abcdef0123456789abcdef\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.FailureText,
            Is.EqualTo("provider detail for alice and digest"));
        Assert.That(result.RetryAfterSeconds,
            Is.EqualTo(status == Pop3AuthenticationStatus.LoginDelay ? 75 : -1));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.ApopCallCount, Is.EqualTo(expectedApopCalls));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR " + expectedCode + " Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("alice"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("provider detail"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void DisabledResponseCodesUseTheSameFailureText()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.ApopResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            "unsafe provider detail",
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        options.EnableResponseCodes = false;
        DuplexStream stream = new DuplexStream(
            "APOP alice 0123456789abcdef0123456789abcdef\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("unsafe"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void ApopClearsAStagedUserBeforeAFollowingPass()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.ApopResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        options.AllowUnprotectedCredentials = true;
        DuplexStream stream = new DuplexStream(
            "USER alice\r\n" +
            "APOP bob 0123456789abcdef0123456789abcdef\r\n" +
            "PASS secret\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult pass = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(pass.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(provider.DelayCallCount, Is.EqualTo(1));
        Assert.That(provider.ApopCallCount, Is.EqualTo(1));
        Assert.That(provider.PasswordCallCount, Is.Zero);
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR [AUTH] Authentication failed.\r\n" +
            "-ERR Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void RejectedApopCanBeRetriedAgainstTheSameSessionChallenge()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.ApopResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            null,
            -1);
        Pop3ServerOptions options = CreateOptions(provider);
        const string command =
            "APOP alice 0123456789abcdef0123456789abcdef\r\n";
        DuplexStream stream = new DuplexStream(command + command);
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult first = engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        string? firstTimestamp = provider.Request!.Timestamp;
        provider.ApopResult = Result(
            Pop3AuthenticationStatus.Success,
            "alice",
            "mail",
            null,
            -1);
        Pop3AuthenticationResult second = engine.HandleApopCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(first.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(second.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(provider.DelayCallCount, Is.EqualTo(2));
        Assert.That(provider.ApopCallCount, Is.EqualTo(2));
        Assert.That(provider.Request.Timestamp, Is.EqualTo(firstTimestamp));

        engine.CloseSession();
        stream.Dispose();
    }

    static AuthenticationProvider SuccessfulProvider()
    {
        AuthenticationProvider provider = new AuthenticationProvider();
        provider.ApopResult = Result(
            Pop3AuthenticationStatus.Success,
            "alice",
            "mail",
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
        IPop3AuthenticationProvider? provider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.EnableApop = true;
        options.AuthenticationProvider = provider;
        return options;
    }

    static Pop3ServerEngine StartEngine(
        DuplexStream stream,
        Pop3ServerOptions options)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        connection.LocalEndPoint = "local";
        connection.RemoteEndPoint = "remote";
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
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
        internal Pop3AuthenticationResult ApopResult = Result(
            Pop3AuthenticationStatus.Rejected,
            null,
            null,
            null,
            -1);
        internal int DelayCallCount;
        internal int ApopCallCount;
        internal int PasswordCallCount;
        internal string? DelayUsername;
        internal Pop3ApopRequest? Request;
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
            PasswordCallCount++;
            return new Pop3AuthenticationResult();
        }

        public Pop3AuthenticationResult AuthenticateApop(
            Pop3ApopRequest request,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApopCallCount++;
            Request = request;
            Connection = connection;
            Events.Add("apop");
            return ApopResult;
        }

        public Pop3AuthenticationResult AuthenticateSaslPlain(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
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
