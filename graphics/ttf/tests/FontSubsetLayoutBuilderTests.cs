using ttf;

namespace ttf.Tests;

internal class FontSubsetLayoutBuilderTests
{
    [Test]
    public void ExposesComputedLayoutClosureThroughThePublicSubsetReport()
    {
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            CreateFace(BuildPrunableGsub(), null, null)
                .CreateDefaultInstance(),
            options);
        builder.AddGlyph(1);
        builder.AddFeature(Tag("liga"));

        FontSubsetClosureReport report = builder.Build().GetClosureReport();
        int[] copiedGlyphs = report.GetLayoutGlyphIds();
        copiedGlyphs[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(report.IsLayoutApplicable(), Is.True);
            Assert.That(report.RetainsGdef(), Is.False);
            Assert.That(report.GetLayoutGlyphIds(),
                Is.EqualTo(new int[] { 0, 1, 3 }));
            Assert.That(report.GetGsubFeatureIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(report.GetGsubLookupIndices(),
                Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(report.GetGposFeatureIndices(), Is.Empty);
            Assert.That(report.GetGposLookupIndices(), Is.Empty);
            Assert.That(report.GetGdefMarkGlyphSetIndices(), Is.Empty);
        });
    }

    [Test]
    public void PrunesLayoutGraphAndRemapsNestedLookupsDeterministically()
    {
        byte[] source = BuildPrunableGsub();
        FontSubsetLayoutClosure closure = new FontSubsetLayoutClosure(
            true,
            false,
            new int[] { 0, 1, 3 },
            new int[] { 0 },
            new int[] { 1, 2 },
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>());
        FontInstance instance = CreateFace(source, null, null)
            .CreateDefaultInstance();

        FontSubsetLayoutData first = FontSubsetLayoutBuilder.Create(
            instance, new int[] { 0, 2, -1, 1 }, closure);
        FontSubsetLayoutData second = FontSubsetLayoutBuilder.Create(
            instance, new int[] { 0, 2, -1, 1 }, closure);
        SfntGsubTable? table = ParseGsub(first.CopyGsub());
        SfntLayoutTable layout = table!.GetLayoutTable();
        SfntLayoutScript script = layout.GetScriptList().GetScript(0);
        SfntGsubContextSubstitution context = table.GetLookup(0)
            .GetContextSubstitution(0);

        Assert.Multiple(() =>
        {
            Assert.That(first.CopyGsub(), Is.EqualTo(second.CopyGsub()));
            Assert.That(table.GetLookupCount(), Is.EqualTo(2));
            Assert.That(table.GetLookup(0).GetLookupType(), Is.EqualTo(5));
            Assert.That(table.GetLookup(1).GetLookupType(), Is.EqualTo(1));
            Assert.That(layout.GetFeatureList().GetFeatureCount(),
                Is.EqualTo(1));
            Assert.That(layout.GetFeatureList().GetFeature(0)
                .GetLookupIndex(0), Is.Zero);
            Assert.That(layout.GetScriptList().GetScriptCount(),
                Is.EqualTo(1));
            Assert.That(script.GetScriptTag(), Is.EqualTo(Tag("latn")));
            Assert.That(script.GetLanguageSystemCount(), Is.Zero);
            Assert.That(script.GetDefaultLanguageSystem()!
                .GetFeatureIndex(0), Is.Zero);
            Assert.That(context.GetLookupRecords()!.GetRecord(0)
                .GetLookupListIndex(), Is.EqualTo(1));
        });
    }

