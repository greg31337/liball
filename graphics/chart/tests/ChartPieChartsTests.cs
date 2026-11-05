namespace chart.Tests;

[TestFixture]
public class ChartPieChartsTests
{
    [Test]
    public void PieLayout_ComputesAnglesAndOrderedSlices()
    {
        ChartPalette palette = CreatePalette();
        ChartPieLayout layout = ChartPieChartsOperations.LayoutPie(
            new string[] { "A", "B", "C" },
            new double[] { 2.0, 1.0, 3.0 },
            50.0,
            60.0,
            20.0,
            0.0,
            ChartPieSliceOrder.Descending,
            palette);

        Assert.That(layout.SliceCount, Is.EqualTo(3));
        AssertSlice(layout.SliceAt(0), "C", 3.0, 0.0, 180.0, 0.0, 20.0);
        AssertSlice(layout.SliceAt(1), "A", 2.0, 180.0, 300.0, 0.0, 20.0);
        AssertSlice(layout.SliceAt(2), "B", 1.0, 300.0, 360.0, 0.0, 20.0);
        AssertColor(layout.SliceAt(0).Color, 30, 0, 0, 255);
        AssertColor(layout.SliceAt(1).Color, 10, 0, 0, 255);
        AssertColor(layout.SliceAt(2).Color, 20, 0, 0, 255);

        int[] ascending = ChartPieChartsOperations.OrderedIndexes(new double[] { 2.0, 1.0, 3.0 }, ChartPieSliceOrder.Ascending);
        Assert.That(ascending, Is.EqualTo(new int[] { 1, 0, 2 }));
    }

    [Test]
    public void DonutAndRoseLayouts_SetInnerRadiusAndScaledOuterRadius()
    {
        ChartPalette palette = CreatePalette();
        ChartPieLayout donut = ChartPieChartsOperations.LayoutDonut(
            new string[] { "A", "B" },
            new double[] { 1.0, 1.0 },
            40.0,
            45.0,
            5.0,
            20.0,
            90.0,
            ChartPieSliceOrder.Original,
            palette);
        ChartPieLayout rose = ChartPieChartsOperations.LayoutRose(
            new string[] { "A", "B" },
            new double[] { 1.0, 4.0 },
            40.0,
            45.0,
            4.0,
            20.0,
            0.0,
            ChartPieSliceOrder.Original,
            palette);

        AssertSlice(donut.SliceAt(0), "A", 1.0, 90.0, 270.0, 5.0, 20.0);
        AssertSlice(donut.SliceAt(1), "B", 1.0, 270.0, 450.0, 5.0, 20.0);
        AssertSlice(rose.SliceAt(0), "A", 1.0, 0.0, 180.0, 4.0, 12.0);
        AssertSlice(rose.SliceAt(1), "B", 4.0, 180.0, 360.0, 4.0, 20.0);
    }

