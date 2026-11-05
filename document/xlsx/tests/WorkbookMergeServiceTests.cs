using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookMergeServiceTests
{
    [Test]
    public void CopyWorksheet_RenamesConflictsAndRemapsCompleteDependencyGraph()
    {
        XlsxWorkbook destination = OpenWorkbook(CreatePackage(new[] { "Data" }, "Destination", true));
        XlsxWorkbook source = OpenWorkbook(CreatePackage(new[] { "Data" }, "Source", true));
        IXlsxWorkbookMergeService service = new XlsxWorkbookMergeService();

        XlsxWorkbookMergeResult result = service.CopyWorksheet(destination, source, "Data", new XlsxWorkbookMergeOptions());
        XlsxWorkbookMergeMapping sheetMapping = result.SheetNameMappings[0];
        XlsxWorksheetPart copied = destination.Worksheets[1];
        XlsxWorkbookSaveResult save = destination.SaveBytes();
        XlsxWorkbook reopened = OpenWorkbook(save.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CopiedSheetCount, Is.EqualTo(1));
            Assert.That(sheetMapping.Source, Is.EqualTo("Data"));
            Assert.That(sheetMapping.Destination, Is.EqualTo("Data (2)"));
            Assert.That(result.StyleMappings[0].Destination, Is.EqualTo("1"));
            Assert.That(result.SharedStringMappings[0].Destination, Is.EqualTo("1"));
            Assert.That(result.TableMappings[0].Source, Is.EqualTo("Sales"));
            Assert.That(result.TableMappings[0].Destination, Is.EqualTo("Sales_2"));
            Assert.That(result.PivotMappings.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.DrawingMappings.Count, Is.EqualTo(1));
            Assert.That(result.ChartMappings.Count, Is.EqualTo(1));
            Assert.That(result.CommentMappings.Count, Is.EqualTo(1));
            Assert.That(result.RelationshipMappings.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(copied.GetCell("A1")!.Style, Is.EqualTo(1));
            Assert.That(copied.GetCell("A1")!.Value, Is.EqualTo("1"));
            Assert.That(copied.GetCell("B1")!.Formula, Is.EqualTo("'Data (2)'!A1"));
            Assert.That(copied.Tables[0].Name, Is.EqualTo("Sales_2"));
            Assert.That(save.IsValid, Is.True);
            Assert.That(reopened.WorkbookPart.Sheets.Count, Is.EqualTo(2));
            Assert.That(reopened.WorkbookPart.Sheets[1].Name, Is.EqualTo("Data (2)"));
            Assert.That(reopened.Worksheets[1].GetCell("B1")!.Formula, Is.EqualTo("'Data (2)'!A1"));
            Assert.That(package.GetEntry(sheetMapping.DestinationPath), Is.Not.Null);
            Assert.That(package.GetEntry(result.ChartMappings[0].DestinationPath), Is.Not.Null);
            Assert.That(package.GetEntry(result.CommentMappings[0].DestinationPath), Is.Not.Null);
        });
    }

    [Test]
    public void MergeWorkbooks_MapsAllSheetsDefinedNamesAndCrossSheetFormulas()
    {
        XlsxWorkbook destination = OpenWorkbook(CreatePackage(new[] { "Data" }, "Destination", false));
        XlsxWorkbook source = OpenWorkbook(CreatePackage(new[] { "Data", "Summary" }, "Source", false));
        IXlsxWorkbookMergeService service = new XlsxWorkbookMergeService();

        XlsxWorkbookMergeResult result = service.MergeWorkbooks(destination, source, new XlsxWorkbookMergeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CopiedSheetCount, Is.EqualTo(2));
            Assert.That(result.SheetNameMappings[0].Destination, Is.EqualTo("Data (2)"));
            Assert.That(result.SheetNameMappings[1].Destination, Is.EqualTo("Summary"));
            Assert.That(result.DefinedNameMappings[0].Source, Is.EqualTo("SourceValue"));
            Assert.That(result.DefinedNameMappings[0].Destination, Is.EqualTo("SourceValue_2"));
            Assert.That(destination.WorkbookPart.DefinedNames[1].Text, Does.Contain("'Data (2)'!$A$1"));
            Assert.That(destination.Worksheets[2].GetCell("B1")!.Formula, Is.EqualTo("'Data (2)'!A1"));
            Assert.That(result.FormulaMappings.Count, Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void CopyWorksheet_FailPolicyLeavesDestinationUnchanged()
    {
        XlsxWorkbook destination = OpenWorkbook(CreatePackage(new[] { "Data" }, "Destination", false));
        XlsxWorkbook source = OpenWorkbook(CreatePackage(new[] { "Data" }, "Source", false));
        XlsxWorkbookMergeOptions options = new XlsxWorkbookMergeOptions();
        options.SheetNameConflictPolicy = XlsxWorkbookMergeConflictPolicy.Fail;

        XlsxWorkbookMergeResult result = new XlsxWorkbookMergeService().CopyWorksheet(destination, source, "Data", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.CopiedSheetCount, Is.EqualTo(0));
            Assert.That(destination.WorkbookPart.Sheets.Count, Is.EqualTo(1));
            Assert.That(destination.Worksheets.Count, Is.EqualTo(1));
            Assert.That(HasDiagnostic(result, "workbookMerge.sheetNameConflict"), Is.True);
        });
    }

    static XlsxWorkbook OpenWorkbook(byte[] bytes)
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.SelectedRanges.Add("A1:XFD1048576");
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes, options);
        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook, Is.Not.Null);
        return open.Workbook!;
    }

    static byte[] CreatePackage(string[] sheetNames, string marker, bool dependencies)
    {
        using MemoryStream stream = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", ContentTypes(sheetNames.Length, dependencies));
            AddEntry(archive, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            AddEntry(archive, "xl/workbook.xml", WorkbookXml(sheetNames));
            AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(sheetNames.Length));
            AddEntry(archive, "xl/styles.xml", StylesXml());
            AddEntry(archive, "xl/sharedStrings.xml", "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"1\" uniqueCount=\"1\"><si><t>" + marker + "</t></si></sst>");
            int index = 0;
            while (index < sheetNames.Length)
            {
                bool first = index == 0;
                AddEntry(archive, "xl/worksheets/sheet" + (index + 1) + ".xml", WorksheetXml(sheetNames[index], first, dependencies));
                index = index + 1;
            }
            if (dependencies)
            {
                AddEntry(archive, "xl/worksheets/_rels/sheet1.xml.rels", WorksheetRelationships());
                AddEntry(archive, "xl/tables/table1.xml", "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"1\" name=\"Sales\" displayName=\"Sales\" ref=\"A1:B2\"><tableColumns count=\"2\"><tableColumn id=\"1\" name=\"Name\"/><tableColumn id=\"2\" name=\"Value\"/></tableColumns></table>");
                AddEntry(archive, "xl/drawings/drawing1.xml", DrawingXml());
                AddEntry(archive, "xl/drawings/_rels/drawing1.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rIdChart\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart\" Target=\"../charts/chart1.xml\"/></Relationships>");
                AddEntry(archive, "xl/charts/chart1.xml", ChartXml());
                AddEntry(archive, "xl/comments1.xml", "<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><authors><author>Codex</author></authors><commentList><comment ref=\"A1\" authorId=\"0\"><text><t>Note</t></text></comment></commentList></comments>");
                AddEntry(archive, "xl/pivotTables/pivotTable1.xml", "<pivotTableDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" name=\"PivotTable1\" cacheId=\"1\"><location ref=\"D3:F8\" firstHeaderRow=\"1\" firstDataRow=\"1\" firstDataCol=\"1\"/></pivotTableDefinition>");
                AddEntry(archive, "xl/pivotTables/_rels/pivotTable1.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rIdCache\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/pivotCacheDefinition\" Target=\"../pivotCache/pivotCacheDefinition1.xml\"/></Relationships>");
                AddEntry(archive, "xl/pivotCache/pivotCacheDefinition1.xml", "<pivotCacheDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" saveData=\"1\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:B2\" sheet=\"Data\"/></cacheSource><cacheFields count=\"1\"><cacheField name=\"Name\"><sharedItems/></cacheField></cacheFields></pivotCacheDefinition>");
            }
        }
        return stream.ToArray();
    }

    static string ContentTypes(int sheetCount, bool dependencies)
    {
        string xml = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>";
        int index = 1;
        while (index <= sheetCount)
        {
            xml = xml + "<Override PartName=\"/xl/worksheets/sheet" + index + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>";
            index = index + 1;
        }
        if (dependencies)
        {
            xml = xml + "<Override PartName=\"/xl/tables/table1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml\"/><Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawing+xml\"/><Override PartName=\"/xl/charts/chart1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawingml.chart+xml\"/><Override PartName=\"/xl/comments1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml\"/><Override PartName=\"/xl/pivotTables/pivotTable1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.pivotTable+xml\"/><Override PartName=\"/xl/pivotCache/pivotCacheDefinition1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheDefinition+xml\"/>";
        }
        return xml + "</Types>";
    }

    static string WorkbookXml(string[] sheetNames)
    {
        string xml = "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>";
        int index = 0;
        while (index < sheetNames.Length)
        {
            xml = xml + "<sheet name=\"" + sheetNames[index] + "\" sheetId=\"" + (index + 1) + "\" r:id=\"rId" + (index + 1) + "\"/>";
            index = index + 1;
        }
        return xml + "</sheets><definedNames><definedName name=\"SourceValue\">'Data'!$A$1</definedName></definedNames></workbook>";
    }

    static string WorkbookRelationships(int sheetCount)
    {
        string xml = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">";
        int index = 1;
        while (index <= sheetCount)
        {
            xml = xml + "<Relationship Id=\"rId" + index + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet" + index + ".xml\"/>";
            index = index + 1;
        }
        return xml + "<Relationship Id=\"rIdStyles\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/><Relationship Id=\"rIdStrings\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/></Relationships>";
    }

    static string WorksheetXml(string sheetName, bool first, bool dependencies)
    {
        string formula = first ? sheetName + "!A1" : "Data!A1";
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetData><row r=\"1\"><c r=\"A1\" s=\"0\" t=\"s\"><v>0</v></c><c r=\"B1\"><f>" + formula + "</f><v>1</v></c></row></sheetData>";
        if (first && dependencies) { xml = xml + "<drawing r:id=\"rIdDrawing\"/><tableParts count=\"1\"><tablePart r:id=\"rIdTable\"/></tableParts>"; }
        return xml + "</worksheet>";
    }

    static string WorksheetRelationships()
    {
        return "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rIdTable\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/table\" Target=\"../tables/table1.xml\"/><Relationship Id=\"rIdDrawing\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing1.xml\"/><Relationship Id=\"rIdComments\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments\" Target=\"../comments1.xml\"/><Relationship Id=\"rIdPivot\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/pivotTable\" Target=\"../pivotTables/pivotTable1.xml\"/></Relationships>";
    }

    static string StylesXml()
    {
        return "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"1\"><font><name val=\"Calibri\"/></font></fonts><fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs></styleSheet>";
    }

    static string DrawingXml()
    {
        return "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><xdr:twoCellAnchor><xdr:from><xdr:col>0</xdr:col><xdr:row>0</xdr:row></xdr:from><xdr:to><xdr:col>3</xdr:col><xdr:row>8</xdr:row></xdr:to><xdr:graphicFrame><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart\"/></a:graphicData></a:graphic></xdr:graphicFrame><xdr:clientData/></xdr:twoCellAnchor></xdr:wsDr>";
    }

    static string ChartXml()
    {
        return "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart><c:plotArea><c:lineChart><c:ser><c:idx val=\"0\"/><c:order val=\"0\"/><c:val><c:numRef><c:f>Data!$A$1:$A$2</c:f></c:numRef></c:val></c:ser></c:lineChart></c:plotArea></c:chart></c:chartSpace>";
    }

    static void AddEntry(ZipArchive archive, string path, string text)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path);
        using Stream stream = entry.Open();
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    static bool HasDiagnostic(XlsxWorkbookMergeResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics) { if (diagnostic.Code == code) { return true; } }
        return false;
    }
}
