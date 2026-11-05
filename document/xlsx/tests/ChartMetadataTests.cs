using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ChartMetadataTests
{
    [Test]
    public void ChartPartReaderWriter_RoundTripsSeriesAxesLegendStyleLabelsTrendlinesAndErrorBars()
    {
        XlsxChartPart chart = CreateMetadataChart();
        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<c:style val=\"10\" />"));
        Assert.That(write.Xml, Does.Contain("<c:legend>"));
        Assert.That(write.Xml, Does.Contain("<c:legendPos val=\"b\" />"));
        Assert.That(write.Xml, Does.Contain("<c:catAx>"));
        Assert.That(write.Xml, Does.Contain("<c:valAx>"));
        Assert.That(write.Xml, Does.Contain("<a:srgbClr val=\"FF0000\" />"));
        Assert.That(write.Xml, Does.Contain("<c:dLblPos val=\"bestFit\" />"));
        Assert.That(write.Xml, Does.Contain("<c:trendlineType val=\"linear\" />"));
        Assert.That(write.Xml, Does.Contain("<c:errBarType val=\"both\" />"));
        Assert.That(write.Xml, Does.Contain("<c:val val=\"2\" />"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasStyle, Is.True);
        Assert.That(readBack.StyleId, Is.EqualTo(10));
        Assert.That(readBack.HasTitle, Is.True);
        Assert.That(readBack.TitleText, Is.EqualTo("Sales"));
        Assert.That(readBack.HasLegend, Is.True);
        Assert.That(readBack.Legend.Position, Is.EqualTo("b"));
        Assert.That(readBack.Legend.HasOverlay, Is.True);
        Assert.That(readBack.Legend.Overlay, Is.False);
        Assert.That(readBack.Axes.Count, Is.EqualTo(2));
        Assert.That(readBack.Axes[0].AxisType, Is.EqualTo("cat"));
        Assert.That(readBack.Axes[0].AxisId, Is.EqualTo("111"));
        Assert.That(readBack.Axes[0].CrossAxisId, Is.EqualTo("222"));
        Assert.That(readBack.Axes[0].HasTitle, Is.True);
        Assert.That(readBack.Axes[0].TitleText, Is.EqualTo("Month"));
        Assert.That(readBack.Axes[1].AxisType, Is.EqualTo("val"));
        Assert.That(readBack.Axes[1].NumberFormat, Is.EqualTo("$#,##0"));
        Assert.That(readBack.Axes[1].TitleText, Is.EqualTo("Revenue"));
        Assert.That(readBack.Series.Count, Is.EqualTo(1));
        Assert.That(readBack.Series[0].HasColor, Is.True);
        Assert.That(readBack.Series[0].ColorRgb, Is.EqualTo("FF0000"));
        Assert.That(readBack.Series[0].HasDataLabels, Is.True);
        Assert.That(readBack.Series[0].DataLabelPosition, Is.EqualTo("bestFit"));
        Assert.That(readBack.Series[0].HasTrendline, Is.True);
        Assert.That(readBack.Series[0].TrendlineType, Is.EqualTo("linear"));
        Assert.That(readBack.Series[0].HasErrorBars, Is.True);
        Assert.That(readBack.Series[0].ErrorBarType, Is.EqualTo("both"));
        Assert.That(readBack.Series[0].ErrorBarValue, Is.EqualTo("2"));
    }

    [Test]
    public void ChartPartWriter_ReportsInvalidAxisAndLegendMetadata()
    {
        XlsxChartPart invalidAxis = CreateMetadataChart();
        invalidAxis.Axes[0].AxisType = "bad";
        XlsxChartPart nullAxis = CreateMetadataChart();
        nullAxis.Axes.Add(null!);
        XlsxChartPart nullLegend = CreateMetadataChart();
        nullLegend.Legend = null!;

        XlsxChartPartWriteResult invalidAxisWrite = new XlsxChartPartWriter().WriteString(invalidAxis);
        XlsxChartPartWriteResult nullAxisWrite = new XlsxChartPartWriter().WriteString(nullAxis);
        XlsxChartPartWriteResult nullLegendWrite = new XlsxChartPartWriter().WriteString(nullLegend);

        Assert.That(invalidAxisWrite.IsValid, Is.False);
        Assert.That(nullAxisWrite.IsValid, Is.False);
        Assert.That(nullLegendWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidAxisWrite, "chartWriter.axisTypeInvalid"), Is.True);
        Assert.That(HasDiagnostic(nullAxisWrite, "chartWriter.axisNull"), Is.True);
        Assert.That(HasDiagnostic(nullLegendWrite, "chartWriter.legendNull"), Is.True);
    }

    static XlsxChartPart CreateMetadataChart()
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = "";
        chart.HasStyle = true;
        chart.StyleId = 10;
        chart.HasTitle = true;
        chart.TitleText = "Sales";
        chart.HasLegend = true;
        chart.Legend.Position = "b";
        chart.Legend.HasOverlay = true;
        chart.Legend.Overlay = false;
        chart.ChartTypes.Add("line");
        XlsxChartSeriesReference series = chart.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4");
        series.HasColor = true;
        series.ColorRgb = "FF0000";
        series.HasDataLabels = true;
        series.DataLabelPosition = "bestFit";
        series.HasTrendline = true;
        series.TrendlineType = "linear";
        series.HasErrorBars = true;
        series.ErrorBarType = "both";
        series.ErrorBarValue = "2";

        XlsxChartAxis categoryAxis = new XlsxChartAxis();
        categoryAxis.AxisType = "cat";
        categoryAxis.AxisId = "111";
        categoryAxis.CrossAxisId = "222";
        categoryAxis.Position = "b";
        categoryAxis.HasTitle = true;
        categoryAxis.TitleText = "Month";
        chart.Axes.Add(categoryAxis);

        XlsxChartAxis valueAxis = new XlsxChartAxis();
        valueAxis.AxisType = "val";
        valueAxis.AxisId = "222";
        valueAxis.CrossAxisId = "111";
        valueAxis.Position = "l";
        valueAxis.NumberFormat = "$#,##0";
        valueAxis.HasTitle = true;
        valueAxis.TitleText = "Revenue";
        chart.Axes.Add(valueAxis);
        return chart;
    }

    static bool HasDiagnostic(XlsxChartPartWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
