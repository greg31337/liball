using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTableBlockTests
{
    [Test]
    public void TableBlock_MeasuresRowsWithFixedColumnWidthsAndInsets()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 30, 50 });
        PdfTableRow first = table.AddRow();
        PdfTableRow second = table.AddRow();
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        table.SetPadding(PdfInsets.Uniform(2));
        table.SetBorder(PdfInsets.Uniform(1));
        table.SetMargin(PdfInsets.Uniform(4));
        first.AddBlock(PdfBlock.WithSize(20, 10));
        first.AddBlock(PdfBlock.WithSize(40, 15));
        second.AddBlock(PdfBlock.WithSize(25, 25));
        PdfLayoutResult result = table.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(2));
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(80));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(40));
        Assert.That(result.Size.Width, Is.EqualTo(94));
        Assert.That(result.Size.Height, Is.EqualTo(54));
    }

    [Test]
    public void TableBlock_CopiesColumnWidths()
    {
        double[] widths = new double[] { 30, 50 };
        PdfTableBlock table = new PdfTableBlock(widths);
        PdfTableRow row = table.AddRow();
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        widths[0] = 90;
        row.AddBlock(PdfBlock.WithSize(20, 10));
        PdfLayoutResult result = table.Measure(context);

        Assert.That(table.ColumnWidths[0], Is.EqualTo(30));
        Assert.That(result.Size.Width, Is.EqualTo(80));
    }

    [Test]
    public void TableBlock_DrawPlacesTextCellsAtFixedColumnOffsets()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 40, 50 });
        PdfTableRow row = table.AddRow();
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 90, 80);

        row.AddTextBlock(new PdfTextBlock("left", paragraphStyle));
        row.AddTextBlock(new PdfTextBlock("right", paragraphStyle));
        PdfLayoutResult result = table.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Width, Is.EqualTo(90));
        Assert.That(result.Size.Height, Is.EqualTo(12));
        Assert.That(content, Does.Contain("10 22 Td"));
        Assert.That(content, Does.Contain("(left) Tj"));
        Assert.That(content, Does.Contain("50 22 Td"));
        Assert.That(content, Does.Contain("(right) Tj"));
    }

    [Test]
    public void TableCell_PaddingExpandsRowHeightAndOffsetsCellContent()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 50 });
        PdfTableRow row = table.AddRow();
        PdfTableCell cell = row.AddTextBlock(new PdfTextBlock("padded", paragraphStyle));
        PdfLayoutContext measureContext = new PdfLayoutContext(80, 80);
        PdfLayoutDrawContext drawContext = new PdfLayoutDrawContext(page, 10, 20, 50, 80);

        cell.SetPadding(PdfInsets.Uniform(4));
        PdfLayoutResult measureResult = table.Measure(measureContext);
        PdfLayoutResult drawResult = table.Draw(drawContext);

        string content = page.Canvas.ToPdfString();
        Assert.That(row.Cells[0], Is.SameAs(cell));
        Assert.That(measureResult.Size.Width, Is.EqualTo(50));
        Assert.That(measureResult.Size.Height, Is.EqualTo(20));
        Assert.That(drawResult.Size.Height, Is.EqualTo(20));
        Assert.That(content, Does.Contain("14 26 Td"));
        Assert.That(content, Does.Contain("(padded) Tj"));
    }

    [Test]
    public void TableCell_DrawsBackgroundAndBorderAcrossFullRowHeight()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 50 });
        PdfTableRow row = table.AddRow();
        PdfTableCell cell = row.AddTextBlock(new PdfTextBlock("boxed", paragraphStyle));
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 50, 80);

        cell.SetBackground(PdfColor.Gray(0.8));
        cell.SetBorder(PdfInsets.Uniform(2));
        cell.SetBorderColor(PdfColor.Gray(0.25));
        PdfLayoutResult result = table.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Height, Is.EqualTo(16));
        Assert.That(content, Does.Contain("0.8 g"));
        Assert.That(content, Does.Contain("10 20 50 16 re f"));
        Assert.That(content, Does.Contain("0.25 g"));
        Assert.That(content, Does.Contain("10 20 50 2 re f"));
        Assert.That(content, Does.Contain("10 34 50 2 re f"));
        Assert.That(content, Does.Contain("10 20 2 16 re f"));
        Assert.That(content, Does.Contain("58 20 2 16 re f"));
        Assert.That(content, Does.Contain("12 24 Td"));
        Assert.That(content, Does.Contain("(boxed) Tj"));
    }

    [Test]
    public void TableCell_ClearBackgroundStopsBackgroundDrawing()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTableBlock table = new PdfTableBlock(new double[] { 50 });
        PdfTableRow row = table.AddRow();
        PdfTableCell cell = row.AddBlock(PdfBlock.WithSize(20, 10));
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 50, 80);

        cell.SetBackground(PdfColor.Gray(0.8));
        cell.ClearBackground();
        table.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(cell.HasBackground, Is.False);
        Assert.That(content, Does.Not.Contain("0.8 g"));
        Assert.That(content, Does.Not.Contain("10 20 50 10 re f"));
    }

    [Test]
    public void TableBlock_AllowsRowsWithTrailingEmptyCells()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 30, 50, 20 });
        PdfTableRow row = table.AddRow();
        PdfLayoutContext context = new PdfLayoutContext(120, 80);

        row.AddBlock(PdfBlock.WithSize(25, 10));
        PdfLayoutResult result = table.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(100));
        Assert.That(result.Size.Height, Is.EqualTo(10));
    }

    [Test]
    public void TableBlock_MeasuresHeaderAndFooterRowsWithBodyRows()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 40 });
        PdfTableRow header = table.AddHeaderRow();
        PdfTableRow body = table.AddRow();
        PdfTableRow footer = table.AddFooterRow();
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        header.AddBlock(PdfBlock.WithSize(20, 8));
        body.AddBlock(PdfBlock.WithSize(20, 10));
        footer.AddBlock(PdfBlock.WithSize(20, 6));
        PdfLayoutResult result = table.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(table.GetHeaderRowCount(), Is.EqualTo(1));
        Assert.That(table.GetFooterRowCount(), Is.EqualTo(1));
        Assert.That(result.Size.Width, Is.EqualTo(40));
        Assert.That(result.Size.Height, Is.EqualTo(24));
    }

    [Test]
    public void TableBlock_DrawRowsIncludesHeadersAndFootersAroundSelectedBodyRows()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 60 });
        PdfTableRow header = table.AddHeaderRow();
        PdfTableRow first = table.AddRow();
        PdfTableRow second = table.AddRow();
        PdfTableRow footer = table.AddFooterRow();
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 60, 80);

        header.AddTextBlock(new PdfTextBlock("head", paragraphStyle));
        first.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        second.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        footer.AddTextBlock(new PdfTextBlock("foot", paragraphStyle));
        PdfLayoutResult result = table.DrawRows(context, 1, 1);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Height, Is.EqualTo(36));
        Assert.That(content, Does.Contain("10 46 Td"));
        Assert.That(content, Does.Contain("(head) Tj"));
        Assert.That(content, Does.Not.Contain("(first) Tj"));
        Assert.That(content, Does.Contain("10 34 Td"));
        Assert.That(content, Does.Contain("(second) Tj"));
        Assert.That(content, Does.Contain("10 22 Td"));
        Assert.That(content, Does.Contain("(foot) Tj"));
    }

    [Test]
    public void TableBlock_MeasuresAndDrawsRowRanges()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 40 });
        PdfTableRow first = table.AddRow();
        PdfTableRow second = table.AddRow();
        PdfTableRow third = table.AddRow();
        PdfLayoutContext measureContext = new PdfLayoutContext(100, 80);
        PdfLayoutDrawContext drawContext = new PdfLayoutDrawContext(page, 10, 20, 40, 80);

        first.AddBlock(PdfBlock.WithSize(20, 10));
        second.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        third.AddBlock(PdfBlock.WithSize(20, 15));
        PdfLayoutResult measureResult = table.MeasureRows(measureContext, 1, 2);
        PdfLayoutResult drawResult = table.DrawRows(drawContext, 1, 1);

        string content = page.Canvas.ToPdfString();
        Assert.That(measureResult.Size.Width, Is.EqualTo(40));
        Assert.That(measureResult.Size.Height, Is.EqualTo(27));
        Assert.That(drawResult.Size.Height, Is.EqualTo(12));
        Assert.That(content, Does.Contain("(second) Tj"));
    }

    [Test]
    public void LayoutDocument_StartsTableOnNextPageWhenFirstRowDoesNotFitRemainingSpace()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 30, 40 });
        PdfTableRow row = table.AddRow();

        layout.AddBlock(PdfBlock.WithSize(50, 40));
        row.AddTextBlock(new PdfTextBlock("name", paragraphStyle));
        row.AddTextBlock(new PdfTextBlock("value", paragraphStyle));
        layout.AddTableBlock(table);
        PdfPageCursor cursor = layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(name) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(value) Tj"));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
    }

    [Test]
    public void LayoutDocument_SplitsTableRowsAcrossPages()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 70 });

        AddTextRow(table, "row1", paragraphStyle);
        AddTextRow(table, "row2", paragraphStyle);
        AddTextRow(table, "row3", paragraphStyle);
        AddTextRow(table, "row4", paragraphStyle);
        AddTextRow(table, "row5", paragraphStyle);
        layout.AddTableBlock(table);
        PdfPageCursor cursor = layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(firstPage, Does.Contain("(row1) Tj"));
        Assert.That(firstPage, Does.Contain("(row4) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(row5) Tj"));
        Assert.That(secondPage, Does.Contain("(row5) Tj"));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
    }

    [Test]
    public void LayoutDocument_RepeatsTableHeaderAndFooterRowsAcrossPages()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 70 });
        PdfTableRow header = table.AddHeaderRow();
        PdfTableRow footer = table.AddFooterRow();

        header.AddTextBlock(new PdfTextBlock("head", paragraphStyle));
        footer.AddTextBlock(new PdfTextBlock("foot", paragraphStyle));
        AddTextRow(table, "row1", paragraphStyle);
        AddTextRow(table, "row2", paragraphStyle);
        AddTextRow(table, "row3", paragraphStyle);
        AddTextRow(table, "row4", paragraphStyle);
        AddTextRow(table, "row5", paragraphStyle);
        layout.AddTableBlock(table);
        PdfPageCursor cursor = layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        string thirdPage = document.Pages[2].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(3));
        Assert.That(firstPage, Does.Contain("(head) Tj"));
        Assert.That(firstPage, Does.Contain("(foot) Tj"));
        Assert.That(firstPage, Does.Contain("(row1) Tj"));
        Assert.That(firstPage, Does.Contain("(row2) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(row3) Tj"));
        Assert.That(secondPage, Does.Contain("(head) Tj"));
        Assert.That(secondPage, Does.Contain("(foot) Tj"));
        Assert.That(secondPage, Does.Contain("(row3) Tj"));
        Assert.That(secondPage, Does.Contain("(row4) Tj"));
        Assert.That(thirdPage, Does.Contain("(head) Tj"));
        Assert.That(thirdPage, Does.Contain("(foot) Tj"));
        Assert.That(thirdPage, Does.Contain("(row5) Tj"));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[2]));
        Assert.That(cursor.CurrentY, Is.EqualTo(19));
    }

    [Test]
    public void PageCursor_ThrowsLayoutOverflowForOversizedUnsplittableTableRow()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTableBlock table = new PdfTableBlock(new double[] { 70 });
        PdfTableRow row = table.AddRow();

        row.AddBlock(PdfBlock.WithSize(50, 80));
        PdfLayoutOverflowException exception = Assert.Throws<PdfLayoutOverflowException>(() => cursor.DrawTableBlock(table))!;

        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
        Assert.That(exception.Element, Is.SameAs(table));
        Assert.That(exception.ElementType, Is.EqualTo("PdfTableBlock"));
        Assert.That(exception.RequiredHeight, Is.EqualTo(80));
        Assert.That(exception.AvailableHeight, Is.EqualTo(50));
    }

    [Test]
    public void TableBlock_ReportsNonFittingWidthOrCellContent()
    {
        PdfTableBlock wideTable = new PdfTableBlock(new double[] { 70, 40 });
        PdfTableBlock tightTable = new PdfTableBlock(new double[] { 30 });
        PdfTableRow wideRow = wideTable.AddRow();
        PdfTableRow tightRow = tightTable.AddRow();
        PdfLayoutContext context = new PdfLayoutContext(100, 40);

        wideRow.AddBlock(PdfBlock.WithSize(20, 10));
        tightRow.AddBlock(PdfBlock.WithSize(40, 10));

        Assert.That(wideTable.Measure(context).Fits, Is.False);
        Assert.That(tightTable.Measure(context).Fits, Is.False);
    }

    [Test]
    public void TableBlocks_RejectInvalidInputs()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 20, 30 });
        PdfTableRow row = new PdfTableRow();
        PdfPageCursor cursor = PdfPageCursor.Create(new PdfDocument());
        PdfLayoutDocument layout = PdfLayoutDocument.Default(new PdfDocument());

        Assert.Throws<ArgumentNullException>(() => new PdfTableBlock(null!));
        Assert.Throws<ArgumentException>(() => new PdfTableBlock(new double[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfTableBlock(new double[] { 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfTableBlock(new double[] { 20, -1 }));
        Assert.Throws<ArgumentNullException>(() => table.AddExistingHeaderRow(null!));
        Assert.Throws<ArgumentNullException>(() => table.AddExistingRow(null!));
        Assert.Throws<ArgumentNullException>(() => table.AddExistingFooterRow(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetPadding(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetBorder(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetMargin(null!));
        Assert.Throws<ArgumentNullException>(() => table.Measure(null!));
        Assert.Throws<ArgumentNullException>(() => table.Draw(null!));
        Assert.Throws<ArgumentNullException>(() => table.MeasureRows(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => table.DrawRows(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRows(new PdfLayoutContext(100, 80), -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRows(new PdfLayoutContext(100, 80), 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRows(new PdfLayoutContext(100, 80), 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRows(new PdfLayoutContext(100, 80), 0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRowHeight(-1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRowHeight(0, 10));
        table.AddRow();
        Assert.Throws<ArgumentOutOfRangeException>(() => table.MeasureRows(new PdfLayoutContext(100, 80), 1, int.MaxValue));
        Assert.Throws<ArgumentNullException>(() => row.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddCell(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(PdfLayoutItem.FromBlock(new PdfBlock())).SetPadding(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(PdfLayoutItem.FromBlock(new PdfBlock())).SetBorder(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(PdfLayoutItem.FromBlock(new PdfBlock())).SetBackground(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(PdfLayoutItem.FromBlock(new PdfBlock())).SetBorderColor(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(PdfLayoutItem.FromBlock(new PdfBlock())).Measure(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfTableCell(PdfLayoutItem.FromBlock(new PdfBlock())).Draw(null!));
        Assert.Throws<ArgumentException>(() => MeasureOverfullCell());
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddTableBlock(null!));
    }

    [Test]
    public void TableBlock_RejectsRowsWithMoreCellsThanColumns()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 20 });
        PdfTableRow row = table.AddRow();

        row.AddBlock(PdfBlock.WithSize(10, 10));
        row.AddBlock(PdfBlock.WithSize(10, 10));

        Assert.Throws<ArgumentException>(() => table.Measure(new PdfLayoutContext(100, 80)));
    }

    [Test]
    public void TableBlock_RejectsHeaderOrFooterRowsWithMoreCellsThanColumns()
    {
        PdfTableBlock headerTable = new PdfTableBlock(new double[] { 20 });
        PdfTableBlock footerTable = new PdfTableBlock(new double[] { 20 });
        PdfTableRow header = headerTable.AddHeaderRow();
        PdfTableRow footer = footerTable.AddFooterRow();

        header.AddBlock(PdfBlock.WithSize(10, 10));
        header.AddBlock(PdfBlock.WithSize(10, 10));
        footer.AddBlock(PdfBlock.WithSize(10, 10));
        footer.AddBlock(PdfBlock.WithSize(10, 10));

        Assert.Throws<ArgumentException>(() => headerTable.Measure(new PdfLayoutContext(100, 80)));
        Assert.Throws<ArgumentException>(() => footerTable.Measure(new PdfLayoutContext(100, 80)));
    }

    static void AddTextRow(PdfTableBlock table, string text, PdfParagraphStyle paragraphStyle)
    {
        PdfTableRow row = table.AddRow();
        row.AddTextBlock(new PdfTextBlock(text, paragraphStyle));
    }

    static void MeasureOverfullCell()
    {
        PdfTableCell cell = new PdfTableCell(PdfLayoutItem.FromBlock(PdfBlock.WithSize(10, 10)));
        cell.SetPadding(PdfInsets.Uniform(6));
        cell.Measure(new PdfLayoutContext(10, 20));
    }
}
