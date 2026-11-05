using System.Text;

namespace pop3.Tests;

public class Pop3ClientBulkPipelineTests
{
    [TestCase(true, false, "PIPELINING", true)]
    [TestCase(false, false, "PIPELINING", false)]
    [TestCase(true, false, "UIDL", false)]
    [TestCase(false, true, "UIDL", true)]
    public void RetrievalBatchSelectsPipelineOrSequentialFallbackFromPolicy(
        bool enablePipelining,
        bool forcePipelining,
        string transactionCapabilities,
        bool expectedPipeline)
    {
        BatchSequenceStream stream = CreateTransactionStream(
            transactionCapabilities,
            "+OK 5 octets\r\nfirst\r\n.\r\n" +
            "+OK 6 octets\r\nsecond\r\n.\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientOptions options = CreateOptions(enablePipelining, forcePipelining);
        Pop3ClientEngine engine = ConnectTransaction(stream, options);

        byte[][] messages = engine.RetrieveMessagesToMemory(
            new[] { 2, 1 },
            CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(messages[0]), Is.EqualTo("first\r\n"));
        Assert.That(Encoding.ASCII.GetString(messages[1]), Is.EqualTo("second\r\n"));
        string expectedAtFirstRead = expectedPipeline
            ? "CAPA\r\nCAPA\r\nRETR 2\r\nRETR 1\r\n"
            : "CAPA\r\nCAPA\r\nRETR 2\r\n";
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(expectedAtFirstRead));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nRETR 2\r\nRETR 1\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void PipelinedRetrievalStreamsDrainLaterSuccessAfterEarlierFailure()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING",
            "-ERR first unavailable\r\n" +
            "+OK 8 octets\r\n" +
            "second\r\n" +
            ".\r\n" +
            "+OK synchronized\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.RetrieveMessagesToStreams(
                new[] { 1, 2 },
                new Stream[] { first, second },
                CancellationToken.None))!;
        engine.NoOperation(CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Retr));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "1" }));
        Assert.That(exception.Response.Text, Is.EqualTo("first unavailable"));
        Assert.That(first.Length, Is.Zero);
        Assert.That(Encoding.ASCII.GetString(second.ToArray()), Is.EqualTo("second\r\n"));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nRETR 1\r\nRETR 2\r\n"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nRETR 1\r\nRETR 2\r\nNOOP\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("synchronized"));

        engine.Abort();
        first.Dispose();
        second.Dispose();
        stream.Dispose();
    }

    [Test]
    public void PipelinedRetrievalDrainsSuccessesAfterAMiddleFailure()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING",
            "+OK first follows\r\nfirst\r\n.\r\n" +
            "-ERR second unavailable\r\n" +
            "+OK third follows\r\nthird\r\n.\r\n" +
            "+OK synchronized\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();
        MemoryStream third = new MemoryStream();

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.RetrieveMessagesToStreams(
                new[] { 1, 2, 3 },
                new Stream[] { first, second, third },
                CancellationToken.None))!;
        engine.NoOperation(CancellationToken.None);

        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "2" }));
        Assert.That(exception.Response.Text, Is.EqualTo("second unavailable"));
        Assert.That(Encoding.ASCII.GetString(first.ToArray()), Is.EqualTo("first\r\n"));
        Assert.That(second.Length, Is.Zero);
        Assert.That(Encoding.ASCII.GetString(third.ToArray()), Is.EqualTo("third\r\n"));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nRETR 1\r\nRETR 2\r\nRETR 3\r\n"));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "RETR 1\r\nRETR 2\r\nRETR 3\r\nNOOP\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("synchronized"));

        engine.Abort();
        first.Dispose();
        second.Dispose();
        third.Dispose();
        stream.Dispose();
    }

    [Test]
    public void PipelinedTopMemoryBatchPreservesArgumentsAndCallerOrder()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING\r\nTOP",
            "+OK top follows\r\n" +
            "headers-two\r\n" +
            ".\r\n" +
            "+OK top follows\r\n" +
            "headers-one\r\n" +
            "..body\r\n" +
            ".\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));

        byte[][] messages = engine.RetrieveTopBatchToMemory(
            new[] { 2, 1 },
            new[] { 0, 1 },
            CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(messages[0]),
            Is.EqualTo("headers-two\r\n"));
        Assert.That(Encoding.ASCII.GetString(messages[1]),
            Is.EqualTo("headers-one\r\n.body\r\n"));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nTOP 2 0\r\nTOP 1 1\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void TopBatchFallsBackToSequentialExchangeWithoutPipeliningCapability()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "TOP",
            "+OK first top\r\nH: two\r\n\r\n.\r\n" +
            "+OK second top\r\nH: one\r\n\r\nbody\r\n.\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));

        byte[][] messages = engine.RetrieveTopBatchToMemory(
            new[] { 2, 1 },
            new[] { 0, 1 },
            CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(messages[0]), Is.EqualTo("H: two\r\n\r\n"));
        Assert.That(Encoding.ASCII.GetString(messages[1]),
            Is.EqualTo("H: one\r\n\r\nbody\r\n"));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nTOP 2 0\r\n"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nTOP 2 0\r\nTOP 1 1\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void PipelinedDeleteAppliesEverySuccessfulIndependentResponse()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING",
            "+OK listing\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            "3 30\r\n" +
            ".\r\n",
            "+OK first marked\r\n" +
            "-ERR second rejected\r\n" +
            "+OK third marked\r\n" +
            "+OK synchronized\r\n");
        stream.CaptureGroupIndex = 4;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        engine.GetAllMessageInfo(CancellationToken.None);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.DeleteMessages(new[] { 1, 2, 3 }, CancellationToken.None))!;
        engine.NoOperation(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "2" }));
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 1).IsDeleted,
            Is.True);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 2).IsDeleted,
            Is.False);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 3).IsDeleted,
            Is.True);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(1));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(20));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("synchronized"));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\nDELE 2\r\nDELE 3\r\n"));
        Assert.That(stream.WrittenText(), Does.EndWith(
            "DELE 1\r\nDELE 2\r\nDELE 3\r\nNOOP\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void DeleteBatchFallsBackToSequentialExchangeWithoutPipeliningCapability()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "UIDL",
            "+OK listing\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            ".\r\n",
            "+OK first marked\r\n" +
            "+OK second marked\r\n");
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        engine.GetAllMessageInfo(CancellationToken.None);
        stream.CaptureGroupIndex = 4;

        engine.DeleteMessages(new[] { 1, 2 }, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.Messages, Is.All.Matches<Pop3MessageInfo>(message =>
            message.IsDeleted));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\n"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\nDELE 2\r\n"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void RetrievalAndTopBatchesValidateEveryInputBeforeWriting()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING\r\nTOP",
            "+OK unused\r\n");
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        string writesBefore = stream.WrittenText();
        MemoryStream writable = new MemoryStream();
        MemoryStream readOnly = new MemoryStream(new byte[0], false);

        Assert.Throws<ArgumentNullException>(() => engine.RetrieveMessagesToMemory(
            null!, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.RetrieveMessagesToMemory(
            new[] { 1, 0 }, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => engine.RetrieveMessagesToStreams(
            new[] { 1 }, null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.RetrieveMessagesToStreams(
            new[] { 1, 2 }, new Stream[] { writable }, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.RetrieveMessagesToStreams(
            new[] { 1, 2 }, new Stream[] { writable, null! }, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.RetrieveMessagesToStreams(
            new[] { 1, 2 }, new Stream[] { writable, readOnly }, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => engine.RetrieveTopBatchToMemory(
            new[] { 1 }, null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.RetrieveTopBatchToMemory(
            new[] { 1, 2 }, new[] { 0 }, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.RetrieveTopBatchToMemory(
            new[] { 1, 2 }, new[] { 0, -1 }, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.RetrieveTopBatchToStreams(
            new[] { 1 }, new[] { 0 }, new Stream[] { readOnly }, CancellationToken.None));

        Assert.That(engine.RetrieveMessagesToMemory(
            new int[0], CancellationToken.None), Is.Empty);
        Assert.That(engine.RetrieveMessagesToStreams(
            new int[0], new Stream[0], CancellationToken.None), Is.Empty);
        Assert.That(engine.RetrieveTopBatchToMemory(
            new int[0], new int[0], CancellationToken.None), Is.Empty);
        Assert.That(engine.RetrieveTopBatchToStreams(
            new int[0], new int[0], new Stream[0], CancellationToken.None), Is.Empty);
        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.Abort();
        writable.Dispose();
        readOnly.Dispose();
        stream.Dispose();
    }

    [Test]
    public void PreCancelledBatchDoesNotWriteAndDestinationsRemainOwnedByCaller()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING",
            "+OK unused\r\n");
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();
        string writesBefore = stream.WrittenText();
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.RetrieveMessagesToStreams(
                    new[] { 1, 2 },
                    new Stream[] { first, second },
                    source.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(first.CanWrite, Is.True);
        Assert.That(second.CanWrite, Is.True);
        engine.Abort();
        first.Dispose();
        second.Dispose();
        stream.Dispose();
    }

    [Test]
    public void MalformedPipelinedBodyInvalidatesSessionAndPreservesSinkOwnership()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING",
            "+OK message follows\r\n" +
            "unterminated\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.RetrieveMessagesToStreams(
                new[] { 1, 2 },
                new Stream[] { first, second },
                CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Retr));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nRETR 1\r\nRETR 2\r\n"));
        Assert.That(first.CanWrite, Is.True);
        Assert.That(second.CanWrite, Is.True);
        first.Dispose();
        second.Dispose();
        stream.Dispose();
    }

    [Test]
    public void MalformedMiddleResponseInvalidatesInsteadOfAttemptingResynchronization()
    {
        BatchSequenceStream stream = CreateTransactionStream(
            "PIPELINING",
            "+OK first follows\r\nfirst\r\n.\r\n" +
            "not-a-response\r\n" +
            "+OK third follows\r\nthird\r\n.\r\n");
        stream.CaptureGroupIndex = 3;
        Pop3ClientEngine engine = ConnectTransaction(
            stream,
            CreateOptions(true, false));
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();
        MemoryStream third = new MemoryStream();

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.RetrieveMessagesToStreams(
                new[] { 1, 2, 3 },
                new Stream[] { first, second, third },
                CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("RETR pipeline"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(Encoding.ASCII.GetString(first.ToArray()), Is.EqualTo("first\r\n"));
        Assert.That(second.Length, Is.Zero);
        Assert.That(third.Length, Is.Zero);
        Assert.That(stream.WrittenAtCapturedRead, Is.EqualTo(
            "CAPA\r\nCAPA\r\nRETR 1\r\nRETR 2\r\nRETR 3\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));

        first.Dispose();
        second.Dispose();
        third.Dispose();
        stream.Dispose();
    }

    static BatchSequenceStream CreateTransactionStream(
        string transactionCapabilities,
        string batchResponses)
    {
        return CreateTransactionStream(
            transactionCapabilities,
            batchResponses,
            "");
    }

    static BatchSequenceStream CreateTransactionStream(
        string transactionCapabilities,
        string firstOperationResponses,
        string batchResponses)
    {
        return new BatchSequenceStream(new[]
        {
            "+OK ready\r\n",
            "+OK authorization capabilities\r\nUSER\r\n.\r\n",
            "+OK transaction capabilities\r\n" + transactionCapabilities + "\r\n.\r\n",
            firstOperationResponses,
            batchResponses
        });
    }

    static Pop3ClientEngine ConnectTransaction(
        BatchSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    static Pop3ClientOptions CreateOptions(
        bool enablePipelining,
        bool forcePipelining)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.MaximumMessageLineOctets = 1024;
        options.MessageMemoryCeilingBytes = 1024;
        options.LeaveStreamOpen = true;
        options.EnablePipelining = enablePipelining;
        options.ForcePipelining = forcePipelining;
        return options;
    }

    class BatchSequenceStream : Stream
    {
        byte[][] Groups;
        int GroupIndex;
        int GroupPosition;
        MemoryStream Written = new MemoryStream();
        public int CaptureGroupIndex = -1;
        public string? WrittenAtCapturedRead;

        public BatchSequenceStream(string[] groups)
        {
            Groups = groups.Select(Encoding.ASCII.GetBytes).ToArray();
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
            get { return Groups.Sum(group => (long)group.Length); }
        }

        public override long Position
        {
            get { return 0; }
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
            while (GroupIndex < Groups.Length &&
                GroupPosition == Groups[GroupIndex].Length)
            {
                GroupIndex++;
                GroupPosition = 0;
            }
            if (GroupIndex == Groups.Length)
            {
                return 0;
            }
            if (GroupIndex == CaptureGroupIndex && WrittenAtCapturedRead == null)
            {
                WrittenAtCapturedRead = WrittenText();
            }

            int remaining = Groups[GroupIndex].Length - GroupPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(
                Groups[GroupIndex],
                GroupPosition,
                buffer,
                offset,
                readCount);
            GroupPosition += readCount;
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
