namespace chart.Tests;

[TestFixture]
public class ChartFamilyBuilderTests
{
    [Test]
    public void PieFamilyBuilders_CreateRenderableScenesDirectlyFromValues()
    {
        ChartPieCharts charts = new ChartPieCharts();
        ChartPalette palette = new ChartPalettes().Categorical();
        string[] labels = new string[] { "A", "B", "C" };
        double[] values = new double[] { 4.0, 3.0, 2.0 };

        ChartRenderScene pie = charts.CreatePieChartScene("pie", 100, 100, labels, values, 50.0, 50.0, 40.0, 0.0, ChartPieSliceOrder.Original, palette);
        ChartRenderScene donut = charts.CreateDonutScene("donut", 100, 100, labels, values, 50.0, 50.0, 15.0, 40.0, 0.0, ChartPieSliceOrder.Descending, palette);
        ChartRenderScene rose = charts.CreateRoseScene("rose", 100, 100, labels, values, 50.0, 50.0, 5.0, 40.0, 0.0, ChartPieSliceOrder.Ascending, palette);

        Assert.That(pie.CommandCount, Is.EqualTo(3));
        Assert.That(donut.CommandCount, Is.EqualTo(3));
        Assert.That(rose.CommandCount, Is.EqualTo(3));
        Assert.That(pie.CommandAt(0).Id, Is.EqualTo("pie-slice-0"));
        Assert.That(donut.ToSvgString(), Does.Contain("id=\"donut-slice-2\""));
        Assert.That(rose.ToSvgString(), Does.Contain("id=\"rose-slice-2\""));
    }

    [Test]
    public void PolarFamilyBuilders_CreateExpectedCommandKinds()
    {
        ChartPolarCharts charts = new ChartPolarCharts();
        ChartPolarTransform transform = new ChartPolarTransform(50.0, 50.0, 0.0, 40.0, 0.0, 10.0, 0.0, true);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(50, 100, 150));
        ChartRenderStyle background = ChartRenderStyle.CreateFill(ChartColor.FromRgb(230, 230, 230));

        ChartRenderScene line = charts.CreatePolarLineScene("line", 100, 100, new double[] { 0.0, 90.0 }, new double[] { 10.0, 5.0 }, transform, false, stroke);
        ChartRenderScene scatter = charts.CreatePolarScatterScene("scatter", 100, 100, new double[] { 0.0, 90.0 }, new double[] { 10.0, 5.0 }, transform, ChartMarkerShape.Circle, 4.0, fill);
        ChartRenderScene bars = charts.CreatePolarBarScene("bars", 100, 100, new double[] { 0.0, 90.0 }, new double[] { 10.0, 5.0 }, transform, 30.0, 0.0, fill);
        ChartRenderScene radar = charts.CreateRadarScene("radar", 100, 100, new double[] { 10.0, 5.0, 8.0 }, transform, fill);
        ChartRenderScene bullet = charts.CreateBulletScene("bullet", 120, 40, 10.0, 10.0, 100.0, 20.0, 0.0, 100.0, 60.0, 80.0, ChartOrientation.Horizontal, background, fill, stroke);

        Assert.That(line.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Polyline));
        Assert.That(scatter.CommandCount, Is.EqualTo(2));
        Assert.That(scatter.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Marker));
        Assert.That(bars.CommandCount, Is.EqualTo(2));
        Assert.That(bars.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Path));
        Assert.That(radar.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(bullet.CommandCount, Is.EqualTo(3));
        Assert.That(bullet.CommandAt(2).Id, Is.EqualTo("bullet-target"));
    }

    [Test]
    public void HierarchyFamilyBuilders_CreateRenderableScenesDirectlyFromData()
    {
        ChartHierarchyCharts charts = new ChartHierarchyCharts();
        ChartHierarchyData data = CreateHierarchy();
        ChartPalette palette = new ChartPalettes().Categorical();

        ChartRenderScene treemap = charts.CreateTreemapScene("tree", 100, 60, data, new ChartRect(0.0, 0.0, 100.0, 60.0), palette, ChartTreemapLayoutKind.Squarified);
        ChartRenderScene icicle = charts.CreateIcicleScene("icicle", 100, 60, data, new ChartRect(0.0, 0.0, 100.0, 60.0), palette);
        ChartRenderScene sunburst = charts.CreateSunburstScene("sun", 100, 100, data, 50.0, 50.0, 5.0, 45.0, 0.0, 360.0, palette);

        Assert.That(treemap.CommandCount, Is.EqualTo(3));
        Assert.That(icicle.CommandCount, Is.EqualTo(3));
        Assert.That(sunburst.CommandCount, Is.EqualTo(3));
        Assert.That(treemap.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(icicle.ToSvgString(), Does.Contain("id=\"icicle-item-2\""));
        Assert.That(sunburst.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Path));
    }

    [Test]
    public void TableBuilder_CreatesCellsAndTextDirectlyFromValues()
    {
        ChartCompactCharts charts = new ChartCompactCharts();
        ChartRenderStyle cell = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle text = new ChartRenderStyle();
        text.SetFill(ChartColor.Black());

        ChartRenderScene scene = charts.CreateTableScene("table", 100, 60, new string[] { "A", "B" }, new string[] { "1", "2" }, 1, 2, new ChartRect(0.0, 0.0, 100.0, 60.0), 20.0, cell, text);

        Assert.That(scene.CommandCount, Is.EqualTo(8));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("table-cell-0"));
        Assert.That(scene.ToSvgString(), Does.Contain(">2</text>"));
    }

    static ChartHierarchyData CreateHierarchy()
    {
        return new ChartHierarchyData(
            new ChartHierarchyNode[]
            {
                new ChartHierarchyNode("root", 0.0, -1),
                new ChartHierarchyNode("A", 4.0, 0),
                new ChartHierarchyNode("B", 3.0, 0),
                new ChartHierarchyNode("C", 2.0, 0)
            });
    }
}
