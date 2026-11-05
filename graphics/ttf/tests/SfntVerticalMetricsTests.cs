using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class SfntVerticalMetricsTests
{
    [TestCase(0, false)]
    [TestCase(0x1000, true)]
    public void ParsesEveryVheaField(int minorVersion, bool typographicDefinitions)
    {
        SfntVheaTable? table = ParseVhea(BuildVhea(3, 900, minorVersion));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.EqualTo(minorVersion));
            Assert.That(table.UsesTypographicFieldDefinitions(),
                Is.EqualTo(typographicDefinitions));
            Assert.That(table.GetAscender(), Is.EqualTo(500));
            Assert.That(table.GetDescender(), Is.EqualTo(-500));
            Assert.That(table.GetLineGap(), Is.EqualTo(40));
            Assert.That(table.GetAdvanceHeightMaximum(), Is.EqualTo(900));
            Assert.That(table.GetMinimumTopSideBearing(), Is.EqualTo(-20));
            Assert.That(table.GetMinimumBottomSideBearing(), Is.EqualTo(-30));
            Assert.That(table.GetMaximumYExtent(), Is.EqualTo(850));
            Assert.That(table.GetCaretSlopeRise(), Is.EqualTo(-2));
            Assert.That(table.GetCaretSlopeRun(), Is.EqualTo(1));
            Assert.That(table.GetCaretOffset(), Is.EqualTo(3));
            Assert.That(table.GetMetricDataFormat(), Is.Zero);
            Assert.That(table.GetMetricCount(), Is.EqualTo(3));
            Assert.That(table.GetExtensionLength(), Is.Zero);
        });
    }

    [Test]
    public void RejectsEveryVheaTruncationPoint()
    {
        byte[] complete = BuildVhea(1, 1000, 0x1000);

        for (int length = 0; length < complete.Length; length += 1)
        {
            Assert.Throws<FontFormatException>(
                () => ParseVhea(complete.AsSpan(0, length).ToArray()),
                "Length {0}",
                length);
        }
    }

    [Test]
    public void ValidatesVheaVersionReservedFieldsAndCounts()
    {
        byte[] invalidMinor = BuildVhea(1, 1000, 1);
        byte[] trailing = Append(BuildVhea(1, 1000, 0), new byte[] { 0 });
        byte[] invalidFormat = BuildVhea(1, 1000, 0);
        WriteInt16(invalidFormat, 32, 1);
        byte[] zeroCount = BuildVhea(1, 1000, 0);
        WriteUInt16(zeroCount, 34, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseVhea(invalidMinor));
            Assert.Throws<FontFormatException>(() => ParseVhea(trailing));
            Assert.Throws<FontFormatException>(() => ParseVhea(invalidFormat));
            Assert.Throws<FontFormatException>(() => ParseVhea(zeroCount));
        });

        foreach (int offset in new int[] { 24, 26, 28, 30 })
        {
            byte[] reserved = BuildVhea(1, 1000, 0);
            WriteInt16(reserved, offset, 1);
            Assert.Throws<FontFormatException>(() => ParseVhea(reserved));
        }
    }

    [Test]
    public void AppliesVheaCompatibleExtensionAndUnknownMajorGating()
    {
        byte[] extension = Append(BuildVhea(1, 1000, 0x2000), new byte[] { 7 });
        byte[] unknownMajor = BuildVhea(1, 1000, 0);
        WriteUInt16(unknownMajor, 0, 2);
        SfntTableCache compatibleCache = CreateCache(
            extension,
            null,
            null,
            null,
            null);
        SfntTableCache unknownCache = CreateCache(
            unknownMajor,
            null,
            null,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(SfntVheaTableParser.FindTable(compatibleCache)!.GetExtensionLength(),
                Is.EqualTo(1));
            Assert.That(SfntVheaTableParser.FindView(compatibleCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntVheaTableParser.FindTable(unknownCache), Is.Null);
            Assert.That(SfntVheaTableParser.FindView(unknownCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
        });
    }

    [Test]
    public void ParsesCompressedVerticalMetricsAndRepeatsLastAdvanceHeight()
    {
        SfntVmtxTable? table = ParseVmtx(
            BuildVhea(2, 900, 0x1000),
            BuildVmtx(
                new int[] { 800, 900 },
                new int[] { -10, 20 },
                new int[] { 30, -40 }),
            BuildCffMaxp(4));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetGlyphCount(), Is.EqualTo(4));
            Assert.That(table.GetMetricCount(), Is.EqualTo(2));
            Assert.That(table.GetTrailingBearingCount(), Is.EqualTo(2));
            Assert.That(table.GetAdvanceHeight(0), Is.EqualTo(800));
            Assert.That(table.GetAdvanceHeight(1), Is.EqualTo(900));
            Assert.That(table.GetAdvanceHeight(2), Is.EqualTo(900));
            Assert.That(table.GetAdvanceHeight(3), Is.EqualTo(900));
            Assert.That(table.GetTopSideBearing(0), Is.EqualTo(-10));
            Assert.That(table.GetTopSideBearing(1), Is.EqualTo(20));
            Assert.That(table.GetTopSideBearing(2), Is.EqualTo(30));
            Assert.That(table.GetTopSideBearing(3), Is.EqualTo(-40));
            Assert.That(table.UsesSharedAdvanceHeight(1), Is.False);
            Assert.That(table.UsesSharedAdvanceHeight(2), Is.True);
        });
    }

    [Test]
    public void ValidatesVerticalMetricDependenciesCountsLengthsAndMaximum()
    {
        byte[] vmtx = BuildVmtx(
            new int[] { 800, 900 },
            new int[] { 0, 0 },
            new int[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseVmtx(
                BuildVhea(2, 900, 0),
                vmtx.AsSpan(0, vmtx.Length - 1).ToArray(),
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => ParseVmtx(
                BuildVhea(2, 900, 0),
                Append(vmtx, new byte[] { 0 }),
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => ParseVmtx(
                BuildVhea(2, 901, 0),
                vmtx,
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => ParseVmtx(
                BuildVhea(4, 900, 0),
                vmtx,
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => SfntVmtxTableParser.FindTable(
                CreateCache(null, vmtx, BuildCffMaxp(3), null, null)));
            Assert.Throws<FontFormatException>(() => SfntVmtxTableParser.FindTable(
                CreateCache(BuildVhea(2, 900, 0), vmtx, null, null, null)));
        });
    }

    [Test]
    public void VerticalMetricAccessRejectsGlyphIdsOutsideMaxpRange()
    {
        SfntVmtxTable? table = ParseVmtx(
            BuildVhea(1, 1000, 0),
            BuildVmtx(new int[] { 1000 }, new int[] { 0 }, Array.Empty<int>()),
            BuildCffMaxp(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAdvanceHeight(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetAdvanceHeight(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetTopSideBearing(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetTopSideBearing(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.UsesSharedAdvanceHeight(1));
        });
    }

    [Test]
    public void ParsesVorgDefaultsAndSortedGlyphOverrides()
    {
        SfntVorgTable? table = ParseVorg(
            BuildVorg(880, new int[] { 1, 3 }, new int[] { 890, 870 }),
            BuildCffMaxp(5));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetDefaultVerticalOriginY(), Is.EqualTo(880));
            Assert.That(table.GetMetricCount(), Is.EqualTo(2));
            Assert.That(table.GetMetricGlyphId(0), Is.EqualTo(1));
            Assert.That(table.GetMetricVerticalOriginY(1), Is.EqualTo(870));
            Assert.That(table.HasGlyphOverride(1), Is.True);
            Assert.That(table.HasGlyphOverride(2), Is.False);
            Assert.That(table.GetVerticalOriginY(0), Is.EqualTo(880));
            Assert.That(table.GetVerticalOriginY(1), Is.EqualTo(890));
            Assert.That(table.GetVerticalOriginY(3), Is.EqualTo(870));
            Assert.That(table.GetExtensionLength(), Is.Zero);
        });
    }

    [Test]
    public void ParsesHeaderOnlyVorg()
    {
        SfntVorgTable? table = ParseVorg(
            BuildVorg(-200, Array.Empty<int>(), Array.Empty<int>()),
            BuildCffMaxp(2));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetMetricCount(), Is.Zero);
            Assert.That(table.GetVerticalOriginY(0), Is.EqualTo(-200));
            Assert.That(table.GetVerticalOriginY(1), Is.EqualTo(-200));
        });
    }

    [Test]
    public void RejectsMalformedVorgTables()
    {
        byte[] complete = BuildVorg(880, new int[] { 1 }, new int[] { 890 });

        for (int length = 0; length < complete.Length; length += 1)
        {
            Assert.Throws<FontFormatException>(() => ParseVorg(
                complete.AsSpan(0, length).ToArray(),
                BuildCffMaxp(3)),
                "Length {0}",
                length);
        }

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseVorg(
                Append(complete, new byte[] { 0 }),
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => ParseVorg(
                BuildVorg(880, new int[] { 2, 1 }, new int[] { 1, 2 }),
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => ParseVorg(
                BuildVorg(880, new int[] { 1, 1 }, new int[] { 1, 2 }),
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => ParseVorg(
                BuildVorg(880, new int[] { 3 }, new int[] { 1 }),
                BuildCffMaxp(3)));
            Assert.Throws<FontFormatException>(() => SfntVorgTableParser.FindTable(
                CreateCache(null, null, null, complete, null)));
        });
    }

    [Test]
    public void AppliesVorgVersionGatingAndCompatibleExtensions()
    {
        byte[] extension = Append(
            BuildVorg(880, Array.Empty<int>(), Array.Empty<int>()),
            new byte[] { 9 });
        WriteUInt16(extension, 2, 1);
        byte[] unknownMajor = BuildVorg(880, Array.Empty<int>(), Array.Empty<int>());
        WriteUInt16(unknownMajor, 0, 2);
        SfntTableCache extensionCache = CreateCache(
            null,
            null,
            BuildCffMaxp(1),
            extension,
            null);
        SfntTableCache unknownCache = CreateCache(
            null,
            null,
            BuildCffMaxp(1),
            unknownMajor,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(SfntVorgTableParser.FindTable(extensionCache)!.GetExtensionLength(),
                Is.EqualTo(1));
            Assert.That(SfntVorgTableParser.FindView(extensionCache)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(SfntVorgTableParser.FindTable(unknownCache), Is.Null);
        });
    }

    [Test]
    public void SynthesizesOneEmAdvancesAndHeadMaximumOriginsWhenVerticalTablesAreAbsent()
    {
        FontFace face = CreateFace(
            FontOutlineKind.Cff1,
            null,
            null,
            BuildCffMaxp(2),
            null,
            BuildHead(2048, 1100));
        SfntVerticalMetrics metrics = face.GetVerticalMetrics();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.UsesSynthesizedAdvanceHeights(), Is.True);
            Assert.That(metrics.UsesVorgOrigins(), Is.False);
            Assert.That(metrics.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(metrics.GetAdvanceHeight(0), Is.EqualTo(2048));
            Assert.That(metrics.GetAdvanceHeight(1), Is.EqualTo(2048));
            Assert.That(metrics.GetVerticalOriginY(1, 700), Is.EqualTo(1100));
            Assert.That(metrics.GetTopSideBearing(1, 700), Is.EqualTo(400));
            Assert.That(face.GetVerticalMetrics(), Is.SameAs(metrics));
        });
    }

    [Test]
    public void ResolvesCffOriginsFromVorgAndAdvancesFromVmtx()
    {
        FontFace face = CreateFace(
            FontOutlineKind.Cff1,
            BuildVhea(1, 1000, 0x1000),
            BuildVmtx(new int[] { 1000 }, new int[] { 50 }, new int[] { 60 }),
            BuildCffMaxp(2),
            BuildVorg(880, new int[] { 1 }, new int[] { 900 }),
            BuildHead(1000, 950));
        SfntVerticalMetrics metrics = face.GetVerticalMetrics();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.UsesSynthesizedAdvanceHeights(), Is.False);
            Assert.That(metrics.UsesVorgOrigins(), Is.True);
            Assert.That(metrics.GetAdvanceHeight(1), Is.EqualTo(1000));
            Assert.That(metrics.GetVerticalOriginY(0, 700), Is.EqualTo(880));
            Assert.That(metrics.GetVerticalOriginY(1, 700), Is.EqualTo(900));
            Assert.That(metrics.GetTopSideBearing(1, 700), Is.EqualTo(200));
        });
    }

    [Test]
    public void ResolvesOriginsFromGlyphMaximumAndVmtxWhenVorgIsAbsent()
    {
        FontFace face = CreateFace(
            FontOutlineKind.Cff2,
            BuildVhea(1, 1000, 0),
            BuildVmtx(new int[] { 1000 }, new int[] { 75 }, Array.Empty<int>()),
            BuildCffMaxp(1),
            null,
            BuildHead(1000, 950));
        SfntVerticalMetrics metrics = face.GetVerticalMetrics();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.UsesVorgOrigins(), Is.False);
            Assert.That(metrics.GetVerticalOriginY(0, 700), Is.EqualTo(775));
            Assert.That(metrics.GetTopSideBearing(0, 700), Is.EqualTo(75));
        });
    }

    [Test]
    public void IgnoresVorgForTrueTypeVerticalOrigins()
    {
        byte[] malformedVorg = BuildVorg(
            880,
            new int[] { 1, 0 },
            new int[] { 1, 2 });
        FontFace face = CreateFace(
            FontOutlineKind.TrueType,
            BuildVhea(1, 1000, 0),
            BuildVmtx(new int[] { 1000 }, new int[] { 40 }, Array.Empty<int>()),
            BuildTrueTypeMaxp(1),
            malformedVorg,
            BuildHead(1000, 950));
        SfntVerticalMetrics metrics = face.GetVerticalMetrics();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.UsesVorgOrigins(), Is.False);
            Assert.That(metrics.GetVerticalOriginY(0, 700), Is.EqualTo(740));
        });
    }

    [Test]
    public void RejectsAnUnpairedVerticalHeaderOrMetricsTable()
    {
        FontFace headerOnly = CreateFace(
            FontOutlineKind.Cff1,
            BuildVhea(1, 1000, 0),
            null,
            BuildCffMaxp(1),
            null,
            BuildHead(1000, 950));
        FontFace metricsOnly = CreateFace(
            FontOutlineKind.Cff1,
            null,
            BuildVmtx(new int[] { 1000 }, new int[] { 0 }, Array.Empty<int>()),
            BuildCffMaxp(1),
            null,
            BuildHead(1000, 950));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => headerOnly.GetVerticalMetrics());
            Assert.Throws<FontFormatException>(() => metricsOnly.GetVerticalMetrics());
        });
    }

    [Test]
    public void FaceCachesVerticalTableViewsAndValidatesHeaderCount()
    {
        FontFace face = CreateFace(
            FontOutlineKind.Cff1,
            BuildVhea(1, 1000, 0),
            BuildVmtx(new int[] { 1000 }, new int[] { 0 }, Array.Empty<int>()),
            BuildCffMaxp(1),
            BuildVorg(880, Array.Empty<int>(), Array.Empty<int>()),
            BuildHead(1000, 950));

        SfntVheaTable? vhea = face.GetVheaTable();
        SfntVmtxTable? vmtx = face.GetVmtxTable();
        SfntVorgTable? vorg = face.GetVorgTable();

        Assert.Multiple(() =>
        {
            Assert.That(face.GetVheaTable(), Is.SameAs(vhea));
            Assert.That(face.GetVmtxTable(), Is.SameAs(vmtx));
            Assert.That(face.GetVorgTable(), Is.SameAs(vorg));
            Assert.That(face.GetVheaTableView()!.GetParsedData(), Is.SameAs(vhea));
            Assert.That(face.GetVmtxTableView()!.GetParsedData(), Is.SameAs(vmtx));
            Assert.That(face.GetVorgTableView()!.GetParsedData(), Is.SameAs(vorg));
        });

        FontFace badFace = CreateFace(
            FontOutlineKind.Cff1,
            BuildVhea(2, 1000, 0),
            null,
            BuildCffMaxp(1),
            null,
            BuildHead(1000, 950));
        Assert.Throws<FontFormatException>(() => badFace.GetVheaTable());
    }

    [Test]
    public void MissingVerticalTablesReturnNoViews()
    {
        SfntTableStore store = new SfntTableStore(
            Array.Empty<SfntTableData>(),
            Array.Empty<FontDiagnostic>());
        SfntTableCache cache = SfntTableCache.Create(store, FontLoadMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(SfntVheaTableParser.FindView(cache), Is.Null);
            Assert.That(SfntVmtxTableParser.FindView(cache), Is.Null);
            Assert.That(SfntVorgTableParser.FindView(cache), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntVheaTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntVmtxTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntVorgTableParser.FindView(null!));
        });
    }

    static SfntVheaTable? ParseVhea(byte[] vhea)
    {
        return SfntVheaTableParser.FindTable(CreateCache(vhea, null, null, null, null));
    }

    static SfntVmtxTable? ParseVmtx(byte[] vhea, byte[] vmtx, byte[] maxp)
    {
        return SfntVmtxTableParser.FindTable(CreateCache(vhea, vmtx, maxp, null, null));
    }

    static SfntVorgTable? ParseVorg(byte[] vorg, byte[] maxp)
    {
        return SfntVorgTableParser.FindTable(CreateCache(null, null, maxp, vorg, null));
    }

    static FontFace CreateFace(
        FontOutlineKind outlineKind,
        byte[]? vhea,
        byte[]? vmtx,
        byte[] maxp,
        byte[]? vorg,
        byte[] head)
    {
        SfntValidationResult validation = new SfntValidationResult(
            outlineKind,
            Array.Empty<FontDiagnostic>());
        uint scalerType = outlineKind == FontOutlineKind.TrueType
            ? 0x00010000U
            : 0x4F54544FU;
        return new FontFace(
            0,
            scalerType,
            validation,
            CreateCache(vhea, vmtx, maxp, vorg, head));
    }

    static SfntTableCache CreateCache(
        byte[]? vhea,
        byte[]? vmtx,
        byte[]? maxp,
        byte[]? vorg,
        byte[]? head)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Vhea, vhea);
        AddTable(tables, FontTags.Vmtx, vmtx);
        AddTable(tables, FontTags.Maxp, maxp);
        AddTable(tables, FontTags.Vorg, vorg);
        AddTable(tables, FontTags.Head, head);
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

    static byte[] BuildVhea(int metricCount, int advanceHeightMaximum, int minorVersion)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)minorVersion)
            .WriteInt16(500)
            .WriteInt16(-500)
            .WriteInt16(40)
            .WriteUInt16((ushort)advanceHeightMaximum)
            .WriteInt16(-20)
            .WriteInt16(-30)
            .WriteInt16(850)
            .WriteInt16(-2)
            .WriteInt16(1)
            .WriteInt16(3)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteInt16(0)
            .WriteUInt16((ushort)metricCount)
            .CopyBytes();
    }

    static byte[] BuildVmtx(
        int[] advanceHeights,
        int[] metricBearings,
        int[] trailingBearings)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();

        for (int index = 0; index < advanceHeights.Length; index += 1)
        {
            builder
                .WriteUInt16((ushort)advanceHeights[index])
                .WriteInt16((short)metricBearings[index]);
        }

        for (int index = 0; index < trailingBearings.Length; index += 1)
        {
            builder.WriteInt16((short)trailingBearings[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildVorg(
        int defaultVerticalOriginY,
        int[] glyphIds,
        int[] verticalOriginsY)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt16((short)defaultVerticalOriginY)
            .WriteUInt16((ushort)glyphIds.Length);

        for (int index = 0; index < glyphIds.Length; index += 1)
        {
            builder
                .WriteUInt16((ushort)glyphIds[index])
                .WriteInt16((short)verticalOriginsY[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildHead(int unitsPerEm, int yMaximum)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0x5F0F3CF5U)
            .WriteUInt16(0)
            .WriteUInt16((ushort)unitsPerEm)
            .WriteInt64(0)
            .WriteInt64(0)
            .WriteInt16(-20)
            .WriteInt16(-30)
            .WriteInt16(1000)
            .WriteInt16((short)yMaximum)
            .WriteUInt16(0)
            .WriteUInt16(9)
            .WriteInt16(2)
            .WriteInt16(0)
            .WriteInt16(0)
            .CopyBytes();
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
