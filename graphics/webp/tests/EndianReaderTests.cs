using webp;

namespace webp.Tests;

[TestFixture]
public class EndianReaderTests
{
    [Test]
    public void ReadsLittleEndianUnsignedValuesAtOffsets()
    {
        byte[] data = new byte[]
        {
            0xFE,
            0x34,
            0x12,
            0x78,
            0x56,
            0x34,
            0x80,
            0xFF,
            0xFF,
            0x78,
            0x56,
            0x34,
            0x12
        };

        Assert.That(WebpEndianReader.ReadByte(data, 0), Is.EqualTo(0xFE));
        Assert.That(WebpEndianReader.ReadUInt16(data, 1), Is.EqualTo(0x1234));
        Assert.That(WebpEndianReader.ReadUInt24(data, 3), Is.EqualTo(0x345678));
        Assert.That(WebpEndianReader.ReadUInt32(data, 9), Is.EqualTo(0x12345678U));
    }

    [Test]
    public void ReadsSignedInt24WithSignExtension()
    {
        byte[] data = new byte[]
        {
            0xFF,
            0xFF,
            0x7F,
            0x00,
            0x00,
            0x80,
            0x80,
            0xFF,
            0xFF
        };

        Assert.That(WebpEndianReader.ReadInt24(data, 0), Is.EqualTo(8388607));
        Assert.That(WebpEndianReader.ReadInt24(data, 3), Is.EqualTo(-8388608));
        Assert.That(WebpEndianReader.ReadInt24(data, 6), Is.EqualTo(-128));
    }

    [Test]
    public void ReadSlice_ReturnsBoundedView()
    {
        byte[] data = new byte[] { 10, 20, 30, 40, 50 };

        ReadOnlySpan<byte> slice = WebpEndianReader.ReadSlice(data, 1, 3);

        Assert.That(slice.ToArray(), Is.EqualTo(new byte[] { 20, 30, 40 }));
    }

    [Test]
    public void Reads_AllowEmptySliceAtEnd()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        ReadOnlySpan<byte> slice = WebpEndianReader.ReadSlice(data, data.Length, 0);

        Assert.That(slice.Length, Is.EqualTo(0));
    }

    [Test]
    public void Reads_RejectNegativeOffsetsAndLengths()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianReader.ReadByte(data, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianReader.ReadSlice(data, 0, -1));
    }

    [Test]
    public void Reads_RejectRangesBeyondInput()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.Throws<ArgumentException>(() => WebpEndianReader.ReadByte(data, 3));
        Assert.Throws<ArgumentException>(() => WebpEndianReader.ReadUInt16(data, 2));
        Assert.Throws<ArgumentException>(() => WebpEndianReader.ReadUInt24(data, 1));
        Assert.Throws<ArgumentException>(() => WebpEndianReader.ReadUInt32(data, 0));
        Assert.Throws<ArgumentException>(() => WebpEndianReader.ReadSlice(data, 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianReader.ReadSlice(data, 4, 0));
    }
}
