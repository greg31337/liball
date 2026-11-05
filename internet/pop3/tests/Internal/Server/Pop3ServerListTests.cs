using System.Text;

namespace pop3.Tests;

public class Pop3ServerListTests
{
    [Test]
    public void StatListAndUidlRemainConsistentAfterDeletion()
    {
        Fixture fixture = StartTransaction(
            "STAT\r\nLIST\r\nUIDL\r\nLIST 2\r\nUIDL 2\r\n",
            new long[] { 10, 20, 30 },
            new bool[] { false, false, false },
            false,
            true,
            null);
        fixture.Engine.MarkMailboxMessageDeleted(2);

        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MessageInfo[] messages = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3UniqueId[] uniqueIds = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3MessageInfo[] deletedMessage = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3UniqueId[] deletedUniqueId = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(40));
        Assert.That(messages.Select(message => message.MessageNumber),
            Is.EqualTo(new[] { 1, 3 }));
        Assert.That(messages.Select(message => message.OctetCount),
            Is.EqualTo(new long[] { 10, 30 }));
        Assert.That(uniqueIds.Select(uniqueId => uniqueId.MessageNumber),
            Is.EqualTo(new[] { 1, 3 }));
        Assert.That(uniqueIds.Select(uniqueId => uniqueId.Value),
            Is.EqualTo(new[] { "uid-1", "uid-3" }));
        Assert.That(deletedMessage, Is.Empty);
        Assert.That(deletedUniqueId, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 2 40 Maildrop status.\r\n" +
            "+OK Scan listing follows.\r\n" +
            "1 10\r\n" +
            "3 30\r\n" +
            ".\r\n" +
            "+OK Unique-id listing follows.\r\n" +
            "1 uid-1\r\n" +
            "3 uid-3\r\n" +
            ".\r\n" +
            "-ERR No such message.\r\n" +
            "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void SingleListReturnsTheStableMessageNumberAndSize()
    {
        Fixture fixture = StartTransaction(
            "LIST 2\r\n",
            new long[] { 0, long.MaxValue },
            new bool[] { false, false },
            false,
            true,
            null);

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].MessageNumber, Is.EqualTo(2));
        Assert.That(result[0].OctetCount, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 2 9223372036854775807 Message scan listing.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void AllListExcludesDeletedMessagesWithoutRenumbering()
    {
        Fixture fixture = StartTransaction(
            "LIST\r\nSTAT\r\n",
            new long[] { 10, 20, 30 },
            new bool[] { false, false, false },
            false,
            true,
            null);
        fixture.Engine.MarkMailboxMessageDeleted(2);

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Select(message => message.MessageNumber),
            Is.EqualTo(new[] { 1, 3 }));
        Assert.That(result.Select(message => message.OctetCount),
            Is.EqualTo(new long[] { 10, 30 }));
        Assert.That(result.Select(message => message.IsDeleted),
            Is.EqualTo(new[] { false, false }));
        Assert.That(fixture.Session.MarkedNumbers, Is.EqualTo(new[] { 2 }));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Scan listing follows.\r\n" +
            "1 10\r\n" +
            "3 30\r\n" +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void AllListFramesAnEmptyMaildrop()
    {
        Fixture fixture = StartTransaction(
            "LIST\r\n",
            Array.Empty<long>(),
            Array.Empty<bool>(),
            false,
            true,
            null);

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Scan listing follows.\r\n.\r\n"));

        fixture.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingAndDeletedSingleMessagesUseTheSameFailure(bool deleted)
    {
        string input = deleted ? "LIST 2\r\n" : "LIST 4\r\n";
        Fixture fixture = StartTransaction(
            input,
            new long[] { 10, 20, 30 },
            new bool[] { false, false, false },
            false,
            true,
            null);
        if (deleted)
        {
            fixture.Engine.MarkMailboxMessageDeleted(2);
        }

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [TestCase(true, "LIST 1\r\n", "Enable UTF8 to access this message.")]
    [TestCase(false, "LIST\r\n", "Enable UTF8 to access this maildrop.")]
    public void Utf8OnlyRepresentationFailsBeforeSingleOrMultilineOutput(
        bool single,
        string commandText,
        string failureText)
    {
        Fixture fixture = StartTransaction(
            commandText,
            new long[] { 12 },
            new bool[] { true },
            false,
            true,
            null);

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(single, Is.EqualTo(commandText.Contains("1")));
        Assert.That(result, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR [UTF8] " + failureText + "\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void DisabledResponseCodesOmitTheUtf8Code()
    {
        Fixture fixture = StartTransaction(
            "LIST 1\r\n",
            new long[] { 12 },
            new bool[] { true },
            false,
            false,
            null);

        fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR Enable UTF8 to access this message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeUsesUtf8RepresentationSizes()
    {
        Fixture fixture = StartTransaction(
            "LIST\r\n",
            new long[] { 12, 18 },
            new bool[] { true, true },
            true,
            true,
            null);

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Select(message => message.IsUtf8Representation),
            Is.EqualTo(new[] { true, true }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Scan listing follows.\r\n" +
            "1 12\r\n" +
            "2 18\r\n" +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void DeletedUtf8OnlyMessageDoesNotBlockAllList()
    {
        Fixture fixture = StartTransaction(
            "LIST\r\n",
            new long[] { 10, 20 },
            new bool[] { false, true },
            false,
            true,
            null);
        fixture.Engine.MarkMailboxMessageDeleted(2);

        Pop3MessageInfo[] result = fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Select(message => message.MessageNumber),
            Is.EqualTo(new[] { 1 }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Scan listing follows.\r\n1 10\r\n.\r\n"));

        fixture.Dispose();
    }

    [TestCase("LIST 2147483647\r\n")]
    [TestCase("LIST 0004\r\n")]
    public void ValidButMissingMessageNumbersReturnANegativeResponse(string input)
    {
        Fixture fixture = StartTransaction(
            input,
            new long[] { 10 },
            new bool[] { false },
            false,
            true,
            null);

        fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void ListUsesLocalizedSuccessAndFailureKeys()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "LIST 1\r\nLIST\r\nLIST 9\r\n",
            new long[] { 10 },
            new bool[] { false },
            false,
            true,
            language);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleListCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.Keys, Is.EqualTo(new[]
        {
            "list-one-success",
            "list-all-success",
            "list-message-unavailable"
        }));
        Assert.That(language.SelectedLanguages,
            Is.EqualTo(new[] { "fr", "fr", "fr" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 1 10 texte-list-one-success\r\n" +
            "+OK texte-list-all-success\r\n" +
            "1 10\r\n.\r\n" +
            "-ERR texte-list-message-unavailable\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void CancellationPrecedesListOutputAndLocalization()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "LIST\r\n",
            new long[] { 10 },
            new bool[] { false },
            false,
            true,
            language);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.HandleListCommand(command, source.Token));

        Assert.That(language.Keys, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ListHandlerValidatesThePendingAndSuppliedCommand(int scenario)
    {
        string input = scenario == 0 ? "STAT\r\n" : "LIST\r\n";
        Fixture fixture = StartTransaction(
            input,
            new long[] { 10 },
            new bool[] { false },
            false,
            true,
            null);
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
            supplied.Keyword = Pop3Commands.List;
            supplied.Arguments = new string[] { "2147483648" };
        }

        if (scenario == 0)
        {
            Assert.Throws<Pop3StateException>(() =>
                fixture.Engine.HandleListCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else if (scenario == 1)
        {
            Assert.Throws<ArgumentNullException>(() =>
                fixture.Engine.HandleListCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                fixture.Engine.HandleListCommand(
                    supplied!,
                    CancellationToken.None));
        }
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [Test]
    public void ListLineWriterPrevalidatesEveryEntryBeforeOutput()
    {
        using MemoryStream destination = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(destination, 32);
        Pop3MessageInfo valid = new Pop3MessageInfo();
        valid.MessageNumber = 1;
        valid.OctetCount = 10;
        Pop3MessageInfo invalid = new Pop3MessageInfo();
        invalid.MessageNumber = 2;
        invalid.OctetCount = -1;

        Assert.Throws<InvalidDataException>(() =>
            writer.WriteServerListEntries(
                new Pop3MessageInfo[] { valid, invalid },
                Pop3Defaults.MaximumResponseLineOctets,
                CancellationToken.None));

        Assert.That(destination.Length, Is.Zero);
    }

    static Fixture StartTransaction(
        string transactionInput,
        long[] sizes,
        bool[] utf8Representations,
        bool utf8Enabled,
        bool responseCodesEnabled,
        IPop3LanguageProvider? languageProvider)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput);
        MailboxSession session = new MailboxSession(
            sizes,
            utf8Representations);
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.EnableResponseCodes = responseCodesEnabled;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider(session);
        options.LanguageProvider = languageProvider;
        options.BufferSize = 128;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        if (utf8Enabled)
        {
            engine.GetStateMachine().EnableUtf8Mode();
        }
        engine.HandleUserCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3AuthenticationResult authentication = engine.HandlePasswordCommand(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        engine.CompleteAuthentication(authentication, CancellationToken.None);
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
        Pop3MailboxStatus Status;
        Pop3MessageInfo[] Messages;
        Pop3UniqueId[] UniqueIds;
        internal List<int> MarkedNumbers = new List<int>();

        internal MailboxSession(long[] sizes, bool[] utf8Representations)
        {
            if (sizes.Length != utf8Representations.Length)
            {
                throw new ArgumentException();
            }
            Status = new Pop3MailboxStatus();
            Status.MessageCount = sizes.Length;
            Messages = new Pop3MessageInfo[sizes.Length];
            UniqueIds = new Pop3UniqueId[sizes.Length];
            int index = 0;
            while (index < sizes.Length)
            {
                Pop3MessageInfo message = new Pop3MessageInfo();
                message.MessageNumber = index + 1;
                message.OctetCount = sizes[index];
                message.IsUtf8Representation = utf8Representations[index];
                Messages[index] = message;
                Status.TotalOctets += sizes[index];
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
            MarkedNumbers.Add(messageNumber);
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            return Status;
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
