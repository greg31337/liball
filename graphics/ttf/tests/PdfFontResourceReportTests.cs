using ttf;

namespace ttf.Tests;

internal class PdfFontResourceReportTests
{
    [Test]
    public void ReportsMissingUnicodeAndRequestedPdfUaConcernWithoutConformanceClaim()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetArchivalProfile(PdfArchivalProfile.PdfA2);
        options.SetAccessibilityProfile(PdfAccessibilityProfile.PdfUa1);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(1, new int[] { 0x41 });
        builder.AddGlyph(2, Array.Empty<int>());

        PdfFontResource resource = builder.FinalizeFont();
        PdfFontResourceReport report = resource.GetCompatibilityReport();
        int[] missing = report.GetMissingUnicodeMappingCodes();
        PdfFontResourceConcern concern = report.GetConcern(0);
        PdfFontResourceConcern?[] copiedConcerns = report.GetConcerns();
        copiedConcerns[0] = null;
        missing[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(report.GetMinimumPdfVersion(),
                Is.EqualTo(PdfVersion.Pdf1_2));
            Assert.That(report.GetTargetPdfVersion(),
                Is.EqualTo(PdfVersion.Pdf1_7));
            Assert.That(report.GetEmbeddingRightsOutcome(),
                Is.EqualTo(PdfFontEmbeddingRightsOutcome.Unavailable));
            Assert.That(report.GetEmbeddingDecision(),
                Is.SameAs(resource.GetEmbeddingDecision()));
            Assert.That(report.GetMissingUnicodeMappingCount(),
                Is.EqualTo(1));
            Assert.That(report.GetMissingUnicodeMappingCode(0),
                Is.EqualTo(2));
            Assert.That(report.GetActualTextRecommendationCount(), Is.Zero);
            Assert.That(report.RequiresActualText(), Is.False);
            Assert.That(report.GetArchivalProfile(),
                Is.EqualTo(PdfArchivalProfile.PdfA2));
            Assert.That(report.GetAccessibilityProfile(),
                Is.EqualTo(PdfAccessibilityProfile.PdfUa1));
            Assert.That(report.HasPdfAEmbeddingConcerns(), Is.False);
            Assert.That(report.HasPdfUaUnicodeConcerns(), Is.True);
            Assert.That(report.GetConcernCount(), Is.EqualTo(1));
            Assert.That(concern.GetKind(), Is.EqualTo(
                PdfFontResourceConcernKind.PdfUaMissingUnicodeMappings));
            Assert.That(concern.GetAffectedCount(), Is.EqualTo(1));
            Assert.That(concern.GetMessage(), Is.Not.Empty);
            Assert.That(concern.IsPdfAConcern(), Is.False);
            Assert.That(concern.IsPdfUaConcern(), Is.True);
            Assert.That(report.GetConcern(0), Is.SameAs(concern));
            Assert.That(report.ClaimsDocumentConformance(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                report.GetMissingUnicodeMappingCode(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                report.GetConcern(1));
        });
    }

    [Test]
    public void ReportsActualTextConcernForAmbiguousOriginalText()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetAccessibilityProfile(PdfAccessibilityProfile.PdfUa2);
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(1, new int[] { 0x41 });
        builder.AddGlyph(1, new int[] { 0x42 });

