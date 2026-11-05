namespace chart;
/// <summary>Provides the public chart render validator operations.</summary>
public class ChartRenderValidator : IChartRenderValidator
{
    /// <summary>Initializes a stateless chart render validator facade.</summary>
    public ChartRenderValidator()
    {
    }

    /// <summary>
    /// Validates document and returns structured issues without rendering.
    /// </summary>
    /// <param name = "document">The chart document whose model, layout, or output is processed.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateDocument(ChartDocument document)
    {
        return ChartRenderValidatorOperations.ValidateDocument(document);
    }

    /// <summary>
    /// Validates plot and returns structured issues without rendering.
    /// </summary>
    /// <param name = "plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidatePlot(ChartPlot plot)
    {
        return ChartRenderValidatorOperations.ValidatePlot(plot);
    }

    /// <summary>
    /// Validates series and returns structured issues without rendering.
    /// </summary>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateSeries(ChartCartesianSeries series)
    {
        return ChartRenderValidatorOperations.ValidateSeries(series);
    }

    /// <summary>
    /// Validates scene and returns structured issues without rendering.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateScene(ChartRenderScene scene)
    {
        return ChartRenderValidatorOperations.ValidateScene(scene);
    }

    /// <summary>
    /// Validates cartesian render and returns structured issues without rendering.
    /// </summary>
    /// <param name = "plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateCartesianRender(ChartPlot plot, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain)
    {
        return ChartRenderValidatorOperations.ValidateCartesianRender(plot, series, xDomain, yDomain);
    }
}
