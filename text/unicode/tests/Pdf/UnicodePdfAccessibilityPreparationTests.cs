using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfAccessibilityPreparationTests
{
    [Test]
    public void AccessibilityPreparation_NormalizesAndRepairsEveryPdfTextContext()
    {
        IUnicodePdfText pdfText = new UnicodePdfText();
        string input = "A\u0300 \uD800";
        string expected = "\u00C0 \uFFFD";

        Assert.That(pdfText.PrepareAccessibilityText(input, UnicodePdfTextContext.ActualText), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareAccessibilityText(input, UnicodePdfTextContext.AltText), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareAccessibilityText(input, UnicodePdfTextContext.Bookmark), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareAccessibilityText(input, UnicodePdfTextContext.Outline), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareAccessibilityText(input, UnicodePdfTextContext.StructureElement), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareAccessibilityText(input, UnicodePdfTextContext.Metadata), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareActualText(input), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareAltText(input), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareBookmarkText(input), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareOutlineText(input), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareStructureElementText(input), Is.EqualTo(expected));
        Assert.That(pdfText.PrepareMetadataText(input), Is.EqualTo(expected));
    }

    [Test]
    public void AccessibilityReadiness_RequiresWellFormedPermittedNfcText()
    {
        IUnicodePdfText pdfText = new UnicodePdfText();

        Assert.That(
            pdfText.IsAccessibilityTextPrepared("\u00C0\tline\n", UnicodePdfTextContext.AltText),
            Is.True);
        Assert.That(
            pdfText.IsAccessibilityTextPrepared("A\u0300", UnicodePdfTextContext.Bookmark),
            Is.False);
        Assert.That(
            pdfText.ContainsOnlyValidAccessibilityScalars("\uD800", UnicodePdfTextContext.Outline),
            Is.False);
        Assert.That(
            pdfText.ContainsOnlyValidAccessibilityScalars("\uFFFD", UnicodePdfTextContext.StructureElement),
            Is.False);
        Assert.That(
            pdfText.ContainsOnlyValidAccessibilityScalars("A\u0001", UnicodePdfTextContext.Metadata),
            Is.False);
        Assert.That(
            pdfText.ContainsOnlyValidAccessibilityScalars("A\tB\nC\rD", UnicodePdfTextContext.ActualText),
            Is.True);
    }

    [Test]
    public void AccessibilityPreparation_RejectsNullTextAndUnknownContexts()
    {
        IUnicodePdfText pdfText = new UnicodePdfText();

        Assert.Throws<ArgumentNullException>(() => pdfText.PrepareAltText(null!));
        Assert.Throws<ArgumentNullException>(() => pdfText.PrepareBookmarkText(null!));
        Assert.Throws<ArgumentNullException>(() => pdfText.PrepareOutlineText(null!));
        Assert.Throws<ArgumentNullException>(() => pdfText.PrepareStructureElementText(null!));
        Assert.Throws<ArgumentNullException>(() => pdfText.PrepareMetadataText(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => pdfText.PrepareAccessibilityText("A", (UnicodePdfTextContext)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => pdfText.IsAccessibilityTextPrepared("A", (UnicodePdfTextContext)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => pdfText.ContainsOnlyValidAccessibilityScalars("A", (UnicodePdfTextContext)99));
    }
}
