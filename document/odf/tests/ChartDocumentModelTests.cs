using System.Text;

namespace odf.Tests;

public class ChartDocumentModelTests
{
    [Test]
    public void LoadsChartLayoutAxesSurfacesCategoriesAndDataTable()
    {
        OdfDocument document = ReadChart();
        OdfChart chart = document.Charts.Single();
        OdfChartPlotArea plot = chart.PlotArea!;
        OdfChartAxis xAxis = plot.Axes[0];
        OdfChartAxis yAxis = plot.Axes[1];

        Assert.Multiple(() =>
        {
            Assert.That(chart.Title?.Text, Is.EqualTo("Quarterly Sales"));
            Assert.That(chart.Title?.CellRange, Is.EqualTo("Local.A1"));
            Assert.That(chart.Subtitle?.Text, Is.EqualTo("FY 2026"));
            Assert.That(chart.Footer?.Text, Is.EqualTo("Internal"));
            Assert.That(chart.Legend?.Position, Is.EqualTo("end"));
            Assert.That(chart.Legend?.Alignment, Is.EqualTo("center"));
            Assert.That(chart.Legend?.Expansion, Is.EqualTo("wide"));
            Assert.That(chart.Legend?.Text, Is.EqualTo("Legend"));
            Assert.That(plot.CellRangeAddress, Is.EqualTo("Local.A1:B3"));
            Assert.That(plot.DataSourceHasLabels, Is.EqualTo("both"));
            Assert.That(plot.Axes, Has.Length.EqualTo(2));
            Assert.That(xAxis.Dimension, Is.EqualTo("x"));
            Assert.That(xAxis.Title?.Text, Is.EqualTo("Quarter"));
            Assert.That(xAxis.Categories?.CellRangeAddress,
                Is.EqualTo("Local.A2:A3"));
            Assert.That(xAxis.Grids.Select(item => item.ClassName),
                Is.EqualTo(new[] { "major", "minor" }));
            Assert.That(yAxis.Dimension, Is.EqualTo("y"));
            Assert.That(plot.Wall?.Width, Is.EqualTo("0.2cm"));
            Assert.That(plot.Floor?.StyleName, Is.EqualTo("floor-style"));
            Assert.That(chart.DataTable, Is.Not.Null);
            Assert.That(chart.DataTable?.Name, Is.EqualTo("Local"));
            Assert.That(chart.DataTable?.RowCount, Is.EqualTo(3));
            Assert.That(chart.DataTable?.ColumnCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void WritesChartLayoutAxisGridSurfaceAndCategoryChanges()
    {
        OdfDocument document = ReadChart();
        OdfChart chart = document.Charts.Single();
        OdfChartPlotArea plot = chart.PlotArea!;
        OdfChartAxis axis = plot.Axes[0];

        chart.Title!.Text = "Updated Sales";
        chart.Subtitle!.StyleName = "new-subtitle-style";
        chart.Footer!.CellRange = "Local.B1";
        chart.Legend!.Position = "bottom";
        chart.Legend.Alignment = "start";
        chart.Legend.Expansion = "balanced";
        chart.Legend.Text = "Updated Legend";
        plot.Width = "15cm";
        plot.DataSourceHasLabels = "column";
        axis.Name = "categories";
        axis.Title!.Text = "Period";
        axis.Categories!.CellRangeAddress = "Local.B2:B3";
        axis.Grids[0].ClassName = "minor";
        axis.Grids[0].StyleName = "changed-grid";
        plot.Wall!.Width = "0.4cm";
        plot.Floor!.StyleName = "changed-floor";

        OdfDocument reread = ReadBytes(Write(document));
        OdfChart changed = reread.Charts.Single();
        OdfChartPlotArea changedPlot = changed.PlotArea!;
        OdfChartAxis changedAxis = changedPlot.Axes[0];

        Assert.Multiple(() =>
        {
            Assert.That(changed.Title?.Text, Is.EqualTo("Updated Sales"));
            Assert.That(changed.Subtitle?.StyleName,
                Is.EqualTo("new-subtitle-style"));
            Assert.That(changed.Footer?.CellRange, Is.EqualTo("Local.B1"));
            Assert.That(changed.Legend?.Position, Is.EqualTo("bottom"));
            Assert.That(changed.Legend?.Alignment, Is.EqualTo("start"));
            Assert.That(changed.Legend?.Expansion, Is.EqualTo("balanced"));
            Assert.That(changed.Legend?.Text, Is.EqualTo("Updated Legend"));
            Assert.That(changedPlot.Width, Is.EqualTo("15cm"));
            Assert.That(changedPlot.DataSourceHasLabels,
                Is.EqualTo("column"));
            Assert.That(changedAxis.Name, Is.EqualTo("categories"));
            Assert.That(changedAxis.Title?.Text, Is.EqualTo("Period"));
            Assert.That(changedAxis.Categories?.CellRangeAddress,
                Is.EqualTo("Local.B2:B3"));
            Assert.That(changedAxis.Grids[0].ClassName,
                Is.EqualTo("minor"));
            Assert.That(changedAxis.Grids[0].StyleName,
                Is.EqualTo("changed-grid"));
            Assert.That(changedPlot.Wall?.Width, Is.EqualTo("0.4cm"));
            Assert.That(changedPlot.Floor?.StyleName,
                Is.EqualTo("changed-floor"));
            Assert.That(changed.DataTable?.Name, Is.EqualTo("Local"));
        });
    }

    static OdfDocument ReadChart()
    {
        return ReadBytes(Encoding.UTF8.GetBytes(ChartXml()));
    }

    static OdfDocument ReadBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "structure.fodc",
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
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Chart + "'>"
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:bar'>"
            + "<chart:title table:cell-range='Local.A1' svg:x='1cm' "
            + "chart:style-name='title-style'><text:p>Quarterly Sales</text:p>"
            + "</chart:title>"
            + "<chart:subtitle svg:y='2cm'><text:p>FY 2026</text:p>"
            + "</chart:subtitle>"
            + "<chart:footer><text:p>Internal</text:p></chart:footer>"
            + "<chart:legend chart:legend-position='end' "
            + "chart:legend-align='center' style:legend-expansion='wide' "
            + "svg:x='12cm'><text:p>Legend</text:p></chart:legend>"
            + "<chart:plot-area svg:x='1cm' svg:y='2cm' svg:width='12cm' "
            + "svg:height='8cm' chart:style-name='plot-style' "
            + "table:cell-range-address='Local.A1:B3' "
            + "chart:data-source-has-labels='both'>"
            + "<chart:axis chart:dimension='x' chart:name='primary-x' "
            + "chart:style-name='axis-style'>"
            + "<chart:title><text:p>Quarter</text:p></chart:title>"
            + "<chart:categories table:cell-range-address='Local.A2:A3'/>"
            + "<chart:grid chart:class='major' chart:style-name='major-grid'/>"
            + "<chart:grid chart:class='minor'/></chart:axis>"
            + "<chart:axis chart:dimension='y' chart:name='primary-y'/>"
            + "<chart:wall svg:width='0.2cm' chart:style-name='wall-style'/>"
            + "<chart:floor svg:width='0.1cm' chart:style-name='floor-style'/>"
            + "</chart:plot-area>"
            + "<table:table table:name='Local'>"
            + DataRow("Quarter", "Sales")
            + DataRow("Q1", "10")
            + DataRow("Q2", "20")
            + "</table:table></chart:chart>"
            + "</office:chart></office:body></office:document>";
    }

    static string DataRow(string first, string second)
    {
        return "<table:table-row><table:table-cell office:value-type='string'>"
            + "<text:p>" + first + "</text:p></table:table-cell>"
            + "<table:table-cell office:value-type='string'><text:p>"
            + second + "</text:p></table:table-cell></table:table-row>";
    }
}
