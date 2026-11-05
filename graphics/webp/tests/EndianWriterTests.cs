using webp;

namespace webp.Tests;

[TestFixture]
public class EndianWriterTests
{
    [Test]
    public void WritesLittleEndianValuesAtOffsets()
    {
        byte[] data = new byte[13];

        WebpEndianWriter.WriteByte(data, 0, 0xFE);
        WebpEndianWriter.WriteUInt16(data, 1, 0x1234);
        WebpEndianWriter.WriteUInt24(data, 3, 0x345678);
        WebpEndianWriter.WriteInt24(data, 6, -128);
        WebpEndianWriter.WriteUInt32(data, 9, 0x12345678U);

        Assert.That(data, Is.EqualTo(new byte[]
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
        }));
    }

    [Test]
    public void WriteInt24_WritesSignedRangeBoundaries()
    {
        byte[] data = new byte[6];

        WebpEndianWriter.WriteInt24(data, 0, 8388607);
        WebpEndianWriter.WriteInt24(data, 3, -8388608);

        Assert.That(data, Is.EqualTo(new byte[]
        {
            0xFF,
            0xFF,
            0x7F,
            0x00,
            0x00,
            0x80
        }));
    }

    [Test]
    public void WriteBytesAndPaddingByte_ModifyOnlyRequestedRange()
    {
        byte[] data = new byte[] { 9, 9, 9, 9, 9, 9 };

        WebpEndianWriter.WriteBytes(data, 1, new byte[] { 1, 2, 3 });
        WebpEndianWriter.WritePaddingByte(data, 4);

        Assert.That(data, Is.EqualTo(new byte[] { 9, 1, 2, 3, 0, 9 }));
    }

    [Test]
    public void AppendMethods_WriteLittleEndianValues()
    {
        List<byte> data = new List<byte>();

        WebpEndianWriter.AppendByte(data, 0xFE);
        WebpEndianWriter.AppendUInt16(data, 0x1234);
        WebpEndianWriter.AppendUInt24(data, 0x345678);
        WebpEndianWriter.AppendInt24(data, -128);
        WebpEndianWriter.AppendUInt32(data, 0x12345678U);

        Assert.That(data.ToArray(), Is.EqualTo(new byte[]
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
        }));
    }

    [Test]
    public void AppendChunkPayload_AddsPaddingOnlyForOddPayloadLengths()
    {
        List<byte> odd = new List<byte>();
        List<byte> even = new List<byte>();

        WebpEndianWriter.AppendChunkPayload(odd, new byte[] { 1, 2, 3 });
        WebpEndianWriter.AppendChunkPayload(even, new byte[] { 1, 2 });

        Assert.That(odd.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
        Assert.That(even.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
    }

    [Test]
    public void Writers_RejectRangesBeyondOutput()
    {
        byte[] data = new byte[3];

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.WriteByte(data, -1, 0));
        Assert.Throws<ArgumentException>(() => WebpEndianWriter.WriteByte(data, 3, 0));
        Assert.Throws<ArgumentException>(() => WebpEndianWriter.WriteUInt16(data, 2, 0));
        Assert.Throws<ArgumentException>(() => WebpEndianWriter.WriteUInt24(data, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpEndianWriter.WriteUInt32(data, 0, 0));
        Assert.Throws<ArgumentException>(() => WebpEndianWriter.WriteBytes(data, 2, new byte[] { 1, 2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.WriteBytes(data, 4, new byte[0]));
    }

    [Test]
    public void Writers_RejectValuesOutside24BitRanges()
    {
        byte[] data = new byte[3];
        List<byte> list = new List<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.WriteUInt24(data, 0, 0x1000000U));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.AppendUInt24(list, 0x1000000U));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.WriteInt24(data, 0, 8388608));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.WriteInt24(data, 0, -8388609));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.AppendInt24(list, 8388608));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.AppendInt24(list, -8388609));
    }

    [Test]
    public void AppendPadding_RejectsNegativePayloadLength()
    {
        List<byte> data = new List<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpEndianWriter.AppendPadding(data, -1));
    }
}
