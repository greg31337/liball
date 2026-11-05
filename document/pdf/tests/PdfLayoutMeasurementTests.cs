using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfLayoutMeasurementTests
{
    [Test]
    public void LayoutContext_CanUsePageContentArea()
    {
        PdfPage page = new PdfPage(200, 100);

        page.SetMargins(10, 20, 30, 5);
        PdfLayoutContext context = PdfLayoutContext.FromPageContent(page);

        Assert.That(context.AvailableWidth, Is.EqualTo(160));
        Assert.That(context.AvailableHeight, Is.EqualTo(75));
    }

    [Test]
    public void Block_MeasuresFixedContentWithInsets()
    {
        PdfBlock block = PdfBlock.WithSize(50, 20);
        PdfLayoutContext context = new PdfLayoutContext(100, 60);

        block.SetPadding(new PdfInsets(5, 6, 7, 8));
        block.SetBorder(PdfInsets.Uniform(1));
        block.SetMargin(new PdfInsets(2, 3, 4, 5));
        PdfLayoutResult result = block.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(70));
        Assert.That(result.Size.Height, Is.EqualTo(44));
        AssertRectangle(result.Box.ContentRectangle, 0, 0, 50, 20);
        AssertRectangle(result.Box.GetMarginRectangle(), -8, -14, 70, 44);
    }

    [Test]
    public void Block_MeasureReportsWhenBlockDoesNotFit()
    {
        PdfBlock block = PdfBlock.WithSize(120, 20);
        PdfLayoutContext context = new PdfLayoutContext(100, 60);

        PdfLayoutResult result = block.Measure(context);

        Assert.That(result.Fits, Is.False);
        Assert.That(result.Size.Width, Is.EqualTo(120));
        Assert.That(result.Size.Height, Is.EqualTo(20));
    }

    [Test]
    public void TextBlock_MeasuresWrappedLinesWithParagraphSpacing()
    {
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 3, 5, 0);
        PdfTextBlock block = new PdfTextBlock("one two three", paragraphStyle);
        PdfLayoutContext context = new PdfLayoutContext(46, 100);

        PdfLayoutResult result = block.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Lines, Is.EqualTo(new string[] { "one two", "three" }));
        Assert.That(result.Size.Width, Is.EqualTo(46));
        Assert.That(result.Size.Height, Is.EqualTo(32));
        AssertRectangle(result.Box.ContentRectangle, 0, 0, 46, 32);
    }

    [Test]
    public void TextBlock_MeasuresInsetsAndFitAgainstContext()
    {
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTextBlock block = new PdfTextBlock("abcdef", paragraphStyle);
        PdfLayoutContext context = new PdfLayoutContext(60, 20);

        block.SetPadding(PdfInsets.Uniform(5));
        block.SetBorder(PdfInsets.Uniform(1));
        block.SetMargin(PdfInsets.Uniform(2));
        PdfLayoutResult result = block.Measure(context);

        Assert.That(result.Fits, Is.False);
        Assert.That(result.Lines, Is.EqualTo(new string[] { "abcdef" }));
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(44));
        Assert.That(result.Size.Width, Is.EqualTo(60));
        Assert.That(result.Size.Height, Is.EqualTo(28));
    }

    [Test]
    public void TextBlock_EmptyTextMeasuresAsBoxInsetsOnly()
    {
        PdfTextBlock block = PdfTextBlock.Plain(string.Empty);
        PdfLayoutContext context = new PdfLayoutContext(50, 10);

        block.SetPadding(PdfInsets.Uniform(2));
        PdfLayoutResult result = block.Measure(context);

        Assert.That(result.Lines, Is.Empty);
        Assert.That(result.Size.Width, Is.EqualTo(50));
        Assert.That(result.Size.Height, Is.EqualTo(4));
        Assert.That(result.Fits, Is.True);
    }

    [Test]
    public void PageBreakBlock_MeasuresAsZeroSizeAndFits()
    {
        PdfPageBreakBlock block = new PdfPageBreakBlock();
        PdfLayoutContext context = new PdfLayoutContext(50, 10);

        PdfLayoutResult result = block.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(0));
        Assert.That(result.Size.Height, Is.EqualTo(0));
        AssertRectangle(result.Box.ContentRectangle, 0, 0, 0, 0);
    }

    [Test]
    public void LayoutMeasurement_RejectsInvalidInputs()
    {
        PdfBlock block = new PdfBlock();
        PdfTextBlock textBlock = PdfTextBlock.Plain("x");
        PdfPageBreakBlock pageBreakBlock = new PdfPageBreakBlock();

        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutContext(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutContext(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutContext(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutContext(0, double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutContext.FromRectangle(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutContext.FromPageContent(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfLayoutResult(null!, PdfBox.FromCoordinates(0, 0, 0, 0), true));
        Assert.Throws<ArgumentNullException>(() => new PdfLayoutResult(new PdfLayoutSize(0, 0), null!, true));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutResult.WithLines(new PdfLayoutSize(0, 0), PdfBox.FromCoordinates(0, 0, 0, 0), true, null!));
        Assert.Throws<ArgumentNullException>(() => block.SetPadding(null!));
        Assert.Throws<ArgumentNullException>(() => block.SetBorder(null!));
        Assert.Throws<ArgumentNullException>(() => block.SetMargin(null!));
        Assert.Throws<ArgumentNullException>(() => block.Measure(null!));
        Assert.Throws<ArgumentNullException>(() => PdfTextBlock.Plain(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTextBlock("x", null!));
        Assert.Throws<ArgumentNullException>(() => textBlock.SetPadding(null!));
        Assert.Throws<ArgumentException>(() => textBlock.Measure(new PdfLayoutContext(0, 10)));
        Assert.Throws<ArgumentNullException>(() => pageBreakBlock.Measure(null!));
    }

    static void AssertRectangle(PdfRectangle rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle.X, Is.EqualTo(x).Within(0.0001));
        Assert.That(rectangle.Y, Is.EqualTo(y).Within(0.0001));
        Assert.That(rectangle.Width, Is.EqualTo(width).Within(0.0001));
        Assert.That(rectangle.Height, Is.EqualTo(height).Within(0.0001));
    }
}
