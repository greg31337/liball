namespace chart.Tests;

[TestFixture]
public class ChartFamilyCompletionBehaviorTests
{
    [Test]
    public void GridFacade_BuildsEveryGridChartFamily()
    {
        ChartGridCharts charts = new ChartGridCharts();
        ChartVisualMap map = new ChartVisualMap(ChartVisualMapKind.Continuous, ChartEncodingChannel.Color);
        map.SetDomainMinimum(0.0);
        map.SetDomainMaximum(2.0);
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(2, 2, new double[] { 0.0, 1.0, 1.0, 0.0 });
        double[] centers = new double[] { 0.0, 1.0 };

        ChartCartesianSeries heatmap = charts.CreateHeatmapSeries("heatmap", centers, centers, matrix, 1.0, 1.0, map);
        ChartCartesianSeries image = charts.CreateMatrixImageSeries("image", matrix, map);
        ChartCartesianSeries calendar = charts.CreateCalendarHeatmapSeries(
            "calendar",
            new System.DateTime[] { new System.DateTime(2026, 1, 4), new System.DateTime(2026, 1, 11) },
            new double[] { 1.0, 2.0 },
            1.0,
            1.0,
            map);
        ChartHistogram2D histogram = charts.Bin2D(new double[] { 0.0, 0.2, 1.0 }, new double[] { 0.0, 0.2, 1.0 }, 2, 2);
        ChartCartesianSeries histogramSeries = charts.CreateHistogram2DSeries("histogram", histogram, map);
        ChartHexbinResult hexbin = charts.Hexbin(new double[] { 0.1, 0.2, 2.0 }, new double[] { 0.1, 0.2, 0.1 }, 1.0);
        ChartCartesianSeries hexbinSeries = charts.CreateHexbinSeries("hexbin", hexbin, map);
        ChartContourSegment[] segments = charts.ContourSegments(centers, centers, matrix, 0.5);
        ChartCartesianSeries contour = charts.CreateContourLineSeries("contour", centers, centers, matrix, new double[] { 0.5 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        ChartCartesianSeries filled = charts.CreateFilledContourSeries("filled", centers, centers, matrix, map);

        Assert.That(heatmap.GetCommandCount(), Is.EqualTo(4));
        Assert.That(image.GetCommandCount(), Is.EqualTo(4));
        Assert.That(calendar.GetCommandCount(), Is.EqualTo(2));
        Assert.That(histogram.Counts.GetRows(), Is.EqualTo(2));
        Assert.That(histogramSeries.GetCommandCount(), Is.EqualTo(4));
        Assert.That(hexbin.GetCellCount(), Is.EqualTo(2));
        Assert.That(hexbinSeries.GetCommandCount(), Is.EqualTo(2));
        Assert.That(segments.Length, Is.EqualTo(2));
        Assert.That(contour.GetCommandCount(), Is.EqualTo(2));
        Assert.That(filled.GetCommandCount(), Is.EqualTo(4));
    }

    [Test]
    public void StatisticalFacade_BuildsHistogramAndAutomaticDensityFromValues()
    {
        ChartStatisticalCharts charts = new ChartStatisticalCharts();
        ChartCartesianSeries histogram = charts.CreateHistogramSeriesFromValues(
            "histogram",
            new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 },
            3,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0)));
        ChartCartesianSeries density = charts.CreateDensitySeriesAutomaticBandwidth(
            "density",
            new double[] { 0.0, 1.0, 2.0, 3.0 },
            5,
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        Assert.That(histogram.GetKind(), Is.EqualTo(ChartCartesianSeriesKind.Bar));
        Assert.That(histogram.GetCommandCount(), Is.EqualTo(3));
        Assert.That(density.GetKind(), Is.EqualTo(ChartCartesianSeriesKind.Line));
        Assert.That(density.CommandAt(0).GetPoints().Length, Is.EqualTo(5));
        Assert.That(density.CommandAt(0).GetPoints()[2].GetY(), Is.GreaterThan(0.0));
    }

    [Test]
    public void OverlayFacade_BuildsVerticalBandAndComputesRSquared()
    {
        ChartOverlayCharts charts = new ChartOverlayCharts();
        ChartCartesianSeries band = charts.CreateVerticalReferenceBand(
            "vertical-band",
            2.0,
            4.0,
            -1.0,
            5.0,
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(220, 220, 220)));
        double rSquared = charts.TrendlineRSquared(
            new double[] { 0.0, 1.0, 2.0 },
            new double[] { 1.0, 3.0, 5.0 },
            2.0,
            1.0,
            3.0);

