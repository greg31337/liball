using System.Text;

namespace odf.Tests;

public class OdfChartEditorTests
{
    [TestCase(OdfChartKind.Bar, "chart:bar")]
    [TestCase(OdfChartKind.Line, "chart:line")]
    [TestCase(OdfChartKind.Area, "chart:area")]
    [TestCase(OdfChartKind.Pie, "chart:circle")]
    [TestCase(OdfChartKind.Ring, "chart:ring")]
    [TestCase(OdfChartKind.Scatter, "chart:scatter")]
    [TestCase(OdfChartKind.Radar, "chart:radar")]
    [TestCase(OdfChartKind.FilledRadar, "chart:filled-radar")]
    [TestCase(OdfChartKind.Stock, "chart:stock")]
    [TestCase(OdfChartKind.Bubble, "chart:bubble")]
    [TestCase(OdfChartKind.Surface, "chart:surface")]
    [TestCase(OdfChartKind.Gantt, "chart:gantt")]
    public void CreatesEveryStandardChartClassThroughContract(
        OdfChartKind kind,
        string className)
    {
        OdfDocument document = Read(EmptyChartXml());
        IOdfChartEditor editor = new OdfChartEditor();

        OdfChart chart = editor.CreateChart(document, kind);

        Assert.Multiple(() =>
        {
            Assert.That(chart.Kind, Is.EqualTo(kind));
            Assert.That(chart.ClassName, Is.EqualTo(className));
            Assert.That(chart.PlotArea, Is.Not.Null);
            Assert.That(editor.GetCharts(document).Single(), Is.SameAs(chart));
        });
    }