        PdfFontResourceReport report = builder.FinalizeFont()
            .GetCompatibilityReport();
        PdfFontResourceConcern concern = report.GetConcern(0);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetMissingUnicodeMappingCount(), Is.Zero);
            Assert.That(report.GetActualTextRecommendationCount(),
                Is.EqualTo(1));
            Assert.That(report.RequiresActualText(), Is.True);
            Assert.That(report.HasPdfUaUnicodeConcerns(), Is.True);
            Assert.That(report.GetConcernCount(), Is.EqualTo(1));
            Assert.That(concern.GetKind(), Is.EqualTo(
                PdfFontResourceConcernKind.PdfUaActualTextRequired));
            Assert.That(concern.GetAffectedCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReportsStrategyMinimumsAndPdfA1OpenTypeConcern()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        openTypeOptions.SetVersion(PdfVersion.Pdf1_6);
        openTypeOptions.SetArchivalProfile(PdfArchivalProfile.PdfA1);
        PdfFontResourceReport openType = Build(
            instance,
            openTypeOptions).GetCompatibilityReport();

        PdfFontOptions simpleOptions = PdfFontOptions.Create();
        simpleOptions.SetFontKind(PdfFontKind.SimpleTrueType);
        simpleOptions.SetVersion(PdfVersion.Pdf1_1);
        PdfFontResourceReport simple = Build(
            instance,
            simpleOptions).GetCompatibilityReport();

        Assert.Multiple(() =>
        {
            Assert.That(openType.GetMinimumPdfVersion(),
                Is.EqualTo(PdfVersion.Pdf1_6));
            Assert.That(openType.GetTargetPdfVersion(),
                Is.EqualTo(PdfVersion.Pdf1_6));
            Assert.That(openType.HasPdfAEmbeddingConcerns(), Is.True);
            Assert.That(openType.GetConcernCount(), Is.EqualTo(1));
            Assert.That(openType.GetConcern(0).GetKind(), Is.EqualTo(
                PdfFontResourceConcernKind.PdfA1OpenTypeFontProgram));
            Assert.That(openType.GetConcern(0).IsPdfAConcern(), Is.True);
            Assert.That(simple.GetMinimumPdfVersion(),
                Is.EqualTo(PdfVersion.Pdf1_1));
            Assert.That(simple.HasPdfAEmbeddingConcerns(), Is.False);
            Assert.That(simple.HasPdfUaUnicodeConcerns(), Is.False);
        });
    }

    [Test]
    public void NormalizesPermittedReportedAndForcedRightsOutcomes()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PdfFontResource baseline = Build(
            instance,
            PdfFontOptions.Create());
        FontEmbeddingRights rights = new FontEmbeddingRights(
            SfntEmbeddingRightsInterpreter.Interpret(5, 0x0100));
        FontSubsetEmbeddingDecision permitted = CreateDecision(
            rights,
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetRightsPolicy.Reject,
            false,
            false,
            false);
        FontSubsetEmbeddingDecision reported = CreateDecision(
            rights,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetRightsPolicy.Report,
            true,
            true,
            false);
        FontSubsetEmbeddingDecision forced = CreateDecision(
            rights,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetRightsPolicy.EmbedFullFont,
            true,
            false,
            true);
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetArchivalProfile(PdfArchivalProfile.PdfA2);

        PdfFontResourceReport permittedReport = CreateReport(
            options,
            baseline,
            permitted);
        PdfFontResourceReport reportedReport = CreateReport(
            options,
            baseline,
            reported);
        PdfFontResourceReport forcedReport = CreateReport(
            options,
            baseline,
            forced);

        Assert.Multiple(() =>
        {
            Assert.That(permittedReport.GetEmbeddingRightsOutcome(),
                Is.EqualTo(PdfFontEmbeddingRightsOutcome.Permitted));
            Assert.That(reportedReport.GetEmbeddingRightsOutcome(),
                Is.EqualTo(
                    PdfFontEmbeddingRightsOutcome.ConflictReported));
            Assert.That(reportedReport.HasPdfAEmbeddingConcerns(), Is.True);
            Assert.That(reportedReport.GetConcern(0).GetKind(), Is.EqualTo(
                PdfFontResourceConcernKind
                    .PdfAEmbeddingRightsConflict));
            Assert.That(forcedReport.GetEmbeddingRightsOutcome(),
                Is.EqualTo(PdfFontEmbeddingRightsOutcome.FullFontForced));
            Assert.That(forcedReport.HasPdfAEmbeddingConcerns(), Is.False);
        });
    }

    static PdfFontResourceReport CreateReport(
        PdfFontOptions options,
        PdfFontResource baseline,
        FontSubsetEmbeddingDecision decision)
    {
        return PdfFontResourceReportBuilder.Build(
            options,
            baseline.GetFontKind(),
            CreateDenseCodes(baseline.GetCharacterCodeCount()),
            baseline.GetToUnicodeMappings(),
            decision);
    }

    static int[] CreateDenseCodes(int count)
    {
        int[] codes = new int[count];
        int index = 0;

        while (index < codes.Length)
        {
            codes[index] = index;
            index += 1;
        }

        return codes;
    }

    static FontSubsetEmbeddingDecision CreateDecision(
        FontEmbeddingRights rights,
        FontSubsetEmbeddingMode requestedMode,
        FontSubsetEmbeddingMode effectiveMode,
        FontSubsetRightsPolicy policy,
        bool conflict,
        bool reported,
        bool forced)
    {
        return new FontSubsetEmbeddingDecision(
            rights,
            requestedMode,
            effectiveMode,
            policy,
            conflict,
            reported,
            forced,
            Array.Empty<FontDiagnostic>());
    }

    static PdfFontResource Build(
        FontInstance instance,
        PdfFontOptions options)
    {
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);
        builder.AddGlyph(1, new int[] { 0x41 });
        return builder.FinalizeFont();
    }
}
