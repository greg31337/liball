namespace chart;
/// <summary>Provides the public chart diagnostics operations.</summary>
public class ChartDiagnostics : IChartDiagnostics
{
    /// <summary>Initializes a stateless chart diagnostics facade.</summary>
    public ChartDiagnostics()
    {
    }

    /// <summary>
    /// Adds the supplied layout overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "layout">The layout.</param>
    public void AddLayoutOverlay(IChartRenderTarget scene, ChartLayoutResult layout)
    {
        ChartDiagnosticsOperations.AddLayoutOverlay(scene, layout);
    }

    /// <summary>
    /// Adds the supplied layout overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddLayoutOverlayWithPrefix(IChartRenderTarget scene, ChartLayoutResult layout, string idPrefix)
    {
        ChartDiagnosticsOperations.AddLayoutOverlay(scene, layout, idPrefix);
    }

    /// <summary>
    /// Computes the layout for trace within the supplied bounds.
    /// </summary>
    /// <param name = "layout">The layout.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string LayoutTrace(ChartLayoutResult layout)
    {
        return ChartDiagnosticsOperations.LayoutTrace(layout);
    }

    /// <summary>
    /// Renders trace using the supplied data and configuration.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string RenderTrace(ChartRenderScene scene)
    {
        return ChartDiagnosticsOperations.RenderTrace(scene);
    }
}
