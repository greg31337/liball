using System.Text;

namespace pop3.Tests;

public class Pop3ServerQuitTests
{
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void AuthorizationQuitWritesSuccessAndClosesWithoutAnUpdate(
        bool leaveOpen,
        int expectedDisposeCount)
    {
        DuplexStream stream = new DuplexStream("QUIT\r\n");
        Pop3ServerEngine engine = StartAuthorization(stream, leaveOpen, null);

        bool succeeded = engine.HandleQuitCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(succeeded, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK Server signing off.\r\n"));
        Assert.That(stream.DisposeCount, Is.EqualTo(expectedDisposeCount));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.HasOpenMailbox(), Is.False);
        Assert.Throws<Pop3StateException>(() => engine.GetOptions());

        if (leaveOpen)
        {
            stream.Dispose();
        }
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void TransactionQuitCommitsReleasesRespondsAndClosesInOrder(
        bool leaveOpen,
        int expectedDisposeCount)
    {
        Fixture fixture = StartTransaction(
            "DELE 2\r\nQUIT\r\n",
            leaveOpen,
            null);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(committed, Is.True);
        Assert.That(fixture.Session.Events,
            Is.EqualTo(new[] { "mark-2", "commit", "release" }));
        Assert.That(fixture.Session.CommitCount, Is.EqualTo(1));
        Assert.That(fixture.Session.RollbackCount, Is.Zero);
        Assert.That(fixture.Session.ReleaseCount, Is.EqualTo(1));
        Assert.That(fixture.Session.StateAtCommit,
            Is.EqualTo(Pop3SessionState.Update));
        Assert.That(fixture.Session.StateAtRelease,
            Is.EqualTo(Pop3SessionState.Update));
        Assert.That(fixture.Session.OutputAtCommit, Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n"));
        Assert.That(fixture.Session.OutputAtRelease, Is.EqualTo(
            fixture.Session.OutputAtCommit));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "+OK Update committed; server signing off.\r\n"));
        Assert.That(fixture.Stream.DisposeCount,
            Is.EqualTo(expectedDisposeCount));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);

