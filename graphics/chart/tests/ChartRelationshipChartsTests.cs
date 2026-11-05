namespace chart.Tests;

[TestFixture]
public class ChartRelationshipChartsTests
{
    [Test]
    public void SankeyLayout_ComputesLayersNodeSizesAndLinks()
    {
        string[] labels = new string[] { "A", "B", "C", "D" };
        ChartGraphLink[] links = new ChartGraphLink[]
        {
            new ChartGraphLink(0, 2, 2.0),
            new ChartGraphLink(1, 2, 1.0),
            new ChartGraphLink(2, 3, 3.0)
        };

        ChartGraphLayout layout = ChartRelationshipChartsOperations.LayoutSankey(labels, links, new ChartRect(0.0, 0.0, 100.0, 60.0), 10.0, 4.0);

        Assert.That(layout.NodeCount, Is.EqualTo(4));
        Assert.That(layout.LinkCount, Is.EqualTo(3));
        AssertNode(layout.NodeAt(0), 0, "A", 0.0, 0.0, 10.0, 37.333333333333336, 0);
        AssertNode(layout.NodeAt(1), 1, "B", 0.0, 41.333333333333336, 10.0, 18.666666666666668, 0);
        AssertNode(layout.NodeAt(2), 2, "C", 45.0, 0.0, 10.0, 60.0, 1);
        AssertNode(layout.NodeAt(3), 3, "D", 90.0, 0.0, 10.0, 60.0, 2);
        AssertLink(layout.LinkAt(0), 0, 2, 10.0, 18.666666666666668, 45.0, 30.0, 2.0, 2.0);
        Assert.That(ChartRelationshipChartsOperations.SankeyLayers(4, links), Is.EqualTo(new int[] { 0, 0, 1, 2 }));
    }

    [Test]
    public void NodeLinkLayout_PlacesNodesOnCircleAndPreservesLinks()
    {
        string[] labels = new string[] { "A", "B", "C", "D" };
        ChartGraphLink[] links = new ChartGraphLink[] { new ChartGraphLink(0, 1, 2.0), new ChartGraphLink(2, 3, 1.0) };
        ChartGraphLayout layout = ChartRelationshipChartsOperations.LayoutNodeLink(labels, links, new ChartRect(0.0, 0.0, 100.0, 100.0));

        Assert.That(layout.NodeCount, Is.EqualTo(4));
        AssertPoint(new ChartPoint(layout.NodeAt(0).X, layout.NodeAt(0).Y), 50.0, 0.0);
        AssertPoint(new ChartPoint(layout.NodeAt(1).X, layout.NodeAt(1).Y), 100.0, 50.0);
        AssertPoint(new ChartPoint(layout.NodeAt(2).X, layout.NodeAt(2).Y), 50.0, 100.0);
        AssertPoint(new ChartPoint(layout.NodeAt(3).X, layout.NodeAt(3).Y), 0.0, 50.0);
        AssertLink(layout.LinkAt(0), 0, 1, 50.0, 0.0, 100.0, 50.0, 2.0, 2.0);
    }

    [Test]
    public void ForceDirectedLayout_IsSeededDeterministicAndBounded()
    {
        string[] labels = new string[] { "A", "B", "C", "D" };
        ChartGraphLink[] links = new ChartGraphLink[]
        {
            new ChartGraphLink(0, 1, 1.0),
            new ChartGraphLink(1, 2, 2.0),
            new ChartGraphLink(2, 3, 1.0),
            new ChartGraphLink(3, 0, 2.0)
        };
        ChartRect bounds = new ChartRect(10.0, 20.0, 200.0, 120.0);
        ChartRelationshipCharts charts = new ChartRelationshipCharts();

        ChartGraphLayout first = charts.LayoutForceDirected(labels, links, bounds, 12345, 120, 45.0, 500.0);
        ChartGraphLayout repeated = charts.LayoutForceDirected(labels, links, bounds, 12345, 120, 45.0, 500.0);
        ChartGraphLayout different = charts.LayoutForceDirected(labels, links, bounds, 54321, 120, 45.0, 500.0);

        Assert.That(first.GetNodeCount(), Is.EqualTo(4));
        Assert.That(first.GetLinkCount(), Is.EqualTo(4));
        bool foundDifferentPosition = false;
        int index = 0;
        while (index < first.GetNodeCount())
        {
            ChartGraphNodeLayout node = first.NodeAt(index);
            Assert.That(node.GetX(), Is.EqualTo(repeated.NodeAt(index).GetX()));
            Assert.That(node.GetY(), Is.EqualTo(repeated.NodeAt(index).GetY()));
            Assert.That(node.GetX(), Is.InRange(10.0, 210.0));
            Assert.That(node.GetY(), Is.InRange(20.0, 140.0));
            if (node.GetX() != different.NodeAt(index).GetX() || node.GetY() != different.NodeAt(index).GetY())
            {
                foundDifferentPosition = true;
            }

            index = index + 1;
        }

        Assert.That(foundDifferentPosition, Is.True);
        Assert.That(first.LinkAt(0).GetX1(), Is.EqualTo(first.NodeAt(0).GetX()));
        Assert.That(first.LinkAt(0).GetY1(), Is.EqualTo(first.NodeAt(0).GetY()));
        Assert.That(first.LinkAt(0).GetX2(), Is.EqualTo(first.NodeAt(1).GetX()));
        Assert.That(first.LinkAt(0).GetY2(), Is.EqualTo(first.NodeAt(1).GetY()));
    }

