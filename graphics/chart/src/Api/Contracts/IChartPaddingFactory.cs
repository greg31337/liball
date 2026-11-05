namespace chart;
/// <summary>Defines substitutable chart padding factory operations.</summary>
public interface IChartPaddingFactory
{
    /// <summary>
    /// Applies the all operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting padding.</returns>
    ChartPadding All(double value);
}
