namespace ttf.Tests;

internal class SfntCmapExtendedTableTests
{
    [Test]
    public void ParsesFormatEightBmpAndSupplementaryMappings()
    {
        SfntCmapSubtable subtable = Parse(
            BuildCmap(BuildFormat8(), 0, 4),
            5)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapFormat8Data? format = subtable.GetFormat8();

        Assert.Multiple(() =>
        {
            Assert.That(subtable.GetFormat(), Is.EqualTo(8));
            Assert.That(subtable.GetLanguage(), Is.Zero);
            Assert.That(format!.GetGroupCount(), Is.EqualTo(2));
            Assert.That(format.Is32BitLead(0xD800), Is.True);
            Assert.That(format.Is32BitLead(0xDC00), Is.True);
            Assert.That(format.Is32BitLead(0x0041), Is.False);
            Assert.That(format.GetGroup(1).GetStartCharacterCode(),
                Is.EqualTo(0xD800DC00L));
            Assert.That(subtable.MapCharacterCode(0x41), Is.EqualTo(1));
            Assert.That(subtable.MapCharacterCode(0x42), Is.EqualTo(2));
            Assert.That(subtable.MapCharacterCode(0x10000), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x10001), Is.EqualTo(4));
            Assert.That(subtable.MapCharacterCode(0xD800DC00L), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x10002), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0xD800), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => format.Is32BitLead(0x10000));
        });
    }

    [Test]
    public void ParsesFormatTenTrimmedNonBmpArray()
    {
        SfntCmapSubtable subtable = Parse(
            BuildCmap(
                BuildFormat10(0, 0x1F600, new int[] { 1, 0, 3 }),
                0,
                4),
            4)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapFormat10Data? format = subtable.GetFormat10();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetStartCharacterCode(), Is.EqualTo(0x1F600));
            Assert.That(format.GetEntryCount(), Is.EqualTo(3));
            Assert.That(format.GetGlyphId(2), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x1F5FF), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x1F600), Is.EqualTo(1));
            Assert.That(subtable.MapCharacterCode(0x1F601), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x1F602), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x1F603), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => format.GetGlyphId(3));
        });
    }

    [Test]
    public void ParsesFormatTwelveAsSortedSequentialGroups()
    {
        byte[] formatBytes = BuildGroupedFormat(
            12,
            0,
            new long[] { 0x41, 0x1F600 },
            new long[] { 0x42, 0x1F602 },
            new int[] { 1, 3 });
        SfntCmapSubtable subtable = Parse(
            BuildCmap(formatBytes, 3, 10),
            6)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapGroupedData? format = subtable.GetFormat12();

        Assert.Multiple(() =>
        {
            Assert.That(format!.UsesConstantGlyphs(), Is.False);
            Assert.That(format.GetGroupCount(), Is.EqualTo(2));
            Assert.That(format.GetGroup(0).GetStartCharacterCode(), Is.EqualTo(0x41));
            Assert.That(format.GetGroup(0).GetEndCharacterCode(), Is.EqualTo(0x42));
            Assert.That(format.GetGroup(1).GetStartGlyphId(), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x41), Is.EqualTo(1));
            Assert.That(subtable.MapCharacterCode(0x42), Is.EqualTo(2));
            Assert.That(subtable.MapCharacterCode(0x43), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x1F600), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x1F602), Is.EqualTo(5));
            Assert.That(subtable.MapCharacterCode(0x1F603), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => format.GetGroup(2));
        });
    }

    [Test]
    public void ParsesFormatThirteenWithoutExpandingLargeRanges()
    {
        byte[] formatBytes = BuildGroupedFormat(
            13,
            0,
            new long[] { 0 },
            new long[] { 0x10FFFF },
            new int[] { 2 });
        SfntCmapSubtable subtable = Parse(
            BuildCmap(formatBytes, 0, 6),
            3)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapGroupedData? format = subtable.GetFormat13();

        Assert.Multiple(() =>
        {
            Assert.That(format!.UsesConstantGlyphs(), Is.True);
            Assert.That(format.GetGroupCount(), Is.EqualTo(1));
            Assert.That(format.GetGroup(0).GetEndCharacterCode(),
                Is.EqualTo(0x10FFFF));
            Assert.That(subtable.MapCharacterCode(0), Is.EqualTo(2));
            Assert.That(subtable.MapCharacterCode(0x10000), Is.EqualTo(2));
            Assert.That(subtable.MapCharacterCode(0x10FFFF), Is.EqualTo(2));
            Assert.That(subtable.MapCharacterCode(0x110000), Is.Zero);
            Assert.That(subtable.GetFormat12(), Is.Null);
        });
    }

    [Test]
    public void AcceptsEmptyExtendedCoverageTables()
    {
        byte[] format8 = BuildEmptyFormat8();
        SetIs32(format8, 0x41, true);
        byte[] format10 = BuildFormat10(0, 0, new int[0]);
        byte[] format12 = BuildGroupedFormat(
            12,
            0,
            new long[0],
            new long[0],
            new int[0]);
        byte[] format13 = BuildGroupedFormat(
            13,
            0,
            new long[0],
            new long[0],
            new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(Parse(BuildCmap(format8, 0, 4), 1)!
                .GetEncodingRecord(0).GetSubtable().MapCharacterCode(65), Is.Zero);
            Assert.That(Parse(BuildCmap(format10, 0, 4), 1)!
                .GetEncodingRecord(0).GetSubtable().MapCharacterCode(65), Is.Zero);
            Assert.That(Parse(BuildCmap(format12, 0, 4), 1)!
                .GetEncodingRecord(0).GetSubtable().MapCharacterCode(65), Is.Zero);
            Assert.That(Parse(BuildCmap(format13, 0, 6), 1)!
                .GetEncodingRecord(0).GetSubtable().MapCharacterCode(65), Is.Zero);
        });
    }

    [Test]
    public void PreservesTheExtendedMacintoshLanguageField()
    {
        byte[] format = BuildFormat10(70000, 65, new int[] { 1 });
        SfntCmapSubtable subtable = Parse(
            BuildCmap(format, 1, 0),
            2)!.GetEncodingRecord(0).GetSubtable();

        Assert.That(subtable.GetLanguage(), Is.EqualTo(70000));
    }

    [Test]
    public void RejectsExtendedHeaderAndLanguageErrors()
    {
        byte[] reserved = BuildFormat10(0, 65, new int[] { 1 });
        SetUInt16(reserved, 2, 1);
        byte[] shortLength = BuildFormat10(0, 65, new int[] { 1 });
        SetUInt32(shortLength, 4, 11);
        byte[] longLength = BuildFormat10(0, 65, new int[] { 1 });
        SetUInt32(longLength, 4, uint.MaxValue);
        byte[] language = BuildFormat10(1, 65, new int[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(reserved, 0, 4), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(shortLength, 0, 4), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(longLength, 0, 4), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(language, 0, 4), 2));
        });
    }

    [Test]
    public void RejectsMalformedFormatEightBitsAndGroups()
    {
        byte[] nonSurrogateLead = BuildFormat8();
        SetIs32(nonSurrogateLead, 0x41, true);
        byte[] missingLead = BuildFormat8();
        SetIs32(missingLead, 0xD800, false);
        byte[] invalidLow = BuildFormat8();
        SetUInt32(invalidLow, 8220, 0xD800DBFFU);
        SetUInt32(invalidLow, 8224, 0xD800DBFFU);
        byte[] overlap = BuildFormat8();
        SetUInt32(overlap, 8220, 0x42);
        byte[] badGlyph = BuildFormat8();
        SetUInt32(badGlyph, 8228, 5);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(nonSurrogateLead, 0, 4), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(missingLead, 0, 4), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(invalidLow, 0, 4), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(overlap, 0, 4), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 0, 4), 5));
        });
    }

    [Test]
    public void RejectsMalformedFormatTenRangesLengthsAndGlyphIds()
    {
        byte[] overflow = BuildFormat10(
            0,
            0xFFFFFFFE,
            new int[] { 1, 2, 3 });
        byte[] badLength = BuildFormat10(0, 65, new int[] { 1 });
        SetUInt32(badLength, 4, 20);
        byte[] badGlyph = BuildFormat10(0, 65, new int[] { 4 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(overflow, 0, 4), 4));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badLength, 0, 4), 4));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 0, 4), 4));
        });
    }

    [Test]
    public void RejectsUnsortedOverlappingAndInvalidSequentialGroups()
    {
        byte[] inverted = BuildGroupedFormat(
            12,
            0,
            new long[] { 0x42 },
            new long[] { 0x41 },
            new int[] { 1 });
        byte[] overlap = BuildGroupedFormat(
            12,
            0,
            new long[] { 0x41, 0x42 },
            new long[] { 0x42, 0x43 },
            new int[] { 1, 3 });
        byte[] glyphOverflow = BuildGroupedFormat(
            12,
            0,
            new long[] { 0x10000 },
            new long[] { 0x10002 },
            new int[] { 3 });
        byte[] badLength = BuildGroupedFormat(
            12,
            0,
            new long[] { 0x41 },
            new long[] { 0x41 },
            new int[] { 1 });
        SetUInt32(badLength, 4, 16);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(inverted, 3, 10), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(overlap, 3, 10), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(glyphOverflow, 3, 10), 5));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badLength, 3, 10), 5));
        });
    }

    [Test]
    public void RejectsInvalidConstantGroupGlyphIds()
    {
        byte[] format = BuildGroupedFormat(
            13,
            0,
            new long[] { 0 },
            new long[] { uint.MaxValue },
            new int[] { 3 });

        Assert.Throws<FontFormatException>(
            () => Parse(BuildCmap(format, 0, 6), 3));
    }

    [Test]
    public void AppliesEntryAndGroupLimitsWithoutExpandingRanges()
    {
        byte[] format8 = BuildFormat8();
        byte[] format10 = BuildFormat10(0, 65, new int[] { 1, 2, 3 });
        byte[] format12 = BuildGroupedFormat(
            12,
            0,
            new long[] { 0x41, 0x10000 },
            new long[] { 0x41, 0x10000 },
            new int[] { 1, 2 });
        byte[] format13 = BuildGroupedFormat(
            13,
            0,
            new long[] { 0 },
            new long[] { uint.MaxValue },
            new int[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                BuildCmap(format8, 0, 4),
                5,
                CreateLimits(1)));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildCmap(format10, 0, 4),
                4,
                CreateLimits(2)));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildCmap(format12, 3, 10),
                3,
                CreateLimits(1)));
            Assert.That(Parse(
                BuildCmap(format13, 0, 6),
                2,
                CreateLimits(1))!.GetEncodingRecord(0)
                .GetSubtable().MapCharacterCode(uint.MaxValue), Is.EqualTo(1));
        });
    }

    static byte[] BuildFormat8()
    {
        byte[] is32 = new byte[8192];
        SetIs32(is32, 0, 0xD800, true);
        SetIs32(is32, 0, 0xDC00, true);
        SetIs32(is32, 0, 0xDC01, true);
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(8)
            .WriteUInt16(0)
            .WriteUInt32(8232)
            .WriteUInt32(0)
            .WriteBytes(is32)
            .WriteUInt32(2)
            .WriteUInt32(0x41)
            .WriteUInt32(0x42)
            .WriteUInt32(1)
            .WriteUInt32(0xD800DC00U)
            .WriteUInt32(0xD800DC01U)
            .WriteUInt32(3);
        return builder.CopyBytes();
    }

    static byte[] BuildEmptyFormat8()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(8)
            .WriteUInt16(0)
            .WriteUInt32(8208)
            .WriteUInt32(0)
            .WriteZeros(8192)
            .WriteUInt32(0)
            .CopyBytes();
    }

    static byte[] BuildFormat10(int language, long startCode, int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(10)
            .WriteUInt16(0)
            .WriteUInt32((uint)(20 + glyphIds.Length * 2))
            .WriteUInt32((uint)language)
            .WriteUInt32((uint)startCode)
            .WriteUInt32((uint)glyphIds.Length);

        for (int index = 0; index < glyphIds.Length; index += 1)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildGroupedFormat(
        int format,
        long language,
        long[] starts,
        long[] ends,
        int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)format)
            .WriteUInt16(0)
            .WriteUInt32((uint)(16 + starts.Length * 12))
            .WriteUInt32((uint)language)
            .WriteUInt32((uint)starts.Length);

        for (int index = 0; index < starts.Length; index += 1)
        {
            builder
                .WriteUInt32((uint)starts[index])
                .WriteUInt32((uint)ends[index])
                .WriteUInt32((uint)glyphIds[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildCmap(byte[] subtable, int platformId, int encodingId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16((ushort)platformId)
            .WriteUInt16((ushort)encodingId)
            .WriteUInt32(12)
            .WriteBytes(subtable)
            .CopyBytes();
    }

    static SfntCmapTable? Parse(byte[] cmap, int glyphCount)
    {
        return Parse(cmap, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntCmapTable? Parse(
        byte[] cmap,
        int glyphCount,
        FontLimits limits)
    {
        byte[] maxp = new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
        byte[][] bytes = new byte[][] { cmap, maxp };
        int[] tags = new int[] { FontTags.Cmap, FontTags.Maxp };
        SfntTableData[] tables = new SfntTableData[2];

        for (int index = 0; index < tables.Length; index += 1)
        {
            FontData data = FontData.Create(bytes[index]);
            tables[index] = new SfntTableData(
                tags[index],
                FontChecksum.CalculateTable(bytes[index]),
                data.GetSlice(0, data.GetLength()),
                SfntTableClassification.Known,
                true);
        }

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, new FontDiagnostic[0]),
            FontLoadMode.Compatible,
            limits);
        return SfntCmapTableParser.FindTable(cache);
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            65535,
            1000,
            1000,
            10,
            10,
            10,
            100,
            100000,
            maxCoverageEntries,
            100000,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static void SetIs32(byte[] format8, int codeUnit, bool value)
    {
        SetIs32(format8, 12, codeUnit, value);
    }

    static void SetIs32(
        byte[] bytes,
        int baseOffset,
        int codeUnit,
        bool value)
    {
        int index = baseOffset + codeUnit / 8;
        int mask = 1 << (7 - codeUnit % 8);

        if (value)
        {
            bytes[index] = (byte)(bytes[index] | mask);
        }
        else
        {
            bytes[index] = (byte)(bytes[index] & ~mask);
        }
    }

    static void SetUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void SetUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
