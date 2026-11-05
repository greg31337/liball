using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfFormFlatteningTests
{
    [Test]
    public void FlattenForm_DrawsFieldValuesAndDisablesAcroForm()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField text = document.AddTextField("name", page, 10, 60, 100, 20);
        PdfAcroFormField checkbox = document.AddCheckbox("agree", page, 10, 30, 12, 12);

        text.SetValue("Ada");
        checkbox.SetChecked(true);
        document.FlattenForm();

        string content = page.Canvas.ToPdfString();
        Assert.That(document.HasAcroForm, Is.False);
        Assert.That(document.AcroForm.Fields.Count, Is.EqualTo(0));
        Assert.That(document.AcroForm.NeedAppearances, Is.False);
        Assert.That(content, Does.Contain("10 60 100 20 re S"));
        Assert.That(content, Does.Contain("(Ada) Tj"));
        Assert.That(content, Does.Contain("10 30 12 12 re S"));
        Assert.That(content, Does.Contain("10 30 m 22 42 l S"));
        Assert.That(content, Does.Contain("22 30 m 10 42 l S"));
    }

    [Test]
    public void FlattenForm_RemovesInteractiveFormObjectsFromSavedPdf()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField text = document.AddTextField("name", page, 10, 60, 100, 20);

        text.SetValue("Ada");
        document.FlattenForm();

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        string pageBody = GetObjectBody(pdf, 5);

        Assert.That(GetObjectBody(pdf, 1), Does.Not.Contain("/AcroForm"));
        Assert.That(pageBody, Does.Not.Contain("/Annots"));
        Assert.That(pdf, Does.Not.Contain("/Subtype /Widget"));
        Assert.That(pdf, Does.Contain("(Ada) Tj"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 7 /Root 1 0 R >>"));
    }

    [Test]
    public void FlattenForm_DoesNotDrawCheckmarkForUncheckedCheckbox()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddCheckbox("agree", page, 10, 30, 12, 12);
        document.FlattenForm();

        string content = page.Canvas.ToPdfString();
        Assert.That(content, Does.Contain("10 30 12 12 re S"));
        Assert.That(content, Does.Not.Contain("10 30 m 22 42 l S"));
        Assert.That(content, Does.Not.Contain("22 30 m 10 42 l S"));
    }

    [Test]
    public void FlattenForm_IsNoOpWhenDocumentHasNoAcroForm()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        page.Canvas.DrawText("Body", 10, 50, 12);
        string before = page.Canvas.ToPdfString();
        document.FlattenForm();

        Assert.That(document.HasAcroForm, Is.False);
        Assert.That(page.Canvas.ToPdfString(), Is.EqualTo(before));
    }

    [Test]
    public void FlattenForm_AllowsFieldNamesToBeReusedAfterFlattening()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddTextField("name", page, 10, 60, 100, 20);
        document.FlattenForm();
        PdfAcroFormField field = document.AddTextField("name", page, 10, 30, 100, 20);

        Assert.That(document.HasAcroForm, Is.True);
        Assert.That(document.AcroForm.Fields.Count, Is.EqualTo(1));
        Assert.That(field.Name, Is.EqualTo("name"));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
