using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfLayoutDrawTests
{
    [Test]
    public void DrawContext_CanUsePageContentArea()
    {
        PdfPage page = new PdfPage(200, 100);

        page.SetMargins(10, 20, 30, 5);
        PdfLayoutDrawContext context = PdfLayoutDrawContext.FromPageContent(page);

        Assert.That(context.Page, Is.SameAs(page));
        Assert.That(context.X, Is.EqualTo(10));
        Assert.That(context.Y, Is.EqualTo(5));
        Assert.That(context.AvailableWidth, Is.EqualTo(160));
        Assert.That(context.AvailableHeight, Is.EqualTo(75));
        Assert.That(context.ToLayoutContext().AvailableWidth, Is.EqualTo(160));
    }

    [Test]
    public void Block_DrawReturnsMeasurementWithoutWritingCanvasCommands()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfBlock block = PdfBlock.WithSize(50, 20);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 100, 60);

        PdfLayoutResult result = block.Draw(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(50));
        Assert.That(result.Size.Height, Is.EqualTo(20));
        Assert.That(page.Canvas.ToPdfString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void TextBlock_DrawWritesWrappedLinesFromTopOfMeasuredContent()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 3, 5, 0);
        PdfTextBlock block = new PdfTextBlock("one two three", paragraphStyle);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 46, 100);

        PdfLayoutResult result = block.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Lines, Is.EqualTo(new string[] { "one two", "three" }));
        Assert.That(result.Size.Height, Is.EqualTo(32));
        Assert.That(content, Does.Contain("/F5 10 Tf"));
        Assert.That(content, Does.Contain("10 39 Td"));
        Assert.That(content, Does.Contain("(one two) Tj"));
        Assert.That(content, Does.Contain("10 27 Td"));
        Assert.That(content, Does.Contain("(three) Tj"));
    }

    [Test]
    public void TextBlock_DrawAppliesInsetsFirstLineIndentAndAlignment()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 4, PdfTextAlignment.Right);
        PdfTextBlock block = new PdfTextBlock("ABC", paragraphStyle);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 60, 100);

        block.SetPadding(PdfInsets.Uniform(5));
        block.SetBorder(PdfInsets.Uniform(1));
        block.SetMargin(PdfInsets.Uniform(2));
        PdfLayoutResult result = block.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(44));
        Assert.That(result.Size.Width, Is.EqualTo(60));
        Assert.That(content, Does.Contain("44 30 Td"));
        Assert.That(content, Does.Contain("(ABC) Tj"));
    }

    [Test]
    public void TextBlock_DrawReturnsNonFittingResultButStillWritesMeasuredLines()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTextBlock block = new PdfTextBlock("one two three", paragraphStyle);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 46, 10);

        PdfLayoutResult result = block.Draw(context);

        Assert.That(result.Fits, Is.False);
        Assert.That(page.Canvas.ToPdfString(), Does.Contain("(one two) Tj"));
    }

    [Test]
    public void PageBreakBlock_DrawReturnsZeroMeasurementWithoutWritingContent()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfPageBreakBlock block = new PdfPageBreakBlock();
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 100, 60);

        PdfLayoutResult result = block.Draw(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(0));
        Assert.That(result.Size.Height, Is.EqualTo(0));
        Assert.That(page.Canvas.ToPdfString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void LayoutDraw_RejectsInvalidInputs()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfBlock block = new PdfBlock();
        PdfTextBlock textBlock = PdfTextBlock.Plain("x");
        PdfPageBreakBlock pageBreakBlock = new PdfPageBreakBlock();

        Assert.Throws<ArgumentNullException>(() => new PdfLayoutDrawContext(null!, 0, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutDrawContext(page, 0, 0, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutDrawContext(page, 0, 0, 10, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutDrawContext(page, double.NaN, 0, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutDrawContext(page, 0, double.PositiveInfinity, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutDrawContext(page, 0, 0, double.NaN, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutDrawContext(page, 0, 0, 10, double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutDrawContext.FromPageContent(null!));
        Assert.Throws<ArgumentNullException>(() => block.Draw(null!));
        Assert.Throws<ArgumentNullException>(() => textBlock.Draw(null!));
        Assert.Throws<ArgumentNullException>(() => pageBreakBlock.Draw(null!));
    }
}
