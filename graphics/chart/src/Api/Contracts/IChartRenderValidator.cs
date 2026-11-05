namespace chart;
/// <summary>Defines substitutable chart render validator operations.</summary>
public interface IChartRenderValidator
{
    /// <summary>
    /// Validates document and returns structured issues without rendering.
    /// </summary>
    /// <param name = "document">The chart document whose model, layout, or output is processed.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartValidationResult ValidateDocument(ChartDocument document);
    /// <summary>
    /// Validates plot and returns structured issues without rendering.
    /// </summary>
    /// <param name = "plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartValidationResult ValidatePlot(ChartPlot plot);
    /// <summary>
    /// Validates series and returns structured issues without rendering.
    /// </summary>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartValidationResult ValidateSeries(ChartCartesianSeries series);
    /// <summary>
    /// Validates scene and returns structured issues without rendering.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartValidationResult ValidateScene(ChartRenderScene scene);
    /// <summary>
    /// Validates cartesian render and returns structured issues without rendering.
    /// </summary>
    /// <param name = "plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartValidationResult ValidateCartesianRender(ChartPlot plot, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain);
}
