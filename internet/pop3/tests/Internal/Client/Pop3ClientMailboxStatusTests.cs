using System.Text;

namespace pop3.Tests;

public class Pop3ClientMailboxStatusTests
{
    [Test]
    public void StatRefreshesPopulatedAndEmptyMailboxSnapshots()
    {
        MailboxSequenceStream stream = CreateStream(
            "+OK 2 320 messages ready\r\n" +
            "+OK [X-SNAPSHOT] 0 0 empty\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3MailboxStatus populated = engine.GetMailboxStatus(CancellationToken.None);
        populated.MessageCount = 99;
        populated.TotalOctets = 99;
        Pop3ClientSnapshot populatedSnapshot = engine.GetSnapshot();

        Assert.That(populatedSnapshot.MailboxStatus, Is.Not.Null);
        Assert.That(populatedSnapshot.MailboxStatus!.MessageCount, Is.EqualTo(2));
        Assert.That(populatedSnapshot.MailboxStatus.TotalOctets, Is.EqualTo(320));
        Assert.That(populatedSnapshot.LastResponse!.Text, Is.EqualTo("2 320 messages ready"));

        Pop3MailboxStatus empty = engine.GetMailboxStatus(CancellationToken.None);
        Pop3ClientSnapshot emptySnapshot = engine.GetSnapshot();

        Assert.That(empty.MessageCount, Is.Zero);
        Assert.That(empty.TotalOctets, Is.Zero);
        Assert.That(emptySnapshot.MailboxStatus, Is.Not.Null);
        Assert.That(emptySnapshot.MailboxStatus!.MessageCount, Is.Zero);
        Assert.That(emptySnapshot.MailboxStatus.TotalOctets, Is.Zero);
        Assert.That(emptySnapshot.LastResponse!.ResponseCodeLevels, Is.EqualTo(new[] { "X-SNAPSHOT" }));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nSTAT\r\nSTAT\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void StatAcceptsMaximumSignedCountAndSize()
    {
        MailboxSequenceStream stream = CreateStream(
            "+OK 2147483647 9223372036854775807\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3MailboxStatus status = engine.GetMailboxStatus(CancellationToken.None);

        Assert.That(status.MessageCount, Is.EqualTo(int.MaxValue));
        Assert.That(status.TotalOctets, Is.EqualTo(long.MaxValue));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase("+OK 2147483648 0\r\n")]
    [TestCase("+OK 1 9223372036854775808\r\n")]
    [TestCase("+OK 0 1\r\n")]
    [TestCase("+OK 1\r\n")]
    public void StatRejectsOverflowMalformedAndContradictoryPayloads(string response)
    {
        MailboxSequenceStream stream = CreateStream(response);
        Pop3ClientEngine engine = CreateTransactionEngine(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetMailboxStatus(CancellationToken.None))!;

        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(exception.Context, Is.EqualTo("STAT response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().MailboxStatus, Is.Null);
        Assert.That(stream.IsDisposed, Is.False);

        stream.Dispose();
    }

    [Test]
    public void NegativeStatPreservesThePreviousSnapshotForRetry()
    {
        MailboxSequenceStream stream = CreateStream(
            "+OK 3 600\r\n" +
            "-ERR [SYS/TEMP] mailbox unavailable\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream);
        engine.GetMailboxStatus(CancellationToken.None);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetMailboxStatus(CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(exception.Command.Arguments, Is.Empty);
        Assert.That(exception.Response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(exception.Response.Text, Is.EqualTo("[SYS/TEMP] mailbox unavailable"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(3));
        Assert.That(snapshot.MailboxStatus.TotalOctets, Is.EqualTo(600));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("[SYS/TEMP] mailbox unavailable"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void StatRequiresTransactionStateBeforeCancellationOrNetworkWrites()
    {
        MailboxSequenceStream authorizationStream = CreateStream("");
        Pop3ClientEngine authorizationEngine = new Pop3ClientEngine();
        authorizationEngine.ConnectStream(
            authorizationStream,
            CreateOptions(),
            CancellationToken.None);

        Pop3StateException stateException = Assert.Throws<Pop3StateException>(() =>
            authorizationEngine.GetMailboxStatus(CancellationToken.None))!;

        Assert.That(stateException.Operation, Is.EqualTo(Pop3Commands.Stat));
        Assert.That(authorizationStream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        authorizationEngine.CloseConnection();
        authorizationStream.Dispose();

        MailboxSequenceStream cancellationStream = CreateStream("+OK 1 1\r\n");
        Pop3ClientEngine cancellationEngine = CreateTransactionEngine(cancellationStream);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                cancellationEngine.GetMailboxStatus(source.Token));
        }

        Assert.That(cancellationStream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(cancellationEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        cancellationEngine.CloseConnection();
        cancellationStream.Dispose();
    }

    private static Pop3ClientEngine CreateTransactionEngine(MailboxSequenceStream stream)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(), CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    private static MailboxSequenceStream CreateStream(string statResponses)
    {
        return new MailboxSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n" +
            statResponses);
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

    private class MailboxSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

        public MailboxSequenceStream(string responses)
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
