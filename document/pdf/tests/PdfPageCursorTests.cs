using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageCursorTests
{
    [Test]
    public void Cursor_CreatesFirstPageWithConfiguredSizeAndMargins()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(cursor.CurrentPage.Width, Is.EqualTo(100));
        Assert.That(cursor.CurrentPage.Height, Is.EqualTo(60));
        Assert.That(cursor.GetAvailableWidth(), Is.EqualTo(80));
        Assert.That(cursor.GetRemainingHeight(), Is.EqualTo(50));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
    }

    [Test]
    public void Cursor_DrawBlockAdvancesCurrentYWithoutWritingContent()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfBlock block = PdfBlock.WithSize(50, 20);

        PdfLayoutResult result = cursor.DrawBlock(block);

        Assert.That(result.Fits, Is.True);
        Assert.That(cursor.CurrentY, Is.EqualTo(35));
        Assert.That(cursor.HasContentOnPage, Is.True);
        Assert.That(cursor.CurrentPage.Canvas.ToPdfString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Cursor_CreatesNewPageWhenNextBlockDoesNotFitRemainingSpace()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        cursor.DrawBlock(PdfBlock.WithSize(50, 35));
        PdfLayoutResult result = cursor.DrawBlock(PdfBlock.WithSize(50, 30));

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(result.Fits, Is.True);
        Assert.That(cursor.CurrentY, Is.EqualTo(25));
    }

    [Test]
    public void Cursor_ThrowsLayoutOverflowBeforeDrawingOversizedBlock()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfBlock block = PdfBlock.WithSize(50, 80);

        PdfLayoutOverflowException exception = Assert.Throws<PdfLayoutOverflowException>(() => cursor.DrawBlock(block))!;

        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
        Assert.That(cursor.HasContentOnPage, Is.False);
        Assert.That(exception.Element, Is.SameAs(block));
        Assert.That(exception.ElementType, Is.EqualTo("PdfBlock"));
        Assert.That(exception.RequiredWidth, Is.EqualTo(50));
        Assert.That(exception.RequiredHeight, Is.EqualTo(80));
        Assert.That(exception.AvailableWidth, Is.EqualTo(80));
        Assert.That(exception.AvailableHeight, Is.EqualTo(50));
        Assert.That(exception.PageNumber, Is.EqualTo(1));
        Assert.That(exception.ColumnNumber, Is.EqualTo(1));
        Assert.That(exception.Message, Does.Contain("page 1, column 1"));
    }

    [Test]
    public void Cursor_DrawTextBlockWritesToCurrentPageAndAdvances()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTextBlock block = new PdfTextBlock("hello", paragraphStyle);

        PdfLayoutResult result = cursor.DrawTextBlock(block);

        string content = cursor.CurrentPage.Canvas.ToPdfString();
        Assert.That(result.Fits, Is.True);
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
        Assert.That(content, Does.Contain("10 45 Td"));
        Assert.That(content, Does.Contain("(hello) Tj"));
    }

    [Test]
    public void Cursor_DebugOverlayDrawsMeasuredBoxAfterItem()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        cursor.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), 0.5);
        cursor.DrawBlock(PdfBlock.WithSize(50, 20));

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("1 0 0 RG"));
        Assert.That(content, Does.Contain("0.5 w"));
        Assert.That(content, Does.Contain("10 35 50 20 re S"));
        Assert.That(cursor.CurrentY, Is.EqualTo(35));
    }

    [Test]
    public void Cursor_DisableDebugOverlayStopsOverlayDrawing()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        cursor.EnableDebugOverlay();
        cursor.DisableDebugOverlay();
        cursor.DrawBlock(PdfBlock.WithSize(50, 20));

        Assert.That(document.Pages[0].Canvas.ToPdfString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Cursor_DrawPageBreakBlockCreatesNewPageWithoutWritingContent()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        cursor.DrawBlock(PdfBlock.WithSize(50, 20));
        PdfLayoutResult result = cursor.DrawPageBreakBlock(new PdfPageBreakBlock());

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(0));
        Assert.That(result.Size.Height, Is.EqualTo(0));
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
        Assert.That(cursor.HasContentOnPage, Is.False);
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Is.EqualTo(string.Empty));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Cursor_EnsureSpaceCreatesNewPageWhenRemainingSpaceIsTooSmall()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        cursor.DrawBlock(PdfBlock.WithSize(50, 45));
        cursor.EnsureSpace(12);

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
        Assert.That(cursor.HasContentOnPage, Is.False);
    }

    [Test]
    public void Cursor_EnsureSpaceDoesNotCreateEmptyPageForOversizedSpace()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        cursor.EnsureSpace(80);

        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[0]));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
    }

    [Test]
    public void LayoutDocument_RendersItemsAndCreatesPagesAutomatically()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        layout.AddBlock(PdfBlock.WithSize(50, 30));
        layout.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        PdfPageCursor cursor = layout.Render();

        Assert.That(layout.Items.Count, Is.EqualTo(3));
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(first) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(second) Tj"));
    }

    [Test]
    public void LayoutDocument_DebugOverlayDrawsBoxesWithoutChangingPagination()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.EnableDebugOverlayWithStyle(PdfColor.Rgb(0, 1, 0), 0.75);
        layout.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        layout.AddBlock(PdfBlock.WithSize(50, 30));
        layout.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        PdfPageCursor cursor = layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(firstPage, Does.Contain("0 1 0 RG"));
        Assert.That(firstPage, Does.Contain("0.75 w"));
        Assert.That(firstPage, Does.Contain("10 43 80 12 re S"));
        Assert.That(firstPage, Does.Contain("10 13 50 30 re S"));
        Assert.That(secondPage, Does.Contain("(second) Tj"));
        Assert.That(secondPage, Does.Contain("10 43 80 12 re S"));
    }

    [Test]
    public void LayoutDocument_DebugOverlayDrawsBoxesForSplitTableChunks()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 80 });

        AddTextRow(table, "row1", paragraphStyle);
        AddTextRow(table, "row2", paragraphStyle);
        AddTextRow(table, "row3", paragraphStyle);
        AddTextRow(table, "row4", paragraphStyle);
        AddTextRow(table, "row5", paragraphStyle);
        layout.EnableDebugOverlayWithStyle(PdfColor.Rgb(0, 0, 1), 0.25);
        layout.AddTableBlock(table);
        layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("10 7 80 48 re S"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("10 43 80 12 re S"));
    }

    [Test]
    public void LayoutDocument_RendersManualPageBreakBetweenItems()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        PdfPageCursor cursor = layout.Render();

        Assert.That(layout.Items.Count, Is.EqualTo(3));
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(first) Tj"));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Not.Contain("(second) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(second) Tj"));
    }

    [Test]
    public void LayoutDocument_EnsureSpaceMovesFollowingContentToNextPage()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.AddBlock(PdfBlock.WithSize(50, 45));
        layout.AddEnsureSpace(12);
        layout.AddTextBlock(new PdfTextBlock("next", paragraphStyle));
        layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Not.Contain("(next) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(next) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("10 45 Td"));
    }

    [Test]
    public void LayoutDocument_KeepTogetherMovesSplitCapableItemBeforeDrawing()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfTableBlock table = new PdfTableBlock(new double[] { 80 });

        AddTextRow(table, "row1", paragraphStyle);
        AddTextRow(table, "row2", paragraphStyle);
        layout.AddBlock(PdfBlock.WithSize(50, 35));
        layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(table));
        layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Not.Contain("(row1) Tj"));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Not.Contain("(row2) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(row1) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(row2) Tj"));
    }

    [Test]
    public void LayoutItem_CanToggleKeepTogether()
    {
        PdfLayoutItem item = PdfLayoutItem.FromBlock(PdfBlock.WithSize(10, 10));

        item.SetKeepTogether();
        Assert.That(item.KeepTogether, Is.True);

        item.ClearKeepTogether();
        Assert.That(item.KeepTogether, Is.False);
    }

    [Test]
    public void LayoutDocument_RenderCreatesPageEvenWhenEmpty()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));

        PdfPageCursor cursor = layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[0]));
    }

    [Test]
    public void PageCursorAndLayoutDocument_RejectInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document);
        PdfLayoutDocument layout = PdfLayoutDocument.Default(document);

        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(null!));
        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(document, null!, new PdfPageMargins(0, 0, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(document, new PdfPageSizeFactory().Letter(), null!));
        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(0, 0, 0, 0), true, null!, false, new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0)));
        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(0, 0, 0, 0), false, new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0), true, null!));
        Assert.Throws<ArgumentNullException>(() => cursor.GetDrawContext(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawPageBreakBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawEnsureSpaceBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawKeepTogetherItem(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => cursor.Advance(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cursor.Advance(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => cursor.EnsureSpace(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cursor.EnsureSpace(double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => cursor.EnableDebugOverlayWithStyle(null!, 0.25));
        Assert.Throws<ArgumentOutOfRangeException>(() => cursor.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => cursor.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfEnsureSpaceBlock(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfEnsureSpaceBlock(double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromPageBreakBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromEnsureSpaceBlock(null!));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutDocument.Default(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfLayoutDocument(document, null!, new PdfPageMargins(0, 0, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => new PdfLayoutDocument(document, new PdfPageSizeFactory().Letter(), null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddPageBreakBlock(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.AddEnsureSpace(-1));
        Assert.Throws<ArgumentNullException>(() => layout.AddEnsureSpaceBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddItem(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddKeepTogetherItem(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddKeepWithNextGroup(null!));
        Assert.Throws<ArgumentNullException>(() => layout.EnableDebugOverlayWithStyle(null!, 0.25));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), double.PositiveInfinity));
    }

    static void AddTextRow(PdfTableBlock table, string text, PdfParagraphStyle paragraphStyle)
    {
        PdfTableRow row = table.AddRow();
        row.AddTextBlock(new PdfTextBlock(text, paragraphStyle));
    }
}
