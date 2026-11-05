using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageRelationshipsReaderTests
{
    [Test]
    public void ReadString_ParsesPackageRelationshipsAndResolvesTargets()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\" />" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\" />" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/book.xlsx\" TargetMode=\"External\" />" +
            "</Relationships>";
        XlsxPackageRelationshipsReader reader = new XlsxPackageRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.SourcePartPath, Is.EqualTo("/"));
        Assert.That(part.RelationshipElementCount, Is.EqualTo(3));
        Assert.That(part.Relationships.Count, Is.EqualTo(3));
        Assert.That(part.GetRelationship("rId1")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(part.GetRelationship("rId1")!.TargetMode, Is.EqualTo("Internal"));
        Assert.That(part.GetRelationship("rId2")!.TargetPath, Is.EqualTo(XlsxPackagePaths.CoreProperties));
        Assert.That(part.GetRelationship("rId3")!.IsExternal, Is.True);
        Assert.That(part.GetRelationship("rId3")!.ExternalTarget, Is.EqualTo("https://example.com/book.xlsx"));
        Assert.That(part.GetFirstByType(XlsxRelationshipTypes.OfficeDocument)!.Id, Is.EqualTo("rId1"));
    }

    [Test]
    public void ReadString_PreservesFragmentsForInternalTargets()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml#BookView\" />" +
            "</Relationships>";
        XlsxPackageRelationshipsReader reader = new XlsxPackageRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Relationships[0].TargetPath, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(part.Relationships[0].TargetFragment, Is.EqualTo("BookView"));
    }

    [Test]
    public void ReadString_ReportsMissingAttributesDuplicateIdsAndBadTargets()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\" />" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"xl/styles.xml\" />" +
            "<Relationship Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme\" Target=\"xl/theme/theme1.xml\" />" +
            "<Relationship Id=\"rId4\" Target=\"xl/sharedStrings.xml\" />" +
            "<Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" />" +
            "<Relationship Id=\"rId6\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"../evil.xml\" />" +
            "<Relationship Id=\"rId7\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com\" TargetMode=\"Remote\" />" +
            "<Relationship Id=\"rId8\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com\" />" +
            "</Relationships>";
        XlsxPackageRelationshipsReader reader = new XlsxPackageRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part, "relationships.duplicateId"), Is.True);
        Assert.That(HasDiagnostic(part, "relationships.idMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "relationships.typeMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "relationships.targetMissing"), Is.True);
        Assert.That(HasDiagnostic(part, "path.traversal"), Is.True);
        Assert.That(HasDiagnostic(part, "relationships.targetModeUnsupported"), Is.True);
        Assert.That(HasDiagnostic(part, "relationship.target.externalModeRequired"), Is.True);
        Assert.That(part.GetRelationship("rId1")!.TargetPath, Is.EqualTo(XlsxPackagePaths.Workbook));
    }

    [Test]
    public void ReadString_ReportsConfiguredRelationshipCountLimit()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\" />" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\" />" +
            "</Relationships>";
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxRelationshipsPerPart = 1;
        XlsxPackageRelationshipsReader reader = new XlsxPackageRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadStringWithOptions(xml, options);

        Assert.That(part.IsValid, Is.False);
        Assert.That(part.RelationshipElementCount, Is.EqualTo(2));
        Assert.That(HasDiagnostic(part, "relationships.maxRelationships"), Is.True);
        Assert.That(part.Relationships.Count, Is.EqualTo(2));
    }

    [Test]
    public void ReadString_PreservesUnknownAttributesAndElements()
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\" x:item=\"i\" />" +
            "<x:Future x:value=\"1\"><x:Child /></x:Future>" +
            "</Relationships>";
        XlsxPackageRelationshipsReader reader = new XlsxPackageRelationshipsReader();

        XlsxRelationshipsPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownRootAttributes[0].LocalName, Is.EqualTo("root"));
        Assert.That(part.Relationships[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Relationships[0].UnknownAttributes[0].LocalName, Is.EqualTo("item"));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml[0], Does.Contain("Future"));
        Assert.That(part.UnknownXml[0], Does.Contain("Child"));
    }

    [Test]
    public void ReadString_ReportsInvalidOrMissingRoot()
    {
        XlsxPackageRelationshipsReader reader = new XlsxPackageRelationshipsReader();

        XlsxRelationshipsPart invalidRoot = reader.ReadString("<Bad />");
        XlsxRelationshipsPart missingRoot = reader.ReadString("");

        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "relationships.invalidRoot"), Is.True);
        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "relationships.missingRoot"), Is.True);
    }

    static bool HasDiagnostic(XlsxRelationshipsPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
