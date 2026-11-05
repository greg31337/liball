namespace chart.Tests;

[TestFixture]
public class ChartScatterChartsTests
{
    [Test]
    public void ScatterSeries_CreatesOneMarkerPerPoint()
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0));
        ChartCartesianSeries series = ChartScatterChartsOperations.CreateScatterSeries(
            "scatter",
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 4.0, 5.0, 6.0 },
            ChartMarkerShape.Diamond,
            7.0,
            style);

        style.FillColor.R = 0;

        Assert.That(series.Kind, Is.EqualTo(ChartCartesianSeriesKind.Scatter));
        Assert.That(series.CommandCount, Is.EqualTo(3));
        Assert.That(series.CommandAt(1).Id, Is.EqualTo("scatter-point-1"));
        Assert.That(series.CommandAt(1).MarkerShape, Is.EqualTo(ChartMarkerShape.Diamond));
        Assert.That(series.CommandAt(1).MarkerSize, Is.EqualTo(7.0));
        Assert.That(series.CommandAt(1).X1, Is.EqualTo(2.0));
        Assert.That(series.CommandAt(1).Y1, Is.EqualTo(5.0));
        AssertColor(series.CommandAt(1).Style.FillColor, 255, 0, 0, 255);
    }

    [Test]
    public void BubbleSeries_MapsSizeValuesAreaCorrectly()
    {
        ChartCartesianSeries series = ChartScatterChartsOperations.CreateBubbleSeries(
            "bubble",
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 10.0, 20.0, 30.0 },
            4.0,
            12.0,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 255)));

        Assert.That(series.CommandCount, Is.EqualTo(3));
        Assert.That(series.CommandAt(0).MarkerSize, Is.EqualTo(4.0).Within(0.0000001));
        Assert.That(series.CommandAt(1).MarkerSize, Is.EqualTo(System.Math.Sqrt(80.0)).Within(0.0000001));
        Assert.That(series.CommandAt(2).MarkerSize, Is.EqualTo(12.0).Within(0.0000001));
        Assert.That(ChartScatterChartsOperations.MapBubbleSize(1.0, 1.0, 1.0, 2.0, 6.0), Is.EqualTo(4.0));
        Assert.That(ChartScatterChartsOperations.MapBubbleSize(-1.0, 0.0, 10.0, 2.0, 6.0), Is.EqualTo(2.0));
        Assert.That(ChartScatterChartsOperations.MapBubbleSize(11.0, 0.0, 10.0, 2.0, 6.0), Is.EqualTo(6.0));
    }

    [Test]
    public void RugSeries_CreatesHorizontalAndVerticalTicks()
    {
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartCartesianSeries horizontal = ChartScatterChartsOperations.CreateRugSeries("x-rug", new double[] { 1.0, 2.0 }, -1.0, 0.25, ChartOrientation.Horizontal, stroke);
        ChartCartesianSeries vertical = ChartScatterChartsOperations.CreateRugSeries("y-rug", new double[] { 3.0, 4.0 }, 10.0, -0.5, ChartOrientation.Vertical, stroke);

        Assert.That(horizontal.CommandCount, Is.EqualTo(2));
        Assert.That(horizontal.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        AssertLine(horizontal.CommandAt(0), 1.0, -1.0, 1.0, -0.75);
        AssertLine(vertical.CommandAt(1), 10.0, 4.0, 9.5, 4.0);
    }

    [Test]
    public void MarginalScatterSeries_ComposesScatterAndTwoRugSeries()
    {
        ChartCartesianSeries[] series = ChartScatterChartsOperations.CreateMarginalScatterSeries(
            "marginal",
            new double[] { 1.0, 2.0 },
            new double[] { 3.0, 4.0 },
            ChartMarkerShape.Circle,
            5.0,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0)),
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0),
            -0.5,
            -1.0,
            0.25);

        Assert.That(series.Length, Is.EqualTo(3));
        Assert.That(series[0].Id, Is.EqualTo("marginal-scatter"));
        Assert.That(series[1].Id, Is.EqualTo("marginal-x-rug"));
        Assert.That(series[2].Id, Is.EqualTo("marginal-y-rug"));
        Assert.That(series[0].Order, Is.EqualTo(1));
        Assert.That(series[1].Order, Is.EqualTo(0));
        Assert.That(series[2].Order, Is.EqualTo(0));
        AssertLine(series[1].CommandAt(0), 1.0, -0.5, 1.0, -0.25);
        AssertLine(series[2].CommandAt(1), -1.0, 4.0, -0.75, 4.0);
    }

    [Test]
    public void ScatterHelpers_RenderThroughCartesianRenderer()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 20.0, 20.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries scatter = ChartScatterChartsOperations.CreateScatterSeries("scatter", new double[] { 5.0, 12.0 }, new double[] { 5.0, 5.0 }, ChartMarkerShape.Square, 4.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 255)));
        ChartCartesianSeries rug = ChartScatterChartsOperations.CreateRugSeries("rug", new double[] { 1.0, 5.0 }, 0.0, 1.0, ChartOrientation.Horizontal, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        ChartRenderScene scene = new ChartRenderScene(20, 20);

        scene.AddClear(ChartColor.White());
        renderer.AddSeries(scatter);
        renderer.AddSeries(rug);
        renderer.RenderToScene(scene);
        RawBitmap bitmap = scene.ToRawBitmap();
        string svg = scene.ToSvgString();

        Assert.That(scene.CommandCount, Is.EqualTo(4));
        Assert.That(svg, Does.Contain("id=\"scatter-point-0\""));
        Assert.That(svg, Does.Not.Contain("id=\"scatter-point-1\""));
        AssertColor(bitmap.GetPixel(10, 10), 0, 0, 255, 255);
    }

    [Test]
    public void ScatterCharts_ValidateInputs()
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());

        Assert.Throws<System.ArgumentException>(() => ChartScatterChartsOperations.CreateScatterSeries("", new double[] { 1.0 }, new double[] { 1.0 }, ChartMarkerShape.Circle, 4.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScatterChartsOperations.CreateScatterSeries("scatter", new double[] { 1.0 }, new double[] { 1.0 }, (ChartMarkerShape)99, 4.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScatterChartsOperations.CreateScatterSeries("scatter", new double[] { 1.0 }, new double[] { 1.0 }, ChartMarkerShape.Circle, 0.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartScatterChartsOperations.CreateScatterSeries("scatter", null!, new double[] { 1.0 }, ChartMarkerShape.Circle, 4.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartScatterChartsOperations.CreateScatterSeries("scatter", new double[] { 1.0 }, new double[] { 1.0 }, ChartMarkerShape.Circle, 4.0, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartScatterChartsOperations.CreateBubbleSeries("bubble", new double[] { 1.0 }, new double[] { 1.0 }, null!, 1.0, 2.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartScatterChartsOperations.CreateBubbleSeries("bubble", new double[] { 1.0 }, new double[] { 1.0 }, new double[] { 1.0, 2.0 }, 1.0, 2.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartScatterChartsOperations.CreateBubbleSeries("bubble", new double[] { 1.0 }, new double[] { 1.0 }, new double[] { 1.0 }, 3.0, 2.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScatterChartsOperations.ValidateSizeValues(new double[] { 1.0 }, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScatterChartsOperations.CreateRugSeries("rug", new double[] { 1.0 }, 0.0, 0.0, ChartOrientation.Horizontal, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
        Assert.Throws<System.ArgumentException>(() => ChartScatterChartsOperations.CreateRugSeries("rug", new double[] { 1.0 }, 0.0, 1.0, ChartOrientation.Radial, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartScatterChartsOperations.ValidateValues(null!, 1, "values"));
        Assert.Throws<System.ArgumentException>(() => ChartScatterChartsOperations.ValidateValues(new double[0], 1, "values"));
        Assert.Throws<System.ArgumentNullException>(() => ChartScatterChartsOperations.CreateMarginalScatterSeries("m", new double[] { 1.0 }, new double[] { 1.0 }, ChartMarkerShape.Circle, 4.0, null!, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0), 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScatterChartsOperations.MapBubbleSize(1.0, 0.0, 1.0, 0.0, 2.0));
        Assert.That(ChartScatterChartsOperations.Minimum(new double[] { 3.0, 1.0, 2.0 }), Is.EqualTo(1.0));
        Assert.That(ChartScatterChartsOperations.Maximum(new double[] { 3.0, 1.0, 2.0 }), Is.EqualTo(3.0));
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
