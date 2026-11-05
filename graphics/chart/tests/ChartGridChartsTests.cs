namespace chart.Tests;

[TestFixture]
public class ChartGridChartsTests
{
    [Test]
    public void HeatmapSeries_CreatesColoredCellPolygons()
    {
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 3.0, ChartPalettesOperations.GrayscaleMap());
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 0.0, 1.0, 2.0, 3.0 });
        ChartCartesianSeries series = ChartGridChartsOperations.CreateHeatmapSeries("heat", new double[] { 10.0, 20.0 }, new double[] { 1.0, 2.0 }, values, 4.0, 2.0, map);

        Assert.That(series.CommandCount, Is.EqualTo(4));
        AssertPoint(series.CommandAt(0).Points[0], 8.0, 0.0);
        AssertPoint(series.CommandAt(0).Points[2], 12.0, 2.0);
        AssertColor(series.CommandAt(0).Style.FillColor, 0, 0, 0, 255);
        AssertColor(series.CommandAt(3).Style.FillColor, 255, 255, 255, 255);
    }

    [Test]
    public void MatrixAndCalendarHeatmaps_MapCellsToIndexAndWeekDayCoordinates()
    {
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 10.0, ChartPalettesOperations.GrayscaleMap());
        ChartCartesianSeries image = ChartGridChartsOperations.CreateMatrixImageSeries("image", ChartDataMatrix.FromValues(1, 2, new double[] { 0.0, 10.0 }), map);
        ChartCartesianSeries calendar = ChartGridChartsOperations.CreateCalendarHeatmapSeries(
            "calendar",
            new System.DateTime[] { new System.DateTime(2026, 1, 4), new System.DateTime(2026, 1, 11) },
            new double[] { 0.0, 10.0 },
            1.0,
            1.0,
            map);

        Assert.That(image.CommandCount, Is.EqualTo(2));
        AssertPoint(image.CommandAt(0).Points[0], 0.0, 0.0);
        AssertPoint(image.CommandAt(1).Points[2], 2.0, 1.0);
        Assert.That(calendar.CommandCount, Is.EqualTo(2));
        AssertPoint(calendar.CommandAt(0).Points[0], -0.5, -0.5);
        AssertPoint(calendar.CommandAt(1).Points[0], 0.5, -0.5);
        Assert.That(ChartGridChartsOperations.WeekIndex(new System.DateTime(2026, 1, 4), new System.DateTime(2026, 1, 18)), Is.EqualTo(2.0));
    }

    [Test]
    public void Histogram2D_BinsPointsAndCreatesHeatmapSeries()
    {
        ChartHistogram2D histogram = ChartGridChartsOperations.Bin2D(new double[] { 0.0, 0.9, 1.1, 2.0 }, new double[] { 0.0, 0.9, 1.1, 2.0 }, 2, 2);
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 2.0, ChartPalettesOperations.GrayscaleMap());
        ChartCartesianSeries series = ChartGridChartsOperations.CreateHistogram2DSeries("hist2d", histogram, map);

        Assert.That(histogram.Counts.GetValue(0, 0), Is.EqualTo(2.0));
        Assert.That(histogram.Counts.GetValue(1, 1), Is.EqualTo(2.0));
        Assert.That(histogram.XBinWidth, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(histogram.YBinWidth, Is.EqualTo(1.0).Within(0.0000001));
        Assert.That(series.CommandCount, Is.EqualTo(4));
        AssertColor(series.CommandAt(0).Style.FillColor, 255, 255, 255, 255);
    }

    [Test]
    public void Hexbin_AggregatesPointsAndCreatesHexagonSeries()
    {
        ChartHexbinResult result = ChartGridChartsOperations.Hexbin(new double[] { 0.1, 0.2, 2.0 }, new double[] { 0.1, 0.2, 0.1 }, 1.0);
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 2.0, ChartPalettesOperations.GrayscaleMap());
        ChartCartesianSeries series = ChartGridChartsOperations.CreateHexbinSeries("hex", result, map);

        Assert.That(result.CellCount, Is.EqualTo(2));
        Assert.That(result.CellAt(0).Count, Is.EqualTo(2));
        Assert.That(result.CellAt(1).Count, Is.EqualTo(1));
        Assert.That(series.CommandCount, Is.EqualTo(2));
        Assert.That(series.CommandAt(0).Points.Length, Is.EqualTo(6));
        AssertColor(series.CommandAt(0).Style.FillColor, 255, 255, 255, 255);
        AssertColor(series.CommandAt(1).Style.FillColor, 128, 128, 128, 255);
    }

    [Test]
    public void ContourLineAndFilledContour_CreateSegmentsAndCells()
    {
        double[] centers = new double[] { 0.0, 1.0 };
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 2, new double[] { 0.0, 1.0, 1.0, 0.0 });
        ChartContourSegment[] segments = ChartGridChartsOperations.ContourSegments(centers, centers, values, 0.5);
        ChartCartesianSeries contour = ChartGridChartsOperations.CreateContourLineSeries("contour", centers, centers, values, new double[] { 0.5 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        ChartCartesianSeries filled = ChartGridChartsOperations.CreateFilledContourSeries("filled", centers, centers, values, ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 1.0, ChartPalettesOperations.GrayscaleMap()));

        Assert.That(segments.Length, Is.EqualTo(2));
        AssertPoint(segments[0].Start, 0.5, 0.0);
        AssertPoint(segments[0].End, 1.0, 0.5);
        Assert.That(contour.CommandCount, Is.EqualTo(2));
        AssertLine(contour.CommandAt(0), 0.5, 0.0, 1.0, 0.5);
        Assert.That(filled.CommandCount, Is.EqualTo(4));
    }

    [Test]
    public void GridCharts_RenderThroughCartesianRenderer()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 20.0, 20.0), new ChartRange(0.0, 2.0), new ChartRange(0.0, 2.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 1.0, ChartPalettesOperations.GrayscaleMap());
        ChartCartesianSeries heat = ChartGridChartsOperations.CreateMatrixImageSeries("image", ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 0.0, 0.0, 0.0 }), map);
        ChartRenderScene scene = new ChartRenderScene(20, 20);

        scene.AddClear(ChartColor.White());
        renderer.AddSeries(heat);
        renderer.RenderToScene(scene);

        Assert.That(scene.CommandCount, Is.EqualTo(5));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"image-cell-0-0\""));
        AssertColor(scene.ToRawBitmap().GetPixel(5, 5), 0, 0, 0, 255);
    }

    [Test]
    public void GridCharts_ValidateInputs()
    {
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 1.0, ChartPalettesOperations.GrayscaleMap());
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 1, new double[] { 1.0 });

        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.CreateHeatmapSeries("heat", null!, new double[] { 0.0 }, values, 1.0, 1.0, map));
        Assert.Throws<System.ArgumentException>(() => ChartGridChartsOperations.CreateHeatmapSeries("heat", new double[] { 0.0, 1.0 }, new double[] { 0.0 }, values, 1.0, 1.0, map));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartGridChartsOperations.CreateHeatmapSeries("heat", new double[] { 0.0 }, new double[] { 0.0 }, values, 0.0, 1.0, map));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.CreateHeatmapSeries("heat", new double[] { 0.0 }, new double[] { 0.0 }, values, 1.0, 1.0, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.CreateMatrixImageSeries("image", null!, map));
        Assert.Throws<System.ArgumentException>(() => ChartGridChartsOperations.CreateCalendarHeatmapSeries("calendar", new System.DateTime[] { new System.DateTime(2026, 1, 1) }, new double[] { 1.0, 2.0 }, 1.0, 1.0, map));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartGridChartsOperations.Bin2D(new double[] { 1.0 }, new double[] { 1.0 }, 0, 1));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.CreateHistogram2DSeries("hist", null!, map));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartGridChartsOperations.Hexbin(new double[] { 1.0 }, new double[] { 1.0 }, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.CreateHexbinSeries("hex", null!, map));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.AddCellContourSegments(null!, new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new ChartDataMatrix(2, 2), 0, 0, 0.5));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.ValidateDatesAndValues(null!, new double[] { 1.0 }));
        Assert.Throws<System.ArgumentNullException>(() => ChartGridChartsOperations.ValidateVisualMap(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartGridChartsOperations.IndexCenters(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartGridChartsOperations.BinCenters(0.0, 0.0, 1));
        Assert.That(ChartGridChartsOperations.CrossesLevel(0.0, 1.0, 0.5), Is.True);
        Assert.That(ChartGridChartsOperations.CrossesLevel(1.0, 1.0, 1.0), Is.False);
        Assert.That(ChartGridChartsOperations.CellSpacing(new double[] { 2.0 }), Is.EqualTo(1.0));
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
