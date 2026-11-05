namespace chart;
/// <summary>Creates and converts chart padding values.</summary>
public class ChartPaddingFactory : IChartPaddingFactory
{
    /// <summary>Initializes a stateless chart padding factory facade.</summary>
    public ChartPaddingFactory()
    {
    }

    /// <summary>
    /// Applies the all operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting padding.</returns>
    public ChartPadding All(double value)
    {
        return new ChartPadding(value, value, value, value);
    }
}
