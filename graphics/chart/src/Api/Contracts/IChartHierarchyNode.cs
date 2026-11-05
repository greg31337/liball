namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHierarchyNode"/>.</summary>
public interface IChartHierarchyNode
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartHierarchyNode Copy();
    /// <summary>Returns the current label.</summary>
    /// <returns>The current field value.</returns>
    string GetLabel();
    /// <summary>Replaces the current label.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLabel(string value);
    /// <summary>Returns the current value.</summary>
    /// <returns>The current field value.</returns>
    double GetValue();
    /// <summary>Replaces the current value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValue(double value);
    /// <summary>Returns the current parent index.</summary>
    /// <returns>The current field value.</returns>
    int GetParentIndex();
    /// <summary>Replaces the current parent index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetParentIndex(int value);
}
