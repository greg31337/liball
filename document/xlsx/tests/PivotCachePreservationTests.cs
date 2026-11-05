using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotCachePreservationTests
{
    [Test]
    public void WorkbookReaderWriter_PreservesPivotCacheReferencesAndResolvesRelationships()
    {
        string workbookXml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets>" +
            "<pivotCaches><pivotCache cacheId=\"1\" r:id=\"rIdPivotCache1\" x:future=\"1\" /></pivotCaches>" +
            "</workbook>";
        XlsxRelationshipsPart relationships = ReadRelationships(
            XlsxPackagePaths.Workbook,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdPivotCache1", XlsxRelationshipTypes.PivotCacheDefinition, "pivotCache/pivotCacheDefinition1.xml") +
            "</Relationships>");

        XlsxWorkbookPart part = new XlsxWorkbookPartReader().ReadString(workbookXml);
        XlsxWorkbookRelationshipResolutionResult resolution = new XlsxWorkbookRelationshipResolver().Resolve(part, relationships);
        XlsxWorkbookPartWriteResult write = new XlsxWorkbookPartWriter().WriteString(part);
        XlsxWorkbookPart readBack = new XlsxWorkbookPartReader().ReadString(write.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PivotCaches.Count, Is.EqualTo(1));
        Assert.That(part.PivotCaches[0].HasCacheId, Is.True);
        Assert.That(part.PivotCaches[0].CacheId, Is.EqualTo(1));
        Assert.That(part.PivotCaches[0].RelationshipId, Is.EqualTo("rIdPivotCache1"));
        Assert.That(part.PivotCaches[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(resolution.IsValid, Is.True);
        Assert.That(part.PivotCaches[0].HasRelationship, Is.True);
        Assert.That(part.PivotCaches[0].TargetPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition1.xml"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<pivotCaches>"));
        Assert.That(write.Xml, Does.Contain("cacheId=\"1\""));
        Assert.That(write.Xml, Does.Contain("r:id=\"rIdPivotCache1\""));
        Assert.That(write.Xml, Does.Contain("x:future=\"1\""));
        Assert.That(readBack.PivotCaches.Count, Is.EqualTo(1));
    }

    [Test]
    public void PivotCacheDefinitionReaderWriter_PreservesRawXmlAndReadsMetadata()
    {
        string xml = PivotCacheDefinitionXml();
        XlsxPivotCacheDefinitionPart part = new XlsxPivotCacheDefinitionReader().ReadStringFromPart("/xl/pivotCache/pivotCacheDefinition1.xml", xml);
        XlsxPivotCacheDefinitionWriteResult write = new XlsxPivotCacheDefinitionWriter().WriteString(part);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition1.xml"));
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(part.HasRecordCount, Is.True);
        Assert.That(part.RecordCount, Is.EqualTo(3));
        Assert.That(part.RefreshedBy, Is.EqualTo("Unit Test"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void WorkbookOpenSave_PreservesPivotCacheDefinitionRecordsAndRelationships()
    {
        byte[] bytes = CreatePivotCachePackage();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.WorkbookPart.PivotCaches.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotCacheDefinitions.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotCacheDefinitions[0].RecordCount, Is.EqualTo(3));
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/pivotCache/pivotCacheDefinition1.xml"), Is.True);
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/pivotCache/pivotCacheRecords1.xml"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(savedPackage.IsValid, Is.True);
        AssertEntryText(savedPackage, XlsxPackagePaths.Workbook, "<pivotCaches>");
        AssertEntryText(savedPackage, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.PivotCacheDefinition + "\"");
        AssertEntryText(savedPackage, "/xl/pivotCache/pivotCacheDefinition1.xml", "recordCount=\"3\"");
        AssertEntryText(savedPackage, "/xl/pivotCache/_rels/pivotCacheDefinition1.xml.rels", "Type=\"" + XlsxRelationshipTypes.PivotCacheRecords + "\"");
        AssertEntryText(savedPackage, "/xl/pivotCache/pivotCacheRecords1.xml", "<r />");
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.PivotCacheDefinition);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.PivotCacheRecords);
    }

    [Test]
    public void WorkbookPivotCacheWriter_ReportsInvalidReferences()
    {
        XlsxWorkbookPart missingCacheId = CreateWorkbookPartWithPivotCache();
        missingCacheId.PivotCaches[0].HasCacheId = false;
        XlsxWorkbookPart missingRelationship = CreateWorkbookPartWithPivotCache();
        missingRelationship.PivotCaches[0].RelationshipId = "";
        XlsxWorkbookPart nullAttributes = CreateWorkbookPartWithPivotCache();
        nullAttributes.PivotCaches[0].UnknownAttributes = null!;

        XlsxWorkbookPartWriteResult missingCacheIdWrite = new XlsxWorkbookPartWriter().WriteString(missingCacheId);
        XlsxWorkbookPartWriteResult missingRelationshipWrite = new XlsxWorkbookPartWriter().WriteString(missingRelationship);
        XlsxWorkbookPartWriteResult nullAttributesWrite = new XlsxWorkbookPartWriter().WriteString(nullAttributes);

        Assert.That(missingCacheIdWrite.IsValid, Is.False);
        Assert.That(missingRelationshipWrite.IsValid, Is.False);
        Assert.That(nullAttributesWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingCacheIdWrite, "workbook.pivotCache.cacheIdMissing"), Is.True);
        Assert.That(HasDiagnostic(missingRelationshipWrite, "workbook.pivotCache.relationshipIdMissing"), Is.True);
        Assert.That(HasDiagnostic(nullAttributesWrite, "workbook.pivotCacheUnknownAttributesNull"), Is.True);
    }

    static byte[] CreatePivotCachePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
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
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>"));
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

    static string PivotCacheDefinitionXml()
    {
        return "<pivotCacheDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" refreshedBy=\"Unit Test\" recordCount=\"3\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:B4\" sheet=\"Sheet1\" /></cacheSource><cacheFields count=\"2\"><cacheField name=\"Category\"><sharedItems /></cacheField><cacheField name=\"Amount\"><sharedItems containsNumber=\"1\" /></cacheField></cacheFields></pivotCacheDefinition>";
    }

    static XlsxWorkbookPart CreateWorkbookPartWithPivotCache()
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        part.Sheets.Add(XlsxWorkbook.CreateSheet("Sheet1", 1, "rIdSheet1"));
        XlsxWorkbookPivotCacheReference cache = new XlsxWorkbookPivotCacheReference();
        cache.CacheId = 1;
        cache.HasCacheId = true;
        cache.RelationshipId = "rIdPivotCache1";
        part.PivotCaches.Add(cache);
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

    static bool HasDiagnostic(XlsxWorkbookPartWriteResult result, string code)
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
