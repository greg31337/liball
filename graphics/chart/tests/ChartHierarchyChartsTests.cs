namespace chart.Tests;

[TestFixture]
public class ChartHierarchyChartsTests
{
    [Test]
    public void HierarchyData_ComputesChildrenTotalsAndDepths()
    {
        ChartHierarchyData data = CreateHierarchy();

        Assert.That(data.NodeCount, Is.EqualTo(6));
        Assert.That(data.RootIndex, Is.EqualTo(0));
        Assert.That(data.ChildCount(0), Is.EqualTo(2));
        Assert.That(data.ChildIndex(0, 1), Is.EqualTo(2));
        Assert.That(data.NodeTotal(0), Is.EqualTo(10.0).Within(0.0000001));
        Assert.That(data.NodeTotal(1), Is.EqualTo(4.0).Within(0.0000001));
        Assert.That(data.NodeDepth(5), Is.EqualTo(2));
        Assert.That(data.MaximumDepth(), Is.EqualTo(2));
    }

    [Test]
    public void TreemapLayout_SlicesChildrenBySubtreeValue()
    {
        ChartHierarchyData data = CreateHierarchy();
        ChartHierarchyRectLayout layout = ChartHierarchyChartsOperations.LayoutTreemap(data, new ChartRect(0.0, 0.0, 100.0, 50.0), CreatePalette());

        Assert.That(layout.ItemCount, Is.EqualTo(5));
        AssertRect(layout.ItemAt(0).Bounds, 0.0, 0.0, 40.0, 50.0);
        Assert.That(layout.ItemAt(0).Label, Is.EqualTo("A"));
        Assert.That(layout.ItemAt(0).Depth, Is.EqualTo(1));
        AssertRect(layout.ItemAt(1).Bounds, 0.0, 0.0, 40.0, 25.0);
        AssertRect(layout.ItemAt(2).Bounds, 0.0, 25.0, 40.0, 25.0);
        AssertRect(layout.ItemAt(3).Bounds, 40.0, 0.0, 60.0, 50.0);
        AssertRect(layout.ItemAt(4).Bounds, 40.0, 0.0, 60.0, 50.0);
        AssertColor(layout.ItemAt(3).Color, 20, 0, 0, 255);
    }

    [Test]
    public void TreemapLayoutKinds_ApplySliceDiceAndAlternatingDirections()
    {
        ChartHierarchyCharts charts = new ChartHierarchyCharts();
        ChartHierarchyData data = CreateHierarchy();
        ChartRect bounds = new ChartRect(0.0, 0.0, 100.0, 50.0);
        ChartHierarchyRectLayout slice = charts.LayoutTreemapByKind(data, bounds, CreatePalette(), ChartTreemapLayoutKind.Slice);
        ChartHierarchyRectLayout dice = charts.LayoutTreemapByKind(data, bounds, CreatePalette(), ChartTreemapLayoutKind.Dice);
        ChartHierarchyRectLayout alternating = charts.LayoutTreemapByKind(data, bounds, CreatePalette(), ChartTreemapLayoutKind.SliceDice);

        AssertRect(slice.ItemAt(0).Bounds, 0.0, 0.0, 100.0, 20.0);
        AssertRect(slice.ItemAt(1).Bounds, 0.0, 0.0, 100.0, 10.0);
        AssertRect(slice.ItemAt(2).Bounds, 0.0, 10.0, 100.0, 10.0);
        AssertRect(dice.ItemAt(0).Bounds, 0.0, 0.0, 40.0, 50.0);
        AssertRect(dice.ItemAt(1).Bounds, 0.0, 0.0, 20.0, 50.0);
        AssertRect(dice.ItemAt(2).Bounds, 20.0, 0.0, 20.0, 50.0);
        AssertRect(alternating.ItemAt(0).Bounds, 0.0, 0.0, 40.0, 50.0);
        AssertRect(alternating.ItemAt(1).Bounds, 0.0, 0.0, 40.0, 25.0);
        AssertRect(alternating.ItemAt(2).Bounds, 0.0, 25.0, 40.0, 25.0);
    }

    [Test]
    public void SquarifiedTreemap_GroupsValuesIntoLowerAspectRows()
    {
        ChartHierarchyRectLayout layout = new ChartHierarchyCharts().LayoutTreemapByKind(
            CreateFlatHierarchy(),
            new ChartRect(0.0, 0.0, 100.0, 60.0),
            CreatePalette(),
            ChartTreemapLayoutKind.Squarified);

        Assert.That(layout.GetItemCount(), Is.EqualTo(3));
        Assert.That(layout.ItemAt(0).GetLabel(), Is.EqualTo("large"));
        AssertRect(layout.ItemAt(0).GetBounds(), 0.0, 0.0, 60.0, 60.0);
        AssertRect(layout.ItemAt(1).GetBounds(), 60.0, 0.0, 40.0, 45.0);
        AssertRect(layout.ItemAt(2).GetBounds(), 60.0, 45.0, 40.0, 15.0);
        Assert.That(layout.ItemAt(0).GetBounds().Width * layout.ItemAt(0).GetBounds().Height, Is.EqualTo(3600.0).Within(0.0000001));
        Assert.That(layout.ItemAt(1).GetBounds().Width * layout.ItemAt(1).GetBounds().Height, Is.EqualTo(1800.0).Within(0.0000001));
        Assert.That(layout.ItemAt(2).GetBounds().Width * layout.ItemAt(2).GetBounds().Height, Is.EqualTo(600.0).Within(0.0000001));
    }

