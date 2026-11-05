using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTableOfContentsTests
{
    [Test]
    public void InsertPage_GeneratesContentsFromOutlines()
    {
        PdfDocument document = new PdfDocument();
        PdfPage firstPage = document.AddPage(200, 100);
        PdfPage secondPage = document.AddPage(200, 100);
        PdfOutlineItem chapter = document.AddOutline("Chapter 1", firstPage, 0, 100);
        document.AddOutlineChild(chapter, "Section 1.1", secondPage, 0, 100);
        PdfTableOfContents tableOfContents = new PdfTableOfContents();

        PdfPage tocPage = tableOfContents.InsertPageWithLayout(document, 0, new PdfPageSizeFactory().Custom(300, 200), new PdfPageMargins(20, 20, 20, 20));

        string content = tocPage.Canvas.ToPdfString();
        Assert.That(document.Pages[0], Is.SameAs(tocPage));
        Assert.That(document.Pages.Count, Is.EqualTo(3));
        Assert.That(content, Does.Contain("(Contents) Tj"));
        Assert.That(content, Does.Contain("(Chapter 1) Tj"));
        Assert.That(content, Does.Contain("(Section 1.1) Tj"));
        Assert.That(content, Does.Contain("(2) Tj"));
        Assert.That(content, Does.Contain("(3) Tj"));
        Assert.That(tocPage.InternalLinks.Count, Is.EqualTo(2));
        Assert.That(document.NamedDestinations.Count, Is.EqualTo(2));
        Assert.That(document.NamedDestinations[0].Page, Is.SameAs(firstPage));
        Assert.That(document.NamedDestinations[1].Page, Is.SameAs(secondPage));
    }

    [Test]
    public void Draw_GeneratesUniqueDestinationsWhenDocumentAlreadyHasTocDestinationNames()
    {
        PdfDocument document = new PdfDocument();
        PdfPage tocPage = document.AddPage(300, 200);
        PdfPage targetPage = document.AddPage(300, 200);
        document.AddNamedDestination("toc-1", targetPage, 0, 100);
        document.AddOutline("Chapter", targetPage, 0, 100);
        PdfTableOfContents tableOfContents = new PdfTableOfContents();

        tableOfContents.Draw(document, tocPage, 20, 180, 260);

        Assert.That(document.NamedDestinations.Count, Is.EqualTo(2));
        Assert.That(document.NamedDestinations[1].Name, Is.EqualTo("toc-2"));
        Assert.That(tocPage.InternalLinks[0].DestinationName, Is.EqualTo("toc-2"));
    }

    [Test]
    public void Save_WritesGeneratedTableOfContentsLinks()
    {
        PdfDocument document = new PdfDocument();
        PdfPage firstPage = document.AddPage(200, 100);
        PdfPage secondPage = document.AddPage(200, 100);
        PdfOutlineItem chapter = document.AddOutline("Chapter", firstPage, 0, 100);
        document.AddOutlineChild(chapter, "Section", secondPage, 0, 100);
        PdfTableOfContents tableOfContents = new PdfTableOfContents();

        tableOfContents.InsertPageWithLayout(document, 0, new PdfPageSizeFactory().Custom(200, 100), new PdfPageMargins(10, 10, 10, 10));
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/toc-1 [10 0 R /XYZ 0 100 0]"));
        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/toc-2 [12 0 R /XYZ 0 100 0]"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/Annots ["));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/Dest /toc-1"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/Dest /toc-2"));
    }

    [Test]
    public void TableOfContentsRejectsInvalidInputs()
    {
        PdfTableOfContents tableOfContents = new PdfTableOfContents();
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);

        Assert.Throws<ArgumentNullException>(() => tableOfContents.InsertPage(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => tableOfContents.InsertPage(document, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tableOfContents.InsertPage(document, 2));
        Assert.Throws<ArgumentNullException>(() => tableOfContents.InsertPageWithLayout(document, 0, null!, new PdfPageMargins(10, 10, 10, 10)));
        Assert.Throws<ArgumentNullException>(() => tableOfContents.InsertPageWithLayout(document, 0, new PdfPageSizeFactory().Letter(), null!));
        Assert.Throws<ArgumentNullException>(() => tableOfContents.Draw(null!, page, 0, 100, 100));
        Assert.Throws<ArgumentNullException>(() => tableOfContents.Draw(document, null!, 0, 100, 100));
        Assert.Throws<ArgumentException>(() => tableOfContents.Draw(document, otherPage, 0, 100, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => tableOfContents.Draw(document, page, 0, 100, 0));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
