using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageLayerTests
{
    [Test]
    public void Canvas_PrependAndAppendCanvasMaintainLayerOrderAndFonts()
    {
        PdfCanvas canvas = new PdfCanvas();
        PdfCanvas background = new PdfCanvas();
        PdfCanvas watermark = new PdfCanvas();

        canvas.DrawStyledText("body", 10, 20, PdfTextStyle.Create(10, PdfFont.Regular));
        background.DrawStyledText("background", 0, 0, PdfTextStyle.Create(10, PdfFont.Courier));
        watermark.DrawStyledText("watermark", 20, 30, PdfTextStyle.Create(10, PdfFont.Helvetica));
        canvas.PrependCanvas(background);
        canvas.AppendCanvas(watermark);

        string content = canvas.ToPdfString();
        Assert.That(content.IndexOf("(background) Tj"), Is.LessThan(content.IndexOf("(body) Tj")));
        Assert.That(content.IndexOf("(body) Tj"), Is.LessThan(content.IndexOf("(watermark) Tj")));
        Assert.That(canvas.Fonts.Count, Is.EqualTo(3));
        Assert.That(canvas.Fonts, Does.Contain(PdfFont.Courier));
        Assert.That(canvas.Fonts, Does.Contain(PdfFont.Helvetica));
    }

    [Test]
    public void LayoutDocument_DrawsBackgroundBehindBodyAndWatermarkAboveBodyOnEveryPage()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle bodyTextStyle = PdfTextStyle.Create(10, PdfFont.Regular);
        PdfTextStyle watermarkTextStyle = PdfTextStyle.Create(10, PdfFont.Courier, PdfColor.Gray(0.4));
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(bodyTextStyle, 12, 0, 0, 0);

        layout.SetBackgroundLayer(PdfPageTemplate.FromBackgroundColor(PdfColor.Gray(0.9), 60));
        layout.SetWatermarkLayer(PdfPageTemplate.FromWatermarkText("Draft {page}/{total}", watermarkTextStyle, 60, 30));
        layout.AddTextBlock(new PdfTextBlock("body1", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("body2", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("body3", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("body4", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("body5", paragraphStyle));
        layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(firstPage, Does.Contain("0 0 100 60 re f"));
        Assert.That(secondPage, Does.Contain("0 0 100 60 re f"));
        Assert.That(firstPage, Does.Contain("(Draft 1/2) Tj"));
        Assert.That(secondPage, Does.Contain("(Draft 2/2) Tj"));
        Assert.That(firstPage.IndexOf("0.9 g"), Is.LessThan(firstPage.IndexOf("(body1) Tj")));
        Assert.That(firstPage.IndexOf("(body1) Tj"), Is.LessThan(firstPage.IndexOf("(Draft 1/2) Tj")));
    }

    [Test]
    public void LayoutDocument_ClearLayersStopsLayerRendering()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetBackgroundLayer(PdfPageTemplate.FromBackgroundColor(PdfColor.Gray(0.9), 60));
        layout.SetWatermarkLayer(PdfPageTemplate.FromWatermarkText("Draft", textStyle, 60, 30));
        layout.ClearBackgroundLayer();
        layout.ClearWatermarkLayer();
        layout.AddTextBlock(new PdfTextBlock("body", paragraphStyle));
        layout.Render();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("(body) Tj"));
        Assert.That(content, Does.Not.Contain("0.9 g"));
        Assert.That(content, Does.Not.Contain("(Draft) Tj"));
    }

    [Test]
    public void PageLayers_RejectInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPageTemplate template = PdfPageTemplate.FromBackgroundColor(PdfColor.Gray(0.9), 60);
        PdfLayoutDocument layout = PdfLayoutDocument.Default(document);
        PdfPageCursor cursor = PdfPageCursor.Create(new PdfDocument());
        PdfCanvas canvas = new PdfCanvas();

        Assert.Throws<ArgumentNullException>(() => canvas.PrependCanvas(null!));
        Assert.Throws<ArgumentNullException>(() => canvas.AppendCanvas(null!));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromBackgroundColor(null!, 60));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPageTemplate.FromBackgroundColor(PdfColor.Gray(0.9), 0));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromWatermarkText(null!, new PdfTextStyle(), 60, 30));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromWatermarkText("Draft", null!, 60, 30));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfPageTemplate.FromWatermarkText("Draft", new PdfTextStyle(), 0, 30));
        Assert.Throws<ArgumentNullException>(() => layout.SetBackgroundLayer(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetWatermarkLayer(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawBackgroundLayer(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawWatermarkLayer(null!));

        cursor.DrawBackgroundLayer(template);
        cursor.DrawWatermarkLayer(template);
        Assert.That(cursor.Document.Pages[0].Canvas.ToPdfString(), Does.Contain("0 0 612 60 re f"));
    }
}