        ChartPoint[] points = band.CommandAt(0).GetPoints();
        Assert.That(band.GetKind(), Is.EqualTo(ChartCartesianSeriesKind.Area));
        AssertPoint(points[0], 2.0, -1.0);
        AssertPoint(points[1], 2.0, 5.0);
        AssertPoint(points[2], 4.0, 5.0);
        AssertPoint(points[3], 4.0, -1.0);
        Assert.That(rSquared, Is.EqualTo(1.0).Within(0.0000001));
    }

    [Test]
    public void ScatterFacade_UsesExplicitMarginalRugBaselines()
    {
        ChartScatterCharts charts = new ChartScatterCharts();
        ChartCartesianSeries[] series = charts.CreateMarginalScatterSeriesWithBaselines(
            "marginal",
            new double[] { 1.0, 2.0 },
            new double[] { 3.0, 4.0 },
            ChartMarkerShape.Diamond,
            5.0,
            ChartRenderStyle.CreateFill(ChartColor.Black()),
            ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0),
            -2.0,
            -3.0,
            0.5);

        Assert.That(series.Length, Is.EqualTo(3));
        AssertLine(series[1].CommandAt(0), 1.0, -2.0, 1.0, -1.5);
        AssertLine(series[2].CommandAt(1), -3.0, 4.0, -2.5, 4.0);
    }

    [Test]
    public void AnnotationFacade_MapsCoordinatePointArrays()
    {
        ChartAnnotationCharts charts = new ChartAnnotationCharts();
        ChartRenderScene scene = new ChartRenderScene(200, 100);
        ChartConfiguration configuration = new ChartConfiguration();
        configuration.SetCoordinateUnit(ChartCoordinateUnit.Data);

        ChartPoint[] mapped = charts.MapCoordinatePoints(
            scene,
            new ChartPoint[] { new ChartPoint(0.0, 0.0), new ChartPoint(10.0, 20.0) },
            configuration,
            new ChartRect(20.0, 10.0, 100.0, 80.0),
            new ChartRange(0.0, 10.0),
            new ChartRange(0.0, 20.0));

        AssertPoint(mapped[0], 20.0, 90.0);
        AssertPoint(mapped[1], 120.0, 10.0);
        Assert.Throws<System.ArgumentNullException>(() => charts.MapCoordinatePoints(scene, null!, configuration, new ChartRect(0.0, 0.0, 10.0, 10.0), new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
    }

    [Test]
    public void LineAreaFacade_BuildsInterpolatedMultiLineAndRangeAreaSeries()
    {
        ChartLineAreaCharts charts = new ChartLineAreaCharts();
        double[] xValues = new double[] { 0.0, 1.0, 2.0 };
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(3, 2, new double[] { 0.0, 2.0, 1.0, 3.0, 2.0, 4.0 });
        ChartCartesianSeries[] lines = charts.CreateInterpolatedMultiLineSeries(
            "multi",
            xValues,
            matrix,
            new ChartPalettes().Categorical(),
            1.0,
            ChartLineInterpolation.StepMiddle,
            2);
        ChartCartesianSeries range = charts.CreateRangeAreaSeries(
            "range",
            xValues,
            new double[] { 3.0, 4.0, 5.0 },
            new double[] { 1.0, 2.0, 3.0 },
            ChartRenderStyle.CreateFill(ChartColor.FromRgb(100, 150, 200)));

        Assert.That(lines.Length, Is.EqualTo(2));
        Assert.That(lines[0].CommandAt(0).GetPoints().Length, Is.EqualTo(7));
        Assert.That(lines[1].CommandAt(0).GetPoints()[3].GetY(), Is.EqualTo(3.0));
        Assert.That(range.CommandAt(0).GetPoints().Length, Is.EqualTo(6));
        AssertPoint(range.CommandAt(0).GetPoints()[0], 0.0, 3.0);
        AssertPoint(range.CommandAt(0).GetPoints()[5], 0.0, 1.0);
    }

    [Test]
    public void PolarFacade_CreatesCompleteGaugeScene()
    {
        ChartPolarCharts charts = new ChartPolarCharts();
        ChartRenderStyle track = ChartRenderStyle.CreateStroke(ChartColor.FromRgb(220, 220, 220), 4.0);
        ChartRenderStyle value = ChartRenderStyle.CreateStroke(ChartColor.FromRgb(20, 100, 200), 4.0);
        ChartRenderStyle needle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartRenderScene scene = charts.CreateGaugeScene(
            "gauge", 120, 120, 60.0, 60.0, 30.0, 45.0, 0.0, 100.0, 75.0, -120.0, 120.0,
            track, value, needle);

        Assert.That(scene.GetCommandCount(), Is.EqualTo(3));
        Assert.That(scene.CommandAt(0).GetId(), Is.EqualTo("gauge-track"));
        Assert.That(scene.CommandAt(1).GetId(), Is.EqualTo("gauge-value"));
        Assert.That(scene.CommandAt(2).GetId(), Is.EqualTo("gauge-needle"));
    }

    [Test]
    public void FinancialFacade_ColorsDirectionalVolumeByPriceMovement()
    {
        ChartFinancialCharts charts = new ChartFinancialCharts();
        ChartRenderStyle up = ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 160, 0));
        ChartRenderStyle down = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));

        ChartCartesianSeries volume = charts.CreateDirectionalVolumeSeries(
            "volume",
            new double[] { 1.0, 2.0 },
            new double[] { 10.0, 12.0 },
            new double[] { 11.0, 10.0 },
            new double[] { 100.0, 150.0 },
            0.8,
            0.0,
            up,
            down);

        Assert.That(volume.GetCommandCount(), Is.EqualTo(2));
        Assert.That(volume.CommandAt(0).GetStyle().GetFillColor().GetG(), Is.EqualTo(160));
        Assert.That(volume.CommandAt(1).GetStyle().GetFillColor().GetR(), Is.EqualTo(200));
    }

    [Test]
    public void RelationshipFacade_AddsExplicitAndHierarchyLinks()
    {
        ChartRelationshipCharts charts = new ChartRelationshipCharts();
        ChartGraphLayout graph = new ChartGraphLayout(2, 1);
        graph.AddNode(new ChartGraphNodeLayout(0, "A", 1.0, 2.0, 0.0, 0.0, 0));
        graph.AddNode(new ChartGraphNodeLayout(1, "B", 3.0, 4.0, 0.0, 0.0, 0));
        charts.AddGraphLinks(graph, new ChartGraphLink[] { new ChartGraphLink(0, 1, 2.5) });

        Assert.That(graph.GetLinkCount(), Is.EqualTo(1));
        Assert.That(graph.LinkAt(0).GetSourceIndex(), Is.EqualTo(0));
        Assert.That(graph.LinkAt(0).GetTargetIndex(), Is.EqualTo(1));
        Assert.That(graph.LinkAt(0).GetWidth(), Is.EqualTo(2.5));

        ChartHierarchyData hierarchy = new ChartHierarchyData(new ChartHierarchyNode[]
        {
            new ChartHierarchyNode("root", 0.0, -1),
            new ChartHierarchyNode("child", 4.0, 0)
        });
        ChartGraphLayout tree = new ChartGraphLayout(2, 1);
        tree.AddNode(new ChartGraphNodeLayout(0, "root", 0.0, 0.0, 0.0, 0.0, 0));
        tree.AddNode(new ChartGraphNodeLayout(1, "child", 1.0, 1.0, 0.0, 0.0, 1));
        charts.AddHierarchyLinks(tree, hierarchy);

        Assert.That(tree.GetLinkCount(), Is.EqualTo(1));
        Assert.That(tree.LinkAt(0).GetValue(), Is.EqualTo(4.0));
    }

    [Test]
    public void LabelPlacementFacade_CoversBelowAndLeftPositions()
    {
        ChartLabelPlacementCharts charts = new ChartLabelPlacementCharts();
        ChartPlacedLabel below = charts.LayoutPointLabels(
            "below", new string[] { "A" }, new double[] { 10.0 }, new double[] { 20.0 },
            8.0, 4.0, 2.0, ChartLabelPosition.Below, false)[0];
        ChartPlacedLabel left = charts.LayoutPointLabels(
            "left", new string[] { "B" }, new double[] { 10.0 }, new double[] { 20.0 },
            8.0, 4.0, 2.0, ChartLabelPosition.Left, false)[0];

        Assert.That(below.GetPosition(), Is.EqualTo(ChartLabelPosition.Below));
        Assert.That(below.GetX(), Is.EqualTo(6.0));
        Assert.That(below.GetY(), Is.EqualTo(22.0));
        Assert.That(left.GetPosition(), Is.EqualTo(ChartLabelPosition.Left));
        Assert.That(left.GetX(), Is.EqualTo(0.0));
        Assert.That(left.GetY(), Is.EqualTo(18.0));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.GetY(), Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertLine(ChartRenderCommand command, double x1, double y1, double x2, double y2)
    {
        Assert.That(command.GetX1(), Is.EqualTo(x1).Within(0.0000001));
        Assert.That(command.GetY1(), Is.EqualTo(y1).Within(0.0000001));
        Assert.That(command.GetX2(), Is.EqualTo(x2).Within(0.0000001));
        Assert.That(command.GetY2(), Is.EqualTo(y2).Within(0.0000001));
    }
}
