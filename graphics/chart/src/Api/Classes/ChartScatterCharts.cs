namespace chart;
/// <summary>Provides the public chart scatter charts operations.</summary>
public class ChartScatterCharts : IChartScatterCharts
{
    /// <summary>Initializes a stateless chart scatter charts facade.</summary>
    public ChartScatterCharts()
    {
    }

    /// <summary>
    /// Creates the requested scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateScatterSeries(string id, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        return ChartScatterChartsOperations.CreateScatterSeries(id, xValues, yValues, shape, markerSize, style);
    }

    /// <summary>
    /// Creates the requested bubble series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "sizeValues">The size values in data order.</param>
    /// <param name = "minimumSize">The minimum size.</param>
    /// <param name = "maximumSize">The maximum size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateBubbleSeries(string id, double[] xValues, double[] yValues, double[] sizeValues, double minimumSize, double maximumSize, ChartRenderStyle style)
    {
        return ChartScatterChartsOperations.CreateBubbleSeries(id, xValues, yValues, sizeValues, minimumSize, maximumSize, style);
    }

    /// <summary>
    /// Creates the requested rug series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "length">The length.</param>
    /// <param name = "orientation">The orientation.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateRugSeries(string id, double[] values, double baseline, double length, ChartOrientation orientation, ChartRenderStyle style)
    {
        return ChartScatterChartsOperations.CreateRugSeries(id, values, baseline, length, orientation, style);
    }

    /// <summary>
    /// Creates the requested marginal scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "scatterStyle">The rendering style used for scatter.</param>
    /// <param name = "rugStyle">The rendering style used for rug.</param>
    /// <param name = "rugLength">The rug length.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateMarginalScatterSeries(string idPrefix, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle scatterStyle, ChartRenderStyle rugStyle, double rugLength)
    {
        return ChartScatterChartsOperations.CreateMarginalScatterSeries(idPrefix, xValues, yValues, shape, markerSize, scatterStyle, rugStyle, rugLength);
    }

    /// <summary>
    /// Creates the requested marginal scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "scatterStyle">The rendering style used for scatter.</param>
    /// <param name = "rugStyle">The rendering style used for rug.</param>
    /// <param name = "xRugBaseline">The x rug baseline.</param>
    /// <param name = "yRugBaseline">The y rug baseline.</param>
    /// <param name = "rugLength">The rug length.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public ChartCartesianSeries[] CreateMarginalScatterSeriesWithBaselines(string idPrefix, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle scatterStyle, ChartRenderStyle rugStyle, double xRugBaseline, double yRugBaseline, double rugLength)
    {
        return ChartScatterChartsOperations.CreateMarginalScatterSeries(idPrefix, xValues, yValues, shape, markerSize, scatterStyle, rugStyle, xRugBaseline, yRugBaseline, rugLength);
    }
}
