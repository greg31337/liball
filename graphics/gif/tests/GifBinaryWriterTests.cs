using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifBinaryWriterTests
{
    [Test]
    public void WriteUInt16LittleEndian_WritesLowByteFirstAtOffset()
    {
        byte[] data = new byte[] { 0x99, 0x99, 0x99, 0x99 };

        bool success = GifBinaryWriter.WriteUInt16LittleEndian(data, 1, 0x1234);

        Assert.That(success, Is.True);
        Assert.That(data, Is.EqualTo(new byte[] { 0x99, 0x34, 0x12, 0x99 }));
    }

    [Test]
    public void WriteUInt16LittleEndian_SupportsFullUnsignedRange()
    {
        byte[] zero = new byte[2];
        byte[] max = new byte[2];

        bool zeroSuccess = GifBinaryWriter.WriteUInt16LittleEndian(zero, 0, 0);
        bool maxSuccess = GifBinaryWriter.WriteUInt16LittleEndian(max, 0, 65535);

        Assert.That(zeroSuccess, Is.True);
        Assert.That(maxSuccess, Is.True);
        Assert.That(zero, Is.EqualTo(new byte[] { 0x00, 0x00 }));
        Assert.That(max, Is.EqualTo(new byte[] { 0xFF, 0xFF }));
    }

    [Test]
    public void WriteUInt16LittleEndian_ReturnsFalseAndDoesNotMutateForInvalidInput()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.That(GifBinaryWriter.WriteUInt16LittleEndian(null!, 0, 1), Is.False);
        Assert.That(GifBinaryWriter.WriteUInt16LittleEndian(data, -1, 1), Is.False);
        Assert.That(GifBinaryWriter.WriteUInt16LittleEndian(data, 2, 1), Is.False);
        Assert.That(GifBinaryWriter.WriteUInt16LittleEndian(data, 0, -1), Is.False);
        Assert.That(GifBinaryWriter.WriteUInt16LittleEndian(data, 0, 65536), Is.False);
        Assert.That(data, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void CreateUInt16LittleEndian_ReturnsTwoByteLittleEndianPayload()
    {
        byte[] data = GifBinaryWriter.CreateUInt16LittleEndian(0xABCD);

        Assert.That(data, Is.EqualTo(new byte[] { 0xCD, 0xAB }));
        Assert.That(GifBinaryReader.ReadUInt16LittleEndian(data), Is.EqualTo(0xABCD));
    }

    [Test]
    public void CreateUInt16LittleEndian_ReturnsEmptyPayloadForInvalidValue()
    {
        Assert.That(GifBinaryWriter.CreateUInt16LittleEndian(-1), Is.Empty);
        Assert.That(GifBinaryWriter.CreateUInt16LittleEndian(65536), Is.Empty);
    }
}
