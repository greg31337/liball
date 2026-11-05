using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class GoldenFileFixtureTests
{
    [Test]
    public void MinimumWorkbook_GoldenManifestAndCoreXmlAreStable()
    {
        XlsxWorkbookSaveResult save = XlsxWorkbook.Create().SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());
        XlsxValidationResult minimum = new XlsxMinimumPackageValidator().ValidateBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(save.IsValid, Is.True);
        Assert.That(package.IsValid, Is.True);
        Assert.That(minimum.IsValid, Is.True);
        Assert.That(BuildManifest(package), Is.EqualTo(
            "/[Content_Types].xml\n" +
            "/_rels/.rels\n" +
            "/xl/_rels/workbook.xml.rels\n" +
            "/xl/workbook.xml\n" +
            "/xl/worksheets/sheet1.xml"));
        AssertEntryText(package, XlsxPackagePaths.Workbook, "<sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" />");
        AssertEntryText(package, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\"");
    }

    [Test]
    public void ComprehensiveWorkbook_GoldenManifestCoversAdvancedFeatureParts()
    {
        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(CreateComprehensiveFixtureEntries(), new XlsxWriterOptions());
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(write.Bytes, new XlsxReaderOptions());
        XlsxValidationResult minimum = new XlsxMinimumPackageValidator().ValidateBytes(write.Bytes, new XlsxReaderOptions());

        Assert.That(write.IsValid, Is.True);
        Assert.That(package.IsValid, Is.True);
        Assert.That(minimum.IsValid, Is.True);
        Assert.That(BuildManifest(package), Is.EqualTo(
            "/[Content_Types].xml\n" +
            "/_rels/.rels\n" +
            "/xl/_rels/workbook.xml.rels\n" +
            "/xl/charts/chart1.xml\n" +
            "/xl/comments/comment1.xml\n" +
            "/xl/custom/item1.xml\n" +
            "/xl/drawings/_rels/drawing1.xml.rels\n" +
            "/xl/drawings/drawing1.xml\n" +
            "/xl/pivotCache/pivotCacheDefinition1.xml\n" +
            "/xl/pivotTables/pivotTable1.xml\n" +
            "/xl/sharedStrings.xml\n" +
            "/xl/styles.xml\n" +
            "/xl/tables/table1.xml\n" +
            "/xl/vbaProject.bin\n" +
            "/xl/workbook.xml\n" +
            "/xl/worksheets/_rels/sheet1.xml.rels\n" +
            "/xl/worksheets/sheet1.xml"));

        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.MacroEnabledWorkbook);
        AssertEntryText(package, XlsxPackagePaths.Workbook, "<pivotCache cacheId=\"1\" r:id=\"rIdPivotCache1\" />");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<c r=\"A2\"><v>42</v></c>");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<f>A2*2</f><v>84</v>");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<dataValidation type=\"whole\" sqref=\"A2:A10\"><formula1>1</formula1><formula2>100</formula2></dataValidation>");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<conditionalFormatting sqref=\"B2:B10\"><cfRule type=\"cellIs\" priority=\"1\" operator=\"greaterThan\"><formula>10</formula></cfRule></conditionalFormatting>");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<hyperlink ref=\"D2\" r:id=\"rIdHyperlink1\" display=\"Example\" />");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<drawing r:id=\"rIdDrawing1\" />");
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<tableParts count=\"1\"><tablePart r:id=\"rIdTable1\" /></tableParts>");
        AssertEntryText(package, XlsxPackagePaths.SharedStrings, "<si><t>Shared text</t></si>");
        AssertEntryText(package, XlsxPackagePaths.Styles, "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" /></cellXfs>");
        AssertEntryText(package, "/xl/tables/table1.xml", "<tableColumn id=\"2\" name=\"Amount\" />");
        AssertEntryText(package, "/xl/comments/comment1.xml", "<text><t>Golden comment</t></text>");
        AssertEntryText(package, "/xl/drawings/drawing1.xml", "<c:chart r:id=\"rIdChart1\" />");
        AssertEntryText(package, "/xl/charts/chart1.xml", "<c:title><c:tx><c:rich><a:p><a:r><a:t>Golden Chart</a:t></a:r></a:p></c:rich></c:tx></c:title>");
        AssertEntryText(package, "/xl/pivotCache/pivotCacheDefinition1.xml", "ref=\"A1:B10\"");
        AssertEntryText(package, "/xl/pivotTables/pivotTable1.xml", "name=\"GoldenPivot\"");
        AssertEntryText(package, "/xl/custom/item1.xml", "<golden unknown=\"1\" />");
        Assert.That(package.GetEntry(XlsxPackagePaths.VbaProject)!.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    static List<XlsxPackageEntry> CreateComprehensiveFixtureEntries()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateTextEntry(XlsxPackagePaths.ContentTypes, ComprehensiveContentTypesXml()));
        entries.Add(CreateTextEntry(XlsxPackagePaths.PackageRelationships, PackageRelationshipsXml()));
        entries.Add(CreateTextEntry(XlsxPackagePaths.Workbook, WorkbookXml()));
        entries.Add(CreateTextEntry(XlsxPackagePaths.WorkbookRelationships, WorkbookRelationshipsXml()));
        entries.Add(CreateTextEntry(XlsxPackagePaths.Worksheet1, WorksheetXml()));
        entries.Add(CreateTextEntry("/xl/worksheets/_rels/sheet1.xml.rels", WorksheetRelationshipsXml()));
        entries.Add(CreateTextEntry(XlsxPackagePaths.SharedStrings, SharedStringsXml()));
        entries.Add(CreateTextEntry(XlsxPackagePaths.Styles, StylesXml()));
        entries.Add(CreateTextEntry("/xl/tables/table1.xml", TableXml()));
        entries.Add(CreateTextEntry("/xl/comments/comment1.xml", CommentsXml()));
        entries.Add(CreateTextEntry("/xl/drawings/drawing1.xml", DrawingXml()));
        entries.Add(CreateTextEntry("/xl/drawings/_rels/drawing1.xml.rels", DrawingRelationshipsXml()));
        entries.Add(CreateTextEntry("/xl/charts/chart1.xml", ChartXml()));
        entries.Add(CreateTextEntry("/xl/pivotCache/pivotCacheDefinition1.xml", PivotCacheXml()));
        entries.Add(CreateTextEntry("/xl/pivotTables/pivotTable1.xml", PivotTableXml()));
        entries.Add(CreateBinaryEntry(XlsxPackagePaths.VbaProject, new byte[] { 1, 2, 3, 4 }));
        entries.Add(CreateTextEntry("/xl/custom/item1.xml", "<golden unknown=\"1\" />"));
        return entries;
    }

    static string BuildManifest(XlsxZipPackageReadResult package)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        foreach (XlsxPackageEntry entry in package.Entries)
        {
            if (index > 0)
            {
                builder.Append("\n");
            }

            builder.Append(entry.Path);
            index = index + 1;
        }

        return builder.ToString();
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }

    static XlsxPackageEntry CreateTextEntry(string path, string xml)
    {
        return CreateBinaryEntry(path, Encoding.UTF8.GetBytes(xml));
    }

    static XlsxPackageEntry CreateBinaryEntry(string path, byte[] bytes)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = bytes;
        return entry;
    }

    static string ComprehensiveContentTypesXml()
    {
        return "<Types xmlns=\"" + XlsxNamespaces.ContentTypes + "\">" +
            "<Default Extension=\"rels\" ContentType=\"" + XlsxContentTypes.Relationships + "\" />" +
            "<Default Extension=\"xml\" ContentType=\"" + XlsxContentTypes.Xml + "\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.MacroEnabledWorkbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"" + XlsxContentTypes.Styles + "\" />" +
            "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"" + XlsxContentTypes.SharedStrings + "\" />" +
            "<Override PartName=\"/xl/tables/table1.xml\" ContentType=\"" + XlsxContentTypes.Table + "\" />" +
            "<Override PartName=\"/xl/comments/comment1.xml\" ContentType=\"" + XlsxContentTypes.Comments + "\" />" +
            "<Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"" + XlsxContentTypes.Drawing + "\" />" +
            "<Override PartName=\"/xl/charts/chart1.xml\" ContentType=\"" + XlsxContentTypes.Chart + "\" />" +
            "<Override PartName=\"/xl/pivotCache/pivotCacheDefinition1.xml\" ContentType=\"" + XlsxContentTypes.PivotCacheDefinition + "\" />" +
            "<Override PartName=\"/xl/pivotTables/pivotTable1.xml\" ContentType=\"" + XlsxContentTypes.PivotTable + "\" />" +
            "<Override PartName=\"/xl/vbaProject.bin\" ContentType=\"" + XlsxContentTypes.VbaProject + "\" />" +
            "<Override PartName=\"/xl/custom/item1.xml\" ContentType=\"application/vnd.example.golden+xml\" />" +
            "</Types>";
    }

    static string PackageRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + XlsxRelationshipTypes.OfficeDocument + "\" Target=\"xl/workbook.xml\" />" +
            "</Relationships>";
    }

    static string WorkbookRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rIdSheet1\" Type=\"" + XlsxRelationshipTypes.Worksheet + "\" Target=\"worksheets/sheet1.xml\" />" +
            "<Relationship Id=\"rIdStyles1\" Type=\"" + XlsxRelationshipTypes.Styles + "\" Target=\"styles.xml\" />" +
            "<Relationship Id=\"rIdSharedStrings1\" Type=\"" + XlsxRelationshipTypes.SharedStrings + "\" Target=\"sharedStrings.xml\" />" +
            "<Relationship Id=\"rIdPivotCache1\" Type=\"" + XlsxRelationshipTypes.PivotCacheDefinition + "\" Target=\"pivotCache/pivotCacheDefinition1.xml\" />" +
            "<Relationship Id=\"rIdVba1\" Type=\"" + XlsxRelationshipTypes.VbaProject + "\" Target=\"vbaProject.bin\" />" +
            "</Relationships>";
    }

    static string WorkbookXml()
    {
        return "<workbook xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets>" +
            "<pivotCaches><pivotCache cacheId=\"1\" r:id=\"rIdPivotCache1\" /></pivotCaches>" +
            "</workbook>";
    }

    static string WorksheetRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rIdTable1\" Type=\"" + XlsxRelationshipTypes.Table + "\" Target=\"../tables/table1.xml\" />" +
            "<Relationship Id=\"rIdComments1\" Type=\"" + XlsxRelationshipTypes.Comments + "\" Target=\"../comments/comment1.xml\" />" +
            "<Relationship Id=\"rIdHyperlink1\" Type=\"" + XlsxRelationshipTypes.Hyperlink + "\" Target=\"https://example.com\" TargetMode=\"External\" />" +
            "<Relationship Id=\"rIdDrawing1\" Type=\"" + XlsxRelationshipTypes.Drawing + "\" Target=\"../drawings/drawing1.xml\" />" +
            "<Relationship Id=\"rIdPivotTable1\" Type=\"" + XlsxRelationshipTypes.PivotTable + "\" Target=\"../pivotTables/pivotTable1.xml\" />" +
            "</Relationships>";
    }

    static string DrawingRelationshipsXml()
    {
        return "<Relationships xmlns=\"" + XlsxNamespaces.PackageRelationships + "\">" +
            "<Relationship Id=\"rIdChart1\" Type=\"" + XlsxRelationshipTypes.Chart + "\" Target=\"../charts/chart1.xml\" />" +
            "</Relationships>";
    }

    static string WorksheetXml()
    {
        return "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<sheetData><row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\"><f>A2*2</f><v>84</v></c></row><row r=\"2\"><c r=\"A2\"><v>42</v></c><c r=\"B2\" t=\"inlineStr\"><is><t>Inline</t></is></c></row></sheetData>" +
            "<dataValidations count=\"1\"><dataValidation type=\"whole\" sqref=\"A2:A10\"><formula1>1</formula1><formula2>100</formula2></dataValidation></dataValidations>" +
            "<conditionalFormatting sqref=\"B2:B10\"><cfRule type=\"cellIs\" priority=\"1\" operator=\"greaterThan\"><formula>10</formula></cfRule></conditionalFormatting>" +
            "<hyperlinks><hyperlink ref=\"D2\" r:id=\"rIdHyperlink1\" display=\"Example\" /></hyperlinks>" +
            "<drawing r:id=\"rIdDrawing1\" />" +
            "<tableParts count=\"1\"><tablePart r:id=\"rIdTable1\" /></tableParts>" +
            "<pivotTableDefinition r:id=\"rIdPivotTable1\" />" +
            "</worksheet>";
    }

    static string SharedStringsXml()
    {
        return "<sst xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" count=\"1\" uniqueCount=\"1\"><si><t>Shared text</t></si></sst>";
    }

    static string StylesXml()
    {
        return "<styleSheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><fonts count=\"1\"><font><sz val=\"11\" /></font></fonts><fills count=\"1\"><fill><patternFill patternType=\"none\" /></fill></fills><borders count=\"1\"><border /></borders><cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" /></cellXfs></styleSheet>";
    }

    static string TableXml()
    {
        return "<table xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" id=\"1\" name=\"GoldenTable\" displayName=\"GoldenTable\" ref=\"A1:B10\"><tableColumns count=\"2\"><tableColumn id=\"1\" name=\"Label\" /><tableColumn id=\"2\" name=\"Amount\" /></tableColumns></table>";
    }

    static string CommentsXml()
    {
        return "<comments xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><authors><author>Codex</author></authors><commentList><comment ref=\"A2\" authorId=\"0\"><text><t>Golden comment</t></text></comment></commentList></comments>";
    }

    static string DrawingXml()
    {
        return "<xdr:wsDr xmlns:xdr=\"" + XlsxNamespaces.SpreadsheetDrawing + "\" xmlns:a=\"" + XlsxNamespaces.Drawing + "\" xmlns:c=\"" + XlsxNamespaces.Chart + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\"><xdr:twoCellAnchor><xdr:from><xdr:col>0</xdr:col><xdr:row>0</xdr:row></xdr:from><xdr:to><xdr:col>4</xdr:col><xdr:row>10</xdr:row></xdr:to><xdr:graphicFrame><a:graphic><a:graphicData uri=\"" + XlsxNamespaces.Chart + "\"><c:chart r:id=\"rIdChart1\" /></a:graphicData></a:graphic></xdr:graphicFrame></xdr:twoCellAnchor></xdr:wsDr>";
    }

    static string ChartXml()
    {
        return "<c:chartSpace xmlns:c=\"" + XlsxNamespaces.Chart + "\" xmlns:a=\"" + XlsxNamespaces.Drawing + "\"><c:chart><c:title><c:tx><c:rich><a:p><a:r><a:t>Golden Chart</a:t></a:r></a:p></c:rich></c:tx></c:title></c:chart></c:chartSpace>";
    }

    static string PivotCacheXml()
    {
        return "<pivotCacheDefinition xmlns=\"" + XlsxNamespaces.Spreadsheet + "\"><cacheSource type=\"worksheet\"><worksheetSource sheet=\"Sheet1\" ref=\"A1:B10\" /></cacheSource><cacheFields count=\"2\"><cacheField name=\"Label\" /><cacheField name=\"Amount\" /></cacheFields></pivotCacheDefinition>";
    }

    static string PivotTableXml()
    {
        return "<pivotTableDefinition xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" name=\"GoldenPivot\" cacheId=\"1\" dataCaption=\"Values\"><location ref=\"D3:F10\" firstDataCol=\"1\" firstDataRow=\"1\" firstHeaderRow=\"1\" /></pivotTableDefinition>";
    }
}
