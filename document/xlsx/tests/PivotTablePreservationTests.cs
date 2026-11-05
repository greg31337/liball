using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotTablePreservationTests
{
    [Test]
    public void WorksheetReaderWriter_PreservesPivotTableReferenceAndResolvesRelationship()
    {
        string worksheetXml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<sheetData />" +
            "<pivotTableDefinition r:id=\"rIdPivotTable1\" x:future=\"1\" />" +
            "</worksheet>";
        XlsxRelationshipsPart relationships = ReadRelationships(
            XlsxPackagePaths.Worksheet1,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdPivotTable1", XlsxRelationshipTypes.PivotTable, "../pivotTables/pivotTable1.xml") +
            "</Relationships>");

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadStringWithRelationships(worksheetXml, relationships);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadStringWithRelationships(write.Xml, relationships);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PivotTableReferences.Count, Is.EqualTo(1));
        Assert.That(part.PivotTableReferences[0].RelationshipId, Is.EqualTo("rIdPivotTable1"));
        Assert.That(part.PivotTableReferences[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.PivotTableReferences[0].HasRelationship, Is.True);
        Assert.That(part.PivotTableReferences[0].TargetPath, Is.EqualTo("/xl/pivotTables/pivotTable1.xml"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<pivotTableDefinition"));
        Assert.That(write.Xml, Does.Contain("r:id=\"rIdPivotTable1\""));
        Assert.That(write.Xml, Does.Contain("x:future=\"1\""));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.PivotTableReferences.Count, Is.EqualTo(1));
        Assert.That(readBack.PivotTableReferences[0].TargetPath, Is.EqualTo("/xl/pivotTables/pivotTable1.xml"));
    }

    [Test]
    public void PivotTableDefinitionReaderWriter_PreservesRawXmlAndReadsMetadata()
    {
        string xml = PivotTableDefinitionXml();
        XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionReader().ReadStringFromPart("/xl/pivotTables/pivotTable1.xml", xml);
        XlsxPivotTableDefinitionWriteResult write = new XlsxPivotTableDefinitionWriter().WriteString(part);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo("/xl/pivotTables/pivotTable1.xml"));
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(part.Name, Is.EqualTo("PivotTable1"));
        Assert.That(part.HasCacheId, Is.True);
        Assert.That(part.CacheId, Is.EqualTo(1));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WorkbookOpenSave_PreservesPivotTableDefinitionRelationshipsAndCacheLinks()
    {
        byte[] bytes = CreatePivotTablePackage();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.WorkbookPart.PivotCaches.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotCacheDefinitions.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotTableDefinitions.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotTableDefinitions[0].Name, Is.EqualTo("PivotTable1"));
        Assert.That(open.Workbook.PivotTableDefinitions[0].CacheId, Is.EqualTo(1));
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/pivotTables/pivotTable1.xml"), Is.True);
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/pivotTables/_rels/pivotTable1.xml.rels"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(savedPackage.IsValid, Is.True);
        AssertEntryText(savedPackage, XlsxPackagePaths.Workbook, "<pivotCaches>");
        AssertEntryText(savedPackage, XlsxPackagePaths.Worksheet1, "<pivotTableDefinition");
        AssertEntryText(savedPackage, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.PivotTable + "\"");
        AssertEntryText(savedPackage, "/xl/pivotTables/pivotTable1.xml", "name=\"PivotTable1\"");
        AssertEntryText(savedPackage, "/xl/pivotTables/_rels/pivotTable1.xml.rels", "Type=\"" + XlsxRelationshipTypes.PivotCacheDefinition + "\"");
        AssertEntryText(savedPackage, "/xl/pivotCache/pivotCacheDefinition1.xml", "recordCount=\"3\"");
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.PivotTable);
    }

    [Test]
    public void WorksheetPivotTableWriterAndResolver_ReportInvalidReferences()
    {
        XlsxWorksheetPart missingRelationship = CreateWorksheetPartWithPivotTable();
        missingRelationship.PivotTableReferences[0].RelationshipId = "";
        XlsxWorksheetPart nullAttributes = CreateWorksheetPartWithPivotTable();
        nullAttributes.PivotTableReferences[0].UnknownAttributes = null!;
        XlsxWorksheetPart wrongTypePart = CreateWorksheetPartWithPivotTable();
        XlsxRelationshipsPart wrongTypeRelationships = ReadRelationships(
            XlsxPackagePaths.Worksheet1,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdPivotTable1", XlsxRelationshipTypes.Drawing, "../drawings/drawing1.xml") +
            "</Relationships>");

        XlsxWorksheetPartWriteResult missingRelationshipWrite = new XlsxWorksheetPartWriter().WriteString(missingRelationship);
        XlsxWorksheetPartWriteResult nullAttributesWrite = new XlsxWorksheetPartWriter().WriteString(nullAttributes);
        XlsxWorksheetPivotTableRelationshipResult? missingRelationships = wrongTypePart.ResolvePivotTableRelationships(null!);
        XlsxWorksheetPivotTableRelationshipResult wrongTypeResult = wrongTypePart.ResolvePivotTableRelationships(wrongTypeRelationships);

        Assert.That(missingRelationshipWrite.IsValid, Is.False);
        Assert.That(nullAttributesWrite.IsValid, Is.False);
        Assert.That(missingRelationships.IsValid, Is.False);
        Assert.That(wrongTypeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRelationshipWrite, "worksheet.pivotTableRelationshipIdMissing"), Is.True);
        Assert.That(HasDiagnostic(nullAttributesWrite, "worksheet.pivotTableUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(missingRelationships, "worksheet.pivotTableRelationshipsMissing"), Is.True);
        Assert.That(HasDiagnostic(wrongTypeResult, "worksheet.pivotTableRelationshipType"), Is.True);
    }

    static byte[] CreatePivotTablePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/pivotTables/pivotTable1.xml\" ContentType=\"" + XlsxContentTypes.PivotTable + "\" />" +
            "<Override PartName=\"/xl/pivotCache/pivotCacheDefinition1.xml\" ContentType=\"" + XlsxContentTypes.PivotCacheDefinition + "\" />" +
            "<Override PartName=\"/xl/pivotCache/pivotCacheRecords1.xml\" ContentType=\"" + XlsxContentTypes.PivotCacheRecords + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets><pivotCaches><pivotCache cacheId=\"1\" r:id=\"rIdPivotCache1\" /></pivotCaches></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdPivotCache1", XlsxRelationshipTypes.PivotCacheDefinition, "pivotCache/pivotCacheDefinition1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetData /><pivotTableDefinition r:id=\"rIdPivotTable1\" /></worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdPivotTable1", XlsxRelationshipTypes.PivotTable, "../pivotTables/pivotTable1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/pivotTables/pivotTable1.xml", PivotTableDefinitionXml()));
        entries.Add(CreateEntry("/xl/pivotTables/_rels/pivotTable1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdCache1", XlsxRelationshipTypes.PivotCacheDefinition, "../pivotCache/pivotCacheDefinition1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/pivotCache/pivotCacheDefinition1.xml", PivotCacheDefinitionXml()));
        entries.Add(CreateEntry("/xl/pivotCache/_rels/pivotCacheDefinition1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdRecords1", XlsxRelationshipTypes.PivotCacheRecords, "pivotCacheRecords1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/pivotCache/pivotCacheRecords1.xml",
            "<pivotCacheRecords xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"1\"><r /></pivotCacheRecords>"));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string PivotTableDefinitionXml()
    {
        return "<pivotTableDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" name=\"PivotTable1\" cacheId=\"1\" dataCaption=\"Values\"><location ref=\"A3:C10\" firstHeaderRow=\"1\" firstDataRow=\"2\" firstDataCol=\"1\" /><pivotFields count=\"1\"><pivotField axis=\"axisRow\" showAll=\"0\" /></pivotFields></pivotTableDefinition>";
    }

    static string PivotCacheDefinitionXml()
    {
        return "<pivotCacheDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" refreshedBy=\"Unit Test\" recordCount=\"3\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:B4\" sheet=\"Sheet1\" /></cacheSource><cacheFields count=\"2\"><cacheField name=\"Category\"><sharedItems /></cacheField><cacheField name=\"Amount\"><sharedItems containsNumber=\"1\" /></cacheField></cacheFields></pivotCacheDefinition>";
    }

    static XlsxWorksheetPart CreateWorksheetPartWithPivotTable()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetPivotTableReference pivotTable = new XlsxWorksheetPivotTableReference();
        pivotTable.RelationshipId = "rIdPivotTable1";
        part.PivotTableReferences.Add(pivotTable);
        return part;
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

    static bool HasDiagnostic(XlsxWorksheetPivotTableRelationshipResult result, string code)
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
