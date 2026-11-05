using System.Text;

namespace pop3.Tests;

public class Pop3ServerSaslTests
{
    [TestCase(null, false, "")]
    [TestCase("=", true, "")]
    [TestCase("aW5pdGlhbA==", true, "initial")]
    public void AuthSupportsAbsentEmptyAndPopulatedInitialResponses(
        string? initialToken,
        bool expectedHasInitialResponse,
        string expectedInitialResponse)
    {
        SaslSession session = SuccessfulSession();
        SaslFactory factory = new SaslFactory("tEsT", session);
        FakeProtocolLogger logger = new FakeProtocolLogger();
        Pop3ServerOptions options = CreateOptions(factory);
        options.Logger = logger;
        string input = "AuTh test";
        if (initialToken != null)
        {
            input += " " + initialToken;
        }
        DuplexStream stream = new DuplexStream(input + "\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("canonical-user"));
        Assert.That(result.MaildropKey, Is.EqualTo("maildrop"));
        Assert.That(factory.CreateCount, Is.EqualTo(1));
        Assert.That(factory.Connection!.LocalEndPoint, Is.EqualTo("local"));
        Assert.That(session.HasInitialResponse,
            Is.EqualTo(expectedHasInitialResponse));
        Assert.That(Encoding.ASCII.GetString(session.InitialResponse!),
            Is.EqualTo(expectedInitialResponse));
        Assert.That(session.GetResultCount, Is.EqualTo(1));
        Assert.That(session.CancelCount, Is.Zero);
        Assert.That(session.ApplySecurityLayerCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        string expectedLog = initialToken == null ?
            "AUTH test" : "AUTH test [redacted]";
        Assert.That(logger.Entries.Select(entry => entry.Text),
            Is.EqualTo(new[] { expectedLog }));
        Assert.That(logger.Entries[0].IsRedacted,
            Is.EqualTo(initialToken != null));
        if (initialToken != null)
        {
            Assert.That(logger.Entries[0].Text,
                Does.Not.Contain(initialToken));
        }
        Assert.Throws<Pop3StateException>(() =>
            engine.ReadNextCommand(CancellationToken.None));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void AuthRunsCanonicalBase64ChallengeResponseLoops()
    {
        SaslSession session = SuccessfulSession();
        session.BeginStep = Step(
            Pop3SaslStepStatus.Continue,
            Encoding.ASCII.GetBytes("first challenge"),
            true);
        session.ContinueSteps.Add(Step(
            Pop3SaslStepStatus.Continue,
            Array.Empty<byte>(),
            true));
        session.ContinueSteps.Add(Step(
            Pop3SaslStepStatus.Success,
            Array.Empty<byte>(),
            false));
        SaslFactory factory = new SaslFactory("TEST", session);
        FakeProtocolLogger logger = new FakeProtocolLogger();
        Pop3ServerOptions options = CreateOptions(factory);
        options.Logger = logger;
        DuplexStream stream = new DuplexStream(
            "AUTH TEST\r\n" +
            "Zmlyc3QgcmVzcG9uc2U=\r\n" +
            "\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(session.Responses.Count, Is.EqualTo(2));
        Assert.That(Encoding.ASCII.GetString(session.Responses[0]),
            Is.EqualTo("first response"));
        Assert.That(session.Responses[1], Is.Empty);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+ Zmlyc3QgY2hhbGxlbmdl\r\n" +
            "+ \r\n"));
        Assert.That(logger.Entries.Select(entry => entry.Text), Is.EqualTo(new[]
        {
            "AUTH TEST",
            "+ [redacted]",
            "SASL [redacted]",
            "+ [redacted]",
            "SASL [redacted]"
        }));
        Assert.That(logger.Entries.Select(entry => entry.IsRedacted),
            Is.EqualTo(new[] { false, true, true, true, true }));
        string logged = string.Join("|",
            logger.Entries.Select(entry => entry.Text));
        Assert.That(logged, Does.Not.Contain("Zmlyc3QgY2hhbGxlbmdl"));
        Assert.That(logged, Does.Not.Contain("Zmlyc3QgcmVzcG9uc2U"));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase("YQ=")]
    [TestCase("YR==")]
    [TestCase("YQ===")]
    [TestCase("!!!!")]
    [TestCase("*")]
    public void MalformedInitialResponsesAreRejectedBeforeSessionCreation(
        string initialToken)
    {
        SaslSession session = SuccessfulSession();
        SaslFactory factory = new SaslFactory("TEST", session);
        Pop3ServerOptions options = CreateOptions(factory);
        DuplexStream stream = new DuplexStream(
            "AUTH TEST " + initialToken + "\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(factory.CreateCount, Is.Zero);
        Assert.That(session.BeginCount, Is.Zero);
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR [AUTH] Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain(initialToken));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase("YR==")]
    [TestCase("*")]
    public void FailedActiveExchangeIsCancelledAndCanBeRetried(
        string clientToken)
    {
        SaslSession firstSession = SuccessfulSession();
        firstSession.BeginStep = Step(
            Pop3SaslStepStatus.Continue,
            Encoding.ASCII.GetBytes("challenge"),
            true);
        SaslSession secondSession = SuccessfulSession();
        SaslFactory factory = new SaslFactory("TEST", firstSession);
        factory.Sessions.Add(secondSession);
        Pop3ServerOptions options = CreateOptions(factory);
        DuplexStream stream = new DuplexStream(
            "AUTH TEST\r\n" + clientToken + "\r\n" +
            "AUTH test =\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult first = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult second = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(first.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(second.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(firstSession.CancelCount, Is.EqualTo(1));
        Assert.That(firstSession.ContinueCount, Is.Zero);
        Assert.That(secondSession.HasInitialResponse, Is.True);
        Assert.That(secondSession.InitialResponse, Is.Empty);
        Assert.That(factory.CreateCount, Is.EqualTo(2));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+ Y2hhbGxlbmdl\r\n" +
            "-ERR [AUTH] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void SaslLineLimitMustFitTheEmptyContinuationFraming()
    {
        SaslSession invalidSession = SuccessfulSession();
        SaslFactory invalidFactory = new SaslFactory("TEST", invalidSession);
        Pop3ServerOptions invalidOptions = CreateOptions(invalidFactory);
        invalidOptions.MaximumSaslLineOctets = 3;
        DuplexStream invalidStream = new DuplexStream("");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StartEngine(invalidStream, invalidOptions));

        SaslSession boundarySession = SuccessfulSession();
        boundarySession.BeginStep = Step(
            Pop3SaslStepStatus.Continue,
            Array.Empty<byte>(),
            true);
        SaslFactory boundaryFactory = new SaslFactory(
            "TEST",
            boundarySession);
        Pop3ServerOptions boundaryOptions = CreateOptions(boundaryFactory);
        boundaryOptions.MaximumSaslLineOctets = 4;
        DuplexStream boundaryStream = new DuplexStream(
            "AUTH TEST\r\n*\r\n");
        Pop3ServerEngine engine = StartEngine(boundaryStream, boundaryOptions);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(boundaryStream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+ \r\n-ERR [AUTH] Authentication failed.\r\n"));

        engine.CloseSession();
        invalidStream.Dispose();
        boundaryStream.Dispose();
    }

    [Test]
    public void UnknownMechanismIsRejectedWithoutCreatingAnySession()
    {
        SaslSession session = SuccessfulSession();
        SaslFactory factory = new SaslFactory("OTHER", session);
        Pop3ServerOptions options = CreateOptions(factory);
        DuplexStream stream = new DuplexStream("AUTH TEST\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(factory.CreateCount, Is.Zero);
        Assert.That(session.BeginCount, Is.Zero);
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR [AUTH] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(Pop3AuthenticationStatus.Rejected, "[AUTH]")]
    [TestCase(Pop3AuthenticationStatus.LoginDelay, "[LOGIN-DELAY]")]
    [TestCase(Pop3AuthenticationStatus.InUse, "[IN-USE]")]
    [TestCase(Pop3AuthenticationStatus.TemporaryFailure, "[SYS/TEMP]")]
    [TestCase(Pop3AuthenticationStatus.PermanentFailure, "[SYS/PERM]")]
    public void RejectedSessionsPreserveProviderClassification(
        Pop3AuthenticationStatus status,
        string expectedCode)
    {
        SaslSession session = SuccessfulSession();
        session.BeginStep = Step(
            Pop3SaslStepStatus.Rejected,
            Array.Empty<byte>(),
            false);
        session.AuthenticationResult = Result(
            status,
            null,
            null,
            "provider details must not be emitted",
            status == Pop3AuthenticationStatus.LoginDelay ? 60 : -1);
        SaslFactory factory = new SaslFactory("TEST", session);
        Pop3ServerOptions options = CreateOptions(factory);
        DuplexStream stream = new DuplexStream("AUTH TEST\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.FailureText,
            Is.EqualTo("provider details must not be emitted"));
        Assert.That(session.GetResultCount, Is.EqualTo(1));
        Assert.That(session.CancelCount, Is.EqualTo(1));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "-ERR " + expectedCode + " Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("provider details"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void AuthIsRejectedAfterSuccessfulExchangeCompletion()
    {
        SaslSession session = SuccessfulSession();
        SaslFactory factory = new SaslFactory("TEST", session);
        factory.Sessions.Add(SuccessfulSession());
        Pop3ServerOptions options = CreateOptions(factory);
        DuplexStream stream = new DuplexStream(
            "AUTH TEST\r\nAUTH TEST\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Pop3AuthenticationResult first = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Response completion = new Pop3Response();
        completion.Kind = Pop3ResponseKind.Positive;
        completion.Text = "Authentication complete.";
        engine.WriteNextResponse(completion, CancellationToken.None);
        Pop3AuthenticationResult second = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(first.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(second.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(factory.CreateCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "+OK Authentication complete.\r\n" +
            "-ERR [AUTH] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void InvalidServerStepsCancelTheExchange(int scenario)
    {
        SaslSession session = SuccessfulSession();
        if (scenario == 0)
        {
            session.ReturnNullBeginStep = true;
        }
        else if (scenario == 1)
        {
            session.BeginStep = Step(
                Pop3SaslStepStatus.Continue,
                Array.Empty<byte>(),
                false);
        }
        else if (scenario == 2)
        {
            session.BeginStep = Step(
                Pop3SaslStepStatus.Success,
                new byte[] { 1 },
                true);
        }
        else if (scenario == 3)
        {
            session.BeginStep = Step(
                (Pop3SaslStepStatus)99,
                Array.Empty<byte>(),
                false);
        }
        else
        {
            session.BeginStep = Step(
                Pop3SaslStepStatus.Continue,
                Array.Empty<byte>(),
                true);
            session.BeginStep.Data = null!;
        }
        SaslFactory factory = new SaslFactory("TEST", session);
        Pop3ServerOptions options = CreateOptions(factory);
        DuplexStream stream = new DuplexStream("AUTH TEST\r\n");
        Pop3ServerEngine engine = StartEngine(stream, options);

        Assert.Throws<InvalidDataException>(() =>
            engine.HandleAuthCommand(
                engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));
        Assert.That(session.CancelCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    static SaslSession SuccessfulSession()
    {
        SaslSession session = new SaslSession();
        session.BeginStep = Step(
            Pop3SaslStepStatus.Success,
            Array.Empty<byte>(),
            false);
        session.AuthenticationResult = Result(
            Pop3AuthenticationStatus.Success,
            "canonical-user",
            "maildrop",
            null,
            -1);
        return session;
    }

    static Pop3SaslStep Step(
        Pop3SaslStepStatus status,
        byte[] data,
        bool hasData)
    {
        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = status;
        step.Data = data;
        step.HasData = hasData;
        return step;
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

    static Pop3ServerOptions CreateOptions(IPop3SaslServerFactory factory)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.SaslFactories = new IPop3SaslServerFactory[] { factory };
        options.MaximumSaslLineOctets = 1024;
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

    sealed class SaslFactory : IPop3SaslServerFactory
    {
        internal string Mechanism;
        internal List<SaslSession> Sessions = new List<SaslSession>();
        internal int CreateCount;
        internal Pop3ServerConnection? Connection;

        internal SaslFactory(string mechanism, SaslSession firstSession)
        {
            Mechanism = mechanism;
            Sessions.Add(firstSession);
        }

        public string GetMechanism()
        {
            return Mechanism;
        }

        public IPop3SaslServerSession CreateSession(
            Pop3ServerConnection connection)
        {
            Connection = connection;
            SaslSession session = Sessions[CreateCount];
            CreateCount++;
            return session;
        }
    }

    sealed class SaslSession : IPop3SaslServerSession
    {
        internal Pop3SaslStep BeginStep = new Pop3SaslStep();
        internal List<Pop3SaslStep> ContinueSteps = new List<Pop3SaslStep>();
        internal Pop3AuthenticationResult AuthenticationResult =
            new Pop3AuthenticationResult();
        internal byte[]? InitialResponse;
        internal bool HasInitialResponse;
        internal List<byte[]> Responses = new List<byte[]>();
        internal bool ReturnNullBeginStep;
        internal int BeginCount;
        internal int ContinueCount;
        internal int CancelCount;
        internal int GetResultCount;
        internal int ApplySecurityLayerCount;

        public Pop3SaslStep BeginAuthentication(
            byte[] initialResponse,
            bool hasInitialResponse)
        {
            BeginCount++;
            InitialResponse = initialResponse;
            HasInitialResponse = hasInitialResponse;
            if (ReturnNullBeginStep)
            {
                return null!;
            }
            return BeginStep;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] response)
        {
            Responses.Add(response);
            Pop3SaslStep step = ContinueSteps[ContinueCount];
            ContinueCount++;
            return step;
        }

        public void CancelAuthentication()
        {
            CancelCount++;
        }

        public Pop3AuthenticationResult GetAuthenticationResult()
        {
            GetResultCount++;
            return AuthenticationResult;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            ApplySecurityLayerCount++;
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
