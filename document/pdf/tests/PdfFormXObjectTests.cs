using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfFormXObjectTests
{
    [Test]
    public void AddForm_ValidatesSizeAndAssignsStableResourceNames()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddForm(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddForm(10, 0));

        PdfFormXObject first = document.AddForm(20, 10);
        PdfFormXObject second = document.AddForm(30, 15);

        Assert.That(first.Name, Is.EqualTo("Fm1"));
        Assert.That(second.Name, Is.EqualTo("Fm2"));
        Assert.That(document.Forms.Count, Is.EqualTo(2));
        Assert.That(document.Forms[0], Is.SameAs(first));
        Assert.That(document.Forms[1], Is.SameAs(second));
    }

    [Test]
    public void Canvas_DrawFormWritesTransformAndDoOperator()
    {
        PdfFormXObject form = new PdfFormXObject("Fm7", 20, 10);
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawForm(form, 5, 6, 40, 20);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("q"));
        Assert.That(content, Does.Contain("40 0 0 20 5 6 cm"));
        Assert.That(content, Does.Contain("/Fm7 Do"));
        Assert.That(content, Does.Contain("Q"));
    }

    [Test]
    public void Save_WritesFormObjectAndPageResource()
    {
        string pdf = WriteFormDocument();
        string formBody = GetObjectBody(pdf, 5);
        string pageBody = GetObjectBody(pdf, 6);

        Assert.That(formBody, Does.Contain("/Type /XObject /Subtype /Form /FormType 1"));
        Assert.That(formBody, Does.Contain("/BBox [0 0 20 10]"));
        Assert.That(formBody, Does.Contain("/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >>"));
        Assert.That(formBody, Does.Contain("(Reusable) Tj"));
        Assert.That(pageBody, Does.Contain("/XObject << /Fm1 5 0 R >>"));
        Assert.That(pageBody, Does.Contain("/Contents 7 0 R"));
    }

    [Test]
    public void Save_ReusesSingleFormObjectForMultipleDraws()
    {
        string pdf = WriteFormDocument();
        string pageContent = GetObjectBody(pdf, 7);

        Assert.That(CountOccurrences(pdf, "5 0 obj"), Is.EqualTo(1));
        Assert.That(CountOccurrences(pageContent, "/Fm1 Do"), Is.EqualTo(2));
        Assert.That(pageContent, Does.Contain("20 0 0 10 10 20 cm"));
        Assert.That(pageContent, Does.Contain("40 0 0 20 60 20 cm"));
    }

    [Test]
    public void Save_FormStreamLengthMatchesActualBytes()
    {
        string pdf = WriteFormDocument();
        string formBody = GetObjectBody(pdf, 5);
        Match length = Regex.Match(formBody, @"/Length (?<length>\d+)");

        Assert.That(length.Success, Is.True);
        Assert.That(length.Groups["length"].Value, Is.EqualTo(GetStreamData(formBody).Length.ToString()));
    }

    static string WriteFormDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfFormXObject form = document.AddForm(20, 10);
        PdfPage page = document.AddPage(120, 80);

        form.Canvas.SetFillColorValue(PdfColor.Gray(0.8));
        form.Canvas.FillRectangle(0, 0, 20, 10);
        form.Canvas.DrawTextWithFont("Reusable", 2, 3, 4, PdfFont.Bold);
        page.Canvas.DrawForm(form, 10, 20, 20, 10);
        page.Canvas.DrawForm(form, 60, 20, 40, 20);

        return Encoding.ASCII.GetString(document.ToBytes());
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }

    static string GetStreamData(string objectBody)
    {
        string startMarker = "stream\n";
        string endMarker = "endstream";
        int start = objectBody.IndexOf(startMarker, StringComparison.Ordinal);
        int end = objectBody.IndexOf(endMarker, StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));

        start += startMarker.Length;
        return objectBody.Substring(start, end - start);
    }

    static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while (index >= 0)
        {
            index = text.IndexOf(value, index, StringComparison.Ordinal);
            if (index >= 0)
            {
                count++;
                index += value.Length;
            }
        }

        return count;
    }
}
