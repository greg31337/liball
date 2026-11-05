using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class BmffByteReaderTests
{
    [Test]
    public void NumericReads_DecodeBigEndianSignedAndUnsignedValues()
    {
        BmffByteReader reader = new BmffByteReader(new byte[]
        {
            0x7f,
            0x12, 0x34,
            0x89, 0xab, 0xcd, 0xef,
            0xff, 0xff, 0xff, 0xfe
        });

        int value8 = reader.ReadByte();
        uint value16 = reader.ReadUnsigned16();
        uint value32 = reader.ReadUnsigned32();
        int signed32 = reader.ReadSigned32();

        Assert.That(value8, Is.EqualTo(0x7f));
        Assert.That(value16, Is.EqualTo(0x1234));
        Assert.That(value32, Is.EqualTo(0x89abcdef));
        Assert.That(signed32, Is.EqualTo(-2));
        Assert.That(reader.Remaining(), Is.EqualTo(0));
        Assert.That(reader.Valid, Is.True);
    }

    [Test]
    public void ReadUnsigned_SupportsZeroThroughEightBytes()
    {
        BmffByteReader reader = new BmffByteReader(new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
        });

        ulong zero = reader.ReadUnsigned(0);
        ulong value = reader.ReadUnsigned(8);

        Assert.That(zero, Is.EqualTo(0));
        Assert.That(value, Is.EqualTo(0x0123456789abcdef));
        Assert.That(reader.Position, Is.EqualTo(8));
        Assert.That(reader.Valid, Is.True);
    }

    [Test]
    public void TextReads_DecodeFourCcLatin1AndNullTerminatedValues()
    {
        BmffByteReader reader = new BmffByteReader(new byte[]
        {
            0x6d, 0x65, 0x74, 0x61,
            0x43, 0x61, 0x66, 0xe9, 0x00,
            0xaa, 0xbb
        });

        string fourCc = reader.ReadFourCc();
        string text = reader.ReadNullTerminatedLatin1();
        byte[] remaining = reader.ReadRemainingBytes();

        Assert.That(fourCc, Is.EqualTo("meta"));
        Assert.That(text, Is.EqualTo("Caf\u00e9"));
        Assert.That(remaining, Is.EqualTo(new byte[] { 0xaa, 0xbb }));
        Assert.That(reader.Position, Is.EqualTo(11));
        Assert.That(reader.Valid, Is.True);
    }

    [Test]
    public void TruncatedRead_InvalidatesReaderWithoutAdvancing()
    {
        BmffByteReader reader = new BmffByteReader(new byte[] { 1, 2, 3 });

        uint value = reader.ReadUnsigned32();

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(reader.Remaining(), Is.EqualTo(3));
        Assert.That(reader.Valid, Is.False);
    }

    [TestCase(-1)]
    [TestCase(9)]
    public void ReadUnsigned_RejectsUnsupportedWidths(int count)
    {
        BmffByteReader reader = new BmffByteReader(new byte[9]);

        ulong value = reader.ReadUnsigned(count);

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(reader.Valid, Is.False);
    }

    [Test]
    public void UnterminatedString_ReturnsAvailableTextAndInvalidatesReader()
    {
        BmffByteReader reader = new BmffByteReader(new byte[] { 0x61, 0x62, 0x63 });

        string value = reader.ReadNullTerminatedLatin1();

        Assert.That(value, Is.EqualTo("abc"));
        Assert.That(reader.Position, Is.EqualTo(3));
        Assert.That(reader.Valid, Is.False);
    }

    [Test]
    public void NullInput_BehavesAsEmptyBuffer()
    {
        BmffByteReader reader = new BmffByteReader(null);

        byte[] remaining = reader.ReadRemainingBytes();
        int value = reader.ReadByte();

        Assert.That(remaining, Is.Empty);
        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.Position, Is.EqualTo(0));
        Assert.That(reader.Valid, Is.False);
    }
}
