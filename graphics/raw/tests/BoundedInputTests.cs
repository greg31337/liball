using raw;

namespace raw.Tests;

public class BoundedInputTests
{
    [Test]
    public void MemoryReadsAreCappedToTheDeclaredWindow()
    {
        byte[] source = Enumerable.Range(0, 10).Select(value => (byte)value).ToArray();
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 2, 4, new RawLimits());
        byte[] destination = new byte[8];

        int read = input.Read(destination);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(4));
            Assert.That(destination.Take(4), Is.EqualTo(new byte[] { 2, 3, 4, 5 }));
            Assert.That(input.GetLength(), Is.EqualTo(4));
            Assert.That(input.GetPosition(), Is.EqualTo(4));
            Assert.That(input.GetAbsolutePosition(), Is.EqualTo(6));
            Assert.That(input.GetRemaining(), Is.EqualTo(0));
            Assert.That(input.Read(destination), Is.EqualTo(0));
        });
    }

    [Test]
    public void MemoryWindowRejectsOutOfRangeConstructionAndExactReads()
    {
        RawLimits limits = new RawLimits();
        RawFormatException? invalidWindow = Assert.Throws<RawFormatException>(() => new BoundedInput(new ReadOnlyMemory<byte>(new byte[10]), 8, 3, limits));
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(new byte[10]), 2, 4, limits);
        RawFormatException? exact = Assert.Throws<RawFormatException>(() => input.ReadExactly(new byte[5]));

        Assert.Multiple(() =>
        {
            Assert.That((invalidWindow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That((exact ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(exact.Offset, Is.EqualTo(2));
            Assert.That(input.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void SeekableStreamUsesAbsoluteWindowOffsetsAndExactSkip()
    {
        byte[] source = Enumerable.Range(0, 10).Select(value => (byte)value).ToArray();
        MemoryStream stream = new MemoryStream(source, false);
        stream.Position = 9;
        BoundedInput input = BoundedInput.CreateStream(stream, 2, 5, new RawLimits());

        input.Skip(2);
        byte value = input.ReadByte();

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(4));
            Assert.That(input.GetPosition(), Is.EqualTo(3));
            Assert.That(input.GetAbsolutePosition(), Is.EqualTo(5));
            Assert.That(stream.Position, Is.EqualTo(5));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void SeekableWindowRejectsBytesBeyondThePhysicalStream()
    {
        MemoryStream stream = new MemoryStream(new byte[10], false);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => BoundedInput.CreateStream(stream, 8, 3, new RawLimits()));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
    }

    [Test]
    public void NonSeekableStreamConsumesPrefixAndLoopsAcrossShortReads()
    {
        byte[] source = Enumerable.Range(0, 10).Select(value => (byte)value).ToArray();
        ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(source, 1);
        BoundedInput input = BoundedInput.CreateStream(stream, 2, 5, new RawLimits());
        byte[] destination = new byte[3];

        input.ReadExactly(destination);
        input.Skip(2);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new byte[] { 2, 3, 4 }));
            Assert.That(input.GetPosition(), Is.EqualTo(5));
            Assert.That(input.GetAbsolutePosition(), Is.EqualTo(7));
            Assert.That(input.GetRemaining(), Is.EqualTo(0));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void NonSeekableExactReadReportsPhysicalTruncationOffset()
    {
        ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(new byte[] { 0, 1, 2, 3 }, 2);
        BoundedInput input = BoundedInput.CreateStream(stream, 1, 5, new RawLimits());

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => input.ReadExactly(new byte[5]));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(exception.Offset, Is.EqualTo(4));
            Assert.That(input.GetPosition(), Is.EqualTo(3));
            Assert.That(input.GetAbsolutePosition(), Is.EqualTo(4));
        });
    }

    [Test]
    public void NonSeekableExactSkipReportsPhysicalTruncationOffset()
    {
        ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(new byte[] { 0, 1, 2 }, 2);
        BoundedInput input = BoundedInput.CreateStream(stream, 0, 5, new RawLimits());

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => input.Skip(5));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(exception.Offset, Is.EqualTo(3));
            Assert.That(input.GetPosition(), Is.EqualTo(3));
        });
    }

    [Test]
    public void SkipBeyondWindowFailsWithoutAdvancingInput()
    {
        byte[] source = Enumerable.Range(0, 10).Select(value => (byte)value).ToArray();
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 2, 4, new RawLimits());

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => input.Skip(5));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(input.GetPosition(), Is.EqualTo(0));
            Assert.That(input.ReadByte(), Is.EqualTo(2));
        });
    }

    [Test]
    public void NonSeekableConstructorRequiresItsPrefixToExist()
    {
        ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(new byte[] { 0, 1 }, 1);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => BoundedInput.CreateStream(stream, 3, 1, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(exception.Offset, Is.EqualTo(2));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void RandomReadsStayInsideWindowAndPreserveLogicalPosition()
    {
        byte[] source = Enumerable.Range(0, 10).Select(value => (byte)value).ToArray();
        MemoryStream stream = new MemoryStream(source, false);
        BoundedInput input = BoundedInput.CreateStream(stream, 2, 6, new RawLimits());
        byte first = input.ReadByte();
        byte[] random = new byte[2];

        input.ReadAt(3, random);
        byte second = input.ReadByte();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(2));
            Assert.That(random, Is.EqualTo(new byte[] { 5, 6 }));
            Assert.That(second, Is.EqualTo(3));
            Assert.That(input.GetPosition(), Is.EqualTo(2));
        });
    }

    [Test]
    public void RandomReadsRejectWindowEscapesAndNonSeekableStreams()
    {
        BoundedInput memory = new BoundedInput(new ReadOnlyMemory<byte>(new byte[10]), 2, 4, new RawLimits());
        ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(new byte[10], 2);
        BoundedInput nonSeekable = BoundedInput.CreateStream(stream, 0, 10, new RawLimits());

        RawFormatException? outside = Assert.Throws<RawFormatException>(() => memory.ReadAt(3, new byte[2]));
        RawFormatException? unsupported = Assert.Throws<RawFormatException>(() => nonSeekable.ReadAt(0, new byte[1]));

        Assert.Multiple(() =>
        {
            Assert.That((outside ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That((unsupported ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That(nonSeekable.GetPosition(), Is.EqualTo(0));
        });
    }

    class ChunkedNonSeekableStream : Stream
    {
        MemoryStream Inner;
        int MaximumRead;
        public bool WasDisposed;

        public ChunkedNonSeekableStream(byte[] bytes, int maximumRead)
        {
            Inner = new MemoryStream(bytes, false);
            MaximumRead = maximumRead;
            WasDisposed = false;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int limited = Math.Min(count, MaximumRead);
            return Inner.Read(buffer, offset, limited);
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
