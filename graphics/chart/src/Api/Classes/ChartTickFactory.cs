namespace chart;
/// <summary>Creates and converts chart tick values.</summary>
public class ChartTickFactory : IChartTickFactory
{
    /// <summary>Initializes a stateless chart tick factory facade.</summary>
    public ChartTickFactory()
    {
    }

    /// <summary>
    /// Creates the requested time from the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The newly created tick.</returns>
    public ChartTick CreateTime(System.DateTime value, int index)
    {
        return ChartTick.CreateTime(value, index);
    }
}
