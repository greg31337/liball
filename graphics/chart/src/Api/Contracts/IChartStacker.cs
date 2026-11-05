namespace chart;
/// <summary>Defines substitutable chart stacker operations.</summary>
public interface IChartStacker
{
    /// <summary>
    /// Stacks the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "mode">The mode.</param>
    /// <returns>The resulting stack result.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartStackResult Stack(ChartDataMatrix values, ChartStackMode mode);
    /// <summary>
    /// Stacks none using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    ChartStackResult StackNone(ChartDataMatrix values);
    /// <summary>
    /// Stacks normal using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    ChartStackResult StackNormal(ChartDataMatrix values);
    /// <summary>
    /// Stacks percent using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartStackResult StackPercent(ChartDataMatrix values);
    /// <summary>
    /// Stacks diverging using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    ChartStackResult StackDiverging(ChartDataMatrix values);
    /// <summary>
    /// Stacks streamgraph using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    ChartStackResult StackStreamgraph(ChartDataMatrix values);
}
