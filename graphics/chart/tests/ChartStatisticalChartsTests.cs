namespace chart.Tests;

[TestFixture]
public class ChartStatisticalChartsTests
{
    [Test]
    public void HistogramSeries_UsesHistogramBinsAsBars()
    {
        ChartHistogram histogram = ChartBinnerOperations.BinFixedCount(new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 }, 3);
        ChartCartesianSeries series = ChartStatisticalChartsOperations.CreateHistogramSeries("hist", histogram, ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0)));

        Assert.That(series.Kind, Is.EqualTo(ChartCartesianSeriesKind.Bar));
        Assert.That(series.CommandCount, Is.EqualTo(3));
        AssertPoint(series.CommandAt(0).Points[0], 0.0, 0.0);
        AssertPoint(series.CommandAt(0).Points[1], 0.0, 2.0);
        AssertPoint(series.CommandAt(0).Points[2], 5.0 / 3.0, 2.0);
        AssertPoint(series.CommandAt(2).Points[3], 5.0, 0.0);
    }

    [Test]
    public void DensitySeries_EstimatesGaussianKernelDensity()
    {
        ChartCartesianSeries series = ChartStatisticalChartsOperations.CreateDensitySeries(
            "density",
            new double[] { 0.0, 1.0 },
            3,
            1.0,
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        ChartPoint[] points = series.CommandAt(0).Points;

        Assert.That(points.Length, Is.EqualTo(3));
        AssertPoint(points[0], 0.0, ChartStatisticalChartsOperations.GaussianDensity(new double[] { 0.0, 1.0 }, 0.0, 1.0));
        Assert.That(points[1].X, Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(points[1].Y, Is.GreaterThan(points[0].Y));
        Assert.That(ChartStatisticalChartsOperations.GaussianKernel(0.0), Is.EqualTo(1.0 / System.Math.Sqrt(2.0 * System.Math.PI)).Within(0.0000001));
        Assert.That(ChartStatisticalChartsOperations.SilvermanBandwidth(new double[] { 2.0, 2.0 }), Is.GreaterThan(0.0));
    }

    [Test]
    public void EcdfSeries_CreatesRightContinuousStepPoints()
    {
        ChartCartesianSeries series = ChartStatisticalChartsOperations.CreateEcdfSeries("ecdf", new double[] { 2.0, 1.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        ChartPoint[] points = series.CommandAt(0).Points;

        Assert.That(points.Length, Is.EqualTo(5));
        AssertPoint(points[0], 1.0, 0.0);
        AssertPoint(points[1], 1.0, 0.0);
        AssertPoint(points[2], 1.0, 0.5);
        AssertPoint(points[3], 2.0, 0.5);
        AssertPoint(points[4], 2.0, 1.0);
    }

    [Test]
    public void BoxPlotSeries_ComputesQuartilesAndWhiskers()
    {
        ChartBoxPlotSummary summary = ChartStatisticalChartsOperations.CreateBoxPlotSummary(new double[] { 1.0, 2.0, 3.0, 4.0, 100.0 });
        ChartCartesianSeries series = ChartStatisticalChartsOperations.CreateBoxPlotSeries(
            "box",
            10.0,
            2.0,
            new double[] { 1.0, 2.0, 3.0, 4.0, 100.0 },
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(240, 240, 240)),
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        Assert.That(summary.Minimum, Is.EqualTo(1.0));
        Assert.That(summary.FirstQuartile, Is.EqualTo(2.0));
        Assert.That(summary.Median, Is.EqualTo(3.0));
        Assert.That(summary.ThirdQuartile, Is.EqualTo(4.0));
        Assert.That(summary.Maximum, Is.EqualTo(100.0));
        Assert.That(summary.WhiskerMinimum, Is.EqualTo(1.0));
        Assert.That(summary.WhiskerMaximum, Is.EqualTo(4.0));
        Assert.That(series.CommandCount, Is.EqualTo(5));
        AssertPoint(series.CommandAt(0).Points[0], 9.0, 2.0);
        AssertPoint(series.CommandAt(0).Points[2], 11.0, 4.0);
        AssertLine(series.CommandAt(1), 9.0, 3.0, 11.0, 3.0);
        AssertLine(series.CommandAt(2), 10.0, 1.0, 10.0, 4.0);
    }

    [Test]
    public void ViolinPlan_ScalesDensityIntoSymmetricPolygon()
    {
        ChartViolinPlan plan = ChartStatisticalChartsOperations.CreateViolinPlan(5.0, 2.0, new double[] { 0.0, 1.0, 2.0 }, 5, 1.0);
        ChartCartesianSeries series = ChartStatisticalChartsOperations.CreateViolinSeries("violin", 5.0, 2.0, new double[] { 0.0, 1.0, 2.0 }, 5, 1.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(150, 0, 150)));

        Assert.That(plan.DensityPoints.Length, Is.EqualTo(5));
        Assert.That(plan.PolygonPoints.Length, Is.EqualTo(10));
        Assert.That(ChartStatisticalChartsOperations.MaximumDensity(plan.DensityPoints), Is.GreaterThan(0.0));
        Assert.That(plan.PolygonPoints[0].Y, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(plan.PolygonPoints[4].Y, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(plan.PolygonPoints[0].X, Is.LessThan(5.0));
        Assert.That(plan.PolygonPoints[9].X, Is.GreaterThan(5.0));
        Assert.That(series.CommandAt(0).Points.Length, Is.EqualTo(10));
    }

    [Test]
    public void ErrorBarAndStemSeries_CreateLineCommands()
    {
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartCartesianSeries error = ChartStatisticalChartsOperations.CreateErrorBarSeries(
            "error",
            new double[] { 1.0 },
            new double[] { 5.0 },
            new double[] { 1.0 },
            new double[] { 2.0 },
            0.4,
            stroke);
        ChartCartesianSeries stem = ChartStatisticalChartsOperations.CreateStemSeries("stem", new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }, 0.0, stroke);

        Assert.That(error.CommandCount, Is.EqualTo(3));
        AssertLine(error.CommandAt(0), 1.0, 4.0, 1.0, 7.0);
        AssertLine(error.CommandAt(1), 0.8, 4.0, 1.2, 4.0);
        AssertLine(error.CommandAt(2), 0.8, 7.0, 1.2, 7.0);
        Assert.That(stem.CommandCount, Is.EqualTo(2));
        AssertLine(stem.CommandAt(1), 2.0, 0.0, 2.0, 4.0);
    }

    [Test]
    public void StatisticalCharts_RenderThroughCartesianRenderer()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 30.0, 30.0), new ChartRange(0.0, 3.0), new ChartRange(0.0, 3.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries histogram = ChartStatisticalChartsOperations.CreateHistogramSeries("hist", new double[] { 0.0, 0.2, 1.1, 2.5 }, 3, ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0)));
        ChartRenderScene scene = new ChartRenderScene(30, 30);

        scene.AddClear(ChartColor.White());
        renderer.AddSeries(histogram);
        renderer.RenderToScene(scene);

        Assert.That(scene.CommandCount, Is.EqualTo(4));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"hist-bin-0\""));
        AssertColor(scene.ToRawBitmap().GetPixel(2, 20), 255, 0, 0, 255);
    }

    [Test]
    public void StatisticalCharts_ValidateInputs()
    {
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticalChartsOperations.CreateHistogramSeries("hist", (ChartHistogram)null!, ChartRenderStyle.CreateFill(ChartColor.Black())));
        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticalChartsOperations.CreateHistogramSeries("hist", ChartBinnerOperations.BinFixedCount(new double[] { 1.0, 2.0 }, 1), null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.CreateDensitySeries("density", new double[] { 1.0, 2.0 }, 1, 1.0, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.CreateDensitySeries("density", new double[] { 1.0, 2.0 }, 2, 0.0, stroke));
        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticalChartsOperations.CreateEcdfSeries("ecdf", null!, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.CreateBoxPlotSeries("box", 1.0, 0.0, new double[] { 1.0 }, ChartRenderStyle.CreateFill(ChartColor.Black()), stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.CreateViolinSeries("violin", 1.0, 0.0, new double[] { 1.0, 2.0 }, 2, 1.0, ChartRenderStyle.CreateFill(ChartColor.Black())));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.CreateViolinPlan(1.0, 1.0, new double[] { 1.0, 2.0 }, int.MaxValue, 1.0));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticalChartsOperations.CreateErrorBarSeries("error", new double[] { 1.0 }, new double[] { 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 1.0 }, 0.2, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.CreateErrorBarSeries("error", new double[] { 1.0 }, new double[] { 1.0 }, new double[] { 1.0 }, new double[] { 1.0 }, 0.0, stroke));
        Assert.Throws<System.ArgumentNullException>(() => ChartStatisticalChartsOperations.CreateStemSeries("stem", null!, new double[] { 1.0 }, 0.0, stroke));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.DensityWidth(1.0, 0.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartStatisticalChartsOperations.ValidateMatchingValues(new double[] { 1.0 }, 0, "values"));
        Assert.Throws<System.ArgumentException>(() => ChartStatisticalChartsOperations.ValidateMatchingValues(new double[] { 1.0 }, 2, "values"));
        Assert.That(ChartStatisticalChartsOperations.WhiskerMinimum(new double[] { -100.0, 1.0, 2.0 }, 0.0), Is.EqualTo(1.0));
        Assert.That(ChartStatisticalChartsOperations.WhiskerMaximum(new double[] { 1.0, 2.0, 100.0 }, 10.0), Is.EqualTo(2.0));
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

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
