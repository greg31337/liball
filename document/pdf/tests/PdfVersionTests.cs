using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfVersionTests
{
    [Test]
    public void DefaultVersionWritesPdf14HeaderAndCatalogVersion()
    {
        PdfDocument document = CreateDocument();

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.StartWith("%PDF-1.4\n"));
        Assert.That(pdf, Does.Contain("/Type /Catalog /Pages 2 0 R /Version /1.4"));
    }

    [TestCase(PdfVersion.Pdf14, "1.4")]
    [TestCase(PdfVersion.Pdf15, "1.5")]
    [TestCase(PdfVersion.Pdf16, "1.6")]
    [TestCase(PdfVersion.Pdf17, "1.7")]
    [TestCase(PdfVersion.Pdf20, "2.0")]
    public void SetPdfVersionWritesSelectedHeaderAndCatalogVersion(PdfVersion version, string versionText)
    {
        IPdfDocument document = CreateDocument();
        document.SetPdfVersion(version);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.StartWith("%PDF-" + versionText + "\n"));
        Assert.That(pdf, Does.Contain("/Type /Catalog /Pages 2 0 R /Version /" + versionText));
    }

    [Test]
    public void WriterOptionsCanSelectVersion()
    {
        PdfWriterOptions options = new PdfWriterOptions();
        options.Version = PdfVersion.Pdf16;
        PdfDocument document = CreateDocument();

        document.SetWriterOptions(options);
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.StartWith("%PDF-1.6\n"));
        Assert.That(pdf, Does.Contain("/Version /1.6"));
    }

    [Test]
    public void UnsupportedVersionIsRejectedWhenConfiguredOrSaved()
    {
        PdfDocument document = CreateDocument();

        Assert.Throws<ArgumentException>(() => document.SetPdfVersion((PdfVersion)99));

        PdfWriterOptions options = new PdfWriterOptions();
        options.Version = (PdfVersion)99;
        Assert.Throws<ArgumentException>(() => document.SetWriterOptions(options));

        PdfWriterOptions mutableOptions = new PdfWriterOptions();
        document.SetWriterOptions(mutableOptions);
        mutableOptions.Version = (PdfVersion)99;
        Assert.Throws<ArgumentException>(() => document.ToBytes());
    }

    [Test]
    public void Pdf20RejectsLegacyEncryptionInEitherConfigurationOrder()
    {
        PdfDocument versionFirst = CreateDocument();
        versionFirst.SetPdfVersion(PdfVersion.Pdf20);

        Assert.Throws<InvalidOperationException>(() => versionFirst.UseStandardEncryption("user", "owner"));
        Assert.That(versionFirst.HasEncryption, Is.False);

        PdfDocument encryptionFirst = CreateDocument();
        encryptionFirst.UseStandardEncryption("user", "owner");

        Assert.Throws<InvalidOperationException>(() => encryptionFirst.SetPdfVersion(PdfVersion.Pdf20));
        Assert.That(encryptionFirst.WriterOptions.Version, Is.EqualTo(PdfVersion.Pdf14));
    }

    [Test]
    public void SaveRevalidatesMutableWriterOptionsAgainstEnabledFeatures()
    {
        PdfWriterOptions options = new PdfWriterOptions();
        PdfDocument document = CreateDocument();
        document.SetWriterOptions(options);
        document.UseStandardEncryption("user", "owner");

        options.Version = PdfVersion.Pdf20;

        Assert.Throws<InvalidOperationException>(() => document.ToBytes());
    }

    static PdfDocument CreateDocument()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(200, 100);
        return document;
    }
}
