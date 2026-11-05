using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ChartPartReaderTests
{
    [Test]
    public void ChartPartReader_ReadsChartTypesTitleSeriesAndPreservesRawXml()
    {
        string chartXml = ChartXml();
        XlsxChartPart part = new XlsxChartPartReader().ReadStringFromPart("/xl/charts/chart1.xml", chartXml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.PartPath, Is.EqualTo("/xl/charts/chart1.xml"));
        Assert.That(part.RawXml, Is.EqualTo(chartXml));
        Assert.That(part.HasChart, Is.True);
        Assert.That(part.HasTitle, Is.True);
        Assert.That(part.TitleText, Is.EqualTo("Sales"));
        Assert.That(part.ChartTypes.Count, Is.EqualTo(1));
        Assert.That(part.ChartTypes[0], Is.EqualTo("barChart"));
        Assert.That(part.Series.Count, Is.EqualTo(1));
        Assert.That(part.Series[0].HasIndex, Is.True);
        Assert.That(part.Series[0].Index, Is.EqualTo(0));
        Assert.That(part.Series[0].TextFormula, Is.EqualTo("Sheet1!$B$1"));
        Assert.That(part.Series[0].CategoryFormula, Is.EqualTo("Sheet1!$A$2:$A$4"));
        Assert.That(part.Series[0].ValuesFormula, Is.EqualTo("Sheet1!$B$2:$B$4"));
    }

    [Test]
    public void DrawingPartReader_ResolvesEmbeddedChartRelationship()
    {
        string drawingXml = DrawingXml();
        XlsxDrawingPart drawing = new XlsxDrawingPartReader().ReadString(drawingXml);
        drawing.RawXml = "";
        XlsxRelationshipsPart relationships = ReadRelationships(
            "/xl/drawings/drawing1.xml",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdChart1", XlsxRelationshipTypes.Chart, "../charts/chart1.xml") +
            "</Relationships>");

        XlsxDrawingChartRelationshipResult resolution = drawing.ResolveChartRelationships(relationships);
        XlsxDrawingPartWriteResult write = new XlsxDrawingPartWriter().WriteString(drawing);
        XlsxDrawingPart readBack = new XlsxDrawingPartReader().ReadString(write.Xml);

        Assert.That(drawing.IsValid, Is.True);
        Assert.That(drawing.ChartAnchors.Count, Is.EqualTo(1));
        Assert.That(drawing.ChartAnchors[0].Name, Is.EqualTo("Chart 1"));
        Assert.That(drawing.ChartAnchors[0].RelationshipId, Is.EqualTo("rIdChart1"));
        Assert.That(resolution.IsValid, Is.True);
        Assert.That(resolution.ChartAnchors.Count, Is.EqualTo(1));
        Assert.That(drawing.ChartAnchors[0].HasRelationship, Is.True);
        Assert.That(drawing.ChartAnchors[0].TargetPath, Is.EqualTo("/xl/charts/chart1.xml"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("r:id=\"rIdChart1\""));
        Assert.That(readBack.ChartAnchors.Count, Is.EqualTo(1));
    }

    [Test]
    public void WorkbookOpen_ReadsChartsheetChartPartsAndPreservesPackage()
    {
        byte[] bytes = CreateChartsheetChartPackage();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.Charts.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.Charts[0].PartPath, Is.EqualTo("/xl/charts/chart1.xml"));
        Assert.That(open.Workbook.Charts[0].ChartTypes[0], Is.EqualTo("barChart"));
        Assert.That(open.Workbook.Charts[0].TitleText, Is.EqualTo("Sales"));
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/charts/chart1.xml"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(savedPackage.IsValid, Is.True);
        AssertEntryText(savedPackage, "/xl/drawings/drawing1.xml", "r:id=\"rIdChart1\"");
        AssertEntryText(savedPackage, "/xl/drawings/_rels/drawing1.xml.rels", "Type=\"" + XlsxRelationshipTypes.Chart + "\"");
        AssertEntryText(savedPackage, "/xl/charts/chart1.xml", "<c:barChart>");
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Chart);
    }

    [Test]
    public void ChartPartReader_ReportsMissingAndInvalidRoots()
    {
        XlsxChartPart missing = new XlsxChartPartReader().ReadString("");
        XlsxChartPart invalid = new XlsxChartPartReader().ReadString("<chartSpace xmlns=\"urn:bad\" />");

        Assert.That(missing.IsValid, Is.False);
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(HasDiagnostic(missing, "chart.missingRoot"), Is.True);
        Assert.That(HasDiagnostic(invalid, "chart.invalidRoot"), Is.True);
    }

    static byte[] CreateChartsheetChartPackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/chartsheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Chartsheet + "\" />" +
            "<Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"" + XlsxContentTypes.Drawing + "\" />" +
            "<Override PartName=\"/xl/charts/chart1.xml\" ContentType=\"" + XlsxContentTypes.Chart + "\" />" +
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
        entries.Add(CreateEntry("/xl/drawings/drawing1.xml", DrawingXml()));
        entries.Add(CreateEntry("/xl/drawings/_rels/drawing1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdChart1", XlsxRelationshipTypes.Chart, "../charts/chart1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/charts/chart1.xml", ChartXml()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string ChartXml()
    {
        return "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">" +
            "<c:chart><c:title><c:tx><c:rich><a:bodyPr /><a:p><a:r><a:t>Sales</a:t></a:r></a:p></c:rich></c:tx></c:title>" +
            "<c:plotArea><c:barChart><c:ser><c:idx val=\"0\" /><c:order val=\"0\" /><c:tx><c:strRef><c:f>Sheet1!$B$1</c:f></c:strRef></c:tx><c:cat><c:strRef><c:f>Sheet1!$A$2:$A$4</c:f></c:strRef></c:cat><c:val><c:numRef><c:f>Sheet1!$B$2:$B$4</c:f></c:numRef></c:val></c:ser></c:barChart></c:plotArea>" +
            "</c:chart></c:chartSpace>";
    }

    static string DrawingXml()
    {
        return "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<xdr:twoCellAnchor>" +
            Marker("from", "0", "0") +
            Marker("to", "10", "5") +
            "<xdr:graphicFrame><xdr:nvGraphicFramePr><xdr:cNvPr id=\"2\" name=\"Chart 1\" /><xdr:cNvGraphicFramePr /></xdr:nvGraphicFramePr><xdr:xfrm><a:off x=\"0\" y=\"0\" /><a:ext cx=\"0\" cy=\"0\" /></xdr:xfrm><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart1\" /></a:graphicData></a:graphic></xdr:graphicFrame><xdr:clientData />" +
            "</xdr:twoCellAnchor></xdr:wsDr>";
    }

    static string Marker(string elementName, string row, string column)
    {
        return "<xdr:" + elementName + "><xdr:col>" + column + "</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>" + row + "</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:" + elementName + ">";
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

    static bool HasDiagnostic(XlsxChartPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
