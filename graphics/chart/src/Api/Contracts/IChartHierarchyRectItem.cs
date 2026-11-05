namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHierarchyRectItem"/>.</summary>
public interface IChartHierarchyRectItem
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartHierarchyRectItem Copy();
    /// <summary>Returns the current node index.</summary>
    /// <returns>The current field value.</returns>
    int GetNodeIndex();
    /// <summary>Replaces the current node index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNodeIndex(int value);
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
    /// <summary>Returns the current depth.</summary>
    /// <returns>The current field value.</returns>
    int GetDepth();
    /// <summary>Replaces the current depth.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDepth(int value);
    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetBounds();
    /// <summary>Replaces the current bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBounds(ChartRect value);
    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetColor();
    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColor(ChartColor value);
}
