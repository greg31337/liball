namespace chart;
/// <summary>
/// Represents a hierarchy arc item used by the chart model or rendering pipeline.
/// </summary>
public class ChartHierarchyArcItem : IChartHierarchyArcItem
{
    internal int NodeIndex;
    internal string Label;
    internal double Value;
    internal int Depth;
    internal double StartAngle;
    internal double EndAngle;
    internal double InnerRadius;
    internal double OuterRadius;
    internal ChartColor Color;
    /// <summary>
    /// Initializes a hierarchy arc item from the supplied node index, label, value, depth, start angle, end angle, inner radius, outer radius, and color.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "depth">The depth.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "endAngle">The ending angle in degrees.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartHierarchyArcItem(int nodeIndex, string label, double value, int depth, double startAngle, double endAngle, double innerRadius, double outerRadius, ChartColor color)
    {
        ChartTextBlock.ValidateText(label, "label");
        ChartLinearScale.ValidateFinite(value, "value");
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        ChartLinearScale.ValidateFinite(endAngle, "endAngle");
        ChartPieSlice.ValidateRadii(innerRadius, outerRadius);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        NodeIndex = nodeIndex;
        Label = label;
        Value = value;
        Depth = depth;
        StartAngle = startAngle;
        EndAngle = endAngle;
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
        Color = color.Copy();
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartHierarchyArcItem Copy()
    {
        return new ChartHierarchyArcItem(NodeIndex, Label, Value, Depth, StartAngle, EndAngle, InnerRadius, OuterRadius, Color);
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

    /// <summary>Returns the current start angle.</summary>
    /// <returns>The current field value.</returns>
    public double GetStartAngle()
    {
        return StartAngle;
    }

    /// <summary>Replaces the current start angle.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStartAngle(double value)
    {
        StartAngle = value;
    }

    /// <summary>Returns the current end angle.</summary>
    /// <returns>The current field value.</returns>
    public double GetEndAngle()
    {
        return EndAngle;
    }

    /// <summary>Replaces the current end angle.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetEndAngle(double value)
    {
        EndAngle = value;
    }

    /// <summary>Returns the current inner radius.</summary>
    /// <returns>The current field value.</returns>
    public double GetInnerRadius()
    {
        return InnerRadius;
    }

    /// <summary>Replaces the current inner radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetInnerRadius(double value)
    {
        InnerRadius = value;
    }

    /// <summary>Returns the current outer radius.</summary>
    /// <returns>The current field value.</returns>
    public double GetOuterRadius()
    {
        return OuterRadius;
    }

    /// <summary>Replaces the current outer radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetOuterRadius(double value)
    {
        OuterRadius = value;
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
