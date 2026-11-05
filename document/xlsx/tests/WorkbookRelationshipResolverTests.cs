using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookRelationshipResolverTests
{
    [Test]
    public void Resolve_ClassifiesWorkbookRelationshipsAndAttachesSheets()
    {
        XlsxWorkbookPart workbook = ReadWorkbook(
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /><sheet name=\"Chart1\" sheetId=\"2\" r:id=\"rId2\" /></sheets>" +
            "</workbook>");
        XlsxRelationshipsPart relationships = ReadRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rId2", XlsxRelationshipTypes.Chartsheet, "chartsheets/sheet1.xml") +
            Relationship("rId3", XlsxRelationshipTypes.Theme, "theme/theme1.xml") +
            Relationship("rId4", XlsxRelationshipTypes.Styles, "styles.xml") +
            Relationship("rId5", XlsxRelationshipTypes.SharedStrings, "sharedStrings.xml") +
            Relationship("rId6", XlsxRelationshipTypes.ExternalLink, "externalLinks/externalLink1.xml") +
            Relationship("rId7", XlsxRelationshipTypes.PivotCacheDefinition, "pivotCache/pivotCacheDefinition1.xml") +
            Relationship("rId8", XlsxRelationshipTypes.VbaProject, "vbaProject.bin") +
            Relationship("rId9", "urn:future", "future/item.xml") +
            "</Relationships>");
        XlsxWorkbookRelationshipResolver resolver = new XlsxWorkbookRelationshipResolver();

        XlsxWorkbookRelationshipResolutionResult result = resolver.Resolve(workbook, relationships);
        IXlsxWorkbookRelationshipResolutionResultOperations operations = new XlsxWorkbookRelationshipResolutionResultOperations();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Relationships.Count, Is.EqualTo(9));
        Assert.That(result.Worksheets.Count, Is.EqualTo(1));
        Assert.That(result.Worksheets[0].HasSheet, Is.True);
        Assert.That(result.Worksheets[0].Sheet!.Name, Is.EqualTo("Sheet1"));
        Assert.That(result.Worksheets[0].TargetPath, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(result.Chartsheets.Count, Is.EqualTo(1));
        Assert.That(result.Chartsheets[0].Sheet!.Name, Is.EqualTo("Chart1"));
        Assert.That(result.Chartsheets[0].TargetPath, Is.EqualTo("/xl/chartsheets/sheet1.xml"));
        Assert.That(result.HasTheme, Is.True);
        Assert.That(result.Theme!.TargetPath, Is.EqualTo(XlsxPackagePaths.Theme));
        Assert.That(result.HasStyles, Is.True);
        Assert.That(result.Styles!.TargetPath, Is.EqualTo(XlsxPackagePaths.Styles));
        Assert.That(result.HasSharedStrings, Is.True);
        Assert.That(result.SharedStrings!.TargetPath, Is.EqualTo(XlsxPackagePaths.SharedStrings));
        Assert.That(result.ExternalLinks.Count, Is.EqualTo(1));
        Assert.That(result.ExternalLinks[0].TargetPath, Is.EqualTo("/xl/externalLinks/externalLink1.xml"));
        Assert.That(result.PivotCaches.Count, Is.EqualTo(1));
        Assert.That(result.PivotCaches[0].TargetPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition1.xml"));
        Assert.That(result.HasVbaProject, Is.True);
        Assert.That(result.VbaProject!.TargetPath, Is.EqualTo(XlsxPackagePaths.VbaProject));
        Assert.That(result.UnknownRelationships.Count, Is.EqualTo(1));
        Assert.That(result.UnknownRelationships[0].Id, Is.EqualTo("rId9"));
        Assert.That(operations.GetRelationship(result, "rId1")!.Kind, Is.EqualTo(XlsxWorkbookRelationshipKind.Worksheet));
        Assert.That(operations.GetRelationship(result, null), Is.Null);
        Assert.That(operations.GetFirstByKind(result, XlsxWorkbookRelationshipKind.SharedStrings)!.Id, Is.EqualTo("rId5"));
    }

    [Test]
    public void Resolve_ReportsMissingSheetRelationshipAndOrphanSheetRelationship()
    {
        XlsxWorkbookPart workbook = ReadWorkbook(
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Missing\" sheetId=\"1\" r:id=\"rIdMissing\" /></sheets>" +
            "</workbook>");
        XlsxRelationshipsPart relationships = ReadRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            "</Relationships>");

        XlsxWorkbookRelationshipResolutionResult result = new XlsxWorkbookRelationshipResolver().Resolve(workbook, relationships);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbook.relationship.sheetRelationshipMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "workbook.relationship.sheetMissing"), Is.True);
        Assert.That(result.Worksheets.Count, Is.EqualTo(1));
        Assert.That(result.Worksheets[0].HasSheet, Is.False);
    }

    [Test]
    public void Resolve_ReportsWorkbookSheetRelationshipWithNonSheetType()
    {
        XlsxWorkbookPart workbook = ReadWorkbook(
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>");
        XlsxRelationshipsPart relationships = ReadRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.Styles, "styles.xml") +
            "</Relationships>");

        XlsxWorkbookRelationshipResolutionResult result = new XlsxWorkbookRelationshipResolver().Resolve(workbook, relationships);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbook.relationship.sheetRelationshipType"), Is.True);
        Assert.That(result.HasStyles, Is.True);
    }

    [Test]
    public void Resolve_ReportsDuplicateSingletonRelationships()
    {
        XlsxWorkbookPart workbook = ReadWorkbook(
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>");
        XlsxRelationshipsPart relationships = ReadRelationships(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rId2", XlsxRelationshipTypes.Styles, "styles.xml") +
            Relationship("rId3", XlsxRelationshipTypes.Styles, "styles2.xml") +
            "</Relationships>");

        XlsxWorkbookRelationshipResolutionResult result = new XlsxWorkbookRelationshipResolver().Resolve(workbook, relationships);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "workbook.relationship.duplicateSingleton"), Is.True);
        Assert.That(result.HasStyles, Is.True);
        Assert.That(result.Styles!.Id, Is.EqualTo("rId2"));
    }

    static XlsxWorkbookPart ReadWorkbook(string xml)
    {
        return new XlsxWorkbookPartReader().ReadString(xml);
    }

    static XlsxRelationshipsPart ReadRelationships(string xml)
    {
        return new XlsxPartRelationshipsReader().ReadString(XlsxPackagePaths.WorkbookRelationships, xml);
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static bool HasDiagnostic(XlsxWorkbookRelationshipResolutionResult result, string code)
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
