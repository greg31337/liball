namespace chart;
/// <summary>Provides the public chart relationship charts operations.</summary>
public class ChartRelationshipCharts : IChartRelationshipCharts
{
    /// <summary>Initializes a stateless chart relationship charts facade.</summary>
    public ChartRelationshipCharts()
    {
    }

    /// <summary>
    /// Computes the layout for sankey within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "links">The links.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "nodeWidth">The node width.</param>
    /// <param name = "nodePadding">The node padding.</param>
    /// <returns>The resulting graph layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartGraphLayout LayoutSankey(string[] labels, ChartGraphLink[] links, ChartRect bounds, double nodeWidth, double nodePadding)
    {
        return ChartRelationshipChartsOperations.LayoutSankey(labels, links, bounds, nodeWidth, nodePadding);
    }

    /// <summary>
    /// Computes the layout for node link within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "links">The links.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting graph layout.</returns>
    public ChartGraphLayout LayoutNodeLink(string[] labels, ChartGraphLink[] links, ChartRect bounds)
    {
        return ChartRelationshipChartsOperations.LayoutNodeLink(labels, links, bounds);
    }

    /// <summary>
    /// Computes a deterministic seeded force-directed graph layout.
    /// </summary>
    /// <param name = "labels">The node labels.</param>
    /// <param name = "links">The graph links.</param>
    /// <param name = "bounds">The rectangle that constrains node positions.</param>
    /// <param name = "seed">The seed controlling initial node positions.</param>
    /// <param name = "iterations">The number of simulation iterations.</param>
    /// <param name = "linkDistance">The preferred distance between linked nodes.</param>
    /// <param name = "repulsion">The nonnegative pairwise repulsion strength.</param>
    /// <returns>The resulting graph layout.</returns>
    public ChartGraphLayout LayoutForceDirected(string[] labels, ChartGraphLink[] links, ChartRect bounds, int seed, int iterations, double linkDistance, double repulsion)
    {
        return ChartRelationshipChartsOperations.LayoutForceDirected(labels, links, bounds, seed, iterations, linkDistance, repulsion);
    }

    /// <summary>
    /// Computes the layout for tree within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "orientation">The orientation.</param>
    /// <returns>The resulting graph layout.</returns>
    public ChartGraphLayout LayoutTree(ChartHierarchyData data, ChartRect bounds, ChartOrientation orientation)
    {
        return ChartRelationshipChartsOperations.LayoutTree(data, bounds, orientation);
    }

    /// <summary>
    /// Computes the layout for dendrogram within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "orientation">The orientation.</param>
    /// <returns>The resulting graph layout.</returns>
    public ChartGraphLayout LayoutDendrogram(ChartHierarchyData data, ChartRect bounds, ChartOrientation orientation)
    {
        return ChartRelationshipChartsOperations.LayoutDendrogram(data, bounds, orientation);
    }

    /// <summary>
    /// Computes the layout for parallel coordinates within the supplied bounds.
    /// </summary>
    /// <param name = "axisLabels">The axis labels.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting parallel coordinates layout.</returns>
    public ChartParallelCoordinatesLayout LayoutParallelCoordinates(string[] axisLabels, ChartDataMatrix values, ChartRect bounds)
    {
        return ChartRelationshipChartsOperations.LayoutParallelCoordinates(axisLabels, values, bounds);
    }

    /// <summary>
    /// Computes the layout for parallel categories within the supplied bounds.
    /// </summary>
    /// <param name = "axisLabels">The axis labels.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <param name = "axisCount">The requested number of axis entries.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting parallel categories layout.</returns>
    public ChartParallelCategoriesLayout LayoutParallelCategories(string[] axisLabels, string[] values, int rowCount, int axisCount, ChartRect bounds)
    {
        return ChartRelationshipChartsOperations.LayoutParallelCategories(axisLabels, values, rowCount, axisCount, bounds);
    }

    /// <summary>Creates a renderable scene from a Sankey, node-link, force, tree, or dendrogram layout.</summary>
    /// <param name = "id">The stable identifier prefix.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param><param name = "layout">The graph layout.</param><param name = "nodeSize">The marker size used for point nodes.</param><param name = "nodeStyle">The node style.</param><param name = "linkStyle">The link style.</param><param name = "labelStyle">The node label style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateGraphScene(string id, int width, int height, ChartGraphLayout layout, double nodeSize, ChartRenderStyle nodeStyle, ChartRenderStyle linkStyle, ChartRenderStyle labelStyle)
    {
        return ChartRelationshipChartsOperations.CreateGraphScene(id, width, height, layout, nodeSize, nodeStyle, linkStyle, labelStyle);
    }

