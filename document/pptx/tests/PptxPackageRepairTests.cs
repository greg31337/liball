using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageRepairTests
{
    [Test]
    public void RepairMissingContentTypeOverrides_AddsRequiredOverrideAndMarksPackageDirty()
    {
        PptxPackage package = CreatePackageWithReferencedSlides();
        package.RemoveContentTypeOverride("/ppt/slides/slide1.xml");
        package.MarkClean();

        PptxResult repair = new PptxResult();
        PptxPackageRepair.RepairMissingContentTypeOverrides(package, repair);

        Assert.That(repair.Succeeded(), Is.True, repair.FormatDiagnostics());
        Assert.That(repair.FormatDiagnostics(), Does.Contain("PKGR010"));
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/slides/slide1.xml"), Is.EqualTo(PptxContentTypes.Slide));
        Assert.That(package.IsDirty, Is.True);
        Assert.That(package.Validate().FormatDiagnostics(), Does.Not.Contain("PKGV034"));
    }

    [Test]
    public void RemoveUnusedRelationships_RemovesXmlReferencedRelationshipsNotPresentInXml()
    {
        PptxPackage package = CreatePackageWithReferencedSlides();
        package.MarkClean();

        PptxResult repair = new PptxResult();
        PptxPackageRepair.RemoveUnusedRelationships(package, repair);

        PptxRelationshipCollection relationships = package.GetRelationships("/ppt/presentation.xml");
        Assert.That(repair.Succeeded(), Is.True, repair.FormatDiagnostics());
        Assert.That(repair.FormatDiagnostics(), Does.Contain("PKGR020"));
        Assert.That(relationships.Count(), Is.EqualTo(1));
        Assert.That(relationships.Relationships[0].Id, Is.EqualTo("rId1"));
        Assert.That(package.GetPart("/ppt/presentation.xml").IsDirty, Is.True);
        Assert.That(package.IsDirty, Is.True);
    }

    [Test]
    public void RemoveUnusedRelationships_PreservesNonXmlReferencedRelationshipTypes()
    {
        PptxPackage package = CreatePackageWithReferencedSlides();
        package.AddPart("/ppt/slideLayouts/slideLayout1.xml", PptxContentTypes.SlideLayout, Encoding.UTF8.GetBytes("<p:sldLayout/>"));
        package.AddRelationship("/ppt/slides/slide1.xml", PptxRelationshipTypes.SlideLayout, "../slideLayouts/slideLayout1.xml", PptxRelationshipTargetMode.Internal);

        PptxResult repair = new PptxResult();
        PptxPackageRepair.RemoveUnusedRelationships(package, repair);

        Assert.That(package.GetRelationships("/ppt/slides/slide1.xml").Count(), Is.EqualTo(1));
        Assert.That(package.GetRelationships("/ppt/slides/slide1.xml").Relationships[0].Type, Is.EqualTo(PptxRelationshipTypes.SlideLayout));
    }

    [Test]
    public void Repair_CombinesMissingOverrideAndUnusedRelationshipRepairs()
    {
        PptxPackage package = CreatePackageWithReferencedSlides();
        package.RemoveContentTypeOverride("/ppt/slides/slide2.xml");

        PptxResult repair = package.Repair();

        Assert.That(repair.Succeeded(), Is.True, repair.FormatDiagnostics());
        Assert.That(repair.FormatDiagnostics(), Does.Contain("PKGR010"));
        Assert.That(repair.FormatDiagnostics(), Does.Contain("PKGR020"));
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/slides/slide2.xml"), Is.EqualTo(PptxContentTypes.Slide));
        Assert.That(package.GetRelationships("/ppt/presentation.xml").Count(), Is.EqualTo(1));
    }

    [Test]
    public void Repair_ReturnsErrorForNullPackage()
    {
        PptxResult result = PptxPackageRepair.Repair(null!);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR001"));
    }

    public static PptxPackage CreatePackageWithReferencedSlides()
    {
        PptxPackage package = PptxPackage.Create();
        string presentationXml =
            "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<p:sldIdLst>" +
            "<p:sldId id=\"256\" r:id=\"rId1\" />" +
            "</p:sldIdLst>" +
            "</p:presentation>";

        package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes(presentationXml));
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("<p:sld/>"));
        package.AddPart("/ppt/slides/slide2.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("<p:sld/>"));
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Slide, "slides/slide1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Slide, "slides/slide2.xml", PptxRelationshipTargetMode.Internal);
        return package;
    }
}
