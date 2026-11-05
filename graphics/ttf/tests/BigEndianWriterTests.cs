using ttf;

namespace ttf.Tests;

public class BigEndianWriterTests
{
    [Test]
    public void WritesExactBigEndianIntegerBytes()
    {
        BigEndianWriter writer = BigEndianWriter.Create();

        writer.WriteUInt8(0x12);
        writer.WriteInt8(-2);
        writer.WriteUInt16(0x3456);
        writer.WriteInt16(-3);
        writer.WriteUInt24(0x789ABC);
        writer.WriteInt24(-4);
        writer.WriteUInt32(0xDEF01234U);
        writer.WriteInt32(-5);
        writer.WriteUInt64(0x56789ABCDEF01234UL);
        writer.WriteInt64(-6);

        Assert.That(writer.CopyBytes(), Is.EqualTo(new byte[]
        {
            0x12,
            0xFE,
            0x34, 0x56,
            0xFF, 0xFD,
            0x78, 0x9A, 0xBC,
            0xFF, 0xFF, 0xFC,
            0xDE, 0xF0, 0x12, 0x34,
            0xFF, 0xFF, 0xFF, 0xFB,
            0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0x12, 0x34,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFA
        }));
    }

    [Test]
    public void ScalarWritesRoundTripThroughReader()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteUInt8(byte.MaxValue);
        writer.WriteInt8(sbyte.MinValue);
        writer.WriteUInt16(ushort.MaxValue);
        writer.WriteInt16(short.MinValue);
        writer.WriteUInt24(16777215U);
        writer.WriteInt24(-8388608);
        writer.WriteUInt32(uint.MaxValue);
        writer.WriteInt32(int.MinValue);
        writer.WriteUInt64(ulong.MaxValue);
        writer.WriteInt64(long.MinValue);
        writer.WriteFixed16Dot16(-123.5);
        writer.WriteF2Dot14(1.25);
        writer.WriteTag(FontTags.Cmap);
        writer.WriteOffset16(65535);
        writer.WriteOffset24(16777215);
        writer.WriteOffset32(4294967295L);
        writer.WriteLongDateTimeSeconds(-2082844800L);

