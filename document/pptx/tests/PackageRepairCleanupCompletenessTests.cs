using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PackageRepairCleanupCompletenessTests
{
    [Test]
    public void Repair_RestoresRequiredDefaultsAndMissingRootPresentationRelationship()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ContentTypes.Defaults.Clear();
        package.ContentTypes.AddDefault("rels", PptxContentTypes.Xml);
        package.ContentTypes.AddDefault("rels", PptxContentTypes.Relationships);
        package.RootRelationships.Relationships.Clear();

        PptxResult first = package.Repair();
        PptxResult second = package.Repair();

        Assert.That(first.Succeeded(), Is.True, first.FormatDiagnostics());
        Assert.That(first.FormatDiagnostics(), Does.Contain("PKGR005"));
        Assert.That(first.FormatDiagnostics(), Does.Contain("PKGR040"));
        Assert.That(package.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(package.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
        Assert.That(package.RootRelationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(package.RootRelationships.Relationships[0].TargetPartName, Is.EqualTo(PptxPartNames.Presentation));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
        Assert.That(second.Diagnostics.Count, Is.EqualTo(0));
    }

    [Test]
    public void Repair_RetargetsUniqueExternalRootPresentationRelationship()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationship relationship = package.RootRelationships.Relationships[0];
        relationship.TargetMode = PptxRelationshipTargetMode.External;
        relationship.Target = "https://example.com/presentation.xml";
        relationship.TargetPartName = "";

        PptxResult result = package.Repair();
        PptxRelationship repaired = package.RootRelationships.Relationships[0];

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR041"));
        Assert.That(repaired.TargetMode, Is.EqualTo(PptxRelationshipTargetMode.Internal));
        Assert.That(repaired.TargetPartName, Is.EqualTo(PptxPartNames.Presentation));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Repair_RepairsResolvedMetadataAndRemovesReferencedBrokenRelationships()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationship layout = package.GetPart(PptxPartNames.FirstSlide).Relationships.Relationships[0];
        layout.SourcePartName = "/wrong/source.xml";
        layout.TargetPartName = "/ppt/missing-layout.xml";

        string slideXml = Encoding.UTF8.GetString(package.GetPart(PptxPartNames.FirstSlide).GetBytes());
        slideXml = slideXml.Replace(
            "</p:spTree>",
            "<p:pic><p:blipFill><a:blip r:embed=\"rIdBroken\"/></p:blipFill></p:pic></p:spTree>",
            StringComparison.Ordinal);
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(slideXml));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdBroken", PptxRelationshipTypes.Image, "../media/missing.png", PptxRelationshipTargetMode.Internal);
        package.GetPart(PptxPartNames.FirstSlide).Relationships.SourcePartName = "/wrong/owner.xml";

        PptxResult result = package.Repair();
        string repairedXml = Encoding.UTF8.GetString(package.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR024"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR025"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR026"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR030"));
        Assert.That(layout.SourcePartName, Is.EqualTo(PptxPartNames.FirstSlide));
        Assert.That(layout.TargetPartName, Is.EqualTo(PptxPartNames.FirstSlideLayout));
        Assert.That(package.GetPart(PptxPartNames.FirstSlide).Relationships.ContainsId("rIdBroken"), Is.False);
        Assert.That(repairedXml, Does.Not.Contain("rIdBroken"));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Repair_AllocatesValidUniqueRelationshipIdsAndUpdatesXmlReferences()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationship slideRelationship = package.GetPart(PptxPartNames.Presentation).Relationships.Relationships[1];
        string oldId = slideRelationship.Id;
        slideRelationship.Id = "bad id";
        PptxPart presentationPart = package.GetPart(PptxPartNames.Presentation);
        string presentationXml = Encoding.UTF8.GetString(presentationPart.GetBytes());
        presentationXml = presentationXml.Replace("r:id=\"" + oldId + "\"", "r:id=\"bad id\"", StringComparison.Ordinal);
        package.ReplacePart(PptxPartNames.Presentation, PptxContentTypes.Presentation, Encoding.UTF8.GetBytes(presentationXml));
        package.RootRelationships.Relationships[1].Id = package.RootRelationships.Relationships[0].Id;

        PptxResult result = package.Repair();
        string repairedXml = Encoding.UTF8.GetString(package.GetPart(PptxPartNames.Presentation).GetBytes());

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGR027"));
        Assert.That(slideRelationship.Id, Does.StartWith("rId"));
        Assert.That(repairedXml, Does.Contain("r:id=\"" + slideRelationship.Id + "\""));
        Assert.That(repairedXml, Does.Not.Contain("bad id"));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_RemovesReferencedOrphanAndItsXmlReference()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        string slideXml = Encoding.UTF8.GetString(package.GetPart(PptxPartNames.FirstSlide).GetBytes());
        slideXml = slideXml.Replace(
            "</p:spTree>",
            "<p:pic><p:blipFill><a:blip r:embed=\"rIdMissing\"/></p:blipFill></p:pic></p:spTree>",
            StringComparison.Ordinal);
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(slideXml));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdMissing", PptxRelationshipTypes.Image, "../media/missing.png", PptxRelationshipTargetMode.Internal);

        PptxPackageCleanupResult result = package.Cleanup();
        string cleanedXml = Encoding.UTF8.GetString(package.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(result.RelationshipsRemoved, Is.EqualTo(1));
        Assert.That(cleanedXml, Does.Not.Contain("rIdMissing"));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_RepairsResolvedTargetsBeforeRemovingOrphans()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationship layout = package.GetPart(PptxPartNames.FirstSlide).Relationships.Relationships[0];
        layout.TargetPartName = "/ppt/missing-layout.xml";

        PptxPackageCleanupResult result = package.Cleanup();

        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("PKGR026"));
        Assert.That(layout.TargetPartName, Is.EqualTo(PptxPartNames.FirstSlideLayout));
        Assert.That(package.GetPart(PptxPartNames.FirstSlide).Relationships.ContainsId(layout.Id), Is.True);
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_PreservesRoleRequiredNotesSlideBackRelationship()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        presentation.GetSlide(0).SetNotesText("Keep notes");
        PptxPart notesPart = presentation.Package.GetPart(PptxPartNames.FirstNotesSlide);

        PptxPackageCleanupResult result = presentation.Package.Cleanup();

        Assert.That(result.Succeeded(), Is.True, result.Diagnostics.FormatDiagnostics());
        Assert.That(notesPart.Relationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(notesPart.Relationships.Relationships[0].Type, Is.EqualTo(PptxRelationshipTypes.Slide));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_SelectsLexicallyStableReachableMediaCanonical()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        byte[] imageBytes = CreatePngBytes();
        package.AddPart("/ppt/media/z.png", PptxContentTypes.Png, imageBytes);
        package.AddPart("/ppt/media/a.png", PptxContentTypes.Png, imageBytes);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdZ", PptxRelationshipTypes.Image, "../media/z.png", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdA", PptxRelationshipTypes.Image, "../media/a.png", PptxRelationshipTargetMode.Internal);
        AddImageReferences(package, "rIdZ", "rIdA");

        PptxPackageCleanupResult result = package.Cleanup();

        Assert.That(result.DuplicateMediaPartsRemoved, Is.EqualTo(1));
        Assert.That(package.HasPart("/ppt/media/a.png"), Is.True);
        Assert.That(package.HasPart("/ppt/media/z.png"), Is.False);
        Assert.That(package.GetPart(PptxPartNames.FirstSlide).Relationships.Relationships[1].TargetPartName, Is.EqualTo("/ppt/media/a.png"));
        Assert.That(package.GetPart(PptxPartNames.FirstSlide).Relationships.Relationships[2].TargetPartName, Is.EqualTo("/ppt/media/a.png"));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Cleanup_RemovesRedundantOverridesAndNormalizesOrderingIdempotently()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ContentTypes.AddDefault("png", PptxContentTypes.Png);
        package.AddPart("/ppt/media/z.png", PptxContentTypes.Png, CreatePngBytes());
        package.AddPart("/ppt/media/a.png", PptxContentTypes.Png, new byte[] { 1, 2, 3 });
        package.ContentTypes.AddOverride("/ppt/media/z.png", PptxContentTypes.Png);
        package.AddRelationshipWithId("", "rId20", "https://example.com/relationships/asset", "ppt/media/z.png", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId("", "rId10", "https://example.com/relationships/asset", "ppt/media/a.png", PptxRelationshipTargetMode.Internal);

        PptxPackageCleanupResult first = package.Cleanup();
        PptxPackageCleanupResult second = package.Cleanup();

        Assert.That(first.Diagnostics.FormatDiagnostics(), Does.Contain("PKGC041"));
        Assert.That(first.Diagnostics.FormatDiagnostics(), Does.Contain("PKGC042"));
        Assert.That(first.Diagnostics.FormatDiagnostics(), Does.Contain("PKGC060"));
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/media/z.png"), Is.EqualTo(""));
        Assert.That(package.ContentTypes.GetOverrideContentType("/ppt/media/a.png"), Is.EqualTo(""));
        AssertPartsAreSorted(package);
        AssertRelationshipsAreSorted(package.RootRelationships);
        Assert.That(second.ChangeCount(), Is.EqualTo(0));
        Assert.That(package.Validate().Succeeded(), Is.True, package.Validate().FormatDiagnostics());
    }

    static void AddImageReferences(PptxPackage package, string firstId, string secondId)
    {
        string slideXml = Encoding.UTF8.GetString(package.GetPart(PptxPartNames.FirstSlide).GetBytes());
        slideXml = slideXml.Replace(
            "</p:spTree>",
            "<p:pic><p:blipFill><a:blip r:embed=\"" + firstId + "\"/></p:blipFill></p:pic>" +
            "<p:pic><p:blipFill><a:blip r:embed=\"" + secondId + "\"/></p:blipFill></p:pic></p:spTree>",
            StringComparison.Ordinal);
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(slideXml));
    }

    static void AssertPartsAreSorted(PptxPackage package)
    {
        for (int index = 1; index < package.Parts.Count; index++)
        {
            Assert.That(string.Compare(package.Parts[index - 1].PartName, package.Parts[index].PartName, StringComparison.Ordinal), Is.LessThanOrEqualTo(0));
        }
    }

    static void AssertRelationshipsAreSorted(PptxRelationshipCollection relationships)
    {
        for (int index = 1; index < relationships.Relationships.Count; index++)
        {
            Assert.That(PptxRelationshipsWriter.CompareRelationshipIds(relationships.Relationships[index - 1].Id, relationships.Relationships[index].Id), Is.LessThanOrEqualTo(0));
        }
    }

    static byte[] CreatePngBytes()
    {
        return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4 };
    }
}
