namespace chart.Tests;

[TestFixture]
public class ChartCompactChartsTests
{
    [Test]
    public void SlopeAndBumpCharts_CreateLineSeries()
    {
        ChartPalette palette = CreatePalette();
        ChartCartesianSeries slope = ChartCompactChartsOperations.CreateSlopeSeries(
            "slope",
            new string[] { "A", "B" },
            new double[] { 1.0, 3.0 },
            new double[] { 2.0, 1.0 },
            0.0,
            1.0,
            palette,
            1.5);
        ChartCartesianSeries[] bump = ChartCompactChartsOperations.CreateBumpSeries(
            "bump",
            new string[] { "A", "B" },
            new double[] { 0.0, 1.0, 2.0 },
            ChartDataMatrix.FromValues(3, 2, new double[] { 1.0, 2.0, 2.0, 1.0, 1.0, 2.0 }),
            palette,
            2.0);

        Assert.That(slope.CommandCount, Is.EqualTo(2));
        AssertLine(slope.CommandAt(0), 0.0, 1.0, 1.0, 2.0);
        AssertLine(slope.CommandAt(1), 0.0, 3.0, 1.0, 1.0);
        AssertColor(slope.CommandAt(0).Style.StrokeColor, 10, 0, 0, 255);
        Assert.That(bump.Length, Is.EqualTo(2));
        Assert.That(bump[0].Order, Is.EqualTo(0));
        AssertPoint(bump[0].CommandAt(0).Points[0], 0.0, 1.0);
        AssertPoint(bump[0].CommandAt(0).Points[1], 1.0, 2.0);
        AssertPoint(bump[1].CommandAt(0).Points[1], 1.0, 1.0);
    }

