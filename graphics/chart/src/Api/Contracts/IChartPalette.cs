namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPalette"/>.</summary>
public interface IChartPalette
{
    /// <summary>
    /// Returns the number of logical items in this value.
    /// </summary>
    /// <returns>The resulting int.</returns>
    int Count();
    /// <summary>
    /// Returns the requested color.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting color.</returns>
    ChartColor GetColor(int index);
    /// <summary>
    /// Creates an independent copy of the colors.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartColor[] CopyColors();
    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    string GetName();
    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetName(string value);
    /// <summary>Returns the current colors.</summary>
    /// <returns>The current field value.</returns>
    ChartColor[] GetColors();
    /// <summary>Replaces the current colors.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColors(ChartColor[] value);
}
