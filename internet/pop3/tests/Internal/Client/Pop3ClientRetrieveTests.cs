using System.Text;

namespace pop3.Tests;

public class Pop3ClientRetrieveTests
{
    [Test]
    public void RetrieveStreamsExactBinaryOctetsAndReportsAdvisoryProgress()
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            StandardPrefix() + "+OK 999 octets\r\n");
        byte[] body = new byte[]
        {
            0, 255, 13, 10,
            46, 46, 128, 13, 10,
            46, 13, 10
        };
        RetrieveSequenceStream stream = new RetrieveSequenceStream(
            prefix.Concat(body).ToArray());
        RecordingObserver observer = new RecordingObserver();
        Pop3ClientOptions options = CreateOptions();
        options.TransferObserver = observer;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
        TrackingMemoryStream destination = new TrackingMemoryStream();

        long count = engine.RetrieveMessageToStream(
            7,
            destination,
            CancellationToken.None);

        byte[] expected = new byte[]
        {
            0, 255, 13, 10,
            46, 128, 13, 10
        };
        Assert.That(count, Is.EqualTo(expected.Length));
        Assert.That(destination.ToArray(), Is.EqualTo(expected));
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(observer.Reports, Has.Count.EqualTo(3));
        AssertProgress(observer.Reports[0], 7, 4, 999, false);
        AssertProgress(observer.Reports[1], 7, 8, 999, false);
        AssertProgress(observer.Reports[2], 7, 8, 999, true);
        Pop3Response response = engine.GetSnapshot().LastResponse!;
        Assert.That(response.AdvisoryOctetCount, Is.EqualTo(999));
        Assert.That(response.IsMultiline, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nRETR 7\r\n"));

        destination.WriteByte(1);
        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void RetrieveToMemoryDoesNotTrustLargeOrMalformedAdvisorySizes()
    {
        RetrieveSequenceStream stream = CreateStream(
            "+OK [X-SIZE] 9999 octets\r\na\r\n.\r\n" +
            "+OK 9223372036854775808 octets\r\nb\r\n.\r\n");
        RecordingObserver observer = new RecordingObserver();
        Pop3ClientOptions options = CreateOptions();
        options.MessageMemoryCeilingBytes = 3;
        options.TransferObserver = observer;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);

        byte[] first = engine.RetrieveMessageToMemory(1, CancellationToken.None);
        byte[] second = engine.RetrieveMessageToMemory(2, CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(first), Is.EqualTo("a\r\n"));
        Assert.That(Encoding.ASCII.GetString(second), Is.EqualTo("b\r\n"));
        Assert.That(observer.Reports, Has.Count.EqualTo(4));
        AssertProgress(observer.Reports[0], 1, 3, 9999, false);
        AssertProgress(observer.Reports[1], 1, 3, 9999, true);
        AssertProgress(observer.Reports[2], 2, 3, -1, false);
        AssertProgress(observer.Reports[3], 2, 3, -1, true);
        Assert.That(engine.GetSnapshot().LastResponse!.AdvisoryOctetCount, Is.EqualTo(-1));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void RetrieveToMemoryAcceptsTheExactConfiguredCeiling()
    {
        RetrieveSequenceStream stream = CreateStream(
            "+OK message follows\r\nabc\r\n.\r\n");
        Pop3ClientOptions options = CreateOptions();
        options.MessageMemoryCeilingBytes = 5;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);

        byte[] message = engine.RetrieveMessageToMemory(1, CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(message), Is.EqualTo("abc\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void RetrieveStreamsALargeMessageBeyondTheMemoryCeiling()
    {
        string body = BuildRepeatedLines(4096);
        RetrieveSequenceStream stream = CreateStream(
            "+OK 1 octet\r\n" +
            body +
            ".\r\n");
        Pop3ClientOptions options = CreateOptions();
        options.MessageMemoryCeilingBytes = 1;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
        TrackingMemoryStream destination = new TrackingMemoryStream();

        long count = engine.RetrieveMessageToStream(
            1,
            destination,
            CancellationToken.None);

        byte[] expected = Encoding.ASCII.GetBytes(body);
        Assert.That(count, Is.EqualTo(expected.LongLength));
        Assert.That(destination.ToArray(), Is.EqualTo(expected));
        Assert.That(count, Is.GreaterThan(100000));
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().LastResponse!.AdvisoryOctetCount, Is.EqualTo(1));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void RetrieveToMemoryClosesWhenActualDataExceedsTheCeiling()
    {
        RetrieveSequenceStream stream = CreateStream(
            "+OK 1 octet\r\nabcd\r\n.\r\n");
        Pop3ClientOptions options = CreateOptions();
        options.MessageMemoryCeilingBytes = 5;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.RetrieveMessageToMemory(1, CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("RETR data"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void NegativeRetrieveLeavesDestinationAndSessionAvailableForRetry()
    {
        RetrieveSequenceStream stream = CreateStream(
            "-ERR no such message\r\n" +
            "+OK 3 octets\r\nx\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions());
        TrackingMemoryStream destination = new TrackingMemoryStream();

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.RetrieveMessageToStream(4, destination, CancellationToken.None))!;
        byte[] retried = engine.RetrieveMessageToMemory(4, CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Retr));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "4" }));
        Assert.That(exception.Command.ExpectsMultilineResponse, Is.True);
        Assert.That(exception.Response.Text, Is.EqualTo("no such message"));
        Assert.That(destination.Length, Is.Zero);
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(Encoding.ASCII.GetString(retried), Is.EqualTo("x\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void RetrieveValidatesArgumentsAndLimitsBeforeWriting()
    {
        RetrieveSequenceStream stream = CreateStream("+OK follows\r\n.\r\n");
        Pop3ClientOptions options = CreateOptions();
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
        string writesBefore = stream.WrittenText();
        MemoryStream readOnly = new MemoryStream(new byte[0], false);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveMessageToStream(0, new MemoryStream(), CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() =>
            engine.RetrieveMessageToStream(1, null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.RetrieveMessageToStream(1, readOnly, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveMessageToMemory(-1, CancellationToken.None));

        options.MaximumMessageLineOctets = 2;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveMessageToStream(1, new MemoryStream(), CancellationToken.None));
        options.MaximumMessageLineOctets = 64;
        options.MessageMemoryCeilingBytes = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveMessageToMemory(1, CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        readOnly.Dispose();
        stream.Dispose();
    }

    [Test]
    public void CancellationDuringRetrieveClosesTheSessionButNotTheDestination()
    {
        RetrieveSequenceStream stream = CreateStream(
            "+OK 100 octets\r\nfirst\r\nsecond\r\n.\r\n");
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            CancelingObserver observer = new CancelingObserver(source);
            Pop3ClientOptions options = CreateOptions();
            options.TransferObserver = observer;
            Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
            TrackingMemoryStream destination = new TrackingMemoryStream();

            Assert.Throws<OperationCanceledException>(() =>
                engine.RetrieveMessageToStream(3, destination, source.Token));

            Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("first\r\n"));
            Assert.That(destination.IsDisposed, Is.False);
            Assert.That(observer.CallCount, Is.EqualTo(1));
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(stream.IsDisposed, Is.False);

            destination.Dispose();
        }
        stream.Dispose();
    }

    [Test]
    public void PrematureEndOfMessageIsAConnectionFailure()
    {
        RetrieveSequenceStream stream = CreateStream(
            "+OK message follows\r\npartial\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions());
        TrackingMemoryStream destination = new TrackingMemoryStream();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.RetrieveMessageToStream(1, destination, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Retr));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(exception.IsTimeout, Is.False);
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("partial\r\n"));
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));

        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void PreCancelledRetrieveDoesNotWriteOrTouchTheDestination()
    {
        RetrieveSequenceStream stream = CreateStream("+OK follows\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions());
        TrackingMemoryStream destination = new TrackingMemoryStream();
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.RetrieveMessageToStream(1, destination, source.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(destination.Length, Is.Zero);
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    private static void AssertProgress(
        Pop3TransferProgress progress,
        int messageNumber,
        long bytesTransferred,
        long expectedBytes,
        bool complete)
    {
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.Retrieve));
        Assert.That(progress.MessageNumber, Is.EqualTo(messageNumber));
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytesTransferred));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(expectedBytes));
        Assert.That(progress.IsComplete, Is.EqualTo(complete));
    }

    private static string BuildRepeatedLines(int lineCount)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < lineCount)
        {
            builder.Append("0123456789abcdef0123456789abcdef\r\n");
            index++;
        }
        return builder.ToString();
    }

    private static Pop3ClientEngine CreateTransactionEngine(
        RetrieveSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    private static RetrieveSequenceStream CreateStream(string retrieveResponses)
    {
        return new RetrieveSequenceStream(
            Encoding.ASCII.GetBytes(StandardPrefix() + retrieveResponses));
    }

    private static string StandardPrefix()
    {
        return "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n";
    }

    private static Pop3ClientOptions CreateOptions()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.MaximumMessageLineOctets = 64;
        options.MessageMemoryCeilingBytes = 64;
        options.LeaveStreamOpen = true;
        return options;
    }

    private class RecordingObserver : IPop3TransferObserver
    {
        public List<Pop3TransferProgress> Reports = new List<Pop3TransferProgress>();

        public void Report(Pop3TransferProgress progress)
        {
            Reports.Add(progress);
        }
    }

    private class CancelingObserver : IPop3TransferObserver
    {
        CancellationTokenSource Source;
        public int CallCount;

        public CancelingObserver(CancellationTokenSource source)
        {
            Source = source;
        }

        public void Report(Pop3TransferProgress progress)
        {
            CallCount++;
            Source.Cancel();
        }
    }

    private class TrackingMemoryStream : MemoryStream
    {
        public bool IsDisposed;

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private class RetrieveSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

        public RetrieveSequenceStream(byte[] responses)
        {
            Responses = responses;
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
