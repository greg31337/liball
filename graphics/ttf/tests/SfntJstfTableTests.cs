using ttf;

namespace ttf.Tests;

internal class SfntJstfTableTests
{
    static int Arab = (int)TestSfntEncoding.EncodeTag("arab");
    static int Fars = (int)TestSfntEncoding.EncodeTag("FARS");

    [Test]
    public void ParsesScriptsPrioritiesLookupActionsAndMaximums()
    {
        SfntJstfTable? table = Parse(BuildJstf(), true, true);
        SfntJstfScript? script = table!.FindScript(Arab);
        SfntJstfLanguageSystem? defaults =
            script!.GetDefaultLanguageSystem();
        SfntJstfPriority first = defaults!.GetPriority(0);
        SfntJstfOpportunity shrinkage = first.GetShrinkage();
        SfntJstfOpportunity extension = first.GetExtension();
        SfntJstfMaximum? maximum = extension.GetMaximum();
        SfntGposSingleAdjustment maximumAdjustment = maximum!.GetLookup(0)
            .GetSingleAdjustment(0);
        SfntJstfPriority second = defaults.GetPriority(1);
        SfntJstfLanguageSystem? farsi = script.FindLanguageSystem(Fars);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(table.GetMinorVersion(), Is.Zero);
            Assert.That(table.GetScriptCount(), Is.EqualTo(1));
            Assert.That(table.GetScript(0), Is.SameAs(script));
            Assert.That(table.FindScript(Fars), Is.Null);
            Assert.That(script.GetExtenderGlyphCount(), Is.EqualTo(2));
            Assert.That(script.GetExtenderGlyphId(0), Is.EqualTo(3));
            Assert.That(script.GetExtenderGlyphId(1), Is.EqualTo(5));
            Assert.That(defaults.GetLanguageTag(), Is.Zero);
            Assert.That(defaults.GetPriorityCount(), Is.EqualTo(2));
            Assert.That(shrinkage.IsShrinkage(), Is.True);
            Assert.That(shrinkage.IsExtension(), Is.False);
            Assert.That(shrinkage.HasActions(), Is.True);
            Assert.That(shrinkage.GetSubstitutionEnableList()!
                .GetLookupIndex(0), Is.Zero);
            Assert.That(extension.IsExtension(), Is.True);
            Assert.That(extension.GetSubstitutionDisableList()!
                .GetLookupIndex(0), Is.Zero);
            Assert.That(extension.GetPositioningEnableList()!
                .GetLookupIndex(0), Is.Zero);
            Assert.That(maximum.GetLookupCount(), Is.EqualTo(1));
            Assert.That(maximum.GetLookup(0).GetLookupType(), Is.EqualTo(1));
            Assert.That(maximumAdjustment.FindValueRecord(3)!
                .GetXAdvance(), Is.EqualTo(120));
            Assert.That(second.GetShrinkage().GetPositioningEnableList()!
                .GetLookupIndex(0), Is.Zero);
            Assert.That(script.GetLanguageSystemCount(), Is.EqualTo(1));
            Assert.That(script.GetLanguageSystem(0), Is.SameAs(farsi));
            Assert.That(farsi!.GetPriorityCount(), Is.EqualTo(1));
            Assert.That(farsi.GetPriority(0).GetShrinkage().HasActions(),
                Is.False);
            Assert.That(farsi.GetPriority(0).GetExtension().HasActions(),
                Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetScript(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                script.GetExtenderGlyphId(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                defaults.GetPriority(2));
        });
    }

