using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfDocumentTests
{
    [Test]
    public void AddPage_UsesLetterSizeAndAddsPage()
    {
        PdfDocument document = new PdfDocument();

        PdfPage page = document.AddLetterPage();

        Assert.That(page.Width, Is.EqualTo(PdfPageSize.LetterWidth));
        Assert.That(page.Height, Is.EqualTo(PdfPageSize.LetterHeight));
        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(document.Pages[0], Is.SameAs(page));
    }

    [Test]
    public void AddImage_ValidatesDimensionsAndDataLength()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddImage(0, 1, new byte[3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddImage(1, 0, new byte[3]));
        Assert.Throws<ArgumentNullException>(() => document.AddImage(1, 1, null!));
        Assert.Throws<ArgumentException>(() => document.AddImage(1, 1, new byte[2]));
    }

    [Test]
    public void AddImage_AssignsStableResourceNames()
    {
        PdfDocument document = new PdfDocument();

        PdfImage first = document.AddImage(1, 1, new byte[] { 255, 0, 0 });
        PdfImage second = document.AddImage(1, 1, new byte[] { 0, 255, 0 });

        Assert.That(first.Name, Is.EqualTo("Im1"));
        Assert.That(second.Name, Is.EqualTo("Im2"));
        Assert.That(document.Images.Count, Is.EqualTo(2));
        Assert.That(document.Images[0], Is.SameAs(first));
        Assert.That(document.Images[1], Is.SameAs(second));
    }

    [Test]
    public void AddAttachment_StoresMetadataAndCopiesData()
    {
        PdfDocument document = new PdfDocument();
        byte[] data = new byte[] { 1, 2, 3 };

        PdfAttachment attachment = document.AddAttachmentWithMetadata("notes.txt", data, "text/plain", "Review notes");
        data[0] = 9;

        Assert.That(attachment.FileName, Is.EqualTo("notes.txt"));
        Assert.That(attachment.MimeType, Is.EqualTo("text/plain"));
        Assert.That(attachment.Description, Is.EqualTo("Review notes"));
        Assert.That(attachment.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(document.Attachments.Count, Is.EqualTo(1));
        Assert.That(document.Attachments[0], Is.SameAs(attachment));
    }

    [Test]
    public void AddAttachment_ValidatesInputsAndDuplicateFileNames()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentNullException>(() => document.AddAttachment(null!, new byte[] { 1 }));
        Assert.Throws<ArgumentException>(() => document.AddAttachment(string.Empty, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachment("a.txt", null!));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachmentWithMimeType("a.txt", new byte[] { 1 }, null!));
        Assert.Throws<ArgumentNullException>(() => document.AddAttachmentWithMetadata("a.txt", new byte[] { 1 }, "text/plain", null!));

        document.AddAttachment("a.txt", new byte[] { 1 });

        Assert.Throws<ArgumentException>(() => document.AddAttachment("a.txt", new byte[] { 2 }));
    }

    [Test]
    public void AddAttachment_AllowsEmptyAttachmentData()
    {
        PdfDocument document = new PdfDocument();

        PdfAttachment attachment = document.AddAttachment("empty.bin", new byte[0]);

        Assert.That(attachment.Data.Length, Is.EqualTo(0));
    }

    [Test]
    public void Canvas_WritesEscapedTextAndMeasuresSimpleText()
    {
        PdfCanvas canvas = new PdfCanvas();

        canvas.DrawTextWithFont("A (test) \\ value", 10, 20, 12, PdfFont.Bold);

        string content = canvas.ToPdfString();
        Assert.That(content, Does.Contain("/F2 12 Tf"));
        Assert.That(content, Does.Contain("10 20 Td"));
        Assert.That(content, Does.Contain("(A \\(test\\) \\\\ value) Tj"));
        Assert.That(PdfCanvas.MeasureText("A 1", 10), Is.EqualTo(15.01).Within(0.001));
        Assert.That(PdfCanvas.MeasureText("ABC", 12, PdfFont.Courier), Is.EqualTo(21.6).Within(0.001));
    }

    [Test]
    public void Save_WritesPdfStructureAndImageData()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "generated-pdfs");
        string path = Path.Combine(directory, "document-test-" + Guid.NewGuid().ToString("N") + ".pdf");
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfImage image = document.AddImage(2, 1, new byte[] { 255, 0, 0, 0, 255, 0 });

        page.Canvas.DrawTextWithFont("Hello (PDF) \\ world", 10, 20, 12, PdfFont.Bold);
        page.Canvas.DrawImage(image, 10, 30, 20, 10);

        document.Save(path);

        string pdfText = File.ReadAllText(path, Encoding.ASCII);
        Assert.That(pdfText, Does.StartWith("%PDF-1.4"));
        Assert.That(pdfText, Does.Contain("/Type /Catalog"));
        Assert.That(pdfText, Does.Contain("/Type /Pages /Count 1"));
        Assert.That(pdfText, Does.Contain("/MediaBox [0 0 200 100]"));
        Assert.That(pdfText, Does.Contain("/BaseFont /Helvetica-Bold"));
        Assert.That(pdfText, Does.Contain("/Im1"));
        Assert.That(pdfText, Does.Contain("FF000000FF00>"));
        Assert.That(pdfText, Does.Contain("(Hello \\(PDF\\) \\\\ world) Tj"));
        Assert.That(pdfText, Does.Contain("xref"));
        Assert.That(pdfText, Does.Contain("trailer"));
        Assert.That(pdfText, Does.Contain("startxref"));
    }

    [Test]
    public void Save_WritesSameBytesToFileStreamAndByteArray()
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "generated-pdfs");
        string path = Path.Combine(directory, "document-test-" + Guid.NewGuid().ToString("N") + ".pdf");
        PdfDocument document = CreateSimpleDocument();

        document.Save(path);
        byte[] fileBytes = File.ReadAllBytes(path);

        byte[] streamBytes;
        using (MemoryStream stream = new MemoryStream())
        {
            document.SaveToStream(stream);
            streamBytes = stream.ToArray();
        }

        byte[] directBytes = document.ToBytes();

        Assert.That(streamBytes, Is.EqualTo(fileBytes));
        Assert.That(directBytes, Is.EqualTo(fileBytes));
        Assert.That(Encoding.ASCII.GetString(directBytes), Does.StartWith("%PDF-1.4"));
    }

    [Test]
    public void Save_ThrowsWhenDocumentHasNoPages()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<InvalidOperationException>(() => document.Save("empty.pdf"));
    }

    [Test]
    public void SaveStreamAndToBytes_ThrowWhenDocumentHasNoPages()
    {
        PdfDocument document = new PdfDocument();

        using (MemoryStream stream = new MemoryStream())
        {
            Assert.Throws<InvalidOperationException>(() => document.SaveToStream(stream));
        }

        Assert.Throws<InvalidOperationException>(() => document.ToBytes());
    }

    static PdfDocument CreateSimpleDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(144, 72);
        PdfImage image = document.AddImage(1, 1, new byte[] { 10, 20, 30 });

        page.Canvas.DrawTextWithFont("Stream output", 12, 24, 10, PdfFont.Bold);
        page.Canvas.DrawImage(image, 40, 10, 20, 20);

        return document;
    }
}
