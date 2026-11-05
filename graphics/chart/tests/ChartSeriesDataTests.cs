namespace chart.Tests;

[TestFixture]
public class ChartSeriesDataTests
{
    [Test]
    public void SeriesData_StoresEveryChannelAndConvertsToTypedTable()
    {
        ChartSeriesData data = CreateCompleteData();

        ChartDataTable table = data.ToDataTable();

        Assert.That(data.GetRowCount(), Is.EqualTo(3));
        Assert.That(table.GetRowCount(), Is.EqualTo(3));
        Assert.That(table.GetColumnCount(), Is.EqualTo(11));
        Assert.That(table.GetColumnAt(0).GetName(), Is.EqualTo("x"));
        Assert.That(table.GetColumnAt(8).GetName(), Is.EqualTo("color"));
        Assert.That(table.GetColumnAt(9).GetKind(), Is.EqualTo(ChartDataKind.Text));
        Assert.That(table.GetColumnAt(10).GetKind(), Is.EqualTo(ChartDataKind.Category));
        Assert.That(table.GetNumber("z", 1), Is.EqualTo(200.0));
        Assert.That(table.GetNumber("close", 2), Is.EqualTo(14.0));
        Assert.That(table.GetText("label", 0), Is.EqualTo("first"));
        Assert.That(table.GetCategory("group", 2), Is.EqualTo("south"));
        Assert.That(table.GetColumn("group").CopyCategoryOrder(), Is.EqualTo(new string[] { "north", "south" }));
        Assert.That(data.Validate().IsValid(), Is.True);
    }

    [Test]
    public void SeriesData_DefensivelyCopiesSourcesResultsAndCopies()
    {
        double[] xValues = new double[] { 1.0, 2.0 };
        string[] labels = new string[] { "A", "B" };
        ChartSeriesData data = new ChartSeriesData(2);
        data.SetXValues(xValues);
        data.SetLabelValues(labels);
        xValues[0] = 99.0;
        labels[0] = "changed";
        double[] returnedX = data.CopyXValues();
        string?[] returnedLabels = data.CopyLabelValues();
        returnedX[1] = 88.0;
        returnedLabels[1] = "changed";
        ChartSeriesData copy = data.Copy();
        copy.SetXValues(new double[] { 7.0, 8.0 });

        Assert.That(data.CopyXValues(), Is.EqualTo(new double[] { 1.0, 2.0 }));
        Assert.That(data.CopyLabelValues(), Is.EqualTo(new string[] { "A", "B" }));
        Assert.That(copy.CopyXValues(), Is.EqualTo(new double[] { 7.0, 8.0 }));
        Assert.That(data.CopyYValues(), Is.Empty);
        ChartDataTable table = data.ToDataTable();
        table.GetColumn("x").SetNumberValues(new double[] { 20.0, 30.0 });
        Assert.That(data.CopyXValues(), Is.EqualTo(new double[] { 1.0, 2.0 }));
    }

    [Test]
    public void SeriesData_DelegatesToExistingSeriesBuilders()
    {
        ChartSeriesData data = new ChartSeriesData(2);
        data.SetXValues(new double[] { 1.0, 2.0 });
        data.SetYValues(new double[] { 4.0, 6.0 });
        data.SetSizeValues(new double[] { 10.0, 30.0 });
        data.SetOpenValues(new double[] { 10.0, 12.0 });
        data.SetHighValues(new double[] { 13.0, 15.0 });
        data.SetLowValues(new double[] { 9.0, 11.0 });
        data.SetCloseValues(new double[] { 12.0, 14.0 });
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(20, 40, 60));
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartCartesianSeries line = data.CreateLineSeries("line", stroke);
        ChartCartesianSeries scatter = data.CreateScatterSeries("scatter", ChartMarkerShape.Diamond, 7.0, fill);
        ChartCartesianSeries bubble = data.CreateBubbleSeries("bubble", 4.0, 12.0, fill);
        ChartCartesianSeries range = data.CreateRangeBarSeries("range", 0.5, fill);
        ChartCartesianSeries candles = data.CreateCandlestickSeries("candles", 0.5, fill, fill, stroke);

