using System.Text;

namespace pop3.Tests;

public class Pop3ServerMailboxAcquisitionTests
{
    [Test]
    public void SuccessfulAcquisitionEntersTransactionAfterOpeningTheMaildrop()
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\nSTAT\r\n");
        MailboxSession session = new MailboxSession(stream);
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        mailboxProvider.Result = MailboxResult(
            Pop3AuthenticationStatus.Success,
            session,
            -1,
            null);
        Pop3ServerOptions options = Options(mailboxProvider);
        Pop3ServerEngine engine = StartEngine(stream, options);
        Pop3AuthenticationResult authentication = Authenticate(engine);

        Pop3MailboxOpenResult result = engine.CompleteAuthentication(
            authentication,
            CancellationToken.None);
        Pop3Command transactionCommand = engine.ReadNextCommand(
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.Session, Is.SameAs(session));
        Assert.That(mailboxProvider.CallCount, Is.EqualTo(1));
        Assert.That(mailboxProvider.OutputAtOpen, Is.EqualTo(
            "+OK ready\r\n+OK Send password.\r\n"));
        Assert.That(mailboxProvider.Request!.AuthenticatedIdentity,
            Is.EqualTo("canonical-alice"));
        Assert.That(mailboxProvider.Request.MaildropKey,
            Is.EqualTo("maildrop-42"));
        Assert.That(mailboxProvider.Request.Connection, Is.Not.Null);
        Assert.That(mailboxProvider.Request.Connection!.Stream,
            Is.SameAs(stream));
        Assert.That(mailboxProvider.Request.Connection.LocalEndPoint,
            Is.EqualTo("local"));
        Assert.That(mailboxProvider.Request.Connection.RemoteEndPoint,
            Is.EqualTo("remote"));
        Assert.That(mailboxProvider.CancellationToken,
            Is.EqualTo(CancellationToken.None));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetStateMachine().GetIsAuthenticated(), Is.True);
        Assert.That(engine.HasOpenMailbox(), Is.True);
        Assert.That(session.ReleaseCount, Is.Zero);
        Assert.That(transactionCommand.Keyword, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "+OK Authentication successful.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(Pop3AuthenticationStatus.InUse, "[IN-USE]", -1)]
    [TestCase(Pop3AuthenticationStatus.LoginDelay, "[LOGIN-DELAY]", 75)]
    [TestCase(Pop3AuthenticationStatus.TemporaryFailure, "[SYS/TEMP]", -1)]
    [TestCase(Pop3AuthenticationStatus.PermanentFailure, "[SYS/PERM]", -1)]
    public void FailedAcquisitionReleasesAProviderSessionBeforeItsNegativeResponse(
        Pop3AuthenticationStatus status,
        string responseCode,
        int retryAfterSeconds)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\nCAPA\r\n");
        MailboxSession session = new MailboxSession(stream);
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        Pop3MailboxOpenResult providerResult = MailboxResult(
            status,
            session,
            retryAfterSeconds,
            "private provider detail");
        mailboxProvider.Result = providerResult;
        Pop3ServerEngine engine = StartEngine(stream, Options(mailboxProvider));
        Pop3AuthenticationResult authentication = Authenticate(engine);

        Pop3MailboxOpenResult result = engine.CompleteAuthentication(
            authentication,
            CancellationToken.None);
        Pop3Command retryCommand = engine.ReadNextCommand(
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(retryAfterSeconds));
        Assert.That(result.FailureText, Is.EqualTo("private provider detail"));
        Assert.That(result.Session, Is.Null);
        Assert.That(providerResult.Session, Is.SameAs(session));
        Assert.That(session.ReleaseCount, Is.EqualTo(1));
        Assert.That(session.OutputAtRelease, Is.EqualTo(
            "+OK ready\r\n+OK Send password.\r\n"));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetStateMachine().GetIsAuthenticated(), Is.False);
        Assert.That(engine.HasOpenMailbox(), Is.False);
        Assert.That(retryCommand.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "-ERR " + responseCode + " Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("private provider"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void ConcurrentMaildropAttemptReturnsInUseUntilHolderReleases()
    {
        ContendedMailboxProvider mailboxProvider =
            new ContendedMailboxProvider();
        DuplexStream firstStream = new DuplexStream(
            "USER first\r\nPASS secret\r\n");
        DuplexStream secondStream = new DuplexStream(
            "USER second\r\nPASS secret\r\n");
        Pop3ServerEngine first = StartEngine(
            firstStream,
            Options(mailboxProvider));
        Pop3ServerEngine second = StartEngine(
            secondStream,
            Options(mailboxProvider));

        Pop3MailboxOpenResult firstResult = first.CompleteAuthentication(
            Authenticate(first),
            CancellationToken.None);
        Pop3MailboxOpenResult secondResult = second.CompleteAuthentication(
            Authenticate(second),
            CancellationToken.None);

        Assert.That(firstResult.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(secondResult.Status,
            Is.EqualTo(Pop3AuthenticationStatus.InUse));
        Assert.That(first.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(second.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(mailboxProvider.Sessions, Has.Count.EqualTo(2));
        Assert.That(mailboxProvider.Sessions[0].ReleaseCount, Is.Zero);
        Assert.That(mailboxProvider.Sessions[1].ReleaseCount, Is.EqualTo(1));
        Assert.That(secondStream.WrittenText(), Does.EndWith(
            "-ERR [IN-USE] Authentication failed.\r\n"));

        first.CloseSession();

        DuplexStream thirdStream = new DuplexStream(
            "USER third\r\nPASS secret\r\n");
        Pop3ServerEngine third = StartEngine(
            thirdStream,
            Options(mailboxProvider));
        Pop3MailboxOpenResult thirdResult = third.CompleteAuthentication(
            Authenticate(third),
            CancellationToken.None);

        Assert.That(mailboxProvider.Sessions[0].ReleaseCount, Is.EqualTo(1));
        Assert.That(thirdResult.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(third.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(mailboxProvider.CallCount, Is.EqualTo(3));

        second.CloseSession();
        third.CloseSession();
        firstStream.Dispose();
        secondStream.Dispose();
        thirdStream.Dispose();
    }

    [Test]
    public void MissingMailboxProviderReturnsPermanentFailure()
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n");
        Pop3ServerEngine engine = StartEngine(stream, Options(null));
        Pop3AuthenticationResult authentication = Authenticate(engine);

        Pop3MailboxOpenResult result = engine.CompleteAuthentication(
            authentication,
            CancellationToken.None);

        Assert.That(result.Status,
            Is.EqualTo(Pop3AuthenticationStatus.PermanentFailure));
        Assert.That(result.Session, Is.Null);
        Assert.That(engine.HasOpenMailbox(), Is.False);
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "-ERR [SYS/PERM] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void DisabledResponseCodesKeepMailboxFailureDetailsPrivate()
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n");
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        mailboxProvider.Result = MailboxResult(
            Pop3AuthenticationStatus.InUse,
            null,
            -1,
            "alice is already logged in");
        Pop3ServerOptions options = Options(mailboxProvider);
        options.EnableResponseCodes = false;
        Pop3ServerEngine engine = StartEngine(stream, options);

        engine.CompleteAuthentication(
            Authenticate(engine),
            CancellationToken.None);

        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "-ERR Authentication failed.\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("alice is"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void ReleaseFailurePreventsTheNegativeResponse()
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n");
        MailboxSession session = new MailboxSession(stream);
        session.ThrowOnRelease = true;
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        mailboxProvider.Result = MailboxResult(
            Pop3AuthenticationStatus.InUse,
            session,
            -1,
            null);
        Pop3ServerEngine engine = StartEngine(stream, Options(mailboxProvider));
        Pop3AuthenticationResult authentication = Authenticate(engine);

        Assert.Throws<InvalidOperationException>(() =>
            engine.CompleteAuthentication(
                authentication,
                CancellationToken.None));

        Assert.That(session.ReleaseCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Send password.\r\n"));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.HasOpenMailbox(), Is.False);

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void CancellationBeforeAcquisitionDoesNotCallTheProvider()
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n");
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        Pop3ServerEngine engine = StartEngine(stream, Options(mailboxProvider));
        Pop3AuthenticationResult authentication = Authenticate(engine);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            engine.CompleteAuthentication(authentication, source.Token));

        Assert.That(mailboxProvider.CallCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.HasOpenMailbox(), Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Send password.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void InvalidProviderResultsAreRejectedWithoutAProtocolResponse(
        int scenario)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n");
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        if (scenario == 0)
        {
            mailboxProvider.ReturnNull = true;
        }
        else if (scenario == 1)
        {
            mailboxProvider.Result = MailboxResult(
                Pop3AuthenticationStatus.Success,
                null,
                -1,
                null);
        }
        else if (scenario == 2)
        {
            mailboxProvider.Result = MailboxResult(
                Pop3AuthenticationStatus.Rejected,
                null,
                -1,
                null);
        }
        else
        {
            mailboxProvider.Result = MailboxResult(
                Pop3AuthenticationStatus.TemporaryFailure,
                null,
                -2,
                null);
        }
        Pop3ServerEngine engine = StartEngine(stream, Options(mailboxProvider));
        Pop3AuthenticationResult authentication = Authenticate(engine);

        Assert.Throws<InvalidDataException>(() =>
            engine.CompleteAuthentication(
                authentication,
                CancellationToken.None));

        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.HasOpenMailbox(), Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Send password.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void SaslMailboxFailureCancelsTheSessionWithoutApplyingItsLayer()
    {
        DuplexStream stream = new DuplexStream("AUTH TEST cHJvb2Y=\r\n");
        SaslSession saslSession = new SaslSession(stream);
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        mailboxProvider.Result = MailboxResult(
            Pop3AuthenticationStatus.InUse,
            null,
            -1,
            null);
        Pop3ServerOptions options = Options(mailboxProvider);
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory(saslSession)
        };
        Pop3ServerEngine engine = StartEngine(stream, options);
        Pop3AuthenticationResult authentication = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        engine.CompleteAuthentication(authentication, CancellationToken.None);

        Assert.That(saslSession.CancelCount, Is.EqualTo(1));
        Assert.That(saslSession.ApplyCount, Is.Zero);
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR [IN-USE] Authentication failed.\r\n"));

        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void SaslLayerActivatesOnlyAfterMailboxAcquisitionAndSuccessResponse()
    {
        DuplexStream stream = new DuplexStream("AUTH TEST cHJvb2Y=\r\n");
        SaslSession saslSession = new SaslSession(stream);
        MailboxProvider mailboxProvider = new MailboxProvider(stream);
        mailboxProvider.Result = MailboxResult(
            Pop3AuthenticationStatus.Success,
            new MailboxSession(stream),
            -1,
            null);
        Pop3ServerOptions options = Options(mailboxProvider);
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory(saslSession)
        };
        Pop3ServerEngine engine = StartEngine(stream, options);
        Pop3AuthenticationResult authentication = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        engine.CompleteAuthentication(authentication, CancellationToken.None);

        Assert.That(mailboxProvider.OutputAtOpen, Is.EqualTo("+OK ready\r\n"));
        Assert.That(saslSession.ApplyCount, Is.EqualTo(1));
        Assert.That(saslSession.CancelCount, Is.Zero);
        Assert.That(saslSession.OutputAtApply, Is.EqualTo(
            "+OK ready\r\n+OK Authentication successful.\r\n"));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseSession();
        stream.Dispose();
    }

    static Pop3AuthenticationResult Authenticate(Pop3ServerEngine engine)
    {
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        return engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
    }

    static Pop3MailboxOpenResult MailboxResult(
        Pop3AuthenticationStatus status,
        IPop3MailboxSession? session,
        int retryAfterSeconds,
        string? failureText)
    {
        Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
        result.Status = status;
        result.Session = session;
        result.RetryAfterSeconds = retryAfterSeconds;
        result.FailureText = failureText;
        return result;
    }

    static Pop3ServerOptions Options(IPop3MailboxProvider? mailboxProvider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = mailboxProvider;
        options.BufferSize = 128;
        return options;
    }

    static Pop3ServerEngine StartEngine(
        DuplexStream stream,
        Pop3ServerOptions options)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LocalEndPoint = "local";
        connection.RemoteEndPoint = "remote";
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
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
            result.AuthenticatedIdentity = "canonical-" + credentials.Username;
            result.MaildropKey = "maildrop-42";
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

    sealed class MailboxProvider : IPop3MailboxProvider
    {
        DuplexStream Stream;
        internal Pop3MailboxOpenResult Result = new Pop3MailboxOpenResult();
        internal bool ReturnNull;
        internal int CallCount;
        internal Pop3MailboxOpenRequest? Request;
        internal CancellationToken CancellationToken;
        internal string? OutputAtOpen;

        internal MailboxProvider(DuplexStream stream)
        {
            Stream = stream;
        }

        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;
            CancellationToken = cancellationToken;
            OutputAtOpen = Stream.WrittenText();
            if (ReturnNull)
            {
                return null!;
            }
            return Result;
        }
    }

    sealed class ContendedMailboxProvider : IPop3MailboxProvider
    {
        bool IsHeld;
        internal int CallCount;
        internal List<MailboxSession> Sessions =
            new List<MailboxSession>();

        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            MailboxSession session = new MailboxSession(
                (DuplexStream)request.Connection!.Stream!);
            Sessions.Add(session);
            Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
            result.Session = session;
            if (IsHeld)
            {
                result.Status = Pop3AuthenticationStatus.InUse;
                return result;
            }
            IsHeld = true;
            session.ReleaseHandler = () => IsHeld = false;
            result.Status = Pop3AuthenticationStatus.Success;
            return result;
        }
    }

    sealed class MailboxSession : IPop3MailboxSession
    {
        DuplexStream Stream;
        internal int ReleaseCount;
        internal string? OutputAtRelease;
        internal bool ThrowOnRelease;
        internal Action? ReleaseHandler;

        internal MailboxSession(DuplexStream stream)
        {
            Stream = stream;
        }

        public Pop3MailboxStatus GetStatus()
        {
            return new Pop3MailboxStatus();
        }

        public Pop3MessageInfo[] GetMessages()
        {
            return Array.Empty<Pop3MessageInfo>();
        }

        public Pop3UniqueId[] GetUniqueIds()
        {
            return Array.Empty<Pop3UniqueId>();
        }

        public void WriteMessage(
            int messageNumber,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
        }

        public void WriteTop(
            int messageNumber,
            int bodyLineCount,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
        }

        public void MarkDeleted(int messageNumber)
        {
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            return new Pop3MailboxStatus();
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
        }

        public void RollbackChanges()
        {
        }

        public void ReleaseSession()
        {
            ReleaseCount++;
            OutputAtRelease = Stream.WrittenText();
            if (ThrowOnRelease)
            {
                throw new InvalidOperationException("release failed");
            }
            if (ReleaseHandler != null)
            {
                ReleaseHandler();
            }
        }
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
        DuplexStream Stream;
        internal int ApplyCount;
        internal int CancelCount;
        internal string? OutputAtApply;

        internal SaslSession(DuplexStream stream)
        {
            Stream = stream;
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
            CancelCount++;
        }

        public Pop3AuthenticationResult GetAuthenticationResult()
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.AuthenticatedIdentity = "sasl-alice";
            result.MaildropKey = "sasl-maildrop";
            return result;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            ApplyCount++;
            OutputAtApply = Stream.WrittenText();
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