    [Test]
    public void DumbbellChart_CreatesConnectorsAndEndpointMarkers()
    {
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle start = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 100, 200));
        ChartRenderStyle end = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 80, 0));
        ChartCartesianSeries dumbbell = ChartCompactChartsOperations.CreateDumbbellSeries(
            "dumb",
            new double[] { 0.0, 1.0 },
            new double[] { 2.0, 3.0 },
            new double[] { 4.0, 1.0 },
            6.0,
            line,
            start,
            end);

        Assert.That(dumbbell.Kind, Is.EqualTo(ChartCartesianSeriesKind.Mixed));
        Assert.That(dumbbell.CommandCount, Is.EqualTo(6));
        AssertLine(dumbbell.CommandAt(0), 2.0, 0.0, 4.0, 0.0);
        Assert.That(dumbbell.CommandAt(1).MarkerShape, Is.EqualTo(ChartMarkerShape.Circle));
        Assert.That(dumbbell.CommandAt(1).MarkerSize, Is.EqualTo(6.0).Within(0.0000001));
        AssertPoint(new ChartPoint(dumbbell.CommandAt(2).X1, dumbbell.CommandAt(2).Y1), 4.0, 0.0);
        AssertLine(dumbbell.CommandAt(3), 3.0, 1.0, 1.0, 1.0);
    }

    [Test]
    public void TableLayoutAndScene_CreateHeaderAndBodyCells()
    {
        ChartTableLayout layout = ChartCompactChartsOperations.LayoutTable(
            new string[] { "Name", "Value" },
            new string[] { "A", "10", "B", "20" },
            2,
            2,
            new ChartRect(0.0, 0.0, 100.0, 60.0),
            20.0);
        ChartRenderScene scene = new ChartRenderScene(120, 80);

        ChartCompactChartsOperations.AddTableToScene(scene, "table", layout, ChartRenderStyle.CreateFill(ChartColor.White()), ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(layout.CellCount, Is.EqualTo(6));
        Assert.That(layout.CellAt(0).Header, Is.True);
        Assert.That(layout.CellAt(0).Text, Is.EqualTo("Name"));
        AssertRect(layout.CellAt(0).Bounds, 0.0, 0.0, 50.0, 20.0);
        AssertRect(layout.CellAt(2).Bounds, 0.0, 20.0, 50.0, 20.0);
        AssertRect(layout.CellAt(5).Bounds, 50.0, 40.0, 50.0, 20.0);
        Assert.That(scene.CommandCount, Is.EqualTo(12));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("table-cell-0"));
        Assert.That(scene.CommandAt(1).Id, Is.EqualTo("table-text-0"));
        Assert.That(scene.CommandAt(11).Text, Is.EqualTo("20"));
    }

    [Test]
    public void SparklineLayout_MapsValuesIntoCompactBounds()
    {
        ChartSparklineLayout layout = ChartCompactChartsOperations.LayoutSparkline(new double[] { 2.0, 4.0, 3.0 }, new ChartRect(0.0, 0.0, 20.0, 10.0));
        ChartSparklineLayout constant = ChartCompactChartsOperations.LayoutSparkline(new double[] { 5.0, 5.0 }, new ChartRect(0.0, 0.0, 10.0, 10.0));
        ChartRenderScene scene = ChartCompactChartsOperations.CreateSparklineScene("spark", 20, 10, new double[] { 2.0, 4.0, 3.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        Assert.That(layout.Minimum, Is.EqualTo(2.0).Within(0.0000001));
        Assert.That(layout.Maximum, Is.EqualTo(4.0).Within(0.0000001));
        AssertPoint(layout.Points[0], 0.0, 10.0);
        AssertPoint(layout.Points[1], 10.0, 0.0);
        AssertPoint(layout.Points[2], 20.0, 5.0);
        AssertPoint(constant.Points[0], 0.0, 5.0);
        AssertPoint(constant.Points[1], 10.0, 5.0);
        Assert.That(scene.CommandCount, Is.EqualTo(1));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("spark-sparkline"));
    }

    [Test]
    public void CompactCharts_ValidateInputs()
    {
        ChartPalette palette = CreatePalette();
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderScene scene = new ChartRenderScene(20, 20);
        ChartTableLayout table = ChartCompactChartsOperations.LayoutTable(new string[] { "A" }, new string[] { "1" }, 1, 1, new ChartRect(0.0, 0.0, 10.0, 10.0), 2.0);
        ChartSparklineLayout sparkline = ChartCompactChartsOperations.LayoutSparkline(new double[] { 1.0, 2.0 }, new ChartRect(0.0, 0.0, 10.0, 10.0));

        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.CreateSlopeSeries("slope", null!, new double[] { 1.0 }, new double[] { 2.0 }, 0.0, 1.0, palette, 1.0));
        Assert.Throws<System.ArgumentException>(() => ChartCompactChartsOperations.CreateSlopeSeries("slope", new string[] { "A" }, new double[] { 1.0 }, new double[] { 2.0 }, 0.0, 0.0, palette, 1.0));
        Assert.Throws<System.ArgumentException>(() => ChartCompactChartsOperations.CreateSlopeSeries("slope", new string[] { "A" }, new double[] { 1.0, 2.0 }, new double[] { 2.0, 3.0 }, 0.0, 1.0, palette, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.CreateSlopeSeries("slope", new string[] { "A" }, new double[] { 1.0 }, new double[] { 2.0 }, 0.0, 1.0, null!, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartCompactChartsOperations.CreateDumbbellSeries("dumb", new double[] { 0.0 }, new double[] { 1.0 }, new double[] { 2.0 }, 0.0, style, style, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.CreateDumbbellSeries("dumb", null!, new double[] { 1.0 }, new double[] { 2.0 }, 1.0, style, style, style));
        Assert.Throws<System.ArgumentException>(() => ChartCompactChartsOperations.CreateBumpSeries("bump", new string[] { "A" }, new double[] { 0.0, 1.0 }, ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 2.0, 2.0, 1.0 }), palette, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.LayoutTable(null!, new string[] { "1" }, 1, 1, new ChartRect(0.0, 0.0, 10.0, 10.0), 2.0));
        Assert.Throws<System.ArgumentException>(() => ChartCompactChartsOperations.LayoutTable(new string[] { "A" }, new string[] { "1" }, 1, 2, new ChartRect(0.0, 0.0, 10.0, 10.0), 2.0));
        Assert.Throws<System.ArgumentException>(() => ChartCompactChartsOperations.LayoutTable(new string[] { "A" }, new string[] { "1" }, 1, 1, new ChartRect(0.0, 0.0, 10.0, 10.0), 10.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.AddTableToScene(null!, "table", table, style, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.AddTableToScene(scene, "table", null!, style, style));
        Assert.Throws<System.ArgumentException>(() => ChartCompactChartsOperations.LayoutSparkline(new double[] { 1.0 }, new ChartRect(0.0, 0.0, 10.0, 10.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.AddSparklineToScene(scene, "spark", null!, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartCompactChartsOperations.AddSparklineToScene(null!, "spark", sparkline, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartTableLayout(0, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.CellAt(99));
    }

    static ChartPalette CreatePalette()
    {
        return new ChartPalette(
            "compact",
            new ChartColor[]
            {
                ChartColor.FromRgb(10, 0, 0),
                ChartColor.FromRgb(20, 0, 0)
            });
    }

    static void AssertLine(ChartRenderCommand command, double x1, double y1, double x2, double y2)
    {
        Assert.That(command.X1, Is.EqualTo(x1).Within(0.0000001));
        Assert.That(command.Y1, Is.EqualTo(y1).Within(0.0000001));
        Assert.That(command.X2, Is.EqualTo(x2).Within(0.0000001));
        Assert.That(command.Y2, Is.EqualTo(y2).Within(0.0000001));
    }

    static void AssertRect(ChartRect rect, double x, double y, double width, double height)
    {
        Assert.That(rect.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(rect.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(rect.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(rect.Height, Is.EqualTo(height).Within(0.0000001));
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
