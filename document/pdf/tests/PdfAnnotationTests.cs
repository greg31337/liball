using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAnnotationTests
{
    [Test]
    public void AddTextAnnotation_StoresAnnotationOnPage()
    {
        PdfPage page = new PdfPage(200, 100);

        PdfAnnotation annotation = page.AddTextAnnotation("Review this", 10, 20, 16, 16);
        annotation.SetOpen(true);
        annotation.SetIconName("Comment");
        annotation.SetColor(PdfColor.Rgb(1, 0, 0));

        Assert.That(page.Annotations.Count, Is.EqualTo(1));
        Assert.That(page.Annotations[0], Is.SameAs(annotation));
        Assert.That(annotation.Subtype, Is.EqualTo("Text"));
        Assert.That(annotation.Contents, Is.EqualTo("Review this"));
        Assert.That(annotation.Rectangle.X, Is.EqualTo(10));
        Assert.That(annotation.Rectangle.Y, Is.EqualTo(20));
        Assert.That(annotation.Rectangle.Width, Is.EqualTo(16));
        Assert.That(annotation.Rectangle.Height, Is.EqualTo(16));
        Assert.That(annotation.Open, Is.True);
        Assert.That(annotation.IconName, Is.EqualTo("Comment"));
        Assert.That(annotation.HasColor, Is.True);
    }

    [Test]
    public void AddSquareAnnotation_StoresAnnotationOnPage()
    {
        PdfPage page = new PdfPage(200, 100);

        PdfAnnotation annotation = page.AddSquareAnnotation("Boxed area", 10, 20, 80, 30);

        Assert.That(page.Annotations.Count, Is.EqualTo(1));
        Assert.That(annotation.Subtype, Is.EqualTo("Square"));
        Assert.That(annotation.Contents, Is.EqualTo("Boxed area"));
        Assert.That(annotation.Rectangle.Width, Is.EqualTo(80));
        Assert.That(annotation.Rectangle.Height, Is.EqualTo(30));
    }

    [Test]
    public void Save_WritesTextAndSquareAnnotations()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfAnnotation text = page.AddTextAnnotation("Needs review", 10, 40, 16, 16);
        text.SetOpen(true);
        text.SetIconName("Comment");
        text.SetColor(PdfColor.Rgb(1, 0, 0));

        PdfAnnotation square = page.AddSquareAnnotation("Important area", 30, 20, 80, 20);
        square.SetColor(PdfColor.Gray(0.5));

        string pageBody = GetObjectBody(Encoding.ASCII.GetString(document.ToBytes()), 5);

        Assert.That(pageBody, Does.Contain("<< /Type /Annot /Subtype /Text /Rect [10 40 26 56] /Contents (Needs review) /Name /Comment /Open true /C [1 0 0] >>"));
        Assert.That(pageBody, Does.Contain("<< /Type /Annot /Subtype /Square /Rect [30 20 110 40] /Contents (Important area) /C [0.5 0.5 0.5] >>"));
    }

    [Test]
    public void Save_WritesBasicAnnotationsInSameArrayAsLinks()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddNamedDestination("top", page, 0, 100);
        page.AddInternalLink("top", 10, 70, 50, 12);
        page.AddExternalLink("https://example.com", 10, 50, 70, 12);
        page.AddTextAnnotation("Note", 10, 30, 16, 16);

        string pageBody = GetObjectBody(Encoding.ASCII.GetString(document.ToBytes()), 5);

        Assert.That(pageBody, Does.Contain("/Annots ["));
        Assert.That(pageBody, Does.Contain("/Dest /top"));
        Assert.That(pageBody, Does.Contain("/A << /S /URI /URI (https://example.com) >>"));
        Assert.That(pageBody, Does.Contain("/Subtype /Text /Rect [10 30 26 46] /Contents (Note)"));
    }

    [Test]
    public void AnnotationsEscapeTextAndIconNames()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfAnnotation annotation = page.AddTextAnnotation("a (b) \\ c", 10, 40, 16, 16);
        annotation.SetIconName("Review Note");

        string pageBody = GetObjectBody(Encoding.ASCII.GetString(document.ToBytes()), 5);

        Assert.That(pageBody, Does.Contain("/Contents (a \\(b\\) \\\\ c)"));
        Assert.That(pageBody, Does.Contain("/Name /Review#20Note"));
    }

    [Test]
    public void BasicAnnotationsRejectInvalidInputs()
    {
        PdfPage page = new PdfPage(200, 100);
        PdfAnnotation annotation = page.AddTextAnnotation("Note", 1, 1, 1, 1);

        Assert.Throws<ArgumentNullException>(() => page.AddTextAnnotation(null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => page.AddTextAnnotationInRectangle("Note", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddTextAnnotation("Note", 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddTextAnnotation("Note", 1, 1, 1, 0));
        Assert.Throws<ArgumentNullException>(() => page.AddSquareAnnotation(null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => page.AddSquareAnnotationInRectangle("Note", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddSquareAnnotation("Note", 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => page.AddSquareAnnotation("Note", 1, 1, 1, 0));
        Assert.Throws<ArgumentNullException>(() => annotation.SetColor(null!));
        Assert.Throws<ArgumentException>(() => annotation.SetColor(new PdfColor("Invalid", 0, 0, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => annotation.SetColor(new PdfColor("RGB", double.NaN, 0, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => annotation.SetIconName(null!));
        Assert.Throws<ArgumentException>(() => annotation.SetIconName(""));
        Assert.Throws<ArgumentException>(() => new PdfAnnotation("", "Note", new PdfRectangle(1, 1, 1, 1)));
    }

    [Test]
    public void AnnotationOwnsRectangleAndColorValues()
    {
        PdfRectangle rectangle = new PdfRectangle(10, 20, 30, 40);
        PdfColor color = PdfColor.Rgb(0.1, 0.2, 0.3);

        PdfAnnotation annotation = new PdfAnnotation("Square", "Note", rectangle);
        annotation.SetColor(color);
        rectangle.X = 99;
        color.First = 0.9;

        Assert.That(annotation.Rectangle, Is.Not.SameAs(rectangle));
        Assert.That(annotation.Rectangle.X, Is.EqualTo(10));
        Assert.That(annotation.Color, Is.Not.SameAs(color));
        Assert.That(annotation.Color.First, Is.EqualTo(0.1));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
