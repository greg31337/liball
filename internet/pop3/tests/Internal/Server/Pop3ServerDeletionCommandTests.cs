using System.Text;

namespace pop3.Tests;

public class Pop3ServerDeletionCommandTests
{
    [Test]
    public void DeleMarksTheMessageAndUpdatesSubsequentStatMetadata()
    {
        Fixture fixture = StartTransaction(
            "DELE 2\r\nSTAT\r\n",
            null);

        bool deleted = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(deleted, Is.True);
        Assert.That(fixture.Session.MarkedNumbers, Is.EqualTo(new[] { 2 }));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(40));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "+OK 2 40 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void MissingAndRepeatedDeleRequestsFailWithoutChangingOtherMarks()
    {
        Fixture fixture = StartTransaction(
            "DELE 4\r\nDELE 2\r\nDELE 2\r\nSTAT\r\n",
            null);

        bool missing = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        bool first = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        bool repeated = fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(missing, Is.False);
        Assert.That(first, Is.True);
        Assert.That(repeated, Is.False);
        Assert.That(fixture.Session.MarkedNumbers, Is.EqualTo(new[] { 2 }));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(40));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR No such message.\r\n" +
            "+OK Message marked for deletion.\r\n" +
            "-ERR Message is already marked for deletion.\r\n" +
            "+OK 2 40 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void RsetClearsEveryMarkAndReturnsRestoredStatMetadata()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\nDELE 3\r\nRSET\r\nSTAT\r\n",
            null);

        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MailboxStatus restored = fixture.Engine.HandleRsetCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Session.ResetCount, Is.EqualTo(1));
        Assert.That(restored.MessageCount, Is.EqualTo(3));
        Assert.That(restored.TotalOctets, Is.EqualTo(60));
        Assert.That(status.MessageCount, Is.EqualTo(3));
        Assert.That(status.TotalOctets, Is.EqualTo(60));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "+OK Message marked for deletion.\r\n" +
            "+OK 3 60 Maildrop restored.\r\n" +
            "+OK 3 60 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void RsetWithoutMarksStillReturnsTheCompleteSnapshot()
    {
        Fixture fixture = StartTransaction("RSET\r\n", null);

        Pop3MailboxStatus restored = fixture.Engine.HandleRsetCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Session.ResetCount, Is.EqualTo(1));
        Assert.That(restored.MessageCount, Is.EqualTo(3));
        Assert.That(restored.TotalOctets, Is.EqualTo(60));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 3 60 Maildrop restored.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void NoopDoesNotChangeDeletionStateOrCallTheProvider()
    {
        Fixture fixture = StartTransaction(
            "DELE 2\r\nNOOP\r\nSTAT\r\n",
            null);

        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Response response = fixture.Engine.HandleNoopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(response.Text, Is.EqualTo("No operation."));
        Assert.That(fixture.Session.MarkedNumbers, Is.EqualTo(new[] { 2 }));
        Assert.That(fixture.Session.ResetCount, Is.Zero);
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(40));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n" +
            "+OK No operation.\r\n" +
            "+OK 2 40 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void InvalidDeleNumbersReceiveErrorsAndDoNotReachTheProvider()
    {
        Fixture fixture = StartTransaction(
            "DELE 0\r\nDELE -1\r\nDELE 2147483648\r\nNOOP\r\n",
            null);

        Pop3Command noop = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        fixture.Engine.HandleNoopCommand(noop, CancellationToken.None);

        Assert.That(noop.Keyword, Is.EqualTo(Pop3Commands.Noop));
        Assert.That(fixture.Session.MarkedNumbers, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR Invalid command arguments.\r\n" +
            "-ERR Invalid command arguments.\r\n" +
            "-ERR Invalid command arguments.\r\n" +
            "+OK No operation.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void ProviderMarkFailureDoesNotWriteSuccessOrChangeStatMetadata()
    {
        Fixture fixture = StartTransaction("DELE 1\r\n", null);
        fixture.Session.ThrowOnMark = true;

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Engine.HandleDeleCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Session.MarkedNumbers, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [Test]
    public void InvalidProviderResetDoesNotWriteSuccessOrClearLocalMarks()
    {
        Fixture fixture = StartTransaction(
            "DELE 1\r\nRSET\r\n",
            null);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Session.ResetStatus = Status(2, 60);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleRsetCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Session.ResetCount, Is.EqualTo(1));
        Assert.That(fixture.Session.MarkedNumbers, Is.EqualTo(new[] { 1 }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Message marked for deletion.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void TransactionCommandsUseLocalizedResponseKeys()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "DELE 9\r\nDELE 1\r\nDELE 1\r\nNOOP\r\nRSET\r\n",
            language);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleDeleCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleNoopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleRsetCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.Keys, Is.EqualTo(new[]
        {
            "dele-message-unavailable",
            "dele-success",
            "dele-already-deleted",
            "noop-success",
            "rset-success"
        }));
        Assert.That(language.SelectedLanguages,
            Is.EqualTo(new[] { "fr", "fr", "fr", "fr", "fr" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR texte-dele-message-unavailable\r\n" +
            "+OK texte-dele-success\r\n" +
            "-ERR texte-dele-already-deleted\r\n" +
            "+OK texte-noop-success\r\n" +
            "+OK 3 60 texte-rset-success\r\n"));

        fixture.Dispose();
    }

    [TestCase("DELE 1\r\n", "DELE")]
    [TestCase("NOOP\r\n", "NOOP")]
    [TestCase("RSET\r\n", "RSET")]
    public void CancellationPrecedesProviderWorkAndResponses(
        string input,
        string operation)
    {
        Fixture fixture = StartTransaction(input, null);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            InvokeCommand(fixture.Engine, command, operation, source.Token));

        Assert.That(fixture.Session.MarkedNumbers, Is.Empty);
        Assert.That(fixture.Session.ResetCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [TestCase("DELE", 0)]
    [TestCase("DELE", 1)]
    [TestCase("DELE", 2)]
    [TestCase("DELE", 3)]
    [TestCase("NOOP", 0)]
    [TestCase("NOOP", 1)]
    [TestCase("NOOP", 2)]
    [TestCase("RSET", 0)]
    [TestCase("RSET", 1)]
    [TestCase("RSET", 2)]
    public void HandlersValidatePendingAndSuppliedCommands(
        string operation,
        int scenario)
    {
        string input = scenario == 0 ? "STAT\r\n" : operation +
            (operation == "DELE" ? " 1\r\n" : "\r\n");
        Fixture fixture = StartTransaction(input, null);
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
            supplied.Keyword = Pop3Commands.Stat;
        }
        else if (scenario == 3)
        {
            supplied = new Pop3Command();
            supplied.Keyword = Pop3Commands.Dele;
            supplied.Arguments = new string[] { "2147483648" };
        }

        if (scenario == 0)
        {
            Assert.Throws<Pop3StateException>(() =>
                InvokeCommand(
                    fixture.Engine,
                    supplied!,
                    operation,
                    CancellationToken.None));
        }
        else if (scenario == 1)
        {
            Assert.Throws<ArgumentNullException>(() =>
                InvokeCommand(
                    fixture.Engine,
                    supplied!,
                    operation,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                InvokeCommand(
                    fixture.Engine,
                    supplied!,
                    operation,
                    CancellationToken.None));
        }
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    static void InvokeCommand(
        Pop3ServerEngine engine,
        Pop3Command command,
        string operation,
        CancellationToken cancellationToken)
    {
        if (operation == "DELE")
        {
            engine.HandleDeleCommand(command, cancellationToken);
        }
        else if (operation == "NOOP")
        {
            engine.HandleNoopCommand(command, cancellationToken);
        }
        else
        {
            engine.HandleRsetCommand(command, cancellationToken);
        }
    }

    static Fixture StartTransaction(
        string transactionInput,
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
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult authentication = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.CompleteAuthentication(authentication, CancellationToken.None);
        return new Fixture(engine, stream, session);
    }

    static Pop3MailboxStatus Status(int messageCount, long totalOctets)
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = messageCount;
        status.TotalOctets = totalOctets;
        return status;
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

        internal void Dispose()
        {
            Engine.CloseSession();
            Stream.Dispose();
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
        Pop3MailboxStatus FullStatus = Status(3, 60);
        Pop3MessageInfo[] Messages;
        Pop3UniqueId[] UniqueIds;
        internal List<int> MarkedNumbers = new List<int>();
        internal int ResetCount;
        internal bool ThrowOnMark;
        internal Pop3MailboxStatus? ResetStatus;

        internal MailboxSession()
        {
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
            return FullStatus;
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
            if (ThrowOnMark)
            {
                throw new InvalidOperationException("mark failed");
            }
            MarkedNumbers.Add(messageNumber);
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            ResetCount++;
            if (ResetStatus != null)
            {
                return ResetStatus;
            }
            MarkedNumbers.Clear();
            return FullStatus;
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
        }

        public void RollbackChanges()
        {
        }

        public void ReleaseSession()
        {
        }
    }

    sealed class LanguageProvider : IPop3LanguageProvider
    {
        internal List<string> Keys = new List<string>();
        internal List<string?> SelectedLanguages = new List<string?>();

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
            SelectedLanguages.Add(request.SelectedLanguage);
            return "texte-" + request.ResponseTextKey;
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
