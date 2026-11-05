namespace ttf.Tests;

internal class SfntCmapTableTests
{
    [Test]
    public void ParsesFormatZeroAndMapsEveryByte()
    {
        SfntCmapTable? table = Parse(BuildCmap(BuildFormat0(0), 1, 0), 300);
        SfntCmapEncodingRecord record = table!.GetEncodingRecord(0);
        SfntCmapSubtable subtable = record.GetSubtable();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.Zero);
            Assert.That(table.GetEncodingRecordCount(), Is.EqualTo(1));
            Assert.That(record.GetPlatformId(), Is.EqualTo(1));
            Assert.That(record.GetEncodingId(), Is.Zero);
            Assert.That(record.GetSubtableOffset(), Is.EqualTo(12));
            Assert.That(subtable.GetFormat(), Is.Zero);
            Assert.That(subtable.GetLength(), Is.EqualTo(262));
            Assert.That(subtable.GetLanguage(), Is.Zero);
            Assert.That(subtable.GetFormat0()!.GetEntryCount(), Is.EqualTo(256));
            Assert.That(subtable.GetFormat0()!.GetGlyphId(65), Is.EqualTo(65));
            Assert.That(subtable.MapCharacterCode(0), Is.Zero);
            Assert.That(subtable.MapCharacterCode(255), Is.EqualTo(255));
            Assert.That(subtable.MapCharacterCode(256), Is.Zero);
            Assert.That(subtable.GetFormat2(), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => subtable.GetFormat0()!.GetGlyphId(256));
        });
    }

    [Test]
    public void ParsesFormatTwoSingleAndDoubleByteMappings()
    {
        SfntCmapSubtable subtable = Parse(
            BuildCmap(BuildFormat2(), 1, 1),
            10)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapFormat2Data? format = subtable.GetFormat2();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetSubHeaderKeyCount(), Is.EqualTo(256));
            Assert.That(format.GetSubHeaderCount(), Is.EqualTo(2));
            Assert.That(format.GetSubHeaderIndex(0), Is.Zero);
            Assert.That(format.GetSubHeaderIndex(0x81), Is.EqualTo(1));
            Assert.That(format.GetSubHeader(1).GetFirstCode(), Is.EqualTo(0x40));
            Assert.That(format.GetSubHeader(1).GetEntryCount(), Is.EqualTo(2));
            Assert.That(format.GetSubHeader(1).GetIdDelta(), Is.EqualTo(1));
            Assert.That(format.GetSubHeader(1).GetIdRangeOffset(), Is.EqualTo(6));
            Assert.That(subtable.MapCharacterCode(0x20), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x21), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x8140), Is.EqualTo(6));
            Assert.That(subtable.MapCharacterCode(0x8141), Is.EqualTo(7));
            Assert.That(subtable.MapCharacterCode(0x8240), Is.Zero);
            Assert.That(subtable.MapCharacterCode(-1), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x10000), Is.Zero);
        });
    }

    [Test]
    public void AcceptsAnEmptyFormatTwoSingleByteSubheader()
    {
        TestBinaryBuilder subtable = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(526)
            .WriteUInt16(0)
            .WriteZeros(512)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteInt16(0)
            .WriteUInt16(0);

        SfntCmapFormat2Data? format = Parse(
            BuildCmap(subtable.CopyBytes(), 1, 1),
            1)!.GetEncodingRecord(0).GetSubtable().GetFormat2();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetSubHeaderCount(), Is.EqualTo(1));
            Assert.That(format.GetSubHeader(0).GetEntryCount(), Is.Zero);
            Assert.That(format.MapCharacterCode(65), Is.Zero);
        });
    }

    [Test]
    public void ParsesFormatFourDeltaAndGlyphArraySegments()
    {
        SfntCmapSubtable subtable = Parse(
            BuildCmap(BuildFormat4(), 3, 1),
            10)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapFormat4Data? format = subtable.GetFormat4();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetSegmentCount(), Is.EqualTo(3));
            Assert.That(format.GetSearchRange(), Is.EqualTo(4));
            Assert.That(format.GetEntrySelector(), Is.EqualTo(1));
            Assert.That(format.GetRangeShift(), Is.EqualTo(2));
            Assert.That(format.GetSegment(0).GetStartCode(), Is.EqualTo(0x41));
            Assert.That(format.GetSegment(0).GetEndCode(), Is.EqualTo(0x43));
            Assert.That(format.GetSegment(0).GetIdDelta(), Is.EqualTo(-64));
            Assert.That(format.GetSegment(1).GetIdRangeOffset(), Is.EqualTo(4));
            Assert.That(subtable.MapCharacterCode(0x40), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x41), Is.EqualTo(1));
            Assert.That(subtable.MapCharacterCode(0x43), Is.EqualTo(3));
            Assert.That(subtable.MapCharacterCode(0x50), Is.EqualTo(5));
            Assert.That(subtable.MapCharacterCode(0x51), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x52), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0xFFFF), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x10000), Is.Zero);
        });
    }

    [Test]
    public void ParsesFormatSixTrimmedMappings()
    {
        SfntCmapSubtable subtable = Parse(
            BuildCmap(BuildFormat6(0, 0x20, new int[] { 2, 0, 4 }), 3, 1),
            5)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapFormat6Data? format = subtable.GetFormat6();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetFirstCode(), Is.EqualTo(0x20));
            Assert.That(format.GetEntryCount(), Is.EqualTo(3));
            Assert.That(format.GetGlyphId(0), Is.EqualTo(2));
            Assert.That(format.GetGlyphId(1), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x1F), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x20), Is.EqualTo(2));
            Assert.That(subtable.MapCharacterCode(0x21), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x22), Is.EqualTo(4));
            Assert.That(subtable.MapCharacterCode(0x23), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => format.GetGlyphId(3));
        });
    }

    [Test]
    public void ReusesSharedSubtablesAndExposesThemThroughFontFace()
    {
        byte[] format = BuildFormat6(0, 65, new int[] { 1 });
        TestBinaryBuilder cmap = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt32(20)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt32(20)
            .WriteBytes(format);
        SfntTableCache cache = CreateCache(cmap.CopyBytes(), 2);
        SfntCmapTable? table = SfntCmapTableParser.FindTable(cache);
        FontFace face = CreateFace(cache);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetEncodingRecordCount(), Is.EqualTo(2));
            Assert.That(table.GetEncodingRecord(0).GetSubtable(),
                Is.SameAs(table.GetEncodingRecord(1).GetSubtable()));
            Assert.That(face.GetCmapTable(), Is.SameAs(table));
            Assert.That(face.GetCmapTableView()!.GetParsedData(), Is.SameAs(table));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => table.GetEncodingRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => table.GetEncodingRecord(2));
        });
    }

    [Test]
    public void AcceptsMacintoshRecordsSortedByLanguage()
    {
        byte[] first = BuildFormat6(1, 65, new int[] { 1 });
        byte[] second = BuildFormat6(2, 66, new int[] { 2 });
        byte[] cmap = BuildTwoCmap(1, 0, first, 1, 0, second);
        SfntCmapTable? table = Parse(cmap, 3);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetEncodingRecord(0).GetSubtable().GetLanguage(),
                Is.EqualTo(1));
            Assert.That(table.GetEncodingRecord(1).GetSubtable().GetLanguage(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectsHeaderAndEncodingRecordStructuralErrors()
    {
        byte[] valid = BuildCmap(BuildFormat6(0, 65, new int[] { 1 }), 3, 1);
        byte[] badOffset = valid.ToArray();
        SetUInt32(badOffset, 8, 4);
        byte[] outsideOffset = valid.ToArray();
        SetUInt32(outsideOffset, 8, (uint)outsideOffset.Length);
        byte[] unsupported = BuildCmap(
            new TestBinaryBuilder()
                .WriteUInt16(8)
                .WriteUInt16(6)
                .WriteUInt16(0)
                .CopyBytes(),
            3,
            1);

        Assert.Multiple(() =>
        {
            for (int length = 0; length < 4; length += 1)
            {
                byte[] truncated = valid.AsSpan(0, length).ToArray();
                Assert.Throws<FontFormatException>(() => Parse(truncated, 2));
            }

            Assert.Throws<FontFormatException>(() => Parse(valid.AsSpan(0, 11).ToArray(), 2));
            Assert.Throws<FontFormatException>(() => Parse(badOffset, 2));
            Assert.Throws<FontFormatException>(() => Parse(outsideOffset, 2));
            Assert.Throws<FontFormatException>(() => Parse(unsupported, 2));
        });
    }

    [Test]
    public void RejectsUnsortedDuplicateAndInvalidLanguageRecords()
    {
        byte[] first = BuildFormat6(0, 65, new int[] { 1 });
        byte[] second = BuildFormat6(0, 66, new int[] { 2 });
        byte[] unsorted = BuildTwoCmap(3, 1, first, 1, 0, second);
        byte[] duplicate = BuildTwoCmap(3, 1, first, 3, 1, second);
        byte[] language = BuildCmap(
            BuildFormat6(1, 65, new int[] { 1 }),
            3,
            1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(unsorted, 3));
            Assert.Throws<FontFormatException>(() => Parse(duplicate, 3));
            Assert.Throws<FontFormatException>(() => Parse(language, 3));
        });
    }

    [Test]
    public void RejectsPartiallyOverlappingSubtables()
    {
        byte[] outer = BuildFormat0(0);
        byte[] nested = BuildFormat6(0, 65, new int[] { 1 });
        nested.CopyTo(outer, 6);
        TestBinaryBuilder cmap = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(20)
            .WriteUInt16(3)
            .WriteUInt16(1)
            .WriteUInt32(26)
            .WriteBytes(outer);

        Assert.Throws<FontFormatException>(() => Parse(cmap.CopyBytes(), 300));
    }

    [Test]
    public void RejectsInvalidFormatZeroLengthAndGlyphIds()
    {
        byte[] badLength = BuildFormat0(0);
        SetUInt16(badLength, 2, 261);
        byte[] badGlyph = BuildFormat0(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badLength, 1, 0), 300));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 1, 0), 255));
        });
    }

    [Test]
    public void RejectsMalformedFormatTwoStructures()
    {
        byte[] badKeyAlignment = BuildFormat2();
        SetUInt16(badKeyAlignment, 8, 1);
        byte[] badSingleByteKey = BuildFormat2();
        SetUInt16(badSingleByteKey, 6, 8);
        byte[] badRange = BuildFormat2();
        SetUInt16(badRange, 526, 255);
        byte[] badOffsetAlignment = BuildFormat2();
        SetUInt16(badOffsetAlignment, 532, 5);
        byte[] badOffsetBounds = BuildFormat2();
        SetUInt16(badOffsetBounds, 532, 100);
        byte[] badGlyph = BuildFormat2();
        SetUInt16(badGlyph, 538, 20);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badKeyAlignment, 1, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badSingleByteKey, 1, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badRange, 1, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badOffsetAlignment, 1, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badOffsetBounds, 1, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 1, 1), 10));
        });
    }

    [Test]
    public void RejectsMalformedFormatFourStructures()
    {
        byte[] oddSegmentCount = BuildFormat4();
        SetUInt16(oddSegmentCount, 6, 5);
        byte[] badSearch = BuildFormat4();
        SetUInt16(badSearch, 8, 2);
        byte[] badPad = BuildFormat4();
        SetUInt16(badPad, 20, 1);
        byte[] unsortedEnds = BuildFormat4();
        SetUInt16(unsortedEnds, 16, 0x40);
        byte[] overlap = BuildFormat4();
        SetUInt16(overlap, 24, 0x43);
        byte[] inverted = BuildFormat4();
        SetUInt16(inverted, 24, 0x52);
        byte[] badSentinel = BuildFormat4();
        SetUInt16(badSentinel, 26, 0xFFFE);
        byte[] badOffsetAlignment = BuildFormat4();
        SetUInt16(badOffsetAlignment, 36, 3);
        byte[] badOffsetBounds = BuildFormat4();
        SetUInt16(badOffsetBounds, 36, 100);
        byte[] badGlyph = BuildFormat4();
        SetUInt16(badGlyph, 40, 20);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(oddSegmentCount, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badSearch, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badPad, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(unsortedEnds, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(overlap, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(inverted, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badSentinel, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badOffsetAlignment, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badOffsetBounds, 3, 1), 10));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 3, 1), 10));
        });
    }

    [Test]
    public void RejectsMalformedFormatSixRangesLengthsAndGlyphIds()
    {
        byte[] badRange = BuildFormat6(0, 0xFFFF, new int[] { 1, 2 });
        byte[] badLength = BuildFormat6(0, 65, new int[] { 1 });
        SetUInt16(badLength, 2, 10);
        byte[] badGlyph = BuildFormat6(0, 65, new int[] { 4 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badRange, 3, 1), 3));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badLength, 3, 1), 3));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 3, 1), 4));
        });
    }

    [Test]
    public void AppliesCoverageAndEncodingRecordLimits()
    {
        byte[] format0 = BuildCmap(BuildFormat0(0), 1, 0);
        byte[] format2 = BuildCmap(BuildFormat2(), 1, 1);
        byte[] format4 = BuildCmap(BuildFormat4(), 3, 1);
        byte[] format6 = BuildCmap(
            BuildFormat6(0, 65, new int[] { 1, 2, 3 }),
            3,
            1);
        byte[] twoRecords = BuildTwoCmap(
            0,
            3,
            BuildFormat6(0, 65, new int[] { 1 }),
            3,
            1,
            BuildFormat6(0, 66, new int[] { 2 }));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(
                () => Parse(format0, 300, CreateLimits(255, 10)));
            Assert.Throws<FontLimitException>(
                () => Parse(format2, 10, CreateLimits(3, 10)));
            Assert.Throws<FontLimitException>(
                () => Parse(format4, 10, CreateLimits(5, 10)));
            Assert.Throws<FontLimitException>(
                () => Parse(format6, 10, CreateLimits(2, 10)));
            Assert.Throws<FontLimitException>(
                () => Parse(twoRecords, 10, CreateLimits(100, 1)));
        });
    }

    [Test]
    public void RequiresMaxpAndKeepsUnknownCmapVersionsRaw()
    {
        byte[] valid = BuildCmap(BuildFormat6(0, 65, new int[] { 1 }), 3, 1);
        SfntTableCache withoutMaxp = CreateCacheWithoutMaxp(valid);
        SfntTableCache withoutCmap = CreateCacheFromTables(
            new byte[][] { BuildMaxp(2) },
            new int[] { FontTags.Maxp },
            FontLimits.CreateCompatible());
        byte[] unknown = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .CopyBytes();
        SfntTableCache unknownCache = CreateCache(unknown, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => SfntCmapTableParser.FindTable(withoutMaxp));
            Assert.That(SfntCmapTableParser.FindTable(unknownCache), Is.Null);
            Assert.That(
                SfntCmapTableParser.FindView(unknownCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(SfntCmapTableParser.FindTable(withoutCmap), Is.Null);
            Assert.That(SfntCmapTableParser.FindView(withoutCmap), Is.Null);
            Assert.Throws<ArgumentNullException>(
                () => SfntCmapTableParser.FindView(null!));
        });
    }

    static byte[] BuildFormat0(int language)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(262)
            .WriteUInt16((ushort)language);

        for (int index = 0; index < 256; index += 1)
        {
            builder.WriteUInt8((byte)index);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0);

        for (int index = 0; index < 256; index += 1)
        {
            builder.WriteUInt16((ushort)(index == 0x81 ? 8 : 0));
        }

        builder
            .WriteUInt16(0x20)
            .WriteUInt16(2)
            .WriteInt16(0)
            .WriteUInt16(10)
            .WriteUInt16(0x40)
            .WriteUInt16(2)
            .WriteInt16(1)
            .WriteUInt16(6)
            .WriteUInt16(3)
            .WriteUInt16(0)
            .WriteUInt16(5)
            .WriteUInt16(6)
            .PatchUInt16(2, (ushort)builder.GetLength());
        return builder.CopyBytes();
    }

    static byte[] BuildFormat4()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(4)
            .WriteUInt16(44)
            .WriteUInt16(0)
            .WriteUInt16(6)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(0x43)
            .WriteUInt16(0x51)
            .WriteUInt16(0xFFFF)
            .WriteUInt16(0)
            .WriteUInt16(0x41)
            .WriteUInt16(0x50)
            .WriteUInt16(0xFFFF)
            .WriteInt16(-64)
            .WriteInt16(1)
            .WriteInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] BuildFormat6(int language, int firstCode, int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(6)
            .WriteUInt16((ushort)(10 + glyphIds.Length * 2))
            .WriteUInt16((ushort)language)
            .WriteUInt16((ushort)firstCode)
            .WriteUInt16((ushort)glyphIds.Length);

        for (int index = 0; index < glyphIds.Length; index += 1)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
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

    static byte[] BuildTwoCmap(
        int firstPlatform,
        int firstEncoding,
        byte[] firstSubtable,
        int secondPlatform,
        int secondEncoding,
        byte[] secondSubtable)
    {
        int firstOffset = 20;
        int secondOffset = firstOffset + firstSubtable.Length;
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt16((ushort)firstPlatform)
            .WriteUInt16((ushort)firstEncoding)
            .WriteUInt32((uint)firstOffset)
            .WriteUInt16((ushort)secondPlatform)
            .WriteUInt16((ushort)secondEncoding)
            .WriteUInt32((uint)secondOffset)
            .WriteBytes(firstSubtable)
            .WriteBytes(secondSubtable)
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
        return SfntCmapTableParser.FindTable(CreateCache(cmap, glyphCount, limits));
    }

    static SfntTableCache CreateCache(byte[] cmap, int glyphCount)
    {
        return CreateCache(cmap, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntTableCache CreateCache(
        byte[] cmap,
        int glyphCount,
        FontLimits limits)
    {
        byte[] maxp = BuildMaxp(glyphCount);
        return CreateCacheFromTables(
            new byte[][] { cmap, maxp },
            new int[] { FontTags.Cmap, FontTags.Maxp },
            limits);
    }

    static SfntTableCache CreateCacheWithoutMaxp(byte[] cmap)
    {
        return CreateCacheFromTables(
            new byte[][] { cmap },
            new int[] { FontTags.Cmap },
            FontLimits.CreateCompatible());
    }

    static SfntTableCache CreateCacheFromTables(
        byte[][] bytes,
        int[] tags,
        FontLimits limits)
    {
        SfntTableData[] tables = new SfntTableData[bytes.Length];

        for (int index = 0; index < bytes.Length; index += 1)
        {
            FontData data = FontData.Create(bytes[index]);
            tables[index] = new SfntTableData(
                tags[index],
                FontChecksum.CalculateTable(bytes[index]),
                data.GetSlice(0, data.GetLength()),
                SfntTableClassification.Known,
                true);
        }

        return SfntTableCache.Create(
            new SfntTableStore(tables, new FontDiagnostic[0]),
            FontLoadMode.Compatible,
            limits);
    }

    static FontFace CreateFace(SfntTableCache cache)
    {
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                new FontDiagnostic[0]),
            cache);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static FontLimits CreateLimits(int maxCoverageEntries, int maxTables)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            maxTables,
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
