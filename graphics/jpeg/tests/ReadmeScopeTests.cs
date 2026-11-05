namespace jpeg.Tests;

internal class ReadmeScopeTests
{
    [Test]
    public void Readme_DefinesClassicJpegScopeAndNonGoals()
    {
        string root = FindRepositoryRoot();
        string readme = File.ReadAllText(Path.Combine(root, "README.md"));

        Assert.That(readme, Does.Contain("## Scope"));
        Assert.That(readme, Does.Contain("classic JPEG streams from the JPEG-1 family"));
        Assert.That(readme, Does.Contain("JFIF"));
        Assert.That(readme, Does.Contain("Exif"));
        Assert.That(readme, Does.Contain("Adobe-style JPEG"));
        Assert.That(readme, Does.Contain("pure managed C#"));
        Assert.That(readme, Does.Contain("only built-in .NET APIs"));

        Assert.That(readme, Does.Contain("## JPEG Family Detection"));
        Assert.That(readme, Does.Contain("JPEG 2000"));
        Assert.That(readme, Does.Contain("JPEG XR"));
        Assert.That(readme, Does.Contain("JPEG XL"));
        Assert.That(readme, Does.Contain("HEIF/AVIF"));
        Assert.That(readme, Does.Contain("JPEG-LS"));
        Assert.That(readme, Does.Contain("SmartScale"));
        Assert.That(readme, Does.Contain("unsupported rather than misreported"));

        Assert.That(readme, Does.Contain("## Non-Goals"));
        Assert.That(readme, Does.Contain("No native shims"));
        Assert.That(readme, Does.Contain("No dependency on external image-processing or color-management packages"));
        Assert.That(readme, Does.Contain("No best-effort decoding that silently treats unknown JPEG-family processes as baseline JPEG"));
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "jpeg.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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
