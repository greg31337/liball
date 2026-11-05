namespace chart;
/// <summary>
/// Represents a hierarchy node used by the chart model or rendering pipeline.
/// </summary>
public class ChartHierarchyNode : IChartHierarchyNode
{
    internal string Label;
    internal double Value;
    internal int ParentIndex;
    /// <summary>
    /// Initializes a hierarchy node from the supplied label, value, and parent index.
    /// </summary>
    /// <param name = "label">The label.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "parentIndex">The zero-based parent index.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHierarchyNode(string label, double value, int parentIndex)
    {
        ChartTextBlock.ValidateText(label, "label");
        ChartLinearScale.ValidateFinite(value, "value");
        if (value < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        Label = label;
        Value = value;
        ParentIndex = parentIndex;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartHierarchyNode Copy()
    {
        return new ChartHierarchyNode(Label, Value, ParentIndex);
    }

    /// <summary>Returns the current label.</summary>
    /// <returns>The current field value.</returns>
    public string GetLabel()
    {
        return Label;
    }

    /// <summary>Replaces the current label.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLabel(string value)
    {
        Label = value;
    }

    /// <summary>Returns the current value.</summary>
    /// <returns>The current field value.</returns>
    public double GetValue()
    {
        return Value;
    }

    /// <summary>Replaces the current value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetValue(double value)
    {
        Value = value;
    }

    /// <summary>Returns the current parent index.</summary>
    /// <returns>The current field value.</returns>
    public int GetParentIndex()
    {
        return ParentIndex;
    }

    /// <summary>Replaces the current parent index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetParentIndex(int value)
    {
        ParentIndex = value;
    }
}
