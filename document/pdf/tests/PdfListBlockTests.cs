using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfListBlockTests
{
    [Test]
    public void ListBlock_MeasuresItemsWithMarkersSpacingAndInsets()
    {
        PdfListBlock list = new PdfListBlock();
        PdfLayoutContext context = new PdfLayoutContext(100, 80);

        list.SetMarkerWidth(10);
        list.SetMarkerGap(5);
        list.SetSpacing(3);
        list.SetPadding(PdfInsets.Uniform(2));
        list.SetBorder(PdfInsets.Uniform(1));
        list.SetMargin(PdfInsets.Uniform(4));
        list.AddBlock(PdfBlock.WithSize(20, 10));
        list.AddBlock(PdfBlock.WithSize(30, 15));
        PdfLayoutResult result = list.Measure(context);

        Assert.That(result.Fits, Is.True);
        Assert.That(list.Items.Count, Is.EqualTo(2));
        Assert.That(result.Box.ContentRectangle.Width, Is.EqualTo(45));
        Assert.That(result.Box.ContentRectangle.Height, Is.EqualTo(30));
        Assert.That(result.Size.Width, Is.EqualTo(59));
        Assert.That(result.Size.Height, Is.EqualTo(44));
    }

    [Test]
    public void ListBlock_DrawsNumberedMarkersAndIndentedContent()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfListBlock list = PdfListBlock.WithMarkerStyle(PdfListMarkerStyle.Numbered);
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 80, 80);

        list.SetMarkerTextStyle(textStyle);
        list.SetMarkerWidth(14);
        list.SetMarkerGap(4);
        list.SetStartNumber(3);
        list.AddTextBlock(new PdfTextBlock("one", paragraphStyle));
        list.AddTextBlock(new PdfTextBlock("two", paragraphStyle));
        PdfLayoutResult result = list.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Width, Is.EqualTo(80));
        Assert.That(result.Size.Height, Is.EqualTo(24));
        Assert.That(content, Does.Contain("10 34 Td"));
        Assert.That(content, Does.Contain("(3.) Tj"));
        Assert.That(content, Does.Contain("28 34 Td"));
        Assert.That(content, Does.Contain("(one) Tj"));
        Assert.That(content, Does.Contain("10 22 Td"));
        Assert.That(content, Does.Contain("(4.) Tj"));
        Assert.That(content, Does.Contain("28 22 Td"));
        Assert.That(content, Does.Contain("(two) Tj"));
    }

    [Test]
    public void ListBlock_DrawsBulletMarkers()
    {
        PdfPage page = new PdfPage(200, 120);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfListBlock list = new PdfListBlock();
        PdfLayoutDrawContext context = new PdfLayoutDrawContext(page, 10, 20, 80, 80);

        list.SetMarkerTextStyle(textStyle);
        list.AddTextBlock(new PdfTextBlock("item", paragraphStyle));
        list.Draw(context);

        string content = page.Canvas.ToPdfString();
        Assert.That(content, Does.Contain("(*) Tj"));
        Assert.That(content, Does.Contain("(item) Tj"));
    }

    [Test]
    public void ListBlock_NumberedMarkersCanContinuePastTheIntegerLimit()
    {
        PdfListBlock list = PdfListBlock.WithMarkerStyle(PdfListMarkerStyle.Numbered);
        list.SetStartNumber(int.MaxValue);

        Assert.That(list.GetMarkerText(0), Is.EqualTo("2147483647."));
        Assert.That(list.GetMarkerText(1), Is.EqualTo("2147483648."));
    }

    [Test]
    public void LayoutDocument_RendersListBlocksAndPaginatesThemAsAtomicItems()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfListBlock list = new PdfListBlock();

        list.SetMarkerTextStyle(textStyle);
        list.AddTextBlock(new PdfTextBlock("next", paragraphStyle));
        layout.AddBlock(PdfBlock.WithSize(50, 40));
        layout.AddListBlock(list);
        PdfPageCursor cursor = layout.Render();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(next) Tj"));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
    }

    [Test]
    public void ListBlock_ReportsNonFittingItems()
    {
        PdfListBlock list = new PdfListBlock();
        PdfLayoutContext context = new PdfLayoutContext(80, 20);

        list.AddBlock(PdfBlock.WithSize(20, 30));
        PdfLayoutResult result = list.Measure(context);

        Assert.That(result.Fits, Is.False);
    }

    [Test]
    public void ListBlocks_RejectInvalidInputs()
    {
        PdfListBlock list = new PdfListBlock();
        PdfPageCursor cursor = PdfPageCursor.Create(new PdfDocument());
        PdfLayoutDocument layout = PdfLayoutDocument.Default(new PdfDocument());

        Assert.Throws<ArgumentException>(() => PdfListBlock.WithMarkerStyle((PdfListMarkerStyle)99));
        Assert.Throws<ArgumentNullException>(() => new PdfListItem(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddItem(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddTextBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddStackBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddRowBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddColumnBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddGridBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddTableBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.AddListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => list.SetPadding(null!));
        Assert.Throws<ArgumentNullException>(() => list.SetBorder(null!));
        Assert.Throws<ArgumentNullException>(() => list.SetMargin(null!));
        Assert.Throws<ArgumentNullException>(() => list.SetMarkerTextStyle(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetSpacing(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetSpacing(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetMarkerWidth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetMarkerWidth(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetMarkerGap(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetMarkerGap(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetStartNumber(0));
        Assert.Throws<ArgumentNullException>(() => list.Measure(null!));
        Assert.Throws<ArgumentNullException>(() => list.Draw(null!));
        Assert.Throws<ArgumentException>(() => new PdfListBlock().Measure(new PdfLayoutContext(0, 10)));
        Assert.Throws<ArgumentException>(() => MeasureOverfullListMarkerArea());
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.DrawListBlock(null!));
        Assert.Throws<ArgumentNullException>(() => layout.AddListBlock(null!));
    }

    static void MeasureOverfullListMarkerArea()
    {
        PdfListBlock list = new PdfListBlock();
        list.SetMarkerWidth(8);
        list.SetMarkerGap(3);
        list.Measure(new PdfLayoutContext(10, 20));
    }
}
