using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ReadmeDocumentationTests
{
    [Test]
    public void Readme_IncludesIdentifierFormatSelectionGuidance()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("## Choosing an Identifier Format"));
        Assert.That(readme, Does.Contain("| Format | Choose it when | Avoid it when |"));
        Assert.That(readme, Does.Contain("UUIDv4"));
        Assert.That(readme, Does.Contain("UUIDv7"));
        Assert.That(readme, Does.Contain("ULID"));
        Assert.That(readme, Does.Contain("KSUID"));
        Assert.That(readme, Does.Contain("Snowflake"));
        Assert.That(readme, Does.Contain("ObjectId"));
        Assert.That(readme, Does.Contain("Nano ID"));
        Assert.That(readme, Does.Contain("new Identifier().Classify"));
    }

    [Test]
    public void Readme_IncludesSecurityPrivacyAndIndexTradeoffs()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("## Security And Operational Tradeoffs"));
        Assert.That(readme, Does.Contain("Security"));
        Assert.That(readme, Does.Contain("Privacy"));
        Assert.That(readme, Does.Contain("Predictability"));
        Assert.That(readme, Does.Contain("Sortability"));
        Assert.That(readme, Does.Contain("Database indexes"));
        Assert.That(readme, Does.Contain("creation time"));
        Assert.That(readme, Does.Contain("machine ID"));
        Assert.That(readme, Does.Contain("cryptographic tokens"));
    }

    static string FindRepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "uuid.slnx")) && File.Exists(Path.Combine(directory, "README.md")))
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
