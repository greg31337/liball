namespace chart;
/// <summary>Defines substitutable chart scatter charts operations.</summary>
public interface IChartScatterCharts
{
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
    ChartCartesianSeries CreateScatterSeries(string id, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle style);
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
    ChartCartesianSeries CreateBubbleSeries(string id, double[] xValues, double[] yValues, double[] sizeValues, double minimumSize, double maximumSize, ChartRenderStyle style);
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
    ChartCartesianSeries CreateRugSeries(string id, double[] values, double baseline, double length, ChartOrientation orientation, ChartRenderStyle style);
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
    ChartCartesianSeries[] CreateMarginalScatterSeries(string idPrefix, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle scatterStyle, ChartRenderStyle rugStyle, double rugLength);
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
    ChartCartesianSeries[] CreateMarginalScatterSeriesWithBaselines(string idPrefix, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle scatterStyle, ChartRenderStyle rugStyle, double xRugBaseline, double yRugBaseline, double rugLength);
}
