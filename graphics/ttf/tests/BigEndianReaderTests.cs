using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class BigEndianReaderTests
{
    [Test]
    public void ReadsUnsignedAndSignedEightBitExtrema()
    {
        BigEndianReader reader = CreateReader(new byte[] { 0x00, 0xFF, 0x80, 0x7F });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt8(), Is.EqualTo(byte.MinValue));
            Assert.That(reader.ReadUInt8(), Is.EqualTo(byte.MaxValue));
            Assert.That(reader.ReadInt8(), Is.EqualTo(sbyte.MinValue));
            Assert.That(reader.ReadInt8(), Is.EqualTo(sbyte.MaxValue));
        });
    }

    [Test]
    public void ReadsUnsignedAndSignedSixteenBitExtrema()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x00, 0x00,
            0xFF, 0xFF,
            0x80, 0x00,
            0x7F, 0xFF
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MinValue));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MaxValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MinValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MaxValue));
        });
    }

    [Test]
    public void ReadsUnsignedAndSignedTwentyFourBitExtrema()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF,
            0x80, 0x00, 0x00,
            0x7F, 0xFF, 0xFF
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt24(), Is.EqualTo(0U));
            Assert.That(reader.ReadUInt24(), Is.EqualTo(16777215U));
            Assert.That(reader.ReadInt24(), Is.EqualTo(-8388608));
            Assert.That(reader.ReadInt24(), Is.EqualTo(8388607));
        });
    }

    [Test]
    public void ReadsUnsignedAndSignedThirtyTwoBitExtrema()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x80, 0x00, 0x00, 0x00,
            0x7F, 0xFF, 0xFF, 0xFF
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MinValue));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MaxValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void ReadsUnsignedAndSignedSixtyFourBitExtrema()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MinValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MaxValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MinValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void ReadsArbitraryBigEndianPatternsAtEveryMultiByteWidth()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x01, 0x23,
            0x45, 0x67, 0x89,
            0xAB, 0xCD, 0xEF, 0x01,
            0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x01
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt16(), Is.EqualTo(0x0123));
            Assert.That(reader.ReadUInt24(), Is.EqualTo(0x456789U));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0xABCDEF01U));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(0x23456789ABCDEF01UL));
        });
    }

    [Test]
    public void ReadsSignedSixteenDotSixteenValues()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x00, 0x01, 0x80, 0x00,
            0xFF, 0xFE, 0x80, 0x00,
            0x7F, 0xFF, 0xFF, 0xFF,
            0x80, 0x00, 0x00, 0x00
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadFixed16Dot16(), Is.EqualTo(1.5));
            Assert.That(reader.ReadFixed16Dot16(), Is.EqualTo(-1.5));
            Assert.That(reader.ReadFixed16Dot16(), Is.EqualTo(32767.99998474121));
            Assert.That(reader.ReadFixed16Dot16(), Is.EqualTo(-32768.0));
        });
    }

    [Test]
    public void ReadsSignedTwoDotFourteenValues()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x80, 0x00,
            0xA0, 0x00,
            0x00, 0x00,
            0x7F, 0xFF
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadF2Dot14(), Is.EqualTo(-2.0));
            Assert.That(reader.ReadF2Dot14(), Is.EqualTo(-1.5));
            Assert.That(reader.ReadF2Dot14(), Is.EqualTo(0.0));
            Assert.That(reader.ReadF2Dot14(), Is.EqualTo(1.99993896484375));
        });
    }

    [Test]
    public void ReadsTagsAndOffsetsWithoutSignLoss()
    {
        BigEndianReader reader = CreateReader(new byte[]
        {
            0x68, 0x65, 0x61, 0x64,
            0xFF, 0xFF,
            0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF
        });

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadTag(), Is.EqualTo(FontTags.Head));
            Assert.That(reader.ReadOffset16(), Is.EqualTo(65535));
            Assert.That(reader.ReadOffset24(), Is.EqualTo(16777215));
            Assert.That(reader.ReadOffset32(), Is.EqualTo(4294967295L));
        });
    }

    [Test]
    public void ReadsLongDateTimeSecondsAndUtcDates()
    {
        BigEndianReader secondsReader = CreateReader(EncodeInt64(-1));
        BigEndianReader epochReader = CreateReader(EncodeInt64(0));
        BigEndianReader beforeEpochReader = CreateReader(EncodeInt64(-1));
        BigEndianReader afterEpochReader = CreateReader(EncodeInt64(2082844800L));

        Assert.Multiple(() =>
        {
            Assert.That(secondsReader.ReadLongDateTimeSeconds(), Is.EqualTo(-1));
            Assert.That(
                epochReader.ReadLongDateTime(),
                Is.EqualTo(new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(
                beforeEpochReader.ReadLongDateTime(),
                Is.EqualTo(new DateTime(1903, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
            Assert.That(
                afterEpochReader.ReadLongDateTime(),
                Is.EqualTo(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        });
    }

    [Test]
    public void ReadsLongDateTimeDateRangeBoundaries()
    {
        BigEndianReader minimumReader = CreateReader(EncodeInt64(-60052752000L));
        BigEndianReader maximumReader = CreateReader(EncodeInt64(255485145599L));

        DateTime minimum = minimumReader.ReadLongDateTime();
        DateTime maximum = maximumReader.ReadLongDateTime();

        Assert.Multiple(() =>
        {
            Assert.That(minimum, Is.EqualTo(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)));
            Assert.That(maximum, Is.EqualTo(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
        });
    }

    [TestCase(-60052752001L)]
    [TestCase(255485145600L)]
    public void RejectsLongDateTimeOutsideDateRange(long seconds)
    {
        BigEndianReader reader = CreateReader(EncodeInt64(seconds));

        Assert.Throws<FontFormatException>(() => reader.ReadLongDateTime());
    }

    [Test]
    public void TracksRelativeAbsoluteAndRemainingPositions()
    {
        FontData data = FontData.Create(new byte[] { 0, 1, 2, 3, 4, 5 });
        BigEndianReader reader = new BigEndianReader(data.GetSlice(2, 4));

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
            Assert.That(reader.GetAbsolutePosition(), Is.EqualTo(2));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(4));
        });

        Assert.That(reader.ReadUInt16(), Is.EqualTo(0x0203));

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetPosition(), Is.EqualTo(2));
            Assert.That(reader.GetAbsolutePosition(), Is.EqualTo(4));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(2));
        });
    }

    [Test]
    public void TruncationAfterSuccessfulReadPreservesCurrentPosition()
    {
        BigEndianReader reader = CreateReader(new byte[] { 0x12, 0x34, 0x56 });
        Assert.That(reader.ReadUInt8(), Is.EqualTo(0x12));

        Assert.Throws<FontFormatException>(() => reader.ReadUInt32());

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetPosition(), Is.EqualTo(1));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(2));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(0x3456));
        });
    }

    [TestCase("UInt8", 1)]
    [TestCase("Int8", 1)]
    [TestCase("UInt16", 2)]
    [TestCase("Int16", 2)]
    [TestCase("F2Dot14", 2)]
    [TestCase("Offset16", 2)]
    [TestCase("UInt24", 3)]
    [TestCase("Int24", 3)]
    [TestCase("Offset24", 3)]
    [TestCase("UInt32", 4)]
    [TestCase("Int32", 4)]
    [TestCase("Fixed16Dot16", 4)]
    [TestCase("Tag", 4)]
    [TestCase("Offset32", 4)]
    [TestCase("UInt64", 8)]
    [TestCase("Int64", 8)]
    [TestCase("LongDateTimeSeconds", 8)]
    [TestCase("LongDateTime", 8)]
    public void RejectsEveryTruncationPointWithoutAdvancing(string operation, int width)
    {
        int length = 0;

        while (length < width)
        {
            BigEndianReader reader = CreateReader(new byte[length]);

            Assert.Throws<FontFormatException>(() => ReadOperation(reader, operation));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
            length += 1;
        }
    }

    [Test]
    public void ConstructorRejectsNullSlice()
    {
        Assert.Throws<ArgumentNullException>(() => new BigEndianReader(null!));
    }

    private static BigEndianReader CreateReader(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new BigEndianReader(data.GetSlice(0, data.GetLength()));
    }

    private static byte[] EncodeInt64(long value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        return bytes;
    }

    private static void ReadOperation(BigEndianReader reader, string operation)
    {
        switch (operation)
        {
            case "UInt8":
                reader.ReadUInt8();
                break;
            case "Int8":
                reader.ReadInt8();
                break;
            case "UInt16":
                reader.ReadUInt16();
                break;
            case "Int16":
                reader.ReadInt16();
                break;
            case "F2Dot14":
                reader.ReadF2Dot14();
                break;
            case "Offset16":
                reader.ReadOffset16();
                break;
            case "UInt24":
                reader.ReadUInt24();
                break;
            case "Int24":
                reader.ReadInt24();
                break;
            case "Offset24":
                reader.ReadOffset24();
                break;
            case "UInt32":
                reader.ReadUInt32();
                break;
            case "Int32":
                reader.ReadInt32();
                break;
            case "Fixed16Dot16":
                reader.ReadFixed16Dot16();
                break;
            case "Tag":
                reader.ReadTag();
                break;
            case "Offset32":
                reader.ReadOffset32();
                break;
            case "UInt64":
                reader.ReadUInt64();
                break;
            case "Int64":
                reader.ReadInt64();
                break;
            case "LongDateTimeSeconds":
                reader.ReadLongDateTimeSeconds();
                break;
            case "LongDateTime":
                reader.ReadLongDateTime();
                break;
            default:
                throw new InvalidOperationException(operation);
        }
    }
}
