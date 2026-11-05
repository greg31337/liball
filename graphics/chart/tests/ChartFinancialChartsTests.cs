namespace chart.Tests;

[TestFixture]
public class ChartFinancialChartsTests
{
    [Test]
    public void CandlestickAndOhlc_CreateExpectedCommands()
    {
        ChartRenderStyle up = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 160, 0));
        ChartRenderStyle down = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));
        ChartRenderStyle wick = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        double[] xValues = new double[] { 1.0, 2.0 };
        double[] open = new double[] { 10.0, 12.0 };
        double[] high = new double[] { 13.0, 14.0 };
        double[] low = new double[] { 9.0, 10.0 };
        double[] close = new double[] { 12.0, 11.0 };

        ChartCartesianSeries candles = ChartFinancialChartsOperations.CreateCandlestickSeries("candle", xValues, open, high, low, close, 0.5, up, down, wick);
        ChartCartesianSeries ohlc = ChartFinancialChartsOperations.CreateOhlcSeries("ohlc", xValues, open, high, low, close, 0.5, wick, ChartRenderStyle.CreateStroke(ChartColor.FromRgb(200, 0, 0), 1.0));

        Assert.That(candles.Kind, Is.EqualTo(ChartCartesianSeriesKind.Mixed));
        Assert.That(candles.CommandCount, Is.EqualTo(4));
        AssertLine(candles.CommandAt(0), 1.0, 9.0, 1.0, 13.0);
        Assert.That(candles.CommandAt(1).Id, Is.EqualTo("candle-body-0"));
        AssertPoint(candles.CommandAt(1).Points[0], 0.75, 10.0);
        AssertPoint(candles.CommandAt(1).Points[1], 0.75, 12.0);
        AssertColor(candles.CommandAt(1).Style.FillColor, 0, 160, 0, 255);
        AssertColor(candles.CommandAt(3).Style.FillColor, 200, 0, 0, 255);
        Assert.That(ohlc.CommandCount, Is.EqualTo(6));
        AssertLine(ohlc.CommandAt(0), 1.0, 9.0, 1.0, 13.0);
        AssertLine(ohlc.CommandAt(1), 0.75, 10.0, 1.0, 10.0);
        AssertLine(ohlc.CommandAt(2), 1.0, 12.0, 1.25, 12.0);
        AssertColor(ohlc.CommandAt(3).Style.StrokeColor, 200, 0, 0, 255);
    }

    [Test]
    public void VolumeAndMovingAverage_CreateBarAndLineSeries()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(80, 80, 80));
        ChartRenderStyle up = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 160, 0));
        ChartRenderStyle down = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        double[] xValues = new double[] { 1.0, 2.0, 3.0, 4.0 };
        double[] values = new double[] { 2.0, 4.0, 6.0, 8.0 };

        ChartCartesianSeries volume = ChartFinancialChartsOperations.CreateVolumeSeries("volume", new double[] { 1.0, 2.0 }, new double[] { 100.0, 150.0 }, 0.8, 0.0, fill);
        ChartCartesianSeries coloredVolume = ChartFinancialChartsOperations.CreateVolumeSeries("colored", new double[] { 1.0, 2.0 }, new double[] { 10.0, 12.0 }, new double[] { 11.0, 10.0 }, new double[] { 100.0, 150.0 }, 0.8, 0.0, up, down);
        ChartCartesianSeries average = ChartFinancialChartsOperations.CreateMovingAverageSeries("ma", xValues, values, 2, line);
        double[] averages = ChartFinancialChartsOperations.MovingAverage(values, 2);
        double[] averageX = ChartFinancialChartsOperations.MovingAverageXValues(xValues, 2);

        Assert.That(volume.Kind, Is.EqualTo(ChartCartesianSeriesKind.Bar));
        Assert.That(volume.CommandCount, Is.EqualTo(2));
        AssertPoint(volume.CommandAt(0).Points[0], 0.6, 0.0);
        AssertPoint(volume.CommandAt(0).Points[1], 0.6, 100.0);
        AssertColor(coloredVolume.CommandAt(0).Style.FillColor, 0, 160, 0, 255);
        AssertColor(coloredVolume.CommandAt(1).Style.FillColor, 200, 0, 0, 255);
        Assert.That(averages, Is.EqualTo(new double[] { 3.0, 5.0, 7.0 }).Within(0.0000001));
        Assert.That(averageX, Is.EqualTo(new double[] { 2.0, 3.0, 4.0 }).Within(0.0000001));
        Assert.That(average.CommandCount, Is.EqualTo(1));
        AssertPoint(average.CommandAt(0).Points[0], 2.0, 3.0);
        AssertPoint(average.CommandAt(0).Points[2], 4.0, 7.0);
    }

    [Test]
    public void FinancialAxisFormatting_CreatesDateTicksAndLabels()
    {
        System.DateTime[] dates = new System.DateTime[]
        {
            new System.DateTime(2026, 1, 1),
            new System.DateTime(2026, 1, 2),
            new System.DateTime(2026, 1, 3),
            new System.DateTime(2026, 1, 4),
            new System.DateTime(2026, 1, 5)
        };

        ChartTick[] ticks = ChartFinancialChartsOperations.GenerateFinancialDateTicks(dates, 3, "MM-dd");
        double[] indexValues = ChartFinancialChartsOperations.DateIndexValues(dates);
        double[] tickValues = ChartFinancialChartsOperations.DateTickValues(dates);

        Assert.That(ticks.Length, Is.EqualTo(3));
        AssertTick(ticks[0], 0.0, "01-01", 0);
        AssertTick(ticks[1], 3.0, "01-04", 1);
        AssertTick(ticks[2], 4.0, "01-05", 2);
        Assert.That(indexValues, Is.EqualTo(new double[] { 0.0, 1.0, 2.0, 3.0, 4.0 }));
        Assert.That(tickValues[0], Is.EqualTo((double)dates[0].Ticks).Within(1.0));
        Assert.That(ChartFinancialChartsOperations.FormatPrice(123.456, 2, "$"), Is.EqualTo("$123.46"));
        Assert.That(ChartFinancialChartsOperations.FormatVolume(1500000.0), Is.EqualTo("1.5M"));
        Assert.That(ChartFinancialChartsOperations.FormatChange(1.234, 2), Is.EqualTo("+1.23"));
        Assert.That(ChartFinancialChartsOperations.FormatChange(-1.234, 2), Is.EqualTo("-1.23"));
        Assert.That(ChartFinancialChartsOperations.FormatPercentChange(0.1234, 1), Is.EqualTo("+12.3%"));
    }

    [Test]
    public void FinancialCharts_ValidateInputs()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        double[] xValues = new double[] { 1.0 };
        double[] values = new double[] { 10.0 };
        System.DateTime[] dates = new System.DateTime[] { new System.DateTime(2026, 1, 1) };

        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.CreateCandlestickSeries("", xValues, values, values, values, values, 0.5, fill, fill, stroke));
        Assert.Throws<System.ArgumentNullException>(() => ChartFinancialChartsOperations.CreateCandlestickSeries("c", null!, values, values, values, values, 0.5, fill, fill, stroke));
        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.CreateCandlestickSeries("c", xValues, new double[] { 10.0 }, new double[] { 9.0 }, new double[] { 8.0 }, new double[] { 10.0 }, 0.5, fill, fill, stroke));
        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.CreateCandlestickSeries("c", xValues, new double[] { 10.0 }, new double[] { 12.0 }, new double[] { 11.0 }, new double[] { 10.0 }, 0.5, fill, fill, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartFinancialChartsOperations.CreateCandlestickSeries("c", xValues, values, values, values, values, 0.0, fill, fill, stroke));
        Assert.Throws<System.ArgumentNullException>(() => ChartFinancialChartsOperations.CreateCandlestickSeries("c", xValues, values, values, values, values, 0.5, null!, fill, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartFinancialChartsOperations.CreateOhlcSeries("o", xValues, values, values, values, values, 0.0, stroke, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartFinancialChartsOperations.CreateVolumeSeries("v", xValues, new double[] { -1.0 }, 0.8, 0.0, fill));
        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.CreateVolumeSeries("v", xValues, new double[] { 1.0, 2.0 }, 0.8, 0.0, fill));
        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.CreateMovingAverageSeries("ma", xValues, values, 2, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartFinancialChartsOperations.MovingAverage(values, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartFinancialChartsOperations.GenerateFinancialDateTicks(null!, 1, "MM-dd"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartFinancialChartsOperations.GenerateFinancialDateTicks(dates, 0, "MM-dd"));
        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.GenerateFinancialDateTicks(dates, 1, ""));
        Assert.Throws<System.ArgumentNullException>(() => ChartFinancialChartsOperations.FormatPrice(1.0, 2, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartFinancialChartsOperations.FormatVolume(-1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartFinancialChartsOperations.DateIndexValues(null!));
        Assert.Throws<System.ArgumentException>(() => ChartFinancialChartsOperations.DateTickValues(new System.DateTime[0]));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertLine(ChartRenderCommand command, double x1, double y1, double x2, double y2)
    {
        Assert.That(command.X1, Is.EqualTo(x1).Within(0.0000001));
        Assert.That(command.Y1, Is.EqualTo(y1).Within(0.0000001));
        Assert.That(command.X2, Is.EqualTo(x2).Within(0.0000001));
        Assert.That(command.Y2, Is.EqualTo(y2).Within(0.0000001));
    }

    static void AssertTick(ChartTick tick, double value, string label, int index)
    {
        Assert.That(tick.Kind, Is.EqualTo(ChartTickKind.Explicit));
        Assert.That(tick.Value, Is.EqualTo(value).Within(0.0000001));
        Assert.That(tick.Label, Is.EqualTo(label));
        Assert.That(tick.Index, Is.EqualTo(index));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
