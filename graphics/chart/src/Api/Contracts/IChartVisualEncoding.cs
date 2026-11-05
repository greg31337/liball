namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartVisualEncoding"/>.</summary>
public interface IChartVisualEncoding
{
    /// <summary>
    /// Sets the field configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetField(ChartEncodingChannel channel, string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the x configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetX(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the y configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetY(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the x2 configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetX2(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the y2 configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetY2(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the color configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetColor(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the size configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetSize(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the shape configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetShape(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the opacity configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetOpacity(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the text configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetText(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the tooltip configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetTooltip(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the order configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetOrder(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the detail configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetDetail(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the row configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetRow(string fieldName, ChartDataKind fieldKind);
    /// <summary>
    /// Sets the column configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetColumn(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the radial position field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetRadius(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the angular position field in radians.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetAngle(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the latitude position field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetLatitude(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the longitude position field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetLongitude(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the stroke-color field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetStrokeColor(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the fill-color field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetFillColor(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the stroke-width field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetStrokeWidth(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the dash-length field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetDash(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the value-label field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetValueLabel(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the z-index field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetZIndex(string fieldName, ChartDataKind fieldKind);
    /// <summary>Sets the wrap-facet field.</summary>
    /// <param name = "fieldName">The field name.</param><param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetWrap(string fieldName, ChartDataKind fieldKind);
    /// <summary>Attaches live selection and highlight state to this encoding.</summary>
    /// <param name="selection">The live selection state.</param>
    void SetSelection(ChartSelection selection);
    /// <summary>Removes the attached selection state.</summary>
    void ClearSelection();
    /// <summary>Adds or replaces a conditional encoding for one channel.</summary>
    /// <param name="condition">The conditional encoding.</param>
    /// <returns>The stored conditional encoding.</returns>
    ChartConditionalEncoding SetConditionalEncoding(ChartConditionalEncoding condition);
    /// <summary>Returns whether a channel has a conditional encoding.</summary>
    /// <param name="channel">The channel.</param>
    /// <returns><c>true</c> when a condition exists; otherwise, <c>false</c>.</returns>
    bool HasConditionalEncoding(ChartEncodingChannel channel);
    /// <summary>Returns the conditional encoding for a channel.</summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The conditional encoding.</returns>
    ChartConditionalEncoding ConditionalEncodingFor(ChartEncodingChannel channel);
    /// <summary>
    /// Sets the constant number configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetConstantNumber(ChartEncodingChannel channel, double value);
    /// <summary>
    /// Sets the constant text configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetConstantText(ChartEncodingChannel channel, string value);
    /// <summary>
    /// Sets the constant color configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetConstantColor(ChartEncodingChannel channel, ChartColor value);
    /// <summary>
    /// Sets the constant shape configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    ChartEncodingBinding SetConstantShape(ChartEncodingChannel channel, ChartMarkerShape value);
    /// <summary>
    /// Sets the binding configuration.
    /// </summary>
    /// <param name = "binding">The binding.</param>
    /// <returns>The resulting encoding binding.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartEncodingBinding SetBinding(ChartEncodingBinding binding);
    /// <summary>
    /// Determines whether channel.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool HasChannel(ChartEncodingChannel channel);
    /// <summary>
    /// Groups values into ding index using the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns>The resulting int.</returns>
    int BindingIndex(ChartEncodingChannel channel);
    /// <summary>
    /// Groups values into ding for using the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns>The resulting encoding binding.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartEncodingBinding BindingFor(ChartEncodingChannel channel);
    /// <summary>
    /// Groups values into ding at using the supplied data and configuration.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting encoding binding.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartEncodingBinding BindingAt(int index);
    /// <summary>
    /// Creates an independent copy of the bindings.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartEncodingBinding[] CopyBindings();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartVisualEncoding Copy();
    /// <summary>
    /// Validates against table and returns structured issues without rendering.
    /// </summary>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void ValidateAgainstTable(ChartDataTable table);
    /// <summary>Returns the current bindings.</summary>
    /// <returns>The current field value.</returns>
    ChartEncodingBinding[] GetBindings();
    /// <summary>Replaces the current bindings.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBindings(ChartEncodingBinding[] value);
    /// <summary>Returns the current binding count.</summary>
    /// <returns>The current field value.</returns>
    int GetBindingCount();
    /// <summary>Replaces the current binding count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBindingCount(int value);
}