    /// <summary>Adds a Sankey, node-link, force, tree, or dendrogram layout to a render target.</summary>
    /// <param name = "scene">The target that receives retained commands.</param><param name = "id">The stable identifier prefix.</param><param name = "layout">The graph layout.</param><param name = "nodeSize">The marker size used for point nodes.</param><param name = "nodeStyle">The node style.</param><param name = "linkStyle">The link style.</param><param name = "labelStyle">The node label style.</param>
    public void AddGraphLayoutToScene(IChartRenderTarget scene, string id, ChartGraphLayout layout, double nodeSize, ChartRenderStyle nodeStyle, ChartRenderStyle linkStyle, ChartRenderStyle labelStyle)
    {
        ChartRelationshipChartsOperations.AddGraphLayoutToScene(scene, id, layout, nodeSize, nodeStyle, linkStyle, labelStyle);
    }

    /// <summary>Creates a renderable scene from a parallel-coordinate layout.</summary>
    /// <param name = "id">The stable identifier prefix.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param><param name = "layout">The parallel-coordinate layout.</param><param name = "bounds">The axis bounds.</param><param name = "axisStyle">The axis style.</param><param name = "recordStyle">The record style.</param><param name = "labelStyle">The axis label style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateParallelCoordinatesScene(string id, int width, int height, ChartParallelCoordinatesLayout layout, ChartRect bounds, ChartRenderStyle axisStyle, ChartRenderStyle recordStyle, ChartRenderStyle labelStyle)
    {
        return ChartRelationshipChartsOperations.CreateParallelCoordinatesScene(id, width, height, layout, bounds, axisStyle, recordStyle, labelStyle);
    }

    /// <summary>Adds a parallel-coordinate layout to a render target.</summary>
    /// <param name = "scene">The target that receives retained commands.</param><param name = "id">The stable identifier prefix.</param><param name = "layout">The parallel-coordinate layout.</param><param name = "bounds">The axis bounds.</param><param name = "axisStyle">The axis style.</param><param name = "recordStyle">The record style.</param><param name = "labelStyle">The axis label style.</param>
    public void AddParallelCoordinatesToScene(IChartRenderTarget scene, string id, ChartParallelCoordinatesLayout layout, ChartRect bounds, ChartRenderStyle axisStyle, ChartRenderStyle recordStyle, ChartRenderStyle labelStyle)
    {
        ChartRelationshipChartsOperations.AddParallelCoordinatesToScene(scene, id, layout, bounds, axisStyle, recordStyle, labelStyle);
    }

    /// <summary>Creates a renderable scene from a parallel-category layout.</summary>
    /// <param name = "id">The stable identifier prefix.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param><param name = "layout">The parallel-category layout.</param><param name = "bandWidth">The rendered band width.</param><param name = "bandStyle">The band style.</param><param name = "flowStyle">The flow style.</param><param name = "labelStyle">The category label style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateParallelCategoriesScene(string id, int width, int height, ChartParallelCategoriesLayout layout, double bandWidth, ChartRenderStyle bandStyle, ChartRenderStyle flowStyle, ChartRenderStyle labelStyle)
    {
        return ChartRelationshipChartsOperations.CreateParallelCategoriesScene(id, width, height, layout, bandWidth, bandStyle, flowStyle, labelStyle);
    }

    /// <summary>Adds a parallel-category layout to a render target.</summary>
    /// <param name = "scene">The target that receives retained commands.</param><param name = "id">The stable identifier prefix.</param><param name = "layout">The parallel-category layout.</param><param name = "bandWidth">The rendered band width.</param><param name = "bandStyle">The band style.</param><param name = "flowStyle">The flow style.</param><param name = "labelStyle">The category label style.</param>
    public void AddParallelCategoriesToScene(IChartRenderTarget scene, string id, ChartParallelCategoriesLayout layout, double bandWidth, ChartRenderStyle bandStyle, ChartRenderStyle flowStyle, ChartRenderStyle labelStyle)
    {
        ChartRelationshipChartsOperations.AddParallelCategoriesToScene(scene, id, layout, bandWidth, bandStyle, flowStyle, labelStyle);
    }

    /// <summary>
    /// Adds the supplied graph links to this object.
    /// </summary>
    /// <param name = "layout">The layout.</param>
    /// <param name = "links">The links.</param>
    public void AddGraphLinks(ChartGraphLayout layout, ChartGraphLink[] links)
    {
        ChartRelationshipChartsOperations.AddGraphLinks(layout, links);
    }

    /// <summary>
    /// Adds the supplied hierarchy links to this object.
    /// </summary>
    /// <param name = "layout">The layout.</param>
    /// <param name = "data">The data.</param>
    public void AddHierarchyLinks(ChartGraphLayout layout, ChartHierarchyData data)
    {
        ChartRelationshipChartsOperations.AddHierarchyLinks(layout, data);
    }
}
