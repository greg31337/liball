using System.Text;

namespace odf.Tests;

public class ChartReferenceUpdateTests
{
    [Test]
    public void SpreadsheetEditsUpdateChartRangesAndEmbeddedFormulas()
    {
        OdfDocument document = ReadChart(ChartXml());
        OdfTable table = document.Charts.Single().DataTable!;

        TableStructureEditor.InsertRows(document, table, 1, 1);

        OdfChart chart = document.Charts.Single();
        OdfChartPlotArea plot = chart.PlotArea!;
        OdfChartSeries series = plot.Series.Single();
        OdfChartStyleMetadata error =
            series.ErrorIndicators.Single().StyleMetadata;
        OdfCell formulaCell = TableDocumentModel.GetCell(
            chart.DataTable!,
            3,
            2)!;

        Assert.Multiple(() =>
        {
            Assert.That(chart.Title?.CellRange, Is.EqualTo("Sheet1.A1"));
            Assert.That(plot.CellRangeAddress,
                Is.EqualTo("Sheet1.A1:Sheet1.C4"));
            Assert.That(plot.Axes[0].Categories?.CellRangeAddress,
                Is.EqualTo("Sheet1.A3:Sheet1.A4"));
            Assert.That(series.ValuesCellRangeAddress,
                Is.EqualTo("Sheet1.B3:Sheet1.B4"));
            Assert.That(series.LabelCellAddress,
                Is.EqualTo("Sheet1.B1"));
            Assert.That(series.Domains[0].CellRangeAddress,
                Is.EqualTo("Sheet1.C3:Sheet1.C4"));
            Assert.That(error.ErrorLowerRange,
                Is.EqualTo("Sheet1.C3:Sheet1.C4"));
            Assert.That(error.ErrorUpperRange,
                Is.EqualTo("Sheet1.B3:Sheet1.B4"));
            Assert.That(error.ErrorLowerRanges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(formulaCell.Formula,
                Is.EqualTo("of:=[.A4]+[.B4]"));
        });

        OdfDocument reread = ReadChart(Write(document));
        Assert.That(reread.Charts.Single().PlotArea?.Series.Single()
            .ErrorIndicators.Single().StyleMetadata.ErrorLowerRange,
            Is.EqualTo("Sheet1.C3:Sheet1.C4"));
        Assert.That(TableDocumentModel.GetCell(
            reread.Charts.Single().DataTable!,
            3,
            2)?.Formula, Is.EqualTo("of:=[.A4]+[.B4]"));
    }

    [Test]
    public void ChartStyleRenameUpdatesTypedAndSerializedReferences()
    {
        OdfDocument document = ReadChart(ChartXml());
        OdfStyle style = document.Styles.Single(item =>
            item.Name == "ChartStyle");

        StyleMaintenanceModel.RenameStyle(
            document,
            style,
            "RenamedChartStyle");

        OdfChart chart = document.Charts.Single();
        OdfChartSeries series = chart.PlotArea!.Series.Single();
        Assert.Multiple(() =>
        {
            Assert.That(chart.StyleName, Is.EqualTo("RenamedChartStyle"));
            Assert.That(series.StyleName, Is.EqualTo("RenamedChartStyle"));
            Assert.That(series.ErrorIndicators.Single().StyleName,
                Is.EqualTo("RenamedChartStyle"));
            Assert.That(series.StyleMetadata.Style, Is.SameAs(style));
        });

        OdfChart reread = ReadChart(Write(document)).Charts.Single();
        Assert.Multiple(() =>
        {
            Assert.That(reread.StyleName, Is.EqualTo("RenamedChartStyle"));
            Assert.That(reread.PlotArea?.Series.Single().StyleName,
                Is.EqualTo("RenamedChartStyle"));
            Assert.That(reread.PlotArea?.Series.Single()
                .ErrorIndicators.Single().StyleName,
                Is.EqualTo("RenamedChartStyle"));
        });
    }

    [Test]
    public void DrawingImportCopiesAndRemapsInlineChartStyles()
    {
        OdfDocument source = ReadDrawing(DrawingXml(true));
        OdfDocument target = ReadDrawing(DrawingXml(false));
        IOdfDrawingEditor editor = new OdfDrawingEditor();
        OdfDrawingObject sourceFrame = editor.FindDrawingObject(
            source,
            "Source Chart")!;
        OdfDrawingObject targetPage = editor.FindDrawingObject(
            target,
            "Target Page")!;

        editor.ImportDrawingObject(
            source,
            target,
            sourceFrame,
            targetPage);

        OdfChart imported = target.Charts.Single();
        OdfStyle copiedStyle = target.Styles.Single(item =>
            item.Name == imported.StyleName
            && item.Family == OdfStyleFamily.Chart);
        string importedStyleName = imported.StyleName;
        Assert.Multiple(() =>
        {
            Assert.That(imported.StyleName, Is.Not.EqualTo("ChartStyle"));
            Assert.That(imported.PlotArea?.Series.Single().StyleName,
                Is.EqualTo(imported.StyleName));
            Assert.That(imported.StyleMetadata.LinesValue,
                Is.EqualTo("true"));
            Assert.That(copiedStyle.ChartProperties.Single(item =>
                item.Name.LocalName == "lines").LexicalValue,
                Is.EqualTo("true"));
        });

        OdfChart reread = ReadDrawing(Write(target)).Charts.Single();
        Assert.That(reread.StyleName, Is.EqualTo(importedStyleName));
        Assert.That(reread.StyleMetadata.LinesValue, Is.EqualTo("true"));
    }

    static OdfDocument ReadChart(string xml)
    {
        return ReadChart(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadChart(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "references.fodc",
            new OdfLoadOptions());
    }

    static OdfDocument ReadDrawing(string xml)
    {
        return ReadDrawing(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadDrawing(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "references.fodg",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string ChartXml()
    {
        return RootStart(OdfMediaTypes.Chart)
            + "<office:automatic-styles>"
            + "<style:style style:name='ChartStyle' style:family='chart'>"
            + "<style:chart-properties chart:lines='true' "
            + "chart:error-category='cell-range' "
            + "chart:error-lower-range='Sheet1.C2:Sheet1.C3' "
            + "chart:error-upper-range='Sheet1.B2:Sheet1.B3'/>"
            + "</style:style></office:automatic-styles>"
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:line' "
            + "chart:style-name='ChartStyle'>"
            + "<chart:title table:cell-range='Sheet1.A1'><text:p>Title</text:p>"
            + "</chart:title>"
            + "<chart:plot-area table:cell-range-address='Sheet1.A1:Sheet1.C3'>"
            + "<chart:axis chart:dimension='x'><chart:categories "
            + "table:cell-range-address='Sheet1.A2:Sheet1.A3'/>"
            + "</chart:axis>"
            + "<chart:series chart:style-name='ChartStyle' "
            + "chart:values-cell-range-address='Sheet1.B2:Sheet1.B3' "
            + "chart:label-cell-address='Sheet1.B1'>"
            + "<chart:domain table:cell-range-address='Sheet1.C2:Sheet1.C3'/>"
            + "<chart:error-indicator chart:dimension='y' "
            + "chart:style-name='ChartStyle'/></chart:series>"
            + "</chart:plot-area>"
            + "<table:table table:name='Sheet1'>"
            + DataRow("1", "2", "3", "")
            + DataRow("4", "5", "6", "")
            + DataRow("7", "8", "9", "of:=[.A3]+[.B3]")
            + "</table:table></chart:chart>"
            + "</office:chart></office:body></office:document>";
    }

    static string DrawingXml(bool source)
    {
        string content = source
            ? "<draw:page draw:name='Source Page'><draw:frame "
                + "draw:name='Source Chart'><draw:object>"
                + "<office:document office:version='1.4' office:mimetype='"
                + OdfMediaTypes.Chart + "'><office:body><office:chart>"
                + "<chart:chart chart:class='chart:line' "
                + "chart:style-name='ChartStyle'><chart:plot-area>"
                + "<chart:series chart:style-name='ChartStyle'/>"
                + "</chart:plot-area></chart:chart>"
                + "</office:chart></office:body></office:document>"
                + "</draw:object></draw:frame></draw:page>"
            : "<draw:page draw:name='Target Page'/>";
        return RootStart(OdfMediaTypes.Graphics)
            + "<office:automatic-styles>"
            + "<style:style style:name='ChartStyle' style:family='chart'>"
            + "<style:chart-properties chart:lines='"
            + (source ? "true" : "false")
            + "'/></style:style></office:automatic-styles>"
            + "<office:body><office:drawing>" + content
            + "</office:drawing></office:body></office:document>";
    }

    static string RootStart(string mediaType)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " office:version='1.4' office:mimetype='" + mediaType + "'>";
    }

    static string DataRow(
        string first,
        string second,
        string third,
        string formula)
    {
        return "<table:table-row>"
            + DataCell(first, "")
            + DataCell(second, "")
            + DataCell(third, formula)
            + "</table:table-row>";
    }

    static string DataCell(string value, string formula)
    {
        string formulaAttribute = formula.Length == 0
            ? ""
            : " table:formula='" + formula + "'";
        return "<table:table-cell office:value-type='float' office:value='"
            + value + "'" + formulaAttribute + "><text:p>" + value
            + "</text:p></table:table-cell>";
    }
}
