using ttf;

namespace ttf.Tests;

internal class FontSubsetClosureDifferentialTests
{
    static int Latin = Tag("latn");
    static int Ligatures = Tag("liga");

    // fontTools 4.60.2 and HarfBuzz 14.3.0 both retain source GIDs
    // 0/4/5/6 for "fi" with liga, remap them to 0/1/2/3, retain one
    // ligature lookup, and shape the subset to GID 3 with advance 900.
    [Test]
    public void TrueTypeLayoutClosureMatchesPinnedSubsettersAndReopens()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            source.CreateDefaultInstance(),
            options);
        builder.AddText("fi");
        builder.AddFeature(Ligatures);

        FontSubset subset = builder.Build();
        FontSubsetResult result = builder.BuildResult();
        FontFace reopened = FontLoader.Load(
            result.CopyFontBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        SfntGsubTable? gsub = reopened.GetGsubTable();
        SfntGposTable? gpos = reopened.GetGposTable();
        int[] oldToNew = result.GetOldToNewGlyphMap();
        SfntLayoutGlyphBuffer buffer = SfntLayoutGlyphBuffer.Create(
            new int[] { oldToNew[4], oldToNew[5] });

        ApplyLigature(gsub, reopened.GetGdefTable(), buffer);

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphIds(subset),
                Is.EqualTo(new int[] { 0, 4, 5, 6 }));
            Assert.That(result.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 4, 5, 6 }));
            Assert.That(oldToNew[4], Is.EqualTo(1));
            Assert.That(oldToNew[5], Is.EqualTo(2));
            Assert.That(oldToNew[6], Is.EqualTo(3));
            Assert.That(gsub, Is.Not.Null);
            Assert.That(gsub!.GetLookupCount(), Is.EqualTo(1));
            Assert.That(gsub.GetLookup(0).GetLookupType(), Is.EqualTo(4));
            Assert.That(gsub.GetLayoutTable().GetFeatureList()
                .GetFeature(0).GetFeatureTag(), Is.EqualTo(Ligatures));
            Assert.That(gpos, Is.Not.Null);
            Assert.That(gpos!.GetLookupCount(), Is.Zero);
            Assert.That(buffer.GetCount(), Is.EqualTo(1));
            Assert.That(buffer.GetGlyphId(0), Is.EqualTo(3));
            Assert.That(buffer.GetClusterStart(0), Is.Zero);
            Assert.That(buffer.GetClusterLength(0), Is.EqualTo(2));
            Assert.That(reopened.CreateDefaultInstance()
                .GetGlyphMetrics(3).GetAdvanceWidth(), Is.EqualTo(900));
        });
    }

    // fontTools 4.60.2 reports the CID closure .notdef/cid00010, retains
    // both referenced Font DICTs, and emits a loadable two-CharString CFF
    // table for this pinned test-authored face.
    [Test]
    public void CffClosureMatchesPinnedFontToolsAndReopens()
    {
        FontFace outlines = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
        FontFace source = FontSubsetReopenDifferentialTests
            .AddOracleCommonTables(outlines, null);
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            source.CreateDefaultInstance(),
            options);
        builder.AddCodePoint(0x41);

        FontSubset subset = builder.Build();
        FontSubsetResult result = builder.BuildResult();
        FontFace reopened = FontLoader.Load(
            result.CopyFontBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        SfntCffTable? cff = reopened.GetCffTable();

        Assert.Multiple(() =>
        {
            Assert.That(CopyGlyphIds(subset),
                Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(subset.CopyCffFontDictionaryIndices(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(subset.CopyCffGlobalSubroutineIndices(), Is.Empty);
            Assert.That(result.GetNewToOldGlyphMap(),
                Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(reopened.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(cff, Is.Not.Null);
            Assert.That(cff!.GetCharStringsIndex().GetCount(), Is.EqualTo(2));
            Assert.That(cff.GetFontDictionaryCountForSubsetting(),
                Is.EqualTo(2));
            Assert.That(reopened.GetCmapMapping()!.MapUnicode(0x41),
                Is.EqualTo(1));
            Assert.That(reopened.CreateDefaultInstance()
                .GetGlyphOutline(1), Is.Not.Null);
        });
    }

    static void ApplyLigature(
        SfntGsubTable? table,
        SfntGdefTable? gdef,
        SfntLayoutGlyphBuffer buffer)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            new int[0],
            new int[] { Ligatures },
            new int[0],
            new SfntUserFeature[0],
            buffer.GetCount());
        SfntLayoutSelection selection = plan.Select(
            table!.GetLayoutTable(),
            Latin,
            0);
        SfntGsubExecutor.ApplyLookup(
            table,
            selection.GetLookup(0).GetLookupIndex(),
            buffer,
            gdef!,
            SfntLayoutTraversalDirection.Forward,
            1);
    }

    static int[] CopyGlyphIds(FontSubset subset)
    {
        int[] glyphIds = new int[subset.GetGlyphCount()];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphIds[index] = subset.GetGlyph(index);
            index += 1;
        }

        return glyphIds;
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
