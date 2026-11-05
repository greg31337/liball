namespace chart;
/// <summary>Defines substitutable chart statistical charts operations.</summary>
public interface IChartStatisticalCharts
{
    /// <summary>
    /// Creates the requested histogram series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "histogram">The histogram.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartCartesianSeries CreateHistogramSeries(string id, ChartHistogram histogram, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested histogram series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateHistogramSeriesFromValues(string id, double[] values, int binCount, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested density series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateDensitySeries(string id, double[] values, int sampleCount, double bandwidth, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested density series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateDensitySeriesAutomaticBandwidth(string id, double[] values, int sampleCount, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested ecdf series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateEcdfSeries(string id, double[] values, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested box plot series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenter">The x center.</param>
    /// <param name = "boxWidth">The box width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "boxStyle">The rendering style used for box.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateBoxPlotSeries(string id, double xCenter, double boxWidth, double[] values, ChartRenderStyle boxStyle, ChartRenderStyle lineStyle);
    /// <summary>
    /// Creates the requested violin series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "center">The center.</param>
    /// <param name = "maximumWidth">The maximum width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateViolinSeries(string id, double center, double maximumWidth, double[] values, int sampleCount, double bandwidth, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested error bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "lowerErrors">The lower errors.</param>
    /// <param name = "upperErrors">The upper errors.</param>
    /// <param name = "capWidth">The cap width.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateErrorBarSeries(string id, double[] xValues, double[] yValues, double[] lowerErrors, double[] upperErrors, double capWidth, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested stem series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateStemSeries(string id, double[] xValues, double[] yValues, double baseline, ChartRenderStyle lineStyle);
    /// <summary>
    /// Creates the requested box plot summary from the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created box plot summary.</returns>
    ChartBoxPlotSummary CreateBoxPlotSummary(double[] values);
    /// <summary>
    /// Creates the requested violin plan from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "maximumWidth">The maximum width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <returns>The newly created violin plan.</returns>
    ChartViolinPlan CreateViolinPlan(double center, double maximumWidth, double[] values, int sampleCount, double bandwidth);
}
