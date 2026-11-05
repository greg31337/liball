namespace chart;
/// <summary>Defines substitutable chart tick factory operations.</summary>
public interface IChartTickFactory
{
    /// <summary>
    /// Creates the requested time from the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The newly created tick.</returns>
    ChartTick CreateTime(System.DateTime value, int index);
}
