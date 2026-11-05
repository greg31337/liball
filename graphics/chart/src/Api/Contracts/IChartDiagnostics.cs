namespace chart;
/// <summary>Defines substitutable chart diagnostics operations.</summary>
public interface IChartDiagnostics
{
    /// <summary>
    /// Adds the supplied layout overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "layout">The layout.</param>
    void AddLayoutOverlay(IChartRenderTarget scene, ChartLayoutResult layout);
    /// <summary>
    /// Adds the supplied layout overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void AddLayoutOverlayWithPrefix(IChartRenderTarget scene, ChartLayoutResult layout, string idPrefix);
    /// <summary>
    /// Computes the layout for trace within the supplied bounds.
    /// </summary>
    /// <param name = "layout">The layout.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string LayoutTrace(ChartLayoutResult layout);
    /// <summary>
    /// Renders trace using the supplied data and configuration.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string RenderTrace(ChartRenderScene scene);
}
