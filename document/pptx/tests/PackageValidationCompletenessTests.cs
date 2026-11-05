using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PackageValidationCompletenessTests
{
    [Test]
    public void Validate_ReportsWrongRootsAndMissingMinimumElements()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(
            PptxPartNames.FirstSlide,
            PptxContentTypes.Slide,
            Encoding.UTF8.GetBytes("<p:notSlide xmlns:p=\"" + PptxNamespaces.Presentation + "\"/>"));
        package.AddPart(
            "/ppt/charts/chart1.xml",
            PptxContentTypes.Chart,
            Encoding.UTF8.GetBytes("<c:chartSpace xmlns:c=\"" + PptxNamespaces.Chart + "\"/>"));
        package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        package.ReplacePart(
            PptxPartNames.PresentationProperties,
            PptxContentTypes.PresentationProperties,
            Encoding.UTF8.GetBytes("<x:presentationPr xmlns:x=\"https://example.com/wrong\"/>"));

        PptxResult result = package.Validate();

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV050"));
        Assert.That(result.FormatDiagnostics(), Does.Contain(PptxPartNames.FirstSlide));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV051"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("/chart"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV053"));
    }

    [Test]
    public void Validate_ReportsMissingMasterLayoutThemeAndSlideLinks()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.GetPart(PptxPartNames.FirstSlideMaster).Relationships.Relationships.Clear();
        package.GetPart(PptxPartNames.FirstSlideLayout).Relationships.Relationships.Clear();
        package.GetPart(PptxPartNames.FirstSlide).Relationships.Relationships.Clear();

        PptxResult result = package.Validate();

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV052"));
        Assert.That(result.FormatDiagnostics(), Does.Contain(PptxRelationshipTypes.Theme));
        Assert.That(result.FormatDiagnostics(), Does.Contain(PptxRelationshipTypes.SlideLayout));
        Assert.That(result.FormatDiagnostics(), Does.Contain(PptxRelationshipTypes.SlideMaster));
    }

    [Test]
    public void Validate_ReportsDuplicateXmlIdentifiersAndMissingRelationshipReferences()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        string presentationXml = PptxMinimumPresentation.PresentationXml();
        presentationXml = presentationXml.Replace("xmlns:r=", "xmlns:rel=", StringComparison.Ordinal);
        presentationXml = presentationXml.Replace("r:id=", "rel:id=", StringComparison.Ordinal);
        presentationXml = presentationXml.Replace(
            "<p:sldIdLst><p:sldId id=\"256\" rel:id=\"rId2\"/></p:sldIdLst>",
            "<p:sldIdLst><p:sldId id=\"256\" rel:id=\"rId2\"/><p:sldId id=\"256\" rel:id=\"rIdMissing\"/></p:sldIdLst>",
            StringComparison.Ordinal);
        package.ReplacePart(PptxPartNames.Presentation, PptxContentTypes.Presentation, Encoding.UTF8.GetBytes(presentationXml));

        string slideXml = PptxMinimumPresentation.SlideXml();
        slideXml = slideXml.Replace(
            "</p:spTree>",
            "<p:sp><p:nvSpPr><p:cNvPr id=\"1\" name=\"Duplicate\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr/></p:sp></p:spTree>",
            StringComparison.Ordinal);
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(slideXml));

        PptxResult result = package.Validate();

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV060"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV061"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("rIdMissing"));
    }

    [Test]
    public void Validate_ReportsMutableRelationshipMetadataDefects()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationshipCollection masterRelationships = package.GetPart(PptxPartNames.FirstSlideMaster).Relationships;
        masterRelationships.Relationships[1].Id = masterRelationships.Relationships[0].Id;
        masterRelationships.Relationships[1].Target = "../theme/theme1.xml";

        PptxRelationship hyperlink = package.AddRelationship(
            PptxPartNames.FirstSlide,
            PptxRelationshipTypes.Hyperlink,
            "https://example.com",
            PptxRelationshipTargetMode.External);
        hyperlink.Target = "relative.html";

        PptxResult result = package.Validate();

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV060"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV023"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("REL017"));
    }

    [Test]
    public void Validate_ReportsExternalTargetsForInternalRelationshipTypes()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationship relationship = package.GetPart(PptxPartNames.FirstSlide).Relationships.Relationships[0];
        relationship.TargetMode = PptxRelationshipTargetMode.External;
        relationship.Target = "https://example.com/layout.xml";
        relationship.TargetPartName = "";

        PptxResult result = package.Validate();

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV070"));
        Assert.That(result.FormatDiagnostics(), Does.Contain(relationship.Id));
    }

    [Test]
    public void Validate_ReportsKnownRelationshipTargetContentTypeMismatch()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        PptxRelationship relationship = package.GetPart(PptxPartNames.FirstSlideMaster).Relationships.Relationships[0];
        relationship.Type = PptxRelationshipTypes.SlideLayout;

        PptxResult result = package.Validate();

        Assert.That(result.FormatDiagnostics(), Does.Contain("PKGV071"));
        Assert.That(result.FormatDiagnostics(), Does.Contain(relationship.Id));
    }

    [Test]
    public void Validate_PreservesUnknownAbsoluteExternalRelationships()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddRelationship(
            PptxPartNames.FirstSlide,
            "https://example.com/relationships/custom",
            "https://example.com/resource",
            PptxRelationshipTargetMode.External);

        PptxResult result = package.Validate();

        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
    }

    [TestCase(PptxRelationshipTypes.SlideMaster, PptxContentTypes.SlideMaster)]
    [TestCase(PptxRelationshipTypes.SlideLayout, PptxContentTypes.SlideLayout)]
    [TestCase(PptxRelationshipTypes.Slide, PptxContentTypes.Slide)]
    [TestCase(PptxRelationshipTypes.Theme, PptxContentTypes.Theme)]
    [TestCase(PptxRelationshipTypes.NotesSlide, PptxContentTypes.NotesSlide)]
    [TestCase(PptxRelationshipTypes.NotesMaster, PptxContentTypes.NotesMaster)]
    [TestCase(PptxRelationshipTypes.HandoutMaster, PptxContentTypes.HandoutMaster)]
    [TestCase(PptxRelationshipTypes.Chart, PptxContentTypes.Chart)]
    [TestCase(PptxRelationshipTypes.ChartStyle, PptxContentTypes.ChartStyle)]
    [TestCase(PptxRelationshipTypes.ChartColorStyle, PptxContentTypes.ChartColorStyle)]
    [TestCase(PptxRelationshipTypes.DiagramData, PptxContentTypes.DiagramData)]
    [TestCase(PptxRelationshipTypes.DiagramLayout, PptxContentTypes.DiagramLayout)]
    [TestCase(PptxRelationshipTypes.DiagramQuickStyle, PptxContentTypes.DiagramStyle)]
    [TestCase(PptxRelationshipTypes.DiagramColors, PptxContentTypes.DiagramColors)]
    [TestCase(PptxRelationshipTypes.Image, PptxContentTypes.Png)]
    [TestCase(PptxRelationshipTypes.Thumbnail, PptxContentTypes.Png)]
    [TestCase(PptxRelationshipTypes.Audio, PptxContentTypes.Mp3)]
    [TestCase(PptxRelationshipTypes.Video, PptxContentTypes.Mp4)]
    [TestCase(PptxRelationshipTypes.OleObject, PptxContentTypes.OleObject)]
    public void RelationshipTargetMatching_CoversPlannedTypedLinks(string relationshipType, string contentType)
    {
        Assert.That(PptxPackageValidator.RelationshipTargetMatchesContentType(relationshipType, contentType), Is.True);
        Assert.That(PptxPackageValidator.RelationshipTargetMatchesContentType(relationshipType, PptxContentTypes.Binary), Is.False);
    }
}
