using System.Text;

namespace pop3.Tests;

public class Pop3ClientExtensionTests
{
    [Test]
    public void SingleLineExtensionReturnsNegativeResponseAndRedactsSensitiveCommand()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-CAP value",
            "-ERR [SYS/TEMP] later\r\n");
        FakeProtocolLogger logger = new FakeProtocolLogger();
        Pop3ClientOptions options = CreateOptions();
        options.Logger = logger;
        Pop3ClientEngine engine = ConnectAuthorization(stream, options);
        Pop3Command command = CreateCommand("xsec", false);
        command.Arguments = new[] { "secret" };
        command.ContainsSensitiveData = true;

        Pop3Response response = engine.ExecuteSingleLineExtension(
            command,
            CancellationToken.None);

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(response.Text, Is.EqualTo("[SYS/TEMP] later"));
        Assert.That(response.ResponseCodeLevels, Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nXSEC secret\r\n"));
        Assert.That(command.Keyword, Is.EqualTo("xsec"));
        Assert.That(logger.Entries, Has.Count.EqualTo(2));
        Assert.That(logger.Entries[0].Text, Is.EqualTo("XSEC [redacted]"));
        Assert.That(logger.Entries[0].IsRedacted, Is.True);
        Assert.That(logger.Entries[0].ByteCount, Is.EqualTo(13));
        Assert.That(logger.Entries[1].Text, Is.EqualTo("-ERR [SYS/TEMP] later"));
        Assert.That(engine.GetSnapshot().Capabilities.Entries[0].Name,
            Is.EqualTo("X-CAP"));
        Assert.That(engine.GetSnapshot().LastResponse!.Text,
            Is.EqualTo("[SYS/TEMP] later"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void UnknownExtensionDoesNotMutateTrackedMailboxOrCapabilities()
    {
        ExtensionSequenceStream stream = CreateTransactionStream(
            "PIPELINING\r\nX-OLD first",
            "+OK listing\r\n1 10\r\n2 20\r\n.\r\n" +
            "+OK extension accepted\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, CreateOptions());
        engine.GetAllMessageInfo(CancellationToken.None);
        Pop3ClientSnapshot before = engine.GetSnapshot();
        Pop3Command command = CreateCommand("xdel", false);
        command.Arguments = new[] { "1" };

        Pop3Response response = engine.ExecuteSingleLineExtension(
            command,
            CancellationToken.None);
        Pop3ClientSnapshot after = engine.GetSnapshot();

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(after.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(after.Messages.Select(message => message.MessageNumber),
            Is.EqualTo(before.Messages.Select(message => message.MessageNumber)));
        Assert.That(after.Messages.Select(message => message.OctetCount),
            Is.EqualTo(before.Messages.Select(message => message.OctetCount)));
        Assert.That(after.Messages.Select(message => message.IsDeleted),
            Is.EqualTo(new[] { false, false }));
        Assert.That(after.MailboxStatus!.MessageCount,
            Is.EqualTo(before.MailboxStatus!.MessageCount));
        Assert.That(after.MailboxStatus.TotalOctets,
            Is.EqualTo(before.MailboxStatus.TotalOctets));
        Assert.That(after.Capabilities.SupportsPipelining, Is.True);
        Assert.That(after.Capabilities.Entries.Select(capability => capability.Name),
            Is.EqualTo(new[] { "PIPELINING", "X-OLD" }));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nXDEL 1\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void MultilineExtensionCopiesExactDataAndReportsProgressAndLogging()
    {
        ExtensionSequenceStream stream = CreateTransactionStream(
            "X-DATA",
            "+OK data follows\r\n" +
            "one\r\n" +
            "..two\r\n" +
            ".\r\n");
        FakeProtocolLogger logger = new FakeProtocolLogger();
        FakeTransferObserver observer = new FakeTransferObserver();
        Pop3ClientOptions options = CreateOptions();
        options.Logger = logger;
        options.TransferObserver = observer;
        Pop3ClientEngine engine = ConnectTransaction(stream, options);
        Pop3Command command = CreateCommand("xdat", true);
        command.Arguments = new[] { "value" };
        MemoryStream destination = new MemoryStream();

        Pop3Response response = engine.ExecuteMultilineExtension(
            command,
            destination,
            CancellationToken.None);

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(response.IsMultiline, Is.True);
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("one\r\n.two\r\n"));
        Assert.That(destination.CanWrite, Is.True);
        Assert.That(observer.Reports, Has.Count.EqualTo(3));
        AssertProgress(observer.Reports[0], 5, false);
        AssertProgress(observer.Reports[1], 11, false);
        AssertProgress(observer.Reports[2], 11, true);
        Assert.That(logger.Entries, Has.Count.EqualTo(3));
        Assert.That(logger.Entries[0].Text, Is.EqualTo("XDAT value"));
        Assert.That(logger.Entries[1].Text, Is.EqualTo("+OK data follows"));
        Assert.That(logger.Entries[2].Text, Is.EqualTo("extension data"));
        Assert.That(logger.Entries[2].ByteCount, Is.EqualTo(11));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nXDAT value\r\n"));

        engine.Abort();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void NegativeMultilineExtensionDoesNotConsumeFollowingResponse()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-MULTI",
            "-ERR unavailable\r\n" +
            "+OK synchronized\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream, CreateOptions());
        Pop3Command multiline = CreateCommand("xmul", true);
        Pop3Command singleLine = CreateCommand("xone", false);
        MemoryStream destination = new MemoryStream();

        Pop3Response rejected = engine.ExecuteMultilineExtension(
            multiline,
            destination,
            CancellationToken.None);
        Pop3Response accepted = engine.ExecuteSingleLineExtension(
            singleLine,
            CancellationToken.None);

        Assert.That(rejected.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(rejected.IsMultiline, Is.False);
        Assert.That(destination.Length, Is.Zero);
        Assert.That(accepted.Text, Is.EqualTo("synchronized"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nXMUL\r\nXONE\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        engine.Abort();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void ExtensionMethodsRejectStandardCommandsAndInvalidInputsBeforeWriting()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-ONE",
            "+OK unused\r\n");
        Pop3ClientOptions options = CreateOptions();
        Pop3ClientEngine engine = ConnectAuthorization(stream, options);
        string writesBefore = stream.WrittenText();
        string[] standardCommands = new[]
        {
            "user", "PASS", "apop", "STAT", "list", "RETR", "dele",
            "NOOP", "rset", "QUIT", "top", "UIDL", "capa", "STLS",
            "auth", "UTF8", "lang"
        };
        int index = 0;
        while (index < standardCommands.Length)
        {
            Pop3Command standard = CreateCommand(standardCommands[index], false);
            Assert.Throws<ArgumentException>(() =>
                engine.ExecuteSingleLineExtension(standard, CancellationToken.None));
            index++;
        }

        Pop3Command multiline = CreateCommand("xmul", true);
        Pop3Command singleLine = CreateCommand("xone", false);
        Pop3Command invalid = CreateCommand("XX", false);
        MemoryStream readOnly = new MemoryStream(new byte[0], false);
        Assert.Throws<ArgumentNullException>(() =>
            engine.ExecuteSingleLineExtension(null!, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() =>
            engine.ExecuteSingleLineExtension(invalid, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.ExecuteSingleLineExtension(multiline, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.ExecuteMultilineExtension(
                singleLine,
                new MemoryStream(),
                CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() =>
            engine.ExecuteMultilineExtension(multiline, null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.ExecuteMultilineExtension(multiline, readOnly, CancellationToken.None));
        options.MaximumMessageLineOctets = 2;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.ExecuteMultilineExtension(
                multiline,
                new MemoryStream(),
                CancellationToken.None));

        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();
            Assert.Throws<OperationCanceledException>(() =>
                engine.ExecuteSingleLineExtension(singleLine, source.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        engine.Abort();
        readOnly.Dispose();
        stream.Dispose();
    }

    [Test]
    public void ExtensionCommandAcceptsTheExactLineLimitAndRejectsOverflowBeforeWriting()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-MAX",
            "+OK accepted\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream, CreateOptions());
        Pop3Command exact = CreateCommand("xmax", false);
        exact.Arguments = new[] { new string('a', 248) };
        Pop3Command overflow = CreateCommand("xmax", false);
        overflow.Arguments = new[] { new string('b', 249) };

        Pop3Response response = engine.ExecuteSingleLineExtension(
            exact,
            CancellationToken.None);
        string writesAfterExact = stream.WrittenText();
        Assert.Throws<InvalidDataException>(() =>
            engine.ExecuteSingleLineExtension(overflow, CancellationToken.None));

        Assert.That(response.Text, Is.EqualTo("accepted"));
        Assert.That(writesAfterExact, Is.EqualTo(
            "CAPA\r\nXMAX " + new string('a', 248) + "\r\n"));
        Assert.That(stream.WrittenText(), Is.EqualTo(writesAfterExact));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void MultilineExtensionEnforcesTheConfiguredDataLineLimit()
    {
        ExtensionSequenceStream exactStream = CreateAuthorizationStream(
            "X-MULTI",
            "+OK data follows\r\n123456\r\n.\r\n");
        Pop3ClientOptions exactOptions = CreateOptions();
        exactOptions.MaximumMessageLineOctets = 8;
        Pop3ClientEngine exactEngine = ConnectAuthorization(exactStream, exactOptions);
        MemoryStream exactDestination = new MemoryStream();

        exactEngine.ExecuteMultilineExtension(
            CreateCommand("xmul", true),
            exactDestination,
            CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(exactDestination.ToArray()),
            Is.EqualTo("123456\r\n"));
        Assert.That(exactEngine.GetSnapshot().State,
            Is.EqualTo(Pop3SessionState.Authorization));
        exactEngine.Abort();
        exactDestination.Dispose();
        exactStream.Dispose();

        ExtensionSequenceStream overflowStream = CreateAuthorizationStream(
            "X-MULTI",
            "+OK data follows\r\n1234567\r\n.\r\n");
        Pop3ClientOptions overflowOptions = CreateOptions();
        overflowOptions.MaximumMessageLineOctets = 8;
        Pop3ClientEngine overflowEngine = ConnectAuthorization(
            overflowStream,
            overflowOptions);
        MemoryStream overflowDestination = new MemoryStream();

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            overflowEngine.ExecuteMultilineExtension(
                CreateCommand("xmul", true),
                overflowDestination,
                CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("XMUL data"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(overflowDestination.Length, Is.Zero);
        Assert.That(overflowDestination.CanWrite, Is.True);
        Assert.That(overflowEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        overflowDestination.Dispose();
        overflowStream.Dispose();
    }

    [Test]
    public void RejectedMultilineExtensionHasNoDataLogOrProgressCallbacks()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-MULTI",
            "-ERR unavailable\r\n");
        FakeProtocolLogger logger = new FakeProtocolLogger();
        FakeTransferObserver observer = new FakeTransferObserver();
        Pop3ClientOptions options = CreateOptions();
        options.Logger = logger;
        options.TransferObserver = observer;
        Pop3ClientEngine engine = ConnectAuthorization(stream, options);
        MemoryStream destination = new MemoryStream();

        Pop3Response response = engine.ExecuteMultilineExtension(
            CreateCommand("xmul", true),
            destination,
            CancellationToken.None);

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(destination.Length, Is.Zero);
        Assert.That(observer.Reports, Is.Empty);
        Assert.That(logger.Entries.Select(entry => entry.Text),
            Is.EqualTo(new[] { "XMUL", "-ERR unavailable" }));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        engine.Abort();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void UnexpectedContinuationInvalidatesTheSession()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-ONE",
            "+ challenge\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream, CreateOptions());
        Pop3Command command = CreateCommand("xone", false);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.ExecuteSingleLineExtension(command, CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("XONE response"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().LastResponse!.Kind,
            Is.EqualTo(Pop3ResponseKind.SaslContinuation));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nXONE\r\n"));

        stream.Dispose();
    }

    [Test]
    public void IncompleteMultilineExtensionClosesSessionWithoutOwningDestination()
    {
        ExtensionSequenceStream stream = CreateAuthorizationStream(
            "X-MULTI",
            "+OK data follows\r\npartial\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream, CreateOptions());
        Pop3Command command = CreateCommand("xmul", true);
        MemoryStream destination = new MemoryStream();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ExecuteMultilineExtension(
                command,
                destination,
                CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("XMUL"));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("partial\r\n"));
        Assert.That(destination.CanWrite, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));

        destination.Dispose();
        stream.Dispose();
    }

    static void AssertProgress(
        Pop3TransferProgress progress,
        long bytesTransferred,
        bool isComplete)
    {
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.ExtensionData));
        Assert.That(progress.MessageNumber, Is.Zero);
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytesTransferred));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(-1));
        Assert.That(progress.IsComplete, Is.EqualTo(isComplete));
    }

    static Pop3Command CreateCommand(string keyword, bool expectsMultiline)
    {
        Pop3Command command = new Pop3Command();
        command.Keyword = keyword;
        command.ExpectsMultilineResponse = expectsMultiline;
        return command;
    }

    static ExtensionSequenceStream CreateAuthorizationStream(
        string capabilities,
        string extensionResponses)
    {
        return new ExtensionSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\n" + capabilities + "\r\n.\r\n" +
            extensionResponses);
    }

    static ExtensionSequenceStream CreateTransactionStream(
        string capabilities,
        string operationResponses)
    {
        return new ExtensionSequenceStream(
            "+OK ready\r\n" +
            "+OK authorization capabilities\r\nUSER\r\n.\r\n" +
            "+OK transaction capabilities\r\n" + capabilities + "\r\n.\r\n" +
            operationResponses);
    }

    static Pop3ClientEngine ConnectAuthorization(
        ExtensionSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        return engine;
    }

    static Pop3ClientEngine ConnectTransaction(
        ExtensionSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = ConnectAuthorization(stream, options);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    static Pop3ClientOptions CreateOptions()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.MaximumMessageLineOctets = 1024;
        options.LeaveStreamOpen = true;
        return options;
    }

    class ExtensionSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public ExtensionSequenceStream(string responses)
        {
            Responses = Encoding.UTF8.GetBytes(responses);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return Responses.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public string WrittenText()
        {
            return Encoding.ASCII.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = Responses.Length - ReadPosition;
            if (remaining == 0)
            {
                return 0;
            }
            int readCount = Math.Min(count, remaining);
            Array.Copy(Responses, ReadPosition, buffer, offset, readCount);
            ReadPosition += readCount;
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Written.Write(buffer, offset, count);
        }
    }
}
