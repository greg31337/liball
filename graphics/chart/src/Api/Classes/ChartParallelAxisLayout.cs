namespace chart;
/// <summary>
/// Represents a parallel axis layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartParallelAxisLayout : IChartParallelAxisLayout
{
    internal int AxisIndex;
    internal string Label;
    internal double X;
    internal double Minimum;
    internal double Maximum;
    /// <summary>
    /// Initializes a parallel axis layout from the supplied axis index, label, x, minimum, and maximum.
    /// </summary>
    /// <param name = "axisIndex">The zero-based axis index.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    public ChartParallelAxisLayout(int axisIndex, string label, double x, double minimum, double maximum)
    {
        ChartTextBlock.ValidateText(label, "label");
        ChartLinearScale.ValidateFinite(x, "x");
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        AxisIndex = axisIndex;
        Label = label;
        X = x;
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartParallelAxisLayout Copy()
    {
        return new ChartParallelAxisLayout(AxisIndex, Label, X, Minimum, Maximum);
    }

    /// <summary>Returns the current axis index.</summary>
    /// <returns>The current field value.</returns>
    public int GetAxisIndex()
    {
        return AxisIndex;
    }

    /// <summary>Replaces the current axis index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAxisIndex(int value)
    {
        AxisIndex = value;
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

    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimum()
    {
        return Minimum;
    }

    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimum(double value)
    {
        Minimum = value;
    }

    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMaximum()
    {
        return Maximum;
    }

    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMaximum(double value)
    {
        Maximum = value;
    }
}
