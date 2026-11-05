using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ChartTypedCompletionTests
{
    [TestCase(XlsxChartKind.Column, "barChart")]
    [TestCase(XlsxChartKind.Bar, "barChart")]
    [TestCase(XlsxChartKind.Line, "lineChart")]
    [TestCase(XlsxChartKind.Area, "areaChart")]
    [TestCase(XlsxChartKind.Pie, "pieChart")]
    [TestCase(XlsxChartKind.Doughnut, "doughnutChart")]
    [TestCase(XlsxChartKind.Scatter, "scatterChart")]
    [TestCase(XlsxChartKind.Bubble, "bubbleChart")]
    [TestCase(XlsxChartKind.Radar, "radarChart")]
    [TestCase(XlsxChartKind.Stock, "stockChart")]
    [TestCase(XlsxChartKind.Surface, "surfaceChart")]
    public void TypedChartOperations_CreateAndRoundTripEveryPlannedChartKind(XlsxChartKind chartKind, string elementName)
    {
        IXlsxChartPartOperations operations = new XlsxChartPartOperations();
        XlsxChartPart chart = operations.CreateChart(chartKind, "Sales");
        string bubbleFormula = chartKind == XlsxChartKind.Bubble ? "Sheet1!$C$2:$C$4" : "";
        operations.AddSeriesForChart(chart, chartKind, "Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4", bubbleFormula);

        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(write.IsValid, Is.True);
            Assert.That(write.Xml, Does.Contain("<c:" + elementName + ">"));
            Assert.That(readBack.IsValid, Is.True);
            Assert.That(readBack.ChartKinds, Does.Contain(chartKind));
            Assert.That(readBack.Series.Count, Is.EqualTo(1));
            Assert.That(readBack.Series[0].ChartKind, Is.EqualTo(chartKind));
            Assert.That(readBack.Series[0].ValuesFormula, Is.EqualTo("Sheet1!$B$2:$B$4"));
        });

        if (chartKind == XlsxChartKind.Bubble)
        {
            Assert.That(write.Xml, Does.Contain("<c:bubbleSize>"));
            Assert.That(readBack.Series[0].BubbleSizeFormula, Is.EqualTo(bubbleFormula));
        }
    }

    [Test]
    public void TypedChartOperations_EditComboPlotsPresentationAxesAndSeriesFeatures()
    {
        IXlsxChartPartOperations operations = new XlsxChartPartOperations();
        XlsxChartPart chart = operations.CreateChart(XlsxChartKind.Column, "Original");
        operations.AddChartKind(chart, XlsxChartKind.Line);
        operations.AddSeriesForChart(chart, XlsxChartKind.Column, "Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4", "");
        operations.AddSeriesForChart(chart, XlsxChartKind.Line, "Sheet1!$C$1", "Sheet1!$A$2:$A$4", "Sheet1!$C$2:$C$4", "");
        operations.SetPresentation(chart, "Revenue", 12, "b", false);
        operations.SetManualLayout(chart, "0.1", "0.2", "0.7", "0.6");
        operations.SetDataTable(chart, true, false, true, true);
        XlsxChartAxis categoryAxis = operations.AddAxis(chart, "cat", "100", "200", "b", "Month");
        XlsxChartAxis valueAxis = operations.AddAxis(chart, "val", "200", "100", "l", "Amount");
        valueAxis.NumberFormat = "$#,##0";
        operations.SetAxisGridlines(chart, 1, true, "CCCCCC", true, "EEEEEE", "0", "100");
        operations.SetSeriesAppearance(chart, 1, "3366FF", "t", true, true, false);
        operations.SetSeriesTrendline(chart, 1, "poly", "Forecast", 3, 2, true, true);
        operations.SetSeriesErrorBars(chart, 1, "y", "both", "fixedVal", "2.5", true);

        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(write.IsValid, Is.True);
            Assert.That(CountOccurrences(write.Xml, "Sheet1!$B$2:$B$4"), Is.EqualTo(1));
            Assert.That(CountOccurrences(write.Xml, "Sheet1!$C$2:$C$4"), Is.EqualTo(1));
            Assert.That(write.Xml, Does.Contain("<c:manualLayout>"));
            Assert.That(write.Xml, Does.Contain("<c:dTable>"));
            Assert.That(write.Xml, Does.Contain("<c:majorGridlines>"));
            Assert.That(write.Xml, Does.Contain("<a:srgbClr val=\"CCCCCC\" />"));
            Assert.That(write.Xml, Does.Contain("<c:showVal val=\"1\" />"));
            Assert.That(write.Xml, Does.Contain("<c:trendlineType val=\"poly\" />"));
            Assert.That(write.Xml, Does.Contain("<c:order val=\"3\" />"));
            Assert.That(write.Xml, Does.Contain("<c:noEndCap val=\"1\" />"));
            Assert.That(readBack.ChartKinds, Is.EquivalentTo(new[] { XlsxChartKind.Column, XlsxChartKind.Line }));
            Assert.That(readBack.Series.Count, Is.EqualTo(2));
            Assert.That(readBack.HasLayout, Is.True);
            Assert.That(readBack.Layout.Width, Is.EqualTo("0.7"));
            Assert.That(readBack.HasDataTable, Is.True);
            Assert.That(readBack.DataTable.ShowLegendKeys, Is.True);
            Assert.That(readBack.TitleText, Is.EqualTo("Revenue"));
            Assert.That(readBack.StyleId, Is.EqualTo(12));
            Assert.That(readBack.Axes.Count, Is.EqualTo(2));
            Assert.That(readBack.Axes[1].HasMajorGridlines, Is.True);
            Assert.That(readBack.Axes[1].MajorGridlineColorRgb, Is.EqualTo("CCCCCC"));
            Assert.That(readBack.Axes[1].Minimum, Is.EqualTo("0"));
            Assert.That(readBack.Axes[1].Maximum, Is.EqualTo("100"));
            Assert.That(readBack.Series[1].ColorRgb, Is.EqualTo("3366FF"));
            Assert.That(readBack.Series[1].ShowLabelValue, Is.True);
            Assert.That(readBack.Series[1].TrendlineOrder, Is.EqualTo(3));
            Assert.That(readBack.Series[1].DisplayTrendlineEquation, Is.True);
            Assert.That(readBack.Series[1].ErrorBarNoEndCap, Is.True);
            Assert.That(categoryAxis.TitleText, Is.EqualTo("Month"));
        });
    }

    [Test]
    public void ChartReaderWriter_ModelsAndPreservesNewerChartExtensions()
    {
        string xml = "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:c16=\"http://schemas.microsoft.com/office/drawing/2014/chart\"><c:chart><c:plotArea><c:layout/><c:lineChart><c:grouping val=\"standard\"/><c:ser><c:idx val=\"0\"/><c:order val=\"0\"/><c:val><c:numRef><c:f>Sheet1!$A$1:$A$2</c:f></c:numRef></c:val></c:ser><c:axId val=\"1\"/><c:axId val=\"2\"/></c:lineChart></c:plotArea></c:chart><c:extLst><c:ext uri=\"{newer}\"><c16:uniqueId val=\"chart-1\"/></c:ext></c:extLst></c:chartSpace>";
        XlsxChartPart chart = new XlsxChartPartReader().ReadString(xml);
        chart.RawXml = "";
        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);

        Assert.Multiple(() =>
        {
            Assert.That(chart.NewerChartElements.Count, Is.EqualTo(1));
            Assert.That(chart.NewerChartElements[0].Kind, Is.EqualTo("newerChartExtension"));
            Assert.That(chart.NewerChartElements[0].RawXml, Does.Contain("c16:uniqueId"));
            Assert.That(write.IsValid, Is.True);
            Assert.That(write.Xml, Does.Contain("c16:uniqueId"));
            Assert.That(CountOccurrences(write.Xml, "{newer}"), Is.EqualTo(1));
        });
    }

    static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while (index >= 0)
        {
            index = text.IndexOf(value, index, System.StringComparison.Ordinal);
            if (index >= 0)
            {
                count = count + 1;
                index = index + value.Length;
            }
        }
        return count;
    }
}
