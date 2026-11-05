using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfReusableStyleTests
{
    [Test]
    public void ElementStylePaintsBackgroundOpacityAndDashedBorderAndCopiesValues()
    {
        PdfElementStyle style = new PdfElementStyle();
        style.Padding = new PdfInsets(2, 2, 2, 2);
        style.Border = new PdfInsets(1, 1, 1, 1);
        style.Margin = new PdfInsets(3, 3, 3, 3);
        style.HasBackground = true;
        style.BackgroundColor = new PdfColor("RGB", 0.2, 0.4, 0.6, 0);
        style.BackgroundOpacity = 0.5;
        style.BorderColor = new PdfColor("GRAY", 0.25, 0, 0, 0);
        style.BorderOpacity = 0.75;
        style.BorderDashPattern = new double[] { 3, 2 };
        style.BorderDashPhase = 1;
        PdfBlock block = new PdfLayoutFactory().CreateFixedBlock(20, 10);
        block.SetStyle(style);
        style.Padding = new PdfInsets(20, 20, 20, 20);
        style.BorderDashPattern[0] = 99;

        PdfPage page = new PdfPage(100, 100);
        PdfLayoutResult result = block.Draw(new PdfLayoutDrawContext(page, 10, 20, 80, 60));
        string content = page.GetCanvas().ToPdfString();

        Assert.That(result.Size.Width, Is.EqualTo(32));
        Assert.That(result.Size.Height, Is.EqualTo(22));
        Assert.That(content, Does.Contain("0.2 0.4 0.6 rg"));
        Assert.That(content, Does.Contain("/GSF0p5 gs"));
        Assert.That(content, Does.Contain("13 23 26 16 re f"));
        Assert.That(content, Does.Contain("0.25 G"));
        Assert.That(content, Does.Contain("/GSS0p75 gs"));
        Assert.That(content, Does.Contain("[3 2] 1 d"));
        Assert.That(content, Does.Not.Contain("99"));
    }

    [Test]
    public void ReusableStylesControlContainerInsetsAndInterItemSpacing()
    {
        PdfElementStyle style = new PdfElementStyle();
        style.Padding = new PdfInsets(1, 1, 1, 1);
        style.ItemSpacing = 4;
        PdfStackBlock stack = new PdfStackBlock();
        stack.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        stack.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        stack.SetStyle(style);

        PdfLayoutResult result = stack.Measure(new PdfLayoutContext(100, 100));

        Assert.That(result.Size.Width, Is.EqualTo(12));
        Assert.That(result.Size.Height, Is.EqualTo(26));

        PdfRowBlock row = new PdfRowBlock();
        row.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        row.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        row.SetSpacing(4);
        Assert.That(row.Measure(new PdfLayoutContext(100, 100)).Size.Width, Is.EqualTo(24));

        PdfColumnBlock column = new PdfColumnBlock();
        column.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        column.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        column.SetSpacing(4);
        Assert.That(column.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(24));

        PdfGridBlock grid = new PdfLayoutFactory().CreateGridBlock(2);
        grid.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        grid.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        grid.AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        grid.SetGaps(3, 4);
        Assert.That(grid.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(24));
    }

    [Test]
    public void TableStyleAppliesSeparateHeaderBodyAndFooterCellStyles()
    {
        PdfTableStyle style = new PdfTableStyle();
        ConfigureCellStyle(style.HeaderCell, 1, 0.8);
        ConfigureCellStyle(style.BodyCell, 2, 0.6);
        ConfigureCellStyle(style.FooterCell, 3, 0.4);
        PdfTableBlock table = new PdfTableBlock(new double[] { 50 });
        table.AddHeaderRow().AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        table.AddRow().AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        table.AddFooterRow().AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        table.SetTableStyle(style);
        style.HeaderCell.BackgroundColor.First = 0.1;

        PdfPage page = new PdfPage(100, 100);
        PdfLayoutResult result = table.Draw(new PdfLayoutDrawContext(page, 0, 0, 50, 100));
        string content = page.GetCanvas().ToPdfString();

        Assert.That(result.Size.Height, Is.EqualTo(42));
        Assert.That(content, Does.Contain("0.8 g"));
        Assert.That(content, Does.Contain("0 30 50 12 re f"));
        Assert.That(content, Does.Contain("0.6 g"));
        Assert.That(content, Does.Contain("0 16 50 14 re f"));
        Assert.That(content, Does.Contain("0.4 g"));
        Assert.That(content, Does.Contain("0 0 50 16 re f"));
        Assert.That(content, Does.Not.Contain("0.1 g"));
    }

    [Test]
    public void DocumentThemeAppliesDefensiveParagraphBlockAndTableDefaults()
    {
        PdfStyleFactory factory = new PdfStyleFactory();
        PdfDocumentTheme theme = factory.CreateDocumentTheme();
        theme.BodyParagraph = factory.CreateParagraphStyleWithMetrics(factory.CreateTextStyle(8, PdfFont.Courier), 18, 0, 0, 0);
        theme.Block.Padding = new PdfInsets(2, 2, 2, 2);
        theme.Table.BodyCell.Padding = new PdfInsets(3, 3, 3, 3);
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0));
        layout.SetTheme(theme);
        theme.BodyParagraph.LineHeight = 90;
        theme.Block.Padding = new PdfInsets(20, 20, 20, 20);
        theme.Table.BodyCell.Padding = new PdfInsets(30, 30, 30, 30);
        PdfTextBlock text = new PdfTextBlock("body", factory.CreateParagraphStyle(factory.CreateRegularTextStyle(12)));
        layout.AddTextBlock(text);
        PdfTableBlock table = new PdfTableBlock(new double[] { 50 });
        table.AddRow().AddBlock(new PdfLayoutFactory().CreateFixedBlock(10, 10));
        layout.AddTableBlock(table);

        Assert.That(text.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(22));
        Assert.That(table.Measure(new PdfLayoutContext(100, 100)).Size.Height, Is.EqualTo(16));
    }

    [Test]
    public void UnitConverterReturnsPdfPointsForSupportedPhysicalUnits()
    {
        PdfUnitConverter units = new PdfStyleFactory().CreateUnitConverter();

        Assert.That(units.Points(12), Is.EqualTo(12));
        Assert.That(units.Inches(1), Is.EqualTo(72));
        Assert.That(units.Millimetres(25.4), Is.EqualTo(72).Within(0.0000001));
        Assert.That(units.Centimetres(2.54), Is.EqualTo(72).Within(0.0000001));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.Points(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.Inches(double.PositiveInfinity));
    }

    [Test]
    public void ReusableStyleValidationRejectsInvalidOpacityDashAndSpacing()
    {
        PdfElementStyle style = new PdfElementStyle();
        PdfBlock block = new PdfBlock();
        Assert.Throws<ArgumentNullException>(() => block.SetStyle(null!));

        style.BackgroundOpacity = 2;
        Assert.Throws<ArgumentOutOfRangeException>(() => block.SetStyle(style));
        style.BackgroundOpacity = 1;
        style.BorderDashPattern = new double[] { 0, 0 };
        Assert.Throws<ArgumentException>(() => block.SetStyle(style));
        style.BorderDashPattern = new double[] { 2, -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => block.SetStyle(style));
        style.BorderDashPattern = new double[0];
        PdfInsets invalidPadding = new PdfInsets(0, 0, 0, 0);
        invalidPadding.Left = double.NaN;
        style.Padding = invalidPadding;
        Assert.Throws<ArgumentOutOfRangeException>(() => block.SetStyle(style));
        style.Padding = new PdfInsets(0, 0, 0, 0);
        style.BorderColor = new PdfColor("UNKNOWN", 0, 0, 0, 0);
        Assert.Throws<ArgumentException>(() => block.SetStyle(style));

        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfStackBlock().SetSpacing(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfRowBlock().SetSpacing(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfColumnBlock().SetSpacing(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfGridBlock().SetGaps(0, -1));
        PdfLayoutDocument layout = new PdfLayoutDocument(new PdfDocument(), new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0));
        Assert.Throws<ArgumentNullException>(() => layout.SetTheme(null!));
    }

    static void ConfigureCellStyle(PdfElementStyle style, double padding, double gray)
    {
        style.Padding = new PdfInsets(padding, padding, padding, padding);
        style.HasBackground = true;
        style.BackgroundColor = new PdfColor("GRAY", gray, 0, 0, 0);
    }
}
