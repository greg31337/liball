using System.Text;

namespace pop3.Tests;

public class Pop3ServerStatTests
{
    [Test]
    public void StatReturnsStablePopulatedStatusWithDefaultText()
    {
        Fixture fixture = StartTransaction(
            "STAT\r\nSTAT\r\n",
            new long[] { 10, 20 },
            null);

        Pop3MailboxStatus first = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        first.MessageCount = 99;
        first.TotalOctets = 99;
        Pop3MailboxStatus second = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(second.MessageCount, Is.EqualTo(2));
        Assert.That(second.TotalOctets, Is.EqualTo(30));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 2 30 Maildrop status.\r\n" +
            "+OK 2 30 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void StatReturnsZeroForAnEmptyMaildrop()
    {
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            Array.Empty<long>(),
            null);

        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(status.MessageCount, Is.Zero);
        Assert.That(status.TotalOctets, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK 0 0 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void StatExcludesMessagesMarkedForDeletion()
    {
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { 10, 20, 30 },
            null);
        fixture.Engine.MarkMailboxMessageDeleted(2);

        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(40));
        Assert.That(fixture.Session.MarkedNumbers, Is.EqualTo(new[] { 2 }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK 2 40 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void StatFormatsTheMaximumSignedOctetCountInvariantly()
    {
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { long.MaxValue },
            null);

        Pop3MailboxStatus status = fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(status.MessageCount, Is.EqualTo(1));
        Assert.That(status.TotalOctets, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 1 9223372036854775807 Maildrop status.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void StatUsesTheSelectedLanguageAndStableResponseKey()
    {
        LanguageProvider language = new LanguageProvider();
        language.ResponseText = "boîte prête";
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { 10 },
            language);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.CallCount, Is.EqualTo(1));
        Assert.That(language.Request, Is.Not.Null);
        Assert.That(language.Request!.SelectedLanguage, Is.EqualTo("fr"));
        Assert.That(language.Request.ResponseTextKey,
            Is.EqualTo("stat-success"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK 1 10 boîte prête\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void EmptyLocalizedTextEmitsOnlyTheRequiredDropListing()
    {
        LanguageProvider language = new LanguageProvider();
        language.ResponseText = "";
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { 10 },
            language);

        fixture.Engine.HandleStatCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.Request!.SelectedLanguage, Is.Null);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK 1 10\r\n"));

        fixture.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void InvalidLocalizedTextIsRejectedBeforeWritingStat(int scenario)
    {
        LanguageProvider language = new LanguageProvider();
        if (scenario == 0)
        {
            language.ReturnNull = true;
        }
        else if (scenario == 1)
        {
            language.ResponseText = "bad\ntext";
        }
        else
        {
            language.ResponseText = new string('a', 600);
        }
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { 10 },
            language);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleStatCommand(command, CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));
        Assert.Throws<Pop3StateException>(() =>
            fixture.Engine.ReadNextCommand(CancellationToken.None));

        fixture.Dispose();
    }

    [Test]
    public void CancellationPrecedesLocalizationAndWireOutput()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { 10 },
            language);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.HandleStatCommand(command, source.Token));

        Assert.That(language.CallCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [Test]
    public void StatHandlerRequiresAPendingStatCommand()
    {
        Fixture fixture = StartTransaction(
            "CAPA\r\n",
            new long[] { 10 },
            null);
        Pop3Command capa = fixture.Engine.ReadNextCommand(
            CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            fixture.Engine.HandleStatCommand(capa, CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void StatHandlerValidatesTheDispatchedCommand(int scenario)
    {
        Fixture fixture = StartTransaction(
            "STAT\r\n",
            new long[] { 10 },
            null);
        fixture.Engine.ReadNextCommand(CancellationToken.None);
        Pop3Command? suppliedCommand = new Pop3Command();
        if (scenario == 0)
        {
            suppliedCommand = null;
        }
        else if (scenario == 1)
        {
            suppliedCommand.Keyword = Pop3Commands.Noop;
        }
        else
        {
            suppliedCommand.Keyword = Pop3Commands.Stat;
            suppliedCommand.Arguments = new string[] { "1" };
        }

        if (scenario == 0)
        {
            Assert.Throws<ArgumentNullException>(() =>
                fixture.Engine.HandleStatCommand(
                    suppliedCommand!,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                fixture.Engine.HandleStatCommand(
                    suppliedCommand!,
                    CancellationToken.None));
        }
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    static Fixture StartTransaction(
        string transactionInput,
        long[] sizes,
        IPop3LanguageProvider? languageProvider)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput);
        MailboxSession session = new MailboxSession(sizes);
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

        internal MailboxSession(long[] sizes)
        {
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
        internal string ResponseText = "localized";
        internal bool ReturnNull;
        internal int CallCount;
        internal Pop3LanguageTextRequest? Request;

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
            CallCount++;
            Request = request;
            if (ReturnNull)
            {
                return null!;
            }
            return ResponseText;
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
