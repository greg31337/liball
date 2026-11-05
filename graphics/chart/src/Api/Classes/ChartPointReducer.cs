namespace chart;
/// <summary>Provides the public chart point reducer operations.</summary>
public class ChartPointReducer : IChartPointReducer
{
    /// <summary>Initializes a stateless chart point reducer facade.</summary>
    public ChartPointReducer()
    {
    }

    /// <summary>
    /// Applies the simplify line operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    public ChartPoint[] SimplifyLine(ChartPoint[] points, double tolerance)
    {
        return ChartPointReducerOperations.SimplifyLine(points, tolerance);
    }

    /// <summary>
    /// Applies the simplify radial distance operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    public ChartPoint[] SimplifyRadialDistance(ChartPoint[] points, double tolerance)
    {
        return ChartPointReducerOperations.SimplifyRadialDistance(points, tolerance);
    }

    /// <summary>
    /// Applies the simplify douglas peucker operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    public ChartPoint[] SimplifyDouglasPeucker(ChartPoint[] points, double tolerance)
    {
        return ChartPointReducerOperations.SimplifyDouglasPeucker(points, tolerance);
    }

    /// <summary>
    /// Applies the decimate scatter operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "maxPointCount">The requested number of max point entries.</param>
    /// <returns>The resulting point array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPoint[] DecimateScatter(ChartPoint[] points, int maxPointCount)
    {
        return ChartPointReducerOperations.DecimateScatter(points, maxPointCount);
    }
}
