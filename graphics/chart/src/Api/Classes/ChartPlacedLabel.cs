namespace chart;
/// <summary>
/// Represents a placed label used by the chart model or rendering pipeline.
/// </summary>
public class ChartPlacedLabel : IChartPlacedLabel
{
    internal string Id;
    internal string Text;
    internal int SourceIndex;
    internal double AnchorX;
    internal double AnchorY;
    internal double X;
    internal double Y;
    internal double Width;
    internal double Height;
    internal bool Visible;
    internal ChartLabelPosition Position;
    /// <summary>
    /// Initializes a placed label from the supplied id, text, source index, anchor x, anchor y, x, y, width, height, visible, and position.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "sourceIndex">The zero-based source index.</param>
    /// <param name = "anchorX">The anchor x.</param>
    /// <param name = "anchorY">The anchor y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "visible">The visible.</param>
    /// <param name = "position">The position.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPlacedLabel(string id, string text, int sourceIndex, double anchorX, double anchorY, double x, double y, double width, double height, bool visible, ChartLabelPosition position)
    {
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        if (sourceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceIndex");
        }

        ChartRenderCommand.ValidateFinitePair(anchorX, anchorY, "anchor");
        ChartRenderCommand.ValidateFinitePair(x, y, "label");
        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        ChartLabelPlacementChartsOperations.ValidatePosition(position);
        Id = id;
        Text = text;
        SourceIndex = sourceIndex;
        AnchorX = anchorX;
        AnchorY = anchorY;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Visible = visible;
        Position = position;
    }

    /// <summary>
    /// Applies the bounds operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    public ChartRect Bounds()
    {
        return new ChartRect(X, Y, Width, Height);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartPlacedLabel Copy()
    {
        return new ChartPlacedLabel(Id, Text, SourceIndex, AnchorX, AnchorY, X, Y, Width, Height, Visible, Position);
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    public string GetText()
    {
        return Text;
    }

    /// <summary>Replaces the current text.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetText(string value)
    {
        Text = value;
    }

    /// <summary>Returns the current source index.</summary>
    /// <returns>The current field value.</returns>
    public int GetSourceIndex()
    {
        return SourceIndex;
    }

    /// <summary>Replaces the current source index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSourceIndex(int value)
    {
        SourceIndex = value;
    }

    /// <summary>Returns the current anchor x.</summary>
    /// <returns>The current field value.</returns>
    public double GetAnchorX()
    {
        return AnchorX;
    }

    /// <summary>Replaces the current anchor x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAnchorX(double value)
    {
        AnchorX = value;
    }

    /// <summary>Returns the current anchor y.</summary>
    /// <returns>The current field value.</returns>
    public double GetAnchorY()
    {
        return AnchorY;
    }

    /// <summary>Replaces the current anchor y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAnchorY(double value)
    {
        AnchorY = value;
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

    /// <summary>Returns the current visible.</summary>
    /// <returns>The current field value.</returns>
    public bool GetVisible()
    {
        return Visible;
    }

    /// <summary>Replaces the current visible.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetVisible(bool value)
    {
        Visible = value;
    }

    /// <summary>Returns the current position.</summary>
    /// <returns>The current field value.</returns>
    public ChartLabelPosition GetPosition()
    {
        return Position;
    }

    /// <summary>Replaces the current position.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPosition(ChartLabelPosition value)
    {
        Position = value;
    }
}
