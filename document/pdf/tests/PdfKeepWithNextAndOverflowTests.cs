using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfKeepWithNextAndOverflowTests
{
    [Test]
    public void KeepWithNextGroupMeasuresOrderedItemsAsOneVerticalReservation()
    {
        PdfKeepWithNextGroup group = new PdfLayoutFactory().CreateKeepWithNextGroup();
        group.AddItem(PdfBlock.WithSize(20, 8));
        group.AddItem(PdfBlock.WithSize(30, 12));

        PdfLayoutResult fitting = group.Measure(new PdfLayoutContext(40, 20));
        PdfLayoutResult tooShort = group.Measure(new PdfLayoutContext(40, 19));

        Assert.That(group.GetItemCount(), Is.EqualTo(2));
        Assert.That(fitting.Size.Width, Is.EqualTo(30));
        Assert.That(fitting.Size.Height, Is.EqualTo(20));
        Assert.That(fitting.Fits, Is.True);
        Assert.That(tooShort.Fits, Is.False);
        Assert.Throws<ArgumentNullException>(() => group.AddItem(null!));
    }

    [Test]
    public void LayoutDocumentMovesCompleteKeepWithNextGroupToNextPage()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = CreateShortLayout(document);
        PdfKeepWithNextGroup group = new PdfKeepWithNextGroup();
        group.AddItem(PdfBlock.WithSize(20, 10));
        group.AddItem(PdfBlock.WithSize(20, 10));

        layout.AddBlock(PdfBlock.WithSize(20, 35));
        layout.AddKeepWithNextGroup(group);
        PdfPageCursor cursor = layout.Render();

        Assert.That(document.GetPageCount(), Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.GetPage(1)));
        Assert.That(cursor.CurrentY, Is.EqualTo(35));
    }

    [Test]
    public void KeepWithNextGroupFallsBackToItemPaginationWhenTallerThanFreshPage()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = CreateShortLayout(document);
        PdfKeepWithNextGroup group = new PdfKeepWithNextGroup();
        group.AddItem(PdfBlock.WithSize(20, 30));
        group.AddItem(PdfBlock.WithSize(20, 30));

        layout.AddKeepWithNextGroup(group);
        PdfPageCursor cursor = layout.Render();

        Assert.That(document.GetPageCount(), Is.EqualTo(2));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.GetPage(1)));
        Assert.That(cursor.CurrentY, Is.EqualTo(25));
    }

    [Test]
    public void WidthOverflowReportsTheSelectedFlowAreaWithoutDrawing()
    {
        PdfDocument document = new PdfDocument();
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfBlock block = PdfBlock.WithSize(90, 10);

        PdfLayoutOverflowException exception = Assert.Throws<PdfLayoutOverflowException>(() => cursor.DrawBlock(block))!;

        Assert.That(exception.Element, Is.SameAs(block));
        Assert.That(exception.RequiredWidth, Is.EqualTo(90));
        Assert.That(exception.AvailableWidth, Is.EqualTo(80));
        Assert.That(exception.AvailableHeight, Is.EqualTo(50));
        Assert.That(cursor.CurrentY, Is.EqualTo(55));
        Assert.That(document.GetPage(0).GetCanvas().ToPdfString(), Is.Empty);
    }

    static PdfLayoutDocument CreateShortLayout(PdfDocument document)
    {
        return new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
    }
}
