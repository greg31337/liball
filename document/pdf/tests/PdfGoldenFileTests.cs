using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfGoldenFileTests
{
    [Test]
    public void SampleReport_MatchesDeterministicGoldenHash()
    {
        AssertGoldenHash("sample-report.sha256", SampleReport.BuildDocument(), "10000000000000000000000000000001");
    }

    [Test]
    public void SampleInvoice_MatchesDeterministicGoldenHash()
    {
        AssertGoldenHash("sample-invoice.sha256", SampleInvoice.BuildDocument(), "10000000000000000000000000000002");
    }

    [Test]
    public void SamplePaper_MatchesDeterministicGoldenHash()
    {
        AssertGoldenHash("sample-paper.sha256", SamplePaper.BuildDocument(), "10000000000000000000000000000003");
    }

    static void AssertGoldenHash(string goldenFileName, PdfDocument document, string documentId)
    {
        string expectedHash = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tests", "GoldenFiles", goldenFileName)).Trim();

        document.UseDeterministicWriterModeWithId(documentId);
        byte[] bytes = document.ToBytes();
        string actualHash = Convert.ToHexString(SHA256.HashData(bytes));
        string pdf = Encoding.ASCII.GetString(bytes);

        Assert.That(actualHash, Is.EqualTo(expectedHash));
        Assert.That(pdf, Does.Contain("/ID [<" + documentId + "><" + documentId + ">]"));
    }

    static string FindRepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "pdf.slnx")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
