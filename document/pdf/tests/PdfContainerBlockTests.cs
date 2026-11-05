using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfContainerBlockTests
{
    [Test]
    public void StackBlock_MeasuresChildrenVerticallyWithSpacingAndInsets()
    {
        PdfStackBlock stack = PdfStackBlock.WithSpacing(3);
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        stack.SetPadding(PdfInsets.Uniform(2));
        stack.SetBorder(PdfInsets.Uniform(1));
        stack.SetMargin(PdfInsets.Uniform(4));
        stack.AddBlock(PdfBlock.WithSize(40, 10));
        stack.AddBlock(PdfBlock.WithSize(60, 20));
        PdfLayoutResult result = stack.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(60));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(33));
        Assert.That(result.Size.Width, Is.EqualTo(74));
        Assert.That(result.Size.Height, Is.EqualTo(47));
    }

    [Test]
    public void RowBlock_MeasuresChildrenHorizontallyWithSpacingAndInsets()
    {
        PdfRowBlock row = PdfRowBlock.WithSpacing(5);
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        row.SetPadding(PdfInsets.Uniform(2));
        row.SetBorder(PdfInsets.Uniform(1));
        row.SetMargin(PdfInsets.Uniform(4));
        row.AddBlock(PdfBlock.WithSize(30, 10));
        row.AddBlock(PdfBlock.WithSize(40, 20));
        PdfLayoutResult result = row.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(75));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(20));
        Assert.That(result.Size.Width, Is.EqualTo(89));
        Assert.That(result.Size.Height, Is.EqualTo(34));
    }

    [Test]
    public void ColumnBlock_ConstrainsChildTextWidthAndStacksChildren()
    {
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfColumnBlock column = PdfColumnBlock.Create(46, 2);
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        column.AddTextBlock(new PdfTextBlock("one two three", paragraphStyle));
        column.AddBlock(PdfBlock.WithSize(20, 8));
        PdfLayoutResult result = column.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(46));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(34));
        Assert.That(result.Size.Width, Is.EqualTo(46));
        Assert.That(result.Size.Height, Is.EqualTo(34));
    }

    [Test]
    public void GridBlock_MeasuresItemsInRowMajorCells()
    {
        PdfGridBlock grid = PdfGridBlock.Create(3, 5, 4);
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        grid.AddBlock(PdfBlock.WithSize(20, 10));
        grid.AddBlock(PdfBlock.WithSize(20, 20));
        grid.AddBlock(PdfBlock.WithSize(20, 15));
        grid.AddBlock(PdfBlock.WithSize(20, 25));
        PdfLayoutResult result = grid.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(grid.GetRowCount(), Is.EqualTo(2));
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(100));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(49));
        Assert.That(result.Size.Width, Is.EqualTo(100));
        Assert.That(result.Size.Height, Is.EqualTo(49));
    }

    [Test]
    public void GridBlock_HandlesAColumnCountNearTheIntegerLimit()
    {
        PdfGridBlock grid = PdfGridBlock.Create(int.MaxValue);
        grid.AddBlock(PdfBlock.WithSize(20, 10));

        PdfLayoutResult result = grid.Measure(new PdfLayoutContext(100, 80));

        Assert.That(grid.GetRowCount(), Is.EqualTo(1));
        Assert.That(result.Size.Height, Is.EqualTo(10));
    }

    [Test]
    public void StackBlock_DrawPlacesTextChildrenFromTopToBottom()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfStackBlock stack = PdfStackBlock.WithSpacing(4);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 100, 80);

        stack.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        stack.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        PdfLayoutResult result = stack.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Height, Is.EqualTo(28));
        Assert.That(content, Does.Contain("10 38 Td"));
        Assert.That(content, Does.Contain("(first) Tj"));
        Assert.That(content, Does.Contain("10 22 Td"));
        Assert.That(content, Does.Contain("(second) Tj"));
    }

    [Test]
    public void RowBlock_DrawPlacesColumnTextSideBySide()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfColumnBlock left = PdfColumnBlock.Create(40);
        PdfColumnBlock right = PdfColumnBlock.Create(50);
        PdfRowBlock row = PdfRowBlock.WithSpacing(6);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 120, 80);

        left.AddTextBlock(new PdfTextBlock("left", paragraphStyle));
        right.AddTextBlock(new PdfTextBlock("right", paragraphStyle));
        row.AddColumnBlock(left);
        row.AddColumnBlock(right);
        PdfLayoutResult result = row.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Width, Is.EqualTo(96));
        Assert.That(result.Size.Height, Is.EqualTo(12));
        Assert.That(content, Does.Contain("10 22 Td"));
        Assert.That(content, Does.Contain("(left) Tj"));
        Assert.That(content, Does.Contain("56 22 Td"));
        Assert.That(content, Does.Contain("(right) Tj"));
    }

    [Test]
    public void GridBlock_DrawPlacesTextInRowsAndColumns()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfGridBlock grid = PdfGridBlock.Create(2, 6, 3);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 106, 80);

        grid.AddTextBlock(new PdfTextBlock("one", paragraphStyle));
        grid.AddTextBlock(new PdfTextBlock("two", paragraphStyle));
        grid.AddTextBlock(new PdfTextBlock("three", paragraphStyle));
        grid.AddTextBlock(new PdfTextBlock("four", paragraphStyle));
        PdfLayoutResult result = grid.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Width, Is.EqualTo(106));
        Assert.That(result.Size.Height, Is.EqualTo(27));
        Assert.That(content, Does.Contain("10 37 Td"));
        Assert.That(content, Does.Contain("(one) Tj"));
        Assert.That(content, Does.Contain("66 37 Td"));
        Assert.That(content, Does.Contain("(two) Tj"));
        Assert.That(content, Does.Contain("10 22 Td"));
        Assert.That(content, Does.Contain("(three) Tj"));
        Assert.That(content, Does.Contain("66 22 Td"));
        Assert.That(content, Does.Contain("(four) Tj"));
    }

    [Test]
    public void NestedContainerBlocks_MeasureAndDrawComplexValidComposition()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfStackBlock stack = PdfStackBlock.WithSpacing(3);
        PdfRowBlock row = PdfRowBlock.WithSpacing(5);
        PdfColumnBlock leftColumn = PdfColumnBlock.Create(40, 2);
        PdfTableBlock table = new PdfTableBlock(new double[] { 50 });
        PdfColumnBlock listColumn = PdfColumnBlock.Create(95);
        PdfListBlock list = PdfListBlock.WithMarkerStyle(PdfListMarkerStyle.Numbered);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 140, 100);

        stack.SetPadding(PdfInsets.Uniform(2));
        stack.SetBorder(PdfInsets.Uniform(1));
        stack.SetMargin(PdfInsets.Uniform(4));
        leftColumn.AddTextBlock(new PdfTextBlock("A", paragraphStyle));
        leftColumn.AddTextBlock(new PdfTextBlock("B", paragraphStyle));
        AddTextRow(table, "C", paragraphStyle);
        AddTextRow(table, "D", paragraphStyle);
        row.AddColumnBlock(leftColumn);
        row.AddTableBlock(table);
        list.SetMarkerTextStyle(textStyle);
        list.SetMarkerWidth(12);
        list.SetMarkerGap(3);
        list.SetSpacing(2);
        list.SetStartNumber(4);
        list.AddTextBlock(new PdfTextBlock("E", paragraphStyle));
        list.AddTextBlock(new PdfTextBlock("F", paragraphStyle));
        listColumn.AddListBlock(list);
        stack.AddRowBlock(row);
        stack.AddColumnBlock(listColumn);
        PdfLayoutResult result = stack.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Fits, Is.True);
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(95));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(55));
        Assert.That(result.Size.Width, Is.EqualTo(109));
        Assert.That(result.Size.Height, Is.EqualTo(69));
        Assert.That(content, Does.Contain("17 72 Td"));
        Assert.That(content, Does.Contain("(A) Tj"));
        Assert.That(content, Does.Contain("17 58 Td"));
        Assert.That(content, Does.Contain("(B) Tj"));
        Assert.That(content, Does.Contain("62 72 Td"));
        Assert.That(content, Does.Contain("(C) Tj"));
        Assert.That(content, Does.Contain("62 60 Td"));
        Assert.That(content, Does.Contain("(D) Tj"));
        Assert.That(content, Does.Contain("17 43 Td"));
        Assert.That(content, Does.Contain("(4.) Tj"));
        Assert.That(content, Does.Contain("32 43 Td"));
        Assert.That(content, Does.Contain("(E) Tj"));
        Assert.That(content, Does.Contain("17 29 Td"));
        Assert.That(content, Does.Contain("(5.) Tj"));
        Assert.That(content, Does.Contain("32 29 Td"));
        Assert.That(content, Does.Contain("(F) Tj"));
    }

    [Test]
    public void LayoutDocument_RendersContainerBlocksAndPaginatesThemAsAtomicItems()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfStackBlock stack = PdfStackBlock.WithSpacing(2);

        layout.AddBlock(PdfBlock.WithSize(50, 40));
        stack.AddTextBlock(new PdfTextBlock("next", paragraphStyle));
        layout.AddStackBlock(stack);
        PdfPageCursor cursor = layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(next) Tj"));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
    }

    [Test]
    public void LayoutDocument_RendersGridBlocksAndPaginatesThemAsAtomicItems()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfGridBlock grid = PdfGridBlock.Create(2, 4, 2);

        layout.AddBlock(PdfBlock.WithSize(50, 40));
        grid.AddTextBlock(new PdfTextBlock("left", paragraphStyle));
        grid.AddTextBlock(new PdfTextBlock("right", paragraphStyle));
        layout.AddGridBlock(grid);
        PdfPageCursor cursor = layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(left) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(right) Tj"));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
    }

    [Test]
    public void ContainerBlocks_ReportNonFittingChildren()
    {
        PdfStackBlock stack = new PdfStackBlock();
        PdfRowBlock row = new PdfRowBlock();
        PdfColumnBlock column = PdfColumnBlock.Create(120);
        PdfGridBlock grid = PdfGridBlock.Create(2, 4, 0);
        PdfLayoutContext context = new PdfLayoutContext(100, 40);

        stack.AddBlock(PdfBlock.WithSize(20, 50));
        row.AddBlock(PdfBlock.WithSize(120, 10));
        column.AddBlock(PdfBlock.WithSize(20, 10));
        grid.AddBlock(PdfBlock.WithSize(60, 10));

        Assert.That(stack.Measure(context).Fits, Is.False);
        Assert.That(row.Measure(context).Fits, Is.False);
        Assert.That(column.Measure(context).Fits, Is.False);
        Assert.That(grid.Measure(context).Fits, Is.False);
    }

    [Test]
    public void NestedContainerBlocks_RejectInvalidChildLayoutConstraints()
    {
        PdfStackBlock stack = new PdfStackBlock();
        PdfColumnBlock column = PdfColumnBlock.Create(20);
        PdfListBlock list = new PdfListBlock();
        PdfLayoutContext measureContext = new PdfLayoutContext(60, 60);
        PdfLayoutDrawContext drawContext = new PdfLayoutDrawContext(new PdfPage(100, 100), 0, 0, 60, 60);

        list.SetMarkerWidth(20);
        list.SetMarkerGap(1);
        list.AddTextBlock(PdfTextBlock.Plain("item"));
        column.AddListBlock(list);
        stack.AddColumnBlock(column);

        Assert.Throws<ArgumentException>(() => stack.Measure(measureContext));
        Assert.Throws<ArgumentException>(() => stack.Draw(drawContext));
    }

    [Test]
    public void LayoutDocument_RejectsInvalidNestedContainerDuringRender()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(80, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfRowBlock row = new PdfRowBlock();
        PdfGridBlock grid = PdfGridBlock.Create(2, 61, 0);

        grid.AddBlock(PdfBlock.WithSize(10, 10));
        row.AddGridBlock(grid);
        layout.AddRowBlock(row);

        Assert.Throws<ArgumentException>(() => layout.Render());
    }

    [Test]
    public void ContainerBlocks_RejectInvalidInputs()
    {
        PdfStackBlock stack = new PdfStackBlock();
        PdfRowBlock row = new PdfRowBlock();
        PdfColumnBlock column = new PdfColumnBlock();
        PdfGridBlock grid = new PdfGridBlock();
        PdfPageCursor cursor = PdfPageCursor.Create(new PdfDocument());
        PdfLayoutDocument layout = PdfLayoutDocument.Default(new PdfDocument());

        Assert.Throws<ArgumentOutOfRangeException>(() => PdfStackBlock.WithSpacing(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfStackBlock.WithSpacing(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfRowBlock.WithSpacing(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfRowBlock.WithSpacing(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColumnBlock.Create(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColumnBlock.Create(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColumnBlock.Create(10, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfColumnBlock.Create(10, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfGridBlock.Create(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfGridBlock.Create(2, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfGridBlock.Create(2, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfGridBlock.Create(2, double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfGridBlock.Create(2, 0, double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => stack.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.AddListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => column.AddListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => stack.SetPadding(null!));
        Assert.Throws<ArgumentNullException>(() => row.SetBorder(null!));
        Assert.Throws<ArgumentNullException>(() => column.SetMargin(null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetPadding(null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetBorder(null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetMargin(null!));
        Assert.Throws<ArgumentNullException>(() => stack.Measure(null!));
        Assert.Throws<ArgumentNullException>(() => row.Draw(null!));
        Assert.Throws<ArgumentNullException>(() => column.Measure(null!));
        Assert.Throws<ArgumentNullException>(() => grid.Draw(null!));
        Assert.Throws<ArgumentException>(() => new PdfStackBlock().Measure(new PdfLayoutContext(0, 10)));
        Assert.Throws<ArgumentException>(() => new PdfRowBlock().Measure(new PdfLayoutContext(0, 10)));
        Assert.Throws<ArgumentException>(() => new PdfColumnBlock().Measure(new PdfLayoutContext(0, 10)));
        Assert.Throws<ArgumentException>(() => new PdfGridBlock().Measure(new PdfLayoutContext(0, 10)));
        Assert.Throws<ArgumentException>(() => PdfGridBlock.Create(2, 20, 0).Measure(new PdfLayoutContext(10, 10)));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddGridBlock(null!));
    }

    static void AddTextRow(PdfTableBlock table, string text, PdfParagraphStyle paragraphStyle)
    {
        PdfTableRow row = table.AddRow();
        row.AddTextBlock(new PdfTextBlock(text, paragraphStyle));
    }
}
