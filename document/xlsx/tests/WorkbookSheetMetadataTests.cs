using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookSheetMetadataTests
{
    [Test]
    public void RelationshipResolver_AppliesConcreteSheetMetadataForAllSheetTypes()
    {
        XlsxWorkbookPart workbook = new XlsxWorkbookPartReader().ReadString(
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets>" +
            "<sheet name=\"Worksheet\" sheetId=\"1\" r:id=\"rId1\" />" +
            "<sheet name=\"Chart\" sheetId=\"2\" r:id=\"rId2\" />" +
            "<sheet name=\"Macro\" sheetId=\"3\" r:id=\"rId3\" />" +
            "<sheet name=\"Dialog\" sheetId=\"4\" r:id=\"rId4\" />" +
            "</sheets>" +
            "</workbook>");
        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadString(
            XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rId2", XlsxRelationshipTypes.Chartsheet, "chartsheets/sheet1.xml") +
            Relationship("rId3", XlsxRelationshipTypes.Macrosheet, "macrosheets/sheet1.xml") +
            Relationship("rId4", XlsxRelationshipTypes.Dialogsheet, "dialogSheets/sheet1.xml") +
            "</Relationships>");

        XlsxWorkbookRelationshipResolutionResult result = new XlsxWorkbookRelationshipResolver().Resolve(workbook, relationships);

        Assert.That(result.IsValid, Is.True);
        AssertSheetMetadata(workbook.GetSheetByName("Worksheet"), XlsxSheetType.Worksheet, XlsxRelationshipTypes.Worksheet, XlsxContentTypes.Worksheet, XlsxPackagePaths.Worksheet1);
        AssertSheetMetadata(workbook.GetSheetByName("Chart"), XlsxSheetType.Chartsheet, XlsxRelationshipTypes.Chartsheet, XlsxContentTypes.Chartsheet, "/xl/chartsheets/sheet1.xml");
        AssertSheetMetadata(workbook.GetSheetByName("Macro"), XlsxSheetType.Macrosheet, XlsxRelationshipTypes.Macrosheet, XlsxContentTypes.Macrosheet, "/xl/macrosheets/sheet1.xml");
        AssertSheetMetadata(workbook.GetSheetByName("Dialog"), XlsxSheetType.Dialogsheet, XlsxRelationshipTypes.Dialogsheet, XlsxContentTypes.Dialogsheet, "/xl/dialogSheets/sheet1.xml");
    }

    [Test]
    public void OpenBytes_PopulatesWorksheetMetadataFromWorkbookRelationships()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.AddSheet("Data");

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);

        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        AssertSheetMetadata(openResult.Workbook!.GetSheet("Sheet1"), XlsxSheetType.Worksheet, XlsxRelationshipTypes.Worksheet, XlsxContentTypes.Worksheet, XlsxPackagePaths.Worksheet1);
        AssertSheetMetadata(openResult.Workbook.GetSheet("Data"), XlsxSheetType.Worksheet, XlsxRelationshipTypes.Worksheet, XlsxContentTypes.Worksheet, "/xl/worksheets/sheet2.xml");
    }

    [Test]
    public void CreateSheet_InitializesExplicitSheetTypeMetadata()
    {
        XlsxWorkbookSheet chart = XlsxWorkbook.CreateSheet("Chart", 2, "rId2", XlsxSheetType.Chartsheet);

        Assert.That(chart.Name, Is.EqualTo("Chart"));
        Assert.That(chart.SheetType, Is.EqualTo(XlsxSheetType.Chartsheet));
        Assert.That(chart.ContentType, Is.EqualTo(XlsxContentTypes.Chartsheet));
        Assert.That(chart.HasRelationshipMetadata, Is.False);
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static void AssertSheetMetadata(XlsxWorkbookSheet? sheet, XlsxSheetType sheetType, string relationshipType, string contentType, string targetPath)
    {
        Assert.That(sheet, Is.Not.Null);
        Assert.That(sheet!.HasRelationshipMetadata, Is.True);
        Assert.That(sheet.SheetType, Is.EqualTo(sheetType));
        Assert.That(sheet.RelationshipType, Is.EqualTo(relationshipType));
        Assert.That(sheet.ContentType, Is.EqualTo(contentType));
        Assert.That(sheet.TargetPath, Is.EqualTo(targetPath));
    }
}
