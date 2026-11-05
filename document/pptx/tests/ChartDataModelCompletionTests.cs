using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ChartDataModelCompletionTests
{
    [Test]
    public void ChartData_NumericCategoriesLabelsFormulasAndFormatsRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = new PptxChartData();
        data.CategoryKind = PptxChartCategoryKind.Numeric;
        data.CategoryFormula = "Sheet1!$A$2:$A$3";
        data.CategoriesNumberFormat = "yyyy-mm-dd";
        data.Categories.Add("45292");
        data.Categories.Add("45293");
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Revenue";
        series.NameFormula = "Sheet1!$B$1";
        series.ValuesFormula = "Sheet1!$B$2:$B$3";
        series.ValuesNumberFormat = "$#,##0";
        series.NumberFormat = "0.0%";
        series.LabelFormula = "Sheet1!$C$2:$C$3";
        series.Values.Add("100");
        series.Values.Add("125");
        series.Labels.Add("Start");
        series.Labels.Add("Finish");
        data.Series.Add(series);

        presentation.GetSlide(0).AddChart(PptxChartType.Line, data, new PptxBounds(0L, 0L, 600L, 300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChartData actual = reopened.GetSlide(0).GetChart(0).GetData();
        PptxChartSeries actualSeries = actual.GetSeries(0);

        Assert.That(actual.CategoryKind, Is.EqualTo(PptxChartCategoryKind.Numeric));
        Assert.That(actual.CategoryFormula, Is.EqualTo("Sheet1!$A$2:$A$3"));
        Assert.That(actual.CategoriesNumberFormat, Is.EqualTo("yyyy-mm-dd"));
        Assert.That(actual.GetCategory(0), Is.EqualTo("45292"));
        Assert.That(actualSeries.NameFormula, Is.EqualTo("Sheet1!$B$1"));
        Assert.That(actualSeries.ValuesFormula, Is.EqualTo("Sheet1!$B$2:$B$3"));
        Assert.That(actualSeries.ValuesNumberFormat, Is.EqualTo("$#,##0"));
        Assert.That(actualSeries.NumberFormat, Is.EqualTo("0.0%"));
        Assert.That(actualSeries.LabelFormula, Is.EqualTo("Sheet1!$C$2:$C$3"));
        Assert.That(actualSeries.Labels, Is.EqualTo(new string[] { "Start", "Finish" }));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ChartData_BubbleCoordinatesSizesAndFormulasRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = new PptxChartData();
        PptxChartSeries series = new PptxChartSeries();
        series.Name = "Bubbles";
        series.XFormula = "Sheet1!$A$2:$A$3";
        series.YFormula = "Sheet1!$B$2:$B$3";
        series.BubbleSizeFormula = "Sheet1!$C$2:$C$3";
        series.CategoriesNumberFormat = "0.00";
        series.ValuesNumberFormat = "0.0";
        series.BubbleSizesNumberFormat = "0";
        series.XValues.Add("1.25");
        series.XValues.Add("2.5");
        series.YValues.Add("10");
        series.YValues.Add("20");
        series.BubbleSizes.Add("5");
        series.BubbleSizes.Add("9");
        data.Series.Add(series);

        presentation.GetSlide(0).AddChart(PptxChartType.Bubble, data, new PptxBounds(0L, 0L, 600L, 300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChartSeries actual = reopened.GetSlide(0).GetChart(0).GetData().GetSeries(0);
        PptxChartSeriesOperations operations = new PptxChartSeriesOperations();

        Assert.That(actual.ChartType, Is.EqualTo(PptxChartType.Bubble));
        Assert.That(actual.CategoryKind, Is.EqualTo(PptxChartCategoryKind.Numeric));
        Assert.That(operations.XValueCount(actual), Is.EqualTo(2));
        Assert.That(operations.YValueCount(actual), Is.EqualTo(2));
        Assert.That(operations.BubbleSizeCount(actual), Is.EqualTo(2));
        Assert.That(operations.GetXValue(actual, 1), Is.EqualTo("2.5"));
        Assert.That(operations.GetYValue(actual, 0), Is.EqualTo("10"));
        Assert.That(operations.GetBubbleSize(actual, 1), Is.EqualTo("9"));
        Assert.That(actual.XFormula, Is.EqualTo("Sheet1!$A$2:$A$3"));
        Assert.That(actual.YFormula, Is.EqualTo("Sheet1!$B$2:$B$3"));
        Assert.That(actual.BubbleSizeFormula, Is.EqualTo("Sheet1!$C$2:$C$3"));
        Assert.That(actual.CategoriesNumberFormat, Is.EqualTo("0.00"));
        Assert.That(actual.BubbleSizesNumberFormat, Is.EqualTo("0"));
    }

    [Test]
    public void ChartData_StockSeriesReceiveTypedRoles()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = new PptxChartData();
        data.Categories.Add("Day 1");
        data.Categories.Add("Day 2");
        data.Series.Add(CreateSeries("Open", "10", "11"));
        data.Series.Add(CreateSeries("High", "14", "15"));
        data.Series.Add(CreateSeries("Low", "8", "9"));
        data.Series.Add(CreateSeries("Close", "12", "13"));

        presentation.GetSlide(0).AddChart(PptxChartType.Stock, data, new PptxBounds(0L, 0L, 600L, 300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChartData actual = reopened.GetSlide(0).GetChart(0).GetData();

        Assert.That(actual.GetSeries(0).StockRole, Is.EqualTo(PptxStockSeriesRole.Open));
        Assert.That(actual.GetSeries(1).StockRole, Is.EqualTo(PptxStockSeriesRole.High));
        Assert.That(actual.GetSeries(2).StockRole, Is.EqualTo(PptxStockSeriesRole.Low));
        Assert.That(actual.GetSeries(3).StockRole, Is.EqualTo(PptxStockSeriesRole.Close));
        Assert.That(actual.GetSeries(3).Values, Is.EqualTo(new string[] { "12", "13" }));
    }

    [Test]
    public void ChartData_ComboSeriesKeepIndependentTypesAndOriginalOrder()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxChartData data = new PptxChartData();
        data.Categories.Add("Q1");
        data.Categories.Add("Q2");
        PptxChartSeries first = CreateSeries("Trend A", "1", "2");
        first.ChartType = PptxChartType.Line;
        PptxChartSeries second = CreateSeries("Bars", "3", "4");
        second.ChartType = PptxChartType.Column;
        PptxChartSeries third = CreateSeries("Trend B", "5", "6");
        third.ChartType = PptxChartType.Line;
        data.Series.Add(first);
        data.Series.Add(second);
        data.Series.Add(third);

        presentation.GetSlide(0).AddChart(PptxChartType.Combo, data, new PptxBounds(0L, 0L, 600L, 300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxChart chart = reopened.GetSlide(0).GetChart(0);
        PptxChartData actual = chart.GetData();

        Assert.That(chart.GetChartType(), Is.EqualTo(PptxChartType.Combo));
        Assert.That(actual.GetSeries(0).Name, Is.EqualTo("Trend A"));
        Assert.That(actual.GetSeries(0).ChartType, Is.EqualTo(PptxChartType.Line));
        Assert.That(actual.GetSeries(1).Name, Is.EqualTo("Bars"));
        Assert.That(actual.GetSeries(1).ChartType, Is.EqualTo(PptxChartType.Column));
        Assert.That(actual.GetSeries(2).Name, Is.EqualTo("Trend B"));
        Assert.That(actual.GetSeries(2).ChartType, Is.EqualTo(PptxChartType.Line));

        actual.GetSeries(1).ChartType = PptxChartType.Line;
        actual.GetSeries(2).ChartType = PptxChartType.Column;
        chart.SetData(actual);
        PptxPresentation edited = SaveReopenAndLoad(reopened);
        PptxChartData editedData = edited.GetSlide(0).GetChart(0).GetData();
        Assert.That(editedData.GetSeries(1).ChartType, Is.EqualTo(PptxChartType.Line));
        Assert.That(editedData.GetSeries(2).ChartType, Is.EqualTo(PptxChartType.Column));
    }

    static PptxChartSeries CreateSeries(string name, string first, string second)
    {
        PptxChartSeries series = new PptxChartSeries();
        series.Name = name;
        series.Values.Add(first);
        series.Values.Add(second);
        return series;
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
