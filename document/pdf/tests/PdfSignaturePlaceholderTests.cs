using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfSignaturePlaceholderTests
{
    [Test]
    public void AddSignaturePlaceholder_EnablesAcroFormAndStoresSignatureField()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfAcroFormField field = document.AddSignaturePlaceholderWithLength("signature", page, 10, 20, 120, 30, 16);
        field.SetSignatureReason("Approval");
        field.SetSignatureLocation("Remote");
        field.SetSignatureContactInfo("signer@example.com");

        Assert.That(document.HasAcroForm, Is.True);
        Assert.That(document.AcroForm.Fields.Count, Is.EqualTo(1));
        Assert.That(field.FieldType, Is.EqualTo("Sig"));
        Assert.That(field.HasSignaturePlaceholder, Is.True);
        Assert.That(field.SignatureContentsLength, Is.EqualTo(16));
        Assert.That(field.SignatureReason, Is.EqualTo("Approval"));
        Assert.That(field.SignatureLocation, Is.EqualTo("Remote"));
        Assert.That(field.SignatureContactInfo, Is.EqualTo("signer@example.com"));
    }

    [Test]
    public void Save_WritesSignaturePlaceholderField()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField field = document.AddSignaturePlaceholderWithLength("signature", page, 10, 20, 120, 30, 8);

        field.SetSignatureReason("Approval");
        field.SetSignatureLocation("Remote");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        string fieldBody = GetObjectBody(pdf, 6);

        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Fields [6 0 R ]"));
        Assert.That(fieldBody, Does.Contain("/FT /Sig /T (signature)"));
        Assert.That(fieldBody, Does.Contain("/V << /Type /Sig /Filter /Adobe.PPKLite /SubFilter /ETSI.CAdES.detached"));
        Assert.That(fieldBody, Does.Contain("/ByteRange [0 0000000000 0000000000 0000000000]"));
        Assert.That(fieldBody, Does.Contain("/Contents <0000000000000000>"));
        Assert.That(fieldBody, Does.Contain("/Reason (Approval)"));
        Assert.That(fieldBody, Does.Contain("/Location (Remote)"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/AP << /N 7 0 R >>"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Subtype /Form"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("(Signature) Tj"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/Annots [6 0 R ]"));
    }

    [Test]
    public void Save_EscapesSignaturePlaceholderText()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField field = document.AddSignaturePlaceholderWithLength("sig (1)", page, 10, 20, 120, 30, 4);

        field.SetSignatureReason("A (B) \\ C");
        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        string fieldBody = GetObjectBody(pdf, 6);

        Assert.That(fieldBody, Does.Contain("/T (sig \\(1\\))"));
        Assert.That(fieldBody, Does.Contain("/Reason (A \\(B\\) \\\\ C)"));
    }

    [Test]
    public void Save_WritesPadesMetadataAndPatchableByteRangeReservation()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfAcroFormField field = document.AddSignaturePlaceholderInRectangle("approval", page, new PdfRectangle(10, 20, 120, 30));
        field.ConfigureSignaturePlaceholder(32);
        field.SetSignatureReason("Approved");
        field.SetSignatureLocation("Los Angeles");
        field.SetSignatureContactInfo("signer@example.com");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        string fieldBody = GetObjectBody(pdf, 6);
        Match byteRange = Regex.Match(fieldBody, @"/ByteRange \[0 (?<first>\d{10}) (?<second>\d{10}) (?<third>\d{10})\]");
        Match contents = Regex.Match(fieldBody, @"/Contents <(?<hex>[0-9A-F]+)>");

        Assert.That(field.SignatureSubFilter, Is.EqualTo("ETSI.CAdES.detached"));
        Assert.That(byteRange.Success, Is.True);
        Assert.That(contents.Success, Is.True);
        Assert.That(contents.Groups["hex"].Value, Has.Length.EqualTo(64));
        Assert.That(contents.Groups["hex"].Value, Does.Match("^0+$"));
        Assert.That(fieldBody, Does.Contain("/Reason (Approved)"));
        Assert.That(fieldBody, Does.Contain("/Location (Los Angeles)"));
        Assert.That(fieldBody, Does.Contain("/ContactInfo (signer@example.com)"));
    }

    [Test]
    public void SignaturePlaceholderRejectsInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);
        PdfAcroFormField field = document.AddSignaturePlaceholderWithLength("signature", page, 1, 1, 10, 10, 1);

        Assert.Throws<ArgumentNullException>(() => document.AddSignaturePlaceholder(null!, page, 1, 1, 10, 10));
        Assert.Throws<ArgumentException>(() => document.AddSignaturePlaceholder("", page, 1, 1, 10, 10));
        Assert.Throws<ArgumentNullException>(() => document.AddSignaturePlaceholder("x", null!, 1, 1, 10, 10));
        Assert.Throws<ArgumentException>(() => document.AddSignaturePlaceholder("x", otherPage, 1, 1, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddSignaturePlaceholder("x", page, 1, 1, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddSignaturePlaceholder("x", page, 1, 1, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddSignaturePlaceholderWithLength("x", page, 1, 1, 10, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddSignaturePlaceholderWithLength("x", page, 1, 1, 10, 10, int.MaxValue));
        Assert.Throws<ArgumentNullException>(() => document.AddSignaturePlaceholderInRectangle("x", page, null!));
        Assert.Throws<ArgumentNullException>(() => field.SetSignatureReason(null!));
        Assert.Throws<ArgumentNullException>(() => field.SetSignatureLocation(null!));
        Assert.Throws<ArgumentNullException>(() => field.SetSignatureContactInfo(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => field.ConfigureSignaturePlaceholder(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => field.ConfigureSignaturePlaceholder(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => PdfWriter.ZeroHex(int.MaxValue));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
