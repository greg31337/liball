using System.Text;

namespace pop3.Tests;

public class Pop3ServerRetrTests
{
    [Test]
    public void RetrPreservesRawOctetsInTheAsciiRepresentation()
    {
        byte[] content = new byte[] { 128, 255, 10, 46, 0, 13 };
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { content },
            new long[] { 8 },
            new bool[] { false },
            false,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        byte[] prefix = Encoding.ASCII.GetBytes(
            AuthenticationOutput() + "+OK 8 Message follows.\r\n");
        byte[] framedContent = new byte[]
        {
            128, 255, 13, 10,
            46, 46, 0, 13, 10,
            46, 13, 10
        };
        Assert.That(transferred, Is.EqualTo(8));
        Assert.That(fixture.Stream.WrittenBytes(),
            Is.EqualTo(prefix.Concat(framedContent).ToArray()));

        fixture.Dispose();
    }

    [Test]
    public void RetrStreamsNormalizesAndDotStuffsProviderContent()
    {
        byte[] content = Encoding.ASCII.GetBytes(
            "Header: x\n\r\n.body\rline");
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { content },
            new long[] { 26 },
            new bool[] { false },
            false,
            true,
            null,
            null);
        fixture.Session.ChunkSize = 1;

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(26));
        Assert.That(fixture.Session.WriteCount, Is.EqualTo(1));
        Assert.That(fixture.Session.MessageNumber, Is.EqualTo(1));
        Assert.That(fixture.Session.UseUtf8Representation, Is.False);
        Assert.That(fixture.Session.DestinationWasWritable, Is.True);
        Assert.That(fixture.Session.DestinationWasSeekable, Is.False);
        Assert.Throws<ObjectDisposedException>(() =>
            fixture.Session.Destination!.WriteByte(1));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 26 Message follows.\r\n" +
            "Header: x\r\n" +
            "\r\n" +
            "..body\r\n" +
            "line\r\n" +
            ".\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void EmptyMessageStillReceivesAnExactTerminator()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Array.Empty<byte>() },
            new long[] { 0 },
            new bool[] { false },
            false,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 0 Message follows.\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void LargeMessageIsConsumedConcurrentlyBeyondPipeCapacity()
    {
        byte[] content = new byte[262144];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = 120;
            content[index + 1] = 10;
            index += 2;
        }
        long expectedBytes = content.Length / 2 * 3;
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { content },
            new long[] { expectedBytes },
            new bool[] { false },
            false,
            true,
            null,
            null);
        fixture.Session.ChunkSize = 4096;

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(expectedBytes));
        Assert.That(fixture.Stream.OutputLength,
            Is.GreaterThan(content.Length));
        Assert.That(fixture.Stream.WrittenText(), Does.EndWith("x\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeRequestsAndPreservesTheUtf8Representation()
    {
        byte[] content = Encoding.UTF8.GetBytes("Subject: café\n\nbody");
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { content },
            new long[] { 24 },
            new bool[] { true },
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(24));
        Assert.That(fixture.Session.UseUtf8Representation, Is.True);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 24 Message follows.\r\n" +
            "Subject: café\r\n\r\nbody\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeLeavesAsciiOnlyRepresentationUnchanged()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("body") },
            new long[] { 6 },
            new bool[] { false },
            true,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(6));
        Assert.That(fixture.Session.UseUtf8Representation, Is.False);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 6 Message follows.\r\nbody\r\n.\r\n"));
        fixture.Dispose();
    }

    [Test]
    public void Utf8ModeRejectsMalformedProviderRepresentation()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { new byte[] { 195, 40 } },
            new long[] { 4 },
            new bool[] { true },
            true,
            true,
            null,
            null);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleRetrCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK 4 Message follows.\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        fixture.Dispose();
    }

    [Test]
    public void RetrRequiresTheProviderOutputToMatchListSize()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("x") },
            new long[] { 4 },
            new bool[] { false },
            false,
            true,
            null,
            null);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleRetrCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 4 Message follows.\r\n" +
            "x\r\n"));
        Assert.That(fixture.Stream.WrittenText(), Does.Not.EndWith(".\r\n"));
        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        fixture.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingAndDeletedMessagesAreRejectedBeforeProviderWork(
        bool deleted)
    {
        string input = deleted ? "RETR 1\r\n" : "RETR 4\r\n";
        Fixture fixture = StartTransaction(
            input,
            new byte[][] { Encoding.ASCII.GetBytes("body") },
            new long[] { 6 },
            new bool[] { false },
            false,
            true,
            null,
            null);
        if (deleted)
        {
            fixture.Engine.MarkMailboxMessageDeleted(1);
        }

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(-1));
        Assert.That(fixture.Session.WriteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "-ERR No such message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void Utf8OnlyMessageReturnsTheRegisteredRetryCode()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.UTF8.GetBytes("café") },
            new long[] { 7 },
            new bool[] { true },
            false,
            true,
            null,
            null);

        long transferred = fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(transferred, Is.EqualTo(-1));
        Assert.That(fixture.Session.WriteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR [UTF8] Enable UTF8 to retrieve this message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void DisabledResponseCodesOmitTheUtf8Code()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.UTF8.GetBytes("café") },
            new long[] { 7 },
            new bool[] { true },
            false,
            false,
            null,
            null);

        fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "-ERR Enable UTF8 to retrieve this message.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void ProviderFailureNeverWritesACompletionTerminator()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("partial\n") },
            new long[] { 9 },
            new bool[] { false },
            false,
            true,
            null,
            null);
        fixture.Session.ThrowAfterWrite = true;

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Engine.HandleRetrCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 9 Message follows.\r\npartial\r\n"));
        Assert.That(fixture.Stream.WrittenText(),
            Does.Not.EndWith("\r\n.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void OverlongProviderLineNeverWritesACompletionTerminator()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("1234567") },
            new long[] { 9 },
            new bool[] { false },
            false,
            true,
            null,
            null);
        fixture.Options.MaximumMessageLineOctets = 8;

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleRetrCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() + "+OK 9 Message follows.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void TransferObserverReceivesNormalizedProgressAndCompletion()
    {
        TransferObserver observer = new TransferObserver();
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("one\ntwo") },
            new long[] { 10 },
            new bool[] { false },
            false,
            true,
            null,
            observer);

        fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(observer.Events.Count, Is.EqualTo(3));
        AssertProgress(observer.Events[0], 5, false);
        AssertProgress(observer.Events[1], 10, false);
        AssertProgress(observer.Events[2], 10, true);

        fixture.Dispose();
    }

    [Test]
    public void RetrUsesLocalizedSuccessAndFailureKeys()
    {
        LanguageProvider language = new LanguageProvider();
        Fixture fixture = StartTransaction(
            "RETR 1\r\nRETR 9\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("body") },
            new long[] { 6 },
            new bool[] { false },
            false,
            true,
            language,
            null);
        fixture.Engine.GetStateMachine().SelectLanguage("fr");

        fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        fixture.Engine.HandleRetrCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(language.Keys, Is.EqualTo(new[]
        {
            "retr-success",
            "retr-message-unavailable"
        }));
        Assert.That(language.SelectedLanguages,
            Is.EqualTo(new[] { "fr", "fr" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput() +
            "+OK 6 texte-retr-success\r\nbody\r\n.\r\n" +
            "-ERR texte-retr-message-unavailable\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void CancellationPrecedesSuccessAndProviderOutput()
    {
        Fixture fixture = StartTransaction(
            "RETR 1\r\n",
            new byte[][] { Encoding.ASCII.GetBytes("body") },
            new long[] { 6 },
            new bool[] { false },
            false,
            true,
            null,
            null);
        Pop3Command command = fixture.Engine.ReadNextCommand(
            CancellationToken.None);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            fixture.Engine.HandleRetrCommand(command, source.Token));

        Assert.That(fixture.Session.WriteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            AuthenticationOutput()));

        fixture.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void RetrHandlerValidatesThePendingAndSuppliedCommand(int scenario)
    {
        string input = scenario == 0 ? "STAT\r\n" : "RETR 1\r\n";
        Fixture fixture = StartTransaction(
            input,
            new byte[][] { Encoding.ASCII.GetBytes("body") },
            new long[] { 6 },
            new bool[] { false },
            false,
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
            supplied.Keyword = Pop3Commands.Retr;
            supplied.Arguments = new string[] { "2147483648" };
        }

        if (scenario == 0)
        {
            Assert.Throws<Pop3StateException>(() =>
                fixture.Engine.HandleRetrCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else if (scenario == 1)
        {
            Assert.Throws<ArgumentNullException>(() =>
                fixture.Engine.HandleRetrCommand(
                    supplied!,
                    CancellationToken.None));
        }
        else
        {
            Assert.Throws<ArgumentException>(() =>
                fixture.Engine.HandleRetrCommand(
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
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.Retrieve));
        Assert.That(progress.MessageNumber, Is.EqualTo(1));
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytes));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(10));
        Assert.That(progress.IsComplete, Is.EqualTo(complete));
    }

    static Fixture StartTransaction(
        string transactionInput,
        byte[][] messages,
        long[] sizes,
        bool[] utf8Representations,
        bool utf8Enabled,
        bool responseCodesEnabled,
        IPop3LanguageProvider? languageProvider,
        IPop3TransferObserver? observer)
    {
        DuplexStream stream = new DuplexStream(
            "USER alice\r\nPASS secret\r\n" + transactionInput);
        MailboxSession session = new MailboxSession(
            messages,
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
        byte[][] Messages;
        internal int ChunkSize = int.MaxValue;
        internal bool ThrowAfterWrite;
        internal int WriteCount;
        internal int MessageNumber;
        internal bool UseUtf8Representation;
        internal bool DestinationWasWritable;
        internal bool DestinationWasSeekable;
        internal Stream? Destination;
        internal List<int> MarkedNumbers = new List<int>();

        internal MailboxSession(
            byte[][] messages,
            long[] sizes,
            bool[] utf8Representations)
        {
            if (messages.Length != sizes.Length ||
                messages.Length != utf8Representations.Length)
            {
                throw new ArgumentException();
            }
            Messages = messages;
            Status = new Pop3MailboxStatus();
            Status.MessageCount = messages.Length;
            MessageInfo = new Pop3MessageInfo[messages.Length];
            UniqueIds = new Pop3UniqueId[messages.Length];
            int index = 0;
            while (index < messages.Length)
            {
                Pop3MessageInfo info = new Pop3MessageInfo();
                info.MessageNumber = index + 1;
                info.OctetCount = sizes[index];
                info.IsUtf8Representation = utf8Representations[index];
                MessageInfo[index] = info;
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
            WriteCount++;
            MessageNumber = messageNumber;
            UseUtf8Representation = useUtf8Representation;
            DestinationWasWritable = destination.CanWrite;
            DestinationWasSeekable = destination.CanSeek;
            Destination = destination;
            byte[] content = Messages[messageNumber - 1];
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

        internal long OutputLength
        {
            get => Output.Length;
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
