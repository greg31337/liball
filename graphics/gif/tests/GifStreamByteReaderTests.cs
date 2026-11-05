using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifStreamByteReaderTests
{
    [Test]
    public void ReadByte_ReadsFromConstructorStreamAndTracksOffset()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 4, 5 }));

        int first = reader.ReadByte();
        int second = reader.ReadByte();

        Assert.That(first, Is.EqualTo(4));
        Assert.That(second, Is.EqualTo(5));
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.EndOfStream, Is.False);
    }

    [Test]
    public void ReadByte_NullStreamBehavesAsEmptyStream()
    {
        GifStreamByteReader reader = new GifStreamByteReader(null!);

        int value = reader.ReadByte();

        Assert.That(value, Is.EqualTo(-1));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.EndOfStream, Is.True);
    }

    [Test]
    public void Read_ReadsExactCountAcrossStreamingReads()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 2, 3, 4 }));
        byte[] buffer = new byte[] { 0, 0, 0, 0, 0 };

        int read = reader.Read(buffer, 1, 4);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
        Assert.That(reader.ByteOffset, Is.EqualTo(4));
        Assert.That(reader.EndOfStream, Is.False);
    }

    [Test]
    public void Read_ReturnsMinusOneForInvalidArgumentsWithoutAdvancing()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 2 }));
        byte[] buffer = new byte[2];

        Assert.That(reader.Read(null!, 0, 1), Is.EqualTo(-1));
        Assert.That(reader.Read(buffer, -1, 1), Is.EqualTo(-1));
        Assert.That(reader.Read(buffer, 0, -1), Is.EqualTo(-1));
        Assert.That(reader.Read(buffer, 2, 1), Is.EqualTo(-1));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.EndOfStream, Is.False);
    }

    [Test]
    public void Read_ReturnsMinusOneWhenExactCountCannotBeSatisfiedAfterPartialRead()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 2 }));
        byte[] buffer = new byte[] { 0, 0, 0 };

        int read = reader.Read(buffer, 0, 3);

        Assert.That(read, Is.EqualTo(-1));
        Assert.That(buffer, Is.EqualTo(new byte[] { 1, 2, 0 }));
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.EndOfStream, Is.True);
    }

    [Test]
    public void Skip_ConsumesRequestedBytes()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 2, 3 }));

        bool skipped = reader.Advance(2);
        int next = reader.ReadByte();

        Assert.That(skipped, Is.True);
        Assert.That(next, Is.EqualTo(3));
        Assert.That(reader.ByteOffset, Is.EqualTo(3));
    }

    [Test]
    public void Skip_ReturnsFalseWhenStreamEndsBeforeRequestedCount()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 1, 2 }));

        bool skipped = reader.Advance(3);

        Assert.That(skipped, Is.False);
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.EndOfStream, Is.True);
    }

    [Test]
    public void ReadUInt16LittleEndian_ReadsLowByteFirstAndTracksPosition()
    {
        GifStreamByteReader reader = new GifStreamByteReader(new MemoryStream(new byte[] { 0xCD, 0xAB, 0x99 }));

        int value = reader.ReadUInt16LittleEndian();

        Assert.That(value, Is.EqualTo(0xABCD));
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.EndOfStream, Is.False);
    }
}
