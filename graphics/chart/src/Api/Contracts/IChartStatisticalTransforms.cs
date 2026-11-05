namespace chart;
/// <summary>Defines substitutable chart statistical transforms operations.</summary>
public interface IChartStatisticalTransforms
{
    /// <summary>
    /// Applies the linear regression operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting linear trendline.</returns>
    ChartLinearTrendline LinearRegression(double[] xValues, double[] yValues);
    /// <summary>Fits a polynomial least-squares regression.</summary>
    /// <param name = "xValues">The horizontal values.</param><param name = "yValues">The vertical values.</param><param name = "degree">The positive polynomial degree.</param>
    /// <returns>The fitted polynomial trendline.</returns>
    ChartPolynomialTrendline PolynomialRegression(double[] xValues, double[] yValues, int degree);
    /// <summary>
    /// Applies the moving average operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "period">The period.</param>
    /// <returns>The resulting double array.</returns>
    double[] MovingAverage(double[] values, int period);
    /// <summary>
    /// Applies the exponential moving average operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "alpha">The alpha channel from 0 for transparent through 255 for opaque.</param>
    /// <returns>The resulting double array.</returns>
    double[] ExponentialMovingAverage(double[] values, double alpha);
    /// <summary>
    /// Applies the rolling minimum operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "window">The window.</param>
    /// <returns>The resulting double array.</returns>
    double[] RollingMinimum(double[] values, int window);
    /// <summary>
    /// Applies the rolling maximum operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "window">The window.</param>
    /// <returns>The resulting double array.</returns>
    double[] RollingMaximum(double[] values, int window);
    /// <summary>
    /// Applies the cumulative sum operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double array.</returns>
    double[] CumulativeSum(double[] values);
    /// <summary>Calculates a complete-window rolling aggregation.</summary>
    /// <param name = "values">The finite input values.</param><param name = "window">The positive window size.</param><param name = "aggregation">The aggregation to apply to each window.</param><param name = "percentile">The percentile from zero through 100 when percentile aggregation is selected.</param>
    /// <returns>One value for every complete rolling window.</returns>
    double[] CalculateRolling(double[] values, int window, ChartAggregationKind aggregation, double percentile);
    /// <summary>Calculates an aggregation for every cumulative prefix.</summary>
    /// <param name = "values">The finite input values.</param><param name = "aggregation">The aggregation to apply to each prefix.</param><param name = "percentile">The percentile from zero through 100 when percentile aggregation is selected.</param>
    /// <returns>One value for every cumulative prefix.</returns>
    double[] CalculateCumulative(double[] values, ChartAggregationKind aggregation, double percentile);
    /// <summary>
    /// Applies the pareto operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting pareto result.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartParetoResult Pareto(string[] labels, double[] values);
    /// <summary>
    /// Applies the correlation matrix operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting data matrix.</returns>
    ChartDataMatrix CorrelationMatrix(ChartDataMatrix values);
    /// <summary>
    /// Converts this value to tal.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The converted double.</returns>
    double Total(double[] values);
}
