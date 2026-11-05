using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfSamplePaperTests
{
    [Test]
    public void SamplePaper_BuildDocumentIncludesTitleAbstractSectionsFiguresTablesCitationsBibliographyAndAppendix()
    {
        PdfDocument document = SamplePaper.BuildDocument();

        string content = AllPageContent(document);
        Assert.That(document.Pages.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(document.Images.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(Compact PDF Helper Architecture) Tj"));
        Assert.That(content, Does.Contain("(Ada Lovelace, Analytical Institute) Tj"));
        Assert.That(content, Does.Contain("(Grace Hopper, Compiler Laboratory) Tj"));
        Assert.That(content, Does.Contain("(Abstract) Tj"));
        Assert.That(content, Does.Contain("(Keywords: PDF helpers) Tj"));
        Assert.That(content, Does.Contain("(Introduction) Tj"));
        Assert.That(content, Does.Contain("(Layout Model) Tj"));
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content, Does.Contain("(Figure 1. Synthetic layout pipeline diagram.) Tj"));
        Assert.That(content, Does.Contain("(Helper) Tj"));
        Assert.That(content, Does.Contain("(PdfBibliographyEntry) Tj"));
        Assert.That(content, Does.Contain("(Table 1. Helper responsibilities.) Tj"));
        Assert.That(content, Does.Contain("(Citations) Tj"));
        Assert.That(content, Does.Contain("([L1] Lovelace described structured algorithmic notes.) Tj"));
        Assert.That(content, Does.Contain("(Bibliography) Tj"));
        Assert.That(content, Does.Contain("([H1] Hopper, G. The Education of a Computer. Proceedings ACM, 1952.) Tj"));
        Assert.That(content, Does.Contain("(Appendix: Implementation Notes) Tj"));
        Assert.That(content, Does.Contain("(Page 1 of "));
    }

    [Test]
    public void SamplePaper_GenerateWritesPdfFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "pdf-sample-paper-test.pdf");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        try
        {
            SamplePaper.Generate(path);

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
    public void SamplePaper_RejectsNullOutputPath()
    {
        Assert.Throws<ArgumentNullException>(() => SamplePaper.Generate(null!));
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
