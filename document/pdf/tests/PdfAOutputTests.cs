using System;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAOutputTests
{
    [Test]
    public void PrepareForPdfA3B_EmitsIdentificationOutputIntentEmbeddedFontAndAssociatedFile()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Archive & Records");
        metadata.SetAuthor("Ada Lovelace");
        metadata.SetSubject("Long-term records");
        metadata.SetKeywords("archive, records");
        metadata.SetCreator("Records Tool");
        metadata.SetProducer("pdf library");
        metadata.SetCreationDate(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        document.SetMetadata(metadata);
        PdfPage page = document.AddPage(200, 100);
        PdfTrueTypeFont font = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
        page.GetCanvas().DrawTextWithTrueTypeFont("AC", 10, 20, 12, font);
        PdfImage image = document.AddImage(1, 1, new byte[] { 10, 20, 30 });
        page.GetCanvas().DrawImage(image, 40, 20, 10, 10);
        document.AddAssociatedFile("source.xml", Encoding.UTF8.GetBytes("<data/>"), "text/xml", "Source data", PdfAssociatedFileRelationship.Data);
        PdfAPreparationOptions options = CreateOptions(PdfAConformance.PdfA3B, "Custom RGB");

        document.PrepareForPdfA(options);
        options.IccProfileData[40] = 99;
        string pdf = Encoding.Latin1.GetString(document.ToBytes());

        Assert.Multiple(() =>
        {
            Assert.That(document.IsPreparedForPdfA(), Is.True);
            Assert.That(document.GetPdfAConformance(), Is.EqualTo(PdfAConformance.PdfA3B));
            Assert.That(document.WriterOptions.Version, Is.EqualTo(PdfVersion.Pdf17));
            Assert.That(document.PdfAOptions.IccProfileData[40], Is.EqualTo(0));
            Assert.That(pdf, Does.StartWith("%PDF-1.7"));
            Assert.That(pdf, Does.Contain("/OutputIntents ["));
            Assert.That(pdf, Does.Contain("/Type /OutputIntent /S /GTS_PDFA1"));
            Assert.That(pdf, Does.Contain("/DestOutputProfile "));
            Assert.That(pdf, Does.Contain("/N 3 /Length 128"));
            Assert.That(pdf, Does.Contain("pdfaid:part=\"3\" pdfaid:conformance=\"B\""));
            Assert.That(pdf, Does.Contain("Archive &amp; Records"));
            Assert.That(pdf, Does.Contain("<rdf:li>Ada Lovelace</rdf:li>"));
            Assert.That(pdf, Does.Contain("pdf:Keywords=\"archive, records\""));
            Assert.That(pdf, Does.Contain("xmp:CreatorTool=\"Records Tool\""));
            Assert.That(pdf, Does.Contain("xmp:CreateDate=\"2026-01-02T03:04:05Z\""));
            Assert.That(pdf, Does.Contain("/FontFile2 "));
            Assert.That(pdf, Does.Not.Contain("/BaseFont /Helvetica"));
            Assert.That(pdf, Does.Contain("/ColorSpace /DeviceRGB"));
            Assert.That(pdf, Does.Contain("/Subtype /text#2Fxml"));
            Assert.That(pdf, Does.Contain("/AFRelationship /Data"));
            Assert.That(pdf, Does.Contain(" /AF ["));
            Assert.That(pdf, Does.Not.Contain("/Encrypt "));
        });

        PdfAPreparationResult result = document.CheckPdfAPreparation();
        Assert.That(result.HasIssue("PDFA_IMAGE_COLOR_PROFILE_REVIEW"), Is.False);
    }

    [Test]
    public void PrepareForPdfA1B_UsesPdf14AndPreventsLaterProhibitedFeatures()
    {
        PdfDocument document = CreateShapeDocument();
        document.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA1B, "Archive RGB"));

        string pdf = Encoding.Latin1.GetString(document.ToBytes());

        Assert.That(pdf, Does.StartWith("%PDF-1.4"));
        Assert.That(pdf, Does.Contain("pdfaid:part=\"1\" pdfaid:conformance=\"B\""));
        Assert.Throws<InvalidOperationException>(() => document.AddAttachmentWithMimeType("data.bin", new byte[] { 1 }, "application/octet-stream"));
        Assert.Throws<InvalidOperationException>(() => document.UseStandardEncryption("user", "owner"));
        Assert.Throws<InvalidOperationException>(() => document.SetPdfVersion(PdfVersion.Pdf17));

        document.GetPage(0).GetCanvas().DrawText("Base font", 1, 2, 10);
        Assert.Throws<InvalidOperationException>(() => document.ToBytes());
    }

    [Test]
    public void PrepareForPdfA_RejectsInvalidProfilesAndExistingProhibitedState()
    {
        PdfDocument invalidProfileDocument = CreateShapeDocument();
        PdfAPreparationOptions invalidProfile = CreateOptions(PdfAConformance.PdfA3B, "RGB");
        invalidProfile.IccProfileData[36] = 0;
        Assert.Throws<ArgumentException>(() => invalidProfileDocument.PrepareForPdfA(invalidProfile));

        PdfDocument standardFontDocument = CreateShapeDocument();
        standardFontDocument.GetPage(0).GetCanvas().DrawText("not embedded", 1, 2, 10);
        Assert.Throws<InvalidOperationException>(() => standardFontDocument.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA2B, "RGB")));

        PdfDocument encryptedDocument = CreateShapeDocument();
        encryptedDocument.UseStandardEncryption("user", "owner");
        Assert.Throws<InvalidOperationException>(() => encryptedDocument.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA2B, "RGB")));

        PdfDocument transparentDocument = CreateShapeDocument();
        transparentDocument.GetPage(0).GetCanvas().SetFillOpacity(0.5);
        Assert.Throws<InvalidOperationException>(() => transparentDocument.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA1B, "RGB")));

        PdfDocument mismatchedColorDocument = CreateShapeDocument();
        mismatchedColorDocument.GetPage(0).GetCanvas().SetFillColorValue(new PdfColor("CMYK", 0, 0, 0, 1));
        Assert.Throws<InvalidOperationException>(() => mismatchedColorDocument.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA2B, "RGB")));
    }

    [Test]
    public void PrepareForPdfA2B_RejectsEmbeddedFilesAndSplitPreservesPreparation()
    {
        PdfDocument withAttachment = CreateShapeDocument();
        withAttachment.AddAttachmentWithMimeType("data.bin", new byte[] { 1 }, "application/octet-stream");
        Assert.Throws<InvalidOperationException>(() => withAttachment.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA2B, "RGB")));

        PdfDocument source = new PdfDocument();
        source.AddPage(100, 100).GetCanvas().FillRectangle(1, 1, 10, 10);
        source.AddPage(100, 100).GetCanvas().FillRectangle(2, 2, 10, 10);
        source.PrepareForPdfA(CreateOptions(PdfAConformance.PdfA2B, "RGB"));

        PdfDocument[] parts = source.SplitDocument(1);

        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.That(parts[0].IsPreparedForPdfA(), Is.True);
        Assert.That(parts[1].GetPdfAConformance(), Is.EqualTo(PdfAConformance.PdfA2B));
        Assert.That(Encoding.Latin1.GetString(parts[0].ToBytes()), Does.Contain("pdfaid:part=\"2\""));
    }

    [Test]
    public void AddAssociatedFile_ValidatesMimeTypeAndRelationship()
    {
        PdfDocument document = CreateShapeDocument();

        Assert.Throws<ArgumentException>(() => document.AddAssociatedFile("data.bin", new byte[] { 1 }, string.Empty, string.Empty, PdfAssociatedFileRelationship.Data));
        Assert.Throws<ArgumentException>(() => document.AddAssociatedFile("data.bin", new byte[] { 1 }, "application/octet-stream", string.Empty, (PdfAssociatedFileRelationship)99));
        Assert.Throws<InvalidOperationException>(() => document.GetPdfAConformance());
    }

    static PdfDocument CreateShapeDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Archive");
        metadata.SetProducer("pdf library");
        document.SetMetadata(metadata);
        document.AddPage(100, 100).GetCanvas().FillRectangle(1, 2, 30, 40);
        return document;
    }

    static PdfAPreparationOptions CreateOptions(PdfAConformance conformance, string identifier)
    {
        PdfAPreparationOptions options = new PdfAPreparationOptions();
        options.Conformance = conformance;
        options.IccProfileData = CreateRgbIccProfile();
        options.OutputConditionIdentifier = identifier;
        options.OutputCondition = identifier;
        options.Info = identifier;
        return options;
    }

    static byte[] CreateRgbIccProfile()
    {
        byte[] profile = new byte[128];
        profile[3] = 128;
        profile[12] = (byte)'m';
        profile[13] = (byte)'n';
        profile[14] = (byte)'t';
        profile[15] = (byte)'r';
        profile[16] = (byte)'R';
        profile[17] = (byte)'G';
        profile[18] = (byte)'B';
        profile[19] = (byte)' ';
        profile[20] = (byte)'X';
        profile[21] = (byte)'Y';
        profile[22] = (byte)'Z';
        profile[23] = (byte)' ';
        profile[36] = (byte)'a';
        profile[37] = (byte)'c';
        profile[38] = (byte)'s';
        profile[39] = (byte)'p';
        return profile;
    }
}
