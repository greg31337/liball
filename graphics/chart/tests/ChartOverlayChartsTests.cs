namespace chart.Tests;

[TestFixture]
public class ChartOverlayChartsTests
{
    [Test]
    public void ReferenceLinesAndBands_CreateOverlaySeries()
    {
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(230, 230, 230));
        ChartCartesianSeries horizontal = ChartOverlayChartsOperations.CreateHorizontalReferenceLine("h", 0.0, 10.0, 3.0, line);
        ChartCartesianSeries vertical = ChartOverlayChartsOperations.CreateVerticalReferenceLine("v", 5.0, -1.0, 1.0, line);
        ChartCartesianSeries band = ChartOverlayChartsOperations.CreateHorizontalReferenceBand("band", 0.0, 10.0, 2.0, 4.0, fill);

        Assert.That(horizontal.Kind, Is.EqualTo(ChartCartesianSeriesKind.Line));
        AssertLine(horizontal.CommandAt(0), 0.0, 3.0, 10.0, 3.0);
        AssertLine(vertical.CommandAt(0), 5.0, -1.0, 5.0, 1.0);
        Assert.That(band.Kind, Is.EqualTo(ChartCartesianSeriesKind.Area));
        AssertPoint(band.CommandAt(0).Points[0], 0.0, 2.0);
        AssertPoint(band.CommandAt(0).Points[1], 0.0, 4.0);
        AssertPoint(band.CommandAt(0).Points[2], 10.0, 4.0);
        AssertPoint(band.CommandAt(0).Points[3], 10.0, 2.0);
    }

    [Test]
    public void ConfidenceMeanMedianAndQuantileBands_CreateExpectedGeometry()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 220, 255));
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartCartesianSeries confidence = ChartOverlayChartsOperations.CreateConfidenceBand(
            "confidence",
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 1.0, 2.0, 1.5 },
            new double[] { 3.0, 4.0, 3.5 },
            fill);
        ChartCartesianSeries mean = ChartOverlayChartsOperations.CreateMeanLine("mean", 0.0, 2.0, new double[] { 1.0, 3.0, 5.0 }, line);
        ChartCartesianSeries median = ChartOverlayChartsOperations.CreateMedianLine("median", 0.0, 2.0, new double[] { 1.0, 7.0, 3.0 }, line);
        ChartCartesianSeries quantile = ChartOverlayChartsOperations.CreateQuantileBand("quantile", 0.0, 2.0, new double[] { 0.0, 10.0, 20.0, 30.0, 40.0 }, 0.25, 0.75, fill);

        Assert.That(confidence.CommandAt(0).Points.Length, Is.EqualTo(6));
        AssertPoint(confidence.CommandAt(0).Points[0], 0.0, 3.0);
        AssertPoint(confidence.CommandAt(0).Points[2], 2.0, 3.5);
        AssertPoint(confidence.CommandAt(0).Points[3], 2.0, 1.5);
        AssertLine(mean.CommandAt(0), 0.0, 3.0, 2.0, 3.0);
        AssertLine(median.CommandAt(0), 0.0, 3.0, 2.0, 3.0);
        AssertPoint(quantile.CommandAt(0).Points[0], 0.0, 10.0);
        AssertPoint(quantile.CommandAt(0).Points[2], 2.0, 30.0);
        Assert.That(ChartOverlayChartsOperations.Quantile(new double[] { 0.0, 10.0, 20.0, 30.0, 40.0 }, 0.5), Is.EqualTo(20.0).Within(0.0000001));
    }

    [Test]
    public void StandardDeviationBand_UsesSuppliedCentersAndDeviations()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 220, 255));

        ChartCartesianSeries band = new ChartOverlayCharts().CreateStandardDeviationBand("deviation", new double[] { 0.0, 1.0 }, new double[] { 10.0, 20.0 }, new double[] { 1.0, 2.0 }, 2.0, fill);

        AssertPoint(band.CommandAt(0).Points[0], 0.0, 12.0);
        AssertPoint(band.CommandAt(0).Points[1], 1.0, 24.0);
        AssertPoint(band.CommandAt(0).Points[2], 1.0, 16.0);
        AssertPoint(band.CommandAt(0).Points[3], 0.0, 8.0);
    }

    [Test]
    public void Trendline_FitsLinearRegressionAndCreatesSeries()
    {
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartLinearTrendline trendline = ChartOverlayChartsOperations.FitLinearTrendline(new double[] { 0.0, 1.0, 2.0 }, new double[] { 1.0, 3.0, 5.0 });
        ChartCartesianSeries series = ChartOverlayChartsOperations.CreateTrendlineSeries("trend", new double[] { 0.0, 1.0, 2.0 }, new double[] { 1.0, 3.0, 5.0 }, 0.0, 2.0, line);

        Assert.That(trendline.Slope, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(trendline.Intercept, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(trendline.RSquared, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(trendline.Predict(3.0), Is.EqualTo(7.0).Within(0.0000001));
        AssertPoint(series.CommandAt(0).Points[0], 0.0, 1.0);
        AssertPoint(series.CommandAt(0).Points[1], 2.0, 5.0);
    }

    [Test]
    public void PolynomialTrendline_CreatesSamplesEquationsAndLabels()
    {
        ChartOverlayCharts charts = new ChartOverlayCharts();
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle text = new ChartRenderStyle();
        text.SetFill(ChartColor.Black());
        double[] xValues = new double[] { -2.0, -1.0, 0.0, 1.0, 2.0 };
        double[] yValues = new double[] { 15.0, 6.0, 1.0, 0.0, 3.0 };

        ChartPolynomialTrendline polynomial = charts.FitPolynomialTrendline(xValues, yValues, 2);
        ChartCartesianSeries fitted = charts.CreatePolynomialTrendlineSeries("polynomial", xValues, yValues, 2, -2.0, 2.0, 5, line);
        ChartLinearTrendline linear = charts.FitLinearTrendline(new double[] { 0.0, 1.0, 2.0 }, new double[] { 1.0, 3.0, 5.0 });
        string polynomialEquation = charts.FormatPolynomialTrendlineEquation(polynomial, "y", 2, true);
        string linearEquation = charts.FormatLinearTrendlineEquation(linear, "y", 1, true);
        ChartCartesianSeries polynomialLabel = charts.CreatePolynomialTrendlineLabel("polynomial-label", polynomial, "y", 2, true, 1.0, 2.0, text);
        ChartCartesianSeries linearLabel = charts.CreateLinearTrendlineLabel("linear-label", linear, "y", 1, false, 3.0, 4.0, text);

        Assert.That(polynomialEquation, Is.EqualTo("y = 2.00x^2 - 3.00x + 1.00; R^2 = 1.00"));
        Assert.That(linearEquation, Is.EqualTo("y = 2.0x + 1.0; R^2 = 1.0"));
        Assert.That(fitted.CommandAt(0).Points.Length, Is.EqualTo(5));
        AssertPoint(fitted.CommandAt(0).Points[0], -2.0, 15.0);
        AssertPoint(fitted.CommandAt(0).Points[2], 0.0, 1.0);
        AssertPoint(fitted.CommandAt(0).Points[4], 2.0, 3.0);
        Assert.That(polynomialLabel.CommandAt(0).Text, Is.EqualTo(polynomialEquation));
        Assert.That(polynomialLabel.CommandAt(0).X1, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(linearLabel.CommandAt(0).Text, Is.EqualTo("y = 2.0x + 1.0"));
    }

    [Test]
    public void ControlChart_CreatesObservedCenterAndControlLimitSeries()
    {
        ChartOverlayCharts charts = new ChartOverlayCharts();
        ChartRenderStyle dataStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle centerStyle = ChartRenderStyle.CreateStroke(ChartColor.FromRgb(0, 100, 0), 1.0);
        ChartRenderStyle limitStyle = ChartRenderStyle.CreateStroke(ChartColor.FromRgb(200, 0, 0), 1.0);

        ChartCartesianSeries[] series = charts.CreateControlChartSeries("control", new double[] { 0.0, 1.0, 2.0 }, new double[] { 8.0, 10.0, 12.0 }, 2.0, dataStyle, centerStyle, limitStyle);

        Assert.That(series.Length, Is.EqualTo(4));
        Assert.That(series[0].GetId(), Is.EqualTo("control-data"));
        Assert.That(series[1].GetId(), Is.EqualTo("control-center"));
        Assert.That(series[2].GetId(), Is.EqualTo("control-lower"));
        Assert.That(series[3].GetId(), Is.EqualTo("control-upper"));
        AssertLine(series[1].CommandAt(0), 0.0, 10.0, 2.0, 10.0);
        Assert.That(series[2].CommandAt(0).Y1, Is.EqualTo(10.0 - 2.0 * System.Math.Sqrt(8.0 / 3.0)).Within(0.0000001));
        Assert.That(series[3].CommandAt(0).Y1, Is.EqualTo(10.0 + 2.0 * System.Math.Sqrt(8.0 / 3.0)).Within(0.0000001));
        Assert.That(series[3].GetOrder(), Is.EqualTo(3));
    }

    [Test]
    public void OverlayCharts_ValidateInputs()
    {
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());

        Assert.Throws<System.ArgumentException>(() => ChartOverlayChartsOperations.CreateHorizontalReferenceLine("h", 1.0, 1.0, 0.0, line));
        Assert.Throws<System.ArgumentNullException>(() => ChartOverlayChartsOperations.CreateHorizontalReferenceLine("h", 0.0, 1.0, 0.0, null!));
        Assert.Throws<System.ArgumentException>(() => ChartOverlayChartsOperations.CreateVerticalReferenceLine("v", 0.0, 1.0, 1.0, line));
        Assert.Throws<System.ArgumentException>(() => ChartOverlayChartsOperations.CreateHorizontalReferenceBand("b", 0.0, 1.0, 2.0, 2.0, fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartOverlayChartsOperations.CreateConfidenceBand("c", null!, new double[] { 1.0, 2.0 }, new double[] { 2.0, 3.0 }, fill));
        Assert.Throws<System.ArgumentException>(() => ChartOverlayChartsOperations.CreateConfidenceBand("c", new double[] { 0.0, 1.0 }, new double[] { 3.0, 2.0 }, new double[] { 2.0, 3.0 }, fill));
        Assert.Throws<System.InvalidOperationException>(() => ChartOverlayChartsOperations.FitLinearTrendline(new double[] { 1.0, 1.0 }, new double[] { 2.0, 3.0 }));
        Assert.Throws<System.ArgumentException>(() => ChartOverlayChartsOperations.CreateTrendlineSeries("t", new double[] { 0.0 }, new double[] { 1.0, 2.0 }, 0.0, 1.0, line));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartOverlayChartsOperations.CreateQuantileBand("q", 0.0, 1.0, new double[] { 1.0, 2.0 }, -0.1, 0.9, fill));
        Assert.Throws<System.ArgumentException>(() => ChartOverlayChartsOperations.CreateQuantileBand("q", 0.0, 1.0, new double[] { 1.0, 2.0 }, 0.9, 0.1, fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartOverlayChartsOperations.Quantile(null!, 0.5));
        ChartOverlayCharts charts = new ChartOverlayCharts();
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateStandardDeviationBand("s", new double[] { 0.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0, -1.0 }, 1.0, fill));
        Assert.Throws<System.InvalidOperationException>(() => charts.FitPolynomialTrendline(new double[] { 1.0, 1.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 }, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreatePolynomialTrendlineSeries("p", new double[] { 0.0, 1.0, 2.0 }, new double[] { 0.0, 1.0, 4.0 }, 2, 0.0, 2.0, 1, line));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateControlChartSeries("control", new double[] { 0.0, 1.0 }, new double[] { 1.0, 2.0 }, 0.0, line, line, line));
        Assert.Throws<System.ArgumentNullException>(() => charts.FormatPolynomialTrendlineEquation(null!, "y", 2, true));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.FormatLinearTrendlineEquation(new ChartLinearTrendline(1.0, 0.0, 1.0), "y", 16, false));
    }

    static void AssertLine(ChartRenderCommand command, double x1, double y1, double x2, double y2)
    {
        Assert.That(command.X1, Is.EqualTo(x1).Within(0.0000001));
        Assert.That(command.Y1, Is.EqualTo(y1).Within(0.0000001));
        Assert.That(command.X2, Is.EqualTo(x2).Within(0.0000001));
        Assert.That(command.Y2, Is.EqualTo(y2).Within(0.0000001));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
