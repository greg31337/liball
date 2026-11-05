using System.Collections.Generic;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BoundedStreamReaderTests
{
    [Test]
    public void Read_TracksPositionAndRemainingBytes()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 10, 20, 30, 40, 50 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 5, 5, diagnostics);
        byte[] buffer = new byte[3];

        int read = reader.Read(buffer, 0, 3);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(buffer, Is.EqualTo(new byte[] { 10, 20, 30 }));
        Assert.That(reader.Position, Is.EqualTo(3));
        Assert.That(reader.Remaining(), Is.EqualTo(2));
        Assert.That(reader.RemainingLength(), Is.EqualTo(2));
        Assert.That(reader.RemainingLimit(), Is.EqualTo(2));
        Assert.That(reader.EndOfRange(), Is.False);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void ReadBytes_WhenRequestExceedsLength_ReturnsPartialAndAddsTruncatedDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 3, 10, diagnostics);

        byte[] bytes = reader.ReadBytes(5);

        Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(reader.Position, Is.EqualTo(3));
        Assert.That(reader.EndOfRange(), Is.True);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.read.bytes.truncated"));
    }

    [Test]
    public void Read_WhenRequestExceedsLimit_ReturnsPartialAndAddsLimitDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 5, 3, diagnostics);
        byte[] buffer = new byte[5];

        int read = reader.Read(buffer, 0, 5);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(buffer[0], Is.EqualTo(1));
        Assert.That(buffer[1], Is.EqualTo(2));
        Assert.That(buffer[2], Is.EqualTo(3));
        Assert.That(reader.Position, Is.EqualTo(3));
        Assert.That(reader.LimitReached(), Is.True);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.read.limit"));
    }

    [Test]
    public void SkipAndSeekAbsolute_UpdateRelativePositionWithinRange()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 9, 8, 7, 6, 5 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 5, 5, diagnostics);

        bool skipped = reader.SkipBytes(2);
        int value = reader.ReadByte();
        bool sought = reader.SeekAbsolute(4);
        int finalValue = reader.ReadByte();

        Assert.That(skipped, Is.True);
        Assert.That(value, Is.EqualTo(7));
        Assert.That(sought, Is.True);
        Assert.That(finalValue, Is.EqualTo(5));
        Assert.That(reader.Position, Is.EqualTo(5));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void SeekAbsolute_WhenTargetExceedsRange_ClampsAndAddsDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 4, 4, diagnostics);

        bool sought = reader.SeekAbsolute(9);

        Assert.That(sought, Is.False);
        Assert.That(reader.Position, Is.EqualTo(4));
        Assert.That(reader.EndOfRange(), Is.True);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.seek.truncated"));
    }

    [Test]
    public void FromStream_UsesCurrentSeekablePositionAsStart()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        stream.Position = 2;
        BoundedStreamReader reader = BoundedStreamReader.FromStream(stream, 10, diagnostics);

        byte[] bytes = reader.ReadBytes(2);

        Assert.That(reader.StartPosition, Is.EqualTo(2));
        Assert.That(reader.Length, Is.EqualTo(3));
        Assert.That(bytes, Is.EqualTo(new byte[] { 3, 4 }));
        Assert.That(stream.Position, Is.EqualTo(4));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void InvalidArguments_AddRecoverableDiagnosticsWithoutReading()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 3, 3, diagnostics);
        byte[] buffer = new byte[2];

        int read = reader.Read(buffer, 1, 4);
        bool skipped = reader.SkipBytes(-1);
        bool sought = reader.SeekAbsolute(-1);

        Assert.That(read, Is.EqualTo(0));
        Assert.That(skipped, Is.False);
        Assert.That(sought, Is.False);
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(3));
    }

    [Test]
    public void Constructor_NormalizesNegativeBoundsAndAddsDiagnostics()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });

        BoundedStreamReader reader = new BoundedStreamReader(stream, -2, -3, diagnostics);

        Assert.That(reader.Length, Is.EqualTo(0));
        Assert.That(reader.Limit, Is.EqualTo(0));
        Assert.That(reader.Remaining(), Is.EqualTo(0));
        Assert.That(diagnostics, Has.Count.EqualTo(2));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.length.negative"));
        Assert.That(diagnostics[1].Code, Is.EqualTo("stream.limit.negative"));
    }

    [Test]
    public void Read_WhenSourceShortReads_FillsRequestedBufferWithoutDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        ShortReadStream stream = new ShortReadStream(new byte[] { 1, 2, 3, 4 }, true, 1);
        BoundedStreamReader reader = new BoundedStreamReader(stream, 4, 4, diagnostics);
        byte[] buffer = new byte[4];

        int read = reader.Read(buffer, 0, buffer.Length);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(reader.Position, Is.EqualTo(4));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Read_WhenSourceEndsBeforeDeclaredLength_ReportsMissingRange()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 4, 4, diagnostics);
        byte[] buffer = new byte[4];

        int read = reader.Read(buffer, 0, buffer.Length);

        Assert.That(read, Is.EqualTo(2));
        Assert.That(reader.Position, Is.EqualTo(2));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.read.short"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(diagnostics[0].Offset, Is.EqualTo(2));
        Assert.That(diagnostics[0].Length, Is.EqualTo(2));
    }

    [Test]
    public void Read_RejectsEveryInvalidBufferRangeWithoutMoving()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 3, 3, diagnostics);
        byte[] buffer = new byte[2];

        reader.Read(null, 0, 1);
        reader.Read(buffer, -1, 1);
        reader.Read(buffer, 0, -1);
        reader.Read(buffer, 3, 0);
        reader.Read(buffer, 1, 2);

        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(diagnostics, Has.Count.EqualTo(5));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.buffer.null"));
        Assert.That(diagnostics[1].Code, Is.EqualTo("stream.buffer.offset.negative"));
        Assert.That(diagnostics[2].Code, Is.EqualTo("stream.buffer.count.negative"));
        Assert.That(diagnostics[3].Code, Is.EqualTo("stream.buffer.offset.large"));
        Assert.That(diagnostics[4].Code, Is.EqualTo("stream.buffer.range.large"));
    }

    [Test]
    public void ReadBytes_NegativeCountReportsErrorWhileZeroCountIsValid()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        MemoryStream stream = new MemoryStream(new byte[] { 1 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 1, 1, diagnostics);

        byte[] negative = reader.ReadBytes(-1);
        byte[] zero = reader.ReadBytes(0);

        Assert.That(negative, Is.Empty);
        Assert.That(zero, Is.Empty);
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.read.negative"));
    }

    [Test]
    public void NonSeekableReader_SkipsForwardAndRejectsBackwardSeek()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        NonSeekableReadStream stream = new NonSeekableReadStream(new byte[] { 1, 2, 3, 4 });
        BoundedStreamReader reader = new BoundedStreamReader(stream, 4, 4, diagnostics);

        bool skipped = reader.SkipBytes(2);
        bool sought = reader.SeekAbsolute(1);
        int value = reader.ReadByte();

        Assert.That(skipped, Is.True);
        Assert.That(sought, Is.False);
        Assert.That(value, Is.EqualTo(3));
        Assert.That(reader.Position, Is.EqualTo(3));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.seek.backward"));
    }

    [Test]
    public void SeekAbsolute_WhenSourceOffsetWouldOverflow_ReportsLimitDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        LargePositionSeekStream stream = new LargePositionSeekStream(10, long.MaxValue);
        BoundedStreamReader reader = new BoundedStreamReader(stream, long.MaxValue, long.MaxValue, diagnostics);

        bool sought = reader.SeekAbsolute(long.MaxValue);

        Assert.That(sought, Is.False);
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(stream.SeekCallCount, Is.EqualTo(0));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("stream.seek.overflow"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(diagnostics[0].Length, Is.EqualTo(long.MaxValue));
    }
}

public class LargePositionSeekStream : Stream
{
    public long PositionValue;
    public long LengthValue;
    public int SeekCallCount;

    public LargePositionSeekStream(long position, long length)
    {
        PositionValue = position;
        LengthValue = length;
        SeekCallCount = 0;
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return true; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { return LengthValue; }
    }

    public override long Position
    {
        get { return PositionValue; }
        set { PositionValue = value; }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        SeekCallCount = SeekCallCount + 1;
        PositionValue = offset;
        return PositionValue;
    }

    public override void SetLength(long value)
    {
        LengthValue = value;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }
}
