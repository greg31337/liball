using System.Text;

namespace pop3.Tests;

public class Pop3ClientTopTests
{
    [Test]
    public void TopZeroPreservesAllHeadersAndTheHeaderBodySeparator()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK headers follow\r\n" +
            "From: sender@example.test\r\n" +
            "Subject: example\r\n" +
            "\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        byte[] result = engine.RetrieveTopToMemory(3, 0, CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo(
            "From: sender@example.test\r\n" +
            "Subject: example\r\n" +
            "\r\n"));
        Pop3Response response = engine.GetSnapshot().LastResponse!;
        Assert.That(response.IsMultiline, Is.True);
        Assert.That(response.AdvisoryOctetCount, Is.EqualTo(-1));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nTOP 3 0\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void PositiveTopStreamsTheExactServerReturnedRepresentation()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK top follows\r\n" +
            "Header: value\r\n" +
            "\r\n" +
            "first\r\n" +
            "..second\r\n" +
            ".\r\n");
        RecordingObserver observer = new RecordingObserver();
        Pop3ClientOptions options = CreateOptions(false);
        options.TransferObserver = observer;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
        TrackingMemoryStream destination = new TrackingMemoryStream();

        long count = engine.RetrieveTopToStream(
            5,
            1,
            destination,
            CancellationToken.None);

        string expected = "Header: value\r\n\r\nfirst\r\n.second\r\n";
        Assert.That(count, Is.EqualTo(Encoding.ASCII.GetByteCount(expected)));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo(expected));
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(observer.Reports, Has.Count.EqualTo(5));
        Assert.That(observer.Reports, Is.All.Matches<Pop3TransferProgress>(progress =>
            progress.Kind == Pop3TransferKind.Top &&
            progress.MessageNumber == 5 &&
            progress.ExpectedBytes == -1));
        Assert.That(observer.Reports[4].BytesTransferred, Is.EqualTo(count));
        Assert.That(observer.Reports[4].IsComplete, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nTOP 5 1\r\n"));

        destination.WriteByte(1);
        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void TopStreamsALargeResultBeyondTheMemoryCeilingAndIgnoresAdvisorySizeText()
    {
        string headers = BuildRepeatedHeaders(4096);
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK 1 octet\r\n" +
            headers +
            "\r\n" +
            ".\r\n");
        Pop3ClientOptions options = CreateOptions(false);
        options.MessageMemoryCeilingBytes = 1;
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
        TrackingMemoryStream destination = new TrackingMemoryStream();

        long count = engine.RetrieveTopToStream(
            1,
            0,
            destination,
            CancellationToken.None);

        byte[] expected = Encoding.ASCII.GetBytes(headers + "\r\n");
        Assert.That(count, Is.EqualTo(expected.LongLength));
        Assert.That(destination.ToArray(), Is.EqualTo(expected));
        Assert.That(count, Is.GreaterThan(100000));
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().LastResponse!.AdvisoryOctetCount, Is.EqualTo(-1));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void TopToMemoryHonorsTheActualBoundedResultSize()
    {
        TopSequenceStream exactStream = CreateAdvertisedStream(
            "+OK follows\r\nH: v\r\n\r\n.\r\n");
        Pop3ClientOptions exactOptions = CreateOptions(false);
        exactOptions.MessageMemoryCeilingBytes = 8;
        Pop3ClientEngine exactEngine = CreateTransactionEngine(exactStream, exactOptions);

        byte[] exact = exactEngine.RetrieveTopToMemory(1, 0, CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(exact), Is.EqualTo("H: v\r\n\r\n"));
        exactEngine.CloseConnection();
        exactStream.Dispose();

        TopSequenceStream overflowStream = CreateAdvertisedStream(
            "+OK follows\r\nH: v\r\n\r\nx\r\n.\r\n");
        Pop3ClientOptions overflowOptions = CreateOptions(false);
        overflowOptions.MessageMemoryCeilingBytes = 8;
        Pop3ClientEngine overflowEngine = CreateTransactionEngine(
            overflowStream,
            overflowOptions);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            overflowEngine.RetrieveTopToMemory(1, 1, CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("TOP data"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(overflowEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(overflowStream.IsDisposed, Is.False);
        overflowStream.Dispose();
    }

    [Test]
    public void NegativeTopLeavesTheDestinationAndSessionAvailableForRetry()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "-ERR no such message\r\n" +
            "+OK follows\r\nH: v\r\n\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        TrackingMemoryStream destination = new TrackingMemoryStream();

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.RetrieveTopToStream(4, 0, destination, CancellationToken.None))!;
        byte[] retried = engine.RetrieveTopToMemory(4, 0, CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Top));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "4", "0" }));
        Assert.That(exception.Command.ExpectsMultilineResponse, Is.True);
        Assert.That(destination.Length, Is.Zero);
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(Encoding.ASCII.GetString(retried), Is.EqualTo("H: v\r\n\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void TopValidatesArgumentsBeforeWriting()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK follows\r\nH: v\r\n\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        string writesBefore = stream.WrittenText();
        MemoryStream readOnly = new MemoryStream(new byte[0], false);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveTopToStream(0, 0, new MemoryStream(), CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveTopToStream(1, -1, new MemoryStream(), CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() =>
            engine.RetrieveTopToStream(1, 0, null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.RetrieveTopToStream(1, 0, readOnly, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveTopToMemory(-1, 0, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.RetrieveTopToMemory(1, -1, CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        readOnly.Dispose();
        stream.Dispose();
    }

    [Test]
    public void TopIsRejectedLocallyWhenAdvertisedCapabilitiesExcludeIt()
    {
        TopSequenceStream stream = CreateUnadvertisedStream("");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(true));

        Assert.Throws<InvalidOperationException>(() =>
            engine.RetrieveTopToMemory(1, 0, CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() =>
            engine.RetrieveTopToStream(1, 0, new MemoryStream(), CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void TopProbeRequiresExplicitOptInWhenCapaIsUnavailable()
    {
        TopSequenceStream disabledStream = CreateUnavailableStream("");
        Pop3ClientEngine disabledEngine = CreateTransactionEngine(
            disabledStream,
            CreateOptions(false));

        Assert.Throws<InvalidOperationException>(() =>
            disabledEngine.RetrieveTopToMemory(1, 0, CancellationToken.None));
        Assert.That(disabledStream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        disabledEngine.CloseConnection();
        disabledStream.Dispose();

        TopSequenceStream enabledStream = CreateUnavailableStream(
            "+OK follows\r\nH: v\r\n\r\n.\r\n");
        Pop3ClientEngine enabledEngine = CreateTransactionEngine(
            enabledStream,
            CreateOptions(true));

        byte[] result = enabledEngine.RetrieveTopToMemory(1, 0, CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo("H: v\r\n\r\n"));
        Assert.That(enabledStream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nTOP 1 0\r\n"));
        enabledEngine.CloseConnection();
        enabledStream.Dispose();
    }

    [Test]
    public void PreCancelledTopDoesNotWriteOrTouchTheDestination()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK follows\r\nH: v\r\n\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        TrackingMemoryStream destination = new TrackingMemoryStream();
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.RetrieveTopToStream(1, 0, destination, source.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(destination.Length, Is.Zero);
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        destination.Dispose();
        stream.Dispose();
    }

    [Test]
    public void CancellationDuringTopClosesTheSessionButNotTheDestination()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK follows\r\n" +
            "Header: value\r\n" +
            "\r\n" +
            "body\r\n" +
            ".\r\n");
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            CancelingObserver observer = new CancelingObserver(source);
            Pop3ClientOptions options = CreateOptions(false);
            options.TransferObserver = observer;
            Pop3ClientEngine engine = CreateTransactionEngine(stream, options);
            TrackingMemoryStream destination = new TrackingMemoryStream();

            Assert.Throws<OperationCanceledException>(() =>
                engine.RetrieveTopToStream(1, 1, destination, source.Token));

            Assert.That(Encoding.ASCII.GetString(destination.ToArray()),
                Is.EqualTo("Header: value\r\n"));
            Assert.That(destination.IsDisposed, Is.False);
            Assert.That(observer.CallCount, Is.EqualTo(1));
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(stream.IsDisposed, Is.False);

            destination.Dispose();
        }
        stream.Dispose();
    }

    [Test]
    public void PrematureTopTerminationReportsTheTopOperationAndCloses()
    {
        TopSequenceStream stream = CreateAdvertisedStream(
            "+OK follows\r\nH: v\r\n\r\nbody\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        TrackingMemoryStream destination = new TrackingMemoryStream();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.RetrieveTopToStream(1, 1, destination, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Top));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(destination.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));

        destination.Dispose();
        stream.Dispose();
    }

    private static Pop3ClientEngine CreateTransactionEngine(
        TopSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    private static string BuildRepeatedHeaders(int lineCount)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < lineCount)
        {
            builder.Append("X-Long-Test: 0123456789abcdef0123456789\r\n");
            index++;
        }
        return builder.ToString();
    }

    private static TopSequenceStream CreateAdvertisedStream(string topResponses)
    {
        return new TopSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nTOP\r\n.\r\n" +
            "+OK capabilities\r\nTOP\r\n.\r\n" +
            topResponses);
    }

    private static TopSequenceStream CreateUnadvertisedStream(string topResponses)
    {
        return new TopSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\n.\r\n" +
            "+OK capabilities\r\n.\r\n" +
            topResponses);
    }

    private static TopSequenceStream CreateUnavailableStream(string topResponses)
    {
        return new TopSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n" +
            topResponses);
    }

    private static Pop3ClientOptions CreateOptions(bool probeTop)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.MaximumMessageLineOctets = 64;
        options.MessageMemoryCeilingBytes = 64;
        options.ProbeTopWhenCapaUnavailable = probeTop;
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

    private class TopSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

        public TopSequenceStream(string responses)
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
