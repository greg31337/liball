using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfInternalLinkTests
{
    [Test]
    public void AddNamedDestination_StoresDestinationOnDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfNamedDestination destination = document.AddNamedDestination("chapter1", page, 12, 88);

        Assert.That(document.NamedDestinations.Count, Is.EqualTo(1));
        Assert.That(document.NamedDestinations[0], Is.SameAs(destination));
        Assert.That(destination.Name, Is.EqualTo("chapter1"));
        Assert.That(destination.Page, Is.SameAs(page));
        Assert.That(destination.X, Is.EqualTo(12));
        Assert.That(destination.Y, Is.EqualTo(88));
    }

    [Test]
    public void AddInternalLink_StoresLinkOnPage()
    {
        PdfPage page = new PdfPage(200, 100);

        PdfInternalLink link = page.AddInternalLink("chapter1", 10, 20, 30, 12);

        Assert.That(page.InternalLinks.Count, Is.EqualTo(1));
        Assert.That(page.InternalLinks[0], Is.SameAs(link));
        Assert.That(link.DestinationName, Is.EqualTo("chapter1"));
        Assert.That(link.Rectangle.X, Is.EqualTo(10));
        Assert.That(link.Rectangle.Y, Is.EqualTo(20));
        Assert.That(link.Rectangle.Width, Is.EqualTo(30));
        Assert.That(link.Rectangle.Height, Is.EqualTo(12));
    }

    [Test]
    public void Save_WritesNamedDestinationsAndInternalLinkAnnotations()
    {
        PdfDocument document = new PdfDocument();
        PdfPage firstPage = document.AddPage(200, 100);
        PdfPage secondPage = document.AddPage(200, 100);

        firstPage.Canvas.DrawText("Go", 10, 50, 12);
        secondPage.Canvas.DrawText("Target", 10, 50, 12);
        document.AddNamedDestination("chapter1", secondPage, 0, 100);
        firstPage.AddInternalLink("chapter1", 10, 40, 50, 14);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/Dests << /chapter1 [7 0 R /XYZ 0 100 0] >>"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Annots [<< /Type /Annot /Subtype /Link /Rect [10 40 60 54] /Border [0 0 0] /Dest /chapter1 >>]"));
        Assert.That(GetObjectBody(pdf, 7), Does.Not.Contain("/Annots"));
    }

    [Test]
    public void Save_EscapesSpecialCharactersInDestinationNames()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddNamedDestination("section 1/intro", page, 0, 100);
        page.AddInternalLink("section 1/intro", 10, 40, 50, 14);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/section#201#2Fintro [5 0 R /XYZ 0 100 0]"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Dest /section#201#2Fintro"));
    }

    [Test]
    public void NavigationRejectsInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);

        Assert.Throws<ArgumentNullException>(() => document.AddNamedDestination(null!, page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddNamedDestination("", page, 0, 0));
        Assert.Throws<ArgumentNullException>(() => document.AddNamedDestination("x", null!, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddNamedDestination("x", otherPage, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddNamedDestination("nan", page, double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddNamedDestination("infinity", page, 0, double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => new PdfInternalLink("", new PdfRectangle(1, 1, 1, 1)));

        document.AddNamedDestination("x", page, 0, 0);
        Assert.Throws<ArgumentException>(() => document.AddNamedDestination("x", page, 0, 0));

        Assert.Throws<ArgumentNullException>(() => page.AddInternalLink(null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => page.AddInternalLink("", 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => page.AddInternalLinkInRectangle("x", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddInternalLink("x", 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddInternalLink("x", 1, 1, 1, 0));
    }

    [Test]
    public void InternalLinkOwnsItsValidatedRectangle()
    {
        PdfRectangle rectangle = new PdfRectangle(10, 20, 30, 40);

        PdfInternalLink link = new PdfInternalLink("target", rectangle);
        rectangle.X = 99;

        Assert.That(link.Rectangle, Is.Not.SameAs(rectangle));
        Assert.That(link.Rectangle.X, Is.EqualTo(10));
    }

    [Test]
    public void Save_RejectsLinksToUndefinedNamedDestinations()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.AddInternalLink("missing", 10, 40, 50, 14);

        Assert.Throws<InvalidOperationException>(() => document.ToBytes());
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
