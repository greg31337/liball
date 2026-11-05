using ttf;

namespace ttf.Tests;

internal class FontSubsetReopenDifferentialTests
{
    // fontTools 4.60.2 independently reopened the deterministic TrueType
    // output and reported glyph order .notdef/A/Euro, cmap values 1/2,
    // advances 500/600/700, and PostScript name FURDET+OracleSans-Regular.
    [Test]
    public void TrueTypeSubsetReopensWithMatchingOutlinesMetricsMapsAndNames()
    {
        FontFace source = FontLoader.Load(
            FontToolsFreeTypeDifferentialTests.GetOracleFont(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        FontInstance sourceInstance = source.CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            sourceInstance,
            options);
        builder.AddCodePoint(0x41);
        builder.AddCodePoint(0x20AC);

        FontSubsetResult result = builder.BuildResult();
        FontFace reopened = FontLoader.Load(
            result.CopyFontBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        FontInstance reopenedInstance = reopened.CreateDefaultInstance();
        int[] oldToNew = result.GetOldToNewGlyphMap();

        Assert.Multiple(() =>
        {
            Assert.That(reopenedInstance.GetGlyphCount(), Is.EqualTo(3));
            Assert.That(reopened.GetCmapMapping()!.MapUnicode(0x41),
                Is.EqualTo(oldToNew[2]));
            Assert.That(reopened.GetCmapMapping()!.MapUnicode(0x20AC),
                Is.EqualTo(oldToNew[3]));
            Assert.That(reopened.GetNames().GetPdfPostScriptName(),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(result.GetSubsetName(),
                Is.EqualTo("FURDET+OracleSans-Regular"));
            Assert.That(reopened.GetNames().GetFamilyName()!.GetValue(),
                Does.StartWith(result.GetSubsetName().Substring(0, 7)));
        });

        AssertGlyphEqual(
            sourceInstance,
            2,
            reopenedInstance,
            oldToNew[2]);
        AssertGlyphEqual(
            sourceInstance,
            3,
            reopenedInstance,
            oldToNew[3]);
    }

    // fontTools 4.60.2 reports glyph order .notdef/cid00010, cmap A,
    // advance 800, and the same EDGFQM name in CFF and name ID 6.
    [Test]
    public void CffSubsetReopensWithMatchingOutlinesMetricsMapsAndNames()
    {
        FontFace outlineFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);
        FontFace source = AddOracleCommonTables(outlineFace, null);
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        FontInstance sourceInstance = source.CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            sourceInstance,
            options);
        builder.AddCodePoint(0x41);

        FontSubsetResult result = builder.BuildResult();
        FontFace reopened = FontLoader.Load(
            result.CopyFontBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        int subsetGlyphId = result.GetOldToNewGlyphMap()[2];

        Assert.Multiple(() =>
        {
            Assert.That(reopened.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(reopened.GetCmapMapping()!.MapUnicode(0x41),
                Is.EqualTo(subsetGlyphId));
            Assert.That(reopened.GetNames().GetPdfPostScriptName(),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(GetCffProgramName(reopened),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(result.GetSubsetName(),
                Is.EqualTo("EDGFQM+OracleSans-Regular"));
            Assert.That(result.GetTableReport().GetRetainedTableTags(),
                Does.Contain(FontTags.Cff));
        });

        AssertGlyphEqual(
            source.CreateDefaultInstance(),
            2,
            reopened.CreateDefaultInstance(),
            subsetGlyphId);
    }

    // fontTools 4.60.2 reports a static CFF table, glyph order
    // .notdef/gid0001, cmap A, advance 700, and matching BLCDYN names.
    [Test]
    public void InstancedCff2SubsetReopensAsMatchingStaticCff()
    {
        FontFace outlineFace =
            FontInstanceCffTests.CreateReopenableVariableOutlineFace();
        FontFace source = AddOracleCommonTables(
            outlineFace,
            BuildCmap(0x41, 1));
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        FontInstance sourceInstance = source.CreateDefaultInstance();
        FontSubsetBuilder builder = new FontSubsetBuilder(
            sourceInstance,
            options);
        builder.AddCodePoint(0x41);

        FontSubsetResult result = builder.BuildResult();
        FontSubsetOptions fullOptions = FontSubsetOptions.CreateStrict();
        fullOptions.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);
        fullOptions.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        FontSubsetBuilder fullBuilder = new FontSubsetBuilder(
            sourceInstance,
            fullOptions);
        fullBuilder.AddCodePoint(0x41);
        FontSubsetResult fullResult = fullBuilder.BuildResult();
        FontFace reopened = FontLoader.Load(
            result.CopyFontBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        int subsetGlyphId = result.GetOldToNewGlyphMap()[1];

        Assert.Multiple(() =>
        {
            Assert.That(reopened.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(reopened.HasTable(FontTags.Cff), Is.True);
            Assert.That(reopened.HasTable(FontTags.Cff2), Is.False);
            Assert.That(reopened.GetCmapMapping()!.MapUnicode(0x41),
                Is.EqualTo(subsetGlyphId));
            Assert.That(reopened.GetNames().GetPdfPostScriptName(),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(GetCffProgramName(reopened),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(result.GetSubsetName(),
                Is.EqualTo("BLCDYN+Demo-Regular"));
            Assert.That(result.GetSourceInstance(), Is.SameAs(sourceInstance));
            Assert.That(result.GetOutputOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(result.HadVariationData(), Is.True);
            Assert.That(result.WasVariationFrozen(), Is.True);
            Assert.That(fullResult.GetOutputOutlineKind(),
                Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(fullResult.HadVariationData(), Is.True);
            Assert.That(fullResult.WasVariationFrozen(), Is.True);
        });

        AssertGlyphEqual(
            source.CreateDefaultInstance(),
            1,
            reopened.CreateDefaultInstance(),
            subsetGlyphId);
    }

    // fontTools 4.60.2 reports .notdef/A, cmap A, advance 672,
    // no variation tables, and name ID 6 DKYSFW+T094Oracle-Regular.
    [Test]
    public void InstancedVariableTrueTypeSubsetReopensAsMatchingStaticFont()
    {
        FontFace source =
            VariableStaticInstanceDifferentialTests.GetVariableFontFace();
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(0x77676874, 700);
        FontInstance sourceInstance = source.CreateInstance(coordinates);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            sourceInstance,
            FontSubsetOptions.CreateStrict());
        builder.AddCodePoint(0x41);

        FontSubsetResult result = builder.BuildResult();
        FontFace reopened = FontLoader.Load(
            result.CopyFontBytes(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        int subsetGlyphId = result.GetOldToNewGlyphMap()[1];

        Assert.Multiple(() =>
        {
            Assert.That(reopened.GetOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(reopened.HasTable(FontTags.Fvar), Is.False);
            Assert.That(reopened.HasTable(FontTags.Gvar), Is.False);
            Assert.That(reopened.GetCmapMapping()!.MapUnicode(0x41),
                Is.EqualTo(subsetGlyphId));
            Assert.That(reopened.GetNames().GetPdfPostScriptName(),
                Is.EqualTo(result.GetSubsetName()));
            Assert.That(result.GetSubsetName(),
                Is.EqualTo("DKYSFW+T094Oracle-Regular"));
            Assert.That(result.GetSourceInstance(), Is.SameAs(sourceInstance));
            Assert.That(result.GetOutputOutlineKind(),
                Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(result.HadVariationData(), Is.True);
            Assert.That(result.WasVariationFrozen(), Is.True);
            Assert.That(result.GetPrefixReport().GetCoordinateKey(),
                Is.EqualTo(sourceInstance.GetCoordinates().GetKey()));
        });

        AssertGlyphEqual(
            VariableStaticInstanceDifferentialTests
                .GetFontToolsInstanceFace()
                .CreateDefaultInstance(),
            1,
            reopened.CreateDefaultInstance(),
            subsetGlyphId);
    }

    static string GetCffProgramName(FontFace face)
    {
        CffIndex names = face.GetCffTable()!.GetNameIndex();
        return System.Text.Encoding.ASCII.GetString(names.CopyObject(0));
    }

    internal static FontFace AddOracleCommonTables(
        FontFace outlineFace,
        byte[]? cmapOverride)
    {
        FontFace oracle = FontLoader.Load(
            FontToolsFreeTypeDifferentialTests.GetOracleFont(),
            FontLoadOptions.CreateStrict()).GetFace(0);
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        SfntTableStore source = outlineFace.GetTableStore();
        int index = 0;

        while (index < source.GetTableCount())
        {
            SfntTableData table = source.GetTable(index);

            if (table.GetTag() != FontTags.Cmap &&
                table.GetTag() != FontTags.Os2)
            {
                tables.Add(table);
            }

            index++;
        }

        AddTable(tables, FontTags.Cmap, cmapOverride == null ?
            oracle.CopyTable(FontTags.Cmap) : cmapOverride);
        if (!outlineFace.HasTable(FontTags.Name))
        {
            AddTable(tables, FontTags.Name, oracle.CopyTable(FontTags.Name));
        }
        AddTable(tables, FontTags.Os2, oracle.CopyTable(FontTags.Os2));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict,
            FontLimits.CreateStrict());
        return new FontFace(
            0,
            0x4F54544FU,
            new SfntValidationResult(
                outlineFace.GetOutlineKind(),
                System.Array.Empty<FontDiagnostic>()),
            cache);
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static byte[] BuildCmap(int codePoint, int glyphId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(3)
            .WriteUInt16(10)
            .WriteUInt32(12)
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(28)
            .WriteUInt32(0)
            .WriteUInt32(1)
            .WriteUInt32((uint)codePoint)
            .WriteUInt32((uint)codePoint)
            .WriteUInt32((uint)glyphId)
            .CopyBytes();
    }

    static void AssertGlyphEqual(
        FontInstance expectedInstance,
        int expectedGlyphId,
        FontInstance actualInstance,
        int actualGlyphId)
    {
        GlyphOutline expected = expectedInstance.GetGlyphOutline(
            expectedGlyphId);
        GlyphOutline actual = actualInstance.GetGlyphOutline(actualGlyphId);
        GlyphMetrics expectedMetrics = expectedInstance.GetGlyphMetrics(
            expectedGlyphId);
        GlyphMetrics actualMetrics = actualInstance.GetGlyphMetrics(
            actualGlyphId);

        Assert.Multiple(() =>
        {
            Assert.That(actualMetrics.GetAdvanceWidth(),
                Is.EqualTo(expectedMetrics.GetAdvanceWidth()));
            Assert.That(actualMetrics.GetLeftSideBearing(),
                Is.EqualTo(expectedMetrics.GetLeftSideBearing()));
            Assert.That(actualMetrics.GetAdvanceHeight(),
                Is.EqualTo(expectedMetrics.GetAdvanceHeight()));
            Assert.That(actual.GetCommandCount(),
                Is.EqualTo(expected.GetCommandCount()));
        });

        int index = 0;

        while (index < expected.GetCommandCount())
        {
            GlyphPathCommand expectedCommand = expected.GetCommand(index);
            GlyphPathCommand actualCommand = actual.GetCommand(index);
            Assert.Multiple(() =>
            {
                Assert.That(actualCommand.GetKind(),
                    Is.EqualTo(expectedCommand.GetKind()));

                if (expectedCommand.GetKind() !=
                    GlyphPathCommandKind.Close)
                {
                    Assert.That(actualCommand.GetEndX(),
                        Is.EqualTo(expectedCommand.GetEndX()));
                    Assert.That(actualCommand.GetEndY(),
                        Is.EqualTo(expectedCommand.GetEndY()));

                    if (expectedCommand.GetKind() ==
                            GlyphPathCommandKind.Quadratic ||
                        expectedCommand.GetKind() ==
                            GlyphPathCommandKind.Cubic)
                    {
                        Assert.That(actualCommand.GetControlX(),
                            Is.EqualTo(expectedCommand.GetControlX()));
                        Assert.That(actualCommand.GetControlY(),
                            Is.EqualTo(expectedCommand.GetControlY()));
                    }

                    if (expectedCommand.GetKind() ==
                        GlyphPathCommandKind.Cubic)
                    {
                        Assert.That(actualCommand.GetSecondControlX(),
                            Is.EqualTo(expectedCommand.GetSecondControlX()));
                        Assert.That(actualCommand.GetSecondControlY(),
                            Is.EqualTo(expectedCommand.GetSecondControlY()));
                    }
                }
            });
            index++;
        }
    }
}
