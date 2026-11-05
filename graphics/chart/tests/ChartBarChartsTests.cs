namespace chart.Tests;

[TestFixture]
public class ChartBarChartsTests
{
    [Test]
    public void VerticalAndHorizontalBars_CreateDataSpacePolygons()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0));
        ChartCartesianSeries vertical = ChartBarChartsOperations.CreateVerticalBarSeries("vertical", new double[] { 1.0, 2.0 }, new double[] { 3.0, -1.0 }, 0.8, 0.0, fill);
        ChartCartesianSeries horizontal = ChartBarChartsOperations.CreateHorizontalBarSeries("horizontal", new double[] { 1.0, 2.0 }, new double[] { 4.0, -2.0 }, 0.5, 0.0, fill);

        Assert.That(vertical.Kind, Is.EqualTo(ChartCartesianSeriesKind.Bar));
        Assert.That(vertical.CommandCount, Is.EqualTo(2));
        AssertPoint(vertical.CommandAt(0).Points[0], 0.6, 0.0);
        AssertPoint(vertical.CommandAt(0).Points[1], 0.6, 3.0);
        AssertPoint(vertical.CommandAt(0).Points[2], 1.4, 3.0);
        AssertPoint(vertical.CommandAt(1).Points[1], 1.6, -1.0);
        AssertPoint(horizontal.CommandAt(0).Points[0], 0.0, 0.75);
        AssertPoint(horizontal.CommandAt(0).Points[1], 4.0, 0.75);
        AssertPoint(horizontal.CommandAt(1).Points[2], -2.0, 2.25);
    }

    [Test]
    public void GroupedVerticalBars_ComputeOffsetsAndPaletteStyles()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 2.0, 3.0, 4.0 });
        ChartPalette palette = new ChartPalette(
            "two",
            new ChartColor[]
            {
                ChartColor.FromRgb(255, 0, 0),
                ChartColor.FromRgb(0, 0, 255)
            });

        ChartCartesianSeries[] series = ChartBarChartsOperations.CreateGroupedVerticalBarSeries("group", new double[] { 10.0, 20.0 }, values, 0.8, 0.1, 0.0, palette);

        Assert.That(series.Length, Is.EqualTo(2));
        Assert.That(ChartBarChartsOperations.GroupedBarWidth(2, 0.8, 0.1), Is.EqualTo(0.35).Within(0.0000001));
        Assert.That(ChartBarChartsOperations.GroupedBarOffset(0, 2, 0.8, 0.1), Is.EqualTo(-0.225).Within(0.0000001));
        Assert.That(ChartBarChartsOperations.GroupedBarOffset(1, 2, 0.8, 0.1), Is.EqualTo(0.225).Within(0.0000001));
        Assert.That(series[0].Order, Is.EqualTo(0));
        Assert.That(series[1].Order, Is.EqualTo(1));
        AssertColor(series[0].CommandAt(0).Style.FillColor, 255, 0, 0, 255);
        AssertColor(series[1].CommandAt(0).Style.FillColor, 0, 0, 255, 255);
        Assert.That((series[0].CommandAt(0).Points[0].X + series[0].CommandAt(0).Points[2].X) * 0.5, Is.EqualTo(9.775).Within(0.0000001));
        Assert.That((series[1].CommandAt(1).Points[0].X + series[1].CommandAt(1).Points[2].X) * 0.5, Is.EqualTo(20.225).Within(0.0000001));
    }

    [Test]
    public void GroupedHorizontalBars_ComputeOffsetsAndPaletteStyles()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 2.0, 3.0, 4.0, 6.0 });
        ChartPalette palette = new ChartPalettes().Categorical();

        ChartCartesianSeries[] series = new ChartBarCharts().CreateGroupedHorizontalBarSeries("group", new double[] { 10.0, 20.0 }, values, 2.0, 0.5, 0.0, palette);

        Assert.That(series.Length, Is.EqualTo(2));
        AssertPoint(series[0].CommandAt(0).Points[0], 0.0, 9.0);
        AssertPoint(series[0].CommandAt(0).Points[2], 2.0, 9.75);
        AssertPoint(series[1].CommandAt(0).Points[0], 0.0, 10.25);
        AssertPoint(series[1].CommandAt(0).Points[2], 3.0, 11.0);
        Assert.That(series[0].CommandAt(0).Style.FillColor.R, Is.Not.EqualTo(series[1].CommandAt(0).Style.FillColor.R));
    }

    [Test]
    public void StackedAndPercentStackedBars_UseStackerBaselines()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 2.0, 2.0, 2.0 });
        ChartPalette palette = ChartPalettesOperations.Categorical();

        ChartCartesianSeries[] stacked = ChartBarChartsOperations.CreateStackedVerticalBarSeries("stack", new double[] { 0.0, 1.0 }, values, 0.8, ChartStackMode.Zero, palette);
        ChartCartesianSeries[] percent = ChartBarChartsOperations.CreatePercentStackedVerticalBarSeries("percent", new double[] { 0.0, 1.0 }, values, 0.8, palette);

        Assert.That(stacked.Length, Is.EqualTo(2));
        AssertPoint(stacked[0].CommandAt(0).Points[1], -0.4, 1.0);
        AssertPoint(stacked[1].CommandAt(0).Points[0], -0.4, 1.0);
        AssertPoint(stacked[1].CommandAt(0).Points[1], -0.4, 3.0);
        AssertPoint(stacked[1].CommandAt(1).Points[0], 0.6, 2.0);
        AssertPoint(stacked[1].CommandAt(1).Points[1], 0.6, 4.0);
        AssertPoint(percent[0].CommandAt(0).Points[1], -0.4, 0.3333333333333333);
        AssertPoint(percent[1].CommandAt(0).Points[0], -0.4, 0.3333333333333333);
        AssertPoint(percent[1].CommandAt(0).Points[1], -0.4, 1.0);
    }

    [Test]
    public void HorizontalStackedBars_UseStackerBaselinesAndPercentTotals()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 2.0, 2.0, 2.0 });
        ChartBarCharts charts = new ChartBarCharts();

        ChartCartesianSeries[] stacked = charts.CreateStackedHorizontalBarSeries("stack", new double[] { 0.0, 1.0 }, values, 0.8, ChartStackMode.Zero, new ChartPalettes().Categorical());
        ChartCartesianSeries[] percent = charts.CreatePercentStackedHorizontalBarSeries("percent", new double[] { 0.0, 1.0 }, values, 0.8, new ChartPalettes().Categorical());

        AssertPoint(stacked[0].CommandAt(0).Points[1], 1.0, -0.4);
        AssertPoint(stacked[1].CommandAt(0).Points[0], 1.0, -0.4);
        AssertPoint(stacked[1].CommandAt(0).Points[1], 3.0, -0.4);
        AssertPoint(stacked[1].CommandAt(1).Points[0], 2.0, 0.6);
        AssertPoint(stacked[1].CommandAt(1).Points[1], 4.0, 0.6);
        AssertPoint(percent[1].CommandAt(0).Points[1], 1.0, -0.4);
    }

    [Test]
    public void FunnelBuilder_CreatesCenteredStagesAndScene()
    {
        ChartPalette palette = new ChartPalettes().Categorical();
        ChartBarCharts charts = new ChartBarCharts();

        ChartCartesianSeries series = charts.CreateFunnelSeries("funnel", new double[] { 100.0, 60.0, 30.0 }, 50.0, 10.0, 10.0, 2.0, 80.0, palette);
        ChartRenderScene scene = charts.CreateFunnelScene("funnel", 100, 50, new double[] { 100.0, 60.0, 30.0 }, 50.0, 10.0, 10.0, 2.0, 80.0, palette);

        Assert.That(series.CommandCount, Is.EqualTo(3));
        AssertPoint(series.CommandAt(0).Points[0], 10.0, 10.0);
        AssertPoint(series.CommandAt(0).Points[1], 90.0, 10.0);
        AssertPoint(series.CommandAt(0).Points[2], 74.0, 20.0);
        AssertPoint(series.CommandAt(0).Points[3], 26.0, 20.0);
        AssertPoint(series.CommandAt(1).Points[0], 26.0, 22.0);
        AssertPoint(series.CommandAt(1).Points[2], 62.0, 32.0);
        Assert.That(scene.CommandCount, Is.EqualTo(3));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"funnel-stage-2\""));
        Assert.Throws<System.ArgumentException>(() => charts.CreateFunnelSeries("bad", new double[] { 10.0, 20.0 }, 0.0, 0.0, 1.0, 0.0, 1.0, palette));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.CreateFunnelSeries("bad", new double[] { 10.0, 0.0 }, 0.0, 0.0, 1.0, 0.0, 1.0, palette));
    }

    [Test]
    public void RangeBarsLollipopsAndWaterfalls_CreateExpectedCommands()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 200, 0));
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle marker = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 255));
        ChartRenderStyle negative = ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0));
        ChartCartesianSeries range = ChartBarChartsOperations.CreateRangeBarSeries("range", new double[] { 1.0 }, new double[] { -1.0 }, new double[] { 3.0 }, 0.5, fill);
        ChartCartesianSeries lollipop = ChartBarChartsOperations.CreateLollipopSeries("lollipop", new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }, 0.0, ChartMarkerShape.Circle, 5.0, line, marker);
        ChartCartesianSeries waterfall = ChartBarChartsOperations.CreateWaterfallSeries("waterfall", new double[] { 1.0, 2.0, 3.0 }, new double[] { 3.0, -1.0, 2.0 }, 0.8, fill, negative, line);

        AssertPoint(range.CommandAt(0).Points[0], 0.75, -1.0);
        AssertPoint(range.CommandAt(0).Points[1], 0.75, 3.0);
        Assert.That(lollipop.Kind, Is.EqualTo(ChartCartesianSeriesKind.Mixed));
        Assert.That(lollipop.CommandCount, Is.EqualTo(4));
        AssertLine(lollipop.CommandAt(0), 1.0, 0.0, 1.0, 3.0);
        Assert.That(lollipop.CommandAt(1).MarkerShape, Is.EqualTo(ChartMarkerShape.Circle));
        Assert.That(waterfall.CommandCount, Is.EqualTo(5));
        AssertPoint(waterfall.CommandAt(0).Points[0], 0.6, 0.0);
        AssertPoint(waterfall.CommandAt(0).Points[1], 0.6, 3.0);
        AssertLine(waterfall.CommandAt(1), 1.4, 3.0, 1.6, 3.0);
        AssertColor(waterfall.CommandAt(2).Style.FillColor, 255, 0, 0, 255);
        AssertPoint(waterfall.CommandAt(2).Points[0], 1.6, 3.0);
        AssertPoint(waterfall.CommandAt(2).Points[1], 1.6, 2.0);
    }

    [Test]
    public void BarCharts_RenderThroughCartesianRenderer()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 30.0, 30.0), new ChartRange(0.0, 3.0), new ChartRange(0.0, 3.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries bars = ChartBarChartsOperations.CreateVerticalBarSeries("bars", new double[] { 1.0, 2.0 }, new double[] { 2.0, 1.0 }, 0.8, 0.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0)));
        ChartRenderScene scene = new ChartRenderScene(30, 30);

        scene.AddClear(ChartColor.White());
        renderer.AddSeries(bars);
        renderer.RenderToScene(scene);
        RawBitmap bitmap = scene.ToRawBitmap();
        string svg = scene.ToSvgString();

        Assert.That(scene.CommandCount, Is.EqualTo(3));
        Assert.That(svg, Does.Contain("id=\"bars-bar-0\""));
        AssertColor(bitmap.GetPixel(10, 15), 255, 0, 0, 255);
    }

    [Test]
    public void BarCharts_ValidateInputs()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, 2.0 });
        ChartCartesianSeries series = new ChartCartesianSeries("series", ChartCartesianSeriesKind.Bar);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBarChartsOperations.CreateVerticalBarSeries("bar", new double[] { 1.0 }, new double[] { 1.0 }, 0.0, 0.0, fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.CreateVerticalBarSeries("bar", null!, new double[] { 1.0 }, 1.0, 0.0, fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.CreateVerticalBarSeries("bar", new double[] { 1.0 }, new double[] { 1.0 }, 1.0, 0.0, null!));
        Assert.Throws<System.ArgumentException>(() => ChartBarChartsOperations.CreateHorizontalBarSeries("bar", new double[] { 1.0 }, new double[] { 1.0, 2.0 }, 1.0, 0.0, fill));
        Assert.Throws<System.ArgumentException>(() => ChartBarChartsOperations.CreateGroupedVerticalBarSeries("group", new double[] { 1.0 }, values, 0.2, 0.3, 0.0, ChartPalettesOperations.Categorical()));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.CreateGroupedVerticalBarSeries("group", new double[] { 1.0 }, values, 1.0, 0.0, 0.0, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.CreateStackedVerticalBarSeries("stack", new double[] { 1.0 }, null!, 1.0, ChartStackMode.Zero, ChartPalettesOperations.Categorical()));
        Assert.Throws<System.ArgumentException>(() => ChartBarChartsOperations.CreatePercentStackedVerticalBarSeries("percent", new double[] { 1.0 }, ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, -1.0 }), 1.0, ChartPalettesOperations.Categorical()));
        Assert.Throws<System.ArgumentException>(() => ChartBarChartsOperations.CreateRangeBarSeries("range", new double[] { 1.0 }, new double[] { 0.0 }, new double[] { 1.0, 2.0 }, 1.0, fill));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBarChartsOperations.CreateLollipopSeries("lollipop", new double[] { 1.0 }, new double[] { 1.0 }, 0.0, ChartMarkerShape.Circle, 0.0, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0), fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.CreateWaterfallSeries("waterfall", new double[] { 1.0 }, new double[] { 1.0 }, 1.0, null!, fill, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.AddVerticalBar(null!, "bar", 1.0, 0.0, 1.0, 1.0, fill));
        Assert.Throws<System.ArgumentNullException>(() => ChartBarChartsOperations.AddHorizontalBar(null!, "bar", 0.0, 1.0, 1.0, 1.0, fill));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBarChartsOperations.GroupedBarWidth(0, 1.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartBarChartsOperations.GroupedBarOffset(2, 2, 1.0, 0.0));
        Assert.That(series.CommandCount, Is.EqualTo(0));
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
