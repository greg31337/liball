using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageSaveTests
{
    [Test]
    public void SaveBytes_EmptyCreatedPackageReopens()
    {
        PptxPackage package = PptxPackage.Create();

        byte[] bytes = package.SaveBytes();
        PptxPackage reopened = PptxPackage.OpenBytes(bytes);

        Assert.That(package.IsDirty, Is.False);
        Assert.That(reopened.Succeeded(), Is.True, reopened.Diagnostics.FormatDiagnostics());
        Assert.That(reopened.PartCount(), Is.EqualTo(0));
        Assert.That(reopened.ContentTypes.GetDefaultContentType("rels"), Is.EqualTo(PptxContentTypes.Relationships));
        Assert.That(reopened.ContentTypes.GetDefaultContentType("xml"), Is.EqualTo(PptxContentTypes.Xml));
    }

    [Test]
    public void SaveBytes_WritesPartsContentTypesAndRelationships()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("<p:presentation/>"));
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("<p:sld/>"));
        package.AddRelationship("", PptxRelationshipTypes.OfficeDocument, "ppt/presentation.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Slide, "slides/slide1.xml", PptxRelationshipTargetMode.Internal);

        byte[] bytes = package.SaveBytes();
        PptxPackage reopened = PptxPackage.OpenBytes(bytes);

        Assert.That(reopened.Succeeded(), Is.True, reopened.Diagnostics.FormatDiagnostics());
        Assert.That(reopened.PartCount(), Is.EqualTo(2));
        Assert.That(reopened.RootRelationships.Count(), Is.EqualTo(1));
        Assert.That(reopened.GetPart("/ppt/presentation.xml").Relationships.Count(), Is.EqualTo(1));
        Assert.That(reopened.GetPart("/ppt/presentation.xml").Relationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slides/slide1.xml"));
        Assert.That(Encoding.UTF8.GetString(reopened.GetPart("/ppt/slides/slide1.xml").Bytes), Is.EqualTo("<p:sld/>"));
    }

    [Test]
    public void SaveBytes_PreservesUnknownPartsAndRelationships()
    {
        PptxPackage package = PptxPackage.Create();
        byte[] unknownBytes = new byte[] { 9, 8, 7, 6 };
        package.AddPart("/custom/data.bin", PptxContentTypes.Binary, unknownBytes);
        package.AddRelationship("", "http://example.com/relationships/custom", "custom/data.bin", PptxRelationshipTargetMode.Internal);
        package.AddRelationship("/custom/data.bin", "http://example.com/relationships/link", "https://example.com/resource", PptxRelationshipTargetMode.External);

        PptxPackage reopened = PptxPackage.OpenBytes(package.SaveBytes());

        Assert.That(reopened.Succeeded(), Is.True, reopened.Diagnostics.FormatDiagnostics());
        Assert.That(reopened.HasPart("/custom/data.bin"), Is.True);
        Assert.That(reopened.GetPart("/custom/data.bin").Bytes, Is.EqualTo(unknownBytes));
        Assert.That(reopened.RootRelationships.Relationships[0].Type, Is.EqualTo("http://example.com/relationships/custom"));
        Assert.That(reopened.GetPart("/custom/data.bin").Relationships.Relationships[0].TargetMode, Is.EqualTo(PptxRelationshipTargetMode.External));
        Assert.That(reopened.GetPart("/custom/data.bin").Relationships.Relationships[0].Target, Is.EqualTo("https://example.com/resource"));
    }

    [Test]
    public void SaveBytes_ReflectsPartRenamesAndRemovals()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("slide"));
        package.AddPart("/ppt/slides/slide2.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("remove"));
        package.AddRelationship("", PptxRelationshipTypes.Slide, "ppt/slides/slide1.xml", PptxRelationshipTargetMode.Internal);

        package.RenamePart("/ppt/slides/slide1.xml", "/ppt/slides/renamed.xml");
        package.RemovePart("/ppt/slides/slide2.xml");

        PptxPackage reopened = PptxPackage.OpenBytes(package.SaveBytes());

        Assert.That(reopened.HasPart("/ppt/slides/renamed.xml"), Is.True);
        Assert.That(reopened.HasPart("/ppt/slides/slide1.xml"), Is.False);
        Assert.That(reopened.HasPart("/ppt/slides/slide2.xml"), Is.False);
        Assert.That(reopened.RootRelationships.Relationships[0].TargetPartName, Is.EqualTo("/ppt/slides/renamed.xml"));
    }

    [Test]
    public void SaveBytes_UsesDirtyXmlDocumentBytes()
    {
        PptxPackage package = PptxPackage.Create();
        PptxPart part = package.AddPart("/ppt/custom/item1.xml", PptxContentTypes.Xml, Encoding.UTF8.GetBytes("<root/>"));
        XmlDocument document = new XmlDocument();
        document.LoadXml("<root><child value=\"1\"/></root>");
        part.SetXmlDocument(document);

        PptxPackage reopened = PptxPackage.OpenBytes(package.SaveBytes());

        Assert.That(Encoding.UTF8.GetString(reopened.GetPart("/ppt/custom/item1.xml").Bytes), Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><root><child value=\"1\" /></root>"));
    }

    [Test]
    public void SaveBytes_NormalizesDirtyXmlDocumentPrefixes()
    {
        PptxPackage package = PptxPackage.Create();
        PptxPart part = package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("<root/>"));
        XmlDocument document = PptxXml.LoadString(
            "<x:presentation xmlns:x=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
            "xmlns:z=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<x:sldIdLst><x:sldId z:id=\"rId1\" /></x:sldIdLst>" +
            "</x:presentation>");
        part.SetXmlDocument(document);

        PptxPackage reopened = PptxPackage.OpenBytes(package.SaveBytes());
        string xml = Encoding.UTF8.GetString(reopened.GetPart("/ppt/presentation.xml").Bytes);

        Assert.That(xml, Does.Contain("<p:presentation"));
        Assert.That(xml, Does.Contain("<p:sldIdLst>"));
        Assert.That(xml, Does.Contain("r:id=\"rId1\""));
        Assert.That(xml, Does.Not.Contain("<x:presentation"));
        Assert.That(xml, Does.Not.Contain("z:id="));
    }

    [Test]
    public void SaveBytes_IsDeterministic()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/slides/slide1.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes("slide"));
        package.AddRelationship("", PptxRelationshipTypes.Slide, "ppt/slides/slide1.xml", PptxRelationshipTargetMode.Internal);

        byte[] first = package.SaveBytes();
        byte[] second = package.SaveBytes();

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void BuildZipEntries_IncludesPartRelationshipEntryNames()
    {
        PptxPackage package = PptxPackage.Create();
        package.AddPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("presentation"));
        package.AddRelationship("/ppt/presentation.xml", PptxRelationshipTypes.Slide, "slides/slide1.xml", PptxRelationshipTargetMode.Internal);

        bool foundRelationshipEntry = false;
        List<PptxZipEntry> entries = package.BuildZipEntries();

        for (int index = 0; index < entries.Count; index++)
        {
            if (entries[index].Name == "ppt/_rels/presentation.xml.rels")
            {
                foundRelationshipEntry = true;
            }
        }

        Assert.That(foundRelationshipEntry, Is.True);
    }
}
