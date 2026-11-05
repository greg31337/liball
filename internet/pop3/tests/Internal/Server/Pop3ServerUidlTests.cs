using System.Text;

namespace pop3.Tests;

public class Pop3ServerUidlTests
{
    [Test]
    public void SingleUidlPreservesTheStableUniqueIdentifier()
    {
        Fixture fixture = StartTransaction(
            "UIDL 2\r\n",
            new string[] { "first", "Case-Sensitive_!~" },
            true,
            null);

        Pop3UniqueId[] result = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].MessageNumber, Is.EqualTo(2));
        Assert.That(result[0].Value, Is.EqualTo("Case-Sensitive_!~"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 2 Case-Sensitive_!~ Unique-id listing.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void AllUidlExcludesDeletedMessagesWithoutRenumbering()
    {
        Fixture fixture = StartTransaction(
            "UIDL\r\nSTAT\r\n",
            new string[] { "one", "two", "three" },
            true,
            null);
        fixture.Engine.MarkMailboxMessageDeleted(2);

        Pop3UniqueId[] result = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command next = fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.That(result.Select(item => item.MessageNumber),
            Is.EqualTo(new[] { 1, 3 }));
        Assert.That(result.Select(item => item.Value),
            Is.EqualTo(new[] { "one", "three" }));
        Assert.That(next.Keyword, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Unique-id listing follows.\r\n" +
            "1 one\r\n" +
            "3 three\r\n" +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void AllUidlFramesAnEmptyMaildrop()
    {
        Fixture fixture = StartTransaction(
            "UIDL\r\n",
            Array.Empty<string>(),
            true,
            null);

        Pop3UniqueId[] result = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Unique-id listing follows.\r\n.\r\n"));

        fixture.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingAndDeletedSingleMessagesUseTheSameFailure(bool deleted)
    {
        string input = deleted ? "UIDL 2\r\n" : "UIDL 4\r\n";
        Fixture fixture = StartTransaction(
            input,
            new string[] { "one", "two", "three" },
            true,
            null);
        if (deleted)
        {
            fixture.Engine.MarkMailboxMessageDeleted(2);
        }

        Pop3UniqueId[] result = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [TestCase("UIDL\r\n")]
    [TestCase("UIDL 1\r\n")]
    public void DisabledUidlRejectsBothFormsAndIsNotAdvertised(string input)
    {
        Fixture fixture = StartTransaction(
            input + "CAPA\r\n",
            new string[] { "one" },
            false,
            null);

        Pop3UniqueId[] result = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3Command capa = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        Pop3CapabilitySet capabilities = fixture.Engine.WriteCapabilitiesResponse(
            CancellationToken.None);

        Assert.That(result, Is.Empty);
        Assert.That(capa.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(capabilities.Entries.Any(entry =>
            entry.Name == Pop3Capabilities.Uidl), Is.False);
        Assert.That(fixture.Stream.WrittenText(), Does.Contain(
            "-ERR UIDL is not available.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void SnapshotProtectsUidlFromProviderMutation()
    {
        Fixture fixture = StartTransaction(
            "UIDL 1\r\n",
            new string[] { "original" },
            true,
            null);
        fixture.Session.UniqueIds[0].Value = "mutated";

        Pop3UniqueId[] result = fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result[0].Value, Is.EqualTo("original"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 1 original Unique-id listing.\r\n"));

        fixture.Dispose();
    }

    [TestCase("UIDL 2147483647\r\n")]
    [TestCase("UIDL 0004\r\n")]
    public void ValidButMissingMessageNumbersReturnANegativeResponse(string input)
    {
        Fixture fixture = StartTransaction(
            input,
            new string[] { "one" },
            true,
            null);

        fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void UidlUsesLocalizedSuccessFailureAndCapabilityKeys()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "UIDL 1\r\nUIDL\r\nUIDL 9\r\n",
            new string[] { "one" },
            true,
            language);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleUidlCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.Keys, Is.EqualTo(new[]
        {
            "uidl-one-success",
            "uidl-all-success",
            "uidl-message-unavailable"
        }));
        Assert.That(language.SelectedLanguages,
            Is.EqualTo(new[] { "fr", "fr", "fr" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 1 one texte-uidl-one-success\r\n" +
            "+OK texte-uidl-all-success\r\n" +
            "1 one\r\n.\r\n" +
            "-ERR texte-uidl-message-unavailable\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void CancellationPrecedesCapabilityChecksAndWireOutput()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "UIDL\r\n",
            new string[] { "one" },
            false,
            language);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.HandleUidlCommand(command, source.Token));

        Assert.That(language.Keys, Is.Empty);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void UidlHandlerValidatesThePendingAndSuppliedCommand(int scenario)
    {
        string input = scenario == 0 ? "STAT\r\n" : "UIDL\r\n";
        Fixture fixture = StartTransaction(
            input,
            new string[] { "one" },
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
            supplied.Keyword = Pop3Commands.Uidl;
            supplied.Arguments = new string[] { "2147483648" };
        }

        if (scenario == 0)
        {
            Assert.Throws<Pop3StateException>(() =>
                fixture.Engine.HandleUidlCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else if (scenario == 1)
        {
            Assert.Throws<ArgumentNullException>(() =>
                fixture.Engine.HandleUidlCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                fixture.Engine.HandleUidlCommand(
                    supplied!,
                    CancellationToken.None));
        }
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void UidlLineWriterPrevalidatesEveryEntryBeforeOutput(int scenario)
    {
        using MemoryStream destination = new MemoryStream();
        Pop3WireWriter writer = new Pop3WireWriter(destination, 32);
        Pop3UniqueId first = UniqueId(1, "one");
        Pop3UniqueId? second = UniqueId(2, "two");
        if (scenario == 0)
        {
            second = null;
        }
        else if (scenario == 1)
        {
            second.MessageNumber = 0;
        }
        else if (scenario == 2)
        {
            second.Value = "";
        }
        else if (scenario == 3)
        {
            second.Value = new string('a', 71);
        }
        else if (scenario == 4)
        {
            second.Value = "has space";
        }
        else if (scenario == 5)
        {
            second.Value = "one";
        }
        else
        {
            second.MessageNumber = 1;
        }

        Assert.Throws<InvalidDataException>(() =>
            writer.WriteServerUniqueIdEntries(
                new Pop3UniqueId[] { first, second! },
                Pop3Defaults.MaximumResponseLineOctets,
                CancellationToken.None));

        Assert.That(destination.Length, Is.Zero);
    }

    static Pop3UniqueId UniqueId(int messageNumber, string value)
    {
        Pop3UniqueId uniqueId = new Pop3UniqueId();
        uniqueId.MessageNumber = messageNumber;
        uniqueId.Value = value;
        return uniqueId;
    }

    static Fixture StartTransaction(
        string transactionInput,
        string[] uniqueIdValues,
        bool uidlEnabled,
        IPop3LanguageProvider? languageProvider)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput);
        MailboxSession session = new MailboxSession(uniqueIdValues);
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.EnableUidl = uidlEnabled;
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
        internal Pop3UniqueId[] UniqueIds;
        internal List<int> MarkedNumbers = new List<int>();

        internal MailboxSession(string[] uniqueIdValues)
        {
            Status = new Pop3MailboxStatus();
            Status.MessageCount = uniqueIdValues.Length;
            Messages = new Pop3MessageInfo[uniqueIdValues.Length];
            UniqueIds = new Pop3UniqueId[uniqueIdValues.Length];
            int index = 0;
            while (index < uniqueIdValues.Length)
            {
                Pop3MessageInfo message = new Pop3MessageInfo();
                message.MessageNumber = index + 1;
                message.OctetCount = (index + 1) * 10;
                Messages[index] = message;
                Status.TotalOctets += message.OctetCount;
                UniqueIds[index] = UniqueId(index + 1, uniqueIdValues[index]);
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
