using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAttachmentTests
{
    [Test]
    public void Attachment_StoresFieldsAndCopiesData()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        PdfAttachment attachment = new PdfAttachment("notes.txt", data, "text/plain", "Notes");
        data[0] = 9;

        Assert.That(attachment.FileName, Is.EqualTo("notes.txt"));
        Assert.That(attachment.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(attachment.Data, Is.Not.SameAs(data));
        Assert.That(attachment.MimeType, Is.EqualTo("text/plain"));
        Assert.That(attachment.Description, Is.EqualTo("Notes"));
    }

    [Test]
    public void Save_WritesEmbeddedFileNameTreeAndFileSpecification()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        byte[] data = Encoding.ASCII.GetBytes("hello notes");

        page.Canvas.DrawText("Attachment test", 10, 50, 12);
        document.AddAttachmentWithMetadata("z-notes.txt", data, "text/plain", "Notes (draft)");
        document.AddAttachmentWithMetadata("a-data.json", Encoding.ASCII.GetBytes("{}"), "application/json", string.Empty);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/Names << /EmbeddedFiles << /Names [(a-data.json) 8 0 R (z-notes.txt) 6 0 R ] >> >>"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("<< /Type /EmbeddedFile /Subtype /text#2Fplain /Params << /Size 11 >> /Length 11 >>"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Type /Filespec /F (z-notes.txt) /UF (z-notes.txt) /EF << /F 5 0 R /UF 5 0 R >> /Desc (Notes \\(draft\\))"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("<< /Type /EmbeddedFile /Subtype /application#2Fjson /Params << /Size 2 >> /Length 2 >>"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/Type /Filespec /F (a-data.json) /UF (a-data.json) /EF << /F 7 0 R /UF 7 0 R >>"));
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 11 /Root 1 0 R >>"));
    }

    [Test]
    public void Save_WritesAttachmentStreamBytesWithoutChangingContent()
    {
        PdfDocument document = new PdfDocument();
        byte[] data = new byte[] { 0, 1, 2, 65, 66, 255 };

        document.AddPage(200, 100);
        document.AddAttachmentWithMimeType("raw.bin", data, "application/octet-stream");

        byte[] pdfBytes = document.ToBytes();
        byte[] streamBytes = ExtractStreamBytes(pdfBytes, 5);

        Assert.That(streamBytes, Is.EqualTo(data));
    }

    [Test]
    public void Save_WritesOptionalMimeMetadataAndEveryAssociatedFileRelationship()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 100);
        document.AddAttachment("untyped.bin", new byte[] { 0 });
        document.AddAssociatedFile("source.xml", new byte[] { 1 }, "text/xml", "Source", PdfAssociatedFileRelationship.Source);
        document.AddAssociatedFile("data.json", new byte[] { 2 }, "application/json", "Data", PdfAssociatedFileRelationship.Data);
        document.AddAssociatedFile("alternative.pdf", new byte[] { 3 }, "application/pdf", "Alternative", PdfAssociatedFileRelationship.Alternative);
        document.AddAssociatedFile("supplement.txt", new byte[] { 4 }, "text/plain", "Supplement", PdfAssociatedFileRelationship.Supplement);
        document.AddAssociatedFile("other.bin", new byte[] { 5 }, "application/octet-stream", "Other", PdfAssociatedFileRelationship.Unspecified);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/Type /EmbeddedFile /Params << /Size 1 >>"));
        Assert.That(pdf, Does.Contain("/Subtype /text#2Fxml"));
        Assert.That(pdf, Does.Contain("/Subtype /application#2Fjson"));
        Assert.That(pdf, Does.Contain("/Subtype /application#2Fpdf"));
        Assert.That(pdf, Does.Contain("/Subtype /text#2Fplain"));
        Assert.That(pdf, Does.Contain("/Subtype /application#2Foctet-stream"));
        Assert.That(pdf, Does.Contain("/AFRelationship /Source"));
        Assert.That(pdf, Does.Contain("/AFRelationship /Data"));
        Assert.That(pdf, Does.Contain("/AFRelationship /Alternative"));
        Assert.That(pdf, Does.Contain("/AFRelationship /Supplement"));
        Assert.That(pdf, Does.Contain("/AFRelationship /Unspecified"));
    }

    [Test]
    public void Save_WritesUnicodeAttachmentNamesAndDescriptionsAsUtf16TextStrings()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 100);
        document.AddAttachmentWithMetadata("r\u00e9sum\u00e9.txt", new byte[] { 1 }, "text/plain", "caf\u00e9");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("<FEFF007200E900730075006D00E9002E007400780074>"));
        Assert.That(pdf, Does.Contain("/Desc <FEFF00630061006600E9>"));
    }

    [Test]
    public void Save_EncryptsAttachmentStreamBytes()
    {
        PdfDocument document = new PdfDocument();
        byte[] data = Encoding.ASCII.GetBytes("secret attachment");

        document.AddPage(200, 100);
        document.AddAttachmentWithMimeType("secret.txt", data, "text/plain");
        document.UseDeterministicWriterModeWithId("00112233445566778899aabbccddeeff");
        document.UseStandardEncryption("user", "owner");

        byte[] pdfBytes = document.ToBytes();
        string pdf = Encoding.ASCII.GetString(pdfBytes);
        byte[] streamBytes = ExtractStreamBytes(pdfBytes, 5);

        Assert.That(streamBytes, Is.Not.EqualTo(data));
        Assert.That(pdf, Does.Not.Contain("secret attachment"));
        Assert.That(pdf, Does.Contain("/Encrypt 7 0 R"));
    }

    [Test]
    public void AttachmentsRejectInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        byte[] data = new byte[] { 1 };

        Assert.Throws<ArgumentNullException>(() => new PdfAttachment(null!, data, "text/plain", "desc"));
        Assert.Throws<ArgumentException>(() => new PdfAttachment("", data, "text/plain", "desc"));
        Assert.Throws<ArgumentNullException>(() => new PdfAttachment("file.txt", null!, "text/plain", "desc"));
        Assert.Throws<ArgumentNullException>(() => new PdfAttachment("file.txt", data, null!, "desc"));
        Assert.Throws<ArgumentNullException>(() => new PdfAttachment("file.txt", data, "text/plain", null!));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachment(null!, data));
        Assert.Throws<ArgumentException>(() => document.AddAttachment("", data));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachment("file.txt", null!));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachmentWithMimeType("file.txt", data, null!));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachmentWithMetadata("file.txt", data, "text/plain", null!));

        document.AddAttachment("file.txt", data);
        Assert.Throws<ArgumentException>(() => document.AddAttachment("file.txt", data));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }

    static byte[] ExtractStreamBytes(byte[] pdfBytes, int objectId)
    {
        string pdf = Encoding.ASCII.GetString(pdfBytes);
        string objectStartMarker = objectId.ToString() + " 0 obj\n";
        string streamStartMarker = "stream\n";
        string streamEndMarker = "endstream";
        int objectStart = pdf.IndexOf(objectStartMarker, StringComparison.Ordinal);
        int streamStart = pdf.IndexOf(streamStartMarker, objectStart, StringComparison.Ordinal);
        int streamEnd = pdf.IndexOf(streamEndMarker, streamStart, StringComparison.Ordinal);

        Assert.That(objectStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(streamStart, Is.GreaterThan(objectStart));
        Assert.That(streamEnd, Is.GreaterThan(streamStart));

        streamStart += streamStartMarker.Length;

        byte[] streamBytes = new byte[streamEnd - streamStart];
        Buffer.BlockCopy(pdfBytes, streamStart, streamBytes, 0, streamBytes.Length);
        return streamBytes;
    }
}
