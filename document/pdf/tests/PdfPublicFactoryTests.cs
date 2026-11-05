using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPublicFactoryTests
{
    [Test]
    public void LayoutFactory_CreatesFixedAndSpacedContainerBlocks()
    {
        IPdfLayoutFactory factory = new PdfLayoutFactory();
        PdfBlock first = factory.CreateFixedBlock(30, 10);
        PdfBlock second = factory.CreateFixedBlock(40, 20);
        PdfStackBlock stack = factory.CreateStackBlock(3);
        PdfRowBlock row = factory.CreateRowBlock(5);

        stack.AddBlock(first);
        stack.AddBlock(second);
        row.AddBlock(first);
        row.AddBlock(second);

        Assert.That(first.Measure(new PdfLayoutContext(100, 100)).Size.Width, Is.EqualTo(30));
        Assert.That(first.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(10));
        Assert.That(stack.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(33));
        Assert.That(row.Measure(new PdfLayoutContext(100, 100)).Size.Width, Is.EqualTo(75));
    }

    [Test]
    public void LayoutFactory_CreatesConfiguredColumnsAndGrids()
    {
        IPdfLayoutFactory factory = new PdfLayoutFactory();
        PdfColumnBlock column = factory.CreateSpacedColumnBlock(46, 2);
        PdfColumnBlock plainColumn = factory.CreateColumnBlock(30);
        PdfGridBlock grid = factory.CreateGappedGridBlock(3, 5, 4);
        PdfGridBlock plainGrid = factory.CreateGridBlock(2);

        column.AddBlock(factory.CreateFixedBlock(20, 8));
        column.AddBlock(factory.CreateFixedBlock(20, 6));
        plainColumn.AddBlock(factory.CreateFixedBlock(10, 5));
        grid.AddBlock(factory.CreateFixedBlock(20, 10));
        grid.AddBlock(factory.CreateFixedBlock(20, 20));
        grid.AddBlock(factory.CreateFixedBlock(20, 15));
        grid.AddBlock(factory.CreateFixedBlock(20, 25));
        plainGrid.AddBlock(factory.CreateFixedBlock(10, 5));
        plainGrid.AddBlock(factory.CreateFixedBlock(10, 5));

        Assert.That(column.Measure(new PdfLayoutContext(100, 100)).Size.Width, Is.EqualTo(46));
        Assert.That(column.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(16));
        Assert.That(plainColumn.Measure(new PdfLayoutContext(100, 100)).Size.Width, Is.EqualTo(30));
        Assert.That(grid.GetRowCount(), Is.EqualTo(2));
        Assert.That(grid.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(49));
        Assert.That(plainGrid.GetRowCount(), Is.EqualTo(1));
    }

    [Test]
    public void LayoutFactory_PreservesBlockConfigurationValidation()
    {
        IPdfLayoutFactory factory = new PdfLayoutFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateStackBlock(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateRowBlock(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateColumnBlock(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateSpacedColumnBlock(10, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateGridBlock(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateGappedGridBlock(2, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateGappedGridBlock(2, 0, -1));
    }

    [Test]
    public void StyleFactory_CreatesCommonInsetsAndDeviceColors()
    {
        IPdfStyleFactory factory = new PdfStyleFactory();
        PdfInsets none = factory.CreateNoInsets();
        PdfInsets uniform = factory.CreateUniformInsets(5);
        PdfInsets verticalHorizontal = factory.CreateVerticalHorizontalInsets(6, 7);
        PdfColor rgb = factory.CreateRgbColor(0.1, 0.2, 0.3);
        PdfColor gray = factory.CreateGrayColor(0.4);
        PdfColor cmyk = factory.CreateCmykColor(0.1, 0.2, 0.3, 0.4);

        Assert.That(none.GetHorizontal(), Is.EqualTo(0));
        Assert.That(none.GetVertical(), Is.EqualTo(0));
        Assert.That(uniform.GetHorizontal(), Is.EqualTo(10));
        Assert.That(uniform.GetVertical(), Is.EqualTo(10));
        Assert.That(verticalHorizontal.GetHorizontal(), Is.EqualTo(14));
        Assert.That(verticalHorizontal.GetVertical(), Is.EqualTo(12));
        Assert.That(rgb.Mode, Is.EqualTo("RGB"));
        Assert.That(gray.Mode, Is.EqualTo("GRAY"));
        Assert.That(cmyk.Mode, Is.EqualTo("CMYK"));
    }

    [Test]
    public void StyleFactory_CreatesTextAndParagraphStyles()
    {
        IPdfStyleFactory factory = new PdfStyleFactory();
        PdfColor color = factory.CreateGrayColor(0.25);
        PdfTextStyle regular = factory.CreateRegularTextStyle(10);
        PdfTextStyle bold = factory.CreateBoldTextStyle(14);
        PdfTextStyle text = factory.CreateTextStyle(11, PdfFont.Courier);
        PdfTextStyle colored = factory.CreateColoredTextStyle(9, PdfFont.TimesRoman, color);
        PdfParagraphStyle natural = factory.CreateParagraphStyle(regular);
        PdfParagraphStyle metrics = factory.CreateParagraphStyleWithMetrics(text, 15, 1, 2, 3);
        PdfParagraphStyle aligned = factory.CreateAlignedParagraphStyle(colored, 12, 2, 3, 4, PdfTextAlignment.Center);

        Assert.That(regular.Font, Is.EqualTo(PdfFont.Regular));
        Assert.That(bold.Font, Is.EqualTo(PdfFont.Bold));
        Assert.That(text.Font, Is.EqualTo(PdfFont.Courier));
        Assert.That(colored.FillColor, Is.SameAs(color));
        Assert.That(natural.LineHeight, Is.EqualTo(12));
        Assert.That(metrics.SpacingBefore, Is.EqualTo(1));
        Assert.That(metrics.SpacingAfter, Is.EqualTo(2));
        Assert.That(metrics.FirstLineIndent, Is.EqualTo(3));
        Assert.That(aligned.Alignment, Is.EqualTo(PdfTextAlignment.Center));
    }

    [Test]
    public void StyleFactory_PreservesStyleValidation()
    {
        IPdfStyleFactory factory = new PdfStyleFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateUniformInsets(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateRgbColor(1.1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateGrayColor(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateRegularTextStyle(0));
        Assert.Throws<ArgumentNullException>(() => factory.CreateColoredTextStyle(10, PdfFont.Regular, null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateParagraphStyle(null!));
        Assert.Throws<ArgumentException>(() => factory.CreateAlignedParagraphStyle(new PdfTextStyle(), 12, 0, 0, 0, (PdfTextAlignment)99));
    }

    [Test]
    public void LayoutFactory_CreatesPageTextAndPageLayerTemplates()
    {
        IPdfLayoutFactory layoutFactory = new PdfLayoutFactory();
        IPdfStyleFactory styleFactory = new PdfStyleFactory();
        PdfTextStyle textStyle = styleFactory.CreateTextStyle(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = styleFactory.CreateParagraphStyleWithMetrics(textStyle, 12, 0, 0, 0);
        PdfPageTemplate pageText = layoutFactory.CreatePageTextTemplate("Page {page}/{total}", paragraphStyle, 12);
        PdfPageTemplate background = layoutFactory.CreateBackgroundTemplate(styleFactory.CreateGrayColor(0.9), 60);
        PdfPageTemplate watermark = layoutFactory.CreateWatermarkTemplate("Draft {page}", textStyle, 60, 30);
        PdfPage page = new PdfPage(100, 60);

        background.Draw(page, 0, 0, 100);
        watermark.DrawForPage(page, 0, 0, 100, 2, 5);
        pageText.DrawForPage(page, 10, 40, 80, 2, 5);

        string content = page.GetCanvas().ToPdfString();
        Assert.That(content, Does.Contain("0.9 g"));
        Assert.That(content, Does.Contain("0 0 100 60 re f"));
        Assert.That(content, Does.Contain("(Draft 2) Tj"));
        Assert.That(content, Does.Contain("(Page 2/5) Tj"));
        Assert.That(pageText.NeedsFinalTemplatePass(), Is.True);
        Assert.That(background.NeedsFinalTemplatePass(), Is.False);
        Assert.That(watermark.NeedsFinalTemplatePass(), Is.True);
    }

    [Test]
    public void LayoutFactory_PreservesPageLayerValidation()
    {
        IPdfLayoutFactory factory = new PdfLayoutFactory();

        Assert.Throws<ArgumentNullException>(() => factory.CreatePageTextTemplate(null!, new PdfParagraphStyle(), 12));
        Assert.Throws<ArgumentNullException>(() => factory.CreatePageTextTemplate("Page", null!, 12));
        Assert.Throws<ArgumentNullException>(() => factory.CreateBackgroundTemplate(null!, 60));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateBackgroundTemplate(new PdfStyleFactory().CreateGrayColor(1), 0));
        Assert.Throws<ArgumentNullException>(() => factory.CreateWatermarkTemplate(null!, new PdfTextStyle(), 60, 0));
        Assert.Throws<ArgumentNullException>(() => factory.CreateWatermarkTemplate("Draft", null!, 60, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateWatermarkTemplate("Draft", new PdfTextStyle(), 0, 0));
    }
}
