using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class WorkflowCompletionTests
{
    [Test]
    public void ImageWorkflow_ReplaceAllEmbeddedRetargetsEveryImageRelationship()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide firstSlide = presentation.GetSlide(0);
        PptxSlide secondSlide = presentation.AddSlide();
        PptxImageOperations images = new PptxImageOperations();
        images.AddEmbedded(firstSlide, CreatePngBytes(20, 10), PptxContentTypes.Png, new PptxBounds(0L, 0L, 20L, 10L));
        images.AddLinked(firstSlide, "https://example.test/original.png", new PptxBounds(20L, 0L, 20L, 10L));
        images.AddEmbedded(secondSlide, CreatePngBytes(30, 15), PptxContentTypes.Png, new PptxBounds(0L, 0L, 30L, 15L));

        int replaced = images.ReplaceAllEmbedded(presentation, CreatePngBytes(64, 32), PptxContentTypes.Png);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxImageFile first = reopened.GetSlide(0).ExtractImages()[0];
        PptxImageFile second = reopened.GetSlide(0).ExtractImages()[1];
        PptxImageFile third = reopened.GetSlide(1).ExtractImages()[0];

        Assert.That(replaced, Is.EqualTo(3));
        Assert.That(first.Linked, Is.False);
        Assert.That(second.Linked, Is.False);
        Assert.That(third.Linked, Is.False);
        Assert.That(first.PartName, Is.EqualTo(second.PartName));
        Assert.That(second.PartName, Is.EqualTo(third.PartName));
        Assert.That(first.Info.WidthPixels, Is.EqualTo(64));
        Assert.That(third.Info.HeightPixels, Is.EqualTo(32));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ImageWorkflow_ReplaceAllLinkedRetargetsEveryImageRelationship()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide firstSlide = presentation.GetSlide(0);
        PptxSlide secondSlide = presentation.AddSlide();
        PptxImageOperations images = new PptxImageOperations();
        images.AddEmbedded(firstSlide, CreatePngBytes(20, 10), PptxContentTypes.Png, new PptxBounds(0L, 0L, 20L, 10L));
        images.AddEmbedded(secondSlide, CreatePngBytes(30, 15), PptxContentTypes.Png, new PptxBounds(0L, 0L, 30L, 15L));

        int replaced = images.ReplaceAllLinked(presentation, "https://example.test/global.png");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxImageFile first = reopened.GetSlide(0).ExtractImages()[0];
        PptxImageFile second = reopened.GetSlide(1).ExtractImages()[0];

        Assert.That(replaced, Is.EqualTo(2));
        Assert.That(first.Linked, Is.True);
        Assert.That(second.Linked, Is.True);
        Assert.That(first.Target, Is.EqualTo("https://example.test/global.png"));
        Assert.That(second.Target, Is.EqualTo("https://example.test/global.png"));
        Assert.That(reopened.GetSlide(0).GetShapeTree().Get(0).Element.OuterXml, Does.Contain("r:link"));
        Assert.That(reopened.GetSlide(1).GetShapeTree().Get(0).Element.OuterXml, Does.Contain("r:link"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void PackageWorkflow_NormalizeOrdersContentWithoutRemovingUnusedParts()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart("/ppt/unused/workflow.bin", PptxContentTypes.Binary, new byte[] { 9, 8, 7 });
        package.Parts.Reverse();
        package.GetRelationships(PptxPartNames.Presentation).Relationships.Reverse();
        package.ContentTypes.Defaults.Reverse();
        package.ContentTypes.Overrides.Reverse();
        package.RawEntries.Clear();
        PptxPackageOperations packages = new PptxPackageOperations();

        PptxResult firstResult = packages.Normalize(package);
        PptxResult secondResult = packages.Normalize(package);
        byte[] firstBytes = packages.SaveBytes(package);
        byte[] secondBytes = packages.SaveBytes(package);

        Assert.That(firstResult.Succeeded(), Is.True, firstResult.FormatDiagnostics());
        Assert.That(firstResult.FormatDiagnostics(), Does.Contain("PKGN010"));
        Assert.That(secondResult.FormatDiagnostics(), Does.Contain("PKGN011"));
        Assert.That(package.HasPart("/ppt/unused/workflow.bin"), Is.True);
        AssertPartNamesOrdered(package);
        AssertRelationshipsOrdered(package.GetRelationships(PptxPartNames.Presentation));
        AssertDefaultsOrdered(package.ContentTypes);
        AssertOverridesOrdered(package.ContentTypes);
        Assert.That(firstBytes, Is.EqualTo(secondBytes));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    static void AssertPartNamesOrdered(PptxPackage package)
    {
        for (int index = 1; index < package.Parts.Count; index++)
        {
            Assert.That(string.Compare(package.Parts[index - 1].PartName, package.Parts[index].PartName, System.StringComparison.Ordinal), Is.LessThanOrEqualTo(0));
        }
    }

    static void AssertRelationshipsOrdered(PptxRelationshipCollection relationships)
    {
        for (int index = 1; index < relationships.Relationships.Count; index++)
        {
            Assert.That(PptxRelationshipsWriter.CompareRelationshipIds(relationships.Relationships[index - 1].Id, relationships.Relationships[index].Id), Is.LessThanOrEqualTo(0));
        }
    }

    static void AssertDefaultsOrdered(PptxContentTypeManifest manifest)
    {
        for (int index = 1; index < manifest.Defaults.Count; index++)
        {
            Assert.That(string.Compare(manifest.Defaults[index - 1].Extension, manifest.Defaults[index].Extension, System.StringComparison.Ordinal), Is.LessThanOrEqualTo(0));
        }
    }

    static void AssertOverridesOrdered(PptxContentTypeManifest manifest)
    {
        for (int index = 1; index < manifest.Overrides.Count; index++)
        {
            Assert.That(string.Compare(manifest.Overrides[index - 1].PartName, manifest.Overrides[index].PartName, System.StringComparison.Ordinal), Is.LessThanOrEqualTo(0));
        }
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static byte[] CreatePngBytes(int width, int height)
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        WriteInt32Big(bytes, 16, width);
        WriteInt32Big(bytes, 20, height);
        return bytes;
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
