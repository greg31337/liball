using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeVersionTests
{
    [Test]
    public void UnicodeVersion_ExposesPinnedVersionMetadata()
    {
        Assert.That(UnicodeVersion.Major, Is.EqualTo(17));
        Assert.That(UnicodeVersion.Minor, Is.EqualTo(0));
        Assert.That(UnicodeVersion.Patch, Is.EqualTo(0));
        Assert.That(UnicodeVersion.Version, Is.EqualTo("17.0.0"));
        Assert.That(new UnicodeData().GetVersion(), Is.EqualTo("17.0.0"));
        Assert.That(UnicodeVersion.BaselineDate, Is.EqualTo("2026-08-03"));
        Assert.That(UnicodeVersion.VersionUrl, Is.EqualTo("https://www.unicode.org/versions/Unicode17.0.0/"));
        Assert.That(UnicodeVersion.UcdUrl, Is.EqualTo("https://www.unicode.org/Public/17.0.0/ucd/"));
        Assert.That(UnicodeVersion.EmojiUrl, Is.EqualTo("https://www.unicode.org/Public/17.0.0/ucd/emoji/"));
    }

    [Test]
    public void UnicodeVersion_ReturnsDataProvenanceRecord()
    {
        UnicodeDataProvenance provenance = new UnicodeData().GetDataProvenance();

        Assert.That(provenance.UnicodeVersion, Is.EqualTo(UnicodeVersion.Version));
        Assert.That(provenance.BaselineDate, Is.EqualTo(UnicodeVersion.BaselineDate));
        Assert.That(provenance.VersionUrl, Is.EqualTo(UnicodeVersion.VersionUrl));
        Assert.That(provenance.UcdUrl, Is.EqualTo(UnicodeVersion.UcdUrl));
        Assert.That(provenance.EmojiUrl, Is.EqualTo(UnicodeVersion.EmojiUrl));
        Assert.That(provenance.ProvenanceDocument, Is.EqualTo("todo.md"));
    }

    [Test]
    public void UnicodeVersion_ReturnsDataSourceRecords()
    {
        UnicodeDataSource[] sources = new UnicodeData().GetDataSources();

        Assert.That(sources, Has.Length.EqualTo(3));
        Assert.That(sources[0].Name, Is.EqualTo("Unicode Version"));
        Assert.That(sources[0].Url, Is.EqualTo(UnicodeVersion.VersionUrl));
        Assert.That(sources[1].Name, Is.EqualTo("Unicode Character Database"));
        Assert.That(sources[1].Url, Is.EqualTo(UnicodeVersion.UcdUrl));
        Assert.That(sources[2].Name, Is.EqualTo("Unicode Emoji Data"));
        Assert.That(sources[2].Url, Is.EqualTo(UnicodeVersion.EmojiUrl));
    }

    [Test]
    public void UnicodeVersion_MetadataMatchesTodoBaseline()
    {
        string root = FindRepositoryRoot();
        string todo = File.ReadAllText(Path.Combine(root, "todo.md"));

        Assert.That(todo, Does.Contain("Unicode " + UnicodeVersion.Version + " is the current target baseline as of " + UnicodeVersion.BaselineDate));
        Assert.That(todo, Does.Contain(UnicodeVersion.VersionUrl));
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
