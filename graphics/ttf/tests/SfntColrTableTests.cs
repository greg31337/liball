using ttf;

namespace ttf.Tests;

internal class SfntColrTableTests
{
    [Test]
    public void ParsesSortedBaseGlyphsAndOverlappingLayerSlices()
    {
        SfntColrTable? table = Parse(BuildColr(), BuildCpal(), BuildMaxp(10));
        SfntColrBaseGlyphRecord first = table!.GetBaseGlyphRecord(0);
        SfntColrBaseGlyphRecord second = table.GetBaseGlyphRecord(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.Zero);
            Assert.That(table.GetBaseGlyphRecordCount(), Is.EqualTo(3));
            Assert.That(table.GetLayerRecordCount(), Is.EqualTo(5));
            Assert.That(first.GetGlyphId(), Is.EqualTo(2));
            Assert.That(first.GetFirstLayerIndex(), Is.Zero);
            Assert.That(first.GetLayerCount(), Is.EqualTo(2));
            Assert.That(first.GetLayer(0), Is.SameAs(table.GetLayerRecord(0)));
            Assert.That(first.GetLayer(1), Is.SameAs(table.GetLayerRecord(1)));
            Assert.That(second.GetGlyphId(), Is.EqualTo(5));
            Assert.That(second.GetFirstLayerIndex(), Is.EqualTo(1));
            Assert.That(second.GetLayerCount(), Is.EqualTo(3));
            Assert.That(second.GetLayer(0), Is.SameAs(first.GetLayer(1)));
            Assert.That(table.GetLayerRecord(0).GetGlyphId(), Is.EqualTo(3));
            Assert.That(table.GetLayerRecord(0).GetPaletteEntryIndex(), Is.Zero);
            Assert.That(table.GetLayerRecord(2).UsesForegroundColor(), Is.True);
        });
    }

    [Test]
    public void FindsBaseGlyphRecordsWithBinarySearch()
    {
        SfntColrTable? table = Parse(BuildColr(), BuildCpal(), BuildMaxp(10));

        Assert.Multiple(() =>
        {
            Assert.That(table!.FindBaseGlyphRecord(2),
                Is.SameAs(table.GetBaseGlyphRecord(0)));
            Assert.That(table.FindBaseGlyphRecord(5),
                Is.SameAs(table.GetBaseGlyphRecord(1)));
            Assert.That(table.FindBaseGlyphRecord(8),
                Is.SameAs(table.GetBaseGlyphRecord(2)));
            Assert.That(table.FindBaseGlyphRecord(4), Is.Null);
        });
    }

    [Test]
    public void ResolvesOrderedLayersAgainstSelectedPaletteAndForeground()
    {
        SfntColrTable? table = Parse(BuildColr(), BuildCpal(), BuildMaxp(10));
        SfntCpalColor foreground = new SfntCpalColor(90, 91, 92, 93);
        SfntColrLayerGlyphClosure? closure =
            table!.ResolveLayerGlyphClosure(5, 1, foreground);

        Assert.Multiple(() =>
        {
            Assert.That(closure!.GetBaseGlyphId(), Is.EqualTo(5));
            Assert.That(closure.GetPaletteIndex(), Is.EqualTo(1));
            Assert.That(closure.GetLayerCount(), Is.EqualTo(3));
            Assert.That(closure.GetLayer(0).GetGlyphId(), Is.EqualTo(4));
            Assert.That(closure.GetLayer(0).GetPaletteEntryIndex(), Is.EqualTo(1));
            Assert.That(closure.GetLayer(0).GetColor().GetBlue(), Is.EqualTo(25));
            Assert.That(closure.GetLayer(1).GetGlyphId(), Is.EqualTo(6));
            Assert.That(closure.GetLayer(1).UsesForegroundColor(), Is.True);
            Assert.That(closure.GetLayer(1).GetColor(), Is.SameAs(foreground));
            Assert.That(closure.GetLayer(2).GetGlyphId(), Is.EqualTo(4));
            Assert.That(closure.GetLayer(2).GetColor().GetBlue(), Is.EqualTo(29));
            Assert.That(closure.GetReferencedGlyphCount(), Is.EqualTo(2));
            Assert.That(closure.GetReferencedGlyphId(0), Is.EqualTo(4));
            Assert.That(closure.GetReferencedGlyphId(1), Is.EqualTo(6));
        });
    }

    [Test]
    public void ReturnsNoClosureForMonochromeGlyphAndValidatesResolutionInputs()
    {
        SfntColrTable? table = Parse(BuildColr(), BuildCpal(), BuildMaxp(10));
        SfntCpalColor foreground = new SfntCpalColor(0, 0, 0, 255);

        Assert.Multiple(() =>
        {
            Assert.That(table!.ResolveLayerGlyphClosure(1, 0, foreground), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.ResolveLayerGlyphClosure(-1, 0, foreground));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.ResolveLayerGlyphClosure(10, 0, foreground));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.ResolveLayerGlyphClosure(2, 2, foreground));
            Assert.Throws<ArgumentNullException>(() =>
                table.ResolveLayerGlyphClosure(5, 0, null!));
        });
    }

    [TestCaseSource(nameof(InvalidColrTables))]
    public void RejectsMalformedHeadersArraysGlyphsLayersAndPaletteEntries(
        byte[] colr,
        byte[] cpal,
        byte[] maxp)
    {
        Assert.Throws<FontFormatException>(() => Parse(colr, cpal, maxp));
    }

    [Test]
    public void RequiresSupportedMaxpAndCpalDependencies()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildColr(), BuildCpal(), null));
            Assert.Throws<FontFormatException>(() =>
                Parse(BuildColr(), null, BuildMaxp(10)));
        });
    }

    [Test]
    public void EnforcesConfiguredRecordLimit()
    {
        SfntTableCache cache = CreateCache(
            BuildColr(),
            BuildCpal(),
            BuildMaxp(10),
            FontLoadMode.Compatible,
            CreateLimits(2));

        Assert.Throws<FontLimitException>(() =>
            SfntColrTableParser.FindTable(cache));
    }

    [Test]
    public void IntegratesWithFontFaceAndKeepsUnknownVersionsRaw()
    {
        SfntTableCache cache = CreateCache(
            BuildColr(),
            BuildCpal(),
            BuildMaxp(10),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);
        SfntParsedTableView? raw = SfntColrTableParser.FindView(CreateCache(
            new byte[] { 0, 2 },
            null,
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            null,
            null,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(face.GetColrTable(), Is.Not.Null);
            Assert.That(face.GetColrTableView()!.GetParsedData(),
                Is.SameAs(face.GetColrTable()));
            Assert.That(raw!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(raw.HasParsedData(), Is.False);
            Assert.That(SfntColrTableParser.FindView(absent), Is.Null);
            Assert.That(SfntColrTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntColrTableParser.FindView(null!));
        });
    }

    [Test]
    public void ValidatesRecordAndClosureAccessorIndexes()
    {
        SfntColrTable? table = Parse(BuildColr(), BuildCpal(), BuildMaxp(10));
        SfntColrLayerGlyphClosure? closure = table!.ResolveLayerGlyphClosure(
            5,
            0,
            new SfntCpalColor(0, 0, 0, 255));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetBaseGlyphRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetBaseGlyphRecord(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetLayerRecord(5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetBaseGlyphRecord(0).GetLayer(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                closure!.GetLayer(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                closure!.GetReferencedGlyphId(2));
        });
    }

    static IEnumerable<TestCaseData> InvalidColrTables()
    {
        byte[] valid = BuildColr();
        yield return InvalidCase(valid[..13], BuildCpal(), BuildMaxp(10),
            "TruncatedHeader");

        byte[] noBaseGlyphs = BuildColr();
        PatchUInt16(noBaseGlyphs, 2, 0);
        yield return InvalidCase(noBaseGlyphs, BuildCpal(), BuildMaxp(10),
            "ZeroBaseGlyphs");

        byte[] noLayers = BuildColr();
        PatchUInt16(noLayers, 12, 0);
        yield return InvalidCase(noLayers, BuildCpal(), BuildMaxp(10),
            "ZeroLayers");

        byte[] baseArrayInHeader = BuildColr();
        PatchUInt32(baseArrayInHeader, 4, 8);
        yield return InvalidCase(baseArrayInHeader, BuildCpal(), BuildMaxp(10),
            "BaseArrayInHeader");

        byte[] layerArrayPastTable = BuildColr();
        PatchUInt32(layerArrayPastTable, 8, 1000);
        yield return InvalidCase(layerArrayPastTable, BuildCpal(), BuildMaxp(10),
            "LayerArrayPastTable");

        yield return InvalidCase(valid[..51], BuildCpal(), BuildMaxp(10),
            "TruncatedLayerArray");

        byte[] duplicateBaseGlyph = BuildColr();
        PatchUInt16(duplicateBaseGlyph, 20, 2);
        yield return InvalidCase(duplicateBaseGlyph, BuildCpal(), BuildMaxp(10),
            "DuplicateBaseGlyph");

        byte[] unsortedBaseGlyph = BuildColr();
        PatchUInt16(unsortedBaseGlyph, 20, 1);
        yield return InvalidCase(unsortedBaseGlyph, BuildCpal(), BuildMaxp(10),
            "UnsortedBaseGlyph");

        byte[] missingBaseGlyph = BuildColr();
        PatchUInt16(missingBaseGlyph, 26, 10);
        yield return InvalidCase(missingBaseGlyph, BuildCpal(), BuildMaxp(10),
            "MissingBaseGlyph");

        byte[] missingLayerGlyph = BuildColr();
        PatchUInt16(missingLayerGlyph, 32, 10);
        yield return InvalidCase(missingLayerGlyph, BuildCpal(), BuildMaxp(10),
            "MissingLayerGlyph");

        byte[] missingLayerSlice = BuildColr();
        PatchUInt16(missingLayerSlice, 22, 4);
        PatchUInt16(missingLayerSlice, 24, 2);
        yield return InvalidCase(missingLayerSlice, BuildCpal(), BuildMaxp(10),
            "MissingLayerSlice");

        byte[] missingPaletteEntry = BuildColr();
        PatchUInt16(missingPaletteEntry, 34, 3);
        yield return InvalidCase(missingPaletteEntry, BuildCpal(), BuildMaxp(10),
            "MissingPaletteEntry");
    }

    static TestCaseData InvalidCase(
        byte[] colr,
        byte[] cpal,
        byte[] maxp,
        string name)
    {
        return new TestCaseData(colr, cpal, maxp).SetName("Rejects" + name);
    }

    static SfntColrTable? Parse(byte[] colr, byte[]? cpal, byte[]? maxp)
    {
        return SfntColrTableParser.FindTable(CreateCache(
            colr,
            cpal,
            maxp,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? colr,
        byte[]? cpal,
        byte[]? maxp,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (colr != null)
        {
            tables.Add(CreateTable(FontTags.Colr, colr));
        }

        if (cpal != null)
        {
            tables.Add(CreateTable(FontTags.Cpal, cpal));
        }

        if (maxp != null)
        {
            tables.Add(CreateTable(FontTags.Maxp, maxp));
        }

        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
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

    static byte[] BuildColr()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt32(14)
            .WriteUInt32(32)
            .WriteUInt16(5)
            .WriteUInt16(2).WriteUInt16(0).WriteUInt16(2)
            .WriteUInt16(5).WriteUInt16(1).WriteUInt16(3)
            .WriteUInt16(8).WriteUInt16(4).WriteUInt16(1)
            .WriteUInt16(3).WriteUInt16(0)
            .WriteUInt16(4).WriteUInt16(1)
            .WriteUInt16(6).WriteUInt16(0xFFFF)
            .WriteUInt16(4).WriteUInt16(2)
            .WriteUInt16(7).WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildCpal()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt16(2)
            .WriteUInt16(6)
            .WriteUInt32(16)
            .WriteUInt16(0)
            .WriteUInt16(3)
            .WriteUInt8(1).WriteUInt8(2).WriteUInt8(3).WriteUInt8(255)
            .WriteUInt8(5).WriteUInt8(6).WriteUInt8(7).WriteUInt8(255)
            .WriteUInt8(9).WriteUInt8(10).WriteUInt8(11).WriteUInt8(255)
            .WriteUInt8(21).WriteUInt8(22).WriteUInt8(23).WriteUInt8(255)
            .WriteUInt8(25).WriteUInt8(26).WriteUInt8(27).WriteUInt8(255)
            .WriteUInt8(29).WriteUInt8(30).WriteUInt8(31).WriteUInt8(255)
            .CopyBytes();
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            1000,
            maxCoverageEntries,
            1000,
            1000000,
            100,
            100,
            10,
            100);
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
