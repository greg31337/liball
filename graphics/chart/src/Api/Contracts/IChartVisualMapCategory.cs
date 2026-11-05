namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartVisualMapCategory"/>.</summary>
public interface IChartVisualMapCategory
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartVisualMapCategory Copy();
    /// <summary>Returns the current category.</summary>
    /// <returns>The current field value.</returns>
    string GetCategory();
    /// <summary>Replaces the current category.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategory(string value);
    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetColor();
    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColor(ChartColor value);
}
