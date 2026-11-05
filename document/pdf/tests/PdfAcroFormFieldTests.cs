using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAcroFormFieldTests
{
    [Test]
    public void AddTextField_EnablesAcroFormAndStoresField()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfAcroFormField field = document.AddTextField("name", page, 10, 60, 100, 20);
        field.SetValue("Ada");

        Assert.That(document.HasAcroForm, Is.True);
        Assert.That(document.AcroForm.NeedAppearances, Is.False);
        Assert.That(document.AcroForm.Fields.Count, Is.EqualTo(1));
        Assert.That(document.AcroForm.Fields[0], Is.SameAs(field));
        Assert.That(field.FieldType, Is.EqualTo("Tx"));
        Assert.That(field.Name, Is.EqualTo("name"));
        Assert.That(field.Page, Is.SameAs(page));
        Assert.That(field.Value, Is.EqualTo("Ada"));
    }

    [Test]
    public void AddCheckbox_EnablesAcroFormAndStoresField()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfAcroFormField field = document.AddCheckbox("agree", page, 10, 30, 12, 12);
        field.SetChecked(true);

        Assert.That(document.HasAcroForm, Is.True);
        Assert.That(document.AcroForm.Fields.Count, Is.EqualTo(1));
        Assert.That(field.FieldType, Is.EqualTo("Btn"));
        Assert.That(field.Checked, Is.True);
    }

    [Test]
    public void Save_WritesTextFieldAndCheckboxObjects()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField text = document.AddTextField("name", page, 10, 60, 100, 20);
        PdfAcroFormField checkbox = document.AddCheckbox("agree", page, 10, 30, 12, 12);

        text.SetValue("Ada");
        checkbox.SetChecked(true);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/AcroForm 5 0 R"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Fields [6 0 R 8 0 R ]"));
        Assert.That(GetObjectBody(pdf, 5), Does.Not.Contain("/NeedAppearances true"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/P 11 0 R /FT /Tx /T (name) /F 4 /V (Ada) /DA (/Helv 0 Tf 0 g) /AP << /N 7 0 R >>"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Subtype /Form"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("(Ada) Tj"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/P 11 0 R /FT /Btn /T (agree) /F 4 /V /Yes /AS /Yes /AP << /N << /Off 10 0 R /Yes 9 0 R >> >>"));
        Assert.That(GetObjectBody(pdf, 9), Does.Contain("/Subtype /Form"));
        Assert.That(GetObjectBody(pdf, 10), Does.Contain("/Subtype /Form"));
        Assert.That(GetObjectBody(pdf, 11), Does.Contain("/Annots [6 0 R 8 0 R ]"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 13 /Root 1 0 R >>"));
    }

    [Test]
    public void Save_WritesUncheckedCheckboxState()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddCheckbox("agree", page, 10, 30, 12, 12);
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/V /Off /AS /Off"));
    }

    [Test]
    public void FormFieldsEscapeNamesAndValues()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField field = document.AddTextField("full (name)", page, 10, 60, 100, 20);

        field.SetValue("Ada \\ Lovelace");
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/T (full \\(name\\))"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/V (Ada \\\\ Lovelace)"));
    }

    [Test]
    public void FormFieldsRejectInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);
        PdfAcroFormField field = document.AddTextField("name", page, 1, 1, 1, 1);

        Assert.Throws<ArgumentNullException>(() => document.AddTextField(null!, page, 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => document.AddTextField("", page, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => document.AddTextField("x", null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => document.AddTextField("x", otherPage, 1, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddTextField("x", page, 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddTextField("x", page, 1, 1, 1, 0));
        Assert.Throws<ArgumentException>(() => document.AddTextField("name", page, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => document.AddCheckbox(null!, page, 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => document.AddCheckbox("", page, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => document.AddCheckbox("check", null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => document.AddCheckbox("check", otherPage, 1, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddCheckbox("check", page, 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddCheckbox("check", page, 1, 1, 1, 0));
        Assert.Throws<ArgumentNullException>(() => field.SetValue(null!));
    }

    [Test]
    public void DirectFormFieldOwnsItsValidatedRectangle()
    {
        PdfPage page = new PdfPage(100, 100);
        PdfRectangle rectangle = new PdfRectangle(10, 20, 30, 40);

        PdfAcroFormField field = new PdfAcroFormField("Tx", "name", page, rectangle);
        rectangle.X = 99;

        Assert.That(field.Rectangle, Is.Not.SameAs(rectangle));
        Assert.That(field.Rectangle.X, Is.EqualTo(10));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
