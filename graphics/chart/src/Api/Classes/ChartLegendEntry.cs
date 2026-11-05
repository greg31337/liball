namespace chart;
/// <summary>
/// Represents a legend entry used by the chart model or rendering pipeline.
/// </summary>
public class ChartLegendEntry : IChartLegendEntry
{
    internal string Id;
    internal string Label;
    internal ChartLegendEntryKind Kind;
    internal ChartMarkerShape Shape;
    internal ChartStyle Style;
    internal ChartColor[] GradientColors;
    internal double[] GradientOffsets;
    internal string CustomMarkup;
    /// <summary>
    /// Initializes a legend entry from the supplied id, label, and kind.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "kind">The kind.</param>
    public ChartLegendEntry(string id, string label, ChartLegendEntryKind kind)
    {
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(label, "label");
        Id = id;
        Label = label;
        Kind = kind;
        Shape = ChartMarkerShape.Square;
        Style = new ChartStyle();
        GradientColors = new ChartColor[0];
        GradientOffsets = new double[0];
        CustomMarkup = "";
    }

    internal static ChartLegendEntry CreateSymbol(string id, string label, ChartMarkerShape shape, ChartStyle style)
    {
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        ChartLegendEntry entry = new ChartLegendEntry(id, label, ChartLegendEntryKind.Symbol);
        entry.Shape = shape;
        entry.Style = style.Copy();
        return entry;
    }

    internal static ChartLegendEntry CreateGradient(string id, string label, ChartColor[] colors, double[] offsets)
    {
        SvgWriter.ValidateGradient(colors, offsets);
        ChartLegendEntry entry = new ChartLegendEntry(id, label, ChartLegendEntryKind.Gradient);
        entry.GradientColors = ChartPalette.CopyColorArray(colors);
        entry.GradientOffsets = CopyDoubleArray(offsets);
        return entry;
    }

    internal static ChartLegendEntry CreateCustom(string id, string label, string customMarkup)
    {
        if (customMarkup == null)
        {
            throw new System.ArgumentNullException("customMarkup");
        }

        ChartLegendEntry entry = new ChartLegendEntry(id, label, ChartLegendEntryKind.Custom);
        entry.CustomMarkup = customMarkup;
        return entry;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartLegendEntry Copy()
    {
        ChartLegendEntry entry = new ChartLegendEntry(Id, Label, Kind);
        entry.Shape = Shape;
        entry.Style = Style.Copy();
        entry.GradientColors = ChartPalette.CopyColorArray(GradientColors);
        entry.GradientOffsets = CopyDoubleArray(GradientOffsets);
        entry.CustomMarkup = CustomMarkup;
        return entry;
    }

    internal static double[] CopyDoubleArray(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        double[] copy = new double[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
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

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartLegendEntryKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartLegendEntryKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current shape.</summary>
    /// <returns>The current field value.</returns>
    public ChartMarkerShape GetShape()
    {
        return Shape;
    }

    /// <summary>Replaces the current shape.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShape(ChartMarkerShape value)
    {
        Shape = value;
    }

    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    public ChartStyle GetStyle()
    {
        return Style;
    }

    /// <summary>Replaces the current style.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStyle(ChartStyle value)
    {
        Style = value;
    }

    /// <summary>Returns the current gradient colors.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor[] GetGradientColors()
    {
        return GradientColors;
    }

    /// <summary>Replaces the current gradient colors.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGradientColors(ChartColor[] value)
    {
        GradientColors = value;
    }

    /// <summary>Returns the current gradient offsets.</summary>
    /// <returns>The current field value.</returns>
    public double[] GetGradientOffsets()
    {
        return GradientOffsets;
    }

    /// <summary>Replaces the current gradient offsets.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGradientOffsets(double[] value)
    {
        GradientOffsets = value;
    }

    /// <summary>Returns the current custom markup.</summary>
    /// <returns>The current field value.</returns>
    public string GetCustomMarkup()
    {
        return CustomMarkup;
    }

    /// <summary>Replaces the current custom markup.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCustomMarkup(string value)
    {
        CustomMarkup = value;
    }
}
