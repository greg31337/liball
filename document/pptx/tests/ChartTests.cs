using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ChartTests
{
    [Test]
    public void Slide_AddChartCreatesChartPartFrameAndCachedData()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = CreateSimpleChartData();

        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, data, new PptxBounds(0L, 0L, 600L, 300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);
        PptxChartData reopenedData = reopenedChart.GetData();

        Assert.That(chart.PartName, Is.EqualTo("/ppt/charts/chart1.xml"));
        Assert.That(chart.Shape.IsChart(), Is.True);
        Assert.That(reopened.GetSlide(0).GetCharts().Count, Is.EqualTo(1));
        Assert.That(reopened.Package.HasPart("/ppt/charts/chart1.xml"), Is.True);
        Assert.That(reopenedChart.GetChartType(), Is.EqualTo(PptxChartType.Column));
        Assert.That(reopenedChart.Xml, Does.Contain("barDir"));
        Assert.That(reopenedChart.Xml, Does.Contain("val=\"col\""));
        Assert.That(reopenedData.GetCategory(0), Is.EqualTo("1"));
        Assert.That(reopenedData.GetSeries(0).Name, Is.EqualTo("Revenue"));
        Assert.That(reopenedData.GetSeries(0).GetValue(1), Is.EqualTo("20"));
        Assert.That(reopenedData.GetSeries(1).Name, Is.EqualTo("Margin"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetDataAndTypeRewritesExistingChartPart()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Line, CreateSimpleChartData(), new PptxBounds(0L, 0L, 600L, 300L));

        chart.SetDataWithChartType(PptxChartType.Pie, CreateEditedChartData());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);
        PptxChartData reopenedData = reopenedChart.GetData();

        Assert.That(reopenedChart.PartName, Is.EqualTo("/ppt/charts/chart1.xml"));
        Assert.That(reopenedChart.GetChartType(), Is.EqualTo(PptxChartType.Pie));
        Assert.That(reopenedChart.Xml, Does.Contain("pieChart"));
        Assert.That(reopenedChart.Xml, Does.Not.Contain("lineChart"));
        Assert.That(reopenedData.GetCategory(0), Is.EqualTo("3"));
        Assert.That(reopenedData.GetSeries(0).Name, Is.EqualTo("Edited"));
        Assert.That(reopenedData.GetSeries(0).GetValue(0), Is.EqualTo("30"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_AddChartSupportsCommonChartFamilies()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<string> chartTypes = CreateAllChartTypes();

        for (int index = 0; index < chartTypes.Count; index++)
        {
            slide.AddChart(chartTypes[index], CreateSimpleChartData(), new PptxBounds(0L, (long)index * 100L, 600L, 90L));
        }

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        Assert.That(reopened.GetSlide(0).GetCharts().Count, Is.EqualTo(chartTypes.Count));
        for (int index = 0; index < chartTypes.Count; index++)
        {
            Assert.That(reopened.GetSlide(0).GetChart(index).GetChartType(), Is.EqualTo(PptxChartTypeCore.Normalize(chartTypes[index])));
        }

        Assert.That(reopened.Package.HasPart("/ppt/charts/chart12.xml"), Is.True);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetTitlesLegendAxesGridlinesMarkersAndDataLabels()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Line, CreateSimpleChartData(), new PptxBounds(0L, 0L, 600L, 300L));

        chart.SetTitle("Quarterly Results");
        chart.SetLegendPosition("bottom");
        chart.SetAxisTitle("category", "Quarter");
        chart.SetAxisTitle("value", "Amount");
        chart.SetMajorGridlines("value", true);
        chart.SetMarkers(true);
        chart.SetDataLabels(true);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);

        Assert.That(reopenedChart.GetTitle(), Is.EqualTo("Quarterly Results"));
        Assert.That(reopenedChart.GetLegendPosition(), Is.EqualTo("b"));
        Assert.That(reopenedChart.GetAxisTitle("category"), Is.EqualTo("Quarter"));
        Assert.That(reopenedChart.GetAxisTitle("value"), Is.EqualTo("Amount"));
        Assert.That(reopenedChart.HasMajorGridlines("value"), Is.True);
        Assert.That(reopenedChart.HasMarkers(), Is.True);
        Assert.That(reopenedChart.HasDataLabels(), Is.True);
        Assert.That(reopenedChart.Xml, Does.Contain("legendPos"));
        Assert.That(reopenedChart.Xml, Does.Contain("majorGridlines"));
        Assert.That(reopenedChart.Xml, Does.Contain("marker"));
        Assert.That(reopenedChart.Xml, Does.Contain("dLbls"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetStyleAndColorStylePartsRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, CreateSimpleChartData(), new PptxBounds(0L, 0L, 600L, 300L));

        chart.SetStyleXml("<cs:chartStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"42\"/>");
        chart.SetColorStyleXml("<cs:colorStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"43\"/>");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);
        PptxPart? stylePart = reopenedChart.GetStylePart();
        PptxPart? colorPart = reopenedChart.GetColorStylePart();

        Assert.That(stylePart, Is.Not.Null);
        Assert.That(colorPart, Is.Not.Null);
        Assert.That(stylePart!.PartName, Is.EqualTo("/ppt/charts/style1.xml"));
        Assert.That(colorPart!.PartName, Is.EqualTo("/ppt/charts/colors1.xml"));
        Assert.That(Encoding.UTF8.GetString(stylePart.GetBytes()), Does.Contain("id=\"42\""));
        Assert.That(Encoding.UTF8.GetString(colorPart.GetBytes()), Does.Contain("id=\"43\""));
        Assert.That(FindRelationship(reopenedChart.GetRelationships(), PptxRelationshipTypes.ChartStyle).TargetPartName, Is.EqualTo("/ppt/charts/style1.xml"));
        Assert.That(FindRelationship(reopenedChart.GetRelationships(), PptxRelationshipTypes.ChartColorStyle).TargetPartName, Is.EqualTo("/ppt/charts/colors1.xml"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_UpdateCachesRewritesCachedValuesAndWorkbookFormulas()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, CreateSimpleChartData(), new PptxBounds(0L, 0L, 600L, 300L));

        chart.UpdateCaches(CreateEditedChartData());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);
        PptxChartData data = reopenedChart.GetData();

        Assert.That(data.GetCategory(0), Is.EqualTo("3"));
        Assert.That(data.GetSeries(0).Name, Is.EqualTo("Edited"));
        Assert.That(data.GetSeries(0).GetValue(1), Is.EqualTo("40"));
        Assert.That(reopenedChart.Xml, Does.Contain("Sheet1!$A$2:$A$3"));
        Assert.That(reopenedChart.Xml, Does.Contain("Sheet1!$B$1"));
        Assert.That(reopenedChart.Xml, Does.Contain("Sheet1!$B$2:$B$3"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_SetEmbeddedWorkbookCreatesXlsxPartAndExternalData()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChart chart = presentation.GetSlide(0).AddChart(PptxChartType.Column, CreateSimpleChartData(), new PptxBounds(0L, 0L, 600L, 300L));

        chart.SetEmbeddedWorkbook(CreateSimpleChartData());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart reopenedChart = reopened.GetSlide(0).GetChart(0);
        PptxPart? workbookPart = reopenedChart.GetEmbeddedWorkbookPart();
        string worksheetXml = GetZipEntryText(workbookPart!.GetBytes(), "xl/worksheets/sheet1.xml");

        Assert.That(workbookPart, Is.Not.Null);
        Assert.That(workbookPart!.PartName, Is.EqualTo("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"));
        Assert.That(reopenedChart.GetExternalDataRelationshipId(), Is.EqualTo(FindRelationship(reopenedChart.GetRelationships(), PptxRelationshipTypes.Package).Id));
        Assert.That(GetZipEntryText(workbookPart.GetBytes(), "[Content_Types].xml"), Does.Contain("spreadsheetml.sheet.main+xml"));
        Assert.That(GetZipEntryText(workbookPart.GetBytes(), "xl/workbook.xml"), Does.Contain("Sheet1"));
        Assert.That(worksheetXml, Does.Contain("Revenue"));
        Assert.That(worksheetXml, Does.Contain("<v>20</v>"));
        Assert.That(reopenedChart.Xml, Does.Contain("externalData"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_ExtractDataTableReturnsHeaderCategoriesAndSeriesValues()
    {
        PptxPresentation presentation = LoadChartPresentation();
        List<List<string>> table = presentation.GetSlide(0).GetChart(0).ExtractDataTable();

        Assert.That(table.Count, Is.EqualTo(3));
        Assert.That(table[0].Count, Is.EqualTo(3));
        Assert.That(table[0][0], Is.EqualTo("Category"));
        Assert.That(table[0][1], Is.EqualTo("Revenue"));
        Assert.That(table[0][2], Is.EqualTo("Margin"));
        Assert.That(table[1][0], Is.EqualTo("Q1"));
        Assert.That(table[1][1], Is.EqualTo("10"));
        Assert.That(table[1][2], Is.EqualTo("0.15"));
        Assert.That(table[2][0], Is.EqualTo("Q2"));
        Assert.That(table[2][1], Is.EqualTo("20"));
        Assert.That(table[2][2], Is.EqualTo("0.25"));
    }

    [Test]
    public void Slide_ExtractChartDataTablesPadsMissingSeriesValues()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.AddChart(PptxChartType.Column, CreateUnevenChartData(), new PptxBounds(0L, 0L, 600L, 300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<List<List<string>>> tables = reopened.GetSlide(0).ExtractChartDataTables();
        List<List<string>> table = tables[0];

        Assert.That(tables.Count, Is.EqualTo(1));
        Assert.That(table.Count, Is.EqualTo(4));
        Assert.That(table[0][1], Is.EqualTo("Short"));
        Assert.That(table[0][2], Is.EqualTo("Long"));
        Assert.That(table[1][0], Is.EqualTo("1"));
        Assert.That(table[1][1], Is.EqualTo("10"));
        Assert.That(table[1][2], Is.EqualTo("100"));
        Assert.That(table[2][0], Is.EqualTo("2"));
        Assert.That(table[2][1], Is.EqualTo(""));
        Assert.That(table[2][2], Is.EqualTo("200"));
        Assert.That(table[3][0], Is.EqualTo("3"));
        Assert.That(table[3][1], Is.EqualTo(""));
        Assert.That(table[3][2], Is.EqualTo("300"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_LoadsChartFramesAndResolvesChartPart()
    {
        PptxPresentation presentation = LoadChartPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxChart> charts = slide.GetCharts();
        PptxChart chart = charts[0];
        XmlDocument chartDocument = chart.GetXmlDocument();

        Assert.That(charts.Count, Is.EqualTo(1));
        Assert.That(slide.GetChart(0).RelationshipId, Is.EqualTo("rIdChart"));
        Assert.That(chart.Shape.IsChart(), Is.True);
        Assert.That(chart.Shape.IsTable(), Is.False);
        Assert.That(chart.Shape.GetChartRelationshipId(), Is.EqualTo("rIdChart"));
        Assert.That(chart.Shape.AsChart().PartName, Is.EqualTo("/ppt/charts/chart1.xml"));
        Assert.That(chart.HasPart(), Is.True);
        Assert.That(chart.Relationship, Is.Not.Null);
        Assert.That(chart.Relationship!.Target, Is.EqualTo("../charts/chart1.xml"));
        Assert.That(chart.PartName, Is.EqualTo("/ppt/charts/chart1.xml"));
        Assert.That(chart.ContentType, Is.EqualTo(PptxContentTypes.Chart));
        Assert.That(chart.Xml, Does.Contain("Preserved Extension"));
        Assert.That(chartDocument.DocumentElement!.LocalName, Is.EqualTo("chartSpace"));
    }

    [Test]
    public void ChartPartRelationshipsAndUnknownXmlPreserveOnRoundTrip()
    {
        PptxPresentation presentation = LoadChartPresentation();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart chart = reopened.GetSlide(0).GetChart(0);
        List<PptxRelationship> relationships = chart.GetRelationships();
        List<PptxPart> relatedParts = chart.GetRelatedParts();

        Assert.That(reopened.Package.HasPart("/ppt/charts/chart1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/charts/style1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/charts/colors1.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"), Is.True);
        Assert.That(reopened.Package.GetPart("/ppt/charts/chart1.xml").GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateChartXml())));
        Assert.That(relationships.Count, Is.EqualTo(3));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.ChartStyle).TargetPartName, Is.EqualTo("/ppt/charts/style1.xml"));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.ChartColorStyle).TargetPartName, Is.EqualTo("/ppt/charts/colors1.xml"));
        Assert.That(FindRelationship(relationships, PptxRelationshipTypes.Package).TargetPartName, Is.EqualTo("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"));
        Assert.That(ContainsPart(relatedParts, "/ppt/charts/style1.xml"), Is.True);
        Assert.That(ContainsPart(relatedParts, "/ppt/charts/colors1.xml"), Is.True);
        Assert.That(ContainsPart(relatedParts, "/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"), Is.True);
        Assert.That(chart.Xml, Does.Contain("cx:unknown"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Chart_LoadsDataModelForCategoriesSeriesValuesLabelsAndFormats()
    {
        PptxPresentation presentation = LoadChartPresentation();
        PptxChartData data = presentation.GetSlide(0).GetChart(0).GetData();
        PptxChartSeries revenue = data.GetSeries(0);
        PptxChartSeries margin = data.GetSeries(1);

        Assert.That(data.CategoryCount(), Is.EqualTo(2));
        Assert.That(data.GetCategory(0), Is.EqualTo("Q1"));
        Assert.That(data.GetCategory(1), Is.EqualTo("Q2"));
        Assert.That(data.SeriesCount(), Is.EqualTo(2));
        Assert.That(revenue.Name, Is.EqualTo("Revenue"));
        Assert.That(revenue.CategoryCount(), Is.EqualTo(2));
        Assert.That(revenue.GetCategory(0), Is.EqualTo("Q1"));
        Assert.That(revenue.ValueCount(), Is.EqualTo(2));
        Assert.That(revenue.GetValue(0), Is.EqualTo("10"));
        Assert.That(revenue.GetValue(1), Is.EqualTo("20"));
        Assert.That(revenue.ValuesNumberFormat, Is.EqualTo("$#,##0"));
        Assert.That(revenue.NumberFormat, Is.EqualTo("0.0%"));
        Assert.That(revenue.LabelCount(), Is.EqualTo(1));
        Assert.That(revenue.GetLabel(0), Is.EqualTo("Revenue Label"));
        Assert.That(margin.Name, Is.EqualTo("Margin"));
        Assert.That(margin.GetValue(0), Is.EqualTo("0.15"));
        Assert.That(margin.GetValue(1), Is.EqualTo("0.25"));
        Assert.That(margin.ValuesNumberFormat, Is.EqualTo("0.0%"));
        Assert.That(data.NumberFormatCount(), Is.EqualTo(2));
        Assert.That(ContainsString(data.NumberFormats, "$#,##0"), Is.True);
        Assert.That(ContainsString(data.NumberFormats, "0.0%"), Is.True);
    }

    static PptxPresentation LoadChartPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateChartSlideXml()));
        package.AddPart("/ppt/charts/chart1.xml", PptxContentTypes.Chart, Encoding.UTF8.GetBytes(CreateChartXml()));
        package.AddPart("/ppt/charts/style1.xml", PptxContentTypes.ChartStyle, Encoding.UTF8.GetBytes("<cs:chartStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"10\"/>"));
        package.AddPart("/ppt/charts/colors1.xml", PptxContentTypes.ChartColorStyle, Encoding.UTF8.GetBytes("<cs:colorStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"11\"/>"));
        package.AddPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx", PptxContentTypes.Binary, new byte[] { 1, 2, 3, 4 });
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdChart", PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId("/ppt/charts/chart1.xml", "rIdStyle", PptxRelationshipTypes.ChartStyle, "style1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId("/ppt/charts/chart1.xml", "rIdColor", PptxRelationshipTypes.ChartColorStyle, "colors1.xml", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId("/ppt/charts/chart1.xml", "rIdWorkbook", PptxRelationshipTypes.Package, "../embeddings/Microsoft_Excel_Worksheet1.xlsx", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateChartSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"2\" name=\"Sales Chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"10\" y=\"20\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart\"/></a:graphicData></a:graphic>" +
            "</p:graphicFrame>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateChartXml()
    {
        return
            "<c:chartSpace xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:c=\"" + PptxNamespaces.Chart + "\" xmlns:cx=\"http://example.com/chart-extension\">" +
            "<c:chart>" +
            "<c:title><c:tx><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>Preserved Chart</c:v></c:pt></c:strCache></c:strRef></c:tx></c:title>" +
            "<c:plotArea><c:barChart>" +
            CreateRevenueSeriesXml() +
            CreateMarginSeriesXml() +
            "</c:barChart></c:plotArea>" +
            "<c:extLst><c:ext uri=\"preserve\"><cx:unknown>Preserved Extension</cx:unknown></c:ext></c:extLst>" +
            "</c:chart>" +
            "</c:chartSpace>";
    }

    static string CreateRevenueSeriesXml()
    {
        return
            "<c:ser>" +
            "<c:idx val=\"0\"/><c:order val=\"0\"/>" +
            "<c:tx><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>Revenue</c:v></c:pt></c:strCache></c:strRef></c:tx>" +
            CreateCategoryXml() +
            "<c:val><c:numRef><c:f>Sheet1!$B$2:$B$3</c:f><c:numCache><c:formatCode>$#,##0</c:formatCode><c:pt idx=\"0\"><c:v>10</c:v></c:pt><c:pt idx=\"1\"><c:v>20</c:v></c:pt></c:numCache></c:numRef></c:val>" +
            "<c:dLbls><c:numFmt formatCode=\"0.0%\" sourceLinked=\"0\"/><c:dLbl><c:idx val=\"0\"/><c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>Revenue Label</a:t></a:r></a:p></c:rich></c:tx></c:dLbl></c:dLbls>" +
            "</c:ser>";
    }

    static string CreateMarginSeriesXml()
    {
        return
            "<c:ser>" +
            "<c:idx val=\"1\"/><c:order val=\"1\"/>" +
            "<c:tx><c:strRef><c:strCache><c:pt idx=\"0\"><c:v>Margin</c:v></c:pt></c:strCache></c:strRef></c:tx>" +
            CreateCategoryXml() +
            "<c:val><c:numRef><c:f>Sheet1!$C$2:$C$3</c:f><c:numCache><c:formatCode>0.0%</c:formatCode><c:pt idx=\"0\"><c:v>0.15</c:v></c:pt><c:pt idx=\"1\"><c:v>0.25</c:v></c:pt></c:numCache></c:numRef></c:val>" +
            "</c:ser>";
    }

    static string CreateCategoryXml()
    {
        return
            "<c:cat><c:strRef><c:f>Sheet1!$A$2:$A$3</c:f><c:strCache><c:pt idx=\"0\"><c:v>Q1</c:v></c:pt><c:pt idx=\"1\"><c:v>Q2</c:v></c:pt></c:strCache></c:strRef></c:cat>";
    }

    static PptxRelationship FindRelationship(List<PptxRelationship> relationships, string type)
    {
        for (int index = 0; index < relationships.Count; index++)
        {
            if (relationships[index].Type == type)
            {
                return relationships[index];
            }
        }

        throw new System.ArgumentException("Relationship was not found.");
    }

    static bool ContainsPart(List<PptxPart> parts, string partName)
    {
        for (int index = 0; index < parts.Count; index++)
        {
            if (parts[index].PartName == partName)
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsString(List<string> values, string value)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (values[index] == value)
            {
                return true;
            }
        }

        return false;
    }

    static PptxChartData CreateSimpleChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("1");
        data.Categories.Add("2");
        PptxChartSeries revenue = new PptxChartSeries();
        revenue.Name = "Revenue";
        revenue.ValuesNumberFormat = "$#,##0";
        revenue.Values.Add("10");
        revenue.Values.Add("20");
        data.Series.Add(revenue);
        PptxChartSeries margin = new PptxChartSeries();
        margin.Name = "Margin";
        margin.ValuesNumberFormat = "0.0%";
        margin.Values.Add("0.15");
        margin.Values.Add("0.25");
        data.Series.Add(margin);
        return data;
    }

    static PptxChartData CreateEditedChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("3");
        data.Categories.Add("4");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Edited";
        series.ValuesNumberFormat = "General";
        series.Values.Add("30");
        series.Values.Add("40");
        data.Series.Add(series);
        return data;
    }

    static PptxChartData CreateUnevenChartData()
    {
        PptxChartData data = new PptxChartData();
        data.Categories.Add("1");
        data.Categories.Add("2");
        data.Categories.Add("3");
        PptxChartSeries shortSeries = new PptxChartSeries();
        shortSeries.Name = "Short";
        shortSeries.Values.Add("10");
        data.Series.Add(shortSeries);
        PptxChartSeries longSeries = new PptxChartSeries();
        longSeries.Name = "Long";
        longSeries.Values.Add("100");
        longSeries.Values.Add("200");
        longSeries.Values.Add("300");
        data.Series.Add(longSeries);
        return data;
    }

    static List<string> CreateAllChartTypes()
    {
        List<string> chartTypes = new List<string>();
        chartTypes.Add(PptxChartType.Column);
        chartTypes.Add(PptxChartType.Bar);
        chartTypes.Add(PptxChartType.Line);
        chartTypes.Add(PptxChartType.Pie);
        chartTypes.Add(PptxChartType.Doughnut);
        chartTypes.Add(PptxChartType.Area);
        chartTypes.Add(PptxChartType.Scatter);
        chartTypes.Add(PptxChartType.Bubble);
        chartTypes.Add(PptxChartType.Radar);
        chartTypes.Add(PptxChartType.Stock);
        chartTypes.Add(PptxChartType.Surface);
        chartTypes.Add(PptxChartType.Combo);
        return chartTypes;
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
