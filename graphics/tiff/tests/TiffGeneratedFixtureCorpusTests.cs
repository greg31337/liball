using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGeneratedFixtureCorpusTests
{
    [Test]
    public void GeneratedFixtureCorpus_ContainsExpectedLegalTiffFiles()
    {
        string directory = GetFixtureDirectory();
        string[] expectedFiles = TiffGeneratedFixtureFactory.CreateAll()
            .Select(fixture => fixture.FileName)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] files = Directory.GetFiles(directory, "*.tif", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray()!;

        Assert.That(files, Is.EqualTo(expectedFiles));
    }

    [Test]
    public void GeneratedFixtureCorpus_MatchesBytesGeneratedByLibraryHelpers()
    {
        foreach (TiffGeneratedFixtureDefinition fixture in TiffGeneratedFixtureFactory.CreateAll())
        {
            string path = Path.Combine(GetFixtureDirectory(), fixture.FileName);
            byte[] bytes = File.ReadAllBytes(path);

            Assert.That(bytes, Is.EqualTo(fixture.Bytes), fixture.FileName);
        }
    }

    [Test]
    public void GeneratedFixtureHelpers_CreateValidInspectableTiffFiles()
    {
        foreach (TiffGeneratedFixtureDefinition fixture in TiffGeneratedFixtureFactory.CreateAll())
        {
            TiffFile file = TiffFile.OpenRead(fixture.Bytes);
            TiffInspectionResult inspection = TiffInspector.Inspect(fixture.Bytes);
            TiffValidationResult validation = TiffValidator.Validate(fixture.Bytes);

            Assert.That(file.Success, Is.True, fixture.FileName);
            Assert.That(file.GetImageCount(), Is.EqualTo(1), fixture.FileName);
            Assert.That(inspection.Success, Is.True, fixture.FileName);
            Assert.That(inspection.ImageCount(), Is.EqualTo(1), fixture.FileName);
            Assert.That(validation.Success, Is.True, fixture.FileName);
            Assert.That(validation.ImageCount, Is.EqualTo(1), fixture.FileName);
            AssertExpectedStorageAndBytes(fixture, file.GetImage(0));
        }
    }

    [Test]
    public void GeneratedFixtureCorpus_AllFixturesOpenInspectAndValidate()
    {
        string[] paths = Directory.GetFiles(GetFixtureDirectory(), "*.tif", SearchOption.TopDirectoryOnly);

        Assert.That(paths.Length, Is.GreaterThanOrEqualTo(3));
        foreach (string path in paths)
        {
            byte[] bytes = File.ReadAllBytes(path);
            TiffFile file = TiffFile.OpenRead(bytes);
            TiffInspectionResult inspection = TiffInspector.Inspect(bytes);
            TiffValidationResult validation = TiffValidator.Validate(bytes);

            Assert.That(file.Success, Is.True, Path.GetFileName(path));
            Assert.That(file.GetImageCount(), Is.EqualTo(1), Path.GetFileName(path));
            Assert.That(inspection.Success, Is.True, Path.GetFileName(path));
            Assert.That(inspection.ImageCount(), Is.EqualTo(1), Path.GetFileName(path));
            Assert.That(validation.Success, Is.True, Path.GetFileName(path));
            Assert.That(validation.ImageCount, Is.EqualTo(1), Path.GetFileName(path));
            Assert.That(file.GetImage(0).GetInfo().Width, Is.EqualTo(2), Path.GetFileName(path));
            Assert.That(file.GetImage(0).GetInfo().Length, Is.EqualTo(2), Path.GetFileName(path));
        }
    }

    [Test]
    public void GeneratedFixtureCorpus_HasExpectedStorageAndBytes()
    {
        foreach (TiffGeneratedFixtureDefinition fixture in TiffGeneratedFixtureFactory.CreateAll())
        {
            TiffImage image = OpenFixture(fixture.FileName);

            AssertExpectedStorageAndBytes(fixture, image);
        }
    }

    static TiffImage OpenFixture(string fileName)
    {
        string path = Path.Combine(GetFixtureDirectory(), fileName);
        TiffFile file = TiffFile.OpenRead(File.ReadAllBytes(path));
        Assert.That(file.Success, Is.True, fileName);
        return file.GetImage(0);
    }

    static void AssertExpectedStorageAndBytes(TiffGeneratedFixtureDefinition fixture, TiffImage image)
    {
        Assert.That(image.GetHeader().FileKind, Is.EqualTo(fixture.FileKind), fixture.FileName);
        Assert.That(image.GetInfo().IsTiled, Is.EqualTo(fixture.IsTiled), fixture.FileName);
        TiffTagValue? description = FindValue(TiffCopy.CopyMetadata(image).Values, TiffTagId.ImageDescription);
        Assert.That(description!.Text, Is.EqualTo(fixture.Description), fixture.FileName);

        if (fixture.ExpectedRawStrip != null)
        {
            Assert.That(image.ReadRawStrip(0).Bytes, Is.EqualTo(fixture.ExpectedRawStrip), fixture.FileName);
        }

        if (fixture.ExpectedRawTile != null)
        {
            Assert.That(image.GetTileCount(), Is.EqualTo(fixture.ExpectedTileCount), fixture.FileName);
            Assert.That(image.ReadRawTile(fixture.ExpectedRawTileIndex).Bytes, Is.EqualTo(fixture.ExpectedRawTile), fixture.FileName);
        }
    }

    static TiffTagValue? FindValue(TiffTagValue[] values, int tagId)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].TagId == tagId)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static string GetFixtureDirectory()
    {
        return Path.Combine(FindRepositoryRoot(), "fixtures", "generated");
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "tiff.slnx")) && Directory.Exists(Path.Combine(directory, "fixtures")))
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
