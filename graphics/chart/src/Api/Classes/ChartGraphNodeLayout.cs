namespace chart;
/// <summary>
/// Represents a graph node layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartGraphNodeLayout : IChartGraphNodeLayout
{
    internal int NodeIndex;
    internal string Label;
    internal double X;
    internal double Y;
    internal double Width;
    internal double Height;
    internal int Layer;
    /// <summary>
    /// Initializes a graph node layout from the supplied node index, label, x, y, width, height, and layer.
    /// </summary>
    /// <param name = "nodeIndex">The zero-based node index.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "layer">The layer.</param>
    public ChartGraphNodeLayout(int nodeIndex, string label, double x, double y, double width, double height, int layer)
    {
        ChartTextBlock.ValidateText(label, "label");
        ChartLinearScale.ValidateFinite(x, "x");
        ChartLinearScale.ValidateFinite(y, "y");
        ChartStyle.ValidateNonNegative(width, "width");
        ChartStyle.ValidateNonNegative(height, "height");
        NodeIndex = nodeIndex;
        Label = label;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Layer = layer;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartGraphNodeLayout Copy()
    {
        return new ChartGraphNodeLayout(NodeIndex, Label, X, Y, Width, Height, Layer);
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

    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    public double GetX()
    {
        return X;
    }

    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX(double value)
    {
        X = value;
    }

    /// <summary>Returns the current y.</summary>
    /// <returns>The current field value.</returns>
    public double GetY()
    {
        return Y;
    }

    /// <summary>Replaces the current y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY(double value)
    {
        Y = value;
    }

    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    public double GetWidth()
    {
        return Width;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(double value)
    {
        Width = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public double GetHeight()
    {
        return Height;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(double value)
    {
        Height = value;
    }

    /// <summary>Returns the current layer.</summary>
    /// <returns>The current field value.</returns>
    public int GetLayer()
    {
        return Layer;
    }

    /// <summary>Replaces the current layer.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLayer(int value)
    {
        Layer = value;
    }
}