        if (leaveOpen)
        {
            fixture.Stream.Dispose();
        }
    }

    [Test]
    public void TransactionQuitCommitsEvenWhenNothingWasMarked()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);

        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(committed, Is.True);
        Assert.That(fixture.Session.Events,
            Is.EqualTo(new[] { "commit", "release" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Update committed; server signing off.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));

        fixture.Stream.Dispose();
    }

    [Test]
    public void ResetAllowsRemarkAndQuitCommitsTheFinalDeletionState()
    {
        Fixture fixture = StartTransaction(
            "DELE 2\r\nDELE 2\r\nRSET\r\nDELE 2\r\nNOOP\r\nQUIT\r\n",
            true,
            null);

        bool firstMark = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        bool repeatedMark = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MailboxStatus restored = fixture.Engine.HandleRsetCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        bool finalMark = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleNoopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(firstMark, Is.True);
        Assert.That(repeatedMark, Is.False);
        Assert.That(restored.MessageCount, Is.EqualTo(3));
        Assert.That(restored.TotalOctets, Is.EqualTo(60));
        Assert.That(finalMark, Is.True);
        Assert.That(committed, Is.True);
        Assert.That(fixture.Session.Events, Is.EqualTo(new[]
        {
            "mark-2", "reset", "mark-2", "commit", "release"
        }));
        Assert.That(fixture.Session.CommitCount, Is.EqualTo(1));
        Assert.That(fixture.Session.RollbackCount, Is.Zero);
        Assert.That(fixture.Session.ReleaseCount, Is.EqualTo(1));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "-ERR Message is already marked for deletion.\r\n" +
            "+OK 3 60 Maildrop restored.\r\n" +
            "+OK Message marked for deletion.\r\n" +
            "+OK No operation.\r\n" +
            "+OK Update committed; server signing off.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);

        fixture.Stream.Dispose();
    }

    [Test]
    public void CommitFailureRollsBackReleasesWritesFailureAndCloses()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\nQUIT\r\n",
            true,
            null);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Session.ThrowOnCommit = true;

        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(committed, Is.False);
        Assert.That(fixture.Session.Events, Is.EqualTo(new[]
        {
            "mark-1", "commit", "rollback", "release"
        }));
        Assert.That(fixture.Session.CommitCount, Is.EqualTo(1));
        Assert.That(fixture.Session.RollbackCount, Is.EqualTo(1));
        Assert.That(fixture.Session.ReleaseCount, Is.EqualTo(1));
        Assert.That(fixture.Session.StateAtRollback,
            Is.EqualTo(Pop3SessionState.Update));
        Assert.That(fixture.Session.StateAtRelease,
            Is.EqualTo(Pop3SessionState.Update));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "-ERR Unable to commit update; server signing off.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);

        fixture.Stream.Dispose();
    }

    [Test]
    public void RollbackFailureStillReleasesWritesFailureAndCloses()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);
        fixture.Session.ThrowOnCommit = true;
        fixture.Session.ThrowOnRollback = true;

        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(committed, Is.False);
        Assert.That(fixture.Session.Events,
            Is.EqualTo(new[] { "commit", "rollback", "release" }));
        Assert.That(fixture.Session.ReleaseCount, Is.EqualTo(1));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR Unable to commit update; server signing off.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);

        fixture.Stream.Dispose();
    }

    [Test]
    public void ReleaseFailureWritesFailureAndStillClosesTheConnection()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);
        fixture.Session.ThrowOnRelease = true;

        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(committed, Is.False);
        Assert.That(fixture.Session.Events,
            Is.EqualTo(new[] { "commit", "release" }));
        Assert.That(fixture.Session.RollbackCount, Is.Zero);
        Assert.That(fixture.Session.ReleaseCount, Is.EqualTo(1));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR Unable to commit update; server signing off.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);

        fixture.Stream.Dispose();
    }

    [Test]
    public void ProviderCancellationDuringCommitIsAFailedAtomicUpdate()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);
        fixture.Session.CancelCommit = true;

        bool committed = fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(committed, Is.False);
        Assert.That(fixture.Session.Events,
            Is.EqualTo(new[] { "commit", "rollback", "release" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR Unable to commit update; server signing off.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));

        fixture.Stream.Dispose();
    }

    [Test]
    public void CommitReceivesTheSuppliedCancellationToken()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);
        using CancellationTokenSource source = new CancellationTokenSource();

        fixture.Engine.HandleQuitCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            source.Token);

        Assert.That(fixture.Session.CommitToken,
            Is.EqualTo(source.Token));

        fixture.Stream.Dispose();
    }

    [Test]
    public void QuitResponsesUseStateSpecificLocalizedKeys()
    {
        LanguageProvider authorizationLanguage = new LanguageProvider();
        DuplexStream authorizationStream = new DuplexStream("QUIT\r\n");
        Pop3ServerEngine authorization = StartAuthorization(
            authorizationStream,
            true,
            authorizationLanguage);
        authorizationLanguage.StateMachine = authorization.GetStateMachine();
        authorization.GetStateMachine().SelectLanguage("fr");

        authorization.HandleQuitCommand(
            authorization.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        LanguageProvider updateLanguage = new LanguageProvider();
        Fixture update = StartTransaction(
            "QUIT\r\n",
            true,
            updateLanguage);
        updateLanguage.StateMachine = update.Engine.GetStateMachine();
        update.Engine.GetStateMachine().SelectLanguage("de");
        update.Session.ThrowOnCommit = true;

        update.Engine.HandleQuitCommand(
            update.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(authorizationLanguage.Keys,
            Is.EqualTo(new[] { "quit-authorization-success" }));
        Assert.That(authorizationLanguage.States,
            Is.EqualTo(new[] { Pop3SessionState.Authorization }));
        Assert.That(authorizationLanguage.Languages,
            Is.EqualTo(new[] { "fr" }));
        Assert.That(authorizationStream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK texte-quit-authorization-success\r\n"));
        Assert.That(updateLanguage.Keys,
            Is.EqualTo(new[] { "quit-update-failed" }));
        Assert.That(updateLanguage.States,
            Is.EqualTo(new[] { Pop3SessionState.Update }));
        Assert.That(updateLanguage.Languages,
            Is.EqualTo(new[] { "de" }));
        Assert.That(update.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR texte-quit-update-failed\r\n"));

        authorizationStream.Dispose();
        update.Stream.Dispose();
    }

    [Test]
    public void FinalResponseFailureStillClosesAfterReleasingTheMailbox()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);
        fixture.Stream.ThrowOnWrite = true;

        Assert.Throws<IOException>(() =>
            fixture.Engine.HandleQuitCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Session.Events,
            Is.EqualTo(new[] { "commit", "release" }));
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.False);
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));

        fixture.Stream.Dispose();
    }

    [Test]
    public void CancellationBeforeQuitLeavesTheTransactionUnchanged()
    {
        Fixture fixture = StartTransaction("QUIT\r\n", true, null);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.HandleQuitCommand(command, source.Token));

        Assert.That(fixture.Session.Events, Is.Empty);
        Assert.That(fixture.Engine.HasOpenMailbox(), Is.True);
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Engine.CloseSession();
        fixture.Stream.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void QuitHandlerValidatesPendingAndSuppliedCommands(int scenario)
    {
        string input = scenario == 0 ? "NOOP\r\n" : "QUIT\r\n";
        Fixture fixture = StartTransaction(input, true, null);
        Pop3Command pending = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        Pop3Command? supplied = pending;
        if (scenario == 1)
        {
            supplied = null;
        }
        else if (scenario == 2)
        {
            supplied = new Pop3Command();
            supplied.Keyword = Pop3Commands.Noop;
        }
        else if (scenario == 3)
        {
            supplied = new Pop3Command();
            supplied.Keyword = Pop3Commands.Quit;
            supplied.Arguments = new string[] { "extra" };
        }

        if (scenario == 0)
        {
            Assert.Throws<Pop3StateException>(() =>
                fixture.Engine.HandleQuitCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else if (scenario == 1)
        {
            Assert.Throws<ArgumentNullException>(() =>
                fixture.Engine.HandleQuitCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                fixture.Engine.HandleQuitCommand(
                    supplied!,
                    CancellationToken.None));
        }
        Assert.That(fixture.Session.Events, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Engine.CloseSession();
        fixture.Stream.Dispose();
    }

    [Test]
    public void ServerUpdateTransitionRejectsOtherStartingStates()
    {
        DuplexStream stream = new DuplexStream("");
        Pop3ServerEngine engine = StartAuthorization(stream, true, null);

        Pop3StateException exception = Assert.Throws<Pop3StateException>(() =>
            engine.GetStateMachine().EnterUpdate())!;

        Assert.That(exception.Operation, Is.EqualTo("EnterUpdate"));
        Assert.That(exception.SessionState,
            Is.EqualTo(Pop3SessionState.Authorization));

        engine.CloseSession();
        stream.Dispose();
    }

    static Pop3ServerEngine StartAuthorization(
        DuplexStream stream,
        bool leaveOpen,
        IPop3LanguageProvider? languageProvider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.LanguageProvider = languageProvider;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = leaveOpen;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    static Fixture StartTransaction(
        string transactionInput,
        bool leaveOpen,
        IPop3LanguageProvider? languageProvider)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput);
        MailboxSession session = new MailboxSession();
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider(session);
        options.LanguageProvider = languageProvider;
        options.BufferSize = 128;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = leaveOpen;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult authentication = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.CompleteAuthentication(authentication, CancellationToken.None);
        session.Stream = stream;
        session.StateMachine = engine.GetStateMachine();
        return new Fixture(engine, stream, session);
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
        internal DuplexStream Stream;
        internal MailboxSession Session;

        internal Fixture(
            Pop3ServerEngine engine,
            DuplexStream stream,
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
        internal DuplexStream? Stream;
        internal Pop3ServerStateMachine? StateMachine;
        internal List<string> Events = new List<string>();
        internal bool ThrowOnCommit;
        internal bool CancelCommit;
        internal bool ThrowOnRollback;
        internal bool ThrowOnRelease;
        internal int CommitCount;
        internal int RollbackCount;
        internal int ReleaseCount;
        internal CancellationToken CommitToken;
        internal Pop3SessionState StateAtCommit;
        internal Pop3SessionState StateAtRollback;
        internal Pop3SessionState StateAtRelease;
        internal string OutputAtCommit = "";
        internal string OutputAtRelease = "";

        internal MailboxSession()
        {
            Status = new Pop3MailboxStatus();
            Status.MessageCount = 3;
            Status.TotalOctets = 60;
            Messages = new Pop3MessageInfo[3];
            UniqueIds = new Pop3UniqueId[3];
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
            System.IO.Stream destination,
            CancellationToken cancellationToken)
        {
        }

        public void WriteTop(
            int messageNumber,
            int bodyLineCount,
            bool useUtf8Representation,
            System.IO.Stream destination,
            CancellationToken cancellationToken)
        {
        }

        public void MarkDeleted(int messageNumber)
        {
            Events.Add("mark-" + messageNumber.ToString());
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            Events.Add("reset");
            return Status;
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
            CommitCount++;
            CommitToken = cancellationToken;
            Events.Add("commit");
            if (StateMachine != null)
            {
                StateAtCommit = StateMachine.GetCurrentState();
            }
            if (Stream != null)
            {
                OutputAtCommit = Stream.WrittenText();
            }
            if (CancelCommit)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            if (ThrowOnCommit)
            {
                throw new InvalidOperationException("commit failed");
            }
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
            if (Stream != null)
            {
                OutputAtRelease = Stream.WrittenText();
            }
            if (ThrowOnRelease)
            {
                throw new InvalidOperationException("release failed");
            }
        }
    }

    sealed class LanguageProvider : IPop3LanguageProvider
    {
        internal Pop3ServerStateMachine? StateMachine;
        internal List<string> Keys = new List<string>();
        internal List<string?> Languages = new List<string?>();
        internal List<Pop3SessionState> States = new List<Pop3SessionState>();

        public Pop3Language[] GetLanguages()
        {
            return Array.Empty<Pop3Language>();
        }

        public bool SupportsLanguage(string languageTag)
        {
            return true;
        }

        public string GetResponseText(Pop3LanguageTextRequest request)
        {
            Keys.Add(request.ResponseTextKey);
            Languages.Add(request.SelectedLanguage);
            if (StateMachine != null)
            {
                States.Add(StateMachine.GetCurrentState());
            }
            return "texte-" + request.ResponseTextKey;
        }
    }

    sealed class DuplexStream : System.IO.Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();
        internal bool ThrowOnWrite;
        internal int DisposeCount;

        internal DuplexStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (ThrowOnWrite)
            {
                throw new IOException("write failed");
            }
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
