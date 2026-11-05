using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfExternalLinkTests
{
    [Test]
    public void AddExternalLink_StoresLinkOnPage()
    {
        PdfPage page = new PdfPage(200, 100);

        PdfExternalLink link = page.AddExternalLink("https://example.com", 10, 20, 30, 12);

        Assert.That(page.ExternalLinks.Count, Is.EqualTo(1));
        Assert.That(page.ExternalLinks[0], Is.SameAs(link));
        Assert.That(link.Uri, Is.EqualTo("https://example.com"));
        Assert.That(link.Rectangle.X, Is.EqualTo(10));
        Assert.That(link.Rectangle.Y, Is.EqualTo(20));
        Assert.That(link.Rectangle.Width, Is.EqualTo(30));
        Assert.That(link.Rectangle.Height, Is.EqualTo(12));
    }

    [Test]
    public void Save_WritesExternalLinkAnnotations()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.Canvas.DrawText("Open website", 10, 50, 12);
        page.AddExternalLink("https://example.com/path?q=1", 10, 40, 90, 14);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Annots [<< /Type /Annot /Subtype /Link /Rect [10 40 100 54] /Border [0 0 0] /A << /S /URI /URI (https://example.com/path?q=1) >> >>]"));
    }

    [Test]
    public void Save_EscapesExternalLinkUriText()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.AddExternalLink("https://example.com/a(1)\\b", 10, 40, 90, 14);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/URI (https://example.com/a\\(1\\)\\\\b)"));
    }

    [Test]
    public void Save_WritesInternalAndExternalLinksInOneAnnotationsArray()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddNamedDestination("top", page, 0, 100);
        page.AddInternalLink("top", 10, 40, 50, 14);
        page.AddExternalLink("https://example.com", 10, 20, 80, 14);

        string pageBody = GetObjectBody(Encoding.ASCII.GetString(document.ToBytes()), 5);

        Assert.That(pageBody, Does.Contain("/Dest /top >>"));
        Assert.That(pageBody, Does.Contain("/A << /S /URI /URI (https://example.com) >> >>"));
    }

    [Test]
    public void ExternalLinksRejectInvalidInputs()
    {
        PdfPage page = new PdfPage(200, 100);

        Assert.Throws<ArgumentNullException>(() => page.AddExternalLink(null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => page.AddExternalLink("", 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => new PdfExternalLink("", new PdfRectangle(1, 1, 1, 1)));
        Assert.Throws<ArgumentNullException>(() => page.AddExternalLinkInRectangle("https://example.com", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddExternalLink("https://example.com", 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddExternalLink("https://example.com", 1, 1, 1, 0));
    }

    [Test]
    public void ExternalLinkOwnsItsValidatedRectangle()
    {
        PdfRectangle rectangle = new PdfRectangle(10, 20, 30, 40);

        PdfExternalLink link = new PdfExternalLink("https://example.com", rectangle);
        rectangle.X = 99;

        Assert.That(link.Rectangle, Is.Not.SameAs(rectangle));
        Assert.That(link.Rectangle.X, Is.EqualTo(10));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
