using System.Text;

namespace pop3.Tests;

public class Pop3ServerTopTests
{
    [Test]
    public void TopPreservesSelectedRawOctetsInTheAsciiRepresentation()
    {
        byte[] content = new byte[]
        {
            88, 58, 32, 128, 10,
            10,
            46, 255, 10,
            120
        };
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][] { content },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        byte[] prefix = Encoding.ASCII.GetBytes(
            AuthenticationOutput() + "+OK Top of message follows.\r\n");
        byte[] framedContent = new byte[]
        {
            88, 58, 32, 128, 13, 10,
            13, 10,
            46, 46, 255, 13, 10,
            46, 13, 10
        };
        Assert.That(transferred, Is.EqualTo(12));
        Assert.That(fixture.Stream.WrittenBytes(),
            Is.EqualTo(prefix.Concat(framedContent).ToArray()));

        fixture.Dispose();
    }

    [Test]
    public void TopStreamsAllHeadersAndOnlyTheRequestedBodyLines()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 2\r\n",
            new byte[][]
            {
                Encoding.ASCII.GetBytes(
                    "H1: a\nH2: b\r\n\r\nb1\n.b2\rb3\n")
            },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);
        fixture.Session.ChunkSize = 1;

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(25));
        Assert.That(fixture.Session.TopWriteCount, Is.EqualTo(1));
        Assert.That(fixture.Session.MessageNumber, Is.EqualTo(1));
        Assert.That(fixture.Session.BodyLineCount, Is.EqualTo(2));
        Assert.That(fixture.Session.UseUtf8Representation, Is.False);
        Assert.That(fixture.Session.DestinationWasWritable, Is.True);
        Assert.That(fixture.Session.DestinationWasSeekable, Is.False);
        Assert.Throws<ObjectDisposedException>(() =>
            fixture.Session.Destination!.WriteByte(1));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\n" +
            "H1: a\r\n" +
            "H2: b\r\n" +
            "\r\n" +
            "b1\r\n" +
            "..b2\r\n" +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void ZeroBodyLinesStillWritesAllHeadersAndOneSeparator()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 0\r\n",
            new byte[][]
            {
                Encoding.ASCII.GetBytes("A: one\nB: two\n\nbody\nmore")
            },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(18));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\n" +
            "A: one\r\nB: two\r\n\r\n.\r\n"));

        fixture.Dispose();
    }

    [TestCase("", "\r\n", 2)]
    [TestCase("Header: x", "Header: x\r\n\r\n", 13)]
    [TestCase("H: x\r\n\r\none", "H: x\r\n\r\none\r\n", 13)]
    public void EndOfMessageCompletesAvailableContentAndTheSeparator(
        string providerText,
        string expectedContent,
        long expectedOctets)
    {
        Fixture fixture = StartTransaction(
            "TOP 1 3\r\n",
            new byte[][] { Encoding.ASCII.GetBytes(providerText) },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(expectedOctets));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\n" +
            expectedContent +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void BlankBodyLineCountsTowardTheRequestedLimit()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][]
            {
                Encoding.ASCII.GetBytes("H: x\n\n\nsecond\nthird")
            },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(10));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\n" +
            "H: x\r\n\r\n\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void UnrequestedLargeBodyContentIsDrainedWithoutAFullLineBuffer()
    {
        byte[] header = Encoding.ASCII.GetBytes("H: x\n\n");
        byte[] content = new byte[header.Length + 262144];
        Array.Copy(header, content, header.Length);
        Array.Fill(content, (byte)120, header.Length, content.Length - header.Length);
        Fixture fixture = StartTransaction(
            "TOP 1 0\r\n",
            new byte[][] { content },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);
        fixture.Session.ChunkSize = 4096;
        fixture.Options.MaximumMessageLineOctets = 8;

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(8));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\nH: x\r\n\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeRequestsAndPreservesTheUtf8Representation()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][]
            {
                Encoding.UTF8.GetBytes("Subject: café\n\ncorps\nextra")
            },
            new bool[] { true },
            true,
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(25));
        Assert.That(fixture.Session.UseUtf8Representation, Is.True);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\n" +
            "Subject: café\r\n\r\ncorps\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeLeavesAsciiOnlyTopRepresentationUnchanged()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 0\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\nbody") },
            new bool[] { false },
            true,
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(8));
        Assert.That(fixture.Session.UseUtf8Representation, Is.False);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\nH: x\r\n\r\n.\r\n"));
        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeRejectsMalformedTopRepresentation()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 0\r\n",
            new byte[][] { new byte[] { 72, 58, 32, 195, 40 } },
            new bool[] { true },
            true,
            true,
            true,
            null,
            null);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleTopCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        fixture.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingAndDeletedMessagesAreRejectedBeforeProviderWork(
        bool deleted)
    {
        Fixture fixture = StartTransaction(
            deleted ? "TOP 1 1\r\n" : "TOP 4 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\nbody") },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);
        if (deleted)
        {
            fixture.Engine.MarkMailboxMessageDeleted(1);
        }

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(-1));
        Assert.That(fixture.Session.TopWriteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [TestCase(false, "-ERR Enable UTF8 to access this message.\r\n")]
    [TestCase(true, "-ERR [UTF8] Enable UTF8 to access this message.\r\n")]
    public void Utf8OnlyMessageRequiresNegotiatedUtf8Mode(
        bool responseCodesEnabled,
        string expectedResponse)
    {
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][] { Encoding.UTF8.GetBytes("H: café\n\nbody") },
            new bool[] { true },
            false,
            responseCodesEnabled,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(-1));
        Assert.That(fixture.Session.TopWriteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + expectedResponse));

        fixture.Dispose();
    }

    [Test]
    public void DisabledTopIsRejectedBeforeProviderWork()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\nbody") },
            new bool[] { false },
            false,
            true,
            false,
            null,
            null);

        long transferred = fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(-1));
        Assert.That(fixture.Session.TopWriteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR TOP is not available.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void ProviderFailureNeverWritesACompletionTerminator()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\npartial\n") },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);
        fixture.Session.ThrowAfterWrite = true;

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Engine.HandleTopCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK Top of message follows.\r\nH: x\r\n\r\npartial\r\n"));
        Assert.That(fixture.Stream.WrittenText(),
            Does.Not.EndWith("\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void OverlongSelectedLineNeverWritesACompletionTerminator()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 0\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("1234567\n\nbody") },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);
        fixture.Options.MaximumMessageLineOctets = 8;

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleTopCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK Top of message follows.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void TransferObserverReceivesSelectedProgressAndCompletion()
    {
        TransferObserver observer = new TransferObserver();
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\none\ntwo") },
            new bool[] { false },
            false,
            true,
            true,
            null,
            observer);

        fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(observer.Events.Count, Is.EqualTo(4));
        AssertProgress(observer.Events[0], 6, false);
        AssertProgress(observer.Events[1], 8, false);
        AssertProgress(observer.Events[2], 13, false);
        AssertProgress(observer.Events[3], 13, true);

        fixture.Dispose();
    }

    [Test]
    public void TopUsesLocalizedSuccessAndFailureKeys()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "TOP 1 0\r\nTOP 9 0\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\nbody") },
            new bool[] { false },
            false,
            true,
            true,
            language,
            null);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleTopCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.Keys, Is.EqualTo(new[]
        {
            "top-success",
            "top-message-unavailable"
        }));
        Assert.That(language.SelectedLanguages, Is.EqualTo(new[] { "fr", "fr" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK texte-top-success\r\nH: x\r\n\r\n.\r\n" +
            "-ERR texte-top-message-unavailable\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void CancellationPrecedesSuccessAndProviderOutput()
    {
        Fixture fixture = StartTransaction(
            "TOP 1 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\nbody") },
            new bool[] { false },
            false,
            true,
            true,
            null,
            null);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.HandleTopCommand(command, source.Token));

        Assert.That(fixture.Session.TopWriteCount, Is.Zero);
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
    public void TopHandlerValidatesThePendingAndSuppliedCommand(int scenario)
    {
        string input = scenario == 0 ? "STAT\r\n" : "TOP 1 0\r\n";
        Fixture fixture = StartTransaction(
            input,
            new byte[][] { Encoding.ASCII.GetBytes("H: x\n\nbody") },
            new bool[] { false },
            false,
            true,
            true,
            null,
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
            supplied.Keyword = Pop3Commands.Top;
            supplied.Arguments = new string[] { "2147483648", "0" };
        }
        else if (scenario == 4)
        {
            supplied = new Pop3Command();
            supplied.Keyword = Pop3Commands.Top;
            supplied.Arguments = new string[] { "1", "-1" };
        }
        else if (scenario == 5)
        {
            supplied = new Pop3Command();
            supplied.Keyword = Pop3Commands.Top;
            supplied.Arguments = new string[] { "1" };
        }

        if (scenario == 0)
        {
            Assert.Throws<Pop3StateException>(() =>
                fixture.Engine.HandleTopCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else if (scenario == 1)
        {
            Assert.Throws<ArgumentNullException>(() =>
                fixture.Engine.HandleTopCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                fixture.Engine.HandleTopCommand(
                    supplied!,
                    CancellationToken.None));
        }
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    static void AssertProgress(
        Pop3TransferProgress progress,
        long bytes,
        bool complete)
    {
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.Top));
        Assert.That(progress.MessageNumber, Is.EqualTo(1));
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytes));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(-1));
        Assert.That(progress.IsComplete, Is.EqualTo(complete));
    }

    static Fixture StartTransaction(
        string transactionInput,
        byte[][] topContent,
        bool[] utf8Representations,
        bool utf8Enabled,
        bool responseCodesEnabled,
        bool topEnabled,
        IPop3LanguageProvider? languageProvider,
        IPop3TransferObserver? observer)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput);
        MailboxSession session = new MailboxSession(
            topContent,
            utf8Representations);
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.AllowUnprotectedCredentials = true;
        options.EnableResponseCodes = responseCodesEnabled;
        options.EnableTop = topEnabled;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider(session);
        options.LanguageProvider = languageProvider;
        options.TransferObserver = observer;
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
        return new Fixture(engine, stream, session, options);
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
        internal Pop3ServerOptions Options;

        internal Fixture(
            Pop3ServerEngine engine,
            DuplexStream stream,
            MailboxSession session,
            Pop3ServerOptions options)
        {
            Engine = engine;
            Stream = stream;
            Session = session;
            Options = options;
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
        Pop3MessageInfo[] MessageInfo;
        Pop3UniqueId[] UniqueIds;
        byte[][] TopContent;
        internal int ChunkSize = int.MaxValue;
        internal bool ThrowAfterWrite;
        internal int TopWriteCount;
        internal int MessageNumber;
        internal int BodyLineCount;
        internal bool UseUtf8Representation;
        internal bool DestinationWasWritable;
        internal bool DestinationWasSeekable;
        internal Stream? Destination;
        internal List<int> MarkedNumbers = new List<int>();

        internal MailboxSession(
            byte[][] topContent,
            bool[] utf8Representations)
        {
            if (topContent.Length != utf8Representations.Length)
            {
                throw new ArgumentException();
            }
            TopContent = topContent;
            Status = new Pop3MailboxStatus();
            Status.MessageCount = topContent.Length;
            MessageInfo = new Pop3MessageInfo[topContent.Length];
            UniqueIds = new Pop3UniqueId[topContent.Length];
            int index = 0;
            while (index < topContent.Length)
            {
                Pop3MessageInfo info = new Pop3MessageInfo();
                info.MessageNumber = index + 1;
                info.OctetCount = topContent[index].Length;
                info.IsUtf8Representation = utf8Representations[index];
                MessageInfo[index] = info;
                Status.TotalOctets += info.OctetCount;
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
            return MessageInfo;
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
            TopWriteCount++;
            MessageNumber = messageNumber;
            BodyLineCount = bodyLineCount;
            UseUtf8Representation = useUtf8Representation;
            DestinationWasWritable = destination.CanWrite;
            DestinationWasSeekable = destination.CanSeek;
            Destination = destination;
            byte[] content = TopContent[messageNumber - 1];
            int offset = 0;
            while (offset < content.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count = Math.Min(ChunkSize, content.Length - offset);
                destination.Write(content, offset, count);
                offset += count;
            }
            if (ThrowAfterWrite)
            {
                throw new InvalidOperationException("provider failed");
            }
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

    sealed class TransferObserver : IPop3TransferObserver
    {
        internal List<Pop3TransferProgress> Events =
            new List<Pop3TransferProgress>();

        public void Report(Pop3TransferProgress progress)
        {
            Events.Add(progress);
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

        internal byte[] WrittenBytes()
        {
            return Output.ToArray();
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
