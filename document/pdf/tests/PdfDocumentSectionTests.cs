using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfDocumentSectionTests
{
    [Test]
    public void SectionsApplyIndependentPageSizeOrientationAndMargins()
    {
        PdfDocument document = new PdfDocument();
        IPdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(300, 400), new PdfPageMargins(0, 0, 0, 0));
        PdfPageDimensions firstSize = new PdfPageDimensions(200, 100);
        PdfPageMargins firstMargins = new PdfPageMargins(10, 20, 15, 25);
        PdfDocumentSection first = layout.AddSection(firstSize, firstMargins);
        first.SetOrientation(PdfPageOrientation.Portrait);
        first.AddBlock(new PdfLayoutFactory().CreateFixedBlock(20, 20));
        PdfDocumentSection second = layout.AddSection(new PdfPageDimensions(100, 200), new PdfPageMargins(20, 5, 30, 15));
        second.SetOrientation(PdfPageOrientation.Landscape);
        second.AddBlock(new PdfLayoutFactory().CreateFixedBlock(20, 20));
        firstSize.Width = 900;
        firstMargins.Left = 90;

        layout.Render();

        Assert.That(document.GetPageCount(), Is.EqualTo(2));
        AssertDimensions(document.GetPage(0).GetDimensions(), 100, 200);
        AssertRectangle(document.GetPage(0).GetContentArea(), 10, 25, 75, 155);
        AssertDimensions(document.GetPage(1).GetDimensions(), 200, 100);
        AssertRectangle(document.GetPage(1).GetContentArea(), 20, 15, 150, 80);
    }

    [Test]
    public void SectionNumberRestartSelectsFirstOddEvenTemplatesAndRunningHead()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = CreateSectionLayout(document);
        PdfDocumentSection section = layout.AddSection(new PdfPageDimensions(100, 70), new PdfPageMargins(10, 5, 10, 5));
        PdfParagraphStyle style = CreateParagraphStyle();
        PdfLayoutFactory factory = new PdfLayoutFactory();
        section.SetPageNumberStart(5);
        section.SetRunningHead("A");
        section.SetFirstPageHeaderTemplate(factory.CreatePageTextTemplate("first {running} {page}/{total}", style, 12));
        section.SetFirstPageFooterTemplate(factory.CreatePageTextTemplate("first-foot {page}", style, 12));
        section.SetOddPageHeaderTemplate(factory.CreatePageTextTemplate("odd {running} {page}/{total}", style, 12));
        section.SetOddPageFooterTemplate(factory.CreatePageTextTemplate("odd-foot {page}", style, 12));
        section.SetEvenPageHeaderTemplate(factory.CreatePageTextTemplate("even {running} {page}/{total}", style, 12));
        section.SetEvenPageFooterTemplate(factory.CreatePageTextTemplate("even-foot {page}", style, 12));
        section.AddTextBlock(new PdfTextBlock("body-1", style));
        section.AddPageBreak();
        section.AddTextBlock(new PdfTextBlock("body-2", style));
        section.AddPageBreak();
        section.AddTextBlock(new PdfTextBlock("body-3", style));

        layout.Render();

        string first = document.GetPage(0).GetCanvas().ToPdfString();
        string second = document.GetPage(1).GetCanvas().ToPdfString();
        string third = document.GetPage(2).GetCanvas().ToPdfString();
        Assert.That(first, Does.Contain("(first A 5/7) Tj"));
        Assert.That(first, Does.Contain("(first-foot 5) Tj"));
        Assert.That(second, Does.Contain("(even A 6/7) Tj"));
        Assert.That(second, Does.Contain("(even-foot 6) Tj"));
        Assert.That(third, Does.Contain("(odd A 7/7) Tj"));
        Assert.That(third, Does.Contain("(odd-foot 7) Tj"));
    }

    [Test]
    public void SectionsKeepTheirOwnTemplatesAndContinueNumberingByDefault()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = CreateSectionLayout(document);
        PdfParagraphStyle style = CreateParagraphStyle();
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfDocumentSection first = layout.AddSection(new PdfPageDimensions(100, 60), new PdfPageMargins(10, 5, 10, 5));
        first.SetHeaderTemplate(factory.CreatePageTextTemplate("A {page}/{total}", style, 12));
        first.AddTextBlock(new PdfTextBlock("first-body", style));
        PdfDocumentSection second = layout.AddSection(new PdfPageDimensions(120, 80), new PdfPageMargins(10, 5, 10, 5));
        second.SetHeaderTemplate(factory.CreatePageTextTemplate("B {page}/{total}", style, 12));
        second.AddTextBlock(new PdfTextBlock("second-body", style));

        layout.Render();

        string firstPage = document.GetPage(0).GetCanvas().ToPdfString();
        string secondPage = document.GetPage(1).GetCanvas().ToPdfString();
        Assert.That(firstPage, Does.Contain("(A 1/1) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(B 2/2) Tj"));
        Assert.That(secondPage, Does.Contain("(B 2/2) Tj"));
        Assert.That(secondPage, Does.Not.Contain("(A 1/1) Tj"));
    }

    [Test]
    public void MultiColumnSectionsFlowDownColumnsBeforeAddingPages()
    {
        PdfParagraphStyle style = CreateParagraphStyle();
        PdfDocument singleDocument = new PdfDocument();
        PdfLayoutDocument singleLayout = CreateSectionLayout(singleDocument);
        PdfDocumentSection single = singleLayout.AddSection(new PdfPageDimensions(140, 60), new PdfPageMargins(10, 5, 10, 5));
        AddNumberedText(single, style, 5);
        singleLayout.Render();

        PdfDocument columnDocument = new PdfDocument();
        PdfLayoutDocument columnLayout = CreateSectionLayout(columnDocument);
        PdfDocumentSection columns = columnLayout.AddSection(new PdfPageDimensions(140, 60), new PdfPageMargins(10, 5, 10, 5));
        columns.SetContentColumns(2, 10);
        AddNumberedText(columns, style, 8);
        columnLayout.Render();

        Assert.That(singleDocument.GetPageCount(), Is.EqualTo(2));
        Assert.That(columnDocument.GetPageCount(), Is.EqualTo(1));
        string content = columnDocument.GetPage(0).GetCanvas().ToPdfString();
        Assert.That(content, Does.Contain("10 45 Td\n(item-1) Tj"));
        Assert.That(content, Does.Contain("75 45 Td\n(item-5) Tj"));
        Assert.That(content, Does.Contain("75 9 Td\n(item-8) Tj"));
    }

    [Test]
    public void SectionConfigurationRejectsInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = CreateSectionLayout(document);

        Assert.Throws<ArgumentNullException>(() => layout.AddSection(null!, new PdfPageMargins(0, 0, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => layout.AddSection(new PdfPageDimensions(100, 60), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.AddSection(new PdfPageDimensions(0, 60), new PdfPageMargins(0, 0, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.AddSection(new PdfPageDimensions(100, 60), new PdfPageMargins(-1, 0, 0, 0)));
        Assert.Throws<ArgumentException>(() => layout.AddSection(new PdfPageDimensions(100, 60), new PdfPageMargins(50, 0, 50, 0)));

        PdfDocumentSection section = layout.AddSection(new PdfPageDimensions(100, 60), new PdfPageMargins(10, 5, 10, 5));
        Assert.Throws<ArgumentNullException>(() => section.AddItem(null!));
        Assert.Throws<ArgumentException>(() => section.SetOrientation((PdfPageOrientation)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.SetPageNumberStart(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.SetContentColumns(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.SetContentColumns(2, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => section.SetContentColumns(2, -1));
        Assert.Throws<ArgumentException>(() => section.SetContentColumns(2, 80));
        Assert.Throws<ArgumentNullException>(() => section.SetRunningHead(null!));
        Assert.Throws<ArgumentNullException>(() => section.SetHeaderTemplate(null!));
    }

    static PdfLayoutDocument CreateSectionLayout(PdfDocument document)
    {
        return new PdfLayoutDocument(document, new PdfPageDimensions(100, 60), new PdfPageMargins(0, 0, 0, 0));
    }

    static PdfParagraphStyle CreateParagraphStyle()
    {
        PdfTextStyle textStyle = new PdfStyleFactory().CreateTextStyle(10, PdfFont.Courier);
        return new PdfStyleFactory().CreateParagraphStyleWithMetrics(textStyle, 12, 0, 0, 0);
    }

    static void AddNumberedText(PdfDocumentSection section, PdfParagraphStyle style, int count)
    {
        int number = 1;
        while (number <= count)
        {
            section.AddTextBlock(new PdfTextBlock("item-" + number.ToString(), style));
            number++;
        }
    }

    static void AssertDimensions(PdfPageDimensions dimensions, double width, double height)
    {
        Assert.That(dimensions.Width, Is.EqualTo(width));
        Assert.That(dimensions.Height, Is.EqualTo(height));
    }

    static void AssertRectangle(PdfRectangle rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle.X, Is.EqualTo(x));
        Assert.That(rectangle.Y, Is.EqualTo(y));
        Assert.That(rectangle.Width, Is.EqualTo(width));
        Assert.That(rectangle.Height, Is.EqualTo(height));
    }
}
