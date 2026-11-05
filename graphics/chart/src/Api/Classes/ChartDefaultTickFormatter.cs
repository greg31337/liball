namespace chart;
/// <summary>
/// Applies the library's deterministic default tick formatting rules.
/// </summary>
public class ChartDefaultTickFormatter : IChartTickFormatter
{
    /// <summary>
    /// Initializes the default tick formatter.
    /// </summary>
    public ChartDefaultTickFormatter()
    {
    }

    /// <summary>
    /// Formats a tick using <see cref = "ChartTickFormatter"/>.
    /// </summary>
    /// <param name = "tick">The tick to format.</param>
    /// <returns>The formatted label.</returns>
    public string FormatTick(ChartTick tick)
    {
        return ChartTickFormatterOperations.FormatTick(tick);
    }
}
