using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class BinaryReaderTests
{
    [Test]
    public void ReadUInt8_ReadsSingleByteFromArray()
    {
        byte[] data = new byte[] { 0, 127, 255 };

        Assert.That(PngBinaryReader.ReadUInt8(data, 0), Is.EqualTo(0));
        Assert.That(PngBinaryReader.ReadUInt8(data, 1), Is.EqualTo(127));
        Assert.That(PngBinaryReader.ReadUInt8(data, 2), Is.EqualTo(255));
    }

    [Test]
    public void ReadUInt16_ReadsBigEndianValueFromArray()
    {
        byte[] data = new byte[] { 0xAA, 0x12, 0x34, 0xBB };

        Assert.That(PngBinaryReader.ReadUInt16(data, 1), Is.EqualTo(0x1234));
    }

    [Test]
    public void ReadUInt32_ReadsBigEndianValueFromArray()
    {
        byte[] data = new byte[] { 0xAA, 0x89, 0x50, 0x4E, 0x47, 0xBB };

        Assert.That(PngBinaryReader.ReadUInt32(data, 1), Is.EqualTo(0x89504E47u));
    }

    [Test]
    public void ReadUInt32_CanReadMaximumUnsignedValue()
    {
        byte[] data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.That(PngBinaryReader.ReadUInt32(data, 0), Is.EqualTo(uint.MaxValue));
    }

    [Test]
    public void ArrayReaders_ReadAtLastValidOffsets()
    {
        byte[] data = new byte[] { 0xAA, 0xBB, 0x12, 0x34, 0x89, 0x50, 0x4E, 0x47 };

        Assert.That(PngBinaryReader.ReadUInt8(data, 7), Is.EqualTo(0x47));
        Assert.That(PngBinaryReader.ReadUInt16(data, 6), Is.EqualTo(0x4E47));
        Assert.That(PngBinaryReader.ReadUInt32(data, 4), Is.EqualTo(0x89504E47u));
    }

    [Test]
    public void ArrayReaders_ReadMinimumAndMaximumValues()
    {
        byte[] data = new byte[] { 0, 0, 0xFF, 0xFF, 0, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.That(PngBinaryReader.ReadUInt16(data, 0), Is.EqualTo(ushort.MinValue));
        Assert.That(PngBinaryReader.ReadUInt16(data, 2), Is.EqualTo(ushort.MaxValue));
        Assert.That(PngBinaryReader.ReadUInt32(data, 4), Is.EqualTo(uint.MinValue));
        Assert.That(PngBinaryReader.ReadUInt32(data, 8), Is.EqualTo(uint.MaxValue));
    }

    [Test]
    public void ArrayReaders_RoundTripValuesWrittenToArray()
    {
        byte[] data = new byte[7];

        PngBinaryWriter.WriteUInt8(data, 0, 0xFE);
        PngBinaryWriter.WriteUInt16(data, 1, 0xABCD);
        PngBinaryWriter.WriteUInt32(data, 3, 0x01234567u);

        Assert.That(PngBinaryReader.ReadUInt8(data, 0), Is.EqualTo(0xFE));
        Assert.That(PngBinaryReader.ReadUInt16(data, 1), Is.EqualTo(0xABCD));
        Assert.That(PngBinaryReader.ReadUInt32(data, 3), Is.EqualTo(0x01234567u));
    }

    [Test]
    public void ArrayReaders_RejectInvalidOffsets()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryReader.ReadUInt8(data, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryReader.ReadUInt8(data, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryReader.ReadUInt16(data, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryReader.ReadUInt32(data, 0));
    }

    [Test]
    public void StreamReaders_ReadBigEndianValuesAndAdvanceStream()
    {
        MemoryStream stream = new MemoryStream(new byte[] { 0x7F, 0x12, 0x34, 0x89, 0x50, 0x4E, 0x47 });

        Assert.That(PngBinaryReader.ReadUInt8(stream), Is.EqualTo(0x7F));
        Assert.That(stream.Position, Is.EqualTo(1));

        Assert.That(PngBinaryReader.ReadUInt16(stream), Is.EqualTo(0x1234));
        Assert.That(stream.Position, Is.EqualTo(3));

        Assert.That(PngBinaryReader.ReadUInt32(stream), Is.EqualTo(0x89504E47u));
        Assert.That(stream.Position, Is.EqualTo(7));
    }

    [Test]
    public void StreamReaders_RejectIncompleteValues()
    {
        MemoryStream uint8 = new MemoryStream(new byte[0]);
        MemoryStream uint16 = new MemoryStream(new byte[] { 1 });
        MemoryStream uint32 = new MemoryStream(new byte[] { 1, 2, 3 });

        Assert.Throws<EndOfStreamException>(() => PngBinaryReader.ReadUInt8(uint8));
        Assert.Throws<EndOfStreamException>(() => PngBinaryReader.ReadUInt16(uint16));
        Assert.Throws<EndOfStreamException>(() => PngBinaryReader.ReadUInt32(uint32));
        Assert.That(uint8.Position, Is.EqualTo(0));
        Assert.That(uint16.Position, Is.EqualTo(1));
        Assert.That(uint32.Position, Is.EqualTo(3));
    }

    [Test]
    public void Readers_RejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngBinaryReader.ReadUInt8(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryReader.ReadUInt16(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryReader.ReadUInt32(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryReader.ReadUInt8(null!));
        Assert.Throws<ArgumentNullException>(() => PngBinaryReader.ReadUInt16(null!));
        Assert.Throws<ArgumentNullException>(() => PngBinaryReader.ReadUInt32(null!));
    }
}
