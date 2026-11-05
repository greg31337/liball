namespace ttf.Tests;

internal class SfntBaseTableTests
{
    static int Ideo = (int)TestSfntEncoding.EncodeTag("ideo");
    static int Romn = (int)TestSfntEncoding.EncodeTag("romn");
    static int Latn = (int)TestSfntEncoding.EncodeTag("latn");
    static int Hani = (int)TestSfntEncoding.EncodeTag("hani");
    static int English = (int)TestSfntEncoding.EncodeTag("ENG ");
    static int Subs = (int)TestSfntEncoding.EncodeTag("subs");

    [Test]
    public void ParsesHorizontalAndVerticalAxesWithOrderedTagsAndScripts()
    {
        SfntBaseTable? table = Parse(BuildVersion10());
        SfntBaseAxis? horizontal = table!.GetHorizontalAxis();
        SfntBaseAxis? vertical = table.GetVerticalAxis();

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetItemVariationStoreOffset(), Is.Zero);
            Assert.That(horizontal!.IsHorizontal(), Is.True);
            Assert.That(horizontal.IsVertical(), Is.False);
            Assert.That(horizontal.GetBaselineTagCount(), Is.EqualTo(2));
            Assert.That(horizontal.GetBaselineTag(0), Is.EqualTo(Ideo));
            Assert.That(horizontal.GetBaselineTag(1), Is.EqualTo(Romn));
            Assert.That(horizontal.FindBaselineTagIndex(Romn), Is.EqualTo(1));
            Assert.That(horizontal.FindBaselineTagIndex(Hani), Is.EqualTo(-1));
            Assert.That(horizontal.GetScriptCount(), Is.EqualTo(1));
            Assert.That(horizontal.GetScript(0).GetScriptTag(), Is.EqualTo(Latn));
            Assert.That(horizontal.FindScript(Latn), Is.SameAs(horizontal.GetScript(0)));
            Assert.That(horizontal.FindScript(Hani), Is.Null);
            Assert.That(vertical!.IsVertical(), Is.True);
            Assert.That(vertical.GetBaselineTagCount(), Is.EqualTo(1));
            Assert.That(vertical.GetBaselineTag(0), Is.EqualTo(Ideo));
            Assert.That(vertical.GetScript(0).GetScriptTag(), Is.EqualTo(Hani));
        });
    }

    [Test]
    public void QueriesHorizontalAndVerticalBaselinesAndDefaultBaselines()
    {
        SfntBaseTable? table = Parse(BuildVersion10());
        SfntBaseAxis? horizontal = table!.GetHorizontalAxis();
        SfntBaseCoordinate? ideographic = table.FindHorizontalBaseline(Latn, Ideo);
        SfntBaseCoordinate? roman = table.FindHorizontalBaseline(Latn, Romn);
        SfntBaseCoordinate? vertical = table.FindVerticalBaseline(Hani, Ideo);

        Assert.Multiple(() =>
        {
            Assert.That(ideographic!.GetFormat(), Is.EqualTo(1));
            Assert.That(ideographic.GetCoordinate(), Is.EqualTo(-120));
            Assert.That(roman!.GetFormat(), Is.EqualTo(2));
            Assert.That(roman.GetCoordinate(), Is.Zero);
            Assert.That(roman.HasContourPointReference(), Is.True);
            Assert.That(roman.GetReferenceGlyphId(), Is.EqualTo(3));
            Assert.That(roman.GetContourPointIndex(), Is.EqualTo(7));
            Assert.That(horizontal!.FindDefaultBaseline(Latn), Is.SameAs(roman));
            Assert.That(horizontal.FindDefaultBaselineTag(Latn), Is.EqualTo(Romn));
            Assert.That(vertical!.GetFormat(), Is.EqualTo(3));
            Assert.That(vertical.GetCoordinate(), Is.EqualTo(500));
            Assert.That(vertical.HasDeviceOrVariationIndex(), Is.False);
            Assert.That(table.FindHorizontalBaseline(Hani, Romn), Is.Null);
            Assert.That(table.FindVerticalBaseline(Hani, Romn), Is.Null);
        });
    }

    [Test]
    public void ExposesScriptLanguageAndFeatureMinMaxCoordinates()
    {
        SfntBaseScript? script = Parse(BuildVersion10())!
            .GetHorizontalAxis()!
            .FindScript(Latn);
        SfntBaseMinMax? defaults = script!.GetDefaultMinMax();
        SfntBaseFeatureMinMax? feature = defaults!.FindFeature(Subs);
        SfntBaseLanguageSystem? language = script.FindLanguageSystem(English);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetMinimum()!.GetCoordinate(), Is.EqualTo(-250));
            Assert.That(defaults.GetMaximum()!.GetCoordinate(), Is.EqualTo(800));
            Assert.That(defaults.GetMaximum()!.GetFormat(), Is.EqualTo(3));
            Assert.That(defaults.GetMaximum()!.HasDeviceOrVariationIndex(), Is.True);
            Assert.That(defaults.GetMaximum()!.GetDeviceOffset(), Is.EqualTo(6));
            Assert.That(defaults.GetMaximum()!.GetDeviceAbsoluteOffset(), Is.EqualTo(86));
            Assert.That(defaults.GetMaximum()!.GetAdjustment()!.IsDevice(), Is.True);
            Assert.That(defaults.GetMaximum()!.GetAdjustment()!.GetDevice()!
                .GetStartSize(), Is.EqualTo(11));
            Assert.That(defaults.GetFeatureCount(), Is.EqualTo(1));
            Assert.That(defaults.GetFeature(0), Is.SameAs(feature));
            Assert.That(feature!.GetFeatureTag(), Is.EqualTo(Subs));
            Assert.That(feature.GetMinimum()!.GetCoordinate(), Is.EqualTo(-300));
            Assert.That(feature.GetMaximum(), Is.Null);
            Assert.That(defaults.FindFeature(Romn), Is.Null);
            Assert.That(script.GetLanguageSystemCount(), Is.EqualTo(1));
            Assert.That(script.GetLanguageSystem(0), Is.SameAs(language));
            Assert.That(language!.GetLanguageTag(), Is.EqualTo(English));
            Assert.That(language.GetMinMax().GetMinimum()!.GetCoordinate(), Is.EqualTo(-260));
            Assert.That(language.GetMinMax().GetMaximum()!.GetCoordinate(), Is.EqualTo(810));
            Assert.That(language.GetMinMax().GetFeatureCount(), Is.Zero);
            Assert.That(script.FindLanguageSystem(Romn), Is.Null);
        });
    }

    [Test]
    public void PreservesVersion11ItemVariationStoreOffsetAndCompatibleExtensions()
    {
        SfntBaseTable? version11 = Parse(BuildVersion11(1, 12));
        SfntParsedTableView? compatible = SfntBaseTableParser.FindView(CreateCache(
            BuildVersion11(2, 0),
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntBaseTableParser.FindView(CreateCache(
            BuildVersion11(2, 0),
            true,
            FontLoadMode.Strict,
            FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(version11!.GetMinorVersion(), Is.EqualTo(1));
            Assert.That(version11.GetItemVariationStoreOffset(), Is.EqualTo(12));
            Assert.That(version11.GetHorizontalAxis(), Is.Null);
            Assert.That(version11.GetVerticalAxis(), Is.Null);
            Assert.That(compatible!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.HasParsedData(), Is.True);
            Assert.That(((SfntBaseTable)compatible.GetParsedData()).GetMinorVersion(), Is.EqualTo(2));
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(strict.HasParsedData(), Is.False);
        });
    }

    [Test]
    public void AllowsScriptToOmitBaselineValues()
    {
        byte[] bytes = BuildVersion10();
        bytes[30] = 0;
        bytes[31] = 0;
        SfntBaseAxis? horizontal = Parse(bytes)!.GetHorizontalAxis();

        Assert.Multiple(() =>
        {
            Assert.That(horizontal!.FindScript(Latn)!.GetValues(), Is.Null);
            Assert.That(horizontal.FindBaseline(Latn, Romn), Is.Null);
            Assert.That(horizontal.FindDefaultBaseline(Latn), Is.Null);
            Assert.That(horizontal.FindDefaultBaselineTag(Latn), Is.Zero);
        });
    }

    [TestCaseSource(nameof(InvalidBaseTables))]
    public void RejectsMalformedBaseStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes));
    }

    [Test]
    public void EnforcesCoverageEntryLimit()
    {
        FontLimits limits = CreateLimits(1);

        Assert.Throws<FontLimitException>(() => SfntBaseTableParser.FindTable(
            CreateCache(
                BuildVersion10(),
                true,
                FontLoadMode.Compatible,
                limits)));
    }

    [Test]
    public void RequiresMaxpAndReturnsNullWhenBaseIsAbsent()
    {
        SfntTableCache missingMaxp = CreateCache(
            BuildVersion10(),
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache missingBase = CreateCache(
            null,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => SfntBaseTableParser.FindTable(missingMaxp));
            Assert.That(SfntBaseTableParser.FindView(missingBase), Is.Null);
            Assert.That(SfntBaseTableParser.FindTable(missingBase), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntBaseTableParser.FindView(null!));
        });
    }

    [Test]
    public void IntegratesWithFontFaceAndKeepsUnknownMajorVersionsRaw()
    {
        SfntTableCache cache = CreateCache(
            BuildVersion10(),
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(FontOutlineKind.Unknown, Array.Empty<FontDiagnostic>()),
            cache);
        byte[] unknown = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .CopyBytes();
        SfntParsedTableView? raw = SfntBaseTableParser.FindView(CreateCache(
            unknown,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(face.GetBaseTable(), Is.Not.Null);
            Assert.That(face.GetBaseTableView()!.GetParsedData(), Is.SameAs(face.GetBaseTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
        });
    }

    static IEnumerable<TestCaseData> InvalidBaseTables()
    {
        byte[] duplicateBaselineTag = BuildVersion10();
        Array.Copy(duplicateBaselineTag, 14, duplicateBaselineTag, 18, 4);
        yield return InvalidCase(duplicateBaselineTag, "DuplicateBaselineTag");

        byte[] mismatchedCoordinateCount = BuildVersion10();
        mismatchedCoordinateCount[44] = 0;
        mismatchedCoordinateCount[45] = 1;
        yield return InvalidCase(mismatchedCoordinateCount, "MismatchedCoordinateCount");

        byte[] invalidDefaultIndex = BuildVersion10();
        invalidDefaultIndex[42] = 0;
        invalidDefaultIndex[43] = 2;
        yield return InvalidCase(invalidDefaultIndex, "InvalidDefaultIndex");

        byte[] invalidReferenceGlyph = BuildVersion10();
        invalidReferenceGlyph[58] = 0;
        invalidReferenceGlyph[59] = 5;
        yield return InvalidCase(invalidReferenceGlyph, "InvalidReferenceGlyph");

        byte[] unsupportedCoordinate = BuildVersion10();
        unsupportedCoordinate[50] = 0;
        unsupportedCoordinate[51] = 4;
        yield return InvalidCase(unsupportedCoordinate, "UnsupportedCoordinate");

        byte[] coordinateIntoHeader = BuildVersion10();
        coordinateIntoHeader[46] = 0;
        coordinateIntoHeader[47] = 2;
        yield return InvalidCase(coordinateIntoHeader, "CoordinateIntoHeader");

        byte[] nullLanguageMinMax = BuildVersion10();
        nullLanguageMinMax[40] = 0;
        nullLanguageMinMax[41] = 0;
        yield return InvalidCase(nullLanguageMinMax, "NullLanguageMinMax");

        byte[] invalidFeatureTag = BuildVersion10();
        invalidFeatureTag[68] = 0;
        yield return InvalidCase(invalidFeatureTag, "InvalidFeatureTag");

        byte[] nullScriptList = BuildVersion10();
        nullScriptList[10] = 0;
        nullScriptList[11] = 0;
        yield return InvalidCase(nullScriptList, "NullScriptList");

        yield return InvalidCase(BuildVersion11(1, 0xFFFF), "InvalidItemStoreOffset");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntBaseTable? Parse(byte[] bytes)
    {
        return SfntBaseTableParser.FindTable(CreateCache(
            bytes,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? bytes,
        bool includeMaxp,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (bytes != null)
        {
            tables.Add(CreateTable(FontTags.Base, bytes));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(5)
                .CopyBytes()));
        }

        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            mode,
            limits);
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

    internal static byte[] BuildVersion10()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0);
        int horizontalOffset = WriteHorizontalAxis(builder);
        int verticalOffset = WriteVerticalAxis(builder);
        builder.PatchUInt16(4, (ushort)horizontalOffset);
        builder.PatchUInt16(6, (ushort)verticalOffset);
        return builder.CopyBytes();
    }

    static int WriteHorizontalAxis(TestBinaryBuilder builder)
    {
        int axis = builder.GetLength();
        builder.WriteUInt16(0).WriteUInt16(0);
        int tagList = builder.GetLength();
        builder.PatchUInt16(axis, (ushort)(tagList - axis));
        builder.WriteUInt16(2).WriteTag("ideo").WriteTag("romn");
        int scriptList = builder.GetLength();
        builder.PatchUInt16(axis + 2, (ushort)(scriptList - axis));
        builder.WriteUInt16(1).WriteTag("latn").WriteUInt16(0);
        int script = builder.GetLength();
        builder.PatchUInt16(scriptList + 6, (ushort)(script - scriptList));
        builder.WriteUInt16(0).WriteUInt16(0).WriteUInt16(1);
        builder.WriteTag("ENG ").WriteUInt16(0);

        int values = builder.GetLength();
        builder.PatchUInt16(script, (ushort)(values - script));
        builder.WriteUInt16(1).WriteUInt16(2).WriteUInt16(0).WriteUInt16(0);
        int ideographic = builder.GetLength();
        builder.PatchUInt16(values + 4, (ushort)(ideographic - values));
        builder.WriteUInt16(1).WriteInt16(-120);
        int roman = builder.GetLength();
        builder.PatchUInt16(values + 6, (ushort)(roman - values));
        builder.WriteUInt16(2).WriteInt16(0).WriteUInt16(3).WriteUInt16(7);

        int defaults = builder.GetLength();
        builder.PatchUInt16(script + 2, (ushort)(defaults - script));
        builder.WriteUInt16(0).WriteUInt16(0).WriteUInt16(1);
        builder.WriteTag("subs").WriteUInt16(0).WriteUInt16(0);
        int defaultMinimum = builder.GetLength();
        builder.PatchUInt16(defaults, (ushort)(defaultMinimum - defaults));
        builder.WriteUInt16(1).WriteInt16(-250);
        int defaultMaximum = builder.GetLength();
        builder.PatchUInt16(defaults + 2, (ushort)(defaultMaximum - defaults));
        builder.WriteUInt16(3).WriteInt16(800).WriteUInt16(6);
        builder.WriteUInt16(11).WriteUInt16(15).WriteUInt16(1);
        int featureMinimum = builder.GetLength();
        builder.PatchUInt16(defaults + 10, (ushort)(featureMinimum - defaults));
        builder.WriteUInt16(1).WriteInt16(-300);

        int language = builder.GetLength();
        builder.PatchUInt16(script + 10, (ushort)(language - script));
        builder.WriteUInt16(6).WriteUInt16(10).WriteUInt16(0);
        builder.WriteUInt16(1).WriteInt16(-260);
        builder.WriteUInt16(1).WriteInt16(810);
        return axis;
    }

    static int WriteVerticalAxis(TestBinaryBuilder builder)
    {
        int axis = builder.GetLength();
        builder.WriteUInt16(4).WriteUInt16(10);
        builder.WriteUInt16(1).WriteTag("ideo");
        builder.WriteUInt16(1).WriteTag("hani").WriteUInt16(8);
        builder.WriteUInt16(6).WriteUInt16(0).WriteUInt16(0);
        builder.WriteUInt16(0).WriteUInt16(1).WriteUInt16(6);
        builder.WriteUInt16(3).WriteInt16(500).WriteUInt16(0);
        return axis;
    }

    static byte[] BuildVersion11(int minorVersion, int itemStoreOffset)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)minorVersion)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt32((uint)itemStoreOffset);

        if (itemStoreOffset == 12)
        {
            builder.WriteUInt16(1);
        }

        return builder.CopyBytes();
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
