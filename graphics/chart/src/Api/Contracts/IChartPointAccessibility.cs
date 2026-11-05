namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPointAccessibility"/>.</summary>
public interface IChartPointAccessibility
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartPointAccessibility Copy();
    /// <summary>Returns the current point id.</summary>
    /// <returns>The current field value.</returns>
    string GetPointId();
    /// <summary>Replaces the current point id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPointId(string value);
    /// <summary>Returns the current description.</summary>
    /// <returns>The current field value.</returns>
    string GetDescription();
    /// <summary>Replaces the current description.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDescription(string value);
    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    int GetRowIndex();
    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowIndex(int value);
}
