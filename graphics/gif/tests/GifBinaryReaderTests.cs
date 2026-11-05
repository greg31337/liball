using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifBinaryReaderTests
{
    [Test]
    public void ReadUInt16LittleEndian_FromBytesCombinesLowByteFirst()
    {
        int value = GifBinaryReader.ReadUInt16LittleEndian(0x34, 0x12);

        Assert.That(value, Is.EqualTo(0x1234));
    }

    [Test]
    public void ReadUInt16LittleEndian_FromBytesSupportsFullUnsignedRange()
    {
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(0x00, 0x00), Is.EqualTo(0));
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(0xFF, 0xFF), Is.EqualTo(65535));
    }

    [Test]
    public void ReadUInt16LittleEndian_FromArrayReadsAtOffset()
    {
        byte[] data = new byte[] { 0x99, 0x78, 0x56, 0x00 };

        int value = GifBinaryReader.ReadUInt16LittleEndian(data, 1);

        Assert.That(value, Is.EqualTo(0x5678));
    }

    [Test]
    public void ReadUInt16LittleEndian_FromArrayDefaultsToOffsetZero()
    {
        byte[] data = new byte[] { 0xCD, 0xAB };

        int value = GifBinaryReader.ReadUInt16LittleEndian(data);

        Assert.That(value, Is.EqualTo(0xABCD));
    }

    [Test]
    public void ReadUInt16LittleEndian_FromArrayReturnsMinusOneForInvalidInput()
    {
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(null!, 0), Is.EqualTo(-1));
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(new byte[] { 1, 2 }, -1), Is.EqualTo(-1));
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(new byte[] { 1 }, 0), Is.EqualTo(-1));
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(new byte[] { 1, 2 }, 1), Is.EqualTo(-1));
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(new byte[] { 1, 2 }, 2), Is.EqualTo(-1));
    }
}
