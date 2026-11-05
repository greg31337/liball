namespace chart;
/// <summary>Provides the public chart hierarchy charts operations.</summary>
public class ChartHierarchyCharts : IChartHierarchyCharts
{
    /// <summary>Initializes a stateless chart hierarchy charts facade.</summary>
    public ChartHierarchyCharts()
    {
    }

    /// <summary>
    /// Computes the layout for treemap within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting hierarchy rect layout.</returns>
    public ChartHierarchyRectLayout LayoutTreemap(ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        return ChartHierarchyChartsOperations.LayoutTreemap(data, bounds, palette);
    }

    /// <summary>
    /// Computes a treemap with the requested sibling layout strategy.
    /// </summary>
    /// <param name = "data">The hierarchy data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout.</param>
    /// <param name = "palette">The categorical colors to apply in stable node order.</param>
    /// <param name = "kind">The treemap sibling layout strategy.</param>
    /// <returns>The resulting hierarchy rectangle layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the layout kind is unsupported.</exception>
    public ChartHierarchyRectLayout LayoutTreemapByKind(ChartHierarchyData data, ChartRect bounds, ChartPalette palette, ChartTreemapLayoutKind kind)
    {
        return ChartHierarchyChartsOperations.LayoutTreemapByKind(data, bounds, palette, kind);
    }

    /// <summary>
    /// Computes the layout for icicle within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting hierarchy rect layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartHierarchyRectLayout LayoutIcicle(ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        return ChartHierarchyChartsOperations.LayoutIcicle(data, bounds, palette);
    }

    /// <summary>
    /// Computes the layout for sunburst within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "endAngle">The ending angle in degrees.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting sunburst layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartSunburstLayout LayoutSunburst(ChartHierarchyData data, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartPalette palette)
    {
        return ChartHierarchyChartsOperations.LayoutSunburst(data, centerX, centerY, innerRadius, outerRadius, startAngle, endAngle, palette);
    }

    /// <summary>Creates a treemap scene directly from hierarchy data.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "data">The hierarchy data.</param>
    /// <param name = "bounds">The layout bounds.</param>
    /// <param name = "palette">The item colors.</param>
    /// <param name = "kind">The treemap layout strategy.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateTreemapScene(string id, int width, int height, ChartHierarchyData data, ChartRect bounds, ChartPalette palette, ChartTreemapLayoutKind kind)
    {
        return ChartHierarchyChartsOperations.CreateTreemapScene(id, width, height, data, bounds, palette, kind);
    }

    /// <summary>Creates an icicle scene directly from hierarchy data.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "data">The hierarchy data.</param>
    /// <param name = "bounds">The layout bounds.</param>
    /// <param name = "palette">The item colors.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateIcicleScene(string id, int width, int height, ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        return ChartHierarchyChartsOperations.CreateIcicleScene(id, width, height, data, bounds, palette);
    }

    /// <summary>Creates a sunburst scene directly from hierarchy data.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The scene width.</param>
    /// <param name = "height">The scene height.</param>
    /// <param name = "data">The hierarchy data.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius.</param>
    /// <param name = "outerRadius">The outer radius.</param>
    /// <param name = "startAngle">The starting angle.</param>
    /// <param name = "endAngle">The ending angle.</param>
    /// <param name = "palette">The item colors.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateSunburstScene(string id, int width, int height, ChartHierarchyData data, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartPalette palette)
    {
        return ChartHierarchyChartsOperations.CreateSunburstScene(id, width, height, data, centerX, centerY, innerRadius, outerRadius, startAngle, endAngle, palette);
    }

    /// <summary>
    /// Adds the supplied rect layout to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddRectLayoutToScene(IChartRenderTarget scene, string id, ChartHierarchyRectLayout layout)
    {
        ChartHierarchyChartsOperations.AddRectLayoutToScene(scene, id, layout);
    }

    /// <summary>
    /// Adds the supplied sunburst to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddSunburstToScene(IChartRenderTarget scene, string id, ChartSunburstLayout layout)
    {
        ChartHierarchyChartsOperations.AddSunburstToScene(scene, id, layout);
    }
}