    [Test]
    public void TreeAndDendrogramLayouts_ProjectHierarchy()
    {
        ChartHierarchyData data = CreateHierarchy();
        ChartGraphLayout tree = ChartRelationshipChartsOperations.LayoutTree(data, new ChartRect(0.0, 0.0, 100.0, 60.0), ChartOrientation.Horizontal);
        ChartGraphLayout dendrogram = ChartRelationshipChartsOperations.LayoutDendrogram(data, new ChartRect(0.0, 0.0, 100.0, 60.0), ChartOrientation.Horizontal);

        Assert.That(tree.NodeCount, Is.EqualTo(4));
        Assert.That(tree.LinkCount, Is.EqualTo(3));
        AssertPoint(new ChartPoint(tree.NodeAt(0).X, tree.NodeAt(0).Y), 0.0, 0.0);
        AssertPoint(new ChartPoint(tree.NodeAt(1).X, tree.NodeAt(1).Y), 50.0, 20.0);
        AssertPoint(new ChartPoint(tree.NodeAt(3).X, tree.NodeAt(3).Y), 100.0, 60.0);
        AssertLink(tree.LinkAt(0), 0, 1, 0.0, 0.0, 50.0, 20.0, 2.0, 1.0);
        AssertPoint(new ChartPoint(dendrogram.NodeAt(1).X, dendrogram.NodeAt(1).Y), 50.0, 20.0);
        AssertPoint(new ChartPoint(dendrogram.NodeAt(2).X, dendrogram.NodeAt(2).Y), 100.0, 40.0);
    }

    [Test]
    public void ParallelCoordinatesLayout_MapsRowsAcrossAxes()
    {
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 3, new double[] { 0.0, 10.0, 5.0, 10.0, 20.0, 5.0 });
        ChartParallelCoordinatesLayout layout = ChartRelationshipChartsOperations.LayoutParallelCoordinates(new string[] { "A", "B", "C" }, values, new ChartRect(0.0, 0.0, 100.0, 100.0));

        Assert.That(layout.AxisCount, Is.EqualTo(3));
        Assert.That(layout.RecordCount, Is.EqualTo(2));
        Assert.That(layout.AxisAt(0).Minimum, Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(layout.AxisAt(1).Maximum, Is.EqualTo(20.0).Within(0.0000001));
        AssertPoint(layout.RecordAt(0).Points[0], 0.0, 100.0);
        AssertPoint(layout.RecordAt(0).Points[1], 50.0, 100.0);
        AssertPoint(layout.RecordAt(0).Points[2], 100.0, 50.0);
        AssertPoint(layout.RecordAt(1).Points[0], 0.0, 0.0);
        AssertPoint(layout.RecordAt(1).Points[1], 50.0, 0.0);
    }

