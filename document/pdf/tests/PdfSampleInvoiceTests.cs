using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfSampleInvoiceTests
{
    [Test]
    public void SampleInvoice_BuildDocumentIncludesPartiesLineItemsTaxesTotalsTermsNotesAndLogo()
    {
        PdfDocument document = SampleInvoice.BuildDocument();

        string content = AllPageContent(document);
        Assert.That(document.Pages.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(document.Images.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(INVOICE) Tj"));
        Assert.That(content, Does.Contain("(INV-2026-001) Tj"));
        Assert.That(content, Does.Contain("(Acme Supply Co.) Tj"));
        Assert.That(content, Does.Contain("(Contoso Retail) Tj"));
        Assert.That(content, Does.Contain("(Ship To) Tj"));
        Assert.That(content, Does.Contain("(Contoso Retail Warehouse) Tj"));
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content, Does.Contain("(Implementation package) Tj"));
        Assert.That(content, Does.Contain("(Support retainer) Tj"));
        Assert.That(content, Does.Contain("(Training session) Tj"));
        Assert.That(content, Does.Contain("(8%) Tj"));
        Assert.That(content, Does.Contain("($2200.00) Tj"));
        Assert.That(content, Does.Contain("($50.00) Tj"));
        Assert.That(content, Does.Contain("($172.00) Tj"));
        Assert.That(content, Does.Contain("($2322.00) Tj"));
        Assert.That(content, Does.Contain("(Payment Terms) Tj"));
        Assert.That(content, Does.Contain("(Remittance) Tj"));
        Assert.That(content, Does.Contain("(Notes) Tj"));
        Assert.That(content, Does.Contain("(Page 1 of "));
    }

    [Test]
    public void SampleInvoice_GenerateWritesPdfFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "pdf-sample-invoice-test.pdf");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        try
        {
            SampleInvoice.Generate(path);

            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(1000));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void SampleInvoice_RejectsNullOutputPath()
    {
        Assert.Throws<ArgumentNullException>(() => SampleInvoice.Generate(null!));
    }

    static string AllPageContent(PdfDocument document)
    {
        string content = string.Empty;
        for (int index = 0; index < document.Pages.Count; index++)
        {
            content += document.Pages[index].Canvas.ToPdfString();
        }

        return content;
    }
}
