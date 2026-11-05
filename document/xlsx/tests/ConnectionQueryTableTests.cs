using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ConnectionQueryTableTests
{
    [Test]
    public void ConnectionsAndQueryTableReaders_PreserveRawXmlAndReadMetadata()
    {
        string connectionsXml = ConnectionsXml();
        string queryTableXml = QueryTableXml();

        XlsxWorkbookConnectionsPart connections = new XlsxWorkbookConnectionsPartReader().ReadStringFromPart(XlsxPackagePaths.Connections, connectionsXml);
        XlsxQueryTablePart queryTable = new XlsxQueryTablePartReader().ReadString("/xl/queryTables/queryTable1.xml", queryTableXml);
        XlsxWorkbookConnectionsPartWriteResult connectionsWrite = new XlsxWorkbookConnectionsPartWriter().WriteString(connections);
        XlsxQueryTablePartWriteResult queryTableWrite = new XlsxQueryTablePartWriter().WriteString(queryTable);

        Assert.That(connections.IsValid, Is.True);
        Assert.That(connections.RawXml, Is.EqualTo(connectionsXml));
        Assert.That(connections.HasCount, Is.True);
        Assert.That(connections.Count, Is.EqualTo(1));
        Assert.That(connections.Connections.Count, Is.EqualTo(1));
        Assert.That(connections.Connections[0].HasId, Is.True);
        Assert.That(connections.Connections[0].Id, Is.EqualTo(1));
        Assert.That(connections.Connections[0].Name, Is.EqualTo("SalesConnection"));
        Assert.That(queryTable.IsValid, Is.True);
        Assert.That(queryTable.RawXml, Is.EqualTo(queryTableXml));
        Assert.That(queryTable.Name, Is.EqualTo("SalesQuery"));
        Assert.That(queryTable.HasConnectionId, Is.True);
        Assert.That(queryTable.ConnectionId, Is.EqualTo(1));
        Assert.That(queryTable.Fields.Count, Is.EqualTo(2));
        Assert.That(queryTable.Fields[1].Name, Is.EqualTo("Amount"));
        Assert.That(connectionsWrite.IsValid, Is.True);
        Assert.That(connectionsWrite.Xml, Is.EqualTo(connectionsXml));
        Assert.That(queryTableWrite.IsValid, Is.True);
        Assert.That(queryTableWrite.Xml, Is.EqualTo(queryTableXml));
    }

    [Test]
    public void WorkbookOpenSave_PreservesConnectionsAndQueryTables()
    {
        byte[] bytes = CreateConnectionQueryTablePackage();

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult saved = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saved.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.HasConnections, Is.True);
        Assert.That(open.Workbook.GetConnections().Connections.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.GetConnections().Connections[0].Name, Is.EqualTo("SalesConnection"));
        Assert.That(open.Workbook.ListQueryTables().Count, Is.EqualTo(1));
        Assert.That(open.Workbook.QueryTables[0].ConnectionId, Is.EqualTo(1));
        Assert.That(open.Workbook.QueryTables[0].Fields.Count, Is.EqualTo(2));
        Assert.That(saved.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Connections);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.QueryTable);
        AssertEntryText(package, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.Connections + "\"");
        AssertEntryText(package, XlsxPackagePaths.Connections, "SalesConnection");
        AssertEntryText(package, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.QueryTable + "\"");
        AssertEntryText(package, "/xl/tables/table1.xml", "connectionId=\"1\"");
        AssertEntryText(package, "/xl/tables/table1.xml", "queryTableFieldId=\"2\"");
        AssertEntryText(package, "/xl/queryTables/queryTable1.xml", "<queryTableField id=\"2\" name=\"Amount\"");
    }

    [Test]
    public void ConnectionAndQueryTableWriters_ReportInvalidParts()
    {
        XlsxWorkbookConnectionsPart connections = new XlsxWorkbookConnectionsPart();
        connections.Connections = null!;
        XlsxQueryTablePart queryTable = new XlsxQueryTablePart();
        queryTable.Fields = null!;

        XlsxWorkbookConnectionsPartWriteResult connectionsWrite = new XlsxWorkbookConnectionsPartWriter().WriteString(connections);
        XlsxQueryTablePartWriteResult queryTableWrite = new XlsxQueryTablePartWriter().WriteString(queryTable);

        Assert.That(connectionsWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(connectionsWrite.Validation, "connectionsWriter.connectionsNull"), Is.True);
        Assert.That(queryTableWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(queryTableWrite.Validation, "queryTableWriter.fieldsNull"), Is.True);
    }

    static byte[] CreateConnectionQueryTablePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/tables/table1.xml\" ContentType=\"" + XlsxContentTypes.Table + "\" />" +
            "<Override PartName=\"/xl/connections.xml\" ContentType=\"" + XlsxContentTypes.Connections + "\" />" +
            "<Override PartName=\"/xl/queryTables/queryTable1.xml\" ContentType=\"" + XlsxContentTypes.QueryTable + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdConnections", XlsxRelationshipTypes.Connections, "connections.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetData /><tableParts count=\"1\"><tablePart r:id=\"rIdTable1\" /></tableParts></worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdTable1", XlsxRelationshipTypes.Table, "../tables/table1.xml") +
            Relationship("rIdQueryTable1", XlsxRelationshipTypes.QueryTable, "../queryTables/queryTable1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/tables/table1.xml", TableXml()));
        entries.Add(CreateEntry(XlsxPackagePaths.Connections, ConnectionsXml()));
        entries.Add(CreateEntry("/xl/queryTables/queryTable1.xml", QueryTableXml()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string ConnectionsXml()
    {
        return "<connections xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"1\"><connection id=\"1\" name=\"SalesConnection\"><dbPr connection=\"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=sales.accdb\" command=\"SELECT * FROM Sales\" /></connection></connections>";
    }

    static string QueryTableXml()
    {
        return "<queryTable xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" name=\"SalesQuery\" connectionId=\"1\"><queryTableFields count=\"2\"><queryTableField id=\"1\" name=\"Region\" tableColumnId=\"1\" /><queryTableField id=\"2\" name=\"Amount\" tableColumnId=\"2\" /></queryTableFields></queryTable>";
    }

    static string TableXml()
    {
        return "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"1\" name=\"SalesTable\" displayName=\"SalesTable\" ref=\"A1:B3\" connectionId=\"1\"><tableColumns count=\"2\"><tableColumn id=\"1\" name=\"Region\" queryTableFieldId=\"1\" /><tableColumn id=\"2\" name=\"Amount\" queryTableFieldId=\"2\" /></tableColumns></table>";
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = Encoding.UTF8.GetBytes(text);
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
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

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
