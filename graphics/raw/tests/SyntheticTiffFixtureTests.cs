using raw;

namespace raw.Tests;

public class SyntheticTiffFixtureTests
{
    [TestCase(RawByteOrder.LittleEndian, false)]
    [TestCase(RawByteOrder.BigEndian, false)]
    [TestCase(RawByteOrder.LittleEndian, true)]
    [TestCase(RawByteOrder.BigEndian, true)]
    public void EveryFieldTypeAndInlineBoundaryParsesExactly(RawByteOrder byteOrder, bool bigTiff)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, bigTiff);
        SyntheticTiffIfd ifd = builder.AddIfd();
        TiffFieldType[] fieldTypes = Enum.GetValues<TiffFieldType>();
        Dictionary<ushort, byte[]> expectedBytes = new Dictionary<ushort, byte[]>();
        int slotSize = builder.GetSlotSize();

        for (int index = 0; index < fieldTypes.Length; index++)
        {
            TiffFieldType fieldType = fieldTypes[index];
            ushort tag = (ushort)(0x8000 + index);
            byte[] rawBytes = CreateFieldBytes(fieldType, byteOrder, slotSize);
            ulong count = (ulong)(rawBytes.Length / TiffFieldTypes.GetElementSize(fieldType));
            builder.AddField(ifd, tag, fieldType, count, rawBytes);
            expectedBytes.Add(tag, rawBytes);
        }

        byte[] fixture = builder.Build();
        TiffIfdGraph graph = Parse(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Header.ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(graph.Header.BigTiff, Is.EqualTo(bigTiff));
            Assert.That(graph.Ifds.Length, Is.EqualTo(1));
            Assert.That(graph.Ifds[0].Entries.Select(entry => entry.FieldType), Is.EqualTo(fieldTypes));
            Assert.That(graph.Ifds[0].Entries.Any(entry => entry.RawBytes.Length == slotSize && entry.Inline), Is.True);
            Assert.That(graph.Ifds[0].Entries.Any(entry => entry.RawBytes.Length == slotSize + 1 && !entry.Inline), Is.True);
            foreach (TiffEntry entry in graph.Ifds[0].Entries)
            {
                Assert.That(entry.RawBytes, Is.EqualTo(expectedBytes[entry.Tag]), $"tag 0x{entry.Tag:X4}");
                Assert.That(entry.Inline, Is.EqualTo(entry.RawBytes.Length <= slotSize), $"tag 0x{entry.Tag:X4}");
            }
        });
    }

    [TestCase(RawByteOrder.LittleEndian, false)]
    [TestCase(RawByteOrder.BigEndian, false)]
    [TestCase(RawByteOrder.LittleEndian, true)]
    [TestCase(RawByteOrder.BigEndian, true)]
    public void EveryIfdGraphFormHasStablePathsAndAliasTargets(RawByteOrder byteOrder, bool bigTiff)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, bigTiff);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd subIfd0 = builder.AddIfd();
        SyntheticTiffIfd subIfd1 = builder.AddIfd();
        SyntheticTiffIfd exif = builder.AddIfd();
        SyntheticTiffIfd interop = builder.AddIfd();
        SyntheticTiffIfd gps = builder.AddIfd();
        SyntheticTiffIfd next = builder.AddIfd();
        TiffFieldType pointerType = bigTiff ? TiffFieldType.Ifd8 : TiffFieldType.Ifd;

        builder.AddPointer(root, 330, pointerType, subIfd0, subIfd1);
        builder.AddPointer(root, 34665, pointerType, exif);
        builder.AddPointer(root, 34853, pointerType, gps);
        builder.AddPointer(root, 0xC001, pointerType, gps);
        builder.AddPointer(exif, 40965, pointerType, interop);
        builder.SetNextIfd(root, next);

        TiffIfdGraph graph = Parse(builder.Build());

        Assert.Multiple(() =>
        {
            Assert.That(graph.Ifds.Select(ifd => ifd.Path), Is.EqualTo(new string[]
            {
                "IFD0",
                "IFD0/SubIFD0",
                "IFD0/SubIFD1",
                "IFD0/Exif",
                "IFD0/Exif/Interop",
                "IFD0/GPS",
                "IFD1"
            }));
            Assert.That(graph.Links.Select(link => link.Kind), Does.Contain(TiffIfdLinkKind.Next));
            Assert.That(graph.Links.Select(link => link.Kind), Does.Contain(TiffIfdLinkKind.SubIfd));
            Assert.That(graph.Links.Select(link => link.Kind), Does.Contain(TiffIfdLinkKind.Exif));
            Assert.That(graph.Links.Select(link => link.Kind), Does.Contain(TiffIfdLinkKind.Gps));
            Assert.That(graph.Links.Select(link => link.Kind), Does.Contain(TiffIfdLinkKind.Interop));
            Assert.That(graph.Links.Select(link => link.Kind), Does.Contain(TiffIfdLinkKind.Unknown));
            TiffIfdLink alias = graph.Links.Single(link => link.Alias);
            Assert.That(alias.TargetPath, Is.EqualTo("IFD0/GPS"));
            Assert.That(alias.Path, Is.EqualTo("IFD0/Tag0xC001[3:0]"));
        });
    }

    [TestCase(RawByteOrder.LittleEndian, false)]
    [TestCase(RawByteOrder.BigEndian, false)]
    [TestCase(RawByteOrder.LittleEndian, true)]
    [TestCase(RawByteOrder.BigEndian, true)]
    public void MalformedOutOfLineOffsetsAreGeneratedDeterministically(RawByteOrder byteOrder, bool bigTiff)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, bigTiff);
        SyntheticTiffIfd ifd = builder.AddIfd();
        int valueLength = builder.GetSlotSize() + 1;
        SyntheticTiffEntry entry = builder.AddField(ifd, 0x9000, TiffFieldType.Undefined, (ulong)valueLength, Enumerable.Repeat((byte)0x5A, valueLength).ToArray());
        builder.SetMalformedValueOffset(entry, 0xFFFFFFF0UL);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => Parse(builder.Build()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That(exception.Offset, Is.EqualTo(0xFFFFFFF0L));
        });
    }

    [TestCase(RawByteOrder.LittleEndian, false)]
    [TestCase(RawByteOrder.BigEndian, false)]
    [TestCase(RawByteOrder.LittleEndian, true)]
    [TestCase(RawByteOrder.BigEndian, true)]
    public void CyclicIfdPointersAreGeneratedDeterministically(RawByteOrder byteOrder, bool bigTiff)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, bigTiff);
        SyntheticTiffIfd root = builder.AddIfd();
        SyntheticTiffIfd child = builder.AddIfd();
        TiffFieldType pointerType = bigTiff ? TiffFieldType.Ifd8 : TiffFieldType.Ifd;
        builder.AddPointer(root, 330, pointerType, child);
        builder.AddPointer(child, 34665, pointerType, root);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => Parse(builder.Build()));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(exception.Offset, Is.EqualTo(bigTiff ? 16 : 8));
            Assert.That(exception.Message, Does.Contain("cycle"));
        });
    }

    static TiffIfdGraph Parse(byte[] fixture)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(fixture), 0, fixture.Length, new RawLimits());
        return TiffParser.Parse(input);
    }

    static byte[] CreateFieldBytes(TiffFieldType fieldType, RawByteOrder byteOrder, int slotSize)
    {
        switch (fieldType)
        {
            case TiffFieldType.Byte:
                return Enumerable.Range(1, slotSize).Select(value => (byte)value).ToArray();
            case TiffFieldType.Ascii:
                return Enumerable.Repeat((byte)'A', slotSize).Append((byte)0).ToArray();
            case TiffFieldType.Short:
                return UInt16Bytes(byteOrder, 0x1234);
            case TiffFieldType.Long:
                return UInt32Bytes(byteOrder, 0x12345678U);
            case TiffFieldType.Rational:
                return UInt32Bytes(byteOrder, 3, 7);
            case TiffFieldType.SignedByte:
                return new byte[] { 0x80, 0x7F };
            case TiffFieldType.Undefined:
                return Enumerable.Repeat((byte)0xA5, slotSize + 1).ToArray();
            case TiffFieldType.SignedShort:
                return Int16Bytes(byteOrder, -1234);
            case TiffFieldType.SignedLong:
                return Int32Bytes(byteOrder, -12345678);
            case TiffFieldType.SignedRational:
                return Int32Bytes(byteOrder, -3, 7);
            case TiffFieldType.Float:
                return UInt32Bytes(byteOrder, BitConverter.SingleToUInt32Bits(-12.5F));
            case TiffFieldType.Double:
                return UInt64Bytes(byteOrder, BitConverter.DoubleToUInt64Bits(123.5));
            case TiffFieldType.Ifd:
                return UInt32Bytes(byteOrder, 0);
            case TiffFieldType.Long8:
                return UInt64Bytes(byteOrder, 0x0123456789ABCDEFUL);
            case TiffFieldType.SignedLong8:
                return Int64Bytes(byteOrder, -0x0123456789ABCDEFL);
            case TiffFieldType.Ifd8:
                return UInt64Bytes(byteOrder, 0);
            case TiffFieldType.Utf8:
                return System.Text.Encoding.UTF8.GetBytes("Exif 3 東京\0");
            default:
                throw new InvalidOperationException("Unexpected TIFF field type.");
        }
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
}
