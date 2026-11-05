using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class SfntHorizontalMetricsTests
{
    [Test]
    public void ParsesEveryHheaField()
    {
        SfntHheaTable? table = ParseHhea(BuildHhea(3, 700));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetAscender(), Is.EqualTo(900));
            Assert.That(table.GetDescender(), Is.EqualTo(-300));
            Assert.That(table.GetLineGap(), Is.EqualTo(-20));
            Assert.That(table.GetAdvanceWidthMaximum(), Is.EqualTo(700));
            Assert.That(table.GetMinimumLeftSideBearing(), Is.EqualTo(-40));
            Assert.That(table.GetMinimumRightSideBearing(), Is.EqualTo(-50));
            Assert.That(table.GetMaximumXExtent(), Is.EqualTo(680));
            Assert.That(table.GetCaretSlopeRise(), Is.EqualTo(1));
            Assert.That(table.GetCaretSlopeRun(), Is.EqualTo(-2));
            Assert.That(table.GetCaretOffset(), Is.EqualTo(3));
            Assert.That(table.GetMetricDataFormat(), Is.Zero);
            Assert.That(table.GetMetricCount(), Is.EqualTo(3));
            Assert.That(table.GetExtensionLength(), Is.Zero);
        });
    }

    [Test]
    public void RejectsEveryHheaTruncationPoint()
    {
        byte[] complete = BuildHhea(1, 500);

        for (int length = 0; length < complete.Length; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => ParseHhea(complete.AsSpan(0, length).ToArray()),
                "Length {0}",
                length);
        }
    }

    [Test]
    public void AppliesHheaVersionAndExtensionGating()
    {
        byte[] exactWithTrailing = Append(BuildHhea(1, 500), new byte[] { 1 });
        byte[] compatibleExtension = exactWithTrailing.ToArray();
        WriteUInt16(compatibleExtension, 2, 1);
        byte[] unknownMajor = BuildHhea(1, 500);
        WriteUInt16(unknownMajor, 0, 2);

        Assert.Throws<FontFormatException>(() => ParseHhea(exactWithTrailing));

        SfntTableCache compatibleCache = CreateCache(
            compatibleExtension,
            null,
            null);
        SfntHheaTable? compatible = SfntHheaTableParser.FindTable(compatibleCache);
        SfntTableCache unknownCache = CreateCache(unknownMajor, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(compatible!.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(compatible.GetExtensionLength(), Is.EqualTo(1));
            Assert.That(SfntHheaTableParser.FindView(compatibleCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntHheaTableParser.FindView(unknownCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(SfntHheaTableParser.FindTable(unknownCache), Is.Null);
        });
    }

    [TestCase(24)]
    [TestCase(26)]
    [TestCase(28)]
    [TestCase(30)]
    public void RejectsEveryNonzeroReservedHheaField(int offset)
    {
        byte[] bytes = BuildHhea(1, 500);
        WriteInt16(bytes, offset, 1);

        Assert.Throws<FontFormatException>(() => ParseHhea(bytes));
    }

    [Test]
    public void RejectsNonzeroMetricDataFormatAndZeroMetricCount()
    {
        byte[] invalidFormat = BuildHhea(1, 500);
        WriteInt16(invalidFormat, 32, 1);
        byte[] zeroCount = BuildHhea(1, 500);
        WriteUInt16(zeroCount, 34, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseHhea(invalidFormat));
            Assert.Throws<FontFormatException>(() => ParseHhea(zeroCount));
        });
    }

    [Test]
    public void ParsesCompressedTrailingBearingsAndRepeatsLastAdvanceWidth()
    {
        byte[] hhea = BuildHhea(3, 700);
        byte[] maxp = BuildCffMaxp(5);
        byte[] hmtx = BuildHmtx(
            new int[] { 500, 600, 700 },
            new int[] { -10, 20, -30 },
            new int[] { 40, -50 });

        SfntHmtxTable? table = ParseHmtx(hhea, hmtx, maxp);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetGlyphCount(), Is.EqualTo(5));
            Assert.That(table.GetMetricCount(), Is.EqualTo(3));
            Assert.That(table.GetTrailingBearingCount(), Is.EqualTo(2));
            Assert.That(table.GetAdvanceWidth(0), Is.EqualTo(500));
            Assert.That(table.GetAdvanceWidth(1), Is.EqualTo(600));
            Assert.That(table.GetAdvanceWidth(2), Is.EqualTo(700));
            Assert.That(table.GetAdvanceWidth(3), Is.EqualTo(700));
            Assert.That(table.GetAdvanceWidth(4), Is.EqualTo(700));
            Assert.That(table.GetLeftSideBearing(0), Is.EqualTo(-10));
            Assert.That(table.GetLeftSideBearing(1), Is.EqualTo(20));
            Assert.That(table.GetLeftSideBearing(2), Is.EqualTo(-30));
            Assert.That(table.GetLeftSideBearing(3), Is.EqualTo(40));
            Assert.That(table.GetLeftSideBearing(4), Is.EqualTo(-50));
            Assert.That(table.UsesSharedAdvanceWidth(2), Is.False);
            Assert.That(table.UsesSharedAdvanceWidth(3), Is.True);
        });
    }

    [Test]
    public void ParsesOneMetricSharedByEveryRemainingGlyph()
    {
        SfntHmtxTable? table = ParseHmtx(
            BuildHhea(1, 65535),
            BuildHmtx(
                new int[] { 65535 },
                new int[] { short.MinValue },
                new int[] { 0, short.MaxValue }),
            BuildTrueTypeMaxp(3));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetAdvanceWidth(0), Is.EqualTo(65535));
            Assert.That(table.GetAdvanceWidth(1), Is.EqualTo(65535));
            Assert.That(table.GetAdvanceWidth(2), Is.EqualTo(65535));
            Assert.That(table.GetLeftSideBearing(0), Is.EqualTo(short.MinValue));
            Assert.That(table.GetLeftSideBearing(1), Is.Zero);
            Assert.That(table.GetLeftSideBearing(2), Is.EqualTo(short.MaxValue));
            Assert.That(table.GetTrailingBearingCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ParsesOneLongMetricPerGlyphWithoutTrailingBearings()
    {
        SfntHmtxTable? table = ParseHmtx(
            BuildHhea(2, 600),
            BuildHmtx(
                new int[] { 500, 600 },
                new int[] { -1, 2 },
                Array.Empty<int>()),
            BuildCffMaxp(2));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMetricCount(), Is.EqualTo(2));
            Assert.That(table.GetTrailingBearingCount(), Is.Zero);
            Assert.That(table.GetAdvanceWidth(1), Is.EqualTo(600));
            Assert.That(table.GetLeftSideBearing(1), Is.EqualTo(2));
            Assert.That(table.UsesSharedAdvanceWidth(1), Is.False);
        });
    }

    [Test]
    public void RejectsMetricCountGreaterThanGlyphCount()
    {
        byte[] hhea = BuildHhea(3, 700);
        byte[] hmtx = BuildHmtx(
            new int[] { 500, 600, 700 },
            new int[] { 0, 0, 0 },
            Array.Empty<int>());

        Assert.Throws<FontFormatException>(() => ParseHmtx(
            hhea,
            hmtx,
            BuildCffMaxp(2)));
    }

    [Test]
    public void RejectsEveryHmtxTruncationAndTrailingData()
    {
        byte[] hhea = BuildHhea(2, 600);
        byte[] maxp = BuildCffMaxp(4);
        byte[] complete = BuildHmtx(
            new int[] { 500, 600 },
            new int[] { -1, 2 },
            new int[] { 3, 4 });

        for (int length = 0; length < complete.Length; length += 1)
        {
            byte[] truncated = complete.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => ParseHmtx(hhea, truncated, maxp),
                "Length {0}",
                length);
        }

        Assert.Throws<FontFormatException>(() => ParseHmtx(
            hhea,
            Append(complete, new byte[] { 0 }),
            maxp));
    }

    [Test]
    public void RejectsAdvanceWidthMaximumMismatch()
    {
        byte[] hmtx = BuildHmtx(
            new int[] { 500, 600 },
            new int[] { 0, 0 },
            Array.Empty<int>());

        Assert.Throws<FontFormatException>(() => ParseHmtx(
            BuildHhea(2, 601),
            hmtx,
            BuildCffMaxp(2)));
    }

    [Test]
    public void HmtxRequiresBothHheaAndMaxpDependencies()
    {
        byte[] hmtx = BuildHmtx(
            new int[] { 500 },
            new int[] { 0 },
            Array.Empty<int>());
        SfntTableCache missingHhea = CreateCache(null, hmtx, BuildCffMaxp(1));
        SfntTableCache missingMaxp = CreateCache(BuildHhea(1, 500), hmtx, null);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => SfntHmtxTableParser.FindTable(
                missingHhea));
            Assert.Throws<FontFormatException>(() => SfntHmtxTableParser.FindTable(
                missingMaxp));
        });
    }

    [Test]
    public void MetricAccessRejectsGlyphIdsOutsideMaxpRange()
    {
        SfntHmtxTable? table = ParseHmtx(
            BuildHhea(1, 500),
            BuildHmtx(
                new int[] { 500 },
                new int[] { 0 },
                Array.Empty<int>()),
            BuildCffMaxp(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAdvanceWidth(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAdvanceWidth(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetLeftSideBearing(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetLeftSideBearing(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.UsesSharedAdvanceWidth(1));
        });
    }

    [Test]
    public void FaceCachesHeaderAndMetricsAndChecksCountAgainstMaxp()
    {
        SfntTableCache cache = CreateCache(
            BuildHhea(1, 500),
            BuildHmtx(
                new int[] { 500 },
                new int[] { 3 },
                new int[] { 4 }),
            BuildCffMaxp(2));
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.Cff1,
            Array.Empty<FontDiagnostic>());
        FontFace face = new FontFace(0, 0x4F54544FU, validation, cache);

        SfntHheaTable? firstHeader = face.GetHheaTable();
        SfntHmtxTable? firstMetrics = face.GetHmtxTable();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetHheaTable(), Is.SameAs(firstHeader));
            Assert.That(face.GetHmtxTable(), Is.SameAs(firstMetrics));
            Assert.That(face.GetHheaTableView()!.GetParsedData(), Is.SameAs(firstHeader));
            Assert.That(face.GetHmtxTableView()!.GetParsedData(), Is.SameAs(firstMetrics));
            Assert.That(firstMetrics!.GetLeftSideBearing(1), Is.EqualTo(4));
        });

        SfntTableCache badCache = CreateCache(
            BuildHhea(3, 500),
            null,
            BuildCffMaxp(2));
        FontFace badFace = new FontFace(0, 0x4F54544FU, validation, badCache);
        Assert.Throws<FontFormatException>(() => badFace.GetHheaTable());
    }

    [Test]
    public void MissingHorizontalTablesReturnNoViews()
    {
        SfntTableStore store = new SfntTableStore(
            Array.Empty<SfntTableData>(),
            Array.Empty<FontDiagnostic>());
        SfntTableCache cache = SfntTableCache.Create(store, FontLoadMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(SfntHheaTableParser.FindView(cache), Is.Null);
            Assert.That(SfntHmtxTableParser.FindView(cache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntHheaTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntHmtxTableParser.FindView(null!));
        });
    }

    static SfntHheaTable? ParseHhea(byte[] hhea)
    {
        return SfntHheaTableParser.FindTable(CreateCache(hhea, null, null));
    }

    static SfntHmtxTable? ParseHmtx(byte[] hhea, byte[] hmtx, byte[] maxp)
    {
        return SfntHmtxTableParser.FindTable(CreateCache(hhea, hmtx, maxp));
    }

    static SfntTableCache CreateCache(byte[]? hhea, byte[]? hmtx, byte[]? maxp)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Hhea, hhea);
        AddTable(tables, FontTags.Hmtx, hmtx);
        AddTable(tables, FontTags.Maxp, maxp);
        SfntTableStore store = new SfntTableStore(
            tables.ToArray(),
            Array.Empty<FontDiagnostic>());
        return SfntTableCache.Create(store, FontLoadMode.Compatible);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[]? bytes)
    {
        if (bytes == null)
        {
            return;
        }

        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildHhea(int metricCount, int advanceWidthMaximum)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt16(900)
            .WriteInt16(-300)
            .WriteInt16(-20)
            .WriteUInt16((ushort)advanceWidthMaximum)
            .WriteInt16(-40)
            .WriteInt16(-50)
            .WriteInt16(680)
            .WriteInt16(1)
            .WriteInt16(-2)
            .WriteInt16(3)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16((ushort)metricCount)
            .CopyBytes();
    }

    static byte[] BuildHmtx(
        int[] advanceWidths,
        int[] metricBearings,
        int[] trailingBearings)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();

        for (int index = 0; index < advanceWidths.Length; index += 1)
        {
            builder
                .WriteUInt16((ushort)advanceWidths[index])
                .WriteInt16((short)metricBearings[index]);
        }

        for (int index = 0; index < trailingBearings.Length; index += 1)
        {
            builder.WriteInt16((short)trailingBearings[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildCffMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] BuildTrueTypeMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
    }

    static byte[] Append(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), (ushort)value);
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(offset, 2), (short)value);
    }
}
