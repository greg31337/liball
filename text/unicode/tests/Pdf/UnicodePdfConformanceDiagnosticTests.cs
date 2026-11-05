using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfConformanceDiagnosticTests
{
    [Test]
    public void AccessibilityDiagnostics_ReportPreparationEncodingAndScalarFailures()
    {
        IUnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();

        UnicodePdfDiagnosticResult normalization = diagnostics.CheckAccessibilityText(
            "A\u0300",
            UnicodePdfTextContext.AltText);
        UnicodePdfDiagnosticResult replacement = diagnostics.CheckAccessibilityText(
            "\uFFFD",
            UnicodePdfTextContext.StructureElement);
        UnicodePdfDiagnosticResult prohibited = diagnostics.CheckAccessibilityText(
            "A\u0001",
            UnicodePdfTextContext.Metadata);

        Assert.That(normalization.IsValid, Is.False);
        Assert.That(normalization.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.NeedsAccessibilityTextPreparation));
        Assert.That(HasRule(normalization, UnicodePdfDiagnosticRules.NeedsAccessibilityTextPreparationRule), Is.True);
        Assert.That(HasRule(normalization, UnicodePdfDiagnosticRules.RequiresUtf16BeRule), Is.True);
        Assert.That(replacement.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.ReplacementTextScalar));
        Assert.That(HasRule(replacement, UnicodePdfDiagnosticRules.ReplacementTextScalarRule), Is.True);
        Assert.That(prohibited.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.ProhibitedTextScalar));
        Assert.That(HasRule(prohibited, UnicodePdfDiagnosticRules.ProhibitedTextScalarRule), Is.True);
    }

    [Test]
    public void PdfAConformance_ReportsRequiredMissingToUnicodeData()
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckPdfConformance(
            "A",
            UnicodePdfTextContext.ActualText,
            UnicodePdfConformance.PdfA,
            true,
            null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.Diagnostics[0].Rule, Is.EqualTo(UnicodePdfDiagnosticRules.MissingUnicodeMapRule));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("PDF/A"));
    }

    [Test]
    public void PdfUaConformance_ReportsInvalidAndEmptyToUnicodeMaps()
    {
        UnicodePdfGlyphMap invalidMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0041 }),
                new UnicodePdfGlyphMapping(1, new int[] { 0x0042 }),
            });
        UnicodePdfGlyphMap emptyMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[] { });

        UnicodePdfDiagnosticResult invalid = new UnicodePdfDiagnostics().CheckPdfConformance(
            "A",
            UnicodePdfTextContext.AltText,
            UnicodePdfConformance.PdfUa,
            true,
            invalidMap);
        UnicodePdfDiagnosticResult empty = new UnicodePdfDiagnostics().CheckPdfConformance(
            "A",
            UnicodePdfTextContext.AltText,
            UnicodePdfConformance.PdfUa,
            true,
            emptyMap);

        Assert.That(invalid.IsValid, Is.False);
        Assert.That(invalid.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.InvalidUnicodeMap));
        Assert.That(HasRule(invalid, UnicodePdfDiagnosticRules.InvalidUnicodeMapRule), Is.True);
        Assert.That(empty.IsValid, Is.False);
        Assert.That(empty.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(HasRule(empty, UnicodePdfDiagnosticRules.MissingUnicodeMapRule), Is.True);
    }

    [Test]
    public void PdfConformance_AllowsOptionalAbsentMapsAndValidTextStringEncodings()
    {
        UnicodePdfGlyphMap validMap = new UnicodePdfGlyphMap(
            "F1",
            new UnicodePdfGlyphMapping[]
            {
                new UnicodePdfGlyphMapping(1, new int[] { 0x0100 }),
            });

        UnicodePdfDiagnosticResult optional = new UnicodePdfDiagnostics().CheckPdfConformance(
            "Plain",
            UnicodePdfTextContext.Metadata,
            UnicodePdfConformance.PdfAAndPdfUa,
            false,
            null);
        UnicodePdfDiagnosticResult utf16 = new UnicodePdfDiagnostics().CheckPdfConformance(
            "\u0100",
            UnicodePdfTextContext.Bookmark,
            UnicodePdfConformance.PdfAAndPdfUa,
            true,
            validMap);

        Assert.That(optional.IsValid, Is.True);
        Assert.That(optional.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.Valid));
        Assert.That(optional.Diagnostics, Is.Empty);
        Assert.That(utf16.IsValid, Is.True);
        Assert.That(utf16.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.RequiresUtf16Be));
        Assert.That(utf16.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(utf16.Diagnostics[0].Rule, Is.EqualTo(UnicodePdfDiagnosticRules.RequiresUtf16BeRule));
    }

    [Test]
    public void PdfConformance_StatusUsesTheMostSevereCombinedDiagnostic()
    {
        UnicodePdfDiagnosticResult result = new UnicodePdfDiagnostics().CheckPdfConformance(
            "A\u0100",
            UnicodePdfTextContext.Outline,
            UnicodePdfConformance.PdfUa,
            true,
            null);

        Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
        Assert.That(result.Diagnostics[0].Status, Is.EqualTo(UnicodePdfDiagnosticStatus.RequiresUtf16Be));
        Assert.That(result.Diagnostics[1].Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(result.Status, Is.EqualTo(UnicodePdfDiagnosticStatus.MissingUnicodeMap));
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void PdfConformanceDiagnostics_RejectInvalidInputs()
    {
        IUnicodePdfDiagnostics diagnostics = new UnicodePdfDiagnostics();

        Assert.Throws<ArgumentNullException>(() => diagnostics.CheckAccessibilityText(null!, UnicodePdfTextContext.ActualText));
        Assert.Throws<ArgumentOutOfRangeException>(() => diagnostics.CheckAccessibilityText("A", (UnicodePdfTextContext)99));
        Assert.Throws<ArgumentNullException>(() => diagnostics.CheckPdfConformance(
            null!,
            UnicodePdfTextContext.ActualText,
            UnicodePdfConformance.PdfUa,
            false,
            null));
        Assert.Throws<ArgumentOutOfRangeException>(() => diagnostics.CheckPdfConformance(
            "A",
            UnicodePdfTextContext.ActualText,
            (UnicodePdfConformance)99,
            false,
            null));
    }

    static bool HasRule(UnicodePdfDiagnosticResult result, string rule)
    {
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            if (result.Diagnostics[index].Rule == rule)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
