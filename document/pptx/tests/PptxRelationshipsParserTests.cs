using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxRelationshipsParserTests
{
    [Test]
    public void ParseRootString_ReadsRootRelationships()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"ppt/presentation.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParseRootString(xml);

        Assert.That(relationships.Succeeded(), Is.True);
        Assert.That(relationships.SourcePartName, Is.EqualTo(""));
        Assert.That(relationships.Count(), Is.EqualTo(2));
        Assert.That(relationships.Relationships[0].Id, Is.EqualTo("rId1"));
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(relationships.Relationships[1].TargetPartName, Is.EqualTo("/docProps/core.xml"));
        Assert.That(relationships.Relationships[0].IsInternal(), Is.True);
    }

    [Test]
    public void ParsePartString_ResolvesInternalTargetsAndKeepsExternalTargets()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com\" TargetMode=\"External\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/presentation.xml", xml);

        Assert.That(relationships.Succeeded(), Is.True);
        Assert.That(relationships.SourcePartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(relationships.Count(), Is.EqualTo(2));
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slides/slide1.xml"));
        Assert.That(relationships.Relationships[0].TargetMode, Is.EqualTo(PptxRelationshipTargetMode.Internal));
        Assert.That(relationships.Relationships[1].Target, Is.EqualTo("https://example.com"));
        Assert.That(relationships.Relationships[1].TargetPartName, Is.EqualTo(""));
        Assert.That(relationships.Relationships[1].IsExternal(), Is.True);
    }

    [Test]
    public void ParsePartString_ResolvesParentSegmentsAndFragments()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout1.xml#layout\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/slides/slide1.xml", xml);

        Assert.That(relationships.Succeeded(), Is.True);
        Assert.That(relationships.Count(), Is.EqualTo(1));
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slideLayouts/slideLayout1.xml"));
        Assert.That(relationships.Relationships[0].TargetFragment, Is.EqualTo("layout"));
    }

    [Test]
    public void ParsePartString_ReadsRealisticSlideRelationshipsWithMixedTargetForms()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rIdLayout\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout1.xml\"/>" +
            "<Relationship Id=\"rIdNotes\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesSlide\" Target=\"../notesSlides/notesSlide1.xml#speakerNotes\" TargetMode=\"Internal\"/>" +
            "<Relationship Id=\"rIdImage\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image1.png\"/>" +
            "<Relationship Id=\"rIdTheme\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"/ppt/theme/theme1.xml\"/>" +
            "<Relationship Id=\"rIdChart\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart\" Target=\"../charts/chart1.xml\"/>" +
            "<Relationship Id=\"rIdLink\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/path#section\" TargetMode=\"External\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/slides/slide1.xml", xml);

        Assert.That(relationships.Succeeded(), Is.True, relationships.Diagnostics.FormatDiagnostics());
        Assert.That(relationships.SourcePartName, Is.EqualTo("/ppt/slides/slide1.xml"));
        Assert.That(relationships.Count(), Is.EqualTo(6));
        Assert.That(relationships.Relationships[0].Id, Is.EqualTo("rIdLayout"));
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slideLayouts/slideLayout1.xml"));
        Assert.That(relationships.Relationships[1].TargetMode, Is.EqualTo(PptxRelationshipTargetMode.Internal));
        Assert.That(relationships.Relationships[1].TargetPartName, Is.EqualTo("/ppt/notesSlides/notesSlide1.xml"));
        Assert.That(relationships.Relationships[1].TargetFragment, Is.EqualTo("speakerNotes"));
        Assert.That(relationships.Relationships[2].TargetPartName, Is.EqualTo("/ppt/media/image1.png"));
        Assert.That(relationships.Relationships[3].TargetPartName, Is.EqualTo("/ppt/theme/theme1.xml"));
        Assert.That(relationships.Relationships[4].TargetPartName, Is.EqualTo("/ppt/charts/chart1.xml"));
        Assert.That(relationships.Relationships[5].IsExternal(), Is.True);
        Assert.That(relationships.Relationships[5].Target, Is.EqualTo("https://example.com/path#section"));
        Assert.That(relationships.Relationships[5].TargetPartName, Is.EqualTo(""));
    }

    [Test]
    public void ParseBytes_UsesUtf8XmlBytes()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"theme/theme1.xml\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParseBytes("/ppt/presentation.xml", Encoding.UTF8.GetBytes(xml));

        Assert.That(relationships.Succeeded(), Is.True);
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/theme/theme1.xml"));
    }

    [Test]
    public void ParseString_ReportsInvalidRootDuplicateIdsAndBadTargetMode()
    {
        PptxRelationshipCollection invalidRoot = PptxRelationshipsParser.ParseRootString("<Relationships/>");

        Assert.That(invalidRoot.Failed(), Is.True);
        Assert.That(invalidRoot.Diagnostics.FormatDiagnostics(), Does.Contain("REL003"));

        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide1.xml\"/>" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide2.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com\" TargetMode=\"Remote\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/presentation.xml", xml);

        Assert.That(relationships.Failed(), Is.True);
        Assert.That(relationships.Count(), Is.EqualTo(1));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("REL015"));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("REL016"));
    }

    [Test]
    public void ParseString_ReportsMissingAndUnsafeRelationshipAttributes()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide1.xml\"/>" +
            "<Relationship Id=\"1bad\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"not-relative\" Target=\"slides/slide1.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"../../evil.xml\"/>" +
            "<Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"relative.html\" TargetMode=\"External\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/presentation.xml", xml);

        Assert.That(relationships.Failed(), Is.True);
        Assert.That(relationships.Count(), Is.EqualTo(0));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("REL010"));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("REL011"));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("REL013"));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("URI010"));
        Assert.That(relationships.Diagnostics.FormatDiagnostics(), Does.Contain("REL017"));
    }

    [Test]
    public void ParseString_ReportsComplexInvalidTargetsAndKeepsValidRelationships()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rIdValid\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image1.png\"/>" +
            "<Relationship Id=\"rIdExternalAsInternal\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"https://example.com/image.png\"/>" +
            "<Relationship Id=\"rIdQuery\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image2.png?size=large\"/>" +
            "<Relationship Id=\"rIdBadFragment\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesSlide\" Target=\"../notesSlides/notesSlide1.xml#bad fragment\"/>" +
            "<Relationship Id=\"rIdEscapes\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart\" Target=\"../../../evil.xml\"/>" +
            "<Relationship Id=\"rIdExternalSpace\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/file name.html\" TargetMode=\"External\"/>" +
            "<Relationship Id=\"rIdBadMode\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com\" TargetMode=\"Remote\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/slides/slide1.xml", xml);

        Assert.That(relationships.Failed(), Is.True);
        Assert.That(relationships.Count(), Is.EqualTo(1));
        Assert.That(relationships.Relationships[0].Id, Is.EqualTo("rIdValid"));
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/media/image1.png"));

        string diagnostics = relationships.Diagnostics.FormatDiagnostics();
        Assert.That(diagnostics, Does.Contain("URI021"));
        Assert.That(diagnostics, Does.Contain("URI022"));
        Assert.That(diagnostics, Does.Contain("URI023"));
        Assert.That(diagnostics, Does.Contain("URI010"));
        Assert.That(diagnostics, Does.Contain("REL018"));
        Assert.That(diagnostics, Does.Contain("REL016"));
    }

    [Test]
    public void ParseString_IgnoresUnknownRelationshipElementsWithWarnings()
    {
        string xml =
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\" xmlns:x=\"urn:custom\">" +
            "<Relationship Id=\"rIdSlide\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide1.xml\"/>" +
            "<x:Relationship Id=\"rIdIgnored\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"media/image1.png\"/>" +
            "<Metadata Id=\"ignored\"/>" +
            "<Relationship Id=\"rIdTheme\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"theme/theme1.xml\"/>" +
            "</Relationships>";

        PptxRelationshipCollection relationships = PptxRelationshipsParser.ParsePartString("/ppt/presentation.xml", xml);

        Assert.That(relationships.Succeeded(), Is.True, relationships.Diagnostics.FormatDiagnostics());
        Assert.That(relationships.Count(), Is.EqualTo(2));
        Assert.That(relationships.Relationships[0].Id, Is.EqualTo("rIdSlide"));
        Assert.That(relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slides/slide1.xml"));
        Assert.That(relationships.Relationships[1].Id, Is.EqualTo("rIdTheme"));
        Assert.That(relationships.Relationships[1].TargetPartName, Is.EqualTo("/ppt/theme/theme1.xml"));
        Assert.That(relationships.Diagnostics.Count(), Is.EqualTo(2));
        Assert.That(relationships.Diagnostics.Diagnostics[0].Severity, Is.EqualTo(PptxDiagnosticSeverity.Warning));
        Assert.That(relationships.Diagnostics.Diagnostics[0].Code, Is.EqualTo("REL004"));
        Assert.That(relationships.Diagnostics.Diagnostics[1].Severity, Is.EqualTo(PptxDiagnosticSeverity.Warning));
        Assert.That(relationships.Diagnostics.Diagnostics[1].Code, Is.EqualTo("REL005"));
    }
}