        BigEndianReader reader = CreateReader(writer.CopyBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt8(), Is.EqualTo(byte.MaxValue));
            Assert.That(reader.ReadInt8(), Is.EqualTo(sbyte.MinValue));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MaxValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MinValue));
            Assert.That(reader.ReadUInt24(), Is.EqualTo(16777215U));
            Assert.That(reader.ReadInt24(), Is.EqualTo(-8388608));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MaxValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MaxValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MinValue));
            Assert.That(reader.ReadFixed16Dot16(), Is.EqualTo(-123.5));
            Assert.That(reader.ReadF2Dot14(), Is.EqualTo(1.25));
            Assert.That(reader.ReadTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That(reader.ReadOffset16(), Is.EqualTo(65535));
            Assert.That(reader.ReadOffset24(), Is.EqualTo(16777215));
            Assert.That(reader.ReadOffset32(), Is.EqualTo(4294967295L));
            Assert.That(reader.ReadLongDateTimeSeconds(), Is.EqualTo(-2082844800L));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(0));
        });
    }

    [Test]
    public void WritesBothBoundariesOfEveryIntegerEncoding()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteUInt8(byte.MinValue);
        writer.WriteUInt8(byte.MaxValue);
        writer.WriteInt8(sbyte.MinValue);
        writer.WriteInt8(sbyte.MaxValue);
        writer.WriteUInt16(ushort.MinValue);
        writer.WriteUInt16(ushort.MaxValue);
        writer.WriteInt16(short.MinValue);
        writer.WriteInt16(short.MaxValue);
        writer.WriteUInt24(0);
        writer.WriteUInt24(16777215);
        writer.WriteInt24(-8388608);
        writer.WriteInt24(8388607);
        writer.WriteUInt32(uint.MinValue);
        writer.WriteUInt32(uint.MaxValue);
        writer.WriteInt32(int.MinValue);
        writer.WriteInt32(int.MaxValue);
        writer.WriteUInt64(ulong.MinValue);
        writer.WriteUInt64(ulong.MaxValue);
        writer.WriteInt64(long.MinValue);
        writer.WriteInt64(long.MaxValue);

        BigEndianReader reader = CreateReader(writer.CopyBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUInt8(), Is.EqualTo(byte.MinValue));
            Assert.That(reader.ReadUInt8(), Is.EqualTo(byte.MaxValue));
            Assert.That(reader.ReadInt8(), Is.EqualTo(sbyte.MinValue));
            Assert.That(reader.ReadInt8(), Is.EqualTo(sbyte.MaxValue));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MinValue));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(ushort.MaxValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MinValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MaxValue));
            Assert.That(reader.ReadUInt24(), Is.EqualTo(0U));
            Assert.That(reader.ReadUInt24(), Is.EqualTo(16777215U));
            Assert.That(reader.ReadInt24(), Is.EqualTo(-8388608));
            Assert.That(reader.ReadInt24(), Is.EqualTo(8388607));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MinValue));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(uint.MaxValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MaxValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MinValue));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(ulong.MaxValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MinValue));
            Assert.That(reader.ReadInt64(), Is.EqualTo(long.MaxValue));
            Assert.That(reader.GetRemainingLength(), Is.EqualTo(0));
        });
    }

    [Test]
    public void WritesAcceptedFixedPointBoundariesExactly()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteFixed16Dot16(-32768.0);
        writer.WriteFixed16Dot16(32767.9999847412109375);
        writer.WriteF2Dot14(-2.0);
        writer.WriteF2Dot14(1.99993896484375);

        BigEndianReader reader = CreateReader(writer.CopyBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MinValue));
            Assert.That(reader.ReadInt32(), Is.EqualTo(int.MaxValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MinValue));
            Assert.That(reader.ReadInt16(), Is.EqualTo(short.MaxValue));
        });
    }

    [Test]
    public void WritesBothBoundariesOfEveryOffsetEncoding()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteOffset16(0);
        writer.WriteOffset16(65535);
        writer.WriteOffset24(0);
        writer.WriteOffset24(16777215);
        writer.WriteOffset32(0);
        writer.WriteOffset32(4294967295L);

        BigEndianReader reader = CreateReader(writer.CopyBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadOffset16(), Is.EqualTo(0));
            Assert.That(reader.ReadOffset16(), Is.EqualTo(65535));
            Assert.That(reader.ReadOffset24(), Is.EqualTo(0));
            Assert.That(reader.ReadOffset24(), Is.EqualTo(16777215));
            Assert.That(reader.ReadOffset32(), Is.EqualTo(0));
            Assert.That(reader.ReadOffset32(), Is.EqualTo(4294967295L));
        });
    }

    [Test]
    public void FixedPointWritesRoundHalfAwayFromZero()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteFixed16Dot16(0.5 / 65536.0);
        writer.WriteFixed16Dot16(-0.5 / 65536.0);
        writer.WriteF2Dot14(0.5 / 16384.0);
        writer.WriteF2Dot14(-0.5 / 16384.0);

        BigEndianReader reader = CreateReader(writer.CopyBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadInt32(), Is.EqualTo(1));
            Assert.That(reader.ReadInt32(), Is.EqualTo(-1));
            Assert.That(reader.ReadInt16(), Is.EqualTo(1));
            Assert.That(reader.ReadInt16(), Is.EqualTo(-1));
        });
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(-32768.00002)]
    [TestCase(32768.0)]
    public void FixedSixteenDotSixteenRejectsUnrepresentableValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteFixed16Dot16(value));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(-2.0001)]
    [TestCase(2.0)]
    public void F2Dot14RejectsUnrepresentableValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteF2Dot14(value));
    }

    [TestCase(16777216U)]
    [TestCase(uint.MaxValue)]
    public void UInt24RejectsOutOfRangeValue(uint value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteUInt24(value));
    }

    [TestCase(-8388609)]
    [TestCase(8388608)]
    public void Int24RejectsOutOfRangeValue(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteInt24(value));
    }

    [Test]
    public void OffsetWritesRejectOutOfRangeValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteOffset16(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteOffset16(65536));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteOffset24(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteOffset24(16777216));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteOffset32(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create().WriteOffset32(4294967296L));
        });
    }

    [Test]
    public void WritesAndReadsUtcLongDateTime()
    {
        DateTime value = new DateTime(2026, 8, 17, 12, 34, 56, DateTimeKind.Utc);
        BigEndianWriter writer = BigEndianWriter.Create();

        writer.WriteLongDateTime(value);

        Assert.That(CreateReader(writer.CopyBytes()).ReadLongDateTime(), Is.EqualTo(value));
    }

    [Test]
    public void WritesUtcLongDateTimeBoundaries()
    {
        DateTime minimum = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        DateTime maximumWholeSecond = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteLongDateTime(minimum);
        writer.WriteLongDateTime(maximumWholeSecond);

        BigEndianReader reader = CreateReader(writer.CopyBytes());

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadLongDateTime(), Is.EqualTo(minimum));
            Assert.That(reader.ReadLongDateTime(), Is.EqualTo(maximumWholeSecond));
        });
    }

    [Test]
    public void LongDateTimeRejectsNonUtcAndFractionalSecondValues()
    {
        BigEndianWriter writer = BigEndianWriter.Create();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => writer.WriteLongDateTime(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.Throws<ArgumentException>(() => writer.WriteLongDateTime(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)));
            Assert.Throws<ArgumentException>(() => writer.WriteLongDateTime(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1)));
        });
    }

    [Test]
    public void BufferGrowthDoublesDeterministically()
    {
        BigEndianWriter writer = BigEndianWriter.Create(2, 20);

        writer.WriteUInt16(1);
        Assert.That(writer.GetCapacity(), Is.EqualTo(2));

        writer.WriteUInt8(2);
        Assert.That(writer.GetCapacity(), Is.EqualTo(4));

        writer.WriteUInt16(3);
        Assert.That(writer.GetCapacity(), Is.EqualTo(8));

        writer.WriteUInt32(4);
        Assert.That(writer.GetCapacity(), Is.EqualTo(16));

        writer.WriteUInt64(5);
        Assert.Multiple(() =>
        {
            Assert.That(writer.GetCapacity(), Is.EqualTo(20));
            Assert.That(writer.GetLength(), Is.EqualTo(17));
        });
    }

    [Test]
    public void ZeroCapacityWriterGrowsUsingDeterministicSeed()
    {
        BigEndianWriter writer = BigEndianWriter.Create(0, 1000);

        writer.WriteUInt8(1);

        Assert.That(writer.GetCapacity(), Is.EqualTo(256));
    }

    [Test]
    public void MaximumLengthFailureDoesNotChangeWriterState()
    {
        BigEndianWriter writer = BigEndianWriter.Create(2, 5);
        writer.WriteUInt32(0x01020304);
        byte[] before = writer.CopyBytes();
        int capacity = writer.GetCapacity();

        Assert.Throws<FontLimitException>(() => writer.WriteUInt16(0x0506));

        Assert.Multiple(() =>
        {
            Assert.That(writer.GetLength(), Is.EqualTo(4));
            Assert.That(writer.GetCapacity(), Is.EqualTo(capacity));
            Assert.That(writer.CopyBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void OversizedByteWriteDoesNotChangeWriterState()
    {
        BigEndianWriter writer = BigEndianWriter.Create(1, 4);
        writer.WriteUInt8(0x12);
        byte[] before = writer.CopyBytes();

        Assert.Throws<FontLimitException>(() => writer.WriteBytes(new byte[4]));

        Assert.Multiple(() =>
        {
            Assert.That(writer.GetLength(), Is.EqualTo(1));
            Assert.That(writer.CopyBytes(), Is.EqualTo(before));
        });
    }

    [TestCase(0, 0)]
    [TestCase(1, 3)]
    [TestCase(2, 2)]
    [TestCase(3, 1)]
    [TestCase(4, 0)]
    [TestCase(5, 3)]
    public void FourBytePaddingWritesExpectedZeroCount(int originalLength, int expectedPadding)
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteBytes(new byte[originalLength]);

        int actualPadding = writer.WritePaddingToFourBytes();

        Assert.Multiple(() =>
        {
            Assert.That(actualPadding, Is.EqualTo(expectedPadding));
            Assert.That(writer.GetLength() % 4, Is.EqualTo(0));
            Assert.That(writer.CopyBytes(), Is.All.EqualTo(0));
        });
    }

    [Test]
    public void WriteBytesCopiesArrayMemoryAndSpanInputs()
    {
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteBytes(source);
        writer.WriteMemory(new ReadOnlyMemory<byte>(source, 1, 2));
        writer.WriteSpan(source.AsSpan(3, 2));

        source.AsSpan().Fill(99);

        Assert.That(writer.CopyBytes(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 2, 3, 4, 5 }));
    }

    [Test]
    public void WriteBytesRejectsNullArray()
    {
        Assert.Throws<ArgumentNullException>(() => BigEndianWriter.Create().WriteBytes((byte[]?)null));
    }

    [Test]
    public void CopyBytesReturnsOnlyWrittenBytesAndIsIndependent()
    {
        BigEndianWriter writer = BigEndianWriter.Create(16, 16);
        writer.WriteUInt16(0x0102);
        byte[] first = writer.CopyBytes();
        byte[] second = writer.CopyBytes();
        first[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(first, Is.Not.SameAs(second));
        });
    }

    [Test]
    public void FactoriesValidateCapacityBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigEndianWriter.Create(0, -1));
            Assert.Throws<ArgumentException>(() => BigEndianWriter.Create(2, 1));
        });
    }

    [Test]
    public void ZeroMaximumAllowsEmptyOutputOnly()
    {
        BigEndianWriter writer = BigEndianWriter.Create(0);

        Assert.Multiple(() =>
        {
            Assert.That(writer.GetMaximumLength(), Is.EqualTo(0));
            Assert.That(writer.WritePaddingToFourBytes(), Is.EqualTo(0));
            Assert.Throws<FontLimitException>(() => writer.WriteUInt8(1));
        });
    }

    private static BigEndianReader CreateReader(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new BigEndianReader(data.GetSlice(0, data.GetLength()));
    }
}