        Assert.That(line.GetKind(), Is.EqualTo(ChartCartesianSeriesKind.Line));
        Assert.That(line.CommandAt(0).GetPoints()[1].GetY(), Is.EqualTo(6.0));
        Assert.That(scatter.GetCommandCount(), Is.EqualTo(2));
        Assert.That(scatter.CommandAt(0).GetMarkerShape(), Is.EqualTo(ChartMarkerShape.Diamond));
        Assert.That(bubble.CommandAt(0).GetMarkerSize(), Is.EqualTo(4.0).Within(0.0000001));
        Assert.That(bubble.CommandAt(1).GetMarkerSize(), Is.EqualTo(12.0).Within(0.0000001));
        Assert.That(range.GetCommandCount(), Is.EqualTo(2));
        Assert.That(range.CommandAt(0).GetPoints()[1].GetY(), Is.EqualTo(13.0));
        Assert.That(candles.GetCommandCount(), Is.EqualTo(4));
        Assert.That(candles.CommandAt(0).GetY1(), Is.EqualTo(9.0));
        Assert.That(candles.CommandAt(0).GetY2(), Is.EqualTo(13.0));
    }

    [Test]
    public void SeriesData_ValidatesShapesValuesAndRequiredBuilderChannels()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartSeriesData(0));
        ChartSeriesData data = new ChartSeriesData(2);
        Assert.Throws<System.ArgumentNullException>(() => data.SetXValues(null!));
        Assert.Throws<System.ArgumentException>(() => data.SetYValues(new double[] { 1.0 }));
        Assert.Throws<System.ArgumentException>(() => data.SetLabelValues(new string[] { "only" }));
        Assert.Throws<System.InvalidOperationException>(() => data.CreateLineSeries("line", ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
        Assert.That(data.Validate().HasCode("EmptySeries"), Is.True);

        data.SetXValues(new double[] { 1.0, 2.0 });
        data.SetYValues(new double[] { double.NaN, double.PositiveInfinity });
        data.SetGroupValues(new string?[] { "group", null });
        ChartValidationResult validation = data.Validate();
        Assert.That(validation.HasCode("NaN"), Is.True);
        Assert.That(validation.HasCode("Infinity"), Is.True);
        Assert.That(validation.HasCode("MissingCategory"), Is.True);

        ChartSeriesData invalidFinancial = new ChartSeriesData(1);
        invalidFinancial.SetXValues(new double[] { 1.0 });
        invalidFinancial.SetOpenValues(new double[] { 10.0 });
        invalidFinancial.SetHighValues(new double[] { 9.0 });
        invalidFinancial.SetLowValues(new double[] { 8.0 });
        invalidFinancial.SetCloseValues(new double[] { 11.0 });
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());
        Assert.Throws<System.ArgumentException>(() => invalidFinancial.CreateCandlestickSeries("bad", 0.5, style, style, style));
    }

    static ChartSeriesData CreateCompleteData()
    {
        ChartSeriesData data = new ChartSeriesData(3);
        data.SetXValues(new double[] { 1.0, 2.0, 3.0 });
        data.SetYValues(new double[] { 10.0, 20.0, 30.0 });
        data.SetZValues(new double[] { 100.0, 200.0, 300.0 });
        data.SetOpenValues(new double[] { 10.0, 11.0, 12.0 });
        data.SetHighValues(new double[] { 13.0, 14.0, 15.0 });
        data.SetLowValues(new double[] { 9.0, 10.0, 11.0 });
        data.SetCloseValues(new double[] { 12.0, 13.0, 14.0 });
        data.SetSizeValues(new double[] { 4.0, 9.0, 16.0 });
        data.SetColorValues(new double[] { 0.1, 0.5, 0.9 });
        data.SetLabelValues(new string[] { "first", "second", "third" });
        data.SetGroupValues(new string[] { "north", "north", "south" });
        return data;
    }
}
