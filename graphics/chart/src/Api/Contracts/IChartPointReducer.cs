namespace chart;
/// <summary>Defines substitutable chart point reducer operations.</summary>
public interface IChartPointReducer
{
    /// <summary>
    /// Applies the simplify line operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    ChartPoint[] SimplifyLine(ChartPoint[] points, double tolerance);
    /// <summary>
    /// Applies the simplify radial distance operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    ChartPoint[] SimplifyRadialDistance(ChartPoint[] points, double tolerance);
    /// <summary>
    /// Applies the simplify douglas peucker operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    ChartPoint[] SimplifyDouglasPeucker(ChartPoint[] points, double tolerance);
    /// <summary>
    /// Applies the decimate scatter operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "maxPointCount">The requested number of max point entries.</param>
    /// <returns>The resulting point array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartPoint[] DecimateScatter(ChartPoint[] points, int maxPointCount);
}
