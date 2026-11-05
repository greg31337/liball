using System.Text;

namespace pop3.Tests;

public class Pop3ClientDeleteTests
{
    [Test]
    public void SingleDeleteMarksKnownMessageAndRecomputesMailboxStatus()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            "2 200\r\n" +
            "3 300\r\n" +
            ".\r\n" +
            "+OK marked\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetAllMessageInfo(CancellationToken.None);

        engine.DeleteMessage(2, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 1).IsDeleted, Is.False);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 2).IsDeleted, Is.True);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 3).IsDeleted, Is.False);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(2));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(400));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("marked"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nLIST\r\nDELE 2\r\n"));

        Assert.Throws<InvalidOperationException>(() =>
            engine.DeleteMessage(2, CancellationToken.None));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nLIST\r\nDELE 2\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SuccessfulDeleteWithoutKnownMessageSizeInvalidatesStatus()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK 2 300\r\n" +
            "+OK marked\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetMailboxStatus(CancellationToken.None);

        engine.DeleteMessage(5, CancellationToken.None);

        Assert.That(engine.GetSnapshot().MailboxStatus, Is.Null);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void NegativeDeleteIsRetryableAndDoesNotCreateADeletionMark()
    {
        DeleteSequenceStream stream = CreateStream(
            "-ERR no such message\r\n" +
            "+OK marked\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.DeleteMessage(4, CancellationToken.None))!;
        engine.DeleteMessage(4, CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Dele));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "4" }));
        Assert.That(exception.Response.Text, Is.EqualTo("no such message"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nDELE 4\r\nDELE 4\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SelectedBatchIsFullyValidatedBeforeTheFirstDeleteWrite()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            "2 200\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetAllMessageInfo(CancellationToken.None);
        string writesBefore = stream.WrittenText();

        Assert.Throws<ArgumentNullException>(() =>
            engine.DeleteMessages(null!, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.DeleteMessages(new[] { 1, 0 }, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.DeleteMessages(new[] { 1, 1 }, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.DeleteMessages(new[] { 1, 3 }, CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().Messages, Is.All.Matches<Pop3MessageInfo>(message => !message.IsDeleted));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SelectedBatchReportsTheExactFailingCommandAfterPriorSuccesses()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            "2 200\r\n" +
            "3 300\r\n" +
            ".\r\n" +
            "+OK first marked\r\n" +
            "-ERR second rejected\r\n" +
            "+OK third should remain unread\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetAllMessageInfo(CancellationToken.None);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.DeleteMessages(new[] { 1, 2, 3 }, CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Dele));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "2" }));
        Assert.That(exception.Response.Text, Is.EqualTo("second rejected"));
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 1).IsDeleted, Is.True);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 2).IsDeleted, Is.False);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 3).IsDeleted, Is.False);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(2));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(500));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\nDELE 2\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SelectedBatchDeletesInCallerOrderAndAllowsAnEmptyBatch()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            "2 200\r\n" +
            "3 300\r\n" +
            ".\r\n" +
            "+OK third marked\r\n" +
            "+OK first marked\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetAllMessageInfo(CancellationToken.None);

        engine.DeleteMessages(new int[0], CancellationToken.None);
        engine.DeleteMessages(new[] { 3, 1 }, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 1).IsDeleted, Is.True);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 2).IsDeleted, Is.False);
        Assert.That(snapshot.Messages.Single(message => message.MessageNumber == 3).IsDeleted, Is.True);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(1));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(200));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 3\r\nDELE 1\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void DeleteAllListsThenMarksEveryVisibleMessage()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK listing\r\n" +
            "2 200\r\n" +
            "4 400\r\n" +
            ".\r\n" +
            "+OK second marked\r\n" +
            "+OK fourth marked\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        engine.DeleteAllMessages(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.Messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 2, 4 }));
        Assert.That(snapshot.Messages, Is.All.Matches<Pop3MessageInfo>(message => message.IsDeleted));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.Zero);
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 2\r\nDELE 4\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void DeleteAllHandlesAnEmptyMaildropWithoutDeleCommands()
    {
        DeleteSequenceStream stream = CreateStream(
            "+OK empty\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        engine.DeleteAllMessages(CancellationToken.None);

        Assert.That(engine.GetSnapshot().MailboxStatus!.MessageCount, Is.Zero);
        Assert.That(engine.GetSnapshot().Messages, Is.Empty);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nLIST\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void PreCancelledBatchDoesNotWriteAnyDeleteCommands()
    {
        DeleteSequenceStream stream = CreateStream("");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.DeleteMessages(new[] { 1, 2 }, source.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    private static Pop3ClientEngine CreateTransactionEngine(DeleteSequenceStream stream)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(), CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    private static DeleteSequenceStream CreateStream(string responses)
    {
        return new DeleteSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n" +
            responses);
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

    private class DeleteSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public DeleteSequenceStream(string responses)
        {
            Responses = Encoding.ASCII.GetBytes(responses);
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
