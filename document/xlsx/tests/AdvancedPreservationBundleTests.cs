using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class AdvancedPreservationBundleTests
{
    [Test]
    public void WorkbookOpenSave_PreservesPivotSlicerMacroOleExternalLinkCustomXmlAndUnknownPartsTogether()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.AllowMacroEnabledPackages = true;
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(CreateAdvancedPreservationPackage(), options);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.PivotCacheDefinitions.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotTableDefinitions.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.SlicerCaches.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.Slicers.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.HasVbaProject, Is.True);
        Assert.That(open.Workbook.VbaProject.Data, Is.EqualTo(VbaBytes()));
        Assert.That(open.Workbook.ExternalLinks.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.ExternalLinks[0].Target, Is.EqualTo("file:///C:/Data/source.xlsx"));
        Assert.That(open.Workbook.EmbeddedObjects.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.EmbeddedObjects[0].Data, Is.EqualTo(OleBytes()));
        Assert.That(open.Workbook.CustomXmlParts.Count, Is.EqualTo(2));
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/future/future1.bin"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(savedPackage.IsValid, Is.True);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.PivotTable);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.SlicerCache);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Slicer);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.ExternalLink);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.CustomXmlProperties);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.OleObject);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, "application/vnd.example.future");
        AssertEntryText(savedPackage, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.VbaProject);
        AssertEntryText(savedPackage, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.ExternalLink);
        AssertEntryText(savedPackage, XlsxPackagePaths.WorkbookRelationships, XlsxRelationshipTypes.PivotCacheDefinition);
        AssertEntryText(savedPackage, XlsxPackagePaths.PackageRelationships, XlsxRelationshipTypes.CustomXml);
        AssertEntryText(savedPackage, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.PivotTable);
        AssertEntryText(savedPackage, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.OleObject);
        AssertEntryText(savedPackage, "/xl/worksheets/_rels/sheet1.xml.rels", XlsxRelationshipTypes.Slicer);
        AssertEntryText(savedPackage, "/xl/pivotTables/pivotTable1.xml", "name=\"PivotTable1\"");
        AssertEntryText(savedPackage, "/xl/slicerCaches/slicerCache1.xml", "sourceName=\"Region\"");
        AssertEntryText(savedPackage, "/xl/externalLinks/_rels/externalLink1.xml.rels", "TargetMode=\"External\"");
        AssertEntryText(savedPackage, "/customXml/item1.xml", "<customer");
        Assert.That(savedPackage.GetEntry(XlsxPackagePaths.VbaProject)!.Data, Is.EqualTo(VbaBytes()));
        Assert.That(savedPackage.GetEntry("/xl/embeddings/oleObject1.bin")!.Data, Is.EqualTo(OleBytes()));
        Assert.That(savedPackage.GetEntry("/xl/future/future1.bin")!.Data, Is.EqualTo(FutureBytes()));
    }

    static byte[] CreateAdvancedPreservationPackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.MacroEnabledWorkbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/vbaProject.bin\" ContentType=\"" + XlsxContentTypes.VbaProject + "\" />" +
            "<Override PartName=\"/xl/pivotTables/pivotTable1.xml\" ContentType=\"" + XlsxContentTypes.PivotTable + "\" />" +
            "<Override PartName=\"/xl/pivotCache/pivotCacheDefinition1.xml\" ContentType=\"" + XlsxContentTypes.PivotCacheDefinition + "\" />" +
            "<Override PartName=\"/xl/pivotCache/pivotCacheRecords1.xml\" ContentType=\"" + XlsxContentTypes.PivotCacheRecords + "\" />" +
            "<Override PartName=\"/xl/slicerCaches/slicerCache1.xml\" ContentType=\"" + XlsxContentTypes.SlicerCache + "\" />" +
            "<Override PartName=\"/xl/slicers/slicer1.xml\" ContentType=\"" + XlsxContentTypes.Slicer + "\" />" +
            "<Override PartName=\"/xl/externalLinks/externalLink1.xml\" ContentType=\"" + XlsxContentTypes.ExternalLink + "\" />" +
            "<Override PartName=\"/xl/embeddings/oleObject1.bin\" ContentType=\"" + XlsxContentTypes.OleObject + "\" />" +
            "<Override PartName=\"/customXml/itemProps1.xml\" ContentType=\"" + XlsxContentTypes.CustomXmlProperties + "\" />" +
            "<Override PartName=\"/xl/future/future1.bin\" ContentType=\"application/vnd.example.future\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdOffice", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            Relationship("rIdCustomXml1", XlsxRelationshipTypes.CustomXml, "customXml/item1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets>" +
            "<externalReferences><externalReference r:id=\"rIdExternalLink1\" /></externalReferences>" +
            "<pivotCaches><pivotCache cacheId=\"1\" r:id=\"rIdPivotCache1\" /></pivotCaches>" +
            "</workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdVbaProject", XlsxRelationshipTypes.VbaProject, "vbaProject.bin") +
            Relationship("rIdExternalLink1", XlsxRelationshipTypes.ExternalLink, "externalLinks/externalLink1.xml") +
            Relationship("rIdPivotCache1", XlsxRelationshipTypes.PivotCacheDefinition, "pivotCache/pivotCacheDefinition1.xml") +
            Relationship("rIdSlicerCache1", XlsxRelationshipTypes.SlicerCache, "slicerCaches/slicerCache1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheetData /><oleObjects><oleObject progId=\"Package\" shapeId=\"1025\" r:id=\"rIdOleObject1\" /></oleObjects><pivotTableDefinition r:id=\"rIdPivotTable1\" />" +
            "</worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdPivotTable1", XlsxRelationshipTypes.PivotTable, "../pivotTables/pivotTable1.xml") +
            Relationship("rIdOleObject1", XlsxRelationshipTypes.OleObject, "../embeddings/oleObject1.bin") +
            Relationship("rIdSlicer1", XlsxRelationshipTypes.Slicer, "../slicers/slicer1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/pivotTables/pivotTable1.xml", PivotTableXml()));
        entries.Add(CreateEntry("/xl/pivotTables/_rels/pivotTable1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdCache1", XlsxRelationshipTypes.PivotCacheDefinition, "../pivotCache/pivotCacheDefinition1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/pivotCache/pivotCacheDefinition1.xml", PivotCacheDefinitionXml()));
        entries.Add(CreateEntry("/xl/pivotCache/_rels/pivotCacheDefinition1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdRecords1", XlsxRelationshipTypes.PivotCacheRecords, "pivotCacheRecords1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/pivotCache/pivotCacheRecords1.xml", "<pivotCacheRecords xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"0\" />"));
        entries.Add(CreateEntry("/xl/slicerCaches/slicerCache1.xml", SlicerCacheXml()));
        entries.Add(CreateEntry("/xl/slicers/slicer1.xml", SlicerXml()));
        entries.Add(CreateEntry("/xl/externalLinks/externalLink1.xml", ExternalLinkXml()));
        entries.Add(CreateEntry("/xl/externalLinks/_rels/externalLink1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            ExternalRelationship("rIdExternalBook1", XlsxRelationshipTypes.ExternalLinkPath, "file:///C:/Data/source.xlsx") +
            "</Relationships>"));
        entries.Add(CreateEntry("/customXml/item1.xml", "<customer xmlns=\"urn:example:customer\"><name>Ada</name></customer>"));
        entries.Add(CreateEntry("/customXml/itemProps1.xml", "<ds:datastoreItem ds:itemID=\"{11111111-1111-1111-1111-111111111111}\" xmlns:ds=\"http://schemas.openxmlformats.org/officeDocument/2006/customXml\"><ds:schemaRefs /></ds:datastoreItem>"));
        entries.Add(CreateEntry("/customXml/_rels/item1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdProps", XlsxRelationshipTypes.CustomXmlProperties, "itemProps1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.VbaProject, VbaBytes()));
        entries.Add(CreateEntry("/xl/embeddings/oleObject1.bin", OleBytes()));
        entries.Add(CreateEntry("/xl/future/future1.bin", FutureBytes()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string PivotTableXml()
    {
        return "<pivotTableDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" name=\"PivotTable1\" cacheId=\"1\" dataCaption=\"Values\"><location ref=\"E3:G8\" firstHeaderRow=\"1\" firstDataRow=\"2\" firstDataCol=\"1\" /><pivotFields count=\"1\"><pivotField axis=\"axisRow\" showAll=\"1\" /></pivotFields><dataFields count=\"1\"><dataField name=\"Sum of Amount\" fld=\"0\" subtotal=\"sum\" /></dataFields></pivotTableDefinition>";
    }

    static string PivotCacheDefinitionXml()
    {
        return "<pivotCacheDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" recordCount=\"0\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:B5\" sheet=\"Sheet1\" /></cacheSource><cacheFields count=\"1\"><cacheField name=\"Amount\"><sharedItems /></cacheField></cacheFields></pivotCacheDefinition>";
    }

    static string SlicerCacheXml()
    {
        return "<slicerCacheDefinition xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\" name=\"Slicer_Region\" sourceName=\"Region\"><pivotTables><pivotTable tabId=\"1\" name=\"PivotTable1\" /></pivotTables></slicerCacheDefinition>";
    }

    static string SlicerXml()
    {
        return "<slicers xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\"><slicer name=\"Slicer_Region\" cache=\"Slicer_Region\" caption=\"Region\" /></slicers>";
    }

    static string ExternalLinkXml()
    {
        return "<externalLink xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><externalBook r:id=\"rIdExternalBook1\"><sheetNames><sheetName val=\"Sheet1\" /></sheetNames></externalBook></externalLink>";
    }

    static byte[] VbaBytes()
    {
        return new byte[] { 0x56, 0x42, 0x41, 0x01, 0x02 };
    }

    static byte[] OleBytes()
    {
        return new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0x10, 0x20 };
    }

    static byte[] FutureBytes()
    {
        return new byte[] { 0xF0, 0x0D, 0x12, 0x34 };
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        return CreateEntry(path, Encoding.UTF8.GetBytes(text));
    }

    static XlsxPackageEntry CreateEntry(string path, byte[] data)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = data;
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static string ExternalRelationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" TargetMode=\"External\" />";
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }
}
