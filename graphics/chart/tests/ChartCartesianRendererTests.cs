namespace chart.Tests;

[TestFixture]
public class ChartCartesianRendererTests
{
    [Test]
    public void CartesianTransform_MapsAndInvertsDataCoordinates()
    {
        ChartCartesianTransform transform = CreateTransform();

        ChartPoint lowerLeft = transform.MapPoint(0.0, -50.0);
        ChartPoint center = transform.MapPoint(5.0, 0.0);
        ChartPoint upperRight = transform.MapPoint(10.0, 50.0);

        AssertPoint(lowerLeft, 10.0, 100.0);
        AssertPoint(center, 60.0, 60.0);
        AssertPoint(upperRight, 110.0, 20.0);
        Assert.That(transform.InvertX(60.0), Is.EqualTo(5.0).Within(0.0000001));
        Assert.That(transform.InvertY(60.0), Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(transform.ContainsDataPoint(5.0, 0.0), Is.True);
        Assert.That(transform.ContainsDataPoint(11.0, 0.0), Is.False);
        Assert.That(transform.ContainsPlotPoint(new ChartPoint(60.0, 60.0)), Is.True);
    }

    [Test]
    public void CartesianTransform_ClipsLinesAndPolygonsToPlotArea()
    {
        ChartCartesianTransform transform = CreateTransform();

        ChartCartesianLineClip visible = transform.ClipDataLine(-5.0, 0.0, 5.0, 0.0);
        ChartCartesianLineClip hidden = transform.ClipDataLine(-5.0, 80.0, -1.0, 80.0);
        ChartPoint[] polygon = transform.ClipPlotPolygon(
            new ChartPoint[]
            {
                new ChartPoint(0.0, 60.0),
                new ChartPoint(60.0, 0.0),
                new ChartPoint(120.0, 60.0),
                new ChartPoint(60.0, 120.0)
            });

        Assert.That(visible.Visible, Is.True);
        AssertPoint(visible.Start, 10.0, 60.0);
        AssertPoint(visible.End, 60.0, 60.0);
        Assert.That(hidden.Visible, Is.False);
        Assert.That(polygon.Length, Is.GreaterThanOrEqualTo(4));
        int index = 0;
        while (index < polygon.Length)
        {
            Assert.That(transform.ContainsPlotPoint(polygon[index]), Is.True);
            index = index + 1;
        }
    }

    [Test]
    public void CartesianRenderer_ClipsPolylineSegmentsAndSkipsOutsideMarkers()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 20.0, 20.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries series = new ChartCartesianSeries("line", ChartCartesianSeriesKind.Line);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        series.AddPolyline("crossing", new double[] { -5.0, 5.0, 15.0 }, new double[] { 5.0, 5.0, 5.0 }, stroke);
        series.AddMarker("outside", ChartMarkerShape.Circle, 20.0, 20.0, 4.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0)));
        renderer.AddSeries(series);
        ChartRenderScene scene = renderer.Render(20, 20);

        Assert.That(scene.CommandCount, Is.EqualTo(2));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(scene.CommandAt(1).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(scene.CommandAt(0).X1, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(scene.CommandAt(0).Y1, Is.EqualTo(10.0).Within(0.0000001));
        Assert.That(scene.CommandAt(1).X2, Is.EqualTo(20.0).Within(0.0000001));
        Assert.That(scene.CommandAt(1).Y2, Is.EqualTo(10.0).Within(0.0000001));
    }

    [Test]
    public void CartesianRenderer_RendersMixedSeriesByOrder()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 20.0, 20.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries red = new ChartCartesianSeries("red", ChartCartesianSeriesKind.Scatter);
        ChartCartesianSeries blue = new ChartCartesianSeries("blue", ChartCartesianSeriesKind.Line);
        ChartRenderScene scene = new ChartRenderScene(20, 20);

        red.SetOrder(0);
        blue.SetOrder(1);
        red.AddMarker("red-point", ChartMarkerShape.Square, 5.0, 5.0, 6.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(255, 0, 0)));
        blue.AddMarker("blue-point", ChartMarkerShape.Square, 5.0, 5.0, 6.0, ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 0, 255)));
        renderer.AddSeries(blue);
        renderer.AddSeries(red);
        scene.AddClear(ChartColor.White());
        renderer.RenderToScene(scene);

        string svg = scene.ToSvgString();
        RawBitmap bitmap = scene.ToRawBitmap();

        Assert.That(svg.IndexOf("id=\"red-point\""), Is.LessThan(svg.IndexOf("id=\"blue-point\"")));
        AssertColor(bitmap.GetPixel(10, 10), 0, 0, 255, 255);
    }

    [Test]
    public void CartesianRenderer_RendersUnclippedMixedCommandTypes()
    {
        ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 100.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries series = new ChartCartesianSeries("mixed", ChartCartesianSeriesKind.Mixed);

        series.SetClipToPlotArea(false);
        series.AddLine("line", -1.0, 5.0, 11.0, 5.0, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        series.AddPolygon("area", new double[] { 2.0, 4.0, 6.0 }, new double[] { 2.0, 6.0, 2.0 }, ChartRenderStyle.CreateFill(ChartColor.FromRgb(0, 200, 0)));
        series.AddText("label", "A", 5.0, 8.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        renderer.AddSeries(series);

        ChartRenderScene scene = renderer.Render(100, 100);

        Assert.That(scene.CommandCount, Is.EqualTo(3));
        Assert.That(scene.CommandAt(0).X1, Is.EqualTo(-10.0).Within(0.0000001));
        Assert.That(scene.CommandAt(1).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(scene.CommandAt(2).Kind, Is.EqualTo(ChartRenderCommandKind.Text));
    }

    [Test]
    public void CartesianRenderer_ValidatesInputs()
    {
        ChartCartesianTransform transform = CreateTransform();
        ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
        ChartCartesianSeries series = new ChartCartesianSeries("series", ChartCartesianSeriesKind.Mixed);
        ChartAxis xAxis = ChartAxis.CreateBottom(new ChartTick[0], new ChartRange(0.0, 1.0), new ChartRect(0.0, 0.0, 10.0, 10.0));
        ChartAxis yAxis = ChartAxis.CreateLeft(new ChartTick[0], new ChartRange(0.0, 1.0), new ChartRect(0.0, 0.0, 10.0, 10.0));

        Assert.That(ChartCartesianTransform.FromAxes(xAxis, yAxis).MapX(0.5), Is.EqualTo(5.0));
        Assert.Throws<System.ArgumentNullException>(() => new ChartCartesianTransform(null!, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartCartesianTransform.FromAxes(null!, yAxis));
        Assert.Throws<System.ArgumentException>(() => ChartCartesianTransform.FromAxes(yAxis, xAxis));
        Assert.Throws<System.ArgumentNullException>(() => transform.ContainsPlotPoint(null!));
        Assert.Throws<System.ArgumentNullException>(() => transform.ClipPlotLine(null!, new ChartPoint(0.0, 0.0)));
        Assert.Throws<System.ArgumentNullException>(() => transform.ClipPlotPolygon(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartCartesianTransform.IsInsideEdge(new ChartPoint(0.0, 0.0), transform.PlotArea, 9));
        Assert.Throws<System.ArgumentNullException>(() => ChartCartesianTransform.ValidateValueArrays(null!, new double[] { 1.0 }, 1));
        Assert.Throws<System.ArgumentException>(() => ChartCartesianTransform.ValidateValueArrays(new double[] { 1.0 }, new double[] { 1.0, 2.0 }, 1));
        Assert.Throws<System.ArgumentException>(() => new ChartCartesianSeries("", ChartCartesianSeriesKind.Line));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartCartesianSeries("bad", (ChartCartesianSeriesKind)99));
        Assert.Throws<System.ArgumentNullException>(() => series.AddCommand(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => series.CommandAt(0));
        Assert.Throws<System.ArgumentException>(() => series.AddPolyline("bad", new double[] { 1.0 }, new double[] { 1.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => new ChartCartesianRenderer(null!));
        Assert.Throws<System.ArgumentNullException>(() => renderer.AddSeries(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => renderer.SeriesAt(0));
        Assert.Throws<System.ArgumentNullException>(() => renderer.RenderToScene(null!));
        Assert.Throws<System.ArgumentNullException>(() => renderer.RenderSeries(new ChartRenderScene(10, 10), null!));
    }

    static ChartCartesianTransform CreateTransform()
    {
        return new ChartCartesianTransform(new ChartRect(10.0, 20.0, 100.0, 80.0), new ChartRange(0.0, 10.0), new ChartRange(-50.0, 50.0));
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
