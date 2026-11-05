using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPageTemplateTests
{
    [Test]
    public void PageTemplate_DrawsItemAtTopOfTemplateRegion()
    {
        PdfPage page = new PdfPage(100, 60);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfPageTemplate template = PdfPageTemplate.FromTextBlock(new PdfTextBlock("header", paragraphStyle), 12);

        PdfLayoutResult result = template.Draw(page, 10, 40, 80);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Height, Is.EqualTo(12));
        Assert.That(content, Does.Contain("10 42 Td"));
        Assert.That(content, Does.Contain("(header) Tj"));
    }

    [Test]
    public void PageTemplate_DrawsPageTextPlaceholders()
    {
        PdfPage page = new PdfPage(100, 60);
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfPageTemplate template = PdfPageTemplate.FromPageText("Page {page} of {total}", paragraphStyle, 12);

        PdfLayoutResult result = template.DrawForPage(page, 10, 40, 80, 2, 5);

        string content = page.Canvas.ToPdfString();
        Assert.That(result.Size.Height, Is.EqualTo(12));
        Assert.That(content, Does.Contain("10 42 Td"));
        Assert.That(content, Does.Contain("(Page 2 of 5) Tj"));
    }

    [Test]
    public void LayoutDocument_RendersHeaderAndFooterTemplatesOnEveryPageAndReservesSpace()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetHeaderTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock("head", paragraphStyle), 12));
        layout.SetFooterTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock("foot", paragraphStyle), 12));
        layout.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("third", paragraphStyle));
        PdfPageCursor cursor = layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(firstPage, Does.Contain("(head) Tj"));
        Assert.That(firstPage, Does.Contain("10 45 Td"));
        Assert.That(firstPage, Does.Contain("(foot) Tj"));
        Assert.That(firstPage, Does.Contain("10 7 Td"));
        Assert.That(firstPage, Does.Contain("(first) Tj"));
        Assert.That(firstPage, Does.Contain("10 33 Td"));
        Assert.That(firstPage, Does.Contain("(second) Tj"));
        Assert.That(firstPage, Does.Contain("10 21 Td"));
        Assert.That(firstPage, Does.Not.Contain("(third) Tj"));
        Assert.That(secondPage, Does.Contain("(head) Tj"));
        Assert.That(secondPage, Does.Contain("(foot) Tj"));
        Assert.That(secondPage, Does.Contain("(third) Tj"));
        Assert.That(cursor.CurrentPage, Is.SameAs(document.Pages[1]));
        Assert.That(cursor.CurrentY, Is.EqualTo(31));
    }

    [Test]
    public void LayoutDocument_RendersPageTextTemplatesAfterTotalPageCountIsKnown()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", paragraphStyle, 12));
        layout.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("third", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("fourth", paragraphStyle));
        layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(firstPage, Does.Contain("(Page 1 of 2) Tj"));
        Assert.That(secondPage, Does.Contain("(Page 2 of 2) Tj"));
        Assert.That(firstPage, Does.Not.Contain("{total}"));
        Assert.That(secondPage, Does.Not.Contain("{page}"));
    }

    [Test]
    public void LayoutDocument_PageTextTemplatesReserveSpaceBeforeFinalPass()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", paragraphStyle, 12));
        layout.AddTextBlock(new PdfTextBlock("first", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("second", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("third", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("fourth", paragraphStyle));
        layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        Assert.That(firstPage, Does.Contain("(third) Tj"));
        Assert.That(firstPage, Does.Contain("10 21 Td"));
        Assert.That(firstPage, Does.Not.Contain("(fourth) Tj"));
        Assert.That(secondPage, Does.Contain("(fourth) Tj"));
        Assert.That(firstPage, Does.Contain("10 7 Td"));
        Assert.That(secondPage, Does.Contain("10 7 Td"));
    }

    [Test]
    public void LayoutDocument_ClearTemplatesStopsRenderingAndReservation()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetHeaderTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock("head", paragraphStyle), 12));
        layout.SetFooterTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock("foot", paragraphStyle), 12));
        layout.ClearHeaderTemplate();
        layout.ClearFooterTemplate();
        layout.AddTextBlock(new PdfTextBlock("body", paragraphStyle));
        PdfPageCursor cursor = layout.Render();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(content, Does.Not.Contain("(head) Tj"));
        Assert.That(content, Does.Not.Contain("(foot) Tj"));
        Assert.That(content, Does.Contain("(body) Tj"));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
    }

    [Test]
    public void PageCursor_AppliesTemplatesToManuallyAddedPages()
    {
        PdfDocument document = new PdfDocument();
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);
        PdfPageTemplate header = PdfPageTemplate.FromTextBlock(new PdfTextBlock("head", paragraphStyle), 12);
        PdfPageTemplate footer = PdfPageTemplate.FromTextBlock(new PdfTextBlock("foot", paragraphStyle), 12);
        PdfPageCursor cursor = PdfPageCursor.Create(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5), true, header, true, footer);

        cursor.AddPage();

        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(head) Tj"));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(foot) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(head) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(foot) Tj"));
        Assert.That(cursor.CurrentY, Is.EqualTo(43));
        Assert.That(cursor.GetRemainingHeight(), Is.EqualTo(26));
    }

    [Test]
    public void LayoutDocument_UsesFirstPageThenOddEvenHeaderAndFooterVariants()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetHeaderTemplate(TextTemplate("default-head", paragraphStyle));
        layout.SetFooterTemplate(TextTemplate("default-foot", paragraphStyle));
        layout.SetFirstPageHeaderTemplate(TextTemplate("first-head", paragraphStyle));
        layout.SetFirstPageFooterTemplate(TextTemplate("first-foot", paragraphStyle));
        layout.SetOddPageHeaderTemplate(TextTemplate("odd-head", paragraphStyle));
        layout.SetOddPageFooterTemplate(TextTemplate("odd-foot", paragraphStyle));
        layout.SetEvenPageHeaderTemplate(TextTemplate("even-head", paragraphStyle));
        layout.SetEvenPageFooterTemplate(TextTemplate("even-foot", paragraphStyle));
        layout.AddTextBlock(new PdfTextBlock("body1", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("body2", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("body3", paragraphStyle));
        layout.Render();

        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string secondPage = document.Pages[1].Canvas.ToPdfString();
        string thirdPage = document.Pages[2].Canvas.ToPdfString();
        Assert.That(firstPage, Does.Contain("(first-head) Tj"));
        Assert.That(firstPage, Does.Contain("(first-foot) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(odd-head) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(default-head) Tj"));
        Assert.That(secondPage, Does.Contain("(even-head) Tj"));
        Assert.That(secondPage, Does.Contain("(even-foot) Tj"));
        Assert.That(secondPage, Does.Not.Contain("(default-head) Tj"));
        Assert.That(thirdPage, Does.Contain("(odd-head) Tj"));
        Assert.That(thirdPage, Does.Contain("(odd-foot) Tj"));
        Assert.That(thirdPage, Does.Not.Contain("(default-head) Tj"));
    }

    [Test]
    public void LayoutDocument_VariantTemplateHeightsReserveDifferentBodySpace()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetFirstPageHeaderTemplate(new PdfPageTemplate(PdfLayoutItem.FromTextBlock(new PdfTextBlock("first", paragraphStyle)), 20));
        layout.SetEvenPageHeaderTemplate(new PdfPageTemplate(PdfLayoutItem.FromTextBlock(new PdfTextBlock("even", paragraphStyle)), 12));
        layout.SetOddPageHeaderTemplate(new PdfPageTemplate(PdfLayoutItem.FromTextBlock(new PdfTextBlock("odd", paragraphStyle)), 16));
        layout.AddTextBlock(new PdfTextBlock("body1", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("body2", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("body3", paragraphStyle));
        layout.Render();

        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("10 25 Td"));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(body1) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("10 33 Td"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(body2) Tj"));
        Assert.That(document.Pages[2].Canvas.ToPdfString(), Does.Contain("10 29 Td"));
        Assert.That(document.Pages[2].Canvas.ToPdfString(), Does.Contain("(body3) Tj"));
    }

    [Test]
    public void LayoutDocument_ClearVariantTemplatesFallsBackToDefaultTemplate()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetHeaderTemplate(TextTemplate("default-head", paragraphStyle));
        layout.SetFooterTemplate(TextTemplate("default-foot", paragraphStyle));
        layout.SetOddPageHeaderTemplate(TextTemplate("odd-head", paragraphStyle));
        layout.SetOddPageFooterTemplate(TextTemplate("odd-foot", paragraphStyle));
        layout.ClearOddPageHeaderTemplate();
        layout.ClearOddPageFooterTemplate();
        layout.AddTextBlock(new PdfTextBlock("body", paragraphStyle));
        layout.Render();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("(default-head) Tj"));
        Assert.That(content, Does.Contain("(default-foot) Tj"));
        Assert.That(content, Does.Not.Contain("(odd-head) Tj"));
        Assert.That(content, Does.Not.Contain("(odd-foot) Tj"));
    }

    [Test]
    public void LayoutDocument_PageTextPlaceholdersWorkWithTemplateVariants()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageSizeFactory().Custom(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfTextStyle textStyle = PdfTextStyle.Create(10, PdfFont.Courier);
        PdfParagraphStyle paragraphStyle = PdfParagraphStyle.Create(textStyle, 12, 0, 0, 0);

        layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Default {page}/{total}", paragraphStyle, 12));
        layout.SetFirstPageFooterTemplate(PdfPageTemplate.FromPageText("First {page}/{total}", paragraphStyle, 12));
        layout.SetOddPageFooterTemplate(PdfPageTemplate.FromPageText("Odd {page}/{total}", paragraphStyle, 12));
        layout.SetEvenPageFooterTemplate(PdfPageTemplate.FromPageText("Even {page}/{total}", paragraphStyle, 12));
        layout.AddTextBlock(new PdfTextBlock("body1", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("body2", paragraphStyle));
        layout.AddPageBreak();
        layout.AddTextBlock(new PdfTextBlock("body3", paragraphStyle));
        layout.Render();

        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(First 1/3) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(Even 2/3) Tj"));
        Assert.That(document.Pages[2].Canvas.ToPdfString(), Does.Contain("(Odd 3/3) Tj"));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Not.Contain("(Default 1/3) Tj"));
    }

    [Test]
    public void PageTemplates_RejectInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPageDimensions pageSize = new PdfPageSizeFactory().Custom(100, 60);
        PdfPageMargins margins = new PdfPageMargins(10, 5, 10, 5);
        PdfPageTemplate template = PdfPageTemplate.FromBlock(PdfBlock.WithSize(10, 10), 12);
        PdfPageCursor cursor = PdfPageCursor.Create(document);
        PdfLayoutDocument layout = PdfLayoutDocument.Default(new PdfDocument());

        Assert.Throws<ArgumentNullException>(() => new PdfPageTemplate(null!, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromTextBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromStackBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromRowBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromColumnBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromGridBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromTableBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromListBlock(null!, 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromPageText(null!, new PdfParagraphStyle(), 12));
        Assert.Throws<ArgumentNullException>(() => PdfPageTemplate.FromPageText("x", null!, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutFactory().CreateBackgroundTemplate(PdfColor.Gray(0.5), double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLayoutFactory().CreateWatermarkTemplate("x", new PdfTextStyle(), 12, double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => template.Draw(null!, 0, 0, 10));
        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(document, pageSize, margins, true, null!, false, template));
        Assert.Throws<ArgumentNullException>(() => PdfPageCursor.Create(document, pageSize, margins, false, template, true, null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetFirstPageHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetFirstPageFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetOddPageHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetOddPageFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetEvenPageHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => cursor.SetEvenPageFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetFirstPageHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetFirstPageFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetOddPageHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetOddPageFooterTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetEvenPageHeaderTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => layout.SetEvenPageFooterTemplate(null!));
    }

    static PdfPageTemplate TextTemplate(string text, PdfParagraphStyle paragraphStyle)
    {
        return PdfPageTemplate.FromTextBlock(new PdfTextBlock(text, paragraphStyle), 12);
    }
}
