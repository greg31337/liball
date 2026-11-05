namespace chart.Tests;

[TestFixture]
public class ChartLineAreaChartsTests
{
    [Test]
    public void LineAndMultiLineSeries_CreateOrderedCartesianLineSeries()
    {
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 2.0);
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 1.0, 2.0 }, new double[] { 1.0, 3.0, 2.0 }, stroke);
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(3, 2, new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 1.0 });
        ChartPalette palette = new ChartPalette(
            "two",
            new ChartColor[]
            {
                ChartColor.FromRgb(255, 0, 0),
                ChartColor.FromRgb(0, 0, 255)
            });

        ChartCartesianSeries[] lines = ChartLineAreaChartsOperations.CreateMultiLineSeries("multi", new double[] { 0.0, 1.0, 2.0 }, matrix, palette, 1.5);

        Assert.That(line.Kind, Is.EqualTo(ChartCartesianSeriesKind.Line));
        Assert.That(line.CommandCount, Is.EqualTo(1));
        Assert.That(line.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Polyline));
        AssertPoint(line.CommandAt(0).Points[1], 1.0, 3.0);
        Assert.That(lines.Length, Is.EqualTo(2));
        Assert.That(lines[0].Order, Is.EqualTo(0));
        Assert.That(lines[1].Order, Is.EqualTo(1));
        AssertColor(lines[0].CommandAt(0).Style.StrokeColor, 255, 0, 0, 255);
        AssertColor(lines[1].CommandAt(0).Style.StrokeColor, 0, 0, 255, 255);
        AssertPoint(lines[1].CommandAt(0).Points[1], 1.0, 4.0);
    }

    [Test]
    public void StepLineSeries_ExpandsBeforeAfterAndMiddleSteps()
    {
        double[] xValues = new double[] { 0.0, 1.0, 2.0 };
        double[] yValues = new double[] { 0.0, 10.0, 0.0 };
        ChartPoint[] before = ChartLineAreaChartsOperations.CreateStepPoints(xValues, yValues, ChartLineInterpolation.StepBefore);
        ChartPoint[] after = ChartLineAreaChartsOperations.CreateStepPoints(xValues, yValues, ChartLineInterpolation.StepAfter);
        ChartPoint[] middle = ChartLineAreaChartsOperations.CreateStepPoints(xValues, yValues, ChartLineInterpolation.StepMiddle);

        Assert.That(before.Length, Is.EqualTo(5));
        AssertPoint(before[1], 0.0, 10.0);
        AssertPoint(before[2], 1.0, 10.0);
        AssertPoint(after[1], 1.0, 0.0);
        AssertPoint(after[2], 1.0, 10.0);
        Assert.That(middle.Length, Is.EqualTo(7));
        AssertPoint(middle[1], 0.5, 0.0);
        AssertPoint(middle[2], 0.5, 10.0);
        AssertPoint(middle[3], 1.0, 10.0);
    }

    [Test]
    public void SplineLineSeries_SamplesCatmullRomCurve()
    {
        ChartCartesianSeries spline = ChartLineAreaChartsOperations.CreateSplineLineSeries(
            "spline",
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 0.0, 2.0, 0.0 },
            2,
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        ChartPoint[] points = spline.CommandAt(0).Points;

        Assert.That(points.Length, Is.EqualTo(5));
        AssertPoint(points[0], 0.0, 0.0);
        AssertPoint(points[2], 1.0, 2.0);
        AssertPoint(points[4], 2.0, 0.0);
        Assert.That(points[1].Y, Is.GreaterThan(0.0));
        Assert.That(ChartLineAreaChartsOperations.CatmullRomValue(0.0, 1.0, 2.0, 3.0, 0.5), Is.EqualTo(1.5).Within(0.0000001));
    }

    [Test]
    public void AreaSeries_CreatesClosedBaselinePolygon()
    {
        ChartCartesianSeries area = ChartLineAreaChartsOperations.CreateAreaSeries(
            "area",
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 1.0, 2.0, 1.0 },
            0.0,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 200, 0)));
        ChartPoint[] points = area.CommandAt(0).Points;

        Assert.That(area.Kind, Is.EqualTo(ChartCartesianSeriesKind.Area));
        Assert.That(points.Length, Is.EqualTo(6));
        AssertPoint(points[0], 0.0, 1.0);
        AssertPoint(points[2], 2.0, 1.0);
        AssertPoint(points[3], 2.0, 0.0);
        AssertPoint(points[5], 0.0, 0.0);
    }

    [Test]
    public void StackedAreaAndStreamgraphSeries_UseStackerBaselines()
    {
        double[] xValues = new double[] { 0.0, 1.0, 2.0 };
        ChartDataMatrix values = ChartDataMatrix.FromValues(3, 2, new double[] { 1.0, 2.0, 2.0, 1.0, 1.0, 3.0 });
        ChartPalette palette = ChartPalettesOperations.Categorical();

        ChartCartesianSeries[] stacked = ChartLineAreaChartsOperations.CreateStackedAreaSeries("stack", xValues, values, ChartStackMode.Zero, palette);
        ChartCartesianSeries[] stream = ChartLineAreaChartsOperations.CreateStreamgraphSeries("stream", new double[] { 0.0, 1.0 }, ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 1.0, 2.0, 2.0 }), palette);

        Assert.That(stacked.Length, Is.EqualTo(2));
        AssertPoint(stacked[0].CommandAt(0).Points[0], 0.0, 1.0);
        AssertPoint(stacked[0].CommandAt(0).Points[3], 2.0, 0.0);
        AssertPoint(stacked[1].CommandAt(0).Points[0], 0.0, 3.0);
        AssertPoint(stacked[1].CommandAt(0).Points[3], 2.0, 1.0);
        Assert.That(stream.Length, Is.EqualTo(2));
        AssertPoint(stream[0].CommandAt(0).Points[0], 0.0, 0.0);
        AssertPoint(stream[0].CommandAt(0).Points[2], 1.0, -2.0);
        AssertPoint(stream[1].CommandAt(0).Points[0], 0.0, 1.0);
        AssertPoint(stream[1].CommandAt(0).Points[2], 1.0, 0.0);
    }

    [Test]
    public void LineAreaSeries_RenderThroughCartesianRenderer()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 20.0, 20.0), new ChartRange(0.0, 2.0), new ChartRange(0.0, 2.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 1.0, 2.0 }, new double[] { 0.0, 2.0, 0.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        ChartCartesianSeries area = ChartLineAreaChartsOperations.CreateAreaSeries("area", new double[] { 0.0, 1.0, 2.0 }, new double[] { 0.5, 1.0, 0.5 }, 0.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0)));

        area.SetOrder(0);
        line.SetOrder(1);
        renderer.AddSeries(line);
        renderer.AddSeries(area);
        ChartRenderScene scene = renderer.Render(20, 20);
        string svg = scene.ToSvgString();

        Assert.That(scene.CommandCount, Is.EqualTo(3));
        Assert.That(svg.IndexOf("id=\"area-area\""), Is.LessThan(svg.IndexOf("id=\"line-line-segment-0\"")));
    }

    [Test]
    public void LineAreaCharts_ValidateInputs()
    {
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(2, 1, new double[] { 1.0, 2.0 });

        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.ValidateInterpolation((ChartLineInterpolation)99));
        Assert.Throws<System.ArgumentNullException>(() => ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, null!));
        Assert.Throws<System.ArgumentException>(() => ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0 }, new double[] { 0.0 }, style));
        Assert.Throws<System.ArgumentException>(() => ChartLineAreaChartsOperations.CreateStepLineSeries("step", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, ChartLineInterpolation.Linear, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.CreateSplineLineSeries("spline", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.CreateSplineLineSeries("spline", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, int.MaxValue, style));
        Assert.Throws<System.ArgumentException>(() => ChartLineAreaChartsOperations.CreateAreaSeries("area", new double[] { 0.0, 1.0 }, new double[] { 1.0, 2.0 }, new double[] { 0.0 }, ChartRenderStyle.CreateFill(ChartColor.Black())));
        Assert.Throws<System.ArgumentNullException>(() => ChartLineAreaChartsOperations.CreateMultiLineSeries("multi", new double[] { 0.0, 1.0 }, null!, ChartPalettesOperations.Categorical(), 1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartLineAreaChartsOperations.CreateMultiLineSeries("multi", new double[] { 0.0, 1.0 }, matrix, null!, 1.0));
        Assert.Throws<System.ArgumentException>(() => ChartLineAreaChartsOperations.CreateMultiLineSeries("multi", new double[] { 0.0 }, matrix, ChartPalettesOperations.Categorical(), 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.CreateMultiLineSeries("multi", new double[] { 0.0, 1.0 }, matrix, ChartPalettesOperations.Categorical(), 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.CatmullRomPoint(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 2, 0.5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.CatmullRomPoint(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 0, 2.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.ColumnValues(matrix, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.ConstantValues(0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLineAreaChartsOperations.ClampIndex(0, 0));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
