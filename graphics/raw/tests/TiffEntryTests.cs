using raw;

namespace raw.Tests;

public class TiffEntryTests
{
    [Test]
    public void FieldTypeTableContainsEveryStandardAndBigTiffType()
    {
        Dictionary<TiffFieldType, int> expected = new Dictionary<TiffFieldType, int>
        {
            { TiffFieldType.Byte, 1 },
            { TiffFieldType.Ascii, 1 },
            { TiffFieldType.Short, 2 },
            { TiffFieldType.Long, 4 },
            { TiffFieldType.Rational, 8 },
            { TiffFieldType.SignedByte, 1 },
            { TiffFieldType.Undefined, 1 },
            { TiffFieldType.SignedShort, 2 },
            { TiffFieldType.SignedLong, 4 },
            { TiffFieldType.SignedRational, 8 },
            { TiffFieldType.Float, 4 },
            { TiffFieldType.Double, 8 },
            { TiffFieldType.Ifd, 4 },
            { TiffFieldType.Long8, 8 },
            { TiffFieldType.SignedLong8, 8 },
            { TiffFieldType.Ifd8, 8 },
            { TiffFieldType.Utf8, 1 }
        };

        Assert.Multiple(() =>
        {
            foreach (KeyValuePair<TiffFieldType, int> item in expected)
            {
                Assert.That(TiffFieldTypes.FromCode((ushort)item.Key, 99), Is.EqualTo(item.Key));
                Assert.That(TiffFieldTypes.GetElementSize(item.Key), Is.EqualTo(item.Value));
            }
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void ClassicEntriesUseFourByteInlineBoundary(RawByteOrder byteOrder)
    {
        byte[] inlineRaw = UInt16Bytes(byteOrder, 0x1234, 0xFEDC);
        TiffEntry inline = ParseEntry(TiffFieldType.Short, 2, inlineRaw, byteOrder, false);
        byte[] referencedRaw = UInt16Bytes(byteOrder, 1, 2, 3);
        TiffEntry referenced = ParseEntry(TiffFieldType.Short, 3, referencedRaw, byteOrder, false);

        Assert.Multiple(() =>
        {
            Assert.That(inline.Inline, Is.True);
            Assert.That(inline.RawBytes, Is.EqualTo(inlineRaw));
            Assert.That(inline.ValueSlotBytes, Is.EqualTo(inlineRaw));
            Assert.That(inline.GetUInt16Values(), Is.EqualTo(new ushort[] { 0x1234, 0xFEDC }));
            Assert.That(referenced.Inline, Is.False);
            Assert.That(referenced.ValueOffset, Is.EqualTo(64));
            Assert.That(referenced.RawBytes, Is.EqualTo(referencedRaw));
            Assert.That(referenced.GetUInt16Values(), Is.EqualTo(new ushort[] { 1, 2, 3 }));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void BigTiffEntriesUseEightByteInlineBoundary(RawByteOrder byteOrder)
    {
        byte[] inlineRaw = UInt32Bytes(byteOrder, 0x12345678U, 0xFEDCBA98U);
        TiffEntry inline = ParseEntry(TiffFieldType.Long, 2, inlineRaw, byteOrder, true);
        byte[] referencedRaw = UInt16Bytes(byteOrder, 1, 2, 3, 4, 5);
        TiffEntry referenced = ParseEntry(TiffFieldType.Short, 5, referencedRaw, byteOrder, true);

        Assert.Multiple(() =>
        {
            Assert.That(inline.Inline, Is.True);
            Assert.That(inline.RawBytes, Is.EqualTo(inlineRaw));
            Assert.That(inline.GetUInt32Values(), Is.EqualTo(new uint[] { 0x12345678U, 0xFEDCBA98U }));
            Assert.That(referenced.Inline, Is.False);
            Assert.That(referenced.ValueOffset, Is.EqualTo(64));
            Assert.That(referenced.RawBytes, Is.EqualTo(referencedRaw));
        });
    }

    [Test]
    public void UnsignedIntegerAndIfdConversionsPreserveWidths()
    {
        TiffEntry bytes = ParseEntry(TiffFieldType.Byte, 2, new byte[] { 0, 255 }, RawByteOrder.LittleEndian, false);
        TiffEntry shorts = ParseEntry(TiffFieldType.Short, 2, UInt16Bytes(RawByteOrder.BigEndian, 0, 65535), RawByteOrder.BigEndian, false);
        TiffEntry longs = ParseEntry(TiffFieldType.Long, 2, UInt32Bytes(RawByteOrder.LittleEndian, 0, uint.MaxValue), RawByteOrder.LittleEndian, true);
        TiffEntry ifd = ParseEntry(TiffFieldType.Ifd, 1, UInt32Bytes(RawByteOrder.BigEndian, 0xFEDCBA98U), RawByteOrder.BigEndian, false);
        TiffEntry long8 = ParseEntry(TiffFieldType.Long8, 2, UInt64Bytes(RawByteOrder.LittleEndian, 0, ulong.MaxValue), RawByteOrder.LittleEndian, true);
        TiffEntry ifd8 = ParseEntry(TiffFieldType.Ifd8, 1, UInt64Bytes(RawByteOrder.BigEndian, 0xFEDCBA9876543210UL), RawByteOrder.BigEndian, true);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.GetByteValues(), Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(shorts.GetUInt16Values(), Is.EqualTo(new ushort[] { 0, 65535 }));
            Assert.That(longs.GetUInt32Values(), Is.EqualTo(new uint[] { 0, uint.MaxValue }));
            Assert.That(ifd.GetUInt32(), Is.EqualTo(0xFEDCBA98U));
            Assert.That(long8.GetUInt64Values(), Is.EqualTo(new ulong[] { 0, ulong.MaxValue }));
            Assert.That(ifd8.GetUInt64(), Is.EqualTo(0xFEDCBA9876543210UL));
        });
    }

    [Test]
    public void SignedIntegerConversionsPreserveTwosComplementValues()
    {
        TiffEntry signedBytes = ParseEntry(TiffFieldType.SignedByte, 3, new byte[] { 0x80, 0xFF, 0x7F }, RawByteOrder.LittleEndian, false);
        TiffEntry signedShorts = ParseEntry(TiffFieldType.SignedShort, 2, Int16Bytes(RawByteOrder.LittleEndian, short.MinValue, short.MaxValue), RawByteOrder.LittleEndian, false);
        TiffEntry signedLongs = ParseEntry(TiffFieldType.SignedLong, 2, Int32Bytes(RawByteOrder.BigEndian, int.MinValue, int.MaxValue), RawByteOrder.BigEndian, true);
        TiffEntry signedLong8 = ParseEntry(TiffFieldType.SignedLong8, 2, Int64Bytes(RawByteOrder.LittleEndian, long.MinValue, long.MaxValue), RawByteOrder.LittleEndian, true);

        Assert.Multiple(() =>
        {
            Assert.That(signedBytes.GetSignedByteValues(), Is.EqualTo(new sbyte[] { sbyte.MinValue, -1, sbyte.MaxValue }));
            Assert.That(signedShorts.GetInt16Values(), Is.EqualTo(new short[] { short.MinValue, short.MaxValue }));
            Assert.That(signedLongs.GetInt32Values(), Is.EqualTo(new int[] { int.MinValue, int.MaxValue }));
            Assert.That(signedLong8.GetInt64Values(), Is.EqualTo(new long[] { long.MinValue, long.MaxValue }));
        });
    }

    [Test]
    public void RationalConversionsRetainExactComponentsIncludingZeroDenominator()
    {
        byte[] unsignedRaw = new byte[16];
        EndianWriter.WriteUInt32(unsignedRaw.AsSpan(0, 4), 1, RawByteOrder.BigEndian);
        EndianWriter.WriteUInt32(unsignedRaw.AsSpan(4, 4), 2, RawByteOrder.BigEndian);
        EndianWriter.WriteUInt32(unsignedRaw.AsSpan(8, 4), uint.MaxValue, RawByteOrder.BigEndian);
        EndianWriter.WriteUInt32(unsignedRaw.AsSpan(12, 4), 0, RawByteOrder.BigEndian);
        byte[] signedRaw = new byte[8];
        EndianWriter.WriteInt32(signedRaw.AsSpan(0, 4), -3, RawByteOrder.LittleEndian);
        EndianWriter.WriteInt32(signedRaw.AsSpan(4, 4), 4, RawByteOrder.LittleEndian);
        TiffEntry unsigned = ParseEntry(TiffFieldType.Rational, 2, unsignedRaw, RawByteOrder.BigEndian, true);
        TiffEntry signed = ParseEntry(TiffFieldType.SignedRational, 1, signedRaw, RawByteOrder.LittleEndian, true);

        RawRational[] unsignedValues = unsigned.GetRationalValues();
        RawRational signedValue = signed.GetRational();

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValues[0].Numerator, Is.EqualTo(1));
            Assert.That(unsignedValues[0].Denominator, Is.EqualTo(2));
            Assert.That(unsignedValues[0].Signed, Is.False);
            Assert.That(unsignedValues[1].Numerator, Is.EqualTo((long)uint.MaxValue));
            Assert.That(unsignedValues[1].Denominator, Is.EqualTo(0));
            Assert.That(signedValue.Numerator, Is.EqualTo(-3));
            Assert.That(signedValue.Denominator, Is.EqualTo(4));
            Assert.That(signedValue.Signed, Is.True);
        });
    }

    [Test]
    public void FloatingConversionsPreserveIeeeValuesAndNanPayloads()
    {
        float first = -12.5F;
        float nan = BitConverter.UInt32BitsToSingle(0x7FC12345U);
        double third = BitConverter.UInt64BitsToDouble(0x7FF8123456789ABCUL);
        byte[] floatRaw = SingleBytes(RawByteOrder.BigEndian, first, nan);
        byte[] doubleRaw = DoubleBytes(RawByteOrder.LittleEndian, 1.5, third);
        TiffEntry floats = ParseEntry(TiffFieldType.Float, 2, floatRaw, RawByteOrder.BigEndian, true);
        TiffEntry doubles = ParseEntry(TiffFieldType.Double, 2, doubleRaw, RawByteOrder.LittleEndian, true);

        float[] floatValues = floats.GetSingleValues();
        double[] doubleValues = doubles.GetDoubleValues();

        Assert.Multiple(() =>
        {
            Assert.That(floatValues[0], Is.EqualTo(-12.5F));
            Assert.That(BitConverter.SingleToUInt32Bits(floatValues[1]), Is.EqualTo(0x7FC12345U));
            Assert.That(doubleValues[0], Is.EqualTo(1.5));
            Assert.That(BitConverter.DoubleToUInt64Bits(doubleValues[1]), Is.EqualTo(0x7FF8123456789ABCUL));
        });
    }

    [Test]
    public void AsciiAndUndefinedConversionsRetainEveryRawByte()
    {
        byte[] asciiRaw = new byte[] { 0x41, 0x00, 0x42, 0x00 };
        byte[] undefinedRaw = new byte[] { 0x00, 0x7F, 0x80, 0xFF, 0x12 };
        TiffEntry ascii = ParseEntry(TiffFieldType.Ascii, 4, asciiRaw, RawByteOrder.LittleEndian, false);
        TiffEntry undefined = ParseEntry(TiffFieldType.Undefined, 5, undefinedRaw, RawByteOrder.BigEndian, false);

        byte[] copy = undefined.GetUndefinedBytes();
        copy[0] = 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(ascii.GetAscii(), Is.EqualTo("A\0B\0"));
            Assert.That(ascii.RawBytes, Is.EqualTo(asciiRaw));
            Assert.That(undefined.RawBytes, Is.EqualTo(undefinedRaw));
            Assert.That(undefined.GetUndefinedBytes()[0], Is.EqualTo(0));
        });
    }

    [Test]
    public void EntryMetadataAndInlinePaddingAreRetainedExactly()
    {
        byte[] source = Enumerable.Repeat((byte)0xCC, 12).ToArray();
        EndianWriter.WriteUInt16(source.AsSpan(0, 2), 0xC001, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(source.AsSpan(2, 2), (ushort)TiffFieldType.Byte, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(4, 4), 1, RawByteOrder.LittleEndian);
        source[8] = 0x42;
        BoundedInput directory = CreateInput(source);
        TiffHeader header = CreateHeader(RawByteOrder.LittleEndian, false, 0);

        TiffEntry entry = TiffEntry.Parse(directory, CreateInput(source), header);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Tag, Is.EqualTo(0xC001));
            Assert.That(entry.Count, Is.EqualTo(1));
            Assert.That(entry.GetByte(), Is.EqualTo(0x42));
            Assert.That(entry.RawBytes, Is.EqualTo(new byte[] { 0x42 }));
            Assert.That(entry.ValueSlotBytes, Is.EqualTo(new byte[] { 0x42, 0xCC, 0xCC, 0xCC }));
            Assert.That(entry.EntryOffset, Is.EqualTo(0));
            Assert.That(directory.GetPosition(), Is.EqualTo(12));
        });
    }

    [Test]
    public void ReferencedOffsetsAreRelativeToEmbeddedTiffBase()
    {
        byte[] source = new byte[96];
        int entryOffset = 20;
        EndianWriter.WriteUInt16(source.AsSpan(entryOffset, 2), 0x010E, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(source.AsSpan(entryOffset + 2, 2), (ushort)TiffFieldType.Ascii, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(entryOffset + 4, 4), 6, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(source.AsSpan(entryOffset + 8, 4), 40, RawByteOrder.LittleEndian);
        new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00 }.CopyTo(source, 50);
        BoundedInput directory = new BoundedInput(new ReadOnlyMemory<byte>(source), entryOffset, 12, new RawLimits());
        BoundedInput container = CreateInput(source);
        TiffHeader header = CreateHeader(RawByteOrder.LittleEndian, false, 10);

        TiffEntry entry = TiffEntry.Parse(directory, container, header);

        Assert.Multiple(() =>
        {
            Assert.That(entry.ValueOffset, Is.EqualTo(40));
            Assert.That(entry.GetAscii(), Is.EqualTo("Hello\0"));
            Assert.That(container.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidTypesCountsOffsetsLimitsAndConversionsAreRejected()
    {
        byte[] invalidTypeBytes = new byte[12];
        EndianWriter.WriteUInt16(invalidTypeBytes.AsSpan(2, 2), 14, RawByteOrder.LittleEndian);
        BoundedInput invalidTypeInput = CreateInput(invalidTypeBytes);
        RawFormatException? invalidType = Assert.Throws<RawFormatException>(() => TiffEntry.Parse(invalidTypeInput, CreateInput(invalidTypeBytes), CreateHeader(RawByteOrder.LittleEndian, false, 0)));

        byte[] overflowBytes = new byte[20];
        EndianWriter.WriteUInt16(overflowBytes.AsSpan(2, 2), (ushort)TiffFieldType.Double, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt64(overflowBytes.AsSpan(4, 8), ulong.MaxValue, RawByteOrder.LittleEndian);
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => TiffEntry.Parse(CreateInput(overflowBytes), CreateInput(overflowBytes), CreateHeader(RawByteOrder.LittleEndian, true, 0)));

        byte[] outsideBytes = new byte[12];
        EndianWriter.WriteUInt16(outsideBytes.AsSpan(2, 2), (ushort)TiffFieldType.Byte, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(outsideBytes.AsSpan(4, 4), 5, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(outsideBytes.AsSpan(8, 4), 100, RawByteOrder.LittleEndian);
        RawFormatException? outside = Assert.Throws<RawFormatException>(() => TiffEntry.Parse(CreateInput(outsideBytes), CreateInput(outsideBytes), CreateHeader(RawByteOrder.LittleEndian, false, 0)));

        byte[] limitBytes = BuildEntryBytes(TiffFieldType.Byte, 5, new byte[] { 1, 2, 3, 4, 5 }, RawByteOrder.LittleEndian, false);
        RawLimits limits = new RawLimits();
        limits.MaxTagValueBytes = 4;
        BoundedInput limitedContainer = new BoundedInput(new ReadOnlyMemory<byte>(limitBytes), 0, limitBytes.Length, limits);
        RawLimitException? limit = Assert.Throws<RawLimitException>(() => TiffEntry.Parse(new BoundedInput(new ReadOnlyMemory<byte>(limitBytes), 0, 12, limits), limitedContainer, CreateHeader(RawByteOrder.LittleEndian, false, 0)));

        TiffEntry twoShorts = ParseEntry(TiffFieldType.Short, 2, UInt16Bytes(RawByteOrder.LittleEndian, 1, 2), RawByteOrder.LittleEndian, false);
        RawFormatException? scalar = Assert.Throws<RawFormatException>(() => twoShorts.GetUInt16());
        RawFormatException? mismatch = Assert.Throws<RawFormatException>(() => twoShorts.GetInt16Values());

        Assert.Multiple(() =>
        {
            Assert.That((invalidType ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That(invalidType.Offset, Is.EqualTo(2));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
            Assert.That((outside ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(outside.Offset, Is.EqualTo(100));
            Assert.That((limit ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((scalar ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((mismatch ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        });
    }

    [Test]
    public void TruncatedClassicAndBigTiffEntriesAreRejectedWithoutPartialModels()
    {
        RawFormatException? classic = Assert.Throws<RawFormatException>(() => TiffEntry.Parse(CreateInput(new byte[11]), CreateInput(new byte[11]), CreateHeader(RawByteOrder.LittleEndian, false, 0)));
        RawFormatException? big = Assert.Throws<RawFormatException>(() => TiffEntry.Parse(CreateInput(new byte[19]), CreateInput(new byte[19]), CreateHeader(RawByteOrder.BigEndian, true, 0)));

        Assert.Multiple(() =>
        {
            Assert.That((classic ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((big ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
        });
    }

    static TiffEntry ParseEntry(TiffFieldType fieldType, ulong count, byte[] rawBytes, RawByteOrder byteOrder, bool bigTiff)
    {
        byte[] source = BuildEntryBytes(fieldType, count, rawBytes, byteOrder, bigTiff);
        int entrySize = bigTiff ? 20 : 12;
        BoundedInput directory = new BoundedInput(new ReadOnlyMemory<byte>(source), 0, entrySize, new RawLimits());
        BoundedInput container = CreateInput(source);
        return TiffEntry.Parse(directory, container, CreateHeader(byteOrder, bigTiff, 0));
    }

    static byte[] BuildEntryBytes(TiffFieldType fieldType, ulong count, byte[] rawBytes, RawByteOrder byteOrder, bool bigTiff)
    {
        int entrySize = bigTiff ? 20 : 12;
        int slotOffset = bigTiff ? 12 : 8;
        int slotSize = bigTiff ? 8 : 4;
        byte[] source = Enumerable.Repeat((byte)0xCC, 64 + rawBytes.Length).ToArray();
        EndianWriter.WriteUInt16(source.AsSpan(0, 2), 0x0100, byteOrder);
        EndianWriter.WriteUInt16(source.AsSpan(2, 2), (ushort)fieldType, byteOrder);
        if (bigTiff)
        {
            EndianWriter.WriteUInt64(source.AsSpan(4, 8), count, byteOrder);
        }
        else
        {
            EndianWriter.WriteUInt32(source.AsSpan(4, 4), (uint)count, byteOrder);
        }
        if (rawBytes.Length <= slotSize)
        {
            rawBytes.CopyTo(source, slotOffset);
        }
        else
        {
            if (bigTiff)
            {
                EndianWriter.WriteUInt64(source.AsSpan(slotOffset, 8), 64, byteOrder);
            }
            else
            {
                EndianWriter.WriteUInt32(source.AsSpan(slotOffset, 4), 64, byteOrder);
            }
            rawBytes.CopyTo(source, 64);
        }
        Assert.That(entrySize, Is.LessThanOrEqualTo(source.Length));
        return source;
    }

    static TiffHeader CreateHeader(RawByteOrder byteOrder, bool bigTiff, long baseOffset)
    {
        return new TiffHeader(byteOrder, bigTiff, bigTiff ? (ushort)43 : (ushort)42, bigTiff ? 8 : 4, 0, baseOffset, bigTiff ? 16 : 8);
    }

    static BoundedInput CreateInput(byte[] source)
    {
        return new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits());
    }

    static byte[] UInt16Bytes(RawByteOrder byteOrder, params ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] Int16Bytes(RawByteOrder byteOrder, params short[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] UInt32Bytes(RawByteOrder byteOrder, params uint[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] Int32Bytes(RawByteOrder byteOrder, params int[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteInt32(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] UInt64Bytes(RawByteOrder byteOrder, params ulong[] values)
    {
        byte[] bytes = new byte[values.Length * 8];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] Int64Bytes(RawByteOrder byteOrder, params long[] values)
    {
        byte[] bytes = new byte[values.Length * 8];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteInt64(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] SingleBytes(RawByteOrder byteOrder, params float[] values)
    {
        byte[] bytes = new byte[values.Length * 4];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteSingle(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] DoubleBytes(RawByteOrder byteOrder, params double[] values)
    {
        byte[] bytes = new byte[values.Length * 8];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteDouble(bytes.AsSpan(index * 8, 8), values[index], byteOrder);
        }
        return bytes;
    }
}
