using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ChartAuthoringCompletionTests
{
    [Test]
    public void ChartOperations_RoundTripAxisSeriesAndStyleAuthoring()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Line, CreateLineData(), new PptxBounds(0L, 0L, 600L, 300L));
        PptxChartOperations operations = new PptxChartOperations();
        PptxChartAxisScale scale = CreateAxisScale();
        PptxChartSeriesFormat format = CreateSeriesFormat();

        operations.SetMajorGridlines(chart, "value", true);
        operations.SetMinorGridlines(chart, "value", true);
        operations.SetAxisScale(chart, "value", scale);
        operations.SetSeriesFormat(chart, 0, format);
        operations.SetStyleXml(chart, "<cs:chartStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"42\"/>");
        operations.SetColorStyleXml(chart, "<cs:colorStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"43\"/>");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);
        PptxChartAxisScale reopenedScale = operations.GetAxisScale(reopenedChart, "value");
        PptxChartSeriesFormat reopenedFormat = operations.GetSeriesFormat(reopenedChart, 0);

        Assert.That(operations.HasMajorGridlines(reopenedChart, "value"), Is.True);
        Assert.That(operations.HasMinorGridlines(reopenedChart, "value"), Is.True);
        AssertAxisScale(reopenedScale);
        AssertSeriesFormat(reopenedFormat);
        XmlElement valueAxis = (XmlElement)reopenedChart.GetXmlDocument().GetElementsByTagName("valAx", PptxNamespaces.Chart)[0]!;
        Assert.That(valueAxis.GetElementsByTagName("crosses", PptxNamespaces.Chart).Count, Is.EqualTo(0));
        Assert.That(valueAxis.GetElementsByTagName("crossesAt", PptxNamespaces.Chart).Count, Is.EqualTo(1));
        Assert.That(Encoding.UTF8.GetString(operations.GetStylePart(reopenedChart)!.GetBytes()), Does.Contain("id=\"42\""));
        Assert.That(Encoding.UTF8.GetString(operations.GetColorStylePart(reopenedChart)!.GetBytes()), Does.Contain("id=\"43\""));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ChartOperations_EmptySeriesFormatClearsAppliedFormatting()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Line, CreateLineData(), new PptxBounds(0L, 0L, 600L, 300L));
        PptxChartOperations operations = new PptxChartOperations();

        operations.SetSeriesFormat(chart, 0, CreateSeriesFormat());
        operations.SetSeriesFormat(chart, 0, new PptxChartSeriesFormat());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChartSeriesFormat actual = operations.GetSeriesFormat(reopened.GetSlide(0).GetChart(0), 0);

        Assert.That(actual.HasFill, Is.False);
        Assert.That(actual.HasLine, Is.False);
        Assert.That(actual.MarkerSymbol, Is.Empty);
        Assert.That(actual.MarkerSize, Is.EqualTo(0));
        Assert.That(actual.Smooth, Is.False);
        Assert.That(actual.LabelPosition, Is.Empty);
        Assert.That(actual.NumberFormat, Is.Empty);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_UpdateCachesPreservesAuthoredChartSettings()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Line, CreateLineData(), new PptxBounds(0L, 0L, 600L, 300L));
        PptxChartOperations operations = new PptxChartOperations();

        operations.SetTitle(chart, "Preserved title");
        operations.SetLegendPosition(chart, "right");
        operations.SetAxisTitle(chart, "value", "Preserved axis");
        operations.SetMinorGridlines(chart, "value", true);
        operations.SetAxisScale(chart, "value", CreateAxisScale());
        operations.SetSeriesFormat(chart, 0, CreateSeriesFormat());
        operations.UpdateCaches(chart, CreateEditedLineData());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);

        Assert.That(operations.GetTitle(reopenedChart), Is.EqualTo("Preserved title"));
        Assert.That(operations.GetLegendPosition(reopenedChart), Is.EqualTo("r"));
        Assert.That(operations.GetAxisTitle(reopenedChart, "value"), Is.EqualTo("Preserved axis"));
        Assert.That(operations.HasMinorGridlines(reopenedChart, "value"), Is.True);
        AssertAxisScale(operations.GetAxisScale(reopenedChart, "value"));
        AssertSeriesFormat(operations.GetSeriesFormat(reopenedChart, 0));
        Assert.That(operations.GetData(reopenedChart).GetSeries(0).GetValue(1), Is.EqualTo("80"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ChartBuilder_CreatesAxesRequiredByEachChartFamily()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.AddChart(PptxChartType.Line, CreateLineData(), new PptxBounds(0L, 0L, 600L, 100L));
        slide.AddChart(PptxChartType.Scatter, CreateCoordinateData(false), new PptxBounds(0L, 100L, 600L, 100L));
        slide.AddChart(PptxChartType.Pie, CreateLineData(), new PptxBounds(0L, 200L, 600L, 100L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        XmlDocument line = reopened.GetSlide(0).GetChart(0).GetXmlDocument();
        XmlDocument scatter = reopened.GetSlide(0).GetChart(1).GetXmlDocument();
        XmlDocument pie = reopened.GetSlide(0).GetChart(2).GetXmlDocument();

        Assert.That(line.GetElementsByTagName("catAx", PptxNamespaces.Chart).Count, Is.EqualTo(1));
        Assert.That(line.GetElementsByTagName("valAx", PptxNamespaces.Chart).Count, Is.EqualTo(1));
        Assert.That(scatter.GetElementsByTagName("catAx", PptxNamespaces.Chart).Count, Is.EqualTo(0));
        Assert.That(scatter.GetElementsByTagName("valAx", PptxNamespaces.Chart).Count, Is.EqualTo(2));
        Assert.That(pie.GetElementsByTagName("catAx", PptxNamespaces.Chart).Count, Is.EqualTo(0));
        Assert.That(pie.GetElementsByTagName("valAx", PptxNamespaces.Chart).Count, Is.EqualTo(0));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetEmbeddedWorkbookWritesBubbleCoordinatesAndSizes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = CreateCoordinateData(true);
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Bubble, data, new PptxBounds(0L, 0L, 600L, 300L));

        PptxPart workbook = chart.SetEmbeddedWorkbook(data);
        string worksheetXml = GetZipEntryText(workbook.GetBytes(), "xl/worksheets/sheet1.xml");

        Assert.That(worksheetXml, Does.Contain("<c r=\"A2\"><v>1.5</v></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"B2\"><v>10</v></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"C2\"><v>3</v></c>"));
        Assert.That(chart.Xml, Does.Contain("Sheet1!$A$2:$A$3"));
        Assert.That(chart.Xml, Does.Contain("Sheet1!$B$2:$B$3"));
        Assert.That(chart.Xml, Does.Contain("Sheet1!$C$2:$C$3"));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetEmbeddedWorkbookWritesNumericCategoriesAsNumbers()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = CreateLineData();
        data.CategoryKind = PptxChartCategoryKind.Numeric;
        data.Categories.Clear();
        data.Categories.Add("45292");
        data.Categories.Add("45293");
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, data, new PptxBounds(0L, 0L, 600L, 300L));

        PptxPart workbook = chart.SetEmbeddedWorkbook(data);
        string worksheetXml = GetZipEntryText(workbook.GetBytes(), "xl/worksheets/sheet1.xml");

        Assert.That(worksheetXml, Does.Contain("<c r=\"A2\"><v>45292</v></c>"));
        Assert.That(worksheetXml, Does.Not.Contain("<c r=\"A2\" t=\"inlineStr\""));
        Assert.That(chart.Xml, Does.Contain("<c:numRef>"));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetEmbeddedWorkbookClassifiesOnlyFiniteInvariantNumbersAsNumericCells()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = new PptxChartData();
        data.Categories.Add("A");
        data.Categories.Add("B");
        data.Categories.Add("C");
        data.Categories.Add("D");
        data.Categories.Add("E");
        data.Categories.Add("F");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Mixed values";
        series.Values.Add(".");
        series.Values.Add("1-2");
        series.Values.Add("+");
        series.Values.Add("1e3");
        series.Values.Add("-2.5");
        series.Values.Add(".75");
        data.Series.Add(series);
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, data, new PptxBounds(0L, 0L, 600L, 300L));

        PptxPart workbook = chart.SetEmbeddedWorkbook(data);
        string worksheetXml = GetZipEntryText(workbook.GetBytes(), "xl/worksheets/sheet1.xml");

        Assert.That(worksheetXml, Does.Contain("<c r=\"B2\" t=\"inlineStr\"><is><t>.</t></is></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"B3\" t=\"inlineStr\"><is><t>1-2</t></is></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"B4\" t=\"inlineStr\"><is><t>+</t></is></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"B5\"><v>1e3</v></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"B6\"><v>-2.5</v></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"B7\"><v>.75</v></c>"));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetEmbeddedWorkbookUsesAdjacentColumnsForScatterSeries()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = CreateCoordinateData(false);
        PptxChartSeries second = new PptxChartSeries();
        second.Name = "Second";
        second.XValues.Add("5");
        second.YValues.Add("50");
        data.Series.Add(second);
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Scatter, data, new PptxBounds(0L, 0L, 600L, 300L));

        PptxPart workbook = chart.SetEmbeddedWorkbook(data);
        string worksheetXml = GetZipEntryText(workbook.GetBytes(), "xl/worksheets/sheet1.xml");

        Assert.That(worksheetXml, Does.Contain("<c r=\"C2\"><v>5</v></c>"));
        Assert.That(worksheetXml, Does.Contain("<c r=\"D2\"><v>50</v></c>"));
        Assert.That(worksheetXml, Does.Not.Contain("Size"));
        Assert.That(chart.Xml, Does.Contain("Sheet1!$C$2:$C$2"));
        Assert.That(chart.Xml, Does.Contain("Sheet1!$D$2:$D$2"));
        Assert.That(presentation.Package.Validate().Succeeded(), Is.True, presentation.Package.Validate().FormatDiagnostics());
    }

    static PptxChartAxisScale CreateAxisScale()
    {
        PptxChartAxisScale scale = new PptxChartAxisScale();
        scale.Minimum = "1";
        scale.Maximum = "100";
        scale.MajorUnit = "20";
        scale.MinorUnit = "5";
        scale.LogBase = "10";
        scale.CrossAt = "1";
        scale.Reverse = true;
        return scale;
    }

    static PptxChartSeriesFormat CreateSeriesFormat()
    {
        PptxChartSeriesFormat format = new PptxChartSeriesFormat();
        format.HasFill = true;
        format.Fill.Type = PptxFill.Solid;
        format.Fill.Present = true;
        format.Fill.Color.Type = PptxColor.Rgb;
        format.Fill.Color.Value = "112233";
        format.HasLine = true;
        format.Line.Type = PptxLine.Solid;
        format.Line.Present = true;
        format.Line.Width = 12700L;
        format.Line.Color.Type = PptxColor.Rgb;
        format.Line.Color.Value = "AABBCC";
        format.MarkerSymbol = "diamond";
        format.MarkerSize = 9;
        format.Smooth = true;
        format.LabelPosition = "t";
        format.NumberFormat = "0.0%";
        return format;
    }

    static void AssertAxisScale(PptxChartAxisScale scale)
    {
        Assert.That(scale.Minimum, Is.EqualTo("1"));
        Assert.That(scale.Maximum, Is.EqualTo("100"));
        Assert.That(scale.MajorUnit, Is.EqualTo("20"));
        Assert.That(scale.MinorUnit, Is.EqualTo("5"));
        Assert.That(scale.LogBase, Is.EqualTo("10"));
        Assert.That(scale.CrossAt, Is.EqualTo("1"));
        Assert.That(scale.Reverse, Is.True);
    }

    static void AssertSeriesFormat(PptxChartSeriesFormat format)
    {
        Assert.That(format.HasFill, Is.True);
        Assert.That(format.Fill.Type, Is.EqualTo(PptxFill.Solid));
        Assert.That(format.Fill.Color.Value, Is.EqualTo("112233"));
        Assert.That(format.HasLine, Is.True);
        Assert.That(format.Line.Type, Is.EqualTo(PptxLine.Solid));
        Assert.That(format.Line.Color.Value, Is.EqualTo("AABBCC"));
        Assert.That(format.Line.Width, Is.EqualTo(12700L));
        Assert.That(format.MarkerSymbol, Is.EqualTo("diamond"));
        Assert.That(format.MarkerSize, Is.EqualTo(9));
        Assert.That(format.Smooth, Is.True);
        Assert.That(format.LabelPosition, Is.EqualTo("t"));
        Assert.That(format.NumberFormat, Is.EqualTo("0.0%"));
    }

    static PptxChartData CreateLineData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("1");
        data.Categories.Add("2");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Revenue";
        series.Values.Add("10");
        series.Values.Add("20");
        data.Series.Add(series);
        return data;
    }

    static PptxChartData CreateEditedLineData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("3");
        data.Categories.Add("4");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Updated";
        series.Values.Add("40");
        series.Values.Add("80");
        data.Series.Add(series);
        return data;
    }

    static PptxChartData CreateCoordinateData(bool bubble)
    {
        PptxChartData data = new PptxChartData();
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Coordinates";
        series.XValues.Add("1.5");
        series.XValues.Add("2.5");
        series.YValues.Add("10");
        series.YValues.Add("20");
        if (bubble)
        {
            series.BubbleSizes.Add("3");
            series.BubbleSizes.Add("7");
        }

        data.Series.Add(series);
        return data;
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        return PptxDocument.OpenBytes(bytes).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        return PptxDocument.OpenBytes(bytes).LoadPresentation();
    }

    static string GetZipEntryText(byte[] bytes, string entryName)
    {
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry? entry = archive.GetEntry(entryName);
                if (entry == null)
                {
                    return "";
                }

                using (Stream entryStream = entry.Open())
                {
                    using (StreamReader reader = new StreamReader(entryStream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
