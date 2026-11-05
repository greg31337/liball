using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageCreateTests
{
    [Test]
    public void Create_ReturnsEmptyDirtyPackageWithCoreContentTypeDefaults()
    {
        PptxPackage package = PptxPackage.Create();

        Assert.That(package.Succeeded(), Is.True);
        Assert.That(package.PartCount(), Is.EqualTo(0));
        Assert.That(package.RawEntryCount(), Is.EqualTo(0));
        Assert.That(package.RootRelationships.Count(), Is.EqualTo(0));
        Assert.That(package.RootRelationships.SourcePartName, Is.EqualTo(""));
        Assert.That(package.ContentTypes.DefaultCount(), Is.EqualTo(2));
        Assert.That(package.ContentTypes.OverrideCount(), Is.EqualTo(0));
        Assert.That(package.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(package.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
        Assert.That(package.IsDirty, Is.True);
    }

    [Test]
    public void Create_ContentTypesCanBeWrittenAndParsed()
    {
        PptxPackage package = PptxPackage.Create();

        byte[] bytes = PptxContentTypesWriter.WriteBytes(package.ContentTypes);
        PptxContentTypeManifest parsed = PptxContentTypesParser.ParseBytes(bytes);

        Assert.That(parsed.Succeeded(), Is.True);
        Assert.That(parsed.DefaultCount(), Is.EqualTo(2));
        Assert.That(parsed.GetDefaultContentType("rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(parsed.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
    }

    [Test]
    public void Create_ReturnsIndependentPackageInstances()
    {
        PptxPackage first = PptxPackage.Create();
        PptxPackage second = PptxPackage.Create();

        first.ContentTypes.AddDefault("png", PptxContentTypes.Png);
        first.RootRelationships.Add(new PptxRelationship(
            "",
            "rId1",
            PptxRelationshipTypes.OfficeDocument,
            "ppt/presentation.xml",
            PptxRelationshipTargetMode.Internal,
            "/ppt/presentation.xml",
            ""));

        Assert.That(first.ContentTypes.DefaultCount(), Is.EqualTo(3));
        Assert.That(second.ContentTypes.DefaultCount(), Is.EqualTo(2));
        Assert.That(first.RootRelationships.Count(), Is.EqualTo(1));
        Assert.That(second.RootRelationships.Count(), Is.EqualTo(0));
    }

    [Test]
    public void AddCoreContentTypeDefaults_IsIdempotent()
    {
        PptxPackage package = PptxPackage.Create();

        PptxPackage.AddCoreContentTypeDefaults(package);

        Assert.That(package.ContentTypes.DefaultCount(), Is.EqualTo(2));
        Assert.That(package.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(package.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
    }
}