    [Test]
    public void CreatesAndAppliesChartChanges()
    {
        OdfDocument document = Read(EmptyChartXml());
        IOdfChartEditor editor = new OdfChartEditor();

        OdfChart chart = editor.CreateChart(document, OdfChartKind.Line);
        chart.Kind = OdfChartKind.Area;
        chart.Width = "12cm";
        chart.Height = "7cm";
        chart.PlotArea!.X = "1cm";
        editor.ApplyChartChanges(document);

        OdfChart changed = editor.GetCharts(document).Single();
        OdfChart reread = Read(Write(document)).Charts.Single();
        OdfDocument factoryDocument = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Chart);
        OdfChart initialized = editor.CreateChart(
            factoryDocument,
            OdfChartKind.Scatter);
        Assert.Multiple(() =>
        {
            Assert.That(changed.Kind, Is.EqualTo(OdfChartKind.Area));
            Assert.That(changed.ClassName, Is.EqualTo("chart:area"));
            Assert.That(changed.Width, Is.EqualTo("12cm"));
            Assert.That(changed.PlotArea?.X, Is.EqualTo("1cm"));
            Assert.That(reread.Kind, Is.EqualTo(OdfChartKind.Area));
            Assert.That(reread.Height, Is.EqualTo("7cm"));
            Assert.That(editor.GetCharts(document),
                Is.Not.SameAs(document.Charts));
            Assert.That(initialized.Kind, Is.EqualTo(OdfChartKind.Scatter));
            Assert.That(factoryDocument.Charts, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void BindsColumnsAsSeriesWithLabelsAndCategories()
    {
        OdfDocument document = Read(BindableChartXml());
        IOdfChartEditor editor = new OdfChartEditor();

        OdfChart chart = editor.BindChartData(
            document,
            document.Charts.Single(),
            document.Tables.Single(),
            0,
            0,
            3,
            3,
            true,
            true,
            true);

        OdfChartPlotArea plot = chart.PlotArea!;
        OdfChartSeries first = plot.Series[0];
        OdfChartSeries second = plot.Series[1];
        Assert.Multiple(() =>
        {
            Assert.That(plot.CellRangeAddress,
                Is.EqualTo("Data.A1:Data.C3"));
            Assert.That(plot.DataSourceHasLabels, Is.EqualTo("both"));
            Assert.That(plot.Axes.Single().Categories?.CellRangeAddress,
                Is.EqualTo("Data.A2:Data.A3"));
            Assert.That(plot.Series, Has.Length.EqualTo(2));
            Assert.That(first.ValuesCellRangeAddress,
                Is.EqualTo("Data.B2:Data.B3"));
            Assert.That(first.LabelCellAddress, Is.EqualTo("Data.B1"));
            Assert.That(second.ValuesCellRangeAddress,
                Is.EqualTo("Data.C2:Data.C3"));
            Assert.That(first.ValueRanges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(editor.ValidateChart(document, chart), Is.Empty);
        });

        chart.Kind = OdfChartKind.Bar;
        first.Kind = OdfChartKind.Bar;
        chart.Title!.Text = "Updated sales";
        editor.ApplyChartChanges(document);
        OdfChart reread = Read(Write(document)).Charts.Single();
        Assert.Multiple(() =>
        {
            Assert.That(reread.Kind, Is.EqualTo(OdfChartKind.Bar));
            Assert.That(reread.Title?.Text, Is.EqualTo("Updated sales"));
            Assert.That(reread.PlotArea?.Series[0].Kind,
                Is.EqualTo(OdfChartKind.Bar));
            Assert.That(reread.PlotArea?.Series[1].ValuesCellRangeAddress,
                Is.EqualTo("Data.C2:Data.C3"));
        });
    }

    [Test]
    public void ReportsInvalidChartReferencesAndRejectsInvalidBindings()
    {
        OdfDocument document = Read(BindableChartXml());
        IOdfChartEditor editor = new OdfChartEditor();
        OdfChart chart = editor.BindChartData(
            document,
            document.Charts.Single(),
            document.Tables.Single(),
            0,
            0,
            3,
            3,
            false,
            true,
            true);
        Assert.Multiple(() =>
        {
            Assert.That(chart.PlotArea?.Axes.Single()
                .Categories?.CellRangeAddress,
                Is.EqualTo("Data.B1:Data.C1"));
            Assert.That(chart.PlotArea?.Series[0].ValuesCellRangeAddress,
                Is.EqualTo("Data.B2:Data.C2"));
            Assert.That(chart.PlotArea?.Series[0].LabelCellAddress,
                Is.EqualTo("Data.A2"));
        });
        chart.StyleName = "MissingStyle";
        chart.PlotArea!.Series[0].ValuesCellRangeAddress = "not-a-range";
        chart.PlotArea.Series[1].ValuesCellRangeAddress = "Unknown.A1";

        OdfDiagnostic[] diagnostics = editor.ValidateChart(document, chart);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain(OdfDiagnosticCodes.InvalidStyleReference));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain(OdfDiagnosticCodes.InvalidRangeReference));
            Assert.That(diagnostics.Count(item =>
                item.Code == OdfDiagnosticCodes.InvalidRangeReference),
                Is.EqualTo(2));
            Assert.That(() => editor.BindChartData(
                    document,
                    chart,
                    document.Tables.Single(),
                    2,
                    2,
                    2,
                    2,
                    true,
                    false,
                    false),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void EditsAdvancedSeriesAndResolvesEverySourceRange()
    {
        OdfDocument document = Read(AdvancedChartXml());
        IOdfChartEditor editor = new OdfChartEditor();
        OdfChart chart = editor.GetCharts(document).Single();
        OdfChartPlotArea plot = chart.PlotArea!;
        OdfChartAxis axis = plot.Axes.Single();
        OdfChartSeries series = plot.Series.Single();
        OdfChartDataPoint point = series.DataPoints[0];
        OdfChartErrorIndicator error = series.ErrorIndicators.Single();
        OdfChartRegressionCurve regression =
            series.RegressionCurves.Single();

        Assert.Multiple(() =>
        {
            Assert.That(chart.Kind, Is.EqualTo(OdfChartKind.Stock));
            Assert.That(chart.DataTable?.Name, Is.EqualTo("Local"));
            Assert.That(chart.DataTable?.RowCount, Is.EqualTo(4));
            Assert.That(chart.DataTable?.ColumnCount, Is.EqualTo(5));
            Assert.That(chart.Title?.SourceRanges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(plot.SourceRanges.Single().EndColumnIndex,
                Is.EqualTo(4));
            Assert.That(axis.Categories?.Ranges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(series.ValueRanges.Single().StartColumnIndex,
                Is.EqualTo(2));
            Assert.That(series.LabelRanges.Single().StartRowIndex,
                Is.EqualTo(0));
            Assert.That(series.Domains.Single().Ranges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(point.RepeatCount, Is.EqualTo(2));
            Assert.That(point.Label?.Text, Is.EqualTo("Peak"));
            Assert.That(error.StyleMetadata.ErrorCategory,
                Is.EqualTo("cell-range"));
            Assert.That(error.StyleMetadata.ErrorLowerRanges.Single()
                .StartColumnIndex, Is.EqualTo(3));
            Assert.That(error.StyleMetadata.ErrorUpperRanges.Single()
                .StartColumnIndex, Is.EqualTo(4));
            Assert.That(regression.StyleMetadata.RegressionType,
                Is.EqualTo("polynomial"));
            Assert.That(regression.Equation?.Text,
                Is.EqualTo("y = ax² + bx + c"));
            Assert.That(editor.ValidateChart(document, chart), Is.Empty);
        });

        axis.Categories!.CellRangeAddress = "Local.B2:Local.B4";
        point.Label!.Text = "Changed peak";
        error.Dimension = "x";
        regression.StyleMetadata.RegressionName = "Changed trend";
        regression.Equation!.Text = "changed equation";
        editor.ApplyChartChanges(document);
        OdfChart changed = editor.GetCharts(document).Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.PlotArea?.Axes.Single()
                .Categories?.CellRangeAddress,
                Is.EqualTo("Local.B2:Local.B4"));
            Assert.That(changed.PlotArea?.Series.Single()
                .DataPoints[0].Label?.Text,
                Is.EqualTo("Changed peak"));
            Assert.That(changed.PlotArea?.Series.Single()
                .ErrorIndicators.Single().Dimension,
                Is.EqualTo("x"));
            Assert.That(changed.PlotArea?.Series.Single()
                .RegressionCurves.Single().StyleMetadata.RegressionName,
                Is.EqualTo("Changed trend"));
            Assert.That(changed.PlotArea?.Series.Single()
                .RegressionCurves.Single().Equation?.Text,
                Is.EqualTo("changed equation"));
            Assert.That(editor.ValidateChart(document, changed), Is.Empty);
        });
    }

    static OdfDocument Read(string xml)
    {
        return Read(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument Read(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "editor.fodc",
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

    static string EmptyChartXml()
    {
        return RootStart()
            + "<office:body><office:chart/>"
            + "</office:body></office:document>";
    }

    static string BindableChartXml()
    {
        return RootStart()
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:line'>"
            + "<chart:title><text:p>Sales</text:p></chart:title>"
            + "<chart:plot-area/>"
            + "<table:table table:name='Data'>"
            + Row("Category", "Sales", "Cost")
            + Row("Jan", "10", "7")
            + Row("Feb", "12", "8")
            + "</table:table></chart:chart>"
            + "</office:chart></office:body></office:document>";
    }

    static string AdvancedChartXml()
    {
        return RootStart()
            + "<office:automatic-styles>"
            + ChartStyle(
                "error-style",
                " chart:error-category='cell-range' "
                    + "chart:error-upper-indicator='true' "
                    + "chart:error-lower-indicator='true' "
                    + "chart:error-lower-range='Local.D2:Local.D4' "
                    + "chart:error-upper-range='Local.E2:Local.E4'")
            + ChartStyle(
                "regression-style",
                " chart:regression-type='polynomial' "
                    + "chart:regression-max-degree='3' "
                    + "chart:regression-force-intercept='true' "
                    + "chart:regression-name='Trend'")
            + "</office:automatic-styles>"
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:stock'>"
            + "<chart:title table:cell-range='Local.A1'>"
            + "<text:p>Stock</text:p></chart:title>"
            + "<chart:plot-area table:cell-range-address='Local.A1:Local.E4' "
            + "chart:data-source-has-labels='both'>"
            + "<chart:axis chart:dimension='x' chart:name='primary-x'>"
            + "<chart:categories "
            + "table:cell-range-address='Local.A2:Local.A4'/>"
            + "</chart:axis>"
            + "<chart:series chart:class='chart:stock' "
            + "chart:values-cell-range-address='Local.C2:Local.C4' "
            + "chart:label-cell-address='Local.C1'>"
            + "<chart:domain "
            + "table:cell-range-address='Local.B2:Local.B4'/>"
            + "<chart:data-point chart:repeated='2'>"
            + "<chart:data-label><text:p>Peak</text:p>"
            + "</chart:data-label></chart:data-point>"
            + "<chart:data-point/>"
            + "<chart:error-indicator chart:dimension='y' "
            + "chart:style-name='error-style'/>"
            + "<chart:regression-curve "
            + "chart:style-name='regression-style'>"
            + "<chart:equation chart:automatic-content='true' "
            + "chart:display-r-square='true' "
            + "chart:display-equation='true'>"
            + "<text:p>y = ax² + bx + c</text:p>"
            + "</chart:equation></chart:regression-curve>"
            + "</chart:series></chart:plot-area>"
            + "<table:table table:name='Local'>"
            + DataRow("Date", "Open", "Close", "Low", "High")
            + DataRow("1", "8", "10", "7", "11")
            + DataRow("2", "10", "12", "9", "13")
            + DataRow("3", "12", "11", "10", "14")
            + "</table:table></chart:chart>"
            + "</office:chart></office:body></office:document>";
    }

    static string RootStart()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Chart + "'>";
    }

    static string Row(string first, string second, string third)
    {
        return "<table:table-row>" + Cell(first) + Cell(second)
            + Cell(third) + "</table:table-row>";
    }

    static string DataRow(
        string first,
        string second,
        string third,
        string fourth,
        string fifth)
    {
        return "<table:table-row>"
            + NumericCell(first)
            + NumericCell(second)
            + NumericCell(third)
            + NumericCell(fourth)
            + NumericCell(fifth)
            + "</table:table-row>";
    }

    static string NumericCell(string value)
    {
        return "<table:table-cell office:value-type='float' office:value='"
            + value + "'><text:p>" + value
            + "</text:p></table:table-cell>";
    }

    static string ChartStyle(string name, string properties)
    {
        return "<style:style style:name='" + name
            + "' style:family='chart'><style:chart-properties"
            + properties + "/></style:style>";
    }

    static string Cell(string value)
    {
        return "<table:table-cell office:value-type='string' "
            + "office:string-value='" + value + "'><text:p>" + value
            + "</text:p></table:table-cell>";
    }
}
