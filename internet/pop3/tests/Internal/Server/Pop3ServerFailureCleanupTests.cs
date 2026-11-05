using System.Text;

namespace pop3.Tests;

public class Pop3ServerFailureCleanupTests
{
    [Test]
    public void EndOfStreamRollsBackReleasesAndClosesWithoutAResponse()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\n",
            false,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.Throws<EndOfStreamException>(() =>
            fixture.Engine.ReadNextCommand(CancellationToken.None));

        AssertAborted(fixture, new[] { "mark-1", "rollback", "release" });
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n"));

        fixture.Stream.Dispose();
    }

    [Test]
    public void EndOfStreamWithoutDeletionStillRollsBackAndReleases()
    {
        Fixture fixture = StartTransaction(
            "",
            false,
            2000,
            true);

        Assert.Throws<EndOfStreamException>(() =>
            fixture.Engine.ReadNextCommand(CancellationToken.None));

        AssertAborted(fixture, new[] { "rollback", "release" });
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Stream.Dispose();
    }

    [Test]
    public void ExplicitAbortIsIdempotentAndNeverCommitsDeletionMarks()
    {
        Fixture fixture = StartTransaction(
            "DELE 2\r\n",
            false,
            2000,
            false);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        fixture.Engine.AbortSession();
        fixture.Engine.AbortSession();

        AssertAborted(fixture, new[] { "mark-2", "rollback", "release" });
        Assert.That(fixture.Stream.DisposeCount, Is.EqualTo(1));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n"));
    }

    [Test]
    public void CommandReadCancellationRollsBackAndReleases()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\n",
            true,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.ReadNextCommand(source.Token));

        AssertAborted(fixture, new[] { "mark-1", "rollback", "release" });

        fixture.Stream.Dispose();
    }

    [Test]
    public void CommandReadTimeoutRollsBackAndReportsTimeoutContext()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\n",
            true,
            30,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Pop3ConnectionException exception =
            Assert.Throws<Pop3ConnectionException>(() =>
                fixture.Engine.ReadNextCommand(CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("Command read"));
        Assert.That(exception.SessionState,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(exception.IsTimeout, Is.True);
        Assert.That(exception.IsEndOfStream, Is.False);
        AssertAborted(fixture, new[] { "mark-1", "rollback", "release" });

        fixture.Stream.Dispose();
    }

    [Test]
    public void InactivityAutologoutRollsBackWithoutResponseOrUpdate()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\n",
            true,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        long expiredAt = Environment.TickCount64 +
            Pop3Defaults.MinimumServerInactivityTimeoutMilliseconds;

        Assert.That(
            fixture.Engine.GetStateMachine().ExpireInactivity(expiredAt),
            Is.True);
        Pop3ConnectionException exception =
            Assert.Throws<Pop3ConnectionException>(() =>
                fixture.Engine.ReadNextCommand(CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("Inactivity autologout"));
        Assert.That(exception.SessionState,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(exception.IsTimeout, Is.True);
        AssertAborted(fixture, new[] { "mark-1", "rollback", "release" });
        Assert.That(fixture.Session.CommitCount, Is.Zero);
        Assert.That(fixture.Session.StateAtRollback,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n"));

        fixture.Stream.Dispose();
    }

    [TestCase("broken\n")]
    [TestCase("broken\rx")]
    public void MalformedFramingRollsBackAndReleases(string malformedInput)
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\n" + malformedInput,
            false,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.ReadNextCommand(CancellationToken.None));

        AssertAborted(fixture, new[] { "mark-1", "rollback", "release" });

        fixture.Stream.Dispose();
    }

    [Test]
    public void DeletionProviderFailureAbortsTheWholeTransaction()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\nDELE 2\r\n",
            false,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Session.FailMarkNumber = 2;

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Engine.HandleDeleCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        AssertAborted(fixture, new[]
        {
            "mark-1", "mark-2", "rollback", "release"
        });
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n"));

        fixture.Stream.Dispose();
    }

    [Test]
    public void RetrievalProviderFailureRollsBackWithoutWritingATerminator()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\nRETR 2\r\n",
            false,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Session.ThrowOnWriteMessage = true;

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Engine.HandleRetrCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        AssertAborted(fixture, new[]
        {
            "mark-1", "write-message", "rollback", "release"
        });
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "+OK 20 Message follows.\r\n"));
        Assert.That(fixture.Stream.WrittenText(),
            Does.Not.EndWith("\r\n.\r\n"));

        fixture.Stream.Dispose();
    }

    [Test]
    public void TransactionCommandLoggerFailureRollsBackWithoutDeletion()
    {
        ThrowingLogger logger = new ThrowingLogger();
        logger.FailureText = Pop3Commands.Noop;
        Fixture fixture = StartTransaction(
            "NOOP\r\n",
            false,
            2000,
            true,
            logger,
            null);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                fixture.Engine.ReadNextCommand(CancellationToken.None))!;

        Assert.That(exception.Message, Is.EqualTo("logger failed"));
        Assert.That(logger.FailureCount, Is.EqualTo(1));
        AssertAborted(fixture, new[] { "rollback", "release" });
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Stream.Dispose();
    }

    [Test]
    public void RetrievalObserverFailureRollsBackWithoutDeletion()
    {
        ThrowingObserver observer = new ThrowingObserver();
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            false,
            2000,
            true,
            null,
            observer);
        fixture.Session.MessageContent =
            Encoding.ASCII.GetBytes("12345678");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                fixture.Engine.HandleRetrCommand(
                    fixture.Engine.ReadNextCommand(CancellationToken.None),
                    CancellationToken.None))!;

        Assert.That(exception.Message, Is.EqualTo("observer failed"));
        Assert.That(observer.FailureCount, Is.EqualTo(1));
        AssertAborted(fixture, new[]
        {
            "write-message", "rollback", "release"
        });
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 10 Message follows.\r\n" +
            "12345678\r\n"));

        fixture.Stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void CleanupFailuresDoNotPreventReleaseAttemptOrConnectionClose(
        bool throwOnRollback,
        bool throwOnRelease)
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\n",
            false,
            2000,
            true);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Session.ThrowOnRollback = throwOnRollback;
        fixture.Session.ThrowOnRelease = throwOnRelease;

        fixture.Engine.AbortSession();

        AssertAborted(fixture, new[] { "mark-1", "rollback", "release" });

        fixture.Stream.Dispose();
    }

    [Test]
    public void FailedSaslSecurityLayerIsCancelledAndClosesTheConnection()
    {
        FailureStream stream = new FailureStream(
            "AUTH TEST cHJvb2Y=\r\n",
            false);
        SaslSession session = new SaslSession();
        Pop3ServerOptions options = AuthorizationOptions();
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory(session)
        };
        Pop3ServerEngine engine = StartAuthorization(stream, options, true);
        Pop3AuthenticationResult result = engine.HandleAuthCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Response success = new Pop3Response();
        success.Kind = Pop3ResponseKind.Positive;
        success.Text = "authenticated";

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.Throws<InvalidOperationException>(() =>
            engine.WriteAuthenticationSuccessResponse(
                success,
                CancellationToken.None));

        Assert.That(session.ApplyCount, Is.EqualTo(1));
        Assert.That(session.CancelCount, Is.EqualTo(1));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK authenticated\r\n"));

        stream.Dispose();
    }

    [Test]
    public void FailedTlsUpgradeClosesTheConnection()
    {
        FailureStream stream = new FailureStream("STLS\r\n", false);
        Pop3ServerOptions options = AuthorizationOptions();
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        options.TlsProvider = new FailingTlsProvider();
        Pop3ServerEngine engine = StartAuthorization(stream, options, true);

        Assert.Throws<Pop3TlsException>(() =>
            engine.UpgradeTransportToTls(
                engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Begin TLS negotiation\r\n"));

        stream.Dispose();
    }

    static void AssertAborted(Fixture fixture, string[] expectedEvents)
    {
        Assert.That(fixture.Session.Events, Is.EqualTo(expectedEvents));
        Assert.That(fixture.Session.CommitCount, Is.Zero);
        Assert.That(fixture.Session.RollbackCount, Is.EqualTo(1));
        Assert.That(fixture.Session.ReleaseCount, Is.EqualTo(1));
        Assert.That(fixture.Session.StateAtRollback,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(fixture.Session.StateAtRelease,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
    }

    static Fixture StartTransaction(
        string transactionInput,
        bool blockAtEnd,
        int readTimeoutMilliseconds,
        bool leaveOpen)
    {
        return StartTransaction(
            transactionInput,
            blockAtEnd,
            readTimeoutMilliseconds,
            leaveOpen,
            null,
            null);
    }

    static Fixture StartTransaction(
        string transactionInput,
        bool blockAtEnd,
        int readTimeoutMilliseconds,
        bool leaveOpen,
        IPop3ProtocolLogger? logger,
        IPop3TransferObserver? observer)
    {
        FailureStream stream = new FailureStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput,
            blockAtEnd);
        MailboxSession session = new MailboxSession();
        Pop3ServerOptions options = AuthorizationOptions();
        options.ReadTimeoutMilliseconds = readTimeoutMilliseconds;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider(session);
        options.Logger = logger;
        options.TransferObserver = observer;
        Pop3ServerEngine engine = StartAuthorization(
            stream,
            options,
            leaveOpen);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult authentication = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.CompleteAuthentication(authentication, CancellationToken.None);
        session.StateMachine = engine.GetStateMachine();
        return new Fixture(engine, stream, session);
    }

    static Pop3ServerEngine StartAuthorization(
        FailureStream stream,
        Pop3ServerOptions options,
        bool leaveOpen)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = leaveOpen;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    static Pop3ServerOptions AuthorizationOptions()
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.BufferSize = 128;
        return options;
    }

    static string AuthenticationOutput()
    {
        return "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "+OK Authentication successful.\r\n";
    }

    sealed class Fixture
    {
        internal Pop3ServerEngine Engine;
        internal FailureStream Stream;
        internal MailboxSession Session;

        internal Fixture(
            Pop3ServerEngine engine,
            FailureStream stream,
            MailboxSession session)
        {
            Engine = engine;
            Stream = stream;
            Session = session;
        }
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

    sealed class MailboxProvider : IPop3MailboxProvider
    {
        MailboxSession Session;

        internal MailboxProvider(MailboxSession session)
        {
            Session = session;
        }

        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.Session = Session;
            return result;
        }
    }

    sealed class MailboxSession : IPop3MailboxSession
    {
        Pop3MailboxStatus Status;
        Pop3MessageInfo[] Messages;
        Pop3UniqueId[] UniqueIds;
        internal Pop3ServerStateMachine? StateMachine;
        internal List<string> Events = new List<string>();
        internal int FailMarkNumber;
        internal bool ThrowOnWriteMessage;
        internal bool ThrowOnRollback;
        internal bool ThrowOnRelease;
        internal int CommitCount;
        internal int RollbackCount;
        internal int ReleaseCount;
        internal Pop3SessionState StateAtRollback;
        internal Pop3SessionState StateAtRelease;
        internal byte[] MessageContent = Array.Empty<byte>();

        internal MailboxSession()
        {
            Status = new Pop3MailboxStatus();
            Status.MessageCount = 2;
            Status.TotalOctets = 30;
            Messages = new Pop3MessageInfo[2];
            UniqueIds = new Pop3UniqueId[2];
            int index = 0;
            while (index < Messages.Length)
            {
                Pop3MessageInfo message = new Pop3MessageInfo();
                message.MessageNumber = index + 1;
                message.OctetCount = (index + 1) * 10;
                Messages[index] = message;
                Pop3UniqueId uniqueId = new Pop3UniqueId();
                uniqueId.MessageNumber = index + 1;
                uniqueId.Value = "uid-" + (index + 1).ToString();
                UniqueIds[index] = uniqueId;
                index++;
            }
        }

        public Pop3MailboxStatus GetStatus()
        {
            return Status;
        }

        public Pop3MessageInfo[] GetMessages()
        {
            return Messages;
        }

        public Pop3UniqueId[] GetUniqueIds()
        {
            return UniqueIds;
        }

        public void WriteMessage(
            int messageNumber,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
            Events.Add("write-message");
            if (ThrowOnWriteMessage)
            {
                throw new InvalidOperationException("retrieval failed");
            }
            destination.Write(
                MessageContent,
                0,
                MessageContent.Length);
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
            Events.Add("mark-" + messageNumber.ToString());
            if (messageNumber == FailMarkNumber)
            {
                throw new InvalidOperationException("mark failed");
            }
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            return Status;
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
            CommitCount++;
            Events.Add("commit");
        }

        public void RollbackChanges()
        {
            RollbackCount++;
            Events.Add("rollback");
            if (StateMachine != null)
            {
                StateAtRollback = StateMachine.GetCurrentState();
            }
            if (ThrowOnRollback)
            {
                throw new InvalidOperationException("rollback failed");
            }
        }

        public void ReleaseSession()
        {
            ReleaseCount++;
            Events.Add("release");
            if (StateMachine != null)
            {
                StateAtRelease = StateMachine.GetCurrentState();
            }
            if (ThrowOnRelease)
            {
                throw new InvalidOperationException("release failed");
            }
        }
    }

    sealed class ThrowingLogger : IPop3ProtocolLogger
    {
        internal string FailureText = "";
        internal int FailureCount;

        public void Log(Pop3LogEntry entry)
        {
            if (entry.Text == FailureText)
            {
                FailureCount++;
                throw new InvalidOperationException("logger failed");
            }
        }
    }

    sealed class ThrowingObserver : IPop3TransferObserver
    {
        internal int FailureCount;

        public void Report(Pop3TransferProgress progress)
        {
            FailureCount++;
            throw new InvalidOperationException("observer failed");
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
        internal int ApplyCount;
        internal int CancelCount;

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
            result.AuthenticatedIdentity = "alice";
            result.MaildropKey = "mail";
            return result;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            ApplyCount++;
            throw new InvalidOperationException("security layer failed");
        }
    }

    sealed class FailingTlsProvider : IPop3ServerTlsProvider
    {
        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("TLS failed");
        }
    }

    sealed class FailureStream : Stream
    {
        byte[] Input;
        int InputOffset;
        bool BlockAtEnd;
        MemoryStream Output = new MemoryStream();
        internal int DisposeCount;

        internal FailureStream(string input, bool blockAtEnd)
        {
            Input = Encoding.ASCII.GetBytes(input);
            BlockAtEnd = blockAtEnd;
        }

        internal string WrittenText()
        {
            return Encoding.UTF8.GetString(Output.ToArray());
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

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (InputOffset < Input.Length || !BlockAtEnd)
            {
                return Task.FromResult(Read(buffer, offset, count));
            }
            return WaitForCancellation(cancellationToken);
        }

        async Task<int> WaitForCancellation(
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
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
        }
    }
}
