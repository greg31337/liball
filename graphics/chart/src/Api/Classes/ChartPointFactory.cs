namespace chart;
/// <summary>Creates and converts chart point values.</summary>
public class ChartPointFactory : IChartPointFactory
{
    /// <summary>Initializes a stateless chart point factory facade.</summary>
    public ChartPointFactory()
    {
    }

    /// <summary>
    /// Adds the supplied the configured value to this object.
    /// </summary>
    /// <param name = "left">The left.</param>
    /// <param name = "right">The right.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint Add(ChartPoint left, ChartPoint right)
    {
        return ChartPoint.Add(left, right);
    }

    /// <summary>
    /// Applies the subtract operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "left">The left.</param>
    /// <param name = "right">The right.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint Subtract(ChartPoint left, ChartPoint right)
    {
        return ChartPoint.Subtract(left, right);
    }

    /// <summary>
    /// Applies the scale operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <param name = "factor">The factor.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint Scale(ChartPoint point, double factor)
    {
        return ChartPoint.Scale(point, factor);
    }

    /// <summary>
    /// Applies the lerp operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "start">The start.</param>
    /// <param name = "end">The end.</param>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint Lerp(ChartPoint start, ChartPoint end, double amount)
    {
        return ChartPoint.Lerp(start, end, amount);
    }
}
