using ttf;

namespace ttf.Tests;

internal class Woff2IntegerTests
{
    [Test]
    public void ReadsEverySingleByteUIntBase128Value()
    {
        for (int value = 0; value <= 127; value += 1)
        {
            BigEndianReader reader = CreateReader(new byte[] { (byte)value });

            Assert.Multiple(() =>
            {
                Assert.That(Woff2Integer.ReadUIntBase128(reader), Is.EqualTo((uint)value));
                Assert.That(reader.GetPosition(), Is.EqualTo(1));
                Assert.That(reader.GetRemainingLength(), Is.Zero);
            });
        }
    }

    [Test]
    public void ReadsCanonicalMultiByteUIntBase128Boundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ReadUIntBase128(0x81, 0x00), Is.EqualTo(128U));
            Assert.That(ReadUIntBase128(0x81, 0x7F), Is.EqualTo(255U));
            Assert.That(ReadUIntBase128(0xFF, 0x7F), Is.EqualTo(16383U));
            Assert.That(ReadUIntBase128(0x81, 0x80, 0x00), Is.EqualTo(16384U));
            Assert.That(ReadUIntBase128(0xFF, 0xFF, 0x7F), Is.EqualTo(2097151U));
            Assert.That(ReadUIntBase128(0x81, 0x80, 0x80, 0x00), Is.EqualTo(2097152U));
            Assert.That(ReadUIntBase128(0xFF, 0xFF, 0xFF, 0x7F), Is.EqualTo(268435455U));
            Assert.That(ReadUIntBase128(0x81, 0x80, 0x80, 0x80, 0x00), Is.EqualTo(268435456U));
            Assert.That(ReadUIntBase128(0x8F, 0xFF, 0xFF, 0xFF, 0x7F), Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void StopsUIntBase128AtItsTerminator()
    {
        BigEndianReader reader = CreateReader(new byte[] { 0x81, 0x00, 0xAB });

        uint value = Woff2Integer.ReadUIntBase128(reader);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(128U));
            Assert.That(reader.GetPosition(), Is.EqualTo(2));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(1));
            Assert.That(reader.ReadUInt8(), Is.EqualTo(0xAB));
        });
    }

    [Test]
    public void RejectsNonMinimalUIntBase128LeadingZero()
    {
        BigEndianReader reader = CreateReader(new byte[] { 0x80, 0x01 });

        FontFormatException? exception = Assert.Throws<FontFormatException>(
            () => Woff2Integer.ReadUIntBase128(reader));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("leading zero"));
            Assert.That(reader.GetPosition(), Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsUIntBase128LongerThanFiveBytes()
    {
        BigEndianReader reader = CreateReader(
            new byte[] { 0x81, 0x80, 0x80, 0x80, 0x80, 0x00 });

        FontFormatException? exception = Assert.Throws<FontFormatException>(
            () => Woff2Integer.ReadUIntBase128(reader));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("five-byte"));
            Assert.That(reader.GetPosition(), Is.EqualTo(5));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsUIntBase128Overflow()
    {
        BigEndianReader reader = CreateReader(
            new byte[] { 0x90, 0x80, 0x80, 0x80, 0x00 });

        FontFormatException? exception = Assert.Throws<FontFormatException>(
            () => Woff2Integer.ReadUIntBase128(reader));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("UInt32 range"));
            Assert.That(reader.GetPosition(), Is.EqualTo(5));
        });
    }

    [TestCase(new byte[] { 0x81 })]
    [TestCase(new byte[] { 0x81, 0x80 })]
    [TestCase(new byte[] { 0x81, 0x80, 0x80 })]
    [TestCase(new byte[] { 0x81, 0x80, 0x80, 0x80 })]
    public void RejectsTruncatedUIntBase128(byte[] encoded)
    {
        Assert.Throws<FontFormatException>(
            () => Woff2Integer.ReadUIntBase128(CreateReader(encoded)));
    }

    [Test]
    public void ReadsEveryDirect255UInt16Value()
    {
        for (int value = 0; value <= 252; value += 1)
        {
            BigEndianReader reader = CreateReader(new byte[] { (byte)value });

            Assert.Multiple(() =>
            {
                Assert.That(Woff2Integer.Read255UInt16(reader), Is.EqualTo(value));
                Assert.That(reader.GetPosition(), Is.EqualTo(1));
            });
        }
    }

    [Test]
    public void ReadsEveryOneAdditionalByte255UInt16Value()
    {
        for (int suffix = 0; suffix <= 255; suffix += 1)
        {
            BigEndianReader code255Reader = CreateReader(new byte[] { 255, (byte)suffix });
            BigEndianReader code254Reader = CreateReader(new byte[] { 254, (byte)suffix });

            Assert.Multiple(() =>
            {
                Assert.That(Woff2Integer.Read255UInt16(code255Reader), Is.EqualTo(253 + suffix));
                Assert.That(Woff2Integer.Read255UInt16(code254Reader), Is.EqualTo(506 + suffix));
                Assert.That(code255Reader.GetPosition(), Is.EqualTo(2));
                Assert.That(code254Reader.GetPosition(), Is.EqualTo(2));
            });
        }
    }

    [Test]
    public void ReadsWordEncoded255UInt16Boundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Read255UInt16(253, 0x00, 0x00), Is.Zero);
            Assert.That(Read255UInt16(253, 0x00, 0xFC), Is.EqualTo(252));
            Assert.That(Read255UInt16(253, 0x00, 0xFD), Is.EqualTo(253));
            Assert.That(Read255UInt16(253, 0x01, 0xFA), Is.EqualTo(506));
            Assert.That(Read255UInt16(253, 0xFF, 0xFF), Is.EqualTo(65535));
        });
    }

    [Test]
    public void AcceptsAllNonUnique255UInt16Encodings()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Read255UInt16(255, 253), Is.EqualTo(506));
            Assert.That(Read255UInt16(254, 0), Is.EqualTo(506));
            Assert.That(Read255UInt16(253, 1, 250), Is.EqualTo(506));
            Assert.That(Read255UInt16(255, 255), Is.EqualTo(508));
            Assert.That(Read255UInt16(254, 2), Is.EqualTo(508));
            Assert.That(Read255UInt16(253, 1, 252), Is.EqualTo(508));
        });
    }

    [TestCase(new byte[] { })]
    [TestCase(new byte[] { 253 })]
    [TestCase(new byte[] { 253, 0 })]
    [TestCase(new byte[] { 254 })]
    [TestCase(new byte[] { 255 })]
    public void RejectsTruncated255UInt16(byte[] encoded)
    {
        Assert.Throws<FontFormatException>(
            () => Woff2Integer.Read255UInt16(CreateReader(encoded)));
    }

    [Test]
    public void Stops255UInt16AfterItsEncodedValue()
    {
        BigEndianReader reader = CreateReader(new byte[] { 253, 0x12, 0x34, 0xAB });

        int value = Woff2Integer.Read255UInt16(reader);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(0x1234));
            Assert.That(reader.GetPosition(), Is.EqualTo(3));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(1));
            Assert.That(reader.ReadUInt8(), Is.EqualTo(0xAB));
        });
    }

    [Test]
    public void RejectsNullReaders()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => Woff2Integer.ReadUIntBase128(null!));
            Assert.Throws<ArgumentNullException>(() => Woff2Integer.Read255UInt16(null!));
        });
    }

    static uint ReadUIntBase128(params byte[] encoded)
    {
        return Woff2Integer.ReadUIntBase128(CreateReader(encoded));
    }

    static int Read255UInt16(params byte[] encoded)
    {
        return Woff2Integer.Read255UInt16(CreateReader(encoded));
    }

    static BigEndianReader CreateReader(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new BigEndianReader(data.GetSlice(0, data.GetLength()));
    }
}