    [Test]
    public void ParallelCategoriesLayout_CreatesBandsAndRowFlows()
    {
        ChartParallelCategoriesLayout layout = ChartRelationshipChartsOperations.LayoutParallelCategories(
            new string[] { "A", "B" },
            new string[] { "x", "m", "x", "n", "y", "m" },
            3,
            2,
            new ChartRect(0.0, 0.0, 100.0, 90.0));

        Assert.That(layout.AxisCount, Is.EqualTo(2));
        Assert.That(layout.RowCount, Is.EqualTo(3));
        Assert.That(layout.BandCount, Is.EqualTo(4));
        Assert.That(layout.FlowCount, Is.EqualTo(3));
        AssertBand(layout.BandAt(0), 0, 0, "x", 0.0, 0.0, 60.0, 2);
        AssertBand(layout.BandAt(1), 0, 1, "y", 0.0, 60.0, 30.0, 1);
        AssertBand(layout.BandAt(2), 1, 0, "m", 100.0, 0.0, 60.0, 2);
        AssertBand(layout.BandAt(3), 1, 1, "n", 100.0, 60.0, 30.0, 1);
        Assert.That(layout.FlowAt(1).SourceBandIndex, Is.EqualTo(0));
        Assert.That(layout.FlowAt(1).TargetBandIndex, Is.EqualTo(3));
        AssertPoint(new ChartPoint(layout.FlowAt(2).X1, layout.FlowAt(2).Y1), 0.0, 75.0);
        AssertPoint(new ChartPoint(layout.FlowAt(2).X2, layout.FlowAt(2).Y2), 100.0, 30.0);
    }

    [Test]
    public void GraphSceneRenderer_SupportsEveryGraphLayoutFamily()
    {
        ChartRelationshipCharts charts = new ChartRelationshipCharts();
        string[] labels = new string[] { "A", "B", "C" };
        ChartGraphLink[] links = new ChartGraphLink[] { new ChartGraphLink(0, 1, 2.0), new ChartGraphLink(1, 2, 1.0) };
        ChartRect bounds = new ChartRect(10.0, 10.0, 100.0, 80.0);
        ChartHierarchyData hierarchy = CreateHierarchy();
        ChartRenderStyle nodeStyle = ChartRenderStyle.CreateFill(ChartColor.FromRgb(40, 120, 200));
        ChartRenderStyle linkStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 2.0);
        ChartRenderStyle labelStyle = new ChartRenderStyle();
        labelStyle.SetFill(ChartColor.Black());
        ChartGraphLayout[] layouts = new ChartGraphLayout[]
        {
            charts.LayoutSankey(labels, links, bounds, 10.0, 4.0),
            charts.LayoutNodeLink(labels, links, bounds),
            charts.LayoutForceDirected(labels, links, bounds, 123, 20, 25.0, 100.0),
            charts.LayoutTree(hierarchy, bounds, ChartOrientation.Horizontal),
            charts.LayoutDendrogram(hierarchy, bounds, ChartOrientation.Vertical)
        };
        string[] ids = new string[] { "sankey", "node", "force", "tree", "dendrogram" };

