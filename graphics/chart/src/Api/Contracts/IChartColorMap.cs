namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartColorMap"/>.</summary>
public interface IChartColorMap
{
    /// <summary>
    /// Returns the number of logical items in this value.
    /// </summary>
    /// <returns>The resulting int.</returns>
    int Count();
    /// <summary>
    /// Returns the requested stop.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting color.</returns>
    ChartColor GetStop(int index);
    /// <summary>
    /// Returns the requested color.
    /// </summary>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting color.</returns>
    ChartColor GetColor(double amount);
    /// <summary>
    /// Creates a reversed copy of the the configured value.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The resulting color map.</returns>
    ChartColorMap Reverse(string name);
    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    string GetName();
    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetName(string value);
    /// <summary>Returns the current stops.</summary>
    /// <returns>The current field value.</returns>
    ChartColor[] GetStops();
    /// <summary>Replaces the current stops.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStops(ChartColor[] value);
}
