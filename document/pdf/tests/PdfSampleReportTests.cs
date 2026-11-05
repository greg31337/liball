using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfSampleReportTests
{
    [Test]
    public void SampleReport_BuildDocumentIncludesReportSectionsTablesFiguresAppendixAndFurniture()
    {
        PdfDocument document = SampleReport.BuildDocument();

        string content = AllPageContent(document);
        Assert.That(document.Pages.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(document.Images.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(Operational Health Report) Tj"));
        Assert.That(content, Does.Contain("(Executive Summary) Tj"));
        Assert.That(content, Does.Contain("(Annual Recurring Revenue) Tj"));
        Assert.That(content, Does.Contain("($42M) Tj"));
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content, Does.Contain("(Figure 1. Generated landscape used as a report figure with caption.) Tj"));
        Assert.That(content, Does.Contain("(Portfolio Status) Tj"));
        Assert.That(content, Does.Contain("(Quarter) Tj"));
        Assert.That(content, Does.Contain("(Package sample templates) Tj"));
        Assert.That(content, Does.Contain("(Appendix: Methodology) Tj"));
        Assert.That(content, Does.Contain("(Page 1 of "));
    }

    [Test]
    public void SampleReport_GenerateWritesPdfFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "pdf-sample-report-test.pdf");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        try
        {
            SampleReport.Generate(path);

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
    public void SampleReport_RejectsNullOutputPath()
    {
        Assert.Throws<ArgumentNullException>(() => SampleReport.Generate(null!));
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
