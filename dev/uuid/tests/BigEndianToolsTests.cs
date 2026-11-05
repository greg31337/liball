using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class BigEndianToolsTests
{
    [Test]
    public void ReadMethods_ReadUnsignedBigEndianValues()
    {
        byte[] bytes = new byte[]
        {
            0x00,
            0x12,
            0x34,
            0x56,
            0x78,
            0x9A,
            0xBC,
            0xDE,
            0xF0
        };

        Assert.That(BigEndianTools.ReadUInt16(bytes, 1), Is.EqualTo(0x1234));
        Assert.That(BigEndianTools.ReadUInt24(bytes, 1), Is.EqualTo(0x123456));
        Assert.That(BigEndianTools.ReadUInt32(bytes, 1), Is.EqualTo(0x12345678));
        Assert.That(BigEndianTools.ReadUInt48(bytes, 1), Is.EqualTo(0x123456789ABC));
        Assert.That(BigEndianTools.ReadUInt64(bytes, 1), Is.EqualTo(0x123456789ABCDEF0UL));
    }

    [Test]
    public void WriteMethods_WriteUnsignedBigEndianValuesAtOffset()
    {
        byte[] bytes = new byte[25];

        BigEndianTools.WriteUInt16(bytes, 1, 0x1234);
        BigEndianTools.WriteUInt24(bytes, 4, 0x56789A);
        BigEndianTools.WriteUInt32(bytes, 8, 0xBCDEF012U);
        BigEndianTools.WriteUInt48(bytes, 13, 0x3456789ABCD);
        BigEndianTools.WriteUInt64(bytes, 17, 0x123456789ABCDEF0UL);

        Assert.That(bytes[0], Is.EqualTo(0));
        Assert.That(bytes[1], Is.EqualTo(0x12));
        Assert.That(bytes[2], Is.EqualTo(0x34));
        Assert.That(bytes[4], Is.EqualTo(0x56));
        Assert.That(bytes[5], Is.EqualTo(0x78));
        Assert.That(bytes[6], Is.EqualTo(0x9A));
        Assert.That(bytes[8], Is.EqualTo(0xBC));
        Assert.That(bytes[9], Is.EqualTo(0xDE));
        Assert.That(bytes[10], Is.EqualTo(0xF0));
        Assert.That(bytes[11], Is.EqualTo(0x12));
        Assert.That(bytes[13], Is.EqualTo(0x03));
        Assert.That(bytes[14], Is.EqualTo(0x45));
        Assert.That(bytes[15], Is.EqualTo(0x67));
        Assert.That(bytes[16], Is.EqualTo(0x89));
        Assert.That(bytes[17], Is.EqualTo(0x12));
        Assert.That(bytes[18], Is.EqualTo(0x34));
        Assert.That(bytes[19], Is.EqualTo(0x56));
        Assert.That(bytes[20], Is.EqualTo(0x78));
        Assert.That(bytes[21], Is.EqualTo(0x9A));
        Assert.That(bytes[22], Is.EqualTo(0xBC));
        Assert.That(bytes[23], Is.EqualTo(0xDE));
        Assert.That(bytes[24], Is.EqualTo(0xF0));
    }

    [Test]
    public void ReadAndWriteUInt48_PreserveMaximumValue()
    {
        byte[] bytes = new byte[6];

        BigEndianTools.WriteUInt48(bytes, 0, BigEndianTools.MaxUInt48);

        Assert.That(bytes, Is.EqualTo(new byte[] { 255, 255, 255, 255, 255, 255 }));
        Assert.That(BigEndianTools.ReadUInt48(bytes, 0), Is.EqualTo(BigEndianTools.MaxUInt48));
    }

    [Test]
    public void ReadMethods_RejectInvalidOffsetOrWidth()
    {
        byte[] bytes = new byte[2];

        Assert.That(() => BigEndianTools.ReadUInt16(bytes, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => BigEndianTools.ReadUInt24(bytes, 0), Throws.ArgumentException);
        Assert.That(() => BigEndianTools.ReadUInt64(null, 0), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void WriteMethods_RejectValuesOutsideFieldWidth()
    {
        byte[] bytes = new byte[8];

        Assert.That(() => BigEndianTools.WriteUInt16(bytes, 0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => BigEndianTools.WriteUInt16(bytes, 0, 65536), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => BigEndianTools.WriteUInt24(bytes, 0, 16777216), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => BigEndianTools.WriteUInt48(bytes, 0, BigEndianTools.MaxUInt48 + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
