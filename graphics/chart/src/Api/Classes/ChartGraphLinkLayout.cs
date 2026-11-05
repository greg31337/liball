namespace chart;
/// <summary>
/// Represents a graph link layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartGraphLinkLayout : IChartGraphLinkLayout
{
    internal int SourceIndex;
    internal int TargetIndex;
    internal double X1;
    internal double Y1;
    internal double X2;
    internal double Y2;
    internal double Value;
    internal double Width;
    /// <summary>
    /// Initializes a graph link layout from the supplied source index, target index, x1, y1, x2, y2, value, and width.
    /// </summary>
    /// <param name = "sourceIndex">The zero-based source index.</param>
    /// <param name = "targetIndex">The zero-based target index.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartGraphLinkLayout(int sourceIndex, int targetIndex, double x1, double y1, double x2, double y2, double value, double width)
    {
        if (sourceIndex < 0 || targetIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceIndex");
        }

        ChartLinearScale.ValidateFinite(x1, "x1");
        ChartLinearScale.ValidateFinite(y1, "y1");
        ChartLinearScale.ValidateFinite(x2, "x2");
        ChartLinearScale.ValidateFinite(y2, "y2");
        ChartLinearScale.ValidateFinite(value, "value");
        ChartStyle.ValidateNonNegative(width, "width");
        SourceIndex = sourceIndex;
        TargetIndex = targetIndex;
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        Value = value;
        Width = width;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartGraphLinkLayout Copy()
    {
        return new ChartGraphLinkLayout(SourceIndex, TargetIndex, X1, Y1, X2, Y2, Value, Width);
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

    /// <summary>Returns the current target index.</summary>
    /// <returns>The current field value.</returns>
    public int GetTargetIndex()
    {
        return TargetIndex;
    }

    /// <summary>Replaces the current target index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTargetIndex(int value)
    {
        TargetIndex = value;
    }

    /// <summary>Returns the current x1.</summary>
    /// <returns>The current field value.</returns>
    public double GetX1()
    {
        return X1;
    }

    /// <summary>Replaces the current x1.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX1(double value)
    {
        X1 = value;
    }

    /// <summary>Returns the current y1.</summary>
    /// <returns>The current field value.</returns>
    public double GetY1()
    {
        return Y1;
    }

    /// <summary>Replaces the current y1.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY1(double value)
    {
        Y1 = value;
    }

    /// <summary>Returns the current x2.</summary>
    /// <returns>The current field value.</returns>
    public double GetX2()
    {
        return X2;
    }

    /// <summary>Replaces the current x2.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX2(double value)
    {
        X2 = value;
    }

    /// <summary>Returns the current y2.</summary>
    /// <returns>The current field value.</returns>
    public double GetY2()
    {
        return Y2;
    }

    /// <summary>Replaces the current y2.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY2(double value)
    {
        Y2 = value;
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
}
