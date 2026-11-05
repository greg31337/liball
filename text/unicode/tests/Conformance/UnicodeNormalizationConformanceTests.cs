using System.Text;
using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationConformanceTests
{
    [Test]
    public void Unicode17NormalizationTest_MatchesAllNormalizationForms()
    {
        string path = Path.Combine(FindRepositoryRoot(), "data", "Unicode17.0.0", "NormalizationTest.txt");
        UnicodeNormalizationTestRecord[] records = UnicodeDataParser.ReadNormalizationTest(path);

        Assert.That(records, Has.Length.EqualTo(20034));

        int index = 0;
        while (index < records.Length)
        {
            AssertRecord(records[index]);
            index = index + 1;
        }
    }

    static void AssertRecord(UnicodeNormalizationTestRecord record)
    {
        UnicodeNormalization normalization = new UnicodeNormalization();
        string c1 = ToText(record.Source);
        string c2 = ToText(record.Nfc);
        string c3 = ToText(record.Nfd);
        string c4 = ToText(record.Nfkc);
        string c5 = ToText(record.Nfkd);

        Assert.That(normalization.ToNfc(c1), Is.EqualTo(c2), record.Comment + " NFC(c1)");
        Assert.That(normalization.ToNfc(c2), Is.EqualTo(c2), record.Comment + " NFC(c2)");
        Assert.That(normalization.ToNfc(c3), Is.EqualTo(c2), record.Comment + " NFC(c3)");
        Assert.That(normalization.ToNfc(c4), Is.EqualTo(c4), record.Comment + " NFC(c4)");
        Assert.That(normalization.ToNfc(c5), Is.EqualTo(c4), record.Comment + " NFC(c5)");

        Assert.That(normalization.ToNfd(c1), Is.EqualTo(c3), record.Comment + " NFD(c1)");
        Assert.That(normalization.ToNfd(c2), Is.EqualTo(c3), record.Comment + " NFD(c2)");
        Assert.That(normalization.ToNfd(c3), Is.EqualTo(c3), record.Comment + " NFD(c3)");
        Assert.That(normalization.ToNfd(c4), Is.EqualTo(c5), record.Comment + " NFD(c4)");
        Assert.That(normalization.ToNfd(c5), Is.EqualTo(c5), record.Comment + " NFD(c5)");

        Assert.That(normalization.ToNfkc(c1), Is.EqualTo(c4), record.Comment + " NFKC(c1)");
        Assert.That(normalization.ToNfkc(c2), Is.EqualTo(c4), record.Comment + " NFKC(c2)");
        Assert.That(normalization.ToNfkc(c3), Is.EqualTo(c4), record.Comment + " NFKC(c3)");
        Assert.That(normalization.ToNfkc(c4), Is.EqualTo(c4), record.Comment + " NFKC(c4)");
        Assert.That(normalization.ToNfkc(c5), Is.EqualTo(c4), record.Comment + " NFKC(c5)");

        Assert.That(normalization.ToNfkd(c1), Is.EqualTo(c5), record.Comment + " NFKD(c1)");
        Assert.That(normalization.ToNfkd(c2), Is.EqualTo(c5), record.Comment + " NFKD(c2)");
        Assert.That(normalization.ToNfkd(c3), Is.EqualTo(c5), record.Comment + " NFKD(c3)");
        Assert.That(normalization.ToNfkd(c4), Is.EqualTo(c5), record.Comment + " NFKD(c4)");
        Assert.That(normalization.ToNfkd(c5), Is.EqualTo(c5), record.Comment + " NFKD(c5)");
    }

    static string ToText(int[] codePoints)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < codePoints.Length)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
