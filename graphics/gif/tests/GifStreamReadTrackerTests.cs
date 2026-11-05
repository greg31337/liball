using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifStreamReadTrackerTests
{
    [Test]
    public void ReadByte_AdvancesByteOffsetForConsumedBytes()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 10, 20 });
        GifStreamReadTracker tracker = new GifStreamReadTracker();

        int first = tracker.ReadByte(stream);
        int second = tracker.ReadByte(stream);

        Assert.That(first, Is.EqualTo(10));
        Assert.That(second, Is.EqualTo(20));
        Assert.That(tracker.ByteOffset, Is.EqualTo(2));
        Assert.That(tracker.EndOfStream, Is.False);
    }

    [Test]
    public void ReadByte_SetsEndOfStreamWithoutAdvancingAtEof()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 10 });
        GifStreamReadTracker tracker = new GifStreamReadTracker();

        int first = tracker.ReadByte(stream);
        int eof = tracker.ReadByte(stream);

        Assert.That(first, Is.EqualTo(10));
        Assert.That(eof, Is.EqualTo(-1));
        Assert.That(tracker.ByteOffset, Is.EqualTo(1));
        Assert.That(tracker.EndOfStream, Is.True);
    }

    [Test]
    public void Read_AdvancesByActualBulkBytesRead()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        byte[] buffer = new byte[] { 0, 0, 0, 0 };
        GifStreamReadTracker tracker = new GifStreamReadTracker();

        int read = tracker.Read(stream, buffer, 1, 3);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        Assert.That(tracker.ByteOffset, Is.EqualTo(3));
        Assert.That(tracker.EndOfStream, Is.False);
    }

    [Test]
    public void Read_ReturnsMinusOneForInvalidArgumentsWithoutAdvancing()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        byte[] buffer = new byte[2];
        GifStreamReadTracker tracker = new GifStreamReadTracker();

        Assert.That(tracker.Read(null!, buffer, 0, 1), Is.EqualTo(-1));
        Assert.That(tracker.Read(stream, null!, 0, 1), Is.EqualTo(-1));
        Assert.That(tracker.Read(stream, buffer, -1, 1), Is.EqualTo(-1));
        Assert.That(tracker.Read(stream, buffer, 0, -1), Is.EqualTo(-1));
        Assert.That(tracker.Read(stream, buffer, 2, 1), Is.EqualTo(-1));
        Assert.That(tracker.ByteOffset, Is.EqualTo(0));
        Assert.That(tracker.EndOfStream, Is.False);
    }

    [Test]
    public void ReadUInt16LittleEndian_ReadsTwoBytesAndAdvancesPosition()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x34, 0x12, 0x99 });
        GifStreamReadTracker tracker = new GifStreamReadTracker();

        int value = tracker.ReadUInt16LittleEndian(stream);

        Assert.That(value, Is.EqualTo(0x1234));
        Assert.That(tracker.ByteOffset, Is.EqualTo(2));
        Assert.That(tracker.EndOfStream, Is.False);
    }

    [Test]
    public void ReadUInt16LittleEndian_ReturnsMinusOneAfterPartialReadAtEof()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x34 });
        GifStreamReadTracker tracker = new GifStreamReadTracker();

        int value = tracker.ReadUInt16LittleEndian(stream);

        Assert.That(value, Is.EqualTo(-1));
        Assert.That(tracker.ByteOffset, Is.EqualTo(1));
        Assert.That(tracker.EndOfStream, Is.True);
    }
}
