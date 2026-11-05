using System.Text;

namespace odf.Tests;

public class ChartAdvancedModelTests
{
    [Test]
    public void LoadsErrorsRegressionStockRangesAndEmbeddedData()
    {
        OdfDocument document = ReadChart();
        OdfChart chart = document.Charts.Single();
        OdfChartPlotArea plot = chart.PlotArea!;
        OdfChartSeries series = plot.Series.Single();
        OdfChartErrorIndicator error = series.ErrorIndicators[0];
        OdfChartRegressionCurve curve = series.RegressionCurves.Single();

        Assert.Multiple(() =>
        {
            Assert.That(chart.DataTable, Is.Not.Null);
            Assert.That(chart.DataTable?.Name, Is.EqualTo("Local"));
            Assert.That(chart.DataTable?.RowCount, Is.EqualTo(4));
            Assert.That(chart.DataTable?.ColumnCount, Is.EqualTo(5));
            Assert.That(chart.DataTable?.Rows[1].Cells[2].Value.NumericValue,
                Is.EqualTo(10));
            Assert.That(chart.Title?.SourceRanges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(plot.SourceRanges.Single().EndColumnIndex,
                Is.EqualTo(4));
            Assert.That(plot.SourceRanges.Single().EndRowIndex,
                Is.EqualTo(3));
            Assert.That(plot.IsSourceRangeValid, Is.True);
            Assert.That(plot.Axes[0].Categories?.Ranges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(series.ValueRanges.Single().StartColumnIndex,
                Is.EqualTo(2));
            Assert.That(series.ValueRanges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(series.LabelRanges.Single().StartRowIndex,
                Is.EqualTo(0));
            Assert.That(series.Domains.Single().Ranges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(series.ErrorIndicators, Has.Length.EqualTo(2));
            Assert.That(error.Dimension, Is.EqualTo("y"));
            Assert.That(error.StyleMetadata.ErrorCategory,
                Is.EqualTo("cell-range"));
            Assert.That(error.StyleMetadata.HasErrorUpperIndicator, Is.True);
            Assert.That(error.StyleMetadata.HasErrorLowerIndicator, Is.False);
            Assert.That(error.StyleMetadata.ErrorLowerRanges.Single().Table,
                Is.SameAs(chart.DataTable));
            Assert.That(error.StyleMetadata.ErrorLowerRanges.Single()
                .StartColumnIndex, Is.EqualTo(3));
            Assert.That(error.StyleMetadata.ErrorUpperRanges.Single()
                .StartColumnIndex, Is.EqualTo(4));
            Assert.That(curve.StyleMetadata.RegressionType,
                Is.EqualTo("polynomial"));
            Assert.That(curve.StyleMetadata.RegressionMaximumDegree,
                Is.EqualTo("3"));
            Assert.That(curve.StyleMetadata.IsRegressionInterceptForced,
                Is.True);
            Assert.That(curve.StyleMetadata.RegressionName,
                Is.EqualTo("Trend"));
            Assert.That(curve.Equation?.Text, Is.EqualTo("y = ax² + bx + c"));
            Assert.That(curve.Equation?.IsAutomaticContent, Is.True);
            Assert.That(curve.Equation?.DisplaysRSquare, Is.True);
            Assert.That(curve.Equation?.DisplaysEquation, Is.True);
            Assert.That(plot.StockGainMarker?.ElementName,
                Is.EqualTo("stock-gain-marker"));
            Assert.That(plot.StockLossMarker?.StyleName,
                Is.EqualTo("stock-loss"));
            Assert.That(plot.StockRangeLine?.StyleName,
                Is.EqualTo("stock-range"));
        });
    }

    [Test]
    public void WritesErrorsRegressionEquationAndStockChanges()
    {
        OdfDocument document = ReadChart();
        OdfChartPlotArea plot = document.Charts.Single().PlotArea!;
        OdfChartSeries series = plot.Series.Single();
        OdfChartErrorIndicator error = series.ErrorIndicators[0];
        OdfChartRegressionCurve curve = series.RegressionCurves.Single();

        error.Dimension = "x";
        error.StyleMetadata.ErrorCategory = "constant";
        error.StyleMetadata.ErrorLowerLimit = "1.5";
        error.StyleMetadata.ErrorUpperLimit = "2.5";
        error.StyleMetadata.HasErrorUpperIndicator = false;
        error.StyleMetadata.HasErrorLowerIndicator = true;
        error.StyleMetadata.ErrorLowerRange = "Local.C2:Local.C4";
        curve.StyleMetadata.RegressionType = "moving-average";
        curve.StyleMetadata.RegressionMaximumDegree = "4";
        curve.StyleMetadata.IsRegressionInterceptForced = false;
        curve.StyleMetadata.RegressionName = "Moving trend";
        curve.StyleMetadata.RegressionPeriod = "2";
        curve.StyleMetadata.RegressionMovingType = "central";
        curve.Equation!.Text = "moving average";
        curve.Equation.IsAutomaticContent = false;
        curve.Equation.DisplaysRSquare = false;
        curve.Equation.DisplaysEquation = true;
        curve.Equation.X = "6cm";
        plot.StockGainMarker!.StyleName = "stock-loss";
        plot.StockRangeLine!.StyleName = "stock-gain";

        byte[] written = Write(document);
        Assert.That(
            document.Charts.Single().PlotArea?.Series.Single()
                .ValueRanges.Single().Table,
            Is.SameAs(document.Charts.Single().DataTable));
        OdfDocument reread = ReadBytes(written);
        OdfChartPlotArea changedPlot = reread.Charts.Single().PlotArea!;
        OdfChartSeries changedSeries = changedPlot.Series.Single();
        OdfChartErrorIndicator changedError =
            changedSeries.ErrorIndicators[0];
        OdfChartRegressionCurve changedCurve =
            changedSeries.RegressionCurves.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changedError.Dimension, Is.EqualTo("x"));
            Assert.That(changedError.StyleMetadata.ErrorCategory,
                Is.EqualTo("constant"));
            Assert.That(changedError.StyleMetadata.ErrorLowerLimit,
                Is.EqualTo("1.5"));
            Assert.That(changedError.StyleMetadata.ErrorUpperLimit,
                Is.EqualTo("2.5"));
            Assert.That(changedError.StyleMetadata.HasErrorUpperIndicator,
                Is.False);
            Assert.That(changedError.StyleMetadata.HasErrorLowerIndicator,
                Is.True);
            Assert.That(changedError.StyleMetadata.ErrorLowerRange,
                Is.EqualTo("Local.C2:Local.C4"));
            Assert.That(changedError.StyleMetadata.ErrorLowerRanges.Single()
                .Table, Is.SameAs(reread.Charts.Single().DataTable));
            Assert.That(changedCurve.StyleMetadata.RegressionType,
                Is.EqualTo("moving-average"));
            Assert.That(changedCurve.StyleMetadata.RegressionMaximumDegree,
                Is.EqualTo("4"));
            Assert.That(changedCurve.StyleMetadata.IsRegressionInterceptForced,
                Is.False);
            Assert.That(changedCurve.StyleMetadata.RegressionName,
                Is.EqualTo("Moving trend"));
            Assert.That(changedCurve.StyleMetadata.RegressionPeriod,
                Is.EqualTo("2"));
            Assert.That(changedCurve.StyleMetadata.RegressionMovingType,
                Is.EqualTo("central"));
            Assert.That(changedCurve.Equation?.Text,
                Is.EqualTo("moving average"));
            Assert.That(changedCurve.Equation?.IsAutomaticContent, Is.False);
            Assert.That(changedCurve.Equation?.DisplaysRSquare, Is.False);
            Assert.That(changedCurve.Equation?.DisplaysEquation, Is.True);
            Assert.That(changedCurve.Equation?.X, Is.EqualTo("6cm"));
            Assert.That(changedPlot.StockGainMarker?.StyleName,
                Is.EqualTo("stock-loss"));
            Assert.That(changedPlot.StockRangeLine?.StyleName,
                Is.EqualTo("stock-gain"));
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
            "advanced.fodc",
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
        return RootStart()
            + "<office:automatic-styles>"
            + ChartStyle("error-style",
                " chart:error-category='cell-range' "
                + "chart:error-percentage='5' chart:error-margin='1' "
                + "chart:error-lower-limit='0.5' "
                + "chart:error-upper-limit='1.5' "
                + "chart:error-upper-indicator='true' "
                + "chart:error-lower-indicator='false' "
                + "chart:error-lower-range='Local.D2:Local.D4' "
                + "chart:error-upper-range='Local.E2:Local.E4'")
            + ChartStyle("regression-style",
                " chart:regression-type='polynomial' "
                + "chart:regression-max-degree='3' "
                + "chart:regression-force-intercept='true' "
                + "chart:regression-intercept-value='0' "
                + "chart:regression-name='Trend' "
                + "chart:regression-period='3' "
                + "chart:regression-moving-type='prior'")
            + ChartStyle("stock-gain", " chart:lines='true'")
            + ChartStyle("stock-loss", " chart:lines='false'")
            + ChartStyle("stock-range", " chart:lines='true'")
            + "</office:automatic-styles>"
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:stock'>"
            + "<chart:title table:cell-range='Local.A1'>"
            + "<text:p>Stock</text:p></chart:title>"
            + "<chart:plot-area table:cell-range-address='Local.A1:Local.E4' "
            + "chart:data-source-has-labels='both'>"
            + "<chart:axis chart:dimension='x'>"
            + "<chart:categories table:cell-range-address='Local.A2:Local.A4'/>"
            + "</chart:axis>"
            + "<chart:series chart:class='chart:stock' "
            + "chart:values-cell-range-address='Local.C2:Local.C4' "
            + "chart:label-cell-address='Local.C1'>"
            + "<chart:domain table:cell-range-address='Local.B2:Local.B4'/>"
            + "<chart:error-indicator chart:dimension='y' "
            + "chart:style-name='error-style'/>"
            + "<chart:error-indicator chart:dimension='x' "
            + "chart:style-name='error-style'/>"
            + "<chart:regression-curve chart:style-name='regression-style'>"
            + "<chart:equation chart:automatic-content='true' "
            + "chart:display-r-square='true' chart:display-equation='true' "
            + "svg:x='5cm' svg:y='2cm'>"
            + "<text:p>y = ax² + bx + c</text:p></chart:equation>"
            + "</chart:regression-curve></chart:series>"
            + "<chart:stock-gain-marker chart:style-name='stock-gain'/>"
            + "<chart:stock-loss-marker chart:style-name='stock-loss'/>"
            + "<chart:stock-range-line chart:style-name='stock-range'/>"
            + "</chart:plot-area>"
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

    static string ChartStyle(string name, string properties)
    {
        return "<style:style style:name='" + name
            + "' style:family='chart'><style:chart-properties"
            + properties + "/></style:style>";
    }

    static string DataRow(
        string first,
        string second,
        string third,
        string fourth,
        string fifth)
    {
        return "<table:table-row>"
            + DataCell(first) + DataCell(second) + DataCell(third)
            + DataCell(fourth) + DataCell(fifth)
            + "</table:table-row>";
    }

    static string DataCell(string value)
    {
        return "<table:table-cell office:value-type='float' office:value='"
            + value + "'><text:p>" + value
            + "</text:p></table:table-cell>";
    }
}