    [Test]
    public void AppliesVersionPolicyAndHandlesAbsentTable()
    {
        byte[] future = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .CopyBytes();
        SfntParsedTableView? compatible = SfntJstfTableParser.FindView(
            CreateCache(
                future,
                true,
                false,
                FontLoadMode.Compatible,
                FontLimits.CreateCompatible()));
        SfntParsedTableView? strict = SfntJstfTableParser.FindView(
            CreateCache(
                future,
                true,
                false,
                FontLoadMode.Strict,
                FontLimits.CreateCompatible()));
        SfntTableCache absent = CreateCache(
            null,
            true,
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(compatible!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.CompatibleExtension));
            Assert.That(compatible.HasParsedData(), Is.True);
            Assert.That(((SfntJstfTable)compatible.GetParsedData())
                .GetMinorVersion(), Is.EqualTo(1));
            Assert.That(strict!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.RawOnly));
            Assert.That(strict.HasParsedData(), Is.False);
            Assert.That(SfntJstfTableParser.FindView(absent), Is.Null);
            Assert.That(SfntJstfTableParser.FindTable(absent), Is.Null);
            Assert.Throws<ArgumentNullException>(() =>
                SfntJstfTableParser.FindView(null!));
        });
    }

    [TestCaseSource(nameof(InvalidJstfTables))]
    public void RejectsMalformedJstfStructures(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes, true, true));
    }

    [Test]
    public void RequiresDependenciesAndEnforcesEntryLimits()
    {
        SfntTableCache missingMaxp = CreateCache(
            BuildJstf(),
            false,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache missingLayout = CreateCache(
            BuildJstf(),
            true,
            false,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache limited = CreateCache(
            BuildJstf(),
            true,
            true,
            FontLoadMode.Compatible,
            CreateLimits(1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntJstfTableParser.FindTable(missingMaxp));
            Assert.Throws<FontFormatException>(() =>
                SfntJstfTableParser.FindTable(missingLayout));
            Assert.Throws<FontLimitException>(() =>
                SfntJstfTableParser.FindTable(limited));
        });
    }

    [Test]
    public void IntegratesWithFontFaceInspection()
    {
        SfntTableCache cache = CreateCache(
            BuildJstf(),
            true,
            true,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.Unknown,
                Array.Empty<FontDiagnostic>()),
            cache);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetJstfTable(), Is.Not.Null);
            Assert.That(face.GetJstfTableView()!.GetParsedData(),
                Is.SameAs(face.GetJstfTable()));
        });
    }

    static IEnumerable<TestCaseData> InvalidJstfTables()
    {
        byte[] nullScript = BuildJstf();
        PatchUInt16(nullScript, 10, 0);
        yield return InvalidCase(nullScript, "NullScriptOffset");

        byte[] invalidExtender = BuildJstf();
        PatchUInt16(invalidExtender, 26, 10);
        yield return InvalidCase(invalidExtender, "InvalidExtenderGlyph");

        byte[] unorderedExtenders = BuildJstf();
        PatchUInt16(unorderedExtenders, 28, 2);
        yield return InvalidCase(unorderedExtenders, "UnorderedExtenders");

        byte[] invalidLookup = BuildJstf();
        PatchUInt16(invalidLookup, 62, 1);
        yield return InvalidCase(invalidLookup, "InvalidLookupIndex");

        byte[] priorityInsideHeader = BuildJstf();
        PatchUInt16(priorityInsideHeader, 32, 2);
        yield return InvalidCase(priorityInsideHeader, "PriorityInsideHeader");

        byte[] maximumLookupInsideHeader = BuildJstf();
        PatchUInt16(maximumLookupInsideHeader, 74, 2);
        yield return InvalidCase(
            maximumLookupInsideHeader,
            "MaximumLookupInsideHeader");

        byte[] contextualMaximum = BuildJstf();
        PatchUInt16(contextualMaximum, 76, 7);
        yield return InvalidCase(contextualMaximum, "ContextualMaximum");

        byte[] invalidLanguageTag = BuildJstf();
        invalidLanguageTag[18] = 0;
        yield return InvalidCase(invalidLanguageTag, "InvalidLanguageTag");

        yield return InvalidCase(
            BuildJstf()[..15],
            "TruncatedScript");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static SfntJstfTable? Parse(
        byte[] bytes,
        bool includeMaxp,
        bool includeLayout)
    {
        return SfntJstfTableParser.FindTable(CreateCache(
            bytes,
            includeMaxp,
            includeLayout,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? jstf,
        bool includeMaxp,
        bool includeLayout,
        FontLoadMode mode,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();

        if (jstf != null)
        {
            tables.Add(CreateTable(FontTags.Jstf, jstf));
        }

        if (includeMaxp)
        {
            tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
                .WriteUInt32(0x00005000U)
                .WriteUInt16(10)
                .CopyBytes()));
        }

        if (includeLayout)
        {
            tables.Add(CreateTable(FontTags.Gsub, BuildLayout(false)));
            tables.Add(CreateTable(FontTags.Gpos, BuildLayout(true)));
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

    internal static byte[] BuildLayout(bool positioning)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(10)
            .WriteUInt16(12)
            .WriteUInt16(14)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(8);

        if (positioning)
        {
            builder.WriteUInt16(1)
                .WriteUInt16(8)
                .WriteUInt16(0x0004)
                .WriteInt16(10);
        }
        else
        {
            builder.WriteUInt16(1)
                .WriteUInt16(6)
                .WriteInt16(1);
        }

        builder.WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(3);
        return builder.CopyBytes();
    }

    internal static byte[] BuildJstf()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteTag("arab")
            .WriteUInt16(0);
        int script = builder.GetLength();
        builder.PatchUInt16(10, (ushort)script);
        builder.WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteTag("FARS")
            .WriteUInt16(0);

        int extenders = builder.GetLength();
        builder.PatchUInt16(script, (ushort)(extenders - script));
        builder.WriteUInt16(2).WriteUInt16(3).WriteUInt16(5);

        int defaults = builder.GetLength();
        builder.PatchUInt16(script + 2, (ushort)(defaults - script));
        builder.WriteUInt16(2).WriteUInt16(0).WriteUInt16(0);

        int farsi = builder.GetLength();
        builder.PatchUInt16(script + 10, (ushort)(farsi - script));
        builder.WriteUInt16(1).WriteUInt16(0);

        int firstPriority = builder.GetLength();
        builder.PatchUInt16(
            defaults + 2,
            (ushort)(firstPriority - defaults));
        builder.WriteZeros(20);

        int shrinkSubstitutionEnable = builder.GetLength();
        builder.PatchUInt16(
            firstPriority,
            (ushort)(shrinkSubstitutionEnable - firstPriority));
        builder.WriteUInt16(1).WriteUInt16(0);

        int extendSubstitutionDisable = builder.GetLength();
        builder.PatchUInt16(
            firstPriority + 12,
            (ushort)(extendSubstitutionDisable - firstPriority));
        builder.WriteUInt16(1).WriteUInt16(0);

        int extendPositioningEnable = builder.GetLength();
        builder.PatchUInt16(
            firstPriority + 14,
            (ushort)(extendPositioningEnable - firstPriority));
        builder.WriteUInt16(1).WriteUInt16(0);

        int maximum = builder.GetLength();
        builder.PatchUInt16(
            firstPriority + 18,
            (ushort)(maximum - firstPriority));
        builder.WriteUInt16(1).WriteUInt16(0);
        int lookup = builder.GetLength();
        builder.PatchUInt16(maximum + 2, (ushort)(lookup - maximum));
        builder.WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(8);
        builder.WriteUInt16(1)
            .WriteUInt16(8)
            .WriteUInt16(0x0004)
            .WriteInt16(120);
        builder.WriteUInt16(1)
            .WriteUInt16(1)
            .WriteUInt16(3);

        int secondPriority = builder.GetLength();
        builder.PatchUInt16(
            defaults + 4,
            (ushort)(secondPriority - defaults));
        builder.WriteZeros(20);
        int shrinkPositioningEnable = builder.GetLength();
        builder.PatchUInt16(
            secondPriority + 4,
            (ushort)(shrinkPositioningEnable - secondPriority));
        builder.WriteUInt16(1).WriteUInt16(0);

        int farsiPriority = builder.GetLength();
        builder.PatchUInt16(
            farsi + 2,
            (ushort)(farsiPriority - farsi));
        builder.WriteZeros(20);
        return builder.CopyBytes();
    }

    static void PatchUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static FontLimits CreateLimits(int maximumEntries)
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
            maximumEntries,
            defaults.GetMaxStringBytes(),
            defaults.GetMaxDecompressedBytes(),
            defaults.GetMaxDecompressionRatio(),
            defaults.GetMaxVariationRegions(),
            defaults.GetMaxPaintDepth(),
            defaults.GetMaxDiagnostics());
    }
}
