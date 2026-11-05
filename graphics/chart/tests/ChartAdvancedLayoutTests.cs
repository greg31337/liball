namespace chart.Tests;

[TestFixture]
public class ChartAdvancedLayoutTests
{
    [Test]
    public void Facets_CreateRowColumnAndWrappedLayouts()
    {
        ChartDocument rows = CreateDocument(300.0, 300.0);
        ChartDocument columns = CreateDocument(300.0, 300.0);
        ChartDocument wrapped = CreateDocument(300.0, 300.0);
        ChartLayoutEngine engine = new ChartLayoutEngine();

        rows.CreateRowFacets("row", 3);
        columns.CreateColumnFacets("column", 3);
        wrapped.CreateWrapFacets("wrap", 5, 2);

        ChartLayoutResult rowLayout = engine.Layout(rows);
        ChartLayoutResult columnLayout = engine.Layout(columns);
        ChartLayoutResult wrapLayout = engine.Layout(wrapped);

        Assert.That(rows.GetGrid().GetRows(), Is.EqualTo(3));
        Assert.That(rows.GetGrid().GetColumns(), Is.EqualTo(1));
        AssertRect(rowLayout.ItemById("row-2").GetBounds(), 0.0, 200.0, 300.0, 100.0);
        Assert.That(columns.GetGrid().GetRows(), Is.EqualTo(1));
        Assert.That(columns.GetGrid().GetColumns(), Is.EqualTo(3));
        AssertRect(columnLayout.ItemById("column-2").GetBounds(), 200.0, 0.0, 100.0, 300.0);
        Assert.That(wrapped.GetGrid().GetRows(), Is.EqualTo(3));
        Assert.That(wrapped.GetPlotCount(), Is.EqualTo(5));
        AssertRect(wrapLayout.ItemById("wrap-4").GetBounds(), 0.0, 200.0, 150.0, 100.0);
    }

