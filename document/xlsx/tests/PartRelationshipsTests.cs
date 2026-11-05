using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PartRelationshipsTests
{
    [Test]
    public void ReadString_ResolvesTargetsRelativeToOwningPartFromRelationshipPartPath()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\" />" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\" />" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"theme/theme1.xml\" />" +
            "</Relationships>";
        XlsxPartRelationshipsReader reader = new XlsxPartRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadString(XlsxPackagePaths.WorkbookRelationships, xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo(XlsxPackagePaths.WorkbookRelationships));
        Assert.That(part.SourcePartPath, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(part.GetRelationship("rId1")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(part.GetRelationship("rId2")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Styles));
        Assert.That(part.GetRelationship("rId3")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Theme));
    }

    [Test]
    public void ReadString_DerivesRelationshipPartPathFromOwningPartPath()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml#A1\" />" +
            "</Relationships>";
        XlsxPartRelationshipsReader reader = new XlsxPartRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadString(XlsxPackagePaths.Workbook, xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo(XlsxPackagePaths.WorkbookRelationships));
        Assert.That(part.SourcePartPath, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(part.Relationships[0].TargetPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(part.Relationships[0].TargetFragment, Is.EqualTo("A1"));
    }

    [Test]
    public void WriteString_WritesStableRelationshipsAndCanBeReadBack()
    {
        XlsxRelationshipsPart part = CreateWorkbookRelationshipsPart();
        part.Relationships.Add(CreateRelationship("rId2", XlsxRelationshipTypes.Styles, "styles.xml", ""));
        part.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml", ""));
        XlsxPartRelationshipsWriter writer = new XlsxPartRelationshipsWriter();

        XlsxRelationshipsWriteResult writeResult = writer.WriteString(part);
        XlsxRelationshipsPart readPart = new XlsxPartRelationshipsReader().ReadString(XlsxPackagePaths.WorkbookRelationships, writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.StartWith("<?xml"));
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.Relationships[0].Id, Is.EqualTo("rId1"));
        Assert.That(readPart.Relationships[1].Id, Is.EqualTo("rId2"));
        Assert.That(readPart.GetRelationship("rId1")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(readPart.GetRelationship("rId2")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Styles));
    }

    [Test]
    public void WriteBytes_ProducesIdenticalBytesForEquivalentRelationshipParts()
    {
        XlsxRelationshipsPart first = CreateWorkbookRelationshipsPart();
        first.Relationships.Add(CreateRelationship("rId2", XlsxRelationshipTypes.Styles, "styles.xml", ""));
        first.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml", ""));
        XlsxRelationshipsPart second = CreateWorkbookRelationshipsPart();
        second.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml", ""));
        second.Relationships.Add(CreateRelationship("rId2", XlsxRelationshipTypes.Styles, "styles.xml", ""));
        XlsxPartRelationshipsWriter writer = new XlsxPartRelationshipsWriter();

        XlsxRelationshipsWriteResult firstResult = writer.WriteBytes(first);
        XlsxRelationshipsWriteResult secondResult = writer.WriteBytes(second);

        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(secondResult.IsValid, Is.True);
        Assert.That(firstResult.Bytes, Is.EqualTo(secondResult.Bytes));
    }

    [Test]
    public void WriteString_PreservesUnknownAttributesAndRawUnknownXml()
    {
        XlsxRelationshipsPart part = CreateWorkbookRelationshipsPart();
        part.UnknownRootAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        XlsxRelationship relationship = CreateRelationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml", "");
        relationship.UnknownAttributes.Add(CreateAttribute("urn:x", "x", "item", "i"));
        part.Relationships.Add(relationship);
        part.UnknownXml.Add("<x:Future xmlns:x=\"urn:x\" x:value=\"1\" />");
        XlsxPartRelationshipsWriter writer = new XlsxPartRelationshipsWriter();

        XlsxRelationshipsWriteResult writeResult = writer.WriteString(part);
        XlsxRelationshipsPart readPart = new XlsxPartRelationshipsReader().ReadString(XlsxPackagePaths.WorkbookRelationships, writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.UnknownRootAttributes[0].LocalName, Is.EqualTo("root"));
        Assert.That(readPart.Relationships[0].UnknownAttributes[0].LocalName, Is.EqualTo("item"));
        Assert.That(readPart.UnknownXml[0], Does.Contain("Future"));
    }

    [Test]
    public void WriteBytes_RejectsInvalidRelationshipPartsBeforeWriting()
    {
        XlsxRelationshipsPart part = CreateWorkbookRelationshipsPart();
        part.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml", ""));
        part.Relationships.Add(CreateRelationship("rId1", XlsxRelationshipTypes.Styles, "styles.xml", ""));
        part.Relationships.Add(CreateRelationship("rId3", "", "sharedStrings.xml", ""));
        part.Relationships.Add(CreateRelationship("rId4", XlsxRelationshipTypes.Theme, "../../evil.xml", ""));
        part.Relationships.Add(CreateRelationship("rId5", XlsxRelationshipTypes.Hyperlink, "https://example.com", ""));
        part.Relationships.Add(CreateRelationship("rId6", XlsxRelationshipTypes.Hyperlink, "https://example.com", "Remote"));
        XlsxPartRelationshipsWriter writer = new XlsxPartRelationshipsWriter();

        XlsxRelationshipsWriteResult result = writer.WriteBytes(part);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "relationships.duplicateId"), Is.True);
        Assert.That(HasDiagnostic(result, "relationships.typeMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "path.traversal"), Is.True);
        Assert.That(HasDiagnostic(result, "relationship.target.externalModeRequired"), Is.True);
        Assert.That(HasDiagnostic(result, "relationships.targetModeUnsupported"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsNullPart()
    {
        XlsxPartRelationshipsWriter writer = new XlsxPartRelationshipsWriter();

        XlsxRelationshipsWriteResult? result = writer.WriteBytes(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "relationships.partNull"), Is.True);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
    }

    static XlsxRelationshipsPart CreateWorkbookRelationshipsPart()
    {
        XlsxRelationshipsPart part = new XlsxRelationshipsPart();
        part.SourcePartPath = XlsxPackagePaths.Workbook;
        part.PartPath = XlsxPackagePaths.WorkbookRelationships;
        return part;
    }

    static XlsxRelationship CreateRelationship(string id, string type, string target, string targetMode)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        relationship.TargetMode = targetMode;
        if (targetMode == "External")
        {
            relationship.IsExternal = true;
        }

        return relationship;
    }

    static XlsxXmlAttribute CreateAttribute(string namespaceUri, string prefix, string localName, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = namespaceUri;
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxRelationshipsWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
