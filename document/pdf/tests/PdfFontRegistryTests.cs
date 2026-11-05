using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfFontRegistryTests
{
    [Test]
    public void CreateStandard_RegistersStandardTypeOneFontsAndCompatibilityAliases()
    {
        PdfFontRegistry registry = PdfFontRegistry.CreateStandard();

        Assert.That(registry.Fonts.Count, Is.EqualTo(18));
        AssertFont(registry, PdfFont.Regular, "F1", "Helvetica");
        AssertFont(registry, PdfFont.Bold, "F2", "Helvetica-Bold");
        AssertFont(registry, PdfFont.Italic, "F3", "Helvetica-Oblique");
        AssertFont(registry, PdfFont.BoldItalic, "F4", "Helvetica-BoldOblique");
        AssertFont(registry, PdfFont.Courier, "F5", "Courier");
        AssertFont(registry, PdfFont.CourierBold, "F6", "Courier-Bold");
        AssertFont(registry, PdfFont.CourierOblique, "F7", "Courier-Oblique");
        AssertFont(registry, PdfFont.CourierBoldOblique, "F8", "Courier-BoldOblique");
        AssertFont(registry, PdfFont.TimesRoman, "F9", "Times-Roman");
        AssertFont(registry, PdfFont.TimesBold, "F10", "Times-Bold");
        AssertFont(registry, PdfFont.TimesItalic, "F11", "Times-Italic");
        AssertFont(registry, PdfFont.TimesBoldItalic, "F12", "Times-BoldItalic");
        AssertFont(registry, PdfFont.Symbol, "F13", "Symbol");
        AssertFont(registry, PdfFont.ZapfDingbats, "F14", "ZapfDingbats");
    }

    [Test]
    public void RegisterStandardFont_RejectsDuplicateFontKeys()
    {
        PdfFontRegistry registry = new PdfFontRegistry();

        registry.RegisterStandardFont(PdfFont.Regular, "F1", "Helvetica");

        Assert.Throws<ArgumentException>(() => registry.RegisterStandardFont(PdfFont.Regular, "F99", "Courier"));
    }

    [Test]
    public void StandardFonts_RejectMissingResourceAndBaseFontNames()
    {
        PdfFontRegistry registry = new PdfFontRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterStandardFont(PdfFont.Regular, null!, "Helvetica"));
        Assert.Throws<ArgumentException>(() => registry.RegisterStandardFont(PdfFont.Regular, string.Empty, "Helvetica"));
        Assert.Throws<ArgumentNullException>(() => new PdfStandardFont(PdfFont.Regular, "F1", null!));
        Assert.Throws<ArgumentException>(() => new PdfStandardFont(PdfFont.Regular, "F1", string.Empty));
        Assert.That(registry.Fonts, Is.Empty);
    }

    [Test]
    public void TrueTypeIdentityLookupsRejectNullConsistently()
    {
        PdfFontRegistry registry = new PdfFontRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.HasTrueTypeFont(null!));
        Assert.Throws<ArgumentNullException>(() => registry.GetTrueTypeFont(null!));
    }

    [Test]
    public void Canvas_DrawTextUsesRegistryResourceNames()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawTextWithFont("Courier text", 10, 20, 12, PdfFont.Courier);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("/F5 12 Tf"));
        Assert.That(canvas.Fonts.Count, Is.EqualTo(1));
        Assert.That(canvas.Fonts[0], Is.EqualTo(PdfFont.Courier));
    }

    [Test]
    public void Save_WritesAdditionalStandardFontObjectWhenUsed()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.Canvas.DrawTextWithFont("Courier text", 10, 20, 12, PdfFont.Courier);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"));
        Assert.That(pdf, Does.Contain("/Font << /F1 3 0 R /F2 4 0 R /F5 5 0 R >>"));
        Assert.That(pdf, Does.Contain("/F5 12 Tf"));
        Assert.That(pdf, Does.Contain("/Contents 7 0 R"));
    }

    [Test]
    public void Save_FontAliasesDoNotDuplicateExistingFontResources()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.Canvas.DrawTextWithFont("Helvetica alias", 10, 20, 12, PdfFont.Helvetica);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("/Font << /F1 3 0 R /F2 4 0 R >>"));
        Assert.That(pdf, Does.Contain("/Contents 6 0 R"));
        Assert.That(pdf, Does.Not.Contain("/F1 5 0 R"));
    }

    static void AssertFont(PdfFontRegistry registry, PdfFont font, string resourceName, string baseFontName)
    {
        PdfStandardFont standardFont = registry.GetFont(font);

        Assert.That(standardFont.ResourceName, Is.EqualTo(resourceName));
        Assert.That(standardFont.BaseFontName, Is.EqualTo(baseFontName));
        Assert.That(registry.GetResourceName(font), Is.EqualTo(resourceName));
        Assert.That(registry.GetBaseFontName(font), Is.EqualTo(baseFontName));
    }
}
