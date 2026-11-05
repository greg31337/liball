namespace chart.Tests;

[TestFixture]
public class ChartDocumentConvenienceTests
{
    [Test]
    public void DocumentConvenience_CreatesPlotsScenesAndExports()
    {
        ChartDocument document = new ChartDocument(40.0, 30.0);
        document.SetAccessibilityTitleDescription("Convenience", "Scene export");
        ChartPlot plot = document.AddPlotWithBounds("main", new ChartRect(0.0, 0.0, 40.0, 30.0));
        ChartPlot gridPlot = document.AddGridPlot("cell", 0, 0, 1, 1);
        ChartRenderScene scene = document.CreateScene();

        scene.AddLine("line", 0.0, 15.0, 40.0, 15.0, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        string svg = document.ExportSceneSvg(scene);
        byte[] png = document.ExportScenePng(scene);
        SvgWriter writer = document.CreateSvgWriter();

        Assert.That(plot.Id, Is.EqualTo("main"));
        Assert.That(gridPlot.HasGridPosition, Is.True);
        Assert.That(scene.Width, Is.EqualTo(40));
        Assert.That(scene.Height, Is.EqualTo(30));
        Assert.That(scene.CommandCount, Is.EqualTo(2));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Clear));
        Assert.That(svg, Does.Contain("<title>Convenience</title>"));
        Assert.That(svg, Does.Contain("<desc>Scene export</desc>"));
        Assert.That(svg, Does.Contain("<line id=\"line\""));
        Assert.That(new byte[] { png[0], png[1], png[2], png[3], png[4], png[5], png[6], png[7] }, Is.EqualTo(PngEncoder.Signature()));
        Assert.That(writer.ToSvgString(), Does.Contain("<title>Convenience</title>"));
    }

    [Test]
    public void PlotConvenience_AddsAxesAndCreatesCartesianRenderer()
    {
        ChartPlot plot = new ChartPlot("plot");
        plot.SetBounds(new ChartRect(0.0, 0.0, 100.0, 100.0));
        plot.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));

        ChartAxis xAxis = plot.AddBottomAxis(0.0, 10.0, 3);
        ChartAxis yAxis = plot.AddLeftAxis(0.0, 10.0, 3);
        ChartCartesianTransform explicitTransform = plot.CreateCartesianTransform(new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        ChartCartesianTransform axisTransform = plot.CreateCartesianTransformFromAxes();
        ChartCartesianRenderer renderer = plot.CreateCartesianRendererFromAxes();
        ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 10.0 }, new double[] { 0.0, 10.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        renderer.AddSeries(series);
        ChartRenderScene scene = renderer.Render(100, 100);

        Assert.That(xAxis.Kind, Is.EqualTo(ChartAxisKind.X));
        Assert.That(yAxis.Kind, Is.EqualTo(ChartAxisKind.Y));
        Assert.That(plot.AxisCount, Is.EqualTo(2));
        Assert.That(plot.FindFirstAxis(ChartAxisKind.X)!.Id, Is.EqualTo("axis-bottom"));
        AssertPoint(explicitTransform.MapPoint(5.0, 5.0), 50.0, 50.0);
        AssertPoint(axisTransform.MapPoint(5.0, 5.0), 50.0, 50.0);
        Assert.That(scene.CommandCount, Is.EqualTo(1));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
    }

    [Test]
    public void DocumentConvenience_RendersCartesianSeriesByPlotId()
    {
        ChartDocument document = new ChartDocument(40.0, 40.0);
        ChartPlot plot = document.AddPlotWithBounds("plot", new ChartRect(0.0, 0.0, 40.0, 40.0));
        plot.SetMargins(new ChartPadding(0.0, 0.0, 0.0, 0.0));
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 10.0 }, new double[] { 0.0, 10.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        ChartRenderScene scene = document.RenderCartesian("plot", new ChartCartesianSeries[] { line }, new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        string svg = document.RenderCartesianSvg("plot", new ChartCartesianSeries[] { line }, new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));

        Assert.That(scene.CommandCount, Is.EqualTo(2));
        Assert.That(scene.CommandAt(1).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
        Assert.That(svg, Does.Contain("id=\"line-line-segment-0\""));
    }

    [Test]
    public void ConvenienceApis_ValidateInputs()
    {
        ChartDocument document = new ChartDocument(20.0, 20.0);
        ChartPlot plot = document.AddPlotWithBounds("plot", new ChartRect(0.0, 0.0, 20.0, 20.0));
        ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));

        Assert.Throws<System.ArgumentNullException>(() => document.AddPlotById((string)null!));
        Assert.Throws<System.ArgumentNullException>(() => document.AddPlotWithBounds("bad", null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.AddGridPlot("grid", -1, 0, 1, 1));
        Assert.Throws<System.ArgumentNullException>(() => document.ExportSceneSvg(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.ExportScenePng(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.RenderCartesian(null!, new ChartCartesianSeries[] { line }, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => document.RenderCartesian("plot", null!, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => document.RenderCartesian("plot", new ChartCartesianSeries[] { null! }, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => document.RenderCartesian("missing", new ChartCartesianSeries[] { line }, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => plot.AddNumericAxis(ChartAxisPlacement.Inside, 0.0, 1.0, 2));
        Assert.Throws<System.InvalidOperationException>(() => new ChartPlot("empty").CreateCartesianTransformFromAxes());
        Assert.Throws<System.ArgumentOutOfRangeException>(() => plot.FindFirstAxis((ChartAxisKind)99));
        Assert.Throws<System.ArgumentNullException>(() => plot.CreateCartesianTransform(null!, new ChartRange(0.0, 1.0)));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
