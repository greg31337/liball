namespace chart;
/// <summary>Defines substitutable chart cartesian series factory operations.</summary>
public interface IChartCartesianSeriesFactory
{
    /// <summary>
    /// Creates the requested data points from the supplied data and configuration.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The newly created point array.</returns>
    ChartPoint[] CreateDataPoints(double[] xValues, double[] yValues);
}