    [Test]
    public void PrunesLookupMadeEmptyByGlyphFiltering()
    {
        FontSubsetLayoutClosure closure = new FontSubsetLayoutClosure(
            true,
            false,
            new int[] { 0, 3 },
            Array.Empty<int>(),
            new int[] { 0 },
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>());

        FontSubsetLayoutData result = FontSubsetLayoutBuilder.Create(
            CreateFace(BuildGsub(), null, null).CreateDefaultInstance(),
            new int[] { 0, -1, -1, 1 },
            closure);
        SfntLayoutTable layout = ParseGsub(result.CopyGsub())!
            .GetLayoutTable();

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLookupList().GetLookupCount(), Is.Zero);
            Assert.That(layout.GetFeatureList().GetFeatureCount(), Is.Zero);
            Assert.That(layout.GetScriptList().GetScriptCount(), Is.Zero);
        });
    }

    [Test]
    public void RewritesGsubGlyphReferencesAndDropsOmittedRules()
    {
        FontSubsetLayoutData result = CreateLayoutData(
            BuildGsub(), null, null, false, new int[] { 0 },
            Array.Empty<int>());

        SfntGsubTable? table = ParseGsub(result.CopyGsub());
        SfntGsubSingleSubstitution substitution = table!.GetLookup(0)
            .GetSingleSubstitution(0);

        Assert.Multiple(() =>
        {
            Assert.That(substitution.GetCoverage().GetCoveredGlyphCount(),
                Is.EqualTo(1));
            Assert.That(substitution.GetCoverage().GetGlyphId(0),
                Is.EqualTo(2));
            Assert.That(substitution.GetSubstituteGlyphId(2), Is.EqualTo(1));
            Assert.That(substitution.GetSubstituteGlyphId(3), Is.EqualTo(-1));
        });
    }

    [Test]
    public void RewritesGposCoverageAndPreservesVariationIndex()
    {
        FontSubsetLayoutData result = CreateLayoutData(
            null, BuildGpos(), BuildGdef(), true, Array.Empty<int>(),
            new int[] { 0 }, new int[] { 0 });

        SfntGposSingleAdjustment adjustment = ParseGpos(
            result.CopyGpos(), result.CopyGdef())!
            .GetLookup(0).GetSingleAdjustment(0);
        SfntGposValueRecord record = adjustment.GetValueRecord(0);

        Assert.Multiple(() =>
        {
            Assert.That(adjustment.GetCoverage().GetCoveredGlyphCount(),
                Is.EqualTo(1));
            Assert.That(adjustment.GetCoverage().GetGlyphId(0),
                Is.EqualTo(2));
            Assert.That(record.GetXAdvance(), Is.EqualTo(20));
            Assert.That(record.GetXAdvanceAdjustment()!.IsVariationIndex(),
                Is.True);
            Assert.That(record.GetXAdvanceAdjustment()!.GetVariationIndex()!
                .GetOuterIndex(), Is.EqualTo(0));
            Assert.That(record.GetXAdvanceAdjustment()!.GetVariationIndex()!
                .GetInnerIndex(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RewritesGdefClassesCaretsMarkSetsAndVariationStore()
    {
        FontSubsetLayoutData result = CreateLayoutData(
            null, null, BuildGdef(), true, Array.Empty<int>(),
            new int[] { 0 });

        SfntGdefTable? table = ParseGdef(result.CopyGdef());
        SfntGdefCaretValue caret = table!.GetLigatureCaretList()!
            .FindLigatureGlyph(2)!.GetCaretValue(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetGlyphClass(2), Is.EqualTo(1));
            Assert.That(table.GetGlyphClass(1), Is.Zero);
            Assert.That(caret.GetCoordinate(), Is.EqualTo(120));
            Assert.That(caret.GetAdjustment()!.GetVariationIndex()!
                .GetOuterIndex(), Is.Zero);
            Assert.That(table.GetMarkGlyphSets()!.GetSetCount(), Is.EqualTo(2));
            Assert.That(table.GetMarkGlyphSets()!.Covers(0, 2), Is.True);
            Assert.That(table.GetMarkGlyphSets()!.GetCoverage(1)
                .GetCoveredGlyphCount(), Is.Zero);
            Assert.That(table.GetItemVariationStore(), Is.Not.Null);
        });
    }

    static FontSubsetLayoutData CreateLayoutData(
        byte[]? gsub,
        byte[]? gpos,
        byte[]? gdef,
        bool retainGdef,
        int[] gsubLookups,
        int[] markSets)
    {
        return CreateLayoutData(
            gsub, gpos, gdef, retainGdef, gsubLookups, markSets,
            Array.Empty<int>());
    }

    static FontSubsetLayoutData CreateLayoutData(
        byte[]? gsub,
        byte[]? gpos,
        byte[]? gdef,
        bool retainGdef,
        int[] gsubLookups,
        int[] markSets,
        int[] gposLookups)
    {
        FontSubsetLayoutClosure closure = new FontSubsetLayoutClosure(
            true,
            retainGdef,
            new int[] { 0, 1, 3 },
            Array.Empty<int>(),
            gsubLookups,
            Array.Empty<int>(),
            gposLookups,
            markSets);
        return FontSubsetLayoutBuilder.Create(
            CreateFace(gsub, gpos, gdef).CreateDefaultInstance(),
            new int[] { 0, 2, -1, 1 },
            closure);
    }

    static FontFace CreateFace(byte[]? gsub, byte[]? gpos, byte[]? gdef)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U).WriteUInt16(4).CopyBytes()));
        if (gsub != null) { tables.Add(CreateTable(FontTags.Gsub, gsub)); }
        if (gpos != null) { tables.Add(CreateTable(FontTags.Gpos, gpos)); }
        if (gdef != null) { tables.Add(CreateTable(FontTags.Gdef, gdef)); }
        if (gdef != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, BuildFvar()));
            tables.Add(CreateTable(FontTags.Name, BuildName()));
        }
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntGsubTable? ParseGsub(byte[]? bytes)
    {
        return SfntGsubTableParser.FindTable(CreateCache(FontTags.Gsub, bytes));
    }

    static SfntGposTable? ParseGpos(byte[]? bytes, byte[]? gdef)
    {
        return SfntGposTableParser.FindTable(
            CreateCache(FontTags.Gpos, bytes, gdef));
    }

    static SfntGdefTable? ParseGdef(byte[]? bytes)
    {
        return SfntGdefTableParser.FindTable(CreateCache(FontTags.Gdef, bytes));
    }

    static SfntTableCache CreateCache(int tag, byte[]? bytes)
    {
        return CreateCache(tag, bytes, tag == FontTags.Gdef ? bytes : null);
    }

    static SfntTableCache CreateCache(int tag, byte[]? bytes, byte[]? gdef)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        tables.Add(CreateTable(tag, bytes));
        if (gdef != null && tag != FontTags.Gdef)
        {
            tables.Add(CreateTable(FontTags.Gdef, gdef));
        }
        if (gdef != null)
        {
            tables.Add(CreateTable(FontTags.Fvar, BuildFvar()));
            tables.Add(CreateTable(FontTags.Name, BuildName()));
        }
        tables.Add(CreateTable(FontTags.Maxp, new TestBinaryBuilder()
            .WriteUInt32(0x00005000U).WriteUInt16(4).CopyBytes()));
        return SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
    }

    static SfntTableData CreateTable(int tag, byte[]? bytes)
    {
        FontData data = FontData.Create(bytes!);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildGsub()
    {
        TestBinaryBuilder subtable = new TestBinaryBuilder()
            .WriteUInt16(2).WriteUInt16(10).WriteUInt16(2)
            .WriteUInt16(3).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(2);
        return BuildLayout(1, subtable.CopyBytes());
    }

    static byte[] BuildPrunableGsub()
    {
        byte[] unused = BuildSingleSubtable(1, 3);
        byte[] context = new TestBinaryBuilder()
            .WriteUInt16(3).WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(12).WriteUInt16(0).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(1)
            .CopyBytes();
        byte[] nested = BuildSingleSubtable(1, 3);
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0).WriteZeros(6);
        int scriptList = builder.GetLength();
        builder.PatchUInt16(4, (ushort)scriptList)
            .WriteUInt16(2)
            .WriteTag("cyrl").WriteUInt16(40)
            .WriteTag("latn").WriteUInt16(14)
            .WriteUInt16(10).WriteUInt16(1)
            .WriteTag("ENG ").WriteUInt16(18)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(4).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(0xFFFF)
            .WriteUInt16(1).WriteUInt16(1);
        int featureList = builder.GetLength();
        builder.PatchUInt16(6, (ushort)featureList)
            .WriteUInt16(2)
            .WriteTag("liga").WriteUInt16(20)
            .WriteTag("salt").WriteUInt16(14)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(1);
        int lookupList = builder.GetLength();
        builder.PatchUInt16(8, (ushort)lookupList)
            .WriteUInt16(3).WriteUInt16(8).WriteUInt16(0).WriteUInt16(0);
        int firstLookup = builder.GetLength();
        WriteLookup(builder, 1, unused);
        int secondLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 4,
            (ushort)(secondLookup - lookupList));
        WriteLookup(builder, 5, context);
        int thirdLookup = builder.GetLength();
        builder.PatchUInt16(lookupList + 6,
            (ushort)(thirdLookup - lookupList));
        WriteLookup(builder, 1, nested);
        return builder.CopyBytes();
    }

    static byte[] BuildSingleSubtable(int input, int output)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(2).WriteUInt16(8).WriteUInt16(1)
            .WriteUInt16((ushort)output)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16((ushort)input)
            .CopyBytes();
    }

    static void WriteLookup(
        TestBinaryBuilder builder,
        int type,
        byte[] subtable)
    {
        builder.WriteUInt16((ushort)type).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(8).WriteBytes(subtable);
    }

    static byte[] BuildGpos()
    {
        TestBinaryBuilder subtable = new TestBinaryBuilder()
            .WriteUInt16(2).WriteUInt16(22).WriteUInt16(0x44)
            .WriteUInt16(2)
            .WriteInt16(20).WriteUInt16(16)
            .WriteInt16(30).WriteUInt16(16)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0x8000)
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(2);
        return BuildLayout(1, subtable.CopyBytes());
    }

    static byte[] BuildLayout(int lookupType, byte[] subtable)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(10).WriteUInt16(12).WriteUInt16(14)
            .WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(4)
            .WriteUInt16((ushort)lookupType).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(8)
            .WriteBytes(subtable);
        return builder.CopyBytes();
    }

    static byte[] BuildGdef()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(3).WriteZeros(14);
        int classes = builder.GetLength();
        builder.PatchUInt16(4, (ushort)classes)
            .WriteUInt16(2).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(2).WriteUInt16(2).WriteUInt16(3);
        int ligatures = builder.GetLength();
        builder.PatchUInt16(8, (ushort)ligatures)
            .WriteUInt16(6).WriteUInt16(1).WriteUInt16(12)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(1).WriteUInt16(4)
            .WriteUInt16(3).WriteInt16(120).WriteUInt16(6)
            .WriteUInt16(0).WriteUInt16(0).WriteUInt16(0x8000);
        int markSets = builder.GetLength();
        builder.PatchUInt16(12, (ushort)markSets)
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt32(12).WriteUInt32(20)
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(2)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(2);
        int variationStore = builder.GetLength();
        builder.PatchUInt32(14, (uint)variationStore);
        WriteVariationStore(builder);
        return builder.CopyBytes();
    }

    static void WriteVariationStore(TestBinaryBuilder builder)
    {
        builder.WriteUInt16(1).WriteUInt32(12)
            .WriteUInt16(1).WriteUInt32(22)
            .WriteUInt16(1).WriteUInt16(1)
            .WriteInt16(0).WriteInt16(0x4000).WriteInt16(0x4000)
            .WriteUInt16(1).WriteUInt16(1).WriteUInt16(1)
            .WriteUInt16(0).WriteInt16(10);
    }

    static byte[] BuildFvar()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(16)
            .WriteUInt16(2).WriteUInt16(1).WriteUInt16(20)
            .WriteUInt16(0).WriteUInt16(8)
            .WriteTag("wght").WriteInt32(0).WriteInt32(0)
            .WriteInt32(0x00010000).WriteUInt16(0).WriteUInt16(256)
            .CopyBytes();
    }

    static byte[] BuildName()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(18)
            .WriteUInt16(3).WriteUInt16(1).WriteUInt16(0x0409)
            .WriteUInt16(256).WriteUInt16(2).WriteUInt16(0)
            .WriteUInt16(0x0057)
            .CopyBytes();
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
