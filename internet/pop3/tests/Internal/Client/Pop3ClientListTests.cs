using System.Text;

namespace pop3.Tests;

public class Pop3ClientListTests
{
    [Test]
    public void SingleListValidatesEchoedNumberAndRefreshesKnownMessages()
    {
        ListSequenceStream stream = CreateStream(
            "+OK [X-LIST] 3 300 third\r\n" +
            "+OK 1 100 first\r\n" +
            "+OK 3 350 refreshed\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3MessageInfo third = engine.GetMessageInfo(3, CancellationToken.None);
        Pop3MessageInfo first = engine.GetMessageInfo(1, CancellationToken.None);
        Pop3MessageInfo refreshed = engine.GetMessageInfo(3, CancellationToken.None);
        third.OctetCount = 999;
        first.OctetCount = 999;
        refreshed.OctetCount = 999;
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(snapshot.Messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(snapshot.Messages.Select(message => message.OctetCount), Is.EqualTo(new long[] { 100, 350 }));
        Assert.That(snapshot.Messages, Is.All.Matches<Pop3MessageInfo>(message => !message.IsDeleted));
        Assert.That(snapshot.Messages, Is.All.Matches<Pop3MessageInfo>(message => !message.IsUtf8Representation));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("3 350 refreshed"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST 3\r\nLIST 1\r\nLIST 3\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void SingleListRejectsNonPositiveNumbersBeforeWriting(int messageNumber)
    {
        ListSequenceStream stream = CreateStream("+OK 1 10\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.GetMessageInfo(messageNumber, CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SingleListTreatsMissingOrDeletedMessageAsRetryableCommandFailure()
    {
        ListSequenceStream stream = CreateStream(
            "-ERR [SYS/TEMP] no such message\r\n" +
            "+OK 2 250 now available\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetMessageInfo(2, CancellationToken.None))!;
        Pop3MessageInfo retried = engine.GetMessageInfo(2, CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.List));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "2" }));
        Assert.That(exception.Response.Text, Is.EqualTo("[SYS/TEMP] no such message"));
        Assert.That(retried.MessageNumber, Is.EqualTo(2));
        Assert.That(retried.OctetCount, Is.EqualTo(250));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SingleListRejectsAResponseForADifferentMessage()
    {
        ListSequenceStream stream = CreateStream("+OK 3 250 wrong message\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetMessageInfo(2, CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("LIST response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().Messages, Is.Empty);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [TestCase("+OK 0 25\r\n")]
    [TestCase("+OK 2 -1\r\n")]
    [TestCase("+OK 2 9223372036854775808\r\n")]
    [TestCase("+OK 2  25\r\n")]
    public void SingleListRejectsMalformedNumericData(string response)
    {
        ListSequenceStream stream = CreateStream(response);
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetMessageInfo(2, CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("LIST response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().Messages, Is.Empty);
        stream.Dispose();
    }

    [Test]
    public void MultilineListPreservesServerOrderAndChecksKnownStatSnapshot()
    {
        ListSequenceStream stream = CreateStream(
            "+OK 3 600\r\n" +
            "+OK listing follows\r\n" +
            "3 300\r\n" +
            "1 100\r\n" +
            "5 200\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetMailboxStatus(CancellationToken.None);

        Pop3MessageInfo[] messages = engine.GetAllMessageInfo(CancellationToken.None);
        messages[0].OctetCount = 999;
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(snapshot.Messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 3, 1, 5 }));
        Assert.That(snapshot.Messages.Select(message => message.OctetCount), Is.EqualTo(new long[] { 300, 100, 200 }));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(3));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(600));
        Assert.That(snapshot.LastResponse!.IsMultiline, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nSTAT\r\nLIST\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MultilineListDerivesStatusWhenStatHasNotRun()
    {
        ListSequenceStream stream = CreateStream(
            "+OK listing follows\r\n" +
            "2 20\r\n" +
            "4 40\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3MessageInfo[] messages = engine.GetAllMessageInfo(CancellationToken.None);
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 2, 4 }));
        Assert.That(snapshot.MailboxStatus, Is.Not.Null);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(2));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(60));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MultilineListAcceptsAnEmptyMaildrop()
    {
        ListSequenceStream stream = CreateStream("+OK empty\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3MessageInfo[] messages = engine.GetAllMessageInfo(CancellationToken.None);
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(messages, Is.Empty);
        Assert.That(snapshot.Messages, Is.Empty);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.Zero);
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.Zero);

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase("+OK 2 300\r\n", "1 100\r\n.\r\n")]
    [TestCase("+OK 2 301\r\n", "1 100\r\n2 200\r\n.\r\n")]
    [TestCase("+OK 2 300\r\n", "1 100\r\n1 200\r\n.\r\n")]
    public void MultilineListRejectsDataContradictingKnownStat(
        string statResponse,
        string listData)
    {
        ListSequenceStream stream = CreateStream(
            statResponse +
            "+OK listing follows\r\n" +
            listData);
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetMailboxStatus(CancellationToken.None);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetAllMessageInfo(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("LIST response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().Messages, Is.Empty);
        stream.Dispose();
    }

    [Test]
    public void MultilineListRejectsAnOverflowingBatchTotal()
    {
        ListSequenceStream stream = CreateStream(
            "+OK listing follows\r\n" +
            "1 9223372036854775807\r\n" +
            "2 1\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetAllMessageInfo(CancellationToken.None));

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [TestCase("0 100\r\n")]
    [TestCase("1 -1\r\n")]
    [TestCase("1  100\r\n")]
    [TestCase("1 100\u0001\r\n")]
    public void MultilineListRejectsMalformedEntryData(string listEntry)
    {
        ListSequenceStream stream = CreateStream(
            "+OK listing follows\r\n" +
            listEntry +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetAllMessageInfo(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("LIST response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().Messages, Is.Empty);
        stream.Dispose();
    }

    [Test]
    public void MultilineListRefreshExcludesDeletedMessagesWithoutRenumbering()
    {
        ListSequenceStream stream = CreateStream(
            "+OK initial listing\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            ".\r\n" +
            "+OK marked\r\n" +
            "+OK refreshed listing\r\n" +
            "2 20\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.DeleteMessage(1, CancellationToken.None);

        Pop3MessageInfo[] messages = engine.GetAllMessageInfo(CancellationToken.None);
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 2 }));
        Assert.That(snapshot.Messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 2 }));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(1));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(20));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\nLIST\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MultilineListNegativeResponseDoesNotConsumeFollowingData()
    {
        ListSequenceStream stream = CreateStream(
            "-ERR listing unavailable\r\n" +
            "+OK 1 25 single result\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetAllMessageInfo(CancellationToken.None))!;
        Pop3MessageInfo message = engine.GetMessageInfo(1, CancellationToken.None);

        Assert.That(exception.Command.ExpectsMultilineResponse, Is.True);
        Assert.That(message.OctetCount, Is.EqualTo(25));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    private static Pop3ClientEngine CreateTransactionEngine(ListSequenceStream stream)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(), CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    private static ListSequenceStream CreateStream(string listResponses)
    {
        return new ListSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n" +
            listResponses);
    }

    private static Pop3ClientOptions CreateOptions()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.LeaveStreamOpen = true;
        return options;
    }

    private class ListSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

        public ListSequenceStream(string responses)
        {
            Responses = Encoding.UTF8.GetBytes(responses);
        }

        public override bool CanRead
        {
            get { return !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !IsDisposed; }
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

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing)
            {
                Written.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
