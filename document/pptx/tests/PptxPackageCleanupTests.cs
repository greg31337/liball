using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageCleanupTests
{
    [Test]
    public void Cleanup_RemovesUnusedPartsOrphanRelationshipsAndDanglingContentTypes()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart("/ppt/slides/slide2.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("<p:sld/>"));
        package.AddPart("/ppt/media/orphan.png", PptxContentTypes.Png, CreatePngBytes(100, 50));
        package.AddRelationshipWithId(PptxPartNames.Presentation, "rIdUnusedSlide", PptxRelationshipTypes.Slide, "slides/slide2.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdMissingImage", PptxRelationshipTypes.Image, "../media/missing.png", PptxRelationshipTargetMode.Internal);
        package.ContentTypes.AddOverride("/ppt/media/stale.png", PptxContentTypes.Png);
        package.MarkClean();

        PptxPackageCleanupResult result = package.Cleanup();

        Assert.That(result.Succeeded(), Is.True, result.Diagnostics.FormatDiagnostics());
        Assert.That(result.RelationshipsRemoved, Is.EqualTo(2));
        Assert.That(result.UnusedPartsRemoved, Is.EqualTo(2));
        Assert.That(result.ContentTypeOverridesRemoved, Is.EqualTo(1));
        Assert.That(package.HasPart("/ppt/slides/slide2.xml"), Is.False);
        Assert.That(package.HasPart("/ppt/media/orphan.png"), Is.False);
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/media/stale.png"), Is.EqualTo(""));
        Assert.That(package.GetRelationships(PptxPartNames.Presentation).ContainsId("rIdUnusedSlide"), Is.False);
        Assert.That(package.GetRelationships(PptxPartNames.FirstSlide).ContainsId("rIdMissingImage"), Is.False);
        Assert.That(package.IsDirty, Is.True);
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_DeduplicatesMediaPartsAndRetargetsRelationships()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateDuplicateImageSlideXml()));
        package.AddPart("/ppt/media/image1.png", PptxContentTypes.Png, CreatePngBytes(320, 200));
        package.AddPart("/ppt/media/image2.png", PptxContentTypes.Png, CreatePngBytes(320, 200));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdImage1", PptxRelationshipTypes.Image, "../media/image1.png", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdImage2", PptxRelationshipTypes.Image, "../media/image2.png", PptxRelationshipTargetMode.Internal);

        PptxPackageCleanupResult result = package.Cleanup();
        PptxRelationship second = package.GetRelationships(PptxPartNames.FirstSlide).Relationships[2];

        Assert.That(result.Succeeded(), Is.True, result.Diagnostics.FormatDiagnostics());
        Assert.That(result.DuplicateMediaPartsRemoved, Is.EqualTo(1));
        Assert.That(result.MediaRelationshipsRetargeted, Is.EqualTo(1));
        Assert.That(result.UnusedPartsRemoved, Is.EqualTo(1));
        Assert.That(package.HasPart("/ppt/media/image1.png"), Is.True);
        Assert.That(package.HasPart("/ppt/media/image2.png"), Is.False);
        Assert.That(second.Id, Is.EqualTo("rIdImage2"));
        Assert.That(second.TargetPartName, Is.EqualTo("/ppt/media/image1.png"));
        Assert.That(second.Target, Is.EqualTo(""));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_CompactsRawEntriesDeterministically()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.RawEntries.Clear();

        PptxPackageCleanupResult result = package.Cleanup();
        byte[] first = package.SaveBytes();
        byte[] second = package.SaveBytes();

        Assert.That(result.Succeeded(), Is.True, result.Diagnostics.FormatDiagnostics());
        Assert.That(result.RawEntriesCompacted, Is.True);
        Assert.That(package.RawEntries.Count, Is.GreaterThan(0));
        Assert.That(package.RawEntries[0].Name, Is.EqualTo(PptxPartNames.ContentTypesEntry));
        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void Cleanup_ReturnsErrorForNullPackage()
    {
        PptxPackageCleanupResult result = PptxPackageCleaner.Cleanup(null!);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("PKGC001"));
    }

    static string CreateDuplicateImageSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreatePictureXml("2", "First", "rIdImage1") +
            CreatePictureXml("3", "Second", "rIdImage2") +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreatePictureXml(string id, string name, string relationshipId)
    {
        return
            "<p:pic>" +
            "<p:nvPicPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvPicPr/><p:nvPr/></p:nvPicPr>" +
            "<p:blipFill><a:blip r:embed=\"" + relationshipId + "\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"320\" cy=\"200\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "</p:pic>";
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
