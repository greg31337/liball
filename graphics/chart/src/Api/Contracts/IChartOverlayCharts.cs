namespace chart;
/// <summary>Defines substitutable chart overlay charts operations.</summary>
public interface IChartOverlayCharts
{
    /// <summary>
    /// Creates the requested horizontal reference line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateHorizontalReferenceLine(string id, double xMinimum, double xMaximum, double y, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested vertical reference line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateVerticalReferenceLine(string id, double x, double yMinimum, double yMaximum, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested horizontal reference band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateHorizontalReferenceBand(string id, double xMinimum, double xMaximum, double yMinimum, double yMaximum, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested vertical reference band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateVerticalReferenceBand(string id, double xMinimum, double xMaximum, double yMinimum, double yMaximum, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested confidence band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "lowerValues">The lower values in data order.</param>
    /// <param name = "upperValues">The upper values in data order.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateConfidenceBand(string id, double[] xValues, double[] lowerValues, double[] upperValues, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested trendline series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateTrendlineSeries(string id, double[] xValues, double[] yValues, double xMinimum, double xMaximum, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested mean line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateMeanLine(string id, double xMinimum, double xMaximum, double[] values, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested median line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateMedianLine(string id, double xMinimum, double xMaximum, double[] values, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested quantile band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "lowerQuantile">The lower quantile.</param>
    /// <param name = "upperQuantile">The upper quantile.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartCartesianSeries CreateQuantileBand(string id, double xMinimum, double xMaximum, double[] values, double lowerQuantile, double upperQuantile, ChartRenderStyle style);
    /// <summary>
    /// Applies the fit linear trendline operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting linear trendline.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    ChartLinearTrendline FitLinearTrendline(double[] xValues, double[] yValues);
    /// <summary>
    /// Applies the trendline r squared operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "slope">The slope.</param>
    /// <param name = "intercept">The intercept.</param>
    /// <param name = "yMean">The y mean.</param>
    /// <returns>The resulting double.</returns>
    double TrendlineRSquared(double[] xValues, double[] yValues, double slope, double intercept, double yMean);
    /// <summary>
    /// Applies the quantile operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "quantile">The quantile.</param>
    /// <returns>The resulting double.</returns>
    double Quantile(double[] values, double quantile);
    /// <summary>Creates a band around supplied center values using supplied standard deviations.</summary>
    /// <param name = "id">The stable series identifier.</param><param name = "xValues">The horizontal values.</param><param name = "centerValues">The band center values.</param><param name = "standardDeviations">The nonnegative supplied deviations.</param><param name = "multiplier">The positive deviation multiplier.</param><param name = "style">The band style.</param>
    /// <returns>The standard-deviation band series.</returns>
    ChartCartesianSeries CreateStandardDeviationBand(string id, double[] xValues, double[] centerValues, double[] standardDeviations, double multiplier, ChartRenderStyle style);
    /// <summary>Fits a polynomial least-squares trendline.</summary>
    /// <param name = "xValues">The horizontal values.</param><param name = "yValues">The vertical values.</param><param name = "degree">The positive polynomial degree.</param>
    /// <returns>The fitted polynomial trendline.</returns>
    ChartPolynomialTrendline FitPolynomialTrendline(double[] xValues, double[] yValues, int degree);
    /// <summary>Creates a sampled polynomial trendline series.</summary>
    /// <param name = "id">The stable series identifier.</param><param name = "xValues">The fitted horizontal values.</param><param name = "yValues">The fitted vertical values.</param><param name = "degree">The polynomial degree.</param><param name = "xMinimum">The sample-domain minimum.</param><param name = "xMaximum">The sample-domain maximum.</param><param name = "sampleCount">The number of output samples.</param><param name = "style">The line style.</param>
    /// <returns>The sampled trendline series.</returns>
    ChartCartesianSeries CreatePolynomialTrendlineSeries(string id, double[] xValues, double[] yValues, int degree, double xMinimum, double xMaximum, int sampleCount, ChartRenderStyle style);
    /// <summary>Creates data, center, lower-limit, and upper-limit series for a control chart.</summary>
    /// <param name = "idPrefix">The stable series identifier prefix.</param><param name = "xValues">The horizontal values.</param><param name = "values">The observed values.</param><param name = "sigmaMultiplier">The positive control-limit multiplier.</param><param name = "dataStyle">The observation style.</param><param name = "centerStyle">The center-line style.</param><param name = "limitStyle">The control-limit style.</param>
    /// <returns>The four control-chart series.</returns>
    ChartCartesianSeries[] CreateControlChartSeries(string idPrefix, double[] xValues, double[] values, double sigmaMultiplier, ChartRenderStyle dataStyle, ChartRenderStyle centerStyle, ChartRenderStyle limitStyle);
    /// <summary>Formats a linear trendline equation.</summary>
    /// <param name = "trendline">The fitted linear trendline.</param><param name = "dependentVariable">The dependent-variable label.</param><param name = "decimalPlaces">The number of fractional digits.</param><param name = "includeRSquared">Whether to append R-squared.</param>
    /// <returns>The invariant-culture equation.</returns>
    string FormatLinearTrendlineEquation(ChartLinearTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared);
    /// <summary>Formats a polynomial trendline equation.</summary>
    /// <param name = "trendline">The fitted polynomial trendline.</param><param name = "dependentVariable">The dependent-variable label.</param><param name = "decimalPlaces">The number of fractional digits.</param><param name = "includeRSquared">Whether to append R-squared.</param>
    /// <returns>The invariant-culture equation.</returns>
    string FormatPolynomialTrendlineEquation(ChartPolynomialTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared);
    /// <summary>Creates a retained text label for a linear trendline equation.</summary>
    /// <param name = "id">The stable label identifier.</param><param name = "trendline">The fitted linear trendline.</param><param name = "dependentVariable">The dependent-variable label.</param><param name = "decimalPlaces">The number of fractional digits.</param><param name = "includeRSquared">Whether to append R-squared.</param><param name = "x">The label x coordinate.</param><param name = "y">The label y coordinate.</param><param name = "style">The text style.</param>
    /// <returns>The label series.</returns>
    ChartCartesianSeries CreateLinearTrendlineLabel(string id, ChartLinearTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared, double x, double y, ChartRenderStyle style);
    /// <summary>Creates a retained text label for a polynomial trendline equation.</summary>
    /// <param name = "id">The stable label identifier.</param><param name = "trendline">The fitted polynomial trendline.</param><param name = "dependentVariable">The dependent-variable label.</param><param name = "decimalPlaces">The number of fractional digits.</param><param name = "includeRSquared">Whether to append R-squared.</param><param name = "x">The label x coordinate.</param><param name = "y">The label y coordinate.</param><param name = "style">The text style.</param>
    /// <returns>The label series.</returns>
    ChartCartesianSeries CreatePolynomialTrendlineLabel(string id, ChartPolynomialTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared, double x, double y, ChartRenderStyle style);
}
