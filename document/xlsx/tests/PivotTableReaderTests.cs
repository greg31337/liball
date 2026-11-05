using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotTableReaderTests
{
    [Test]
    public void PivotTableDefinitionReader_ReadsFieldsAreasDataFieldsFiltersGroupingAndStyles()
    {
        string xml = PivotTableDefinitionXml();

        XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionReader().ReadStringFromPart("/xl/pivotTables/pivotTable1.xml", xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Name, Is.EqualTo("PivotTable1"));
        Assert.That(part.HasCacheId, Is.True);
        Assert.That(part.CacheId, Is.EqualTo(1));
        Assert.That(part.HasLocation, Is.True);
        Assert.That(part.Location.Reference, Is.EqualTo("A3:D12"));
        Assert.That(part.Location.FirstDataColumn, Is.EqualTo(1));
        Assert.That(part.Fields.Count, Is.EqualTo(3));
        Assert.That(part.Fields[0].Name, Is.EqualTo("Region"));
        Assert.That(part.Fields[0].Axis, Is.EqualTo("axisRow"));
        Assert.That(part.Fields[0].ShowAll, Is.False);
        Assert.That(part.Fields[0].Items.Count, Is.EqualTo(2));
        Assert.That(part.Fields[0].Items[0].Index, Is.EqualTo(0));
        Assert.That(part.Fields[1].SortType, Is.EqualTo("ascending"));
        Assert.That(part.Fields[1].Items[0].Hidden, Is.True);
        Assert.That(part.Fields[1].HasGrouping, Is.True);
        Assert.That(part.Fields[1].GroupItems.Count, Is.EqualTo(1));
        Assert.That(part.Fields[2].HasDataField, Is.True);
        Assert.That(part.Fields[2].DataField, Is.True);
        Assert.That(part.RowFields.Count, Is.EqualTo(1));
        Assert.That(part.RowFields[0].FieldIndex, Is.EqualTo(0));
        Assert.That(part.ColumnFields.Count, Is.EqualTo(1));
        Assert.That(part.ColumnFields[0].FieldIndex, Is.EqualTo(1));
        Assert.That(part.PageFields.Count, Is.EqualTo(1));
        Assert.That(part.PageFields[0].FieldIndex, Is.EqualTo(0));
        Assert.That(part.PageFields[0].ItemIndex, Is.EqualTo(1));
        Assert.That(part.PageFields[0].Name, Is.EqualTo("Region Filter"));
        Assert.That(part.DataFields.Count, Is.EqualTo(1));
        Assert.That(part.DataFields[0].Name, Is.EqualTo("Sum of Amount"));
        Assert.That(part.DataFields[0].FieldIndex, Is.EqualTo(2));
        Assert.That(part.DataFields[0].Subtotal, Is.EqualTo("sum"));
        Assert.That(part.DataFields[0].BaseField, Is.EqualTo(-1));
        Assert.That(part.DataFields[0].NumberFormatId, Is.EqualTo(4));
        Assert.That(part.Filters.Count, Is.EqualTo(1));
        Assert.That(part.Filters[0].FieldIndex, Is.EqualTo(0));
        Assert.That(part.Filters[0].Type, Is.EqualTo("captionEqual"));
        Assert.That(part.Filters[0].Areas.Count, Is.EqualTo(1));
        Assert.That(part.Filters[0].Areas[0].References.Count, Is.EqualTo(1));
        Assert.That(part.Filters[0].Areas[0].References[0].Selected, Is.True);
        Assert.That(part.Formats.Count, Is.EqualTo(1));
        Assert.That(part.Formats[0].Action, Is.EqualTo("formatting"));
        Assert.That(part.Formats[0].DifferentialFormatId, Is.EqualTo(2));
        Assert.That(part.Formats[0].HasArea, Is.True);
        Assert.That(part.Formats[0].Area.Type, Is.EqualTo("data"));
        Assert.That(part.HasStyleInfo, Is.True);
        Assert.That(part.StyleInfo.Name, Is.EqualTo("PivotStyleMedium9"));
        Assert.That(part.StyleInfo.ShowColumnStripes, Is.True);
        Assert.That(part.RawXml, Is.EqualTo(xml));
    }

    [Test]
    public void PivotTableDefinitionReader_ResolvesCacheRelationship()
    {
        XlsxRelationshipsPart relationships = ReadRelationships(
            "/xl/pivotTables/pivotTable1.xml",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdCache1", XlsxRelationshipTypes.PivotCacheDefinition, "../pivotCache/pivotCacheDefinition1.xml") +
            "</Relationships>");

        XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionReader().ReadStringWithRelationships("/xl/pivotTables/pivotTable1.xml", PivotTableDefinitionXml(), relationships);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasCacheRelationship, Is.True);
        Assert.That(part.CacheRelationshipId, Is.EqualTo("rIdCache1"));
        Assert.That(part.CacheTargetPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition1.xml"));
    }

    [Test]
    public void WorkbookOpen_ReadsPivotTableDefinitionAndCacheRelationship()
    {
        byte[] bytes = CreatePivotTablePackage();

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook!.PivotTableDefinitions.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotTableDefinitions[0].Fields.Count, Is.EqualTo(3));
        Assert.That(open.Workbook.PivotTableDefinitions[0].DataFields.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotTableDefinitions[0].Filters.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.PivotTableDefinitions[0].HasCacheRelationship, Is.True);
        Assert.That(open.Workbook.PivotTableDefinitions[0].CacheTargetPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition1.xml"));
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

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string PivotTableDefinitionXml()
    {
        return "<pivotTableDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" name=\"PivotTable1\" cacheId=\"1\" dataCaption=\"Values\">" +
            "<location ref=\"A3:D12\" firstHeaderRow=\"1\" firstDataRow=\"2\" firstDataCol=\"1\" />" +
            "<pivotFields count=\"3\">" +
            "<pivotField name=\"Region\" axis=\"axisRow\" showAll=\"0\"><items count=\"2\"><item x=\"0\" /><item t=\"default\" /></items></pivotField>" +
            "<pivotField name=\"Quarter\" axis=\"axisCol\" sortType=\"ascending\"><items><item x=\"1\" h=\"1\" /></items><fieldGroup><rangePr groupBy=\"months\" /></fieldGroup><groupItems count=\"1\"><s v=\"Q1\" /></groupItems></pivotField>" +
            "<pivotField name=\"Amount\" dataField=\"1\" defaultSubtotal=\"0\" />" +
            "</pivotFields>" +
            "<rowFields count=\"1\"><field x=\"0\" /></rowFields>" +
            "<colFields count=\"1\"><field x=\"1\" /></colFields>" +
            "<pageFields count=\"1\"><pageField fld=\"0\" item=\"1\" name=\"Region Filter\" /></pageFields>" +
            "<dataFields count=\"1\"><dataField name=\"Sum of Amount\" fld=\"2\" subtotal=\"sum\" baseField=\"-1\" baseItem=\"0\" numFmtId=\"4\" /></dataFields>" +
            "<pivotFilters count=\"1\"><filter fld=\"0\" type=\"captionEqual\" id=\"1\" name=\"Region\"><pivotArea type=\"normal\" field=\"0\"><references count=\"1\"><reference field=\"0\" count=\"1\" selected=\"1\" byPosition=\"0\" /></references></pivotArea></filter></pivotFilters>" +
            "<formats count=\"1\"><format action=\"formatting\" dxfId=\"2\"><pivotArea type=\"data\" field=\"2\" /></format></formats>" +
            "<pivotTableStyleInfo name=\"PivotStyleMedium9\" showRowHeaders=\"1\" showColHeaders=\"1\" showRowStripes=\"0\" showColStripes=\"1\" showLastColumn=\"0\" />" +
            "</pivotTableDefinition>";
    }

    static string PivotCacheDefinitionXml()
    {
        return "<pivotCacheDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" refreshedBy=\"Unit Test\" recordCount=\"3\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:B4\" sheet=\"Sheet1\" /></cacheSource><cacheFields count=\"2\"><cacheField name=\"Category\"><sharedItems /></cacheField><cacheField name=\"Amount\"><sharedItems containsNumber=\"1\" /></cacheField></cacheFields></pivotCacheDefinition>";
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
}
