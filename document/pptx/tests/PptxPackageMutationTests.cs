using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageMutationTests
{
    [Test]
    public void AddPart_AddsPartContentTypeOverrideAndMarksDirty()
    {
        PptxPackage package = PptxPackage.Create();
        package.MarkClean();

        PptxPart part = package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("presentation"));

        Assert.That(package.PartCount(), Is.EqualTo(1));
        Assert.That(part.PartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(part.IsDirty, Is.True);
        Assert.That(package.IsDirty, Is.True);
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/presentation.xml"), Is.EqualTo(PptxContentTypes.Presentation));
        Assert.Throws<ArgumentException>(() => package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, new byte[0]));
    }

    [Test]
    public void ReplacePart_ReplacesBytesAndContentType()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/custom/item1.xml", PptxContentTypes.Xml, Encoding.UTF8.GetBytes("old"));
        package.MarkClean();

        PptxPart part = package.ReplacePart("/ppt/custom/item1.xml", PptxContentTypes.CustomProperties, Encoding.UTF8.GetBytes("new"));

        Assert.That(Encoding.UTF8.GetString(part.Bytes), Is.EqualTo("new"));
        Assert.That(part.ContentType, Is.EqualTo(PptxContentTypes.CustomProperties));
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/custom/item1.xml"), Is.EqualTo(PptxContentTypes.CustomProperties));
        Assert.That(part.IsDirty, Is.True);
        Assert.That(package.IsDirty, Is.True);
    }

    [Test]
    public void RemovePart_RemovesPartAndContentTypeOverride()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("slide"));

        package.RemovePart("/ppt/slides/slide1.xml");

        Assert.That(package.HasPart("/ppt/slides/slide1.xml"), Is.False);
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/slides/slide1.xml"), Is.EqualTo(""));
        Assert.Throws<ArgumentException>(() => package.RemovePart("/ppt/slides/slide1.xml"));
    }

    [Test]
    public void RenamePart_UpdatesPartContentTypeAndInboundRelationshipTargets()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("slide"));
        package.AddRelationship("", PptxRelationshipTypes.Slide, "ppt/slides/slide1.xml", PptxRelationshipTargetMode.Internal);
        package.MarkClean();

        PptxPart renamed = package.RenamePart("/ppt/slides/slide1.xml", "/ppt/slides/renamed.xml");

        Assert.That(renamed.PartName, Is.EqualTo("/ppt/slides/renamed.xml"));
        Assert.That(package.HasPart("/ppt/slides/slide1.xml"), Is.False);
        Assert.That(package.HasPart("/ppt/slides/renamed.xml"), Is.True);
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/slides/slide1.xml"), Is.EqualTo(""));
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/slides/renamed.xml"), Is.EqualTo(PptxContentTypes.Slide));
        Assert.That(package.RootRelationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slides/renamed.xml"));
        Assert.That(package.RootRelationships.Relationships[0].Target, Is.EqualTo(""));
        Assert.That(package.IsDirty, Is.True);
        Assert.That(renamed.IsDirty, Is.True);
    }

    [Test]
    public void AddRelationship_AllocatesIdsAndSupportsInternalAndExternalTargets()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("presentation"));
        package.MarkClean();

        PptxRelationship root = package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        PptxRelationship external = package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Hyperlink, "https://example.com", PptxRelationshipTargetMode.External);

        Assert.That(root.Id, Is.EqualTo("rId1"));
        Assert.That(root.TargetPartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(external.Id, Is.EqualTo("rId1"));
        Assert.That(external.TargetMode, Is.EqualTo(PptxRelationshipTargetMode.External));
        Assert.That(package.GetPart("/ppt/presentation.xml").Relationships.Count(), Is.EqualTo(1));
        Assert.That(package.GetPart("/ppt/presentation.xml").IsDirty, Is.True);
        Assert.That(package.IsDirty, Is.True);

        PptxRelationship secondRoot = package.AddRelationship("", PptxRelationshipTypes.CoreProperties, "docProps/core.xml", PptxRelationshipTargetMode.Internal);
        Assert.That(secondRoot.Id, Is.EqualTo("rId2"));
    }

    [Test]
    public void RemoveRelationship_RemovesRelationshipAndMarksDirty()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        package.MarkClean();

        package.RemoveRelationship("", "rId1");

        Assert.That(package.RootRelationships.Count(), Is.EqualTo(0));
        Assert.That(package.IsDirty, Is.True);
        Assert.Throws<ArgumentException>(() => package.RemoveRelationship("", "rId1"));
    }

    [Test]
    public void SetRelationshipTarget_ReplacesTargetMetadata()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        package.MarkClean();

        PptxRelationship relationship = package.SetRelationshipTarget("", "rId1", "ppt/renamed.xml#main", PptxRelationshipTargetMode.Internal);

        Assert.That(relationship.Target, Is.EqualTo("ppt/renamed.xml#main"));
        Assert.That(relationship.TargetPartName, Is.EqualTo("/ppt/renamed.xml"));
        Assert.That(relationship.TargetFragment, Is.EqualTo("main"));
        Assert.That(package.IsDirty, Is.True);
    }

    [Test]
    public void RelationshipMutators_RejectInvalidTargets()
    {
        PptxPackage package = PptxPackage.Create();

        Assert.Throws<ArgumentException>(
            () => package.AddRelationship("", PptxRelationshipTypes.Hyperlink, "relative.html", PptxRelationshipTargetMode.External));

        Assert.Throws<ArgumentException>(
            () => package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "../evil.xml", PptxRelationshipTargetMode.Internal));
    }
}
