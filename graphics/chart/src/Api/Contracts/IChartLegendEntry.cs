namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLegendEntry"/>.</summary>
public interface IChartLegendEntry
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartLegendEntry Copy();
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current label.</summary>
    /// <returns>The current field value.</returns>
    string GetLabel();
    /// <summary>Replaces the current label.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLabel(string value);
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartLegendEntryKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartLegendEntryKind value);
    /// <summary>Returns the current shape.</summary>
    /// <returns>The current field value.</returns>
    ChartMarkerShape GetShape();
    /// <summary>Replaces the current shape.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShape(ChartMarkerShape value);
    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    ChartStyle GetStyle();
    /// <summary>Replaces the current style.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStyle(ChartStyle value);
    /// <summary>Returns the current gradient colors.</summary>
    /// <returns>The current field value.</returns>
    ChartColor[] GetGradientColors();
    /// <summary>Replaces the current gradient colors.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGradientColors(ChartColor[] value);
    /// <summary>Returns the current gradient offsets.</summary>
    /// <returns>The current field value.</returns>
    double[] GetGradientOffsets();
    /// <summary>Replaces the current gradient offsets.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGradientOffsets(double[] value);
    /// <summary>Returns the current custom markup.</summary>
    /// <returns>The current field value.</returns>
    string GetCustomMarkup();
    /// <summary>Replaces the current custom markup.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCustomMarkup(string value);
}
