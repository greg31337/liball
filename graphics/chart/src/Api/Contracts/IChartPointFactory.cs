namespace chart;
/// <summary>Defines substitutable chart point factory operations.</summary>
public interface IChartPointFactory
{
    /// <summary>
    /// Adds the supplied the configured value to this object.
    /// </summary>
    /// <param name = "left">The left.</param>
    /// <param name = "right">The right.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint Add(ChartPoint left, ChartPoint right);
    /// <summary>
    /// Applies the subtract operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "left">The left.</param>
    /// <param name = "right">The right.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint Subtract(ChartPoint left, ChartPoint right);
    /// <summary>
    /// Applies the scale operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <param name = "factor">The factor.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint Scale(ChartPoint point, double factor);
    /// <summary>
    /// Applies the lerp operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "start">The start.</param>
    /// <param name = "end">The end.</param>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint Lerp(ChartPoint start, ChartPoint end, double amount);
}
