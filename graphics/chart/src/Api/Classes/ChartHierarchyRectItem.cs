namespace chart;
/// <summary>
/// Represents a hierarchy rect item used by the chart model or rendering pipeline.
/// </summary>
public class ChartHierarchyRectItem : IChartHierarchyRectItem
{
    internal int NodeIndex;
    internal string Label;
    internal double Value;
    internal int Depth;
    internal ChartRect Bounds;
    internal ChartColor Color;
    /// <summary>
    /// Initializes a hierarchy rect item from the supplied node index, label, value, depth, bounds, and color.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "depth">The depth.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartHierarchyRectItem(int nodeIndex, string label, double value, int depth, ChartRect bounds, ChartColor color)
    {
        ChartTextBlock.ValidateText(label, "label");
        ChartLinearScale.ValidateFinite(value, "value");
        ChartAxis.ValidatePlotArea(bounds);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        NodeIndex = nodeIndex;
        Label = label;
        Value = value;
        Depth = depth;
        Bounds = ChartPlot.CopyRect(bounds);
        Color = color.Copy();
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartHierarchyRectItem Copy()
    {
        return new ChartHierarchyRectItem(NodeIndex, Label, Value, Depth, Bounds, Color);
    }

    /// <summary>Returns the current node index.</summary>
    /// <returns>The current field value.</returns>
    public int GetNodeIndex()
    {
        return NodeIndex;
    }

    /// <summary>Replaces the current node index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetNodeIndex(int value)
    {
        NodeIndex = value;
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

    /// <summary>Returns the current depth.</summary>
    /// <returns>The current field value.</returns>
    public int GetDepth()
    {
        return Depth;
    }

    /// <summary>Replaces the current depth.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDepth(int value)
    {
        Depth = value;
    }

    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetBounds()
    {
        return Bounds;
    }

    /// <summary>Replaces the current bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBounds(ChartRect value)
    {
        Bounds = value;
    }

    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetColor()
    {
        return Color;
    }

    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColor(ChartColor value)
    {
        Color = value;
    }
}
