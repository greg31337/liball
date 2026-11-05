using System.Text;

namespace pop3.Tests;

public class Pop3ClientSessionCompletionTests
{
    [Test]
    public void ResetClearsDeletionMarksAndRefreshesRestoredStatus()
    {
        SessionSequenceStream stream = CreateTransactionStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            "2 200\r\n" +
            ".\r\n" +
            "+OK marked\r\n" +
            "+OK reset\r\n" +
            "+OK 2 300\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.DeleteMessage(2, CancellationToken.None);

        Pop3MailboxStatus status = engine.ResetDeletions(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(300));
        Assert.That(snapshot.Messages, Is.All.Matches<Pop3MessageInfo>(message => !message.IsDeleted));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(2));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(300));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("2 300"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 2\r\nRSET\r\nSTAT\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void RejectedResetPreservesDeletionMarksAndTransactionState()
    {
        SessionSequenceStream stream = CreateTransactionStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            ".\r\n" +
            "+OK marked\r\n" +
            "-ERR reset rejected\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.DeleteMessage(1, CancellationToken.None);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.ResetDeletions(CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Rset));
        Assert.That(exception.Response.Text, Is.EqualTo("reset rejected"));
        Assert.That(snapshot.Messages[0].IsDeleted, Is.True);
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.Zero);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\nRSET\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void AcceptedResetRemainsAppliedWhenStatusRefreshIsRejected()
    {
        SessionSequenceStream stream = CreateTransactionStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            ".\r\n" +
            "+OK marked\r\n" +
            "+OK reset\r\n" +
            "-ERR status unavailable\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.DeleteMessage(1, CancellationToken.None);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.ResetDeletions(CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(exception.Response.Text, Is.EqualTo("status unavailable"));
        Assert.That(snapshot.Messages[0].IsDeleted, Is.False);
        Assert.That(snapshot.MailboxStatus, Is.Null);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nDELE 1\r\nRSET\r\nSTAT\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void NoopKeepsTransactionDataUnchanged()
    {
        SessionSequenceStream stream = CreateTransactionStream(
            "+OK 2 300\r\n" +
            "+OK still here\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);
        engine.GetMailboxStatus(CancellationToken.None);

        engine.NoOperation(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(2));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(300));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("still here"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nSTAT\r\nNOOP\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void RejectedNoopIsRetryable()
    {
        SessionSequenceStream stream = CreateTransactionStream(
            "-ERR temporarily unavailable\r\n" +
            "+OK accepted\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.NoOperation(CancellationToken.None))!;
        engine.NoOperation(CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Noop));
        Assert.That(exception.Response.Text, Is.EqualTo("temporarily unavailable"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nNOOP\r\nNOOP\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void QuitFromAuthorizationClosesWithoutEnteringUpdate()
    {
        SessionSequenceStream stream = CreateAuthorizationStream("+OK goodbye\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();
        Pop3SessionState stateDuringDispose = Pop3SessionState.Disconnected;
        stream.DisposedAction = () => stateDuringDispose = engine.GetSnapshot().State;
        engine.ConnectStream(stream, CreateOptions(false), CancellationToken.None);

        engine.QuitAndCommit(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(stateDuringDispose, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(snapshot.IsConnected, Is.False);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("goodbye"));
        Assert.That(stream.IsDisposed, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nQUIT\r\n"));
    }

    [Test]
    public void SuccessfulTransactionQuitEntersUpdateThenClosesOwnedTransport()
    {
        SessionSequenceStream stream = CreateTransactionStream("+OK committed\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, false);
        Pop3SessionState stateDuringDispose = Pop3SessionState.Disconnected;
        stream.DisposedAction = () => stateDuringDispose = engine.GetSnapshot().State;

        engine.QuitAndCommit(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(stateDuringDispose, Is.EqualTo(Pop3SessionState.Update));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(snapshot.IsConnected, Is.False);
        Assert.That(snapshot.IsAuthenticated, Is.False);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("committed"));
        Assert.That(stream.IsDisposed, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nQUIT\r\n"));
    }

    [Test]
    public void RejectedTransactionQuitClosesWithoutEnteringUpdate()
    {
        SessionSequenceStream stream = CreateTransactionStream("-ERR commit failed\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, false);
        Pop3SessionState stateDuringDispose = Pop3SessionState.Disconnected;
        stream.DisposedAction = () => stateDuringDispose = engine.GetSnapshot().State;

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.QuitAndCommit(CancellationToken.None))!;

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Quit));
        Assert.That(exception.Response.Text, Is.EqualTo("commit failed"));
        Assert.That(stateDuringDispose, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("commit failed"));
        Assert.That(stream.IsDisposed, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nQUIT\r\n"));
    }

    [Test]
    public void QuitEndOfStreamRollsBackAndReportsTransactionState()
    {
        SessionSequenceStream stream = CreateTransactionStream("");
        Pop3ClientEngine engine = ConnectTransaction(stream, false);
        Pop3SessionState stateDuringDispose = Pop3SessionState.Disconnected;
        stream.DisposedAction = () => stateDuringDispose = engine.GetSnapshot().State;

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.QuitAndCommit(CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Quit));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(stateDuringDispose, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nQUIT\r\n"));
    }

    [Test]
    public void PreCancelledQuitDoesNotBeginCommitOrCloseTheSession()
    {
        SessionSequenceStream stream = CreateTransactionStream("+OK unused\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.QuitAndCommit(source.Token));
        }

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void CancellationAfterQuitIsWrittenDoesNotUndoCommitIntent()
    {
        SessionSequenceStream stream = CreateTransactionStream("+OK committed\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, false);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            stream.WrittenAction = () => source.Cancel();

            engine.QuitAndCommit(source.Token);
        }

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("committed"));
        Assert.That(stream.IsDisposed, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nQUIT\r\n"));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void AbortRollsBackWithoutQuitAndHonorsStreamOwnership(
        bool leaveStreamOpen,
        bool expectedDisposed)
    {
        SessionSequenceStream stream = CreateTransactionStream("");
        Pop3ClientEngine engine = ConnectTransaction(stream, leaveStreamOpen);
        string writesBeforeAbort = stream.WrittenText();

        engine.Abort();

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().IsConnected, Is.False);
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        Assert.That(stream.WrittenText(), Is.EqualTo(writesBeforeAbort));
        Assert.That(stream.WrittenText(), Does.Not.Contain("QUIT"));
        stream.Dispose();
    }

    [Test]
    public void AbortWithDeletionMarksClosesInTransactionWithoutCommit()
    {
        SessionSequenceStream stream = CreateTransactionStream(
            "+OK listing\r\n" +
            "1 100\r\n" +
            ".\r\n" +
            "+OK marked\r\n");
        Pop3ClientEngine engine = ConnectTransaction(stream, false);
        Pop3SessionState stateDuringDispose = Pop3SessionState.Disconnected;
        stream.DisposedAction = () => stateDuringDispose = engine.GetSnapshot().State;
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.DeleteMessage(1, CancellationToken.None);
        Assert.That(engine.GetSnapshot().Messages[0].IsDeleted, Is.True);
        string writesBeforeAbort = stream.WrittenText();

        engine.Abort();

        Assert.That(stateDuringDispose, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(writesBeforeAbort));
        Assert.That(stream.WrittenText(), Does.Not.Contain("QUIT"));
    }

    [Test]
    public void TransactionOnlyCommandsAreRejectedBeforeWritingInAuthorization()
    {
        SessionSequenceStream stream = CreateAuthorizationStream("");
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            engine.DeleteMessage(1, CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.DeleteMessages(new[] { 1 }, CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.DeleteAllMessages(CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.ResetDeletions(CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.NoOperation(CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void ClosedSessionRejectsCompletionCommandsBeforeWriting()
    {
        SessionSequenceStream stream = CreateTransactionStream("");
        Pop3ClientEngine engine = ConnectTransaction(stream, true);
        engine.Abort();
        string writesBefore = stream.WrittenText();

        Assert.Throws<Pop3StateException>(() =>
            engine.DeleteMessage(1, CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.DeleteMessages(new[] { 1 }, CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.DeleteAllMessages(CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.ResetDeletions(CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.NoOperation(CancellationToken.None));
        Assert.Throws<Pop3StateException>(() =>
            engine.QuitAndCommit(CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    static Pop3ClientEngine ConnectTransaction(
        SessionSequenceStream stream,
        bool leaveStreamOpen)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(leaveStreamOpen), CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    static SessionSequenceStream CreateAuthorizationStream(string responses)
    {
        return new SessionSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            responses);
    }

    static SessionSequenceStream CreateTransactionStream(string responses)
    {
        return new SessionSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n" +
            responses);
    }

    static Pop3ClientOptions CreateOptions(bool leaveStreamOpen)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.LeaveStreamOpen = leaveStreamOpen;
        return options;
    }

    class SessionSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;
        public Action? DisposedAction;
        public Action? WrittenAction;

        public SessionSequenceStream(string responses)
        {
            Responses = Encoding.ASCII.GetBytes(responses);
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
            if (WrittenAction != null)
            {
                WrittenAction();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                if (DisposedAction != null)
                {
                    DisposedAction();
                }
            }
            base.Dispose(disposing);
        }
    }
}