    [Test]
    public void DashboardPanels_UseConfiguredGapsAndSpans()
    {
        ChartDocument document = CreateDocument(620.0, 420.0);
        ChartLayoutEngine engine = new ChartLayoutEngine();

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        document.ConfigureDashboard(2, 3, 10.0, 20.0);
        document.AddDashboardPanel("wide", 0, 0, 1, 2);
        document.AddDashboardPanel("tall", 0, 2, 2, 1);

        ChartLayoutResult layout = engine.Layout(document);

        AssertRect(layout.ItemById("wide").GetBounds(), 10.0, 10.0, 393.3333333333333, 195.0);
        AssertRect(layout.ItemById("tall").GetBounds(), 423.3333333333333, 10.0, 186.6666666666667, 400.0);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.AddDashboardPanel("outside", 1, 2, 2, 1));
    }

    [Test]
    public void OverlayGuides_DoNotReservePlotSpace()
    {
        ChartDocument document = CreateDocument(300.0, 200.0);
        ChartLayoutEngine engine = new ChartLayoutEngine();

        document.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        document.AddLegendLayout("docked", ChartSide.Left, 30.0, 0.0, 5.0);
        document.AddOverlayLegendLayout("overlay-legend", ChartSide.Top, 50.0, 20.0, 4.0);
        document.AddOverlayColorbarLayout("overlay-colorbar", ChartSide.Right, 20.0, 40.0, 4.0);

        ChartLayoutResult layout = engine.Layout(document);

        AssertRect(layout.GetPlotArea(), 45.0, 10.0, 245.0, 180.0);
        AssertRect(layout.ItemById("overlay-legend").GetBounds(), 45.0, 10.0, 50.0, 20.0);
        AssertRect(layout.ItemById("overlay-colorbar").GetBounds(), 270.0, 10.0, 20.0, 40.0);
        Assert.That(document.LayoutRequestAt(1).GetOverlay(), Is.True);
        Assert.That(document.LayoutRequestAt(2).GetOverlay(), Is.True);
    }

    [Test]
    public void ResponsiveTargetAndDevicePixelRatio_ControlLogicalAndPixelSizes()
    {
        ChartDocument document = CreateDocument(100.0, 100.0);

        document.SetResponsiveTargetSize(900.0, 200.0, 320.0, 240.0, 800.0, 600.0);
        document.SetDevicePixelRatio(2.0);
        ChartRenderScene scene = document.CreateScene();

        Assert.That(document.GetWidth(), Is.EqualTo(800.0));
        Assert.That(document.GetHeight(), Is.EqualTo(240.0));
        Assert.That(document.GetDevicePixelRatio(), Is.EqualTo(2.0));
        Assert.That(document.FigureRect().GetWidth(), Is.EqualTo(800.0));
        Assert.That(scene.GetWidth(), Is.EqualTo(1600));
        Assert.That(scene.GetHeight(), Is.EqualTo(480));
    }

    [Test]
    public void TernaryRegion_MapsRelativeVerticesIntoPlotBounds()
    {
        ChartPlot plot = new ChartPlot("ternary");

        plot.SetBounds(new ChartRect(10.0, 20.0, 200.0, 100.0));
        plot.SetMargins(new ChartPadding(10.0, 10.0, 10.0, 10.0));
        plot.SetTernaryRegion(new ChartPoint(0.2, 0.8), new ChartPoint(0.8, 0.8), new ChartPoint(0.5, 0.2));
        ChartTernaryTransform transform = plot.CreateTernaryTransform();

        AssertPoint(transform.Map(1.0, 0.0, 0.0), 56.0, 94.0);
        AssertPoint(transform.Map(0.0, 1.0, 0.0), 164.0, 94.0);
        AssertPoint(transform.Map(0.0, 0.0, 1.0), 110.0, 46.0);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => plot.SetTernaryRegion(new ChartPoint(-0.1, 0.5), new ChartPoint(0.8, 0.8), new ChartPoint(0.5, 0.2)));
    }

    [Test]
    public void PlotClipping_OverridesSeriesClippingForPlotRenderers()
    {
        ChartPlot clippedPlot = new ChartPlot("clipped");
        ChartPlot unclippedPlot = new ChartPlot("unclipped");
        ChartCartesianSeries series = new ChartCartesianSeries("points", ChartCartesianSeriesKind.Scatter);
        ChartRange domain = new ChartRange(0.0, 10.0);

        clippedPlot.SetBounds(new ChartRect(0.0, 0.0, 20.0, 20.0));
        unclippedPlot.SetBounds(new ChartRect(0.0, 0.0, 20.0, 20.0));
        unclippedPlot.SetPlotClipping(false);
        series.AddMarker("outside", ChartMarkerShape.Circle, 20.0, 20.0, 4.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        ChartCartesianRenderer clippedRenderer = clippedPlot.CreateCartesianRenderer(domain, domain);
        ChartCartesianRenderer unclippedRenderer = unclippedPlot.CreateCartesianRenderer(domain, domain);
        clippedRenderer.AddSeries(series);
        unclippedRenderer.AddSeries(series);

        ChartRenderScene clipped = clippedRenderer.Render(40, 40);
        ChartRenderScene unclipped = unclippedRenderer.Render(40, 40);

        Assert.That(clipped.GetCommandCount(), Is.EqualTo(0));
        Assert.That(unclipped.GetCommandCount(), Is.EqualTo(1));
        Assert.That(unclipped.CommandAt(0).GetX1(), Is.EqualTo(40.0));
        Assert.That(unclipped.CommandAt(0).GetY1(), Is.EqualTo(-20.0));
        Assert.That(unclippedPlot.GetPlotClipping(), Is.False);
    }

    private static ChartDocument CreateDocument(double width, double height)
    {
        ChartDocument document = new ChartDocument(width, height);
        document.SetMargins(new ChartPadding(0.0, 0.0, 0.0, 0.0));
        return document;
    }

    private static void AssertRect(ChartRect rect, double x, double y, double width, double height)
    {
        Assert.That(rect.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(rect.GetY(), Is.EqualTo(y).Within(0.0000001));
        Assert.That(rect.GetWidth(), Is.EqualTo(width).Within(0.0000001));
        Assert.That(rect.GetHeight(), Is.EqualTo(height).Within(0.0000001));
    }

    private static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.GetY(), Is.EqualTo(y).Within(0.0000001));
    }
}