    [Test]
    public void PieScene_AddsSlicesLabelsAndLeaderLines()
    {
        ChartPieLayout layout = ChartPieChartsOperations.LayoutPie(
            new string[] { "North", "South" },
            new double[] { 1.0, 1.0 },
            50.0,
            50.0,
            20.0,
            0.0,
            ChartPieSliceOrder.Original,
            CreatePalette());
        ChartRenderScene scene = ChartPieChartsOperations.CreatePieScene("pie", 100, 100, layout);
        ChartRenderStyle labelStyle = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartRenderStyle leaderStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartPieChartsOperations.AddLabelsAndLeaderLines(scene, "pie", layout, 30.0, labelStyle, leaderStyle);

        Assert.That(scene.CommandCount, Is.EqualTo(6));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Path));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("pie-slice-0"));
        Assert.That(scene.CommandAt(2).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(scene.CommandAt(2).Id, Is.EqualTo("pie-leader-0"));
        Assert.That(scene.CommandAt(3).Kind, Is.EqualTo(ChartRenderCommandKind.Text));
        Assert.That(scene.CommandAt(3).Text, Is.EqualTo("North"));
        AssertPoint(new ChartPoint(scene.CommandAt(2).X1, scene.CommandAt(2).Y1), 70.0, 50.0);
        AssertPoint(new ChartPoint(scene.CommandAt(2).X2, scene.CommandAt(2).Y2), 80.0, 50.0);
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"pie-slice-0\""));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"pie-label-1\""));
    }

    [Test]
    public void SlicePath_ApproximatesPieAndDonutGeometry()
    {
        ChartPieSlice pie = new ChartPieSlice("A", 1.0, 0.0, 90.0, 0.0, 10.0, ChartColor.Black());
        ChartPieSlice donut = new ChartPieSlice("B", 1.0, 0.0, 90.0, 5.0, 10.0, ChartColor.Black());
        ChartPath piePath = ChartPieChartsOperations.CreateSlicePath(10.0, 10.0, pie);
        ChartPath donutPath = ChartPieChartsOperations.CreateSlicePath(10.0, 10.0, donut);

        Assert.That(piePath.Commands[0].Kind, Is.EqualTo(ChartPathCommandKind.MoveTo));
        AssertPoint(new ChartPoint(piePath.Commands[0].X1, piePath.Commands[0].Y1), 10.0, 10.0);
        Assert.That(piePath.Commands[1].Kind, Is.EqualTo(ChartPathCommandKind.LineTo));
        AssertPoint(new ChartPoint(piePath.Commands[1].X1, piePath.Commands[1].Y1), 10.0, 0.0);
        Assert.That(piePath.Commands[piePath.Commands.Count - 1].Kind, Is.EqualTo(ChartPathCommandKind.Close));

        Assert.That(donutPath.Commands[0].Kind, Is.EqualTo(ChartPathCommandKind.MoveTo));
        AssertPoint(new ChartPoint(donutPath.Commands[0].X1, donutPath.Commands[0].Y1), 10.0, 0.0);
        Assert.That(SvgWriter.PathData(donutPath), Does.Contain("L 20 10 L 15 10"));
        Assert.That(ChartPieChartsOperations.ArcSegmentCount(0.0, 90.0), Is.EqualTo(6));
    }

    [Test]
    public void PieCharts_ValidateInputs()
    {
        ChartPalette palette = CreatePalette();
        ChartPieLayout layout = ChartPieChartsOperations.LayoutPie(new string[] { "A" }, new double[] { 1.0 }, 10.0, 10.0, 5.0, 0.0, ChartPieSliceOrder.Original, palette);
        ChartRenderScene scene = new ChartRenderScene(20, 20);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());

        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.LayoutPie(null!, new double[] { 1.0 }, 0.0, 0.0, 1.0, 0.0, ChartPieSliceOrder.Original, palette));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.LayoutPie(new string[] { "A" }, null!, 0.0, 0.0, 1.0, 0.0, ChartPieSliceOrder.Original, palette));
        Assert.Throws<System.ArgumentException>(() => ChartPieChartsOperations.LayoutPie(new string[] { "A" }, new double[] { 1.0, 2.0 }, 0.0, 0.0, 1.0, 0.0, ChartPieSliceOrder.Original, palette));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPieChartsOperations.LayoutPie(new string[] { "A" }, new double[] { -1.0 }, 0.0, 0.0, 1.0, 0.0, ChartPieSliceOrder.Original, palette));
        Assert.Throws<System.ArgumentException>(() => ChartPieChartsOperations.LayoutPie(new string[] { "A" }, new double[] { 0.0 }, 0.0, 0.0, 1.0, 0.0, ChartPieSliceOrder.Original, palette));
        Assert.Throws<System.ArgumentException>(() => ChartPieChartsOperations.LayoutDonut(new string[] { "A" }, new double[] { 1.0 }, 0.0, 0.0, 2.0, 2.0, 0.0, ChartPieSliceOrder.Original, palette));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPieChartsOperations.LayoutPie(new string[] { "A" }, new double[] { 1.0 }, 0.0, 0.0, 1.0, 0.0, (ChartPieSliceOrder)99, palette));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.LayoutPie(new string[] { "A" }, new double[] { 1.0 }, 0.0, 0.0, 1.0, 0.0, ChartPieSliceOrder.Original, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => layout.SliceAt(1));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.AddSlices(null!, "pie", layout));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.AddSlices(scene, "pie", null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPieChartsOperations.AddLabelsAndLeaderLines(scene, "pie", layout, 0.0, style, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.AddLabelsAndLeaderLines(scene, "pie", layout, 10.0, null!, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.CreateSlicePath(0.0, 0.0, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartPieChartsOperations.AddArcPoints(null!, 0.0, 0.0, 1.0, 0.0, 90.0, 2, true));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPieChartsOperations.AddArcPoints(new ChartPath(), 0.0, 0.0, 1.0, 0.0, 90.0, 0, true));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPieChartsOperations.PointAtAngle(0.0, 0.0, 0.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPieChartsOperations.ArcSegmentCount(0.0 - double.MaxValue, double.MaxValue));
        Assert.Throws<System.ArgumentException>(() => new ChartPieLayout(0.0, 0.0, new ChartPieSlice[0]));
    }

    static ChartPalette CreatePalette()
    {
        return new ChartPalette(
            "test",
            new ChartColor[]
            {
                ChartColor.FromRgb(10, 0, 0),
                ChartColor.FromRgb(20, 0, 0),
                ChartColor.FromRgb(30, 0, 0)
            });
    }

    static void AssertSlice(ChartPieSlice slice, string label, double value, double startAngle, double endAngle, double innerRadius, double outerRadius)
    {
        Assert.That(slice.Label, Is.EqualTo(label));
        Assert.That(slice.Value, Is.EqualTo(value).Within(0.0000001));
        Assert.That(slice.StartAngle, Is.EqualTo(startAngle).Within(0.0000001));
        Assert.That(slice.EndAngle, Is.EqualTo(endAngle).Within(0.0000001));
        Assert.That(slice.InnerRadius, Is.EqualTo(innerRadius).Within(0.0000001));
        Assert.That(slice.OuterRadius, Is.EqualTo(outerRadius).Within(0.0000001));
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