        int index = 0;
        while (index < layouts.Length)
        {
            ChartRenderScene scene = charts.CreateGraphScene(ids[index], 130, 110, layouts[index], 8.0, nodeStyle, linkStyle, labelStyle);

            Assert.That(scene.CommandCount, Is.EqualTo(layouts[index].GetLinkCount() + layouts[index].GetNodeCount() * 2));
            Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Line));
            Assert.That(scene.ToSvgString(), Does.Contain("id=\"" + ids[index] + "-label-0\""));
            Assert.That(scene.ToRawBitmap().GetWidth(), Is.EqualTo(130));
            index = index + 1;
        }

        ChartRenderScene sankey = charts.CreateGraphScene("weighted", 130, 110, layouts[0], 8.0, nodeStyle, linkStyle, labelStyle);
        ChartRenderScene nodeLink = charts.CreateGraphScene("points", 130, 110, layouts[1], 8.0, nodeStyle, linkStyle, labelStyle);
        Assert.That(sankey.CommandAt(0).Style.GetStrokeWidth(), Is.EqualTo(4.0).Within(0.0000001));
        Assert.That(sankey.CommandAt(layouts[0].GetLinkCount()).Kind, Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(nodeLink.CommandAt(layouts[1].GetLinkCount()).Kind, Is.EqualTo(ChartRenderCommandKind.Marker));
    }

    [Test]
    public void ParallelCoordinateSceneRenderer_DrawsRecordsAxesAndLabels()
    {
        ChartRelationshipCharts charts = new ChartRelationshipCharts();
        ChartRect bounds = new ChartRect(10.0, 10.0, 100.0, 80.0);
        ChartDataMatrix values = ChartDataMatrix.FromValues(2, 3, new double[] { 0.0, 10.0, 5.0, 10.0, 20.0, 0.0 });
        ChartParallelCoordinatesLayout layout = charts.LayoutParallelCoordinates(new string[] { "A", "B", "C" }, values, bounds);
        ChartRenderStyle axisStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle recordStyle = ChartRenderStyle.CreateStroke(ChartColor.FromRgb(30, 100, 180), 2.0);
        ChartRenderStyle labelStyle = new ChartRenderStyle();
        labelStyle.SetFill(ChartColor.Black());

        ChartRenderScene scene = charts.CreateParallelCoordinatesScene("parallel", 130, 110, layout, bounds, axisStyle, recordStyle, labelStyle);

        Assert.That(scene.CommandCount, Is.EqualTo(8));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Polyline));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("parallel-record-0"));
        Assert.That(scene.CommandAt(2).Id, Is.EqualTo("parallel-axis-0"));
        Assert.That(scene.CommandAt(3).Id, Is.EqualTo("parallel-label-0"));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"parallel-record-1\""));
        Assert.That(scene.ToRawBitmap().GetHeight(), Is.EqualTo(110));
    }

    [Test]
    public void ParallelCategorySceneRenderer_DrawsFlowsBandsAndLabels()
    {
        ChartRelationshipCharts charts = new ChartRelationshipCharts();
        ChartParallelCategoriesLayout layout = charts.LayoutParallelCategories(new string[] { "A", "B" }, new string[] { "x", "m", "x", "n", "y", "m" }, 3, 2, new ChartRect(10.0, 10.0, 100.0, 90.0));
        ChartRenderStyle bandStyle = ChartRenderStyle.CreateFill(ChartColor.FromRgb(60, 140, 210));
        ChartRenderStyle flowStyle = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle labelStyle = new ChartRenderStyle();
        labelStyle.SetFill(ChartColor.Black());

        ChartRenderScene scene = charts.CreateParallelCategoriesScene("categories", 130, 120, layout, 8.0, bandStyle, flowStyle, labelStyle);

        Assert.That(scene.CommandCount, Is.EqualTo(11));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("categories-flow-0"));
        Assert.That(scene.CommandAt(3).Kind, Is.EqualTo(ChartRenderCommandKind.Rect));
        Assert.That(scene.CommandAt(3).Id, Is.EqualTo("categories-band-0"));
        Assert.That(scene.CommandAt(4).Id, Is.EqualTo("categories-label-0"));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"categories-band-3\""));
        Assert.That(scene.ToRawBitmap().GetWidth(), Is.EqualTo(130));
    }

    [Test]
    public void RelationshipCharts_ValidateInputs()
    {
        ChartGraphLink[] links = new ChartGraphLink[] { new ChartGraphLink(0, 1, 1.0) };
        ChartRect bounds = new ChartRect(0.0, 0.0, 10.0, 10.0);
        ChartDataMatrix values = ChartDataMatrix.FromValues(1, 2, new double[] { 1.0, 2.0 });
        ChartHierarchyData hierarchy = CreateHierarchy();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartGraphLink(-1, 0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartGraphLink(0, 1, -1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartGraphLayout(0, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartParallelCoordinatesLayout(0, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartParallelCategoriesLayout(0, 0, 0, 0));
        Assert.Throws<System.ArgumentNullException>(() => ChartRelationshipChartsOperations.LayoutSankey(null!, links, bounds, 1.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutSankey(new string[] { "A" }, links, bounds, 1.0, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartRelationshipChartsOperations.LayoutSankey(new string[] { "A", "B" }, null!, bounds, 1.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutSankey(new string[] { "A", "B" }, links, bounds, 0.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => ChartRelationshipChartsOperations.LayoutSankey(new string[] { "A", "B", "C" }, new ChartGraphLink[] { new ChartGraphLink(0, 2, 1.0), new ChartGraphLink(1, 2, 1.0) }, new ChartRect(0.0, 0.0, 10.0, 1.0), 1.0, 2.0));
        Assert.Throws<System.ArgumentNullException>(() => ChartRelationshipChartsOperations.LayoutTree(null!, bounds, ChartOrientation.Horizontal));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutTree(hierarchy, bounds, ChartOrientation.Radial));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutForceDirected(new string[] { "A", "B" }, links, bounds, 1, 0, 5.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutForceDirected(new string[] { "A", "B" }, links, bounds, 1, 10, 0.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutForceDirected(new string[] { "A", "B" }, links, bounds, 1, 10, 5.0, -1.0));
        Assert.Throws<System.ArgumentException>(() => ChartRelationshipChartsOperations.LayoutParallelCoordinates(new string[] { "A" }, values, bounds));
        Assert.Throws<System.ArgumentNullException>(() => ChartRelationshipChartsOperations.LayoutParallelCoordinates(new string[] { "A", "B" }, null!, bounds));
        Assert.Throws<System.ArgumentException>(() => ChartRelationshipChartsOperations.LayoutParallelCategories(new string[] { "A" }, new string[] { "x", "y" }, 1, 2, bounds));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartRelationshipChartsOperations.LayoutParallelCategories(new string[] { "A", "B" }, new string[] { "x", "y" }, 1, 1, bounds));
        Assert.Throws<System.ArgumentException>(() => ChartRelationshipChartsOperations.LayoutParallelCategories(new string[] { "A", "B" }, new string[] { "x" }, 1, 2, bounds));
        Assert.Throws<System.ArgumentException>(() => ChartRelationshipChartsOperations.MaximumInt(new int[0]));

        ChartRelationshipCharts charts = new ChartRelationshipCharts();
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartGraphLayout graph = charts.LayoutNodeLink(new string[] { "A", "B" }, links, bounds);
        ChartParallelCoordinatesLayout coordinates = charts.LayoutParallelCoordinates(new string[] { "A", "B" }, values, bounds);
        ChartParallelCategoriesLayout categories = charts.LayoutParallelCategories(new string[] { "A", "B" }, new string[] { "x", "y" }, 1, 2, bounds);
        Assert.Throws<System.ArgumentNullException>(() => charts.AddGraphLayoutToScene(null!, "graph", graph, 4.0, style, style, style));
        Assert.Throws<System.ArgumentNullException>(() => charts.AddGraphLayoutToScene(new ChartRenderScene(10, 10), "graph", null!, 4.0, style, style, style));
        Assert.Throws<System.ArgumentNullException>(() => charts.AddParallelCoordinatesToScene(new ChartRenderScene(10, 10), "parallel", null!, bounds, style, style, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => charts.AddParallelCategoriesToScene(new ChartRenderScene(10, 10), "categories", categories, 0.0, style, style, style));
        Assert.Throws<System.ArgumentNullException>(() => charts.CreateParallelCoordinatesScene("parallel", 10, 10, coordinates, bounds, null!, style, style));
    }

    static ChartHierarchyData CreateHierarchy()
    {
        return new ChartHierarchyData(
            new ChartHierarchyNode[]
            {
                new ChartHierarchyNode("root", 0.0, -1),
                new ChartHierarchyNode("A", 0.0, 0),
                new ChartHierarchyNode("B", 1.0, 0),
                new ChartHierarchyNode("A1", 2.0, 1)
            });
    }

    static void AssertNode(ChartGraphNodeLayout node, int nodeIndex, string label, double x, double y, double width, double height, int layer)
    {
        Assert.That(node.NodeIndex, Is.EqualTo(nodeIndex));
        Assert.That(node.Label, Is.EqualTo(label));
        Assert.That(node.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(node.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(node.Width, Is.EqualTo(width).Within(0.0000001));
        Assert.That(node.Height, Is.EqualTo(height).Within(0.0000001));
        Assert.That(node.Layer, Is.EqualTo(layer));
    }

    static void AssertLink(ChartGraphLinkLayout link, int sourceIndex, int targetIndex, double x1, double y1, double x2, double y2, double value, double width)
    {
        Assert.That(link.SourceIndex, Is.EqualTo(sourceIndex));
        Assert.That(link.TargetIndex, Is.EqualTo(targetIndex));
        Assert.That(link.X1, Is.EqualTo(x1).Within(0.0000001));
        Assert.That(link.Y1, Is.EqualTo(y1).Within(0.0000001));
        Assert.That(link.X2, Is.EqualTo(x2).Within(0.0000001));
        Assert.That(link.Y2, Is.EqualTo(y2).Within(0.0000001));
        Assert.That(link.Value, Is.EqualTo(value).Within(0.0000001));
        Assert.That(link.Width, Is.EqualTo(width).Within(0.0000001));
    }

    static void AssertBand(ChartParallelCategoryBand band, int axisIndex, int categoryIndex, string category, double x, double y, double height, int count)
    {
        Assert.That(band.AxisIndex, Is.EqualTo(axisIndex));
        Assert.That(band.CategoryIndex, Is.EqualTo(categoryIndex));
        Assert.That(band.Category, Is.EqualTo(category));
        Assert.That(band.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(band.Y, Is.EqualTo(y).Within(0.0000001));
        Assert.That(band.Height, Is.EqualTo(height).Within(0.0000001));
        Assert.That(band.Count, Is.EqualTo(count));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
