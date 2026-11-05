namespace chart;
/// <summary>Provides the public chart stacker operations.</summary>
public class ChartStacker : IChartStacker
{
    /// <summary>Initializes a stateless chart stacker facade.</summary>
    public ChartStacker()
    {
    }

    /// <summary>
    /// Stacks the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "mode">The mode.</param>
    /// <returns>The resulting stack result.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartStackResult Stack(ChartDataMatrix values, ChartStackMode mode)
    {
        return ChartStackerOperations.Stack(values, mode);
    }

    /// <summary>
    /// Stacks none using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public ChartStackResult StackNone(ChartDataMatrix values)
    {
        return ChartStackerOperations.StackNone(values);
    }

    /// <summary>
    /// Stacks normal using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public ChartStackResult StackNormal(ChartDataMatrix values)
    {
        return ChartStackerOperations.StackNormal(values);
    }

    /// <summary>
    /// Stacks percent using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartStackResult StackPercent(ChartDataMatrix values)
    {
        return ChartStackerOperations.StackPercent(values);
    }

    /// <summary>
    /// Stacks diverging using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public ChartStackResult StackDiverging(ChartDataMatrix values)
    {
        return ChartStackerOperations.StackDiverging(values);
    }

    /// <summary>
    /// Stacks streamgraph using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public ChartStackResult StackStreamgraph(ChartDataMatrix values)
    {
        return ChartStackerOperations.StackStreamgraph(values);
    }
}
