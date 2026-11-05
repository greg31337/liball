using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAccessibilityPreparationTests
{
    [Test]
    public void CheckAccessibilityPreparation_ReturnsReadyWhenRequiredPreparationIsPresent()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        PdfPage page = document.AddPage(200, 100);
        PdfImage informative = document.AddImage(1, 1, new byte[] { 255, 0, 0 });
        PdfImage decorative = document.AddImage(1, 1, new byte[] { 0, 0, 255 });

        metadata.Title = "Accessible document";
        document.SetMetadata(metadata);
        document.SetLanguage("en-US");
        informative.SetAlternateText("Red status swatch");
        decorative.MarkDecorative();
        document.AddAccessibilityNodeWithDescription(page, "H1", "Accessible document");
        document.AddAccessibilityNodeWithDescription(page, "P", "Opening paragraph");

        PdfAccessibilityPreparationResult result = document.CheckAccessibilityPreparation();

        Assert.That(result.IsReady(), Is.True);
        Assert.That(result.CountErrors(), Is.EqualTo(0));
        Assert.That(result.CountWarnings(), Is.EqualTo(0));
    }

    [Test]
    public void CheckAccessibilityPreparation_ReportsMissingRequiredPreparation()
    {
        PdfDocument document = new PdfDocument();

        document.AddPage(200, 100);
        document.AddImage(1, 1, new byte[] { 255, 0, 0 });

        PdfAccessibilityPreparationResult result = document.CheckAccessibilityPreparation();

        Assert.That(result.IsReady(), Is.False);
        Assert.That(result.HasIssue("ACCESSIBILITY_LANGUAGE_MISSING"), Is.True);
        Assert.That(result.HasIssue("ACCESSIBILITY_TITLE_MISSING"), Is.True);
        Assert.That(result.HasIssue("ACCESSIBILITY_READING_ORDER_MISSING"), Is.True);
        Assert.That(result.HasIssue("ACCESSIBILITY_IMAGE_ALT_TEXT_MISSING"), Is.True);
        Assert.That(result.CountErrors(), Is.EqualTo(4));
    }

    [Test]
    public void CheckAccessibilityPreparation_ReportsFormsLinksAnnotationsAndNoPages()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        PdfPage page = document.AddPage(200, 100);

        metadata.Title = "Interactive document";
        document.SetMetadata(metadata);
        document.SetLanguage("en");
        document.AddAccessibilityNode(page, "P");
        document.AddTextField("name", page, 10, 10, 50, 12);
        page.AddExternalLink("https://example.com", 20, 20, 40, 10);
        page.AddTextAnnotation("Review", 30, 30, 16, 16);

        PdfAccessibilityPreparationResult result = document.CheckAccessibilityPreparation();

        Assert.That(result.IsReady(), Is.False);
        Assert.That(result.HasIssue("ACCESSIBILITY_FORM_FIELD_DESCRIPTIONS_REQUIRED"), Is.True);
        Assert.That(result.HasIssue("ACCESSIBILITY_LINK_REVIEW"), Is.True);
        Assert.That(result.HasIssue("ACCESSIBILITY_ANNOTATION_REVIEW"), Is.True);
        Assert.That(result.CountErrors(), Is.EqualTo(1));
        Assert.That(result.CountWarnings(), Is.EqualTo(2));

        PdfDocument empty = new PdfDocument();
        PdfAccessibilityPreparationResult emptyResult = empty.CheckAccessibilityPreparation();

        Assert.That(emptyResult.HasIssue("ACCESSIBILITY_NO_PAGES"), Is.True);
    }

    [Test]
    public void AccessibilityPreparationRejectsInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);
        PdfImage image = document.AddImage(1, 1, new byte[] { 1, 2, 3 });

        Assert.Throws<ArgumentNullException>(() => document.SetLanguage(null!));
        Assert.Throws<ArgumentException>(() => document.SetLanguage(""));
        Assert.Throws<ArgumentException>(() => document.SetLanguage("en US"));
        Assert.Throws<ArgumentNullException>(() => document.AddAccessibilityNode(null!, "P"));
        Assert.Throws<ArgumentException>(() => document.AddAccessibilityNode(otherPage, "P"));
        Assert.Throws<ArgumentNullException>(() => document.AddAccessibilityNode(page, null!));
        Assert.Throws<ArgumentException>(() => document.AddAccessibilityNode(page, ""));
        Assert.Throws<ArgumentNullException>(() => document.AddAccessibilityNodeWithDescription(page, "P", null!));
        Assert.Throws<ArgumentNullException>(() => image.SetAlternateText(null!));
        Assert.Throws<ArgumentException>(() => image.SetAlternateText(""));
        Assert.Throws<ArgumentNullException>(() => new PdfAccessibilityPreparationChecker().Check(null!));
    }

    [Test]
    public void Save_WritesDocumentLanguageInCatalog()
    {
        PdfDocument document = new PdfDocument();

        document.AddPage(200, 100);
        document.SetLanguage("en-US");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/Lang (en-US)"));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
