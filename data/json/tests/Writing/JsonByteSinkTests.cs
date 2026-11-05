namespace json.Tests;

public class JsonByteSinkTests
{
    [Test]
    public void StreamSinkBuffersCopiesFlushesAndRetainsCallerOwnership()
    {
        TrackingMemoryStream stream = new TrackingMemoryStream();
        JsonWriterTarget target = new JsonWriterTarget();
        target.Stream = stream;
        JsonByteSink sink = new JsonByteSink(target, 10);
        target.Stream = null;
        byte[] callerBytes = { 1, 2 };

        sink.WriteBytes(callerBytes);
        callerBytes[0] = 9;
        sink.WriteByte(3);

        Assert.Multiple(() =>
        {
            Assert.That(sink.GetBytesPending(), Is.EqualTo(3));
            Assert.That(sink.GetBytesCommitted(), Is.Zero);
            Assert.That(stream.Length, Is.Zero);
        });

        JsonOperationResult flushed = sink.FlushSink();
        JsonOperationResult emptyFlush = sink.FlushSink();
        sink.WriteByte(4);
        sink.ResetSink();
        sink.WriteByte(5);
        JsonOperationResult afterReset = sink.FlushSink();

        Assert.Multiple(() =>
        {
            Assert.That(flushed.Success, Is.True);
            Assert.That(flushed.BytesWritten, Is.EqualTo(3));
            Assert.That(emptyFlush.Success, Is.True);
            Assert.That(emptyFlush.BytesWritten, Is.Zero);
            Assert.That(afterReset.Success, Is.True);
            Assert.That(afterReset.BytesWritten, Is.EqualTo(1));
            Assert.That(sink.GetBytesPending(), Is.Zero);
            Assert.That(sink.GetBytesCommitted(), Is.EqualTo(1));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 5 }));
            Assert.That(stream.FlushCount, Is.EqualTo(3));
            Assert.That(stream.Disposed, Is.False);
            Assert.That(stream.CanWrite, Is.True);
        });
    }

    [Test]
    public void BufferWriterSinkCommitsOnlyOnFlushAndGrowsPastInitialCapacity()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        JsonByteSink sink = new JsonByteSink(target, 6000);
        byte[] callerBytes = new byte[5000];
        int index = 0;
        while (index < callerBytes.Length)
        {
            callerBytes[index] = (byte)(index % 251);
            index++;
        }

        byte expectedFirst = callerBytes[0];
        byte expectedLast = callerBytes[^1];
        sink.WriteBytes(callerBytes);
        callerBytes[0] = 255;

        Assert.Multiple(() =>
        {
            Assert.That(sink.GetBytesPending(), Is.EqualTo(5000));
            Assert.That(sink.GetBytesCommitted(), Is.Zero);
            Assert.That(destination.WrittenCount, Is.Zero);
        });

        JsonOperationResult result = sink.FlushSink();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesWritten, Is.EqualTo(5000));
            Assert.That(sink.GetBytesPending(), Is.Zero);
            Assert.That(sink.GetBytesCommitted(), Is.EqualTo(5000));
            Assert.That(destination.WrittenCount, Is.EqualTo(5000));
            Assert.That(destination.WrittenSpan[0], Is.EqualTo(expectedFirst));
            Assert.That(destination.WrittenSpan[^1], Is.EqualTo(expectedLast));
            Assert.That(sink.FlushSink().BytesWritten, Is.Zero);
        });
    }

    [Test]
    public void OutputLimitIsAtomicAcrossPendingCommittedAndResetBytes()
    {
        TrackingMemoryStream stream = new TrackingMemoryStream();
        JsonWriterTarget target = new JsonWriterTarget();
        target.Stream = stream;
        JsonByteSink sink = new JsonByteSink(target, 3);

        sink.WriteBytes(new byte[] { 1, 2, 3 });
        JsonLimitException? pendingFailure = Assert.Throws<JsonLimitException>(() =>
            sink.WriteByte(4));
        JsonOperationResult flushed = sink.FlushSink();
        JsonLimitException? committedFailure = Assert.Throws<JsonLimitException>(() =>
            sink.WriteByte(4));

        Assert.Multiple(() =>
        {
            Assert.That(pendingFailure?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(committedFailure?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(flushed.BytesWritten, Is.EqualTo(3));
            Assert.That(sink.GetBytesPending(), Is.Zero);
            Assert.That(sink.GetBytesCommitted(), Is.EqualTo(3));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        sink.ResetSink();
        sink.WriteBytes(new byte[] { 4, 5, 6 });
        JsonOperationResult resetFlush = sink.FlushSink();

        Assert.Multiple(() =>
        {
            Assert.That(resetFlush.Success, Is.True);
            Assert.That(resetFlush.BytesWritten, Is.EqualTo(3));
            Assert.That(sink.GetBytesCommitted(), Is.EqualTo(3));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        });
    }

    [Test]
    public void DestinationFailuresPreserveAccurateCountsAndRemainSticky()
    {
        ThrowingWriteStream writeFailureStream = new ThrowingWriteStream();
        JsonWriterTarget writeFailureTarget = new JsonWriterTarget();
        writeFailureTarget.Stream = writeFailureStream;
        JsonByteSink writeFailureSink = new JsonByteSink(writeFailureTarget, 10);
        writeFailureSink.WriteBytes(new byte[] { 1, 2 });

        JsonOperationResult writeFailure = writeFailureSink.FlushSink();
        JsonOperationResult repeatedFailure = writeFailureSink.FlushSink();
        JsonWriteException? writeAfterFailure = Assert.Throws<JsonWriteException>(() =>
            writeFailureSink.WriteByte(3));

        FlushFailingMemoryStream flushFailureStream =
            new FlushFailingMemoryStream();
        JsonWriterTarget flushFailureTarget = new JsonWriterTarget();
        flushFailureTarget.Stream = flushFailureStream;
        JsonByteSink flushFailureSink = new JsonByteSink(flushFailureTarget, 10);
        flushFailureSink.WriteBytes(new byte[] { 4, 5 });
        JsonOperationResult flushFailure = flushFailureSink.FlushSink();

        ShortBufferWriter shortWriter = new ShortBufferWriter();
        JsonWriterTarget shortTarget = new JsonWriterTarget();
        shortTarget.BufferWriter = shortWriter;
        JsonByteSink shortSink = new JsonByteSink(shortTarget, 10);
        shortSink.WriteBytes(new byte[] { 6, 7 });
        JsonOperationResult shortFailure = shortSink.FlushSink();

        Assert.Multiple(() =>
        {
            Assert.That(writeFailure.Success, Is.False);
            Assert.That(writeFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(writeFailure.BytesWritten, Is.Zero);
            Assert.That(writeFailureSink.GetBytesPending(), Is.EqualTo(2));
            Assert.That(writeFailureSink.GetBytesCommitted(), Is.Zero);
            Assert.That(repeatedFailure.Diagnostic,
                Is.SameAs(writeFailure.Diagnostic));
            Assert.That(writeAfterFailure?.Diagnostic,
                Is.SameAs(writeFailure.Diagnostic));
            Assert.That(writeFailureStream.Disposed, Is.False);
            Assert.That(flushFailure.Success, Is.False);
            Assert.That(flushFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(flushFailure.BytesWritten, Is.EqualTo(2));
            Assert.That(flushFailureSink.GetBytesPending(), Is.Zero);
            Assert.That(flushFailureSink.GetBytesCommitted(), Is.EqualTo(2));
            Assert.That(flushFailureStream.ToArray(), Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(flushFailureStream.Disposed, Is.False);
            Assert.That(shortFailure.Success, Is.False);
            Assert.That(shortFailure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(shortSink.GetBytesPending(), Is.EqualTo(2));
            Assert.That(shortSink.GetBytesCommitted(), Is.Zero);
        });
    }

    class TrackingMemoryStream : System.IO.MemoryStream
    {
        internal bool Disposed;
        internal int FlushCount;

        public override void Flush()
        {
            FlushCount++;
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    class FlushFailingMemoryStream : System.IO.MemoryStream
    {
        internal bool Disposed;

        public override void Flush()
        {
            throw new System.IO.IOException("Flush failed.");
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    class ThrowingWriteStream : System.IO.Stream
    {
        internal bool Disposed;

        public override bool CanRead => false;
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
            throw new NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.IO.IOException("Write failed.");
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    class ShortBufferWriter : System.Buffers.IBufferWriter<byte>
    {
        public void Advance(int count)
        {
        }

        public Memory<byte> GetMemory(int sizeHint)
        {
            return new Memory<byte>(new byte[Math.Max(0, sizeHint - 1)]);
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            return new Span<byte>(new byte[Math.Max(0, sizeHint - 1)]);
        }
    }
}
