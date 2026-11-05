namespace chart;
/// <summary>
/// Represents a visual map bin used by the chart model or rendering pipeline.
/// </summary>
public class ChartVisualMapBin : IChartVisualMapBin
{
    internal double Minimum;
    internal double Maximum;
    internal ChartColor Color;
    internal string Label;
    /// <summary>
    /// Initializes a visual map bin from the supplied minimum, maximum, color, and label.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "label">The label.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartVisualMapBin(double minimum, double maximum, ChartColor color, string label)
    {
        ChartLinearScale.ValidateDomain(minimum, maximum);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartTextBlock.ValidateText(label, "label");
        Minimum = minimum;
        Maximum = maximum;
        Color = color.Copy();
        Label = label;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartVisualMapBin Copy()
    {
        return new ChartVisualMapBin(Minimum, Maximum, Color, Label);
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
}
