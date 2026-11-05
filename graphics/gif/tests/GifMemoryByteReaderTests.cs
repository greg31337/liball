using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifMemoryByteReaderTests
{
    [Test]
    public void ReadByte_ReturnsByteAndAdvancesOffset()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 7, 8 });

        int first = reader.ReadByte();
        int second = reader.ReadByte();

        Assert.That(first, Is.EqualTo(7));
        Assert.That(second, Is.EqualTo(8));
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.RemainingCount(), Is.EqualTo(0));
        Assert.That(reader.EndOfData, Is.False);
    }

    [Test]
    public void ReadByte_ReturnsMinusOneAndSetsEndOfDataAtEnd()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 7 });

        int first = reader.ReadByte();
        int eof = reader.ReadByte();

        Assert.That(first, Is.EqualTo(7));
        Assert.That(eof, Is.EqualTo(-1));
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
        Assert.That(reader.EndOfData, Is.True);
    }

    [Test]
    public void Read_CopiesExactCountIntoBufferAndAdvances()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 1, 2, 3, 4 });
        byte[] buffer = new byte[] { 0, 0, 0, 0, 0 };

        int read = reader.Read(buffer, 1, 3);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 3, 0 }));
        Assert.That(reader.ByteOffset, Is.EqualTo(3));
        Assert.That(reader.RemainingCount(), Is.EqualTo(1));
    }

    [Test]
    public void Read_ReturnsMinusOneForInvalidArgumentsWithoutAdvancing()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 1, 2, 3 });
        byte[] buffer = new byte[2];

        Assert.That(reader.Read(null!, 0, 1), Is.EqualTo(-1));
        Assert.That(reader.Read(buffer, -1, 1), Is.EqualTo(-1));
        Assert.That(reader.Read(buffer, 0, -1), Is.EqualTo(-1));
        Assert.That(reader.Read(buffer, 2, 1), Is.EqualTo(-1));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.EndOfData, Is.False);
    }

    [Test]
    public void Read_ReturnsMinusOneForInsufficientDataWithoutAdvancing()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 1, 2 });
        byte[] buffer = new byte[3];

        int read = reader.Read(buffer, 0, 3);

        Assert.That(read, Is.EqualTo(-1));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.EndOfData, Is.True);
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 0, 0 }));
    }

    [Test]
    public void Skip_AdvancesOffsetWhenEnoughDataRemains()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 1, 2, 3 });

        bool skipped = reader.Advance(2);

        Assert.That(skipped, Is.True);
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.ReadByte(), Is.EqualTo(3));
    }

    [Test]
    public void ReadUInt16LittleEndian_ReadsTwoBytesLowByteFirst()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 0x78, 0x56, 0x99 });

        int value = reader.ReadUInt16LittleEndian();

        Assert.That(value, Is.EqualTo(0x5678));
        Assert.That(reader.ByteOffset, Is.EqualTo(2));
        Assert.That(reader.EndOfData, Is.False);
    }

    [Test]
    public void ReadUInt16LittleEndian_ReturnsMinusOneAfterPartialReadAtEnd()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(new byte[] { 0x78 });

        int value = reader.ReadUInt16LittleEndian();

        Assert.That(value, Is.EqualTo(-1));
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
        Assert.That(reader.EndOfData, Is.True);
    }

    [Test]
    public void NullArrayConstructor_CreatesEmptyReader()
    {
        GifMemoryByteReader reader = new GifMemoryByteReader(null!);

        Assert.That(reader.RemainingCount(), Is.EqualTo(0));
        Assert.That(reader.ReadByte(), Is.EqualTo(-1));
        Assert.That(reader.EndOfData, Is.True);
    }
}
