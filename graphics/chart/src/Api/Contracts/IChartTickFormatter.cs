namespace chart;
/// <summary>
/// Formats axis ticks for display.
/// </summary>
/// <remarks>Implement this interface to apply domain-specific labels without replacing tick generation or axis rendering.</remarks>
public interface IChartTickFormatter
{
    /// <summary>
    /// Formats a tick as display text.
    /// </summary>
    /// <param name = "tick">The tick to format.</param>
    /// <returns>The formatted label.</returns>
    string FormatTick(ChartTick tick);
}
