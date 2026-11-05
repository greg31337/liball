using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DrawingRelationshipPreservationTests
{
    [Test]
    public void WorksheetReaderWriter_PreservesDrawingReferenceAndResolvesRelationship()
    {
        string worksheetXml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<sheetData />" +
            "<drawing r:id=\"rIdDrawing1\" x:future=\"1\" />" +
            "</worksheet>";
        XlsxRelationshipsPart relationships = ReadRelationships(
            XlsxPackagePaths.Worksheet1,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdDrawing1", XlsxRelationshipTypes.Drawing, "../drawings/drawing1.xml") +
            "</Relationships>");

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadStringWithRelationships(worksheetXml, relationships);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadStringWithRelationships(write.Xml, relationships);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasDrawing, Is.True);
        Assert.That(part.Drawing.RelationshipId, Is.EqualTo("rIdDrawing1"));
        Assert.That(part.Drawing.HasRelationship, Is.True);
        Assert.That(part.Drawing.TargetPath, Is.EqualTo("/xl/drawings/drawing1.xml"));
        Assert.That(part.Drawing.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<drawing"));
        Assert.That(write.Xml, Does.Contain("r:id=\"rIdDrawing1\""));
        Assert.That(write.Xml, Does.Contain("x:future=\"1\""));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasDrawing, Is.True);
        Assert.That(readBack.Drawing.TargetPath, Is.EqualTo("/xl/drawings/drawing1.xml"));
    }

    [Test]
    public void WorksheetDrawingRelationship_ReportsMissingWrongTypeAndInvalidIds()
    {
        XlsxWorksheetPart missingPart = new XlsxWorksheetPart();
        XlsxWorksheetPart wrongTypePart = new XlsxWorksheetPart();
        XlsxWorksheetPart invalidIdPart = new XlsxWorksheetPart();
        XlsxRelationshipsPart wrongTypeRelationships = new XlsxRelationshipsPart();
        XlsxRelationship wrongType = new XlsxRelationship();

        missingPart.HasDrawing = true;
        missingPart.Drawing.RelationshipId = "rIdDrawing1";
        wrongTypePart.HasDrawing = true;
        wrongTypePart.Drawing.RelationshipId = "rIdDrawing1";
        invalidIdPart.HasDrawing = true;
        invalidIdPart.Drawing.RelationshipId = "1bad";
        wrongTypeRelationships.SourcePartPath = XlsxPackagePaths.Worksheet1;
        wrongTypeRelationships.PartPath = "/xl/worksheets/_rels/sheet1.xml.rels";
        wrongType.Id = "rIdDrawing1";
        wrongType.Type = XlsxRelationshipTypes.Hyperlink;
        wrongType.Target = "https://example.com";
        wrongType.TargetMode = "External";
        wrongTypeRelationships.Relationships.Add(wrongType);

        XlsxSheetDrawingRelationshipResult? missingRelationships = missingPart.ResolveDrawingRelationship(null!);
        XlsxSheetDrawingRelationshipResult missingRelationship = missingPart.ResolveDrawingRelationship(new XlsxRelationshipsPart());
        XlsxSheetDrawingRelationshipResult wrongTypeResult = wrongTypePart.ResolveDrawingRelationship(wrongTypeRelationships);
        XlsxWorksheetPartWriteResult invalidIdWrite = new XlsxWorksheetPartWriter().WriteString(invalidIdPart);

        Assert.That(missingRelationships.IsValid, Is.False);
        Assert.That(missingRelationship.IsValid, Is.False);
        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(invalidIdWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRelationships, "worksheet.drawingRelationshipsMissing"), Is.True);
        Assert.That(HasDiagnostic(missingRelationship, "worksheet.drawingRelationshipMissing"), Is.True);
        Assert.That(HasDiagnostic(wrongTypeResult, "worksheet.drawingRelationshipType"), Is.True);
        Assert.That(HasDiagnostic(invalidIdWrite, "xml.relationshipId.invalid"), Is.True);
    }

    [Test]
    public void ChartsheetReaderWriter_PreservesDrawingReferenceAndResolvesRelationship()
    {
        string chartsheetXml = "<chartsheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<sheetViews />" +
            "<drawing r:id=\"rIdDrawing1\" x:future=\"1\" />" +
            "</chartsheet>";
        XlsxRelationshipsPart relationships = ReadRelationships(
            "/xl/chartsheets/sheet1.xml",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdDrawing1", XlsxRelationshipTypes.Drawing, "../drawings/drawing1.xml") +
            "</Relationships>");

        XlsxChartsheetPart part = new XlsxChartsheetPartReader().ReadStringWithRelationships(chartsheetXml, relationships);
        XlsxSheetDrawingRelationshipResult operationResolution = new XlsxChartsheetPartOperations().ResolveDrawingRelationship(part, relationships);
        part.RawXml = "";
        XlsxChartsheetPartWriteResult write = new XlsxChartsheetPartWriter().WriteString(part);
        XlsxChartsheetPart readBack = new XlsxChartsheetPartReader().ReadStringWithRelationships(write.Xml, relationships);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasDrawing, Is.True);
        Assert.That(part.Drawing.RelationshipId, Is.EqualTo("rIdDrawing1"));
        Assert.That(part.Drawing.HasRelationship, Is.True);
        Assert.That(part.Drawing.TargetPath, Is.EqualTo("/xl/drawings/drawing1.xml"));
        Assert.That(operationResolution.HasRelationship, Is.True);
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<sheetViews"));
        Assert.That(write.Xml, Does.Contain("<drawing"));
        Assert.That(write.Xml, Does.Contain("r:id=\"rIdDrawing1\""));
        Assert.That(write.Xml, Does.Contain("x:future=\"1\""));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasDrawing, Is.True);
        Assert.That(readBack.Drawing.TargetPath, Is.EqualTo("/xl/drawings/drawing1.xml"));
    }

    [Test]
    public void WorkbookSave_PreservesChartsheetDrawingRelationshipPartsAsUnknownParts()
    {
        byte[] originalBytes = CreateChartsheetDrawingPackage();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(originalBytes);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(savedPackage.IsValid, Is.True);
        AssertEntryText(savedPackage, "/xl/chartsheets/sheet1.xml", "<drawing r:id=\"rIdDrawing1\" />");
        AssertEntryText(savedPackage, "/xl/chartsheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.Drawing + "\"");
        AssertEntryText(savedPackage, "/xl/drawings/drawing1.xml", "<xdr:wsDr");
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Chartsheet);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Drawing);
    }

    static byte[] CreateChartsheetDrawingPackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/chartsheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Chartsheet + "\" />" +
            "<Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"" + XlsxContentTypes.Drawing + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Chart\" sheetId=\"1\" r:id=\"rId1\" /></sheets>" +
            "</workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.Chartsheet, "chartsheets/sheet1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/chartsheets/sheet1.xml",
            "<chartsheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><drawing r:id=\"rIdDrawing1\" /></chartsheet>"));
        entries.Add(CreateEntry("/xl/chartsheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdDrawing1", XlsxRelationshipTypes.Drawing, "../drawings/drawing1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/drawings/drawing1.xml",
            "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" />"));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = Encoding.UTF8.GetBytes(text);
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static XlsxRelationshipsPart ReadRelationships(string sourcePartPath, string xml)
    {
        return new XlsxPartRelationshipsReader().ReadString(sourcePartPath, xml);
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }

    static bool HasDiagnostic(XlsxSheetDrawingRelationshipResult result, string code)
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

    static bool HasDiagnostic(XlsxWorksheetPartWriteResult result, string code)
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
