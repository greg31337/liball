using System.Diagnostics;
using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeDataManifestTests
{
    [Test]
    public void UnicodeDataReadme_PinsDownloadAndVerificationInstructions()
    {
        string root = FindRepositoryRoot();
        string readme = File.ReadAllText(Path.Combine(root, "data", "Unicode17.0.0", "README.md"));

        Assert.That(readme, Does.Contain("https://www.unicode.org/Public/17.0.0/ucd/"));
        Assert.That(readme, Does.Contain("https://www.unicode.org/Public/17.0.0/emoji/"));
        Assert.That(readme, Does.Contain("https://www.unicode.org/Public/17.0.0/security/"));
        Assert.That(readme, Does.Contain("checksums.sha256"));
        Assert.That(readme, Does.Contain("curl -fsSL"));
        Assert.That(readme, Does.Contain("shasum -a 256 -c checksums.sha256"));
        Assert.That(readme, Does.Contain("pinned Unicode"));
    }

    [Test]
    public void ChecksumManifest_ContainsValidSha256Entries()
    {
        string root = FindRepositoryRoot();
        ManifestEntry[] entries = ReadManifest(root);

        Assert.That(entries, Has.Length.EqualTo(37));

        foreach (ManifestEntry entry in entries)
        {
            Assert.That(entry.Hash, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(entry.FileName, Does.EndWith(".txt"));
            Assert.That(entry.FileName, Does.Not.Contain(".."));
            Assert.That(entry.FileName, Does.Not.Contain("\\"));
        }
    }

    [Test]
    public void ChecksumManifest_MatchesGeneratorRequiredFileList()
    {
        string root = FindRepositoryRoot();
        ManifestEntry[] entries = ReadManifest(root);
        string[] manifestFileNames = entries.Select(static entry => entry.FileName).ToArray();
        string[] generatorFileNames = RunGeneratorList(root)
            .Select(static line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
            .ToArray();

        Assert.That(manifestFileNames, Is.EqualTo(generatorFileNames));
    }

    [Test]
    public void ChecksumManifest_PinsKnownUnicodeDataChecksum()
    {
        string root = FindRepositoryRoot();
        ManifestEntry[] entries = ReadManifest(root);
        ManifestEntry unicodeData = entries.Single(static entry => entry.FileName == "UnicodeData.txt");
        ManifestEntry arabicShaping = entries.Single(static entry => entry.FileName == "ArabicShaping.txt");
        ManifestEntry emojiData = entries.Single(static entry => entry.FileName == "emoji/emoji-data.txt");
        ManifestEntry confusables = entries.Single(static entry => entry.FileName == "security/confusables.txt");

        Assert.That(unicodeData.Hash, Is.EqualTo("2e1efc1dcb59c575eedf5ccae60f95229f706ee6d031835247d843c11d96470c"));
        Assert.That(arabicShaping.Hash, Is.EqualTo("39afa01e680e27d0fd10b67a9b27be13fbaa3d0efecfb5be45991de9a0d267d0"));
        Assert.That(emojiData.Hash, Is.EqualTo("2cb2bb9455cda83e8481541ecf5b6dfda66a3bb89efa3fa7c5297eccf607b72b"));
        Assert.That(confusables.Hash, Is.EqualTo("091c7f82fc39ef208faf8f94d29c244de99254675e09de163160c810d13ef22a"));
    }

    static ManifestEntry[] ReadManifest(string root)
    {
        string path = Path.Combine(root, "data", "Unicode17.0.0", "checksums.sha256");
        List<ManifestEntry> entries = new List<ManifestEntry>();

        foreach (string line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(parts, Has.Length.EqualTo(2), line);
            entries.Add(new ManifestEntry(parts[0], parts[1]));
        }

        return entries.ToArray();
    }

    static string[] RunGeneratorList(string root)
    {
        string projectPath = Path.Combine(root, "tools", "UnicodeDataGenerator", "UnicodeDataGenerator.csproj");
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "run --project \"" + projectPath + "\" -- list");
        startInfo.WorkingDirectory = root;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using Process process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        bool exited = process.WaitForExit(30000);

        Assert.That(exited, Is.True, "Generator did not exit within timeout.");
        Assert.That(process.ExitCode, Is.EqualTo(0), error);
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
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

public sealed record ManifestEntry(string Hash, string FileName);
