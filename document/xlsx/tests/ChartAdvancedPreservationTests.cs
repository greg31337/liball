using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ChartAdvancedPreservationTests
{
    [Test]
    public void ChartPartReaderWriter_PreservesAdvancedChartTypeRawXml()
    {
        string xml = AdvancedChartXml();
        XlsxChartPart part = new XlsxChartPartReader().ReadString(xml);
        part.RawXml = "";

        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(part);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.ChartTypes.Count, Is.EqualTo(1));
        Assert.That(part.ChartTypes[0], Is.EqualTo("doughnutChart"));
        Assert.That(part.AdvancedChartElements.Count, Is.EqualTo(1));
        Assert.That(part.AdvancedChartElements[0].RawXml, Does.Contain("<c:doughnutChart"));
        Assert.That(part.AdvancedChartElements[0].RawXml, Does.Contain("<c:holeSize val=\"55\" />"));
        Assert.That(part.ExtensionXml.Count, Is.EqualTo(1));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<c:doughnutChart"));
        Assert.That(write.Xml, Does.Contain("<c:holeSize val=\"55\" />"));
        Assert.That(write.Xml, Does.Contain("<c:ext uri=\"{future-chart}\""));
        Assert.That(readBack.ChartTypes.Count, Is.EqualTo(1));
        Assert.That(readBack.ChartTypes[0], Is.EqualTo("doughnutChart"));
        Assert.That(readBack.AdvancedChartElements.Count, Is.EqualTo(1));
        Assert.That(readBack.ExtensionXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ChartPartReaderWriter_PreservesChartAndSeriesExtensions()
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = "";
        chart.ChartTypes.Add("line");
        XlsxChartSeriesReference series = chart.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4");
        series.ExtensionXml.Add("<c:extLst><c:ext uri=\"{series-extension}\"><x:seriesFuture xmlns:x=\"urn:future\" value=\"1\" /></c:ext></c:extLst>");
        chart.ExtensionXml.Add("<c:extLst><c:ext uri=\"{chart-extension}\"><x:chartFuture xmlns:x=\"urn:future\" value=\"2\" /></c:ext></c:extLst>");

        XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteString(chart);
        XlsxChartPart readBack = new XlsxChartPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("{series-extension}"));
        Assert.That(write.Xml, Does.Contain("{chart-extension}"));
        Assert.That(readBack.Series.Count, Is.EqualTo(1));
        Assert.That(readBack.Series[0].ExtensionXml.Count, Is.EqualTo(1));
        Assert.That(readBack.Series[0].ExtensionXml[0], Does.Contain("seriesFuture"));
        Assert.That(readBack.ExtensionXml.Count, Is.EqualTo(1));
        Assert.That(readBack.ExtensionXml[0], Does.Contain("chartFuture"));
    }

    [Test]
    public void ChartPartWriter_ReportsInvalidAdvancedPreservationLists()
    {
        XlsxChartPart nullLists = CreateBasicChart();
        nullLists.AdvancedChartElements = null!;
        nullLists.ExtensionXml = null!;
        nullLists.Series[0].ExtensionXml = null!;
        XlsxChartPart missingRaw = CreateBasicChart();
        missingRaw.AdvancedChartElements.Add(new XlsxChartRawElement());

        XlsxChartPartWriteResult nullListsWrite = new XlsxChartPartWriter().WriteString(nullLists);
        XlsxChartPartWriteResult missingRawWrite = new XlsxChartPartWriter().WriteString(missingRaw);

        Assert.That(nullListsWrite.IsValid, Is.False);
        Assert.That(missingRawWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.advancedChartElementsNull"), Is.True);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.extensionXmlNull"), Is.True);
        Assert.That(HasDiagnostic(nullListsWrite, "chartWriter.seriesExtensionXmlNull"), Is.True);
        Assert.That(HasDiagnostic(missingRawWrite, "chartWriter.advancedChartElementXmlMissing"), Is.True);
    }

    static string AdvancedChartXml()
    {
        return "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:c15=\"http://schemas.microsoft.com/office/drawing/2012/chart\">" +
            "<c:chart><c:title><c:tx><c:rich><a:bodyPr /><a:p><a:r><a:t>Advanced</a:t></a:r></a:p></c:rich></c:tx></c:title><c:plotArea><c:layout />" +
            "<c:doughnutChart><c:varyColors val=\"1\" /><c:ser><c:idx val=\"0\" /><c:order val=\"0\" /><c:val><c:numRef><c:f>Sheet1!$B$2:$B$4</c:f></c:numRef></c:val></c:ser><c:holeSize val=\"55\" /></c:doughnutChart>" +
            "</c:plotArea></c:chart><c:extLst><c:ext uri=\"{future-chart}\"><c15:filteredSeriesTitle><c15:v>Future</c15:v></c15:filteredSeriesTitle></c:ext></c:extLst></c:chartSpace>";
    }

    static XlsxChartPart CreateBasicChart()
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = "";
        chart.ChartTypes.Add("line");
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
