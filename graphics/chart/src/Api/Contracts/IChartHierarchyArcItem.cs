namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHierarchyArcItem"/>.</summary>
public interface IChartHierarchyArcItem
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartHierarchyArcItem Copy();
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
    /// <summary>Returns the current start angle.</summary>
    /// <returns>The current field value.</returns>
    double GetStartAngle();
    /// <summary>Replaces the current start angle.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStartAngle(double value);
    /// <summary>Returns the current end angle.</summary>
    /// <returns>The current field value.</returns>
    double GetEndAngle();
    /// <summary>Replaces the current end angle.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetEndAngle(double value);
    /// <summary>Returns the current inner radius.</summary>
    /// <returns>The current field value.</returns>
    double GetInnerRadius();
    /// <summary>Replaces the current inner radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetInnerRadius(double value);
    /// <summary>Returns the current outer radius.</summary>
    /// <returns>The current field value.</returns>
    double GetOuterRadius();
    /// <summary>Replaces the current outer radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetOuterRadius(double value);
    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetColor();
    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColor(ChartColor value);
}
