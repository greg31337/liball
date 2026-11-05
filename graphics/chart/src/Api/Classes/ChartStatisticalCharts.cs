namespace chart;
/// <summary>Provides the public chart statistical charts operations.</summary>
public class ChartStatisticalCharts : IChartStatisticalCharts
{
    /// <summary>Initializes a stateless chart statistical charts facade.</summary>
    public ChartStatisticalCharts()
    {
    }

    /// <summary>
    /// Creates the requested histogram series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "histogram">The histogram.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartCartesianSeries CreateHistogramSeries(string id, ChartHistogram histogram, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateHistogramSeries(id, histogram, style);
    }

    /// <summary>
    /// Creates the requested histogram series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateHistogramSeriesFromValues(string id, double[] values, int binCount, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateHistogramSeries(id, values, binCount, style);
    }

    /// <summary>
    /// Creates the requested density series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateDensitySeries(string id, double[] values, int sampleCount, double bandwidth, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateDensitySeries(id, values, sampleCount, bandwidth, style);
    }

    /// <summary>
    /// Creates the requested density series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateDensitySeriesAutomaticBandwidth(string id, double[] values, int sampleCount, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateDensitySeries(id, values, sampleCount, style);
    }

    /// <summary>
    /// Creates the requested ecdf series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateEcdfSeries(string id, double[] values, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateEcdfSeries(id, values, style);
    }

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
    public ChartCartesianSeries CreateBoxPlotSeries(string id, double xCenter, double boxWidth, double[] values, ChartRenderStyle boxStyle, ChartRenderStyle lineStyle)
    {
        return ChartStatisticalChartsOperations.CreateBoxPlotSeries(id, xCenter, boxWidth, values, boxStyle, lineStyle);
    }

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
    public ChartCartesianSeries CreateViolinSeries(string id, double center, double maximumWidth, double[] values, int sampleCount, double bandwidth, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateViolinSeries(id, center, maximumWidth, values, sampleCount, bandwidth, style);
    }

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
    public ChartCartesianSeries CreateErrorBarSeries(string id, double[] xValues, double[] yValues, double[] lowerErrors, double[] upperErrors, double capWidth, ChartRenderStyle style)
    {
        return ChartStatisticalChartsOperations.CreateErrorBarSeries(id, xValues, yValues, lowerErrors, upperErrors, capWidth, style);
    }

    /// <summary>
    /// Creates the requested stem series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateStemSeries(string id, double[] xValues, double[] yValues, double baseline, ChartRenderStyle lineStyle)
    {
        return ChartStatisticalChartsOperations.CreateStemSeries(id, xValues, yValues, baseline, lineStyle);
    }

    /// <summary>
    /// Creates the requested box plot summary from the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created box plot summary.</returns>
    public ChartBoxPlotSummary CreateBoxPlotSummary(double[] values)
    {
        return ChartStatisticalChartsOperations.CreateBoxPlotSummary(values);
    }

    /// <summary>
    /// Creates the requested violin plan from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "maximumWidth">The maximum width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <returns>The newly created violin plan.</returns>
    public ChartViolinPlan CreateViolinPlan(double center, double maximumWidth, double[] values, int sampleCount, double bandwidth)
    {
        return ChartStatisticalChartsOperations.CreateViolinPlan(center, maximumWidth, values, sampleCount, bandwidth);
    }
}