    [Test]
    public void IcicleLayout_UsesDepthBandsAndParentWidths()
    {
        ChartHierarchyData data = CreateHierarchy();
        ChartHierarchyRectLayout layout = ChartHierarchyChartsOperations.LayoutIcicle(data, new ChartRect(0.0, 0.0, 100.0, 50.0), CreatePalette());

        Assert.That(layout.ItemCount, Is.EqualTo(5));
        AssertRect(layout.ItemAt(0).Bounds, 0.0, 0.0, 40.0, 25.0);
        AssertRect(layout.ItemAt(1).Bounds, 0.0, 25.0, 20.0, 25.0);
        AssertRect(layout.ItemAt(2).Bounds, 20.0, 25.0, 20.0, 25.0);
        AssertRect(layout.ItemAt(3).Bounds, 40.0, 0.0, 60.0, 25.0);
        AssertRect(layout.ItemAt(4).Bounds, 40.0, 25.0, 60.0, 25.0);
    }

    [Test]
    public void SunburstLayout_AllocatesAnglesAndRings()
    {
        ChartHierarchyData data = CreateHierarchy();
        ChartSunburstLayout layout = ChartHierarchyChartsOperations.LayoutSunburst(data, 50.0, 60.0, 0.0, 40.0, 0.0, 360.0, CreatePalette());

        Assert.That(layout.CenterX, Is.EqualTo(50.0).Within(0.0000001));
        Assert.That(layout.CenterY, Is.EqualTo(60.0).Within(0.0000001));
        Assert.That(layout.ItemCount, Is.EqualTo(5));
        AssertArc(layout.ItemAt(0), 1, "A", 4.0, 1, 0.0, 144.0, 0.0, 20.0);
        AssertArc(layout.ItemAt(1), 3, "A1", 2.0, 2, 0.0, 72.0, 20.0, 40.0);
        AssertArc(layout.ItemAt(2), 4, "A2", 2.0, 2, 72.0, 144.0, 20.0, 40.0);
        AssertArc(layout.ItemAt(3), 2, "B", 6.0, 1, 144.0, 360.0, 0.0, 20.0);
        AssertArc(layout.ItemAt(4), 5, "B1", 6.0, 2, 144.0, 360.0, 20.0, 40.0);
    }

