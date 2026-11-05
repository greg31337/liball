using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxRelationshipsWriterTests
{
    [Test]
    public void WriteString_OrdersRelationshipsByStableIdOrder()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("");
        relationships.Add(CreateInternal("", "rId10", PptxRelationshipTypes.CoreProperties, "docProps/core.xml"));
        relationships.Add(CreateExternal("", "rId2", PptxRelationshipTypes.Hyperlink, "https://example.com"));
        relationships.Add(CreateInternal("", "rId1", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml"));

        string xml = PptxRelationshipsWriter.WriteString(relationships);

        Assert.That(xml.IndexOf("Id=\"rId1\""), Is.LessThan(xml.IndexOf("Id=\"rId2\"")));
        Assert.That(xml.IndexOf("Id=\"rId2\""), Is.LessThan(xml.IndexOf("Id=\"rId10\"")));
        Assert.That(xml, Does.Contain("TargetMode=\"External\""));
        Assert.That(xml, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
    }

    [Test]
    public void WriteBytes_RoundTripsRootRelationshipsThroughParser()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("");
        relationships.Add(CreateInternal("", "rId2", PptxRelationshipTypes.CoreProperties, "docProps/core.xml"));
        relationships.Add(CreateInternal("", "rId1", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml"));

        byte[] bytes = PptxRelationshipsWriter.WriteBytes(relationships);
        PptxRelationshipCollection parsed = PptxRelationshipsParser.ParseRootBytes(bytes);

        Assert.That(parsed.Succeeded(), Is.True);
        Assert.That(parsed.Count(), Is.EqualTo(2));
        Assert.That(parsed.Relationships[0].Id, Is.EqualTo("rId1"));
        Assert.That(parsed.Relationships[0].TargetPartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(parsed.Relationships[1].TargetPartName, Is.EqualTo("/docProps/core.xml"));
    }

    [Test]
    public void WriteString_CanComputeRelativeTargetFromTargetPartName()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("/ppt/slides/slide1.xml");
        relationships.Add(new PptxRelationship(
            "/ppt/slides/slide1.xml",
            "rId1",
            PptxRelationshipTypes.SlideLayout,
            "",
            PptxRelationshipTargetMode.Internal,
            "/ppt/slideLayouts/slideLayout1.xml",
            "layout"));

        string xml = PptxRelationshipsWriter.WriteString(relationships);
        PptxRelationshipCollection parsed = PptxRelationshipsParser.ParsePartString("/ppt/slides/slide1.xml", xml);

        Assert.That(xml, Does.Contain("Target=\"../slideLayouts/slideLayout1.xml#layout\""));
        Assert.That(parsed.Succeeded(), Is.True);
        Assert.That(parsed.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slideLayouts/slideLayout1.xml"));
        Assert.That(parsed.Relationships[0].TargetFragment, Is.EqualTo("layout"));
    }

    [Test]
    public void WriteString_CanComputeRelativeTargetForSourcePartAtPackageRoot()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("/metadata.xml");
        relationships.Add(new PptxRelationship(
            "/metadata.xml",
            "rId1",
            PptxRelationshipTypes.Image,
            "",
            PptxRelationshipTargetMode.Internal,
            "/ppt/media/image1.png",
            ""));

        string xml = PptxRelationshipsWriter.WriteString(relationships);

        Assert.That(xml, Does.Contain("Target=\"ppt/media/image1.png\""));
    }

    [Test]
    public void WriteString_ProducesSameXmlForDifferentInsertionOrders()
    {
        PptxRelationshipCollection first = new PptxRelationshipCollection("");
        first.Add(CreateInternal("", "rId2", PptxRelationshipTypes.CoreProperties, "docProps/core.xml"));
        first.Add(CreateInternal("", "rId1", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml"));

        PptxRelationshipCollection second = new PptxRelationshipCollection("");
        second.Add(CreateInternal("", "rId1", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml"));
        second.Add(CreateInternal("", "rId2", PptxRelationshipTypes.CoreProperties, "docProps/core.xml"));

        Assert.That(PptxRelationshipsWriter.WriteString(first), Is.EqualTo(PptxRelationshipsWriter.WriteString(second)));
    }

    [Test]
    public void WriteString_OrdersNumericIdsLargerThanLongMaxValue()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("");
        relationships.Add(CreateInternal("", "rId100000000000000000000", PptxRelationshipTypes.CoreProperties, "docProps/core.xml"));
        relationships.Add(CreateInternal("", "rId99999999999999999999", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml"));

        string xml = PptxRelationshipsWriter.WriteString(relationships);

        Assert.That(xml.IndexOf("Id=\"rId99999999999999999999\""), Is.LessThan(xml.IndexOf("Id=\"rId100000000000000000000\"")));
    }

    [Test]
    public void ValidateCollection_ReportsInvalidRelationships()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("/ppt/presentation.xml");
        relationships.Add(CreateInternal("/ppt/presentation.xml", "rId1", PptxRelationshipTypes.Slide, "slides/slide1.xml"));
        relationships.Add(CreateInternal("/ppt/presentation.xml", "rId1", PptxRelationshipTypes.Slide, "slides/slide2.xml"));
        relationships.Add(CreateExternal("/ppt/presentation.xml", "rId2", PptxRelationshipTypes.Hyperlink, "relative.html"));
        relationships.Add(CreateInternal("/ppt/presentation.xml", "bad id", "not-relative", "slides/slide3.xml"));
        relationships.Add(CreateInternal("/ppt/presentation.xml", "rId4", PptxRelationshipTypes.Slide, "../../evil.xml"));

        PptxResult result = PptxRelationshipsWriter.ValidateCollection(relationships);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.FormatDiagnostics(), Does.Contain("REL015"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("REL017"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("REL011"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("URI010"));
    }

    [Test]
    public void WriteString_ThrowsForInvalidRelationships()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("");
        relationships.Add(CreateExternal("", "rId1", PptxRelationshipTypes.Hyperlink, "relative.html"));

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PptxRelationshipsWriter.WriteString(relationships))!;

        Assert.That(exception.Message, Does.Contain("REL017"));
    }

    [Test]
    public void WriteBytes_UsesUtf8WithoutBom()
    {
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("");
        relationships.Add(CreateInternal("", "rId1", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml"));

        byte[] bytes = PptxRelationshipsWriter.WriteBytes(relationships);

        Assert.That(bytes[0], Is.EqualTo((byte)'<'));
        Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain("encoding=\"utf-8\""));
    }

    static PptxRelationship CreateInternal(string sourcePartName, string id, string type, string target)
    {
        return new PptxRelationship(sourcePartName, id, type, target, PptxRelationshipTargetMode.Internal, "", "");
    }

    static PptxRelationship CreateExternal(string sourcePartName, string id, string type, string target)
    {
        return new PptxRelationship(sourcePartName, id, type, target, PptxRelationshipTargetMode.External, "", "");
    }
}
