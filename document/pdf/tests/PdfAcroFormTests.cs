using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAcroFormTests
{
    [Test]
    public void UseAcroForm_EnablesDocumentAcroFormModel()
    {
        PdfDocument document = new PdfDocument();

        PdfAcroForm acroForm = document.UseAcroForm();

        Assert.That(document.HasAcroForm, Is.True);
        Assert.That(document.AcroForm, Is.SameAs(acroForm));
        Assert.That(acroForm.DefaultAppearance, Is.EqualTo("/Helv 0 Tf 0 g"));
        Assert.That(acroForm.NeedAppearances, Is.False);
    }

    [Test]
    public void SetAcroForm_ReplacesAndEnablesDocumentAcroFormModel()
    {
        PdfDocument document = new PdfDocument();
        PdfAcroForm acroForm = new PdfAcroForm();

        acroForm.SetNeedAppearances(true);
        acroForm.SetDefaultAppearance("/Helv 10 Tf 0 g");
        document.SetAcroForm(acroForm);

        Assert.That(document.HasAcroForm, Is.True);
        Assert.That(document.AcroForm, Is.SameAs(acroForm));
        Assert.That(document.AcroForm.NeedAppearances, Is.True);
        Assert.That(document.AcroForm.DefaultAppearance, Is.EqualTo("/Helv 10 Tf 0 g"));
    }

    [Test]
    public void Save_WritesAcroFormCatalogReferenceAndDictionary()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroForm acroForm = document.UseAcroForm();

        page.Canvas.DrawText("Form", 10, 50, 12);
        acroForm.SetNeedAppearances(true);
        acroForm.SetDefaultAppearance("/Helv 12 Tf 0 g");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/AcroForm 5 0 R"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("<< /Fields [] /DR << /Font << /Helv 3 0 R >> >> /DA (/Helv 12 Tf 0 g) /NeedAppearances true >>"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Contents 7 0 R"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 8 /Root 1 0 R >>"));
    }

    [Test]
    public void Save_DoesNotWriteAcroFormUnlessEnabled()
    {
        PdfDocument document = new PdfDocument();

        document.AddPage(200, 100);
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Not.Contain("/AcroForm"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 7 /Root 1 0 R >>"));
    }

    [Test]
    public void AcroFormRejectsInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfAcroForm acroForm = new PdfAcroForm();

        Assert.Throws<ArgumentNullException>(() => document.SetAcroForm(null!));
        Assert.Throws<ArgumentNullException>(() => acroForm.SetDefaultAppearance(null!));
        Assert.Throws<ArgumentException>(() => acroForm.SetDefaultAppearance(""));
        Assert.Throws<ArgumentNullException>(() => acroForm.HasField(null!));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
