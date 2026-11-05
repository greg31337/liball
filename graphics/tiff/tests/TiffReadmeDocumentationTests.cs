using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffReadmeDocumentationTests
{
    [Test]
    public void Readme_IncludesRequestedUsageExamples()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("### Opening A TIFF"));
        Assert.That(readme, Does.Contain("### Inspecting Metadata"));
        Assert.That(readme, Does.Contain("### Reading Pixels"));
        Assert.That(readme, Does.Contain("### Writing Pixels"));
        Assert.That(readme, Does.Contain("### Copying Metadata"));
        Assert.That(readme, Does.Contain("TiffFile.OpenRead"));
        Assert.That(readme, Does.Contain("TiffInspector.Inspect"));
        Assert.That(readme, Does.Contain("TiffCodec.ReadRgba8"));
        Assert.That(readme, Does.Contain("TiffWriter.Create"));
        Assert.That(readme, Does.Contain("TiffCopy.CopyMetadata"));
    }

    [Test]
    public void Readme_UsesCSharpFencedExamples()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(CountOccurrences(readme, "```csharp"), Is.GreaterThanOrEqualTo(6));
        Assert.That(CountOccurrences(readme, "```"), Is.GreaterThanOrEqualTo(12));
    }

    [Test]
    public void Readme_IncludesCompatibilityFeatureMatrix()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("## Compatibility"));
        Assert.That(readme, Does.Contain("### Supported"));
        Assert.That(readme, Does.Contain("### Raw Pass-Through"));
        Assert.That(readme, Does.Contain("### Recognized Only"));
        Assert.That(readme, Does.Contain("### Unsupported"));
        Assert.That(readme, Does.Contain("Classic TIFF and BigTIFF"));
        Assert.That(readme, Does.Contain("PackBits, LZW, Deflate/AdobeDeflate"));
        Assert.That(readme, Does.Contain("Unknown/private tags"));
        Assert.That(readme, Does.Contain("JPEG, Old JPEG, JPEG 2000"));
        Assert.That(readme, Does.Contain("GeoTIFF"));
        Assert.That(readme, Does.Contain("allocation/limit diagnostics"));
    }

    [Test]
    public void Readme_IncludesFirstAlphaReleaseNotesWithExactFeatureMatrix()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("## First Alpha Release Notes"));
        Assert.That(readme, Does.Contain("### First Alpha Feature Matrix"));
        Assert.That(readme, Does.Contain("| Surface | Alpha status | Exact scope |"));
        Assert.That(readme, Does.Contain("Target runtime"));
        Assert.That(readme, Does.Contain("`net10.0`"));
        Assert.That(readme, Does.Contain("Classic TIFF and BigTIFF"));
        Assert.That(readme, Does.Contain("Built-in codecs"));
        Assert.That(readme, Does.Contain("Validation and security limits"));
        Assert.That(readme, Does.Contain("Registered but external codecs"));
        Assert.That(readme, Does.Contain("JPEG, Old JPEG, JPEG 2000, WebP, JPEG XL, Zstd, LZMA"));
        Assert.That(readme, Does.Contain("Arbitrary large image materialization"));
    }

    [Test]
    public void Readme_IncludesExternalCodecBacklog()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("## External Codec Backlog"));
        Assert.That(readme, Does.Contain("built-in-.NET-only rule"));
        Assert.That(readme, Does.Contain("| Backlog item | Current alpha behavior | Completion requirement |"));
        Assert.That(readme, Does.Contain("JPEG-in-TIFF and Old JPEG"));
        Assert.That(readme, Does.Contain("JPEG 2000"));
        Assert.That(readme, Does.Contain("WebP"));
        Assert.That(readme, Does.Contain("JPEG XL"));
        Assert.That(readme, Does.Contain("Zstd"));
        Assert.That(readme, Does.Contain("LZMA"));
        Assert.That(readme, Does.Contain("JBIG"));
        Assert.That(readme, Does.Contain("ThunderScan, Pixar, IT8, DCS"));
        Assert.That(readme, Does.Contain("SGILog compression variants"));
        Assert.That(readme, Does.Contain("raw pass-through"));
    }

    static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while (index >= 0)
        {
            index = text.IndexOf(value, index, StringComparison.Ordinal);
            if (index >= 0)
            {
                count = count + 1;
                index = index + value.Length;
            }
        }

        return count;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "tiff.slnx")) && File.Exists(Path.Combine(directory, "README.md")))
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
