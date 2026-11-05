using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageBoxAndViewerTests
{
    [Test]
    public void PageBoxesWriteConfiguredBoundariesAndUseDefensiveCopies()
    {
        PdfDocument document = new PdfDocument();
        IPdfPage page = document.AddPage(600, 800);
        PdfRectangle media = new PdfRectangle(-10, -20, 620, 840);
        PdfRectangle crop = new PdfRectangle(0, 0, 600, 800);

        page.SetMediaBox(media);
        page.SetCropBox(crop);
        page.SetBleedBox(new PdfRectangle(-5, -10, 610, 820));
        page.SetTrimBox(new PdfRectangle(10, 20, 580, 760));
        page.SetArtBox(new PdfRectangle(20, 30, 560, 740));
        media.X = 100;
        crop.Width = 1;
        page.GetArtBox().Height = 1;

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/MediaBox [-10 -20 610 820]"));
        Assert.That(pdf, Does.Contain("/CropBox [0 0 600 800]"));
        Assert.That(pdf, Does.Contain("/BleedBox [-5 -10 605 810]"));
        Assert.That(pdf, Does.Contain("/TrimBox [10 20 590 780]"));
        Assert.That(pdf, Does.Contain("/ArtBox [20 30 580 770]"));
    }

    [Test]
    public void EffectivePageBoxesDefaultToMediaAndCropWithoutWritingOptionalEntries()
    {
        PdfDocument document = new PdfDocument();
        IPdfPage page = document.AddPage(200, 300);

        AssertRectangle(page.GetMediaBox(), 0, 0, 200, 300);
        AssertRectangle(page.GetCropBox(), 0, 0, 200, 300);
        AssertRectangle(page.GetBleedBox(), 0, 0, 200, 300);
        AssertRectangle(page.GetTrimBox(), 0, 0, 200, 300);
        AssertRectangle(page.GetArtBox(), 0, 0, 200, 300);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/MediaBox [0 0 200 300]"));
        Assert.That(pdf, Does.Not.Contain("/CropBox"));
        Assert.That(pdf, Does.Not.Contain("/BleedBox"));
        Assert.That(pdf, Does.Not.Contain("/TrimBox"));
        Assert.That(pdf, Does.Not.Contain("/ArtBox"));
    }

    [Test]
    public void MediaBoxOriginAndSizeDriveDimensionsAndContentCoordinates()
    {
        IPdfPage page = new PdfPage(200, 300);
        page.SetMargins(5, 6, 7, 8);

        page.SetMediaBox(new PdfRectangle(10, 20, 200, 300));

        PdfPageDimensions dimensions = page.GetDimensions();
        Assert.That(dimensions.Width, Is.EqualTo(200));
        Assert.That(dimensions.Height, Is.EqualTo(300));
        Assert.That(page.GetContentLeft(), Is.EqualTo(15));
        Assert.That(page.GetContentRight(), Is.EqualTo(203));
        Assert.That(page.GetContentTop(), Is.EqualTo(314));
        Assert.That(page.GetContentBottom(), Is.EqualTo(28));
        AssertRectangle(page.GetContentArea(), 15, 28, 188, 286);
    }

    [Test]
    public void PageBoxesRejectInvalidOrUncontainedRectangles()
    {
        IPdfPage page = new PdfPage(200, 300);

        Assert.Throws<ArgumentNullException>(() => page.SetMediaBox(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetMediaBox(new PdfRectangle(0, 0, 0, 300)));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.SetCropBox(new PdfRectangle(double.NaN, 0, 100, 100)));
        Assert.Throws<ArgumentException>(() => page.SetCropBox(new PdfRectangle(150, 0, 100, 100)));
        Assert.Throws<ArgumentException>(() => page.SetBleedBox(new PdfRectangle(-1, 0, 100, 100)));

        page.SetCropBox(new PdfRectangle(10, 10, 180, 280));
        Assert.Throws<InvalidOperationException>(() => page.SetMediaBox(new PdfRectangle(20, 20, 160, 260)));

        IPdfPage marginedPage = new PdfPage(200, 300);
        marginedPage.SetMargins(60, 20, 60, 20);
        Assert.Throws<InvalidOperationException>(() => marginedPage.SetMediaBox(new PdfRectangle(0, 0, 100, 300)));
    }

    [Test]
    public void ViewerSettingsWriteCatalogNamesAndCopyPreferences()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 300);
        document.SetPdfVersion(PdfVersion.Pdf16);
        PdfViewerPreferences preferences = new PdfViewerPreferences();
        preferences.HideToolbar = true;
        preferences.HideMenubar = true;
        preferences.HideWindowUi = true;
        preferences.FitWindow = true;
        preferences.CenterWindow = true;
        preferences.NonFullScreenPageMode = PdfPageMode.UseOutlines;
        preferences.ReadingDirection = PdfReadingDirection.RightToLeft;

        document.SetViewerPreferences(preferences);
        document.SetDisplayDocumentTitle(true);
        document.SetInitialPageMode(PdfPageMode.FullScreen);
        document.SetInitialPageLayout(PdfPageLayout.TwoPageRight);
        preferences.HideToolbar = false;
        preferences.ReadingDirection = PdfReadingDirection.LeftToRight;

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/PageLayout /TwoPageRight /PageMode /FullScreen"));
        Assert.That(pdf, Does.Contain("/ViewerPreferences << /HideToolbar true /HideMenubar true /HideWindowUI true /FitWindow true /CenterWindow true /DisplayDocTitle true /NonFullScreenPageMode /UseOutlines /Direction /R2L >>"));
    }

    [Test]
    public void DefaultDocumentOmitsViewerCatalogEntries()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 300);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Not.Contain("/PageLayout"));
        Assert.That(pdf, Does.Not.Contain("/PageMode"));
        Assert.That(pdf, Does.Not.Contain("/ViewerPreferences"));
    }

    [Test]
    public void ExplicitPageModeOverridesOutlineDefault()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 300);
        document.AddOutlineAtPageTop("Start", page);
        document.SetInitialPageMode(PdfPageMode.UseThumbnails);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/Outlines 5 0 R /PageMode /UseThumbs"));
        Assert.That(pdf, Does.Not.Contain("/PageMode /UseOutlines"));
    }

    [Test]
    public void ViewerSettingsValidateEnumsAndPdfVersionRequirements()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 300);

        Assert.Throws<ArgumentException>(() => document.SetInitialPageMode((PdfPageMode)99));
        Assert.Throws<ArgumentException>(() => document.SetInitialPageLayout((PdfPageLayout)99));
        Assert.Throws<InvalidOperationException>(() => document.SetInitialPageMode(PdfPageMode.UseOptionalContent));
        Assert.Throws<InvalidOperationException>(() => document.SetInitialPageMode(PdfPageMode.UseAttachments));
        Assert.Throws<InvalidOperationException>(() => document.SetInitialPageLayout(PdfPageLayout.TwoPageLeft));

        PdfViewerPreferences invalidMode = new PdfViewerPreferences();
        invalidMode.NonFullScreenPageMode = PdfPageMode.FullScreen;
        Assert.Throws<ArgumentException>(() => document.SetViewerPreferences(invalidMode));
        PdfViewerPreferences invalidDirection = new PdfViewerPreferences();
        invalidDirection.ReadingDirection = (PdfReadingDirection)99;
        Assert.Throws<ArgumentException>(() => document.SetViewerPreferences(invalidDirection));

        document.SetPdfVersion(PdfVersion.Pdf15);
        document.SetInitialPageMode(PdfPageMode.UseOptionalContent);
        document.SetInitialPageLayout(PdfPageLayout.TwoPageLeft);
        Assert.Throws<InvalidOperationException>(() => document.SetInitialPageMode(PdfPageMode.UseAttachments));
    }

    [Test]
    public void LaterViewerSettingsPreventAnIncompatibleVersionChangeOrSave()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 300);
        document.SetPdfVersion(PdfVersion.Pdf16);
        document.SetInitialPageMode(PdfPageMode.UseAttachments);

        Assert.Throws<InvalidOperationException>(() => document.SetPdfVersion(PdfVersion.Pdf14));
        Assert.That(document.WriterOptions.Version, Is.EqualTo(PdfVersion.Pdf16));

        PdfWriterOptions options = new PdfWriterOptions();
        options.Version = PdfVersion.Pdf16;
        PdfDocument mutableDocument = new PdfDocument();
        mutableDocument.AddPage(200, 300);
        mutableDocument.SetWriterOptions(options);
        mutableDocument.SetInitialPageMode(PdfPageMode.UseAttachments);
        options.Version = PdfVersion.Pdf14;

        Assert.Throws<InvalidOperationException>(() => mutableDocument.ToBytes());
    }

    static void AssertRectangle(PdfRectangle rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle.X, Is.EqualTo(x));
        Assert.That(rectangle.Y, Is.EqualTo(y));
        Assert.That(rectangle.Width, Is.EqualTo(width));
        Assert.That(rectangle.Height, Is.EqualTo(height));
    }
}
