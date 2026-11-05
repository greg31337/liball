namespace chart.Tests;

[TestFixture]
public class ChartLabelPlacementChartsTests
{
    [Test]
    public void ValueLabelFormatters_CreateReusableLayoutText()
    {
        IChartLabelPlacementCharts charts = new ChartLabelPlacementCharts();
        string[] defaults = charts.FormatValueLabels(new double[] { 1.5, 2000.0 }, new ChartDefaultValueLabelFormatter());
        string[] custom = charts.FormatValueLabels(new double[] { 1.5, 2.0 }, new UnitValueLabelFormatter());
        ChartPlacedLabel[] labels = charts.LayoutPointLabels("value", custom, new double[] { 10.0, 30.0 }, new double[] { 20.0, 20.0 }, 8.0, 4.0, 1.0, ChartLabelPosition.Above, false);

        Assert.That(defaults, Is.EqualTo(new string[] { "1.5", "2000" }));
        Assert.That(custom, Is.EqualTo(new string[] { "1.5 kg", "2 kg" }));
        Assert.That(labels[0].GetText(), Is.EqualTo("1.5 kg"));
        Assert.That(labels[1].GetText(), Is.EqualTo("2 kg"));
    }

    [Test]
    public void LeaderLineOutput_OmitsLabelsHiddenByCollisionReduction()
    {
        IChartLabelPlacementCharts charts = new ChartLabelPlacementCharts();
        ChartPlacedLabel[] labels = charts.LayoutPointLabels(
            "leader",
            new string[] { "A", "B", "C" },
            new double[] { 10.0, 11.0, 30.0 },
            new double[] { 10.0, 10.0, 10.0 },
            8.0,
            4.0,
            2.0,
            ChartLabelPosition.Right,
            true);
        ChartRenderScene scene = new ChartRenderScene(50, 30);

        charts.AddVisibleLabelsAndLeaderLinesToScene(scene, labels, ChartRenderStyle.CreateFill(ChartColor.Black()), ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        Assert.That(labels[1].GetVisible(), Is.False);
        Assert.That(scene.CommandCount, Is.EqualTo(4));
        Assert.That(scene.CommandAt(0).GetId(), Is.EqualTo("leader-0-leader"));
        AssertLine(scene.CommandAt(0), 10.0, 10.0, 12.0, 10.0);
        Assert.That(scene.CommandAt(1).GetId(), Is.EqualTo("leader-0"));
        Assert.That(scene.CommandAt(2).GetId(), Is.EqualTo("leader-2-leader"));
        Assert.That(scene.CommandAt(3).GetId(), Is.EqualTo("leader-2"));
    }

    [Test]
    public void PointLabels_PlaceAroundAnchorsAndReduceCollisions()
    {
        ChartPlacedLabel[] labels = ChartLabelPlacementChartsOperations.LayoutPointLabels(
            "point-label",
            new string[] { "A", "B", "C" },
            new double[] { 0.0, 1.0, 10.0 },
            new double[] { 0.0, 0.0, 0.0 },
            4.0,
            2.0,
            1.0,
            ChartLabelPosition.Right,
            true);
        ChartCartesianSeries series = ChartLabelPlacementChartsOperations.CreateLabelSeries("labels", labels, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(labels.Length, Is.EqualTo(3));
        Assert.That(labels[0].Visible, Is.True);
        Assert.That(labels[1].Visible, Is.False);
        Assert.That(labels[2].Visible, Is.True);
        AssertLabel(labels[0], "point-label-0", "A", 1.0, -1.0, 0.0, 0.0);
        Assert.That(series.CommandCount, Is.EqualTo(2));
        Assert.That(series.CommandAt(0).Text, Is.EqualTo("A"));
        Assert.That(series.CommandAt(1).Text, Is.EqualTo("C"));
    }

    [Test]
    public void BarAndHeatmapLabels_UseBarBoundsAndCellCenters()
    {
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartCartesianSeries bars = ChartBarChartsOperations.CreateVerticalBarSeries("bars", new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }, 0.8, 0.0, fill);
        ChartPlacedLabel[] barLabels = ChartLabelPlacementChartsOperations.LayoutBarLabels("bar-label", new string[] { "One", "Two" }, bars, 1.0, 0.5, 0.0, ChartLabelPosition.Center, false);
        ChartPlacedLabel[] heatmapLabels = ChartLabelPlacementChartsOperations.LayoutHeatmapCellLabels("heat-label", new string[] { "a", "b", "c", "d" }, new double[] { 10.0, 20.0 }, new double[] { 1.0, 2.0 }, 4.0, 2.0, 2.0, 1.0, false);

        AssertLabel(barLabels[0], "bar-label-0", "One", 0.5, 1.25, 1.0, 1.5);
        AssertLabel(barLabels[1], "bar-label-1", "Two", 1.5, 1.75, 2.0, 2.0);
        AssertLabel(heatmapLabels[0], "heat-label-0", "a", 9.0, 0.5, 10.0, 1.0);
        AssertLabel(heatmapLabels[3], "heat-label-3", "d", 19.0, 1.5, 20.0, 2.0);
    }

    [Test]
    public void PieContourAndAnnotationLabels_CreateExpectedAnchors()
    {
        ChartPieLayout pie = ChartPieChartsOperations.LayoutPie(
            new string[] { "North", "South" },
            new double[] { 1.0, 1.0 },
            10.0,
            10.0,
            5.0,
            0.0,
            ChartPieSliceOrder.Original,
            ChartPalettesOperations.Categorical());
        ChartPlacedLabel[] pieLabels = ChartLabelPlacementChartsOperations.LayoutPieSliceLabels("pie-label", pie, 10.0, 4.0, 2.0, false);
        ChartContourSegment[] segments = new ChartContourSegment[] { new ChartContourSegment(0.5, new ChartPoint(0.0, 0.0), new ChartPoint(4.0, 4.0)) };
        ChartPlacedLabel[] contourLabels = ChartLabelPlacementChartsOperations.LayoutContourLabels("contour-label", new string[] { "0.5" }, segments, 2.0, 1.0, false);
        ChartPlacedLabel[] annotationLabels = ChartLabelPlacementChartsOperations.LayoutAnnotationLabels("annotation-label", new string[] { "Callout" }, new ChartRect[] { new ChartRect(5.0, 6.0, 10.0, 4.0) }, 6.0, 2.0, 1.0, ChartLabelPosition.Above, false);
        ChartRenderScene scene = new ChartRenderScene(80, 60);

        ChartLabelPlacementChartsOperations.AddVisibleLabelsToScene(scene, annotationLabels, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(pieLabels[0].AnchorX, Is.EqualTo(20.0).Within(0.0000001));
        Assert.That(pieLabels[0].AnchorY, Is.EqualTo(10.0).Within(0.0000001));
        AssertLabel(contourLabels[0], "contour-label-0", "0.5", 1.0, 1.5, 2.0, 2.0);
        AssertLabel(annotationLabels[0], "annotation-label-0", "Callout", 7.0, 3.0, 10.0, 6.0);
        Assert.That(scene.CommandCount, Is.EqualTo(1));
        Assert.That(scene.CommandAt(0).Text, Is.EqualTo("Callout"));
    }

    [Test]
    public void CollisionReducer_IsDeterministicAndAllowsTouchingEdges()
    {
        ChartPlacedLabel[] labels = new ChartPlacedLabel[]
        {
            new ChartPlacedLabel("a", "A", 0, 0.0, 0.0, 0.0, 0.0, 2.0, 2.0, true, ChartLabelPosition.Center),
            new ChartPlacedLabel("b", "B", 1, 2.0, 0.0, 2.0, 0.0, 2.0, 2.0, true, ChartLabelPosition.Center),
            new ChartPlacedLabel("c", "C", 2, 1.0, 0.0, 1.0, 1.0, 2.0, 2.0, true, ChartLabelPosition.Center)
        };

        int hidden = ChartLabelPlacementChartsOperations.ReduceCollisions(labels);

        Assert.That(hidden, Is.EqualTo(1));
        Assert.That(labels[0].Visible, Is.True);
        Assert.That(labels[1].Visible, Is.True);
        Assert.That(labels[2].Visible, Is.False);
        Assert.That(ChartLabelPlacementChartsOperations.LabelsOverlap(labels[0], labels[1]), Is.False);
        Assert.That(ChartLabelPlacementChartsOperations.LabelsOverlap(labels[0], labels[2]), Is.True);
    }

    [Test]
    public void CollisionHandling_HidesOverlappingLayoutLabelsBeforeRendering()
    {
        ChartPlacedLabel[] labels = ChartLabelPlacementChartsOperations.LayoutAnnotationLabels(
            "collision",
            new string[] { "A", "B", "C" },
            new ChartRect[]
            {
                new ChartRect(10.0, 10.0, 4.0, 4.0),
                new ChartRect(11.0, 10.0, 4.0, 4.0),
                new ChartRect(25.0, 10.0, 4.0, 4.0)
            },
            8.0,
            4.0,
            1.0,
            ChartLabelPosition.Right,
            true);
        ChartCartesianSeries series = ChartLabelPlacementChartsOperations.CreateLabelSeries("collision-series", labels, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(labels[0].Visible, Is.True);
        Assert.That(labels[1].Visible, Is.False);
        Assert.That(labels[2].Visible, Is.True);
        Assert.That(series.CommandCount, Is.EqualTo(2));
        Assert.That(series.CommandAt(0).Id, Is.EqualTo("collision-0"));
        Assert.That(series.CommandAt(1).Id, Is.EqualTo("collision-2"));
    }

    [Test]
    public void LabelPlacementCharts_ValidateInputs()
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());
        ChartPlacedLabel[] placed = new ChartPlacedLabel[] { new ChartPlacedLabel("label", "A", 0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, true, ChartLabelPosition.Center) };
        ChartContourSegment[] segments = new ChartContourSegment[] { new ChartContourSegment(0.5, new ChartPoint(0.0, 0.0), new ChartPoint(1.0, 1.0)) };
        ChartCartesianSeries bars = new ChartCartesianSeries("bars", ChartCartesianSeriesKind.Bar);
        bars.AddLine("line", 0.0, 0.0, 1.0, 1.0, style);

        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.LayoutPointLabels("p", null!, new double[] { 0.0 }, new double[] { 0.0 }, 1.0, 1.0, 0.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentException>(() => ChartLabelPlacementChartsOperations.LayoutPointLabels("p", new string[] { "A" }, new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, 1.0, 1.0, 0.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLabelPlacementChartsOperations.LayoutPointLabels("p", new string[] { "A" }, new double[] { 0.0 }, new double[] { 0.0 }, 0.0, 1.0, 0.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLabelPlacementChartsOperations.LayoutPointLabels("p", new string[] { "A" }, new double[] { 0.0 }, new double[] { 0.0 }, 1.0, 1.0, -1.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentException>(() => ChartLabelPlacementChartsOperations.LayoutBarLabels("b", new string[] { "A" }, bars, 1.0, 1.0, 0.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentException>(() => ChartLabelPlacementChartsOperations.LayoutHeatmapCellLabels("h", new string[] { "A" }, new double[] { 0.0, 1.0 }, new double[] { 0.0 }, 1.0, 1.0, 1.0, 1.0, false));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.LayoutPieSliceLabels("pie", null!, 1.0, 1.0, 1.0, false));
        Assert.Throws<System.ArgumentException>(() => ChartLabelPlacementChartsOperations.LayoutContourLabels("contour", new string[] { "A", "B" }, segments, 1.0, 1.0, false));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.LayoutAnnotationLabels("a", new string[] { "A" }, null!, 1.0, 1.0, 0.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentException>(() => ChartLabelPlacementChartsOperations.LayoutAnnotationLabels("a", new string[] { "A" }, new ChartRect[] { new ChartRect(0.0, 0.0, 0.0, 1.0) }, 1.0, 1.0, 0.0, ChartLabelPosition.Center, false));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.CreateLabelSeries("labels", null!, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.CreateLabelSeries("labels", placed, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.AddVisibleLabelsToScene(null!, placed, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.FormatValueLabels(null!, new ChartDefaultValueLabelFormatter()));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.FormatValueLabels(new double[] { 1.0 }, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLabelPlacementChartsOperations.FormatValueLabels(new double[] { double.NaN }, new ChartDefaultValueLabelFormatter()));
        Assert.Throws<System.ArgumentNullException>(() => ChartLabelPlacementChartsOperations.AddVisibleLabelsAndLeaderLinesToScene(null!, placed, style, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartLabelPlacementChartsOperations.ValidatePosition((ChartLabelPosition)99));
    }

    static void AssertLine(ChartRenderCommand command, double x1, double y1, double x2, double y2)
    {
        Assert.That(command.X1, Is.EqualTo(x1).Within(0.0000001));
        Assert.That(command.Y1, Is.EqualTo(y1).Within(0.0000001));
        Assert.That(command.X2, Is.EqualTo(x2).Within(0.0000001));
        Assert.That(command.Y2, Is.EqualTo(y2).Within(0.0000001));
    }

    static void AssertLabel(ChartPlacedLabel label, string id, string text, double x, double y, double anchorX, double anchorY)
    {
        Assert.That(label.Id, Is.EqualTo(id));
        Assert.That(label.Text, Is.EqualTo(text));
        Assert.That(label.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(label.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(label.AnchorX, Is.EqualTo(anchorX).Within(0.0000001));
        Assert.That(label.AnchorY, Is.EqualTo(anchorY).Within(0.0000001));
        Assert.That(label.Visible, Is.True);
    }

    class UnitValueLabelFormatter : IChartValueLabelFormatter
    {
        public string FormatValue(double value)
        {
            return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + " kg";
        }
    }
}
