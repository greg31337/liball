namespace chart;
/// <summary>Creates and converts chart cartesian series values.</summary>
public class ChartCartesianSeriesFactory : IChartCartesianSeriesFactory
{
    /// <summary>Initializes a stateless chart cartesian series factory facade.</summary>
    public ChartCartesianSeriesFactory()
    {
    }

    /// <summary>
    /// Creates the requested data points from the supplied data and configuration.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The newly created point array.</returns>
    public ChartPoint[] CreateDataPoints(double[] xValues, double[] yValues)
    {
        return ChartCartesianSeries.CreateDataPoints(xValues, yValues);
    }
}
