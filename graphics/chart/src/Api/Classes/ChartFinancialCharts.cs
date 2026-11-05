namespace chart;
/// <summary>Provides the public chart financial charts operations.</summary>
public class ChartFinancialCharts : IChartFinancialCharts
{
    /// <summary>Initializes a stateless chart financial charts facade.</summary>
    public ChartFinancialCharts()
    {
    }

    /// <summary>
    /// Creates the requested candlestick series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "openValues">The open values in data order.</param>
    /// <param name = "highValues">The high values in data order.</param>
    /// <param name = "lowValues">The low values in data order.</param>
    /// <param name = "closeValues">The close values in data order.</param>
    /// <param name = "bodyWidth">The body width.</param>
    /// <param name = "upStyle">The rendering style used for up.</param>
    /// <param name = "downStyle">The rendering style used for down.</param>
    /// <param name = "wickStyle">The rendering style used for wick.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateCandlestickSeries(string id, double[] xValues, double[] openValues, double[] highValues, double[] lowValues, double[] closeValues, double bodyWidth, ChartRenderStyle upStyle, ChartRenderStyle downStyle, ChartRenderStyle wickStyle)
    {
        return ChartFinancialChartsOperations.CreateCandlestickSeries(id, xValues, openValues, highValues, lowValues, closeValues, bodyWidth, upStyle, downStyle, wickStyle);
    }

    /// <summary>
    /// Creates the requested ohlc series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "openValues">The open values in data order.</param>
    /// <param name = "highValues">The high values in data order.</param>
    /// <param name = "lowValues">The low values in data order.</param>
    /// <param name = "closeValues">The close values in data order.</param>
    /// <param name = "tickWidth">The tick width.</param>
    /// <param name = "upStyle">The rendering style used for up.</param>
    /// <param name = "downStyle">The rendering style used for down.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateOhlcSeries(string id, double[] xValues, double[] openValues, double[] highValues, double[] lowValues, double[] closeValues, double tickWidth, ChartRenderStyle upStyle, ChartRenderStyle downStyle)
    {
        return ChartFinancialChartsOperations.CreateOhlcSeries(id, xValues, openValues, highValues, lowValues, closeValues, tickWidth, upStyle, downStyle);
    }

    /// <summary>
    /// Creates the requested volume series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "volumeValues">The volume values in data order.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateVolumeSeries(string id, double[] xValues, double[] volumeValues, double barWidth, double baseline, ChartRenderStyle style)
    {
        return ChartFinancialChartsOperations.CreateVolumeSeries(id, xValues, volumeValues, barWidth, baseline, style);
    }

    /// <summary>
    /// Creates the requested volume series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "openValues">The open values in data order.</param>
    /// <param name = "closeValues">The close values in data order.</param>
    /// <param name = "volumeValues">The volume values in data order.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "upStyle">The rendering style used for up.</param>
    /// <param name = "downStyle">The rendering style used for down.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateDirectionalVolumeSeries(string id, double[] xValues, double[] openValues, double[] closeValues, double[] volumeValues, double barWidth, double baseline, ChartRenderStyle upStyle, ChartRenderStyle downStyle)
    {
        return ChartFinancialChartsOperations.CreateVolumeSeries(id, xValues, openValues, closeValues, volumeValues, barWidth, baseline, upStyle, downStyle);
    }

    /// <summary>
    /// Creates the requested moving average series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "period">The period.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public ChartCartesianSeries CreateMovingAverageSeries(string id, double[] xValues, double[] values, int period, ChartRenderStyle style)
    {
        return ChartFinancialChartsOperations.CreateMovingAverageSeries(id, xValues, values, period, style);
    }

    /// <summary>
    /// Applies the moving average operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "period">The period.</param>
    /// <returns>The resulting double array.</returns>
    public double[] MovingAverage(double[] values, int period)
    {
        return ChartFinancialChartsOperations.MovingAverage(values, period);
    }

    /// <summary>
    /// Applies the moving average x values operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "period">The period.</param>
    /// <returns>The resulting double array.</returns>
    public double[] MovingAverageXValues(double[] xValues, int period)
    {
        return ChartFinancialChartsOperations.MovingAverageXValues(xValues, period);
    }

    /// <summary>
    /// Generates financial date ticks from the supplied data and configuration.
    /// </summary>
    /// <param name = "dates">The dates.</param>
    /// <param name = "stride">The number of bytes between the start of adjacent bitmap rows.</param>
    /// <param name = "pattern">The pattern.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTick[] GenerateFinancialDateTicks(System.DateTime[] dates, int stride, string pattern)
    {
        return ChartFinancialChartsOperations.GenerateFinancialDateTicks(dates, stride, pattern);
    }

    /// <summary>
    /// Applies the date index values operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "dates">The dates.</param>
    /// <returns>The resulting double array.</returns>
    public double[] DateIndexValues(System.DateTime[] dates)
    {
        return ChartFinancialChartsOperations.DateIndexValues(dates);
    }

    /// <summary>
    /// Applies the date tick values operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "dates">The dates.</param>
    /// <returns>The resulting double array.</returns>
    public double[] DateTickValues(System.DateTime[] dates)
    {
        return ChartFinancialChartsOperations.DateTickValues(dates);
    }

    /// <summary>
    /// Formats price as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <param name = "currencySymbol">The currency symbol.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string FormatPrice(double value, int decimalPlaces, string currencySymbol)
    {
        return ChartFinancialChartsOperations.FormatPrice(value, decimalPlaces, currencySymbol);
    }

    /// <summary>
    /// Formats volume as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public string FormatVolume(double value)
    {
        return ChartFinancialChartsOperations.FormatVolume(value);
    }

    /// <summary>
    /// Formats change as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public string FormatChange(double value, int decimalPlaces)
    {
        return ChartFinancialChartsOperations.FormatChange(value, decimalPlaces);
    }

    /// <summary>
    /// Formats percent change as deterministic display text.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public string FormatPercentChange(double value, int decimalPlaces)
    {
        return ChartFinancialChartsOperations.FormatPercentChange(value, decimalPlaces);
    }
}
