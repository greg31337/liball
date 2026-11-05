using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class BinaryWriterTests
{
    [Test]
    public void WriteUInt8_WritesSingleByteToArray()
    {
        byte[] data = new byte[] { 0, 0, 0 };

        PngBinaryWriter.WriteUInt8(data, 1, 255);

        Assert.That(data, Is.EqualTo(new byte[] { 0, 255, 0 }));
    }

    [Test]
    public void WriteUInt16_WritesBigEndianValueToArray()
    {
        byte[] data = new byte[] { 0xAA, 0, 0, 0xBB };

        PngBinaryWriter.WriteUInt16(data, 1, 0x1234);

        Assert.That(data, Is.EqualTo(new byte[] { 0xAA, 0x12, 0x34, 0xBB }));
    }

    [Test]
    public void WriteUInt32_WritesBigEndianValueToArray()
    {
        byte[] data = new byte[] { 0xAA, 0, 0, 0, 0, 0xBB };

        PngBinaryWriter.WriteUInt32(data, 1, 0x89504E47u);

        Assert.That(data, Is.EqualTo(new byte[] { 0xAA, 0x89, 0x50, 0x4E, 0x47, 0xBB }));
    }

    [Test]
    public void WriteUInt32_CanWriteMaximumUnsignedValue()
    {
        byte[] data = new byte[4];

        PngBinaryWriter.WriteUInt32(data, 0, uint.MaxValue);

        Assert.That(data, Is.EqualTo(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }));
        Assert.That(PngBinaryReader.ReadUInt32(data, 0), Is.EqualTo(uint.MaxValue));
    }

    [Test]
    public void ArrayWriters_WriteAtLastValidOffsets()
    {
        byte[] data = new byte[8];

        PngBinaryWriter.WriteUInt8(data, 7, 0x47);
        PngBinaryWriter.WriteUInt16(data, 5, 0x4E47);
        PngBinaryWriter.WriteUInt32(data, 1, 0x89504E47u);

        Assert.That(data, Is.EqualTo(new byte[] { 0, 0x89, 0x50, 0x4E, 0x47, 0x4E, 0x47, 0x47 }));
    }

    [Test]
    public void ArrayWriters_WriteMinimumAndMaximumValues()
    {
        byte[] data = new byte[12];

        PngBinaryWriter.WriteUInt16(data, 0, ushort.MinValue);
        PngBinaryWriter.WriteUInt16(data, 2, ushort.MaxValue);
        PngBinaryWriter.WriteUInt32(data, 4, uint.MinValue);
        PngBinaryWriter.WriteUInt32(data, 8, uint.MaxValue);

        Assert.That(data, Is.EqualTo(new byte[] { 0, 0, 0xFF, 0xFF, 0, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF }));
    }

    [Test]
    public void ArrayWriters_DoNotModifyAdjacentBytes()
    {
        byte[] data = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

        PngBinaryWriter.WriteUInt16(data, 2, 0x1234);

        Assert.That(data, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0x12, 0x34, 0xAA, 0xAA }));
    }

    [Test]
    public void ArrayWriters_RoundTripThroughArrayReaders()
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
    public void ArrayWriters_RejectInvalidOffsets()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryWriter.WriteUInt8(data, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryWriter.WriteUInt8(data, 3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryWriter.WriteUInt16(data, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngBinaryWriter.WriteUInt32(data, 0, 0));
    }

    [Test]
    public void StreamWriters_WriteBigEndianValuesAndAdvanceStream()
    {
        MemoryStream stream = new MemoryStream();

        PngBinaryWriter.WriteUInt8(stream, 0x7F);
        Assert.That(stream.Position, Is.EqualTo(1));

        PngBinaryWriter.WriteUInt16(stream, 0x1234);
        Assert.That(stream.Position, Is.EqualTo(3));

        PngBinaryWriter.WriteUInt32(stream, 0x89504E47u);
        Assert.That(stream.Position, Is.EqualTo(7));

        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x7F, 0x12, 0x34, 0x89, 0x50, 0x4E, 0x47 }));
    }

    [Test]
    public void StreamWriters_RoundTripThroughStreamReaders()
    {
        MemoryStream stream = new MemoryStream();

        PngBinaryWriter.WriteUInt8(stream, 0x42);
        PngBinaryWriter.WriteUInt16(stream, 0xABCD);
        PngBinaryWriter.WriteUInt32(stream, 0x01234567u);
        stream.Position = 0;

        Assert.That(PngBinaryReader.ReadUInt8(stream), Is.EqualTo(0x42));
        Assert.That(PngBinaryReader.ReadUInt16(stream), Is.EqualTo(0xABCD));
        Assert.That(PngBinaryReader.ReadUInt32(stream), Is.EqualTo(0x01234567u));
    }

    [Test]
    public void Writers_RejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngBinaryWriter.WriteUInt8(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryWriter.WriteUInt16(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryWriter.WriteUInt32(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryWriter.WriteUInt8(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryWriter.WriteUInt16(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngBinaryWriter.WriteUInt32(null!, 0));
    }
}
