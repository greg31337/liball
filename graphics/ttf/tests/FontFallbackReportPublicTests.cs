using ttf;

namespace ttf.Tests;

internal class FontFallbackReportPublicTests
{
    [Test]
    public void ReportsGenericScriptAndFamilySubstitutionsInSourceOrder()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041 });
        FontFace script = FontClusterFallbackTests.Clone(
            source,
            "Script",
            new int[] { 0x0042 });
        FontFace fallback = FontClusterFallbackTests.Clone(
            source,
            "Fallback",
            new int[] { 0x0043 });
        FontSet set = new FontSet();
        set.Add(primary);
        set.Add(script);
        set.Add(fallback);
        set.SetGenericFamily(FontGenericFamily.SansSerif, "Primary");
        set.AddScriptFallbackFamily("Latn", "Script");
        set.AddFallbackFamily("Fallback");
        FontRequest request = new FontApiFactory().CreateGenericRequest(
            FontGenericFamily.SansSerif);

        FontFallbackReport report = set.ShapeText("ABC", request)
            .GetFallbackReport();

        Assert.Multiple(() =>
        {
            Assert.That(report.GetDecisionCount(), Is.EqualTo(3));
            AssertDecision(
                report.GetDecision(0),
                FontFallbackDecisionKind.GenericFamilySubstitution,
                0,
                "sans-serif",
                "Primary",
                primary);
            AssertDecision(
                report.GetDecision(1),
                FontFallbackDecisionKind.ScriptFallback,
                1,
                "sans-serif",
                "Script",
                script);
            AssertDecision(
                report.GetDecision(2),
                FontFallbackDecisionKind.FamilySubstitution,
                2,
                "sans-serif",
                "Fallback",
                fallback);
            Assert.That(report.HasFinalMissingGlyphFace(), Is.False);
            Assert.That(report.GetFinalMissingGlyphFace(), Is.Null);
            Assert.That(report.GetFinalMissingGlyphInstance(), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                report.GetDecision(3));
        });
    }

    [Test]
    public void ReportsMissingGlyphAndItsFinalPartialCoverageFace()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041 });
        FontSet set = new FontSet();
        set.Add(primary);

        TextShapingResult result = set.ShapeText(
            "AZ",
            new FontRequest("Primary"));
        FontFallbackReport report = result.GetFallbackReport();
        FontFallbackDecision missing = report.GetDecision(0);
        FontFallbackDecision final = report.GetDecision(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.MissingGlyph));
            Assert.That(report.GetDecisionCount(), Is.EqualTo(2));
            Assert.That(missing.GetKind(),
                Is.EqualTo(FontFallbackDecisionKind.MissingGlyph));
            Assert.That(missing.GetUtf16Start(), Is.EqualTo(1));
            Assert.That(missing.GetUtf16Length(), Is.EqualTo(1));
            Assert.That(missing.GetScript(), Is.EqualTo("Latn"));
            Assert.That(missing.GetRequestedFamilyName(),
                Is.EqualTo("Primary"));
            Assert.That(missing.GetResolvedFamilyName(),
                Is.EqualTo("Primary"));
            Assert.That(missing.GetCodePoint(), Is.EqualTo(0x005A));
            Assert.That(final.GetKind(),
                Is.EqualTo(FontFallbackDecisionKind.FinalMissingGlyphFace));
            Assert.That(final.GetFace(), Is.SameAs(primary));
            Assert.That(final.GetInstance()!.GetFace(), Is.SameAs(primary));
            Assert.That(report.HasFinalMissingGlyphFace(), Is.True);
            Assert.That(report.GetFinalMissingGlyphFace(), Is.SameAs(primary));
            Assert.That(report.GetFinalMissingGlyphInstance()!.GetFace(),
                Is.SameAs(primary));
        });
    }

    [Test]
    public void ReportsUnavailableGlyphWithoutInventingAFinalFace()
    {
        TextShapingResult result = new FontSet().ShapeText(
            "A",
            new FontRequest("Unavailable"));
        FontFallbackReport report = result.GetFallbackReport();
        FontFallbackDecision decision = report.GetDecision(0);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetDecisionCount(), Is.EqualTo(1));
            Assert.That(decision.GetKind(),
                Is.EqualTo(FontFallbackDecisionKind.MissingGlyph));
            Assert.That(decision.GetCodePoint(), Is.EqualTo(0x0041));
            Assert.That(decision.GetFace(), Is.Null);
            Assert.That(decision.GetInstance(), Is.Null);
            Assert.That(report.HasFinalMissingGlyphFace(), Is.False);
        });
    }

    [TestCase("ankr")]
    [TestCase("bsln")]
    [TestCase("feat")]
    [TestCase("just")]
    [TestCase("kerx")]
    [TestCase("lcar")]
    [TestCase("mort")]
    [TestCase("morx")]
    [TestCase("opbd")]
    [TestCase("prop")]
    [TestCase("trak")]
    [TestCase("Feat")]
    [TestCase("Glat")]
    [TestCase("Gloc")]
    [TestCase("Silf")]
    [TestCase("Sill")]
    public void ReportsUnsupportedTechnologyWithTheResolvedFaceAndTag(
        string unsupportedTag)
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = FontClusterFallbackTests.Clone(
            source,
            "Primary",
            new int[] { 0x0041 });
        FontFace unsupported = AddUnsupportedTable(primary, unsupportedTag);
        FontSet set = new FontSet();
        set.Add(unsupported);

        TextShapingResult result = set.ShapeText(
            "A",
            new FontRequest("Primary"));
        FontFallbackDecision decision = result.GetFallbackReport()
            .GetDecision(0);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDiagnostic(0).GetCode(),
                Is.EqualTo(FontDiagnosticCode.UnsupportedShapingTechnology));
            Assert.That(decision.GetKind(), Is.EqualTo(
                FontFallbackDecisionKind.UnsupportedFontTechnology));
            Assert.That(decision.GetFace(), Is.SameAs(unsupported));
            Assert.That(decision.GetInstance()!.GetFace(),
                Is.SameAs(unsupported));
            Assert.That(decision.GetTableTag(),
                Is.EqualTo((int)TestSfntEncoding.EncodeTag(unsupportedTag)));
            Assert.That(decision.GetCodePoint(), Is.EqualTo(-1));
        });
    }

    static void AssertDecision(
        FontFallbackDecision decision,
        FontFallbackDecisionKind kind,
        int utf16Start,
        string requestedFamily,
        string resolvedFamily,
        FontFace face)
    {
        Assert.That(decision.GetKind(), Is.EqualTo(kind));
        Assert.That(decision.GetUtf16Start(), Is.EqualTo(utf16Start));
        Assert.That(decision.GetUtf16Length(), Is.EqualTo(1));
        Assert.That(decision.GetScript(), Is.EqualTo("Latn"));
        Assert.That(decision.GetRequestedFamilyName(),
            Is.EqualTo(requestedFamily));
        Assert.That(decision.GetResolvedFamilyName(),
            Is.EqualTo(resolvedFamily));
        Assert.That(decision.GetFace(), Is.SameAs(face));
        Assert.That(decision.GetInstance()!.GetFace(), Is.SameAs(face));
        Assert.That(decision.GetCodePoint(), Is.EqualTo(-1));
        Assert.That(decision.GetTableTag(), Is.Zero);
    }

    static FontFace AddUnsupportedTable(FontFace source, string tag)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        int index = 0;
        while (index < source.GetTableCount())
        {
            FontTableInfo table = source.GetTableInfo(index);
            tables.Add(CreateTable(table.GetTag(), table.CopyData()));
            index += 1;
        }
        tables.Add(CreateTable(
            (int)TestSfntEncoding.EncodeTag(tag),
            new byte[] { 0, 0, 0, 2 }));
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict,
            source.GetTableCache().GetLimits());
        return new FontFace(
            source.GetFaceIndex(),
            source.GetScalerType(),
            new SfntValidationResult(
                source.GetOutlineKind(),
                Array.Empty<FontDiagnostic>()),
            cache);
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
}
