using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfMetadataTests
{
    [Test]
    public void MetadataInterface_SettersWriteCompleteInfoDictionary()
    {
        PdfMetadata metadata = new PdfMetadata();
        IPdfMetadata metadataApi = metadata;
        metadataApi.SetTitle("Annual (Q1) \\ Draft");
        metadataApi.SetAuthor("Finance Team");
        metadataApi.SetSubject("Board report");
        metadataApi.SetKeywords("revenue, forecast");
        metadataApi.SetCreator("source application");
        metadataApi.SetProducer("pdf library");
        metadataApi.SetCreationDate(new DateTime(2026, 8, 25, 14, 30, 45, DateTimeKind.Utc));
        metadataApi.SetModificationDate(new DateTime(2026, 8, 26, 9, 5, 7, DateTimeKind.Unspecified));
        PdfDocument document = new PdfDocument();
        document.SetMetadata(metadata);
        document.AddPage(200, 100);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(metadataApi.HasValues(), Is.True);
        Assert.That(pdf, Does.Contain("/Title (Annual \\(Q1\\) \\\\ Draft)"));
        Assert.That(pdf, Does.Contain("/Author (Finance Team)"));
        Assert.That(pdf, Does.Contain("/Subject (Board report)"));
        Assert.That(pdf, Does.Contain("/Keywords (revenue, forecast)"));
        Assert.That(pdf, Does.Contain("/Creator (source application)"));
        Assert.That(pdf, Does.Contain("/Producer (pdf library)"));
        Assert.That(pdf, Does.Contain("/CreationDate (D:20260825143045Z)"));
        Assert.That(pdf, Does.Contain("/ModDate (D:20260826090507)"));
        Assert.That(pdf, Does.Contain(" /Info "));
    }

    [Test]
    public void SetXmpMetadata_WritesUtf8MetadataStreamAndCatalogReference()
    {
        string xmp = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">Cafe π</x:xmpmeta>";
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmp);
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetXmpMetadata(xmp);
        PdfDocument document = new PdfDocument();
        document.SetMetadata(metadata);
        document.AddPage(200, 100);

        byte[] pdfBytes = document.ToBytes();
        string pdfText = Encoding.ASCII.GetString(pdfBytes);

        Assert.That(pdfText, Does.Contain("/Metadata 5 0 R"));
        Assert.That(pdfText, Does.Contain("5 0 obj\n<< /Type /Metadata /Subtype /XML /Length " + xmpBytes.Length.ToString() + " >>\nstream\n"));
        Assert.That(ContainsSequence(pdfBytes, xmpBytes), Is.True);
        Assert.That(pdfText, Does.Not.Contain(" /Info "));
    }

    [Test]
    public void EmptyMetadataValuesAreOmitted()
    {
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle(string.Empty);
        metadata.SetAuthor(string.Empty);
        metadata.SetSubject(string.Empty);
        metadata.SetKeywords(string.Empty);
        metadata.SetCreator(string.Empty);
        metadata.SetProducer(string.Empty);
        metadata.SetXmpMetadata(string.Empty);
        PdfDocument document = new PdfDocument();
        document.SetMetadata(metadata);
        document.AddPage(200, 100);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(metadata.HasValues(), Is.False);
        Assert.That(pdf, Does.Not.Contain(" /Info "));
        Assert.That(pdf, Does.Not.Contain("/Metadata"));
    }

    [Test]
    public void MetadataTextSettersRejectNull()
    {
        IPdfMetadata metadata = new PdfMetadata();

        Assert.Throws<ArgumentNullException>(() => metadata.SetTitle(null!));
        Assert.Throws<ArgumentNullException>(() => metadata.SetAuthor(null!));
        Assert.Throws<ArgumentNullException>(() => metadata.SetSubject(null!));
        Assert.Throws<ArgumentNullException>(() => metadata.SetKeywords(null!));
        Assert.Throws<ArgumentNullException>(() => metadata.SetCreator(null!));
        Assert.Throws<ArgumentNullException>(() => metadata.SetProducer(null!));
        Assert.Throws<ArgumentNullException>(() => metadata.SetXmpMetadata(null!));
    }

    static bool ContainsSequence(byte[] data, byte[] sequence)
    {
        if (sequence.Length == 0)
        {
            return true;
        }

        for (int index = 0; index <= data.Length - sequence.Length; index++)
        {
            bool matches = true;
            for (int sequenceIndex = 0; sequenceIndex < sequence.Length; sequenceIndex++)
            {
                if (data[index + sequenceIndex] != sequence[sequenceIndex])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }
}
