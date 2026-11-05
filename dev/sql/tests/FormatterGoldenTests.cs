using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FormatterGoldenTests
{
    [Test]
    public void SqlFormatter_MatchesGoldenFiles()
    {
        string root = FindRepositoryRoot();
        string goldenDirectory = Path.Combine(root, "tests", "Golden", "Formatter");
        string[] inputFiles = Directory.GetFiles(goldenDirectory, "*.input.sql");
        Array.Sort(inputFiles);

        Assert.That(inputFiles.Length, Is.GreaterThan(0));
        foreach (string inputFile in inputFiles)
        {
            string expectedFile = inputFile.Replace(".input.sql", ".expected.sql");
            Assert.That(File.Exists(expectedFile), Is.True, "Missing expected file for " + inputFile);

            string input = File.ReadAllText(inputFile);
            string expected = NormalizeGolden(File.ReadAllText(expectedFile));
            string formatted = NormalizeGolden(SqlFormatter.Format(input));

            Assert.That(formatted, Is.EqualTo(expected), Path.GetFileName(inputFile));
        }
    }

    static string NormalizeGolden(string text)
    {
        string normalized = text.Replace("\r\n", "\n");
        normalized = normalized.Replace("\r", "\n");
        while (normalized.EndsWith("\n"))
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }

        return normalized;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "sql.slnx")) && Directory.Exists(Path.Combine(directory, "tests")))
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
