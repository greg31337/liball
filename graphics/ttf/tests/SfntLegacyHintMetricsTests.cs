namespace ttf.Tests;

internal class SfntLegacyHintMetricsTests
{
    [Test]
    public void ParsesAndQueriesGaspRanges()
    {
        SfntGaspTable? table = ParseGasp(BuildGasp(1));

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetRangeCount(), Is.EqualTo(3));
            Assert.That(table.GetRange(0).GetMaximumPpem(), Is.EqualTo(8));
            Assert.That(table.GetRange(0).UsesGrayscaleRendering(), Is.True);
            Assert.That(table.GetRange(0).UsesGridFitting(), Is.False);
            Assert.That(table.GetRange(1).UsesGridFitting(), Is.True);
            Assert.That(table.GetRange(2).UsesSymmetricGridFitting(), Is.True);
            Assert.That(table.GetRange(2).UsesSymmetricSmoothing(), Is.True);
            Assert.That(table.FindRange(8), Is.SameAs(table.GetRange(0)));
            Assert.That(table.FindRange(9), Is.SameAs(table.GetRange(1)));
            Assert.That(table.FindRange(5000), Is.SameAs(table.GetRange(2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.FindRange(-1));
        });
    }

    [Test]
    public void ParsesAndQueriesHdmxDeviceWidths()
    {
        SfntHdmxTable? table = ParseHdmx(BuildHdmx());
        SfntHdmxRecord? first = table!.FindRecord(12);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.Zero);
            Assert.That(table.GetDeviceRecordSize(), Is.EqualTo(8));
            Assert.That(table.GetRecordCount(), Is.EqualTo(2));
            Assert.That(first, Is.SameAs(table.GetRecord(0)));
            Assert.That(first!.GetPixelSize(), Is.EqualTo(12));
            Assert.That(first.GetMaximumWidth(), Is.EqualTo(7));
            Assert.That(first.GetGlyphCount(), Is.EqualTo(5));
            Assert.That(first.GetWidth(2), Is.EqualTo(7));
            Assert.That(table.FindRecord(16)!.GetWidth(4), Is.EqualTo(8));
            Assert.That(table.FindRecord(14), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => table.FindRecord(256));
        });
    }

    [Test]
    public void ParsesAndQueriesVdmxRatiosGroupsAndHeightRecords()
    {
        SfntVdmxTable? table = ParseVdmx(BuildVdmx());
        SfntVdmxRatio square = table!.GetRatio(0);
        SfntVdmxRatio wide = table.GetRatio(1);
        SfntVdmxRatio fallback = table.GetRatio(2);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetGroupCount(), Is.EqualTo(2));
            Assert.That(table.GetRatioCount(), Is.EqualTo(3));
            Assert.That(square.GetCharacterSet(), Is.EqualTo(1));
            Assert.That(square.GetXRatio(), Is.EqualTo(1));
            Assert.That(square.GetStartingYRatio(), Is.EqualTo(1));
            Assert.That(square.GetEndingYRatio(), Is.EqualTo(1));
            Assert.That(square.IsDefaultRatio(), Is.False);
            Assert.That(wide.Matches(2, 1), Is.True);
            Assert.That(fallback.IsDefaultRatio(), Is.True);
            Assert.That(fallback.GetGroup(), Is.SameAs(square.GetGroup()));
            Assert.That(table.FindRatio(1, 1), Is.SameAs(square));
            Assert.That(table.FindRatio(2, 1), Is.SameAs(wide));
            Assert.That(table.FindRatio(2, 3), Is.SameAs(fallback));
            Assert.That(table.GetGroup(0).GetOffset(), Is.EqualTo(24));
            Assert.That(table.GetGroup(0).GetLength(), Is.EqualTo(16));
            Assert.That(table.GetGroup(0).GetStartingPixelHeight(), Is.EqualTo(8));
            Assert.That(table.GetGroup(0).GetEndingPixelHeight(), Is.EqualTo(12));
            Assert.That(table.GetGroup(0).GetRecordCount(), Is.EqualTo(2));
            Assert.That(table.GetGroup(0).FindRecord(12)!.GetMaximumY(), Is.EqualTo(10));
            Assert.That(table.GetGroup(0).FindRecord(12)!.GetMinimumY(), Is.EqualTo(-3));
            Assert.That(table.GetGroup(0).FindRecord(10), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => square.Matches(0, 1));
        });
    }

    [Test]
    public void ParsesLtshThresholdsWithoutUsingThemAsVectorMetrics()
    {
        SfntLtshTable? table = ParseLtsh(BuildLtsh());

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.Zero);
            Assert.That(table.GetGlyphCount(), Is.EqualTo(5));
            Assert.That(table.GetLinearThreshold(0), Is.EqualTo(1));
            Assert.That(table.GetLinearThreshold(2), Is.EqualTo(24));
            Assert.That(table.IsLinearAtPpem(2, 23), Is.False);
            Assert.That(table.IsLinearAtPpem(2, 24), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => table.IsLinearAtPpem(0, -1));
        });
    }

    [Test]
    public void ParsesVersionZeroGaspAndVdmxForms()
    {
        byte[] vdmxBytes = BuildVdmx();
        vdmxBytes[0] = 0;
        vdmxBytes[1] = 0;
        SfntGaspTable? gasp = ParseGasp(BuildGasp(0));
        SfntVdmxTable? vdmx = ParseVdmx(vdmxBytes);

        Assert.Multiple(() =>
        {
            Assert.That(gasp!.GetVersion(), Is.Zero);
            Assert.That(gasp.GetRange(2).GetBehavior(), Is.EqualTo(3));
            Assert.That(gasp.GetRange(2).UsesSymmetricGridFitting(), Is.False);
            Assert.That(vdmx!.GetVersion(), Is.Zero);
            Assert.That(vdmx.GetRatioCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void PreservesFullWidthVdmxPixelHeights()
    {
        byte[] bytes = BuildVdmx();
        bytes[34] = 1;
        bytes[35] = 44;
        SfntVdmxGroup group = ParseVdmx(bytes)!.GetGroup(0);

        Assert.Multiple(() =>
        {
            Assert.That(group.GetEndingPixelHeight(), Is.EqualTo(12));
            Assert.That(group.GetRecord(1).GetPixelHeight(), Is.EqualTo(300));
            Assert.That(group.FindRecord(300), Is.SameAs(group.GetRecord(1)));
        });
    }

    [TestCaseSource(nameof(InvalidGaspTables))]
    public void RejectsMalformedGaspTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseGasp(bytes));
    }

    [TestCaseSource(nameof(InvalidHdmxTables))]
    public void RejectsMalformedHdmxTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseHdmx(bytes));
    }

    [TestCaseSource(nameof(InvalidVdmxTables))]
    public void RejectsMalformedVdmxTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseVdmx(bytes));
    }

    [TestCaseSource(nameof(InvalidLtshTables))]
    public void RejectsMalformedLtshTables(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => ParseLtsh(bytes));
    }

    [Test]
    public void EnforcesConfiguredCoverageLimits()
    {
        FontLimits limits = CreateLimits(1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => SfntGaspTableParser.FindTable(
                CreateCache(FontTags.Gasp, BuildGasp(1), true, limits)));
            Assert.Throws<FontLimitException>(() => SfntVdmxTableParser.FindTable(
                CreateCache(FontTags.Vdmx, BuildVdmx(), true, limits)));
        });
    }

    [Test]
    public void MaxpDependentTablesRequireMaxpAndAllParsersHandleAbsence()
    {
        SfntTableCache missingMaxpHdmx = CreateCache(
            FontTags.Hdmx,
            BuildHdmx(),
            false,
            FontLimits.CreateCompatible());
        SfntTableCache missingMaxpLtsh = CreateCache(
            FontTags.Ltsh,
            BuildLtsh(),
            false,
            FontLimits.CreateCompatible());
        SfntTableCache empty = CreateCache(
            0,
            null,
            true,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => SfntHdmxTableParser.FindTable(missingMaxpHdmx));
            Assert.Throws<FontFormatException>(() => SfntLtshTableParser.FindTable(missingMaxpLtsh));
            Assert.That(SfntGaspTableParser.FindTable(empty), Is.Null);
            Assert.That(SfntHdmxTableParser.FindTable(empty), Is.Null);
            Assert.That(SfntVdmxTableParser.FindTable(empty), Is.Null);
            Assert.That(SfntLtshTableParser.FindTable(empty), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntGaspTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntHdmxTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntVdmxTableParser.FindView(null!));
            Assert.Throws<ArgumentNullException>(() => SfntLtshTableParser.FindView(null!));
        });
    }

    [Test]
    public void IntegratesAllFourTablesWithFontFace()
    {
        SfntTableCache cache = CreateCompleteCache();
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(FontOutlineKind.TrueType, Array.Empty<FontDiagnostic>()),
            cache);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetGaspTable(), Is.Not.Null);
            Assert.That(face.GetGaspTableView()!.GetParsedData(), Is.SameAs(face.GetGaspTable()));
            Assert.That(face.GetHdmxTable(), Is.Not.Null);
            Assert.That(face.GetHdmxTableView()!.GetParsedData(), Is.SameAs(face.GetHdmxTable()));
            Assert.That(face.GetVdmxTable(), Is.Not.Null);
            Assert.That(face.GetVdmxTableView()!.GetParsedData(), Is.SameAs(face.GetVdmxTable()));
            Assert.That(face.GetLtshTable(), Is.Not.Null);
            Assert.That(face.GetLtshTableView()!.GetParsedData(), Is.SameAs(face.GetLtshTable()));
        });
    }

    [TestCase(FontTags.Gasp, 2)]
    [TestCase(FontTags.Hdmx, 1)]
    [TestCase(FontTags.Vdmx, 2)]
    [TestCase(FontTags.Ltsh, 1)]
    public void KeepsUnknownVersionsAsRawData(int tag, int version)
    {
        byte[] bytes = new TestBinaryBuilder()
            .WriteUInt16((ushort)version)
            .WriteUInt16(0)
            .CopyBytes();
        SfntTableCache cache = CreateCache(
            tag,
            bytes,
            true,
            FontLimits.CreateCompatible());
        SfntParsedTableView? view;

        if (tag == FontTags.Gasp)
        {
            view = SfntGaspTableParser.FindView(cache);
        }
        else if (tag == FontTags.Hdmx)
        {
            view = SfntHdmxTableParser.FindView(cache);
        }
        else if (tag == FontTags.Vdmx)
        {
            view = SfntVdmxTableParser.FindView(cache);
        }
        else
        {
            view = SfntLtshTableParser.FindView(cache);
        }

        Assert.Multiple(() =>
        {
            Assert.That(view!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(view.HasParsedData(), Is.False);
        });
    }

    static IEnumerable<TestCaseData> InvalidGaspTables()
    {
        byte[] unordered = BuildGasp(1);
        unordered[8] = 0;
        unordered[9] = 8;
        yield return InvalidCase(unordered, "GaspUnorderedRanges");

        byte[] missingSentinel = BuildGasp(1);
        missingSentinel[12] = 0;
        missingSentinel[13] = 32;
        yield return InvalidCase(missingSentinel, "GaspMissingSentinel");

        byte[] reservedFlags = BuildGasp(1);
        reservedFlags[6] = 0x10;
        yield return InvalidCase(reservedFlags, "GaspReservedFlags");

        byte[] versionZeroFlags = BuildGasp(0);
        versionZeroFlags[14] = 0;
        versionZeroFlags[15] = 4;
        yield return InvalidCase(versionZeroFlags, "GaspVersionZeroSymmetricFlags");

        yield return InvalidCase(BuildGasp(1).SkipLast(1).ToArray(), "GaspTruncated");
    }

    static IEnumerable<TestCaseData> InvalidHdmxTables()
    {
        byte[] wrongSize = BuildHdmx();
        wrongSize[7] = 7;
        yield return InvalidCase(wrongSize, "HdmxWrongRecordSize");

        byte[] unordered = BuildHdmx();
        unordered[16] = 12;
        yield return InvalidCase(unordered, "HdmxUnorderedRecords");

        byte[] wrongMaximum = BuildHdmx();
        wrongMaximum[9] = 8;
        yield return InvalidCase(wrongMaximum, "HdmxWrongMaximum");

        byte[] nonzeroPadding = BuildHdmx();
        nonzeroPadding[15] = 1;
        yield return InvalidCase(nonzeroPadding, "HdmxNonzeroPadding");

        yield return InvalidCase(BuildHdmx().SkipLast(1).ToArray(), "HdmxTruncated");
    }

    static IEnumerable<TestCaseData> InvalidVdmxTables()
    {
        byte[] earlyDefault = BuildVdmx();
        earlyDefault[7] = 0;
        earlyDefault[8] = 0;
        earlyDefault[9] = 0;
        yield return InvalidCase(earlyDefault, "VdmxEarlyDefaultRatio");

        byte[] badCharacterSet = BuildVdmx();
        badCharacterSet[6] = 2;
        yield return InvalidCase(badCharacterSet, "VdmxCharacterSet");

        byte[] wrongGroupCount = BuildVdmx();
        wrongGroupCount[3] = 3;
        yield return InvalidCase(wrongGroupCount, "VdmxGroupCount");

        byte[] badOffset = BuildVdmx();
        badOffset[18] = 0;
        badOffset[19] = 6;
        yield return InvalidCase(badOffset, "VdmxGroupOffset");

        byte[] unordered = BuildVdmx();
        unordered[34] = 0;
        unordered[35] = 8;
        yield return InvalidCase(unordered, "VdmxUnorderedRecords");

        byte[] wrongBounds = BuildVdmx();
        wrongBounds[26] = 13;
        wrongBounds[27] = 12;
        yield return InvalidCase(wrongBounds, "VdmxGroupBounds");

        byte[] invertedExtents = BuildVdmx();
        invertedExtents[30] = 0xFF;
        invertedExtents[31] = 0xF0;
        yield return InvalidCase(invertedExtents, "VdmxInvertedExtents");

        yield return InvalidCase(
            BuildVdmx().Concat(new byte[] { 0 }).ToArray(),
            "VdmxTrailingData");
    }

    static IEnumerable<TestCaseData> InvalidLtshTables()
    {
        byte[] wrongCount = BuildLtsh();
        wrongCount[3] = 4;
        yield return InvalidCase(wrongCount, "LtshGlyphCount");

        yield return InvalidCase(BuildLtsh().SkipLast(1).ToArray(), "LtshTruncated");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntGaspTable? ParseGasp(byte[] bytes)
    {
        return SfntGaspTableParser.FindTable(CreateCache(
            FontTags.Gasp,
            bytes,
            true,
            FontLimits.CreateCompatible()));
    }

    static SfntHdmxTable? ParseHdmx(byte[] bytes)
    {
        return SfntHdmxTableParser.FindTable(CreateCache(
            FontTags.Hdmx,
            bytes,
            true,
            FontLimits.CreateCompatible()));
    }

    static SfntVdmxTable? ParseVdmx(byte[] bytes)
    {
        return SfntVdmxTableParser.FindTable(CreateCache(
            FontTags.Vdmx,
            bytes,
            true,
            FontLimits.CreateCompatible()));
    }

    static SfntLtshTable? ParseLtsh(byte[] bytes)
    {
        return SfntLtshTableParser.FindTable(CreateCache(
            FontTags.Ltsh,
            bytes,
            true,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        int tag,
        byte[]? bytes,
        bool includeMaxp,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (bytes != null)
        {
            tables.Add(CreateTable(tag, bytes));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, BuildMaxp()));
        }

        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
    }

    static SfntTableCache CreateCompleteCache()
    {
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(FontTags.Gasp, BuildGasp(1)),
            CreateTable(FontTags.Hdmx, BuildHdmx()),
            CreateTable(FontTags.Vdmx, BuildVdmx()),
            CreateTable(FontTags.Ltsh, BuildLtsh()),
            CreateTable(FontTags.Maxp, BuildMaxp())
        };
        return SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(5)
            .CopyBytes();
    }

    static byte[] BuildGasp(int version)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)version)
            .WriteUInt16(3)
            .WriteUInt16(8)
            .WriteUInt16(2)
            .WriteUInt16(16)
            .WriteUInt16(1)
            .WriteUInt16(0xFFFF)
            .WriteUInt16((ushort)(version == 0 ? 3 : 12))
            .CopyBytes();
    }

    static byte[] BuildHdmx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteUInt32(8)
            .WriteUInt8(12)
            .WriteUInt8(7)
            .WriteBytes(new byte[] { 5, 6, 7, 4, 3 })
            .WriteUInt8(0)
            .WriteUInt8(16)
            .WriteUInt8(10)
            .WriteBytes(new byte[] { 8, 9, 10, 9, 8 })
            .WriteUInt8(0)
            .CopyBytes();
    }

    static byte[] BuildVdmx()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(3)
            .WriteBytes(new byte[] { 1, 1, 1, 1 })
            .WriteBytes(new byte[] { 0, 2, 1, 2 })
            .WriteBytes(new byte[] { 1, 0, 0, 0 })
            .WriteUInt16(24)
            .WriteUInt16(40)
            .WriteUInt16(24)
            .WriteUInt16(2)
            .WriteUInt8(8)
            .WriteUInt8(12)
            .WriteUInt16(8)
            .WriteInt16(7)
            .WriteInt16(-2)
            .WriteUInt16(12)
            .WriteInt16(10)
            .WriteInt16(-3)
            .WriteUInt16(2)
            .WriteUInt8(10)
            .WriteUInt8(20)
            .WriteUInt16(10)
            .WriteInt16(9)
            .WriteInt16(-2)
            .WriteUInt16(20)
            .WriteInt16(17)
            .WriteInt16(-4)
            .CopyBytes();
    }

    static byte[] BuildLtsh()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(5)
            .WriteBytes(new byte[] { 1, 12, 24, 1, 50 })
            .CopyBytes();
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
    {
        FontLimits defaults = FontLimits.CreateCompatible();
        return FontLimits.Create(
            defaults.GetMaxInputBytes(),
            defaults.GetMaxOutputBytes(),
            defaults.GetMaxFaces(),
            defaults.GetMaxTablesPerFace(),
            defaults.GetMaxGlyphsPerFace(),
            defaults.GetMaxPointsPerGlyph(),
            defaults.GetMaxContoursPerGlyph(),
            defaults.GetMaxComponentDepth(),
            defaults.GetMaxCffSubroutineDepth(),
            defaults.GetMaxLayoutNesting(),
            defaults.GetMaxLookupsPerTable(),
            defaults.GetMaxLayoutOperations(),
            maxCoverageEntries,
            defaults.GetMaxStringBytes(),
            defaults.GetMaxDecompressedBytes(),
            defaults.GetMaxDecompressionRatio(),
            defaults.GetMaxVariationRegions(),
            defaults.GetMaxPaintDepth(),
            defaults.GetMaxDiagnostics());
    }
}
