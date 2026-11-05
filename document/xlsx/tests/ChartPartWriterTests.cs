using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ChartPartWriterTests
{
    [TestCase("column", "barChart", "barDir val=\"col\"")]
    [TestCase("bar", "barChart", "barDir val=\"bar\"")]
    [TestCase("line", "lineChart", "grouping val=\"standard\"")]
    [TestCase("pie", "pieChart", "varyColors val=\"1\"")]
    [TestCase("area", "areaChart", "grouping val=\"standard\"")]
    [TestCase("scatter", "scatterChart", "<c:xVal>")]
    public void ChartPartWriter_WritesBasicChartTypesAndRoundTrips(string chartType, string expectedElement, string expectedDetail)
    {
        XlsxChartPart chart = CreateChart(chartType);
        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<c:chartSpace"));
        Assert.That(write.Xml, Does.Contain("<c:" + expectedElement + ">"));
        Assert.That(write.Xml, Does.Contain(expectedDetail));
        Assert.That(write.Xml, Does.Contain("<c:f>Sheet1!$B$2:$B$4</c:f>"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasTitle, Is.True);
        Assert.That(readBack.TitleText, Is.EqualTo("Sales"));
        Assert.That(readBack.ChartTypes.Count, Is.EqualTo(1));
        Assert.That(readBack.ChartTypes[0], Is.EqualTo(expectedElement));
        Assert.That(readBack.Series.Count, Is.EqualTo(1));
        Assert.That(readBack.Series[0].ValuesFormula, Is.EqualTo("Sheet1!$B$2:$B$4"));
    }

    [Test]
    public void ChartPartWriter_WritesComboChartsAsMultiplePlotTypes()
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = "";
        chart.HasTitle = true;
        chart.TitleText = "Sales";
        chart.ChartTypes.Add("column");
        chart.ChartTypes.Add("line");
        chart.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4");

        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<c:barChart>"));
        Assert.That(write.Xml, Does.Contain("<c:lineChart>"));
        Assert.That(write.Xml, Does.Contain("<c:catAx>"));
        Assert.That(write.Xml, Does.Contain("<c:valAx>"));
        Assert.That(readBack.ChartTypes.Count, Is.EqualTo(2));
        Assert.That(readBack.ChartTypes[0], Is.EqualTo("barChart"));
        Assert.That(readBack.ChartTypes[1], Is.EqualTo("lineChart"));
    }

    [Test]
    public void ChartPartWriter_PreservesRawXmlWhenPresent()
    {
        string rawXml = "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:futureChart /></c:chartSpace>";
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = rawXml;

        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(rawXml));
    }

    [Test]
    public void ChartPartWriter_ReportsInvalidStructuredCharts()
    {
        XlsxChartPart missingType = new XlsxChartPart();
        missingType.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4");

        XlsxChartPart unsupported = CreateChart("unsupportedChart");
        XlsxChartPart missingValues = new XlsxChartPart();
        missingValues.ChartTypes.Add("line");
        missingValues.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "");

        XlsxChartPart nullLists = new XlsxChartPart();
        nullLists.ChartTypes = null!;
        nullLists.Series = null!;
        nullLists.UnknownRootAttributes = null!;
        nullLists.UnknownXml = null!;

        XlsxChartPartWriteResult missingTypeWrite = new XlsxChartPartWriter().WriteString(missingType);
        XlsxChartPartWriteResult unsupportedWrite = new XlsxChartPartWriter().WriteString(unsupported);
        XlsxChartPartWriteResult missingValuesWrite = new XlsxChartPartWriter().WriteString(missingValues);
        XlsxChartPartWriteResult nullListsWrite = new XlsxChartPartWriter().WriteString(nullLists);

        Assert.That(missingTypeWrite.IsValid, Is.False);
        Assert.That(unsupportedWrite.IsValid, Is.False);
        Assert.That(missingValuesWrite.IsValid, Is.False);
        Assert.That(nullListsWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTypeWrite, "chartWriter.chartTypeMissing"), Is.True);
        Assert.That(HasDiagnostic(unsupportedWrite, "chartWriter.chartTypeUnsupported"), Is.True);
        Assert.That(HasDiagnostic(missingValuesWrite, "chartWriter.seriesValuesFormulaMissing"), Is.True);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.chartTypesNull"), Is.True);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.seriesNull"), Is.True);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.unknownRootAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.unknownXmlNull"), Is.True);
    }

    static XlsxChartPart CreateChart(string chartType)
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = "";
        chart.HasTitle = true;
        chart.TitleText = "Sales";
        chart.ChartTypes.Add(chartType);
        chart.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4");
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