    [Test]
    public void HierarchyLayouts_RenderToScene()
    {
        ChartHierarchyData data = CreateHierarchy();
        ChartHierarchyRectLayout treemap = ChartHierarchyChartsOperations.LayoutTreemap(data, new ChartRect(0.0, 0.0, 100.0, 50.0), CreatePalette());
        ChartSunburstLayout sunburst = ChartHierarchyChartsOperations.LayoutSunburst(data, 50.0, 50.0, 5.0, 45.0, 0.0, 360.0, CreatePalette());
        ChartRenderScene scene = new ChartRenderScene(120, 120);

        ChartHierarchyChartsOperations.AddRectLayoutToScene(scene, "tree", treemap);
        ChartHierarchyChartsOperations.AddSunburstToScene(scene, "sun", sunburst);

        Assert.That(scene.CommandCount, Is.EqualTo(10));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("tree-item-0"));
        Assert.That(scene.CommandAt(5).Kind, Is.EqualTo(ChartRenderCommandKind.Path));
        Assert.That(scene.CommandAt(5).Id, Is.EqualTo("sun-item-0"));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"sun-item-4\""));
    }

    [Test]
    public void HierarchyCharts_ValidateInputs()
    {
        ChartHierarchyData data = CreateHierarchy();
        ChartPalette palette = CreatePalette();
        ChartRect bounds = new ChartRect(0.0, 0.0, 10.0, 10.0);
        ChartRenderScene scene = new ChartRenderScene(20, 20);

        Assert.Throws<System.ArgumentNullException>(() => new ChartHierarchyData(null!));
        Assert.Throws<System.ArgumentException>(() => new ChartHierarchyData(new ChartHierarchyNode[0]));
        Assert.Throws<System.ArgumentException>(() => new ChartHierarchyData(new ChartHierarchyNode[] { new ChartHierarchyNode("root", 1.0, -1), null! }));
        Assert.Throws<System.ArgumentException>(() => new ChartHierarchyData(new ChartHierarchyNode[] { new ChartHierarchyNode("root", 1.0, -1), new ChartHierarchyNode("other", 1.0, -1) }));
        Assert.Throws<System.ArgumentException>(() => new ChartHierarchyData(new ChartHierarchyNode[] { new ChartHierarchyNode("orphan", 1.0, 1) }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartHierarchyNode("bad", -1.0, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => data.NodeAt(99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => data.ChildIndex(0, 99));
        Assert.Throws<System.ArgumentNullException>(() => ChartHierarchyChartsOperations.LayoutTreemap(null!, bounds, palette));
        Assert.Throws<System.ArgumentException>(() => ChartHierarchyChartsOperations.LayoutTreemap(data, new ChartRect(0.0, 0.0, 0.0, 10.0), palette));
        Assert.Throws<System.ArgumentNullException>(() => ChartHierarchyChartsOperations.LayoutTreemap(data, bounds, null!));
        Assert.Throws<System.ArgumentException>(() => ChartHierarchyChartsOperations.LayoutTreemap(CreateZeroHierarchy(), bounds, palette));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartHierarchyChartsOperations.LayoutTreemapByKind(data, bounds, palette, (ChartTreemapLayoutKind)99));
        Assert.Throws<System.ArgumentException>(() => ChartHierarchyChartsOperations.LayoutIcicle(new ChartHierarchyData(new ChartHierarchyNode[] { new ChartHierarchyNode("root", 1.0, -1) }), bounds, palette));
        Assert.Throws<System.ArgumentException>(() => ChartHierarchyChartsOperations.LayoutSunburst(new ChartHierarchyData(new ChartHierarchyNode[] { new ChartHierarchyNode("root", 1.0, -1) }), 0.0, 0.0, 1.0, 2.0, 0.0, 360.0, palette));
        Assert.Throws<System.ArgumentException>(() => ChartHierarchyChartsOperations.LayoutSunburst(data, 0.0, 0.0, 2.0, 2.0, 0.0, 360.0, palette));
        Assert.Throws<System.ArgumentNullException>(() => ChartHierarchyChartsOperations.AddRectLayoutToScene(null!, "tree", ChartHierarchyChartsOperations.LayoutTreemap(data, bounds, palette)));
        Assert.Throws<System.ArgumentNullException>(() => ChartHierarchyChartsOperations.AddRectLayoutToScene(scene, "tree", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartHierarchyChartsOperations.AddSunburstToScene(scene, "sun", null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartHierarchyRectLayout(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartSunburstLayout(0.0, 0.0, 0));
    }

    static ChartHierarchyData CreateHierarchy()
    {
        return new ChartHierarchyData(
            new ChartHierarchyNode[]
            {
                new ChartHierarchyNode("root", 0.0, -1),
                new ChartHierarchyNode("A", 0.0, 0),
                new ChartHierarchyNode("B", 0.0, 0),
                new ChartHierarchyNode("A1", 2.0, 1),
                new ChartHierarchyNode("A2", 2.0, 1),
                new ChartHierarchyNode("B1", 6.0, 2)
            });
    }

    static ChartHierarchyData CreateZeroHierarchy()
    {
        return new ChartHierarchyData(
            new ChartHierarchyNode[]
            {
                new ChartHierarchyNode("root", 0.0, -1),
                new ChartHierarchyNode("empty", 0.0, 0)
            });
    }

    static ChartHierarchyData CreateFlatHierarchy()
    {
        return new ChartHierarchyData(
            new ChartHierarchyNode[]
            {
                new ChartHierarchyNode("root", 0.0, -1),
                new ChartHierarchyNode("large", 6.0, 0),
                new ChartHierarchyNode("medium", 3.0, 0),
                new ChartHierarchyNode("small", 1.0, 0)
            });
    }

    static ChartPalette CreatePalette()
    {
        return new ChartPalette(
            "hierarchy",
            new ChartColor[]
            {
                ChartColor.FromRgb(0, 0, 0),
                ChartColor.FromRgb(10, 0, 0),
                ChartColor.FromRgb(20, 0, 0),
                ChartColor.FromRgb(30, 0, 0),
                ChartColor.FromRgb(40, 0, 0),
                ChartColor.FromRgb(50, 0, 0)
            });
    }

    static void AssertRect(ChartRect rect, double x, double y, double width, double height)
    {
        Assert.That(rect.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(rect.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(rect.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(rect.Height, Is.EqualTo(height).Within(0.0000001));
    }

    static void AssertArc(ChartHierarchyArcItem item, int nodeIndex, string label, double value, int depth, double startAngle, double endAngle, double innerRadius, double outerRadius)
    {
        Assert.That(item.NodeIndex, Is.EqualTo(nodeIndex));
        Assert.That(item.Label, Is.EqualTo(label));
        Assert.That(item.Value, Is.EqualTo(value).Within(0.0000001));
        Assert.That(item.Depth, Is.EqualTo(depth));
        Assert.That(item.StartAngle, Is.EqualTo(startAngle).Within(0.0000001));
        Assert.That(item.EndAngle, Is.EqualTo(endAngle).Within(0.0000001));
        Assert.That(item.InnerRadius, Is.EqualTo(innerRadius).Within(0.0000001));
        Assert.That(item.OuterRadius, Is.EqualTo(outerRadius).Within(0.0000001));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
