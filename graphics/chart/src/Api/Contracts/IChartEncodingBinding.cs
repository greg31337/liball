namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartEncodingBinding"/>.</summary>
public interface IChartEncodingBinding
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartEncodingBinding Copy();
    /// <summary>Returns the current channel.</summary>
    /// <returns>The current field value.</returns>
    ChartEncodingChannel GetChannel();
    /// <summary>Replaces the current channel.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetChannel(ChartEncodingChannel value);
    /// <summary>Returns the current field name.</summary>
    /// <returns>The current field value.</returns>
    string GetFieldName();
    /// <summary>Replaces the current field name.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFieldName(string value);
    /// <summary>Returns the current field kind.</summary>
    /// <returns>The current field value.</returns>
    ChartDataKind GetFieldKind();
    /// <summary>Replaces the current field kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFieldKind(ChartDataKind value);
    /// <summary>Returns the current is constant.</summary>
    /// <returns>The current field value.</returns>
    bool GetIsConstant();
    /// <summary>Replaces the current is constant.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIsConstant(bool value);
    /// <summary>Returns the current number value.</summary>
    /// <returns>The current field value.</returns>
    double GetNumberValue();
    /// <summary>Replaces the current number value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNumberValue(double value);
    /// <summary>Returns the current text value.</summary>
    /// <returns>The current field value.</returns>
    string GetTextValue();
    /// <summary>Replaces the current text value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTextValue(string value);
    /// <summary>Returns the current color value.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetColorValue();
    /// <summary>Replaces the current color value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColorValue(ChartColor value);
    /// <summary>Returns the current shape value.</summary>
    /// <returns>The current field value.</returns>
    ChartMarkerShape GetShapeValue();
    /// <summary>Replaces the current shape value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShapeValue(ChartMarkerShape value);
}
