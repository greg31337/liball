using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAPreparationTests
{
    [Test]
    public void CheckPdfAPreparation_ReturnsReadyWhenNoBlockingIssuesAreDetected()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        PdfPage page = document.AddPage(200, 100);

        metadata.Title = "Shape only";
        metadata.Producer = "pdf library";
        document.SetMetadata(metadata);
        page.Canvas.FillRectangle(10, 10, 50, 30);

        PdfAPreparationResult result = document.CheckPdfAPreparation();

        Assert.That(result.IsReady(), Is.True);
        Assert.That(result.CountErrors(), Is.EqualTo(0));
        Assert.That(result.CountWarnings(), Is.EqualTo(0));
    }

    [Test]
    public void CheckPdfAPreparation_ReportsBlockingIssues()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.Canvas.DrawText("Text requires embedded fonts", 10, 50, 12);
        document.AddAttachment("source.txt", new byte[] { 1, 2, 3 });
        document.AddTextField("name", page, 10, 10, 50, 12);
        document.UseStandardEncryption("user", "owner");

        PdfAPreparationResult result = document.CheckPdfAPreparation();

        Assert.That(result.IsReady(), Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.HasIssue("PDFA_ENCRYPTION_NOT_ALLOWED"), Is.True);
        Assert.That(result.HasIssue("PDFA_ATTACHMENTS_NOT_ALLOWED"), Is.True);
        Assert.That(result.HasIssue("PDFA_EMBED_FONTS"), Is.True);
        Assert.That(result.HasIssue("PDFA_FORM_APPEARANCES_REQUIRED"), Is.True);
        Assert.That(result.CountErrors(), Is.EqualTo(4));
    }

    [Test]
    public void CheckPdfAPreparation_ReportsWarningsForMetadataImagesAndAnnotations()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddImage(1, 1, new byte[] { 255, 0, 0 });
        page.AddTextAnnotation("Review", 10, 10, 20, 20);

        PdfAPreparationResult result = document.CheckPdfAPreparation();

        Assert.That(result.IsReady(), Is.True);
        Assert.That(result.HasIssue("PDFA_METADATA_TITLE_MISSING"), Is.True);
        Assert.That(result.HasIssue("PDFA_METADATA_PRODUCER_MISSING"), Is.True);
        Assert.That(result.HasIssue("PDFA_IMAGE_COLOR_PROFILE_REVIEW"), Is.True);
        Assert.That(result.HasIssue("PDFA_ANNOTATION_REVIEW"), Is.True);
        Assert.That(result.CountWarnings(), Is.EqualTo(4));
    }

    [Test]
    public void CheckPdfAPreparation_ReportsNoPagesAndRejectsNullDocument()
    {
        PdfDocument document = new PdfDocument();

        PdfAPreparationResult result = document.CheckPdfAPreparation();

        Assert.That(result.IsReady(), Is.False);
        Assert.That(result.HasIssue("PDFA_NO_PAGES"), Is.True);
        Assert.Throws<ArgumentNullException>(() => new PdfAPreparationChecker().Check(null!));
    }
}
