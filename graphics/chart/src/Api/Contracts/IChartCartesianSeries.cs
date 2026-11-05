namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartCartesianSeries"/>.</summary>
public interface IChartCartesianSeries
{
    /// <summary>
    /// Sets the order configuration.
    /// </summary>
    /// <param name = "order">The order.</param>
    void SetOrder(int order);
    /// <summary>
    /// Sets the clip to plot area configuration.
    /// </summary>
    /// <param name = "clipToPlotArea">The clip to plot area.</param>
    void SetClipToPlotArea(bool clipToPlotArea);
    /// <summary>
    /// Adds the supplied command to this object.
    /// </summary>
    /// <param name = "command">The command.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartRenderCommand AddCommand(ChartRenderCommand command);
    /// <summary>
    /// Adds the supplied line to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style);
    /// <summary>
    /// Adds the supplied polyline to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddPolyline(string id, double[] xValues, double[] yValues, ChartRenderStyle style);
    /// <summary>
    /// Adds the supplied polygon to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddPolygon(string id, double[] xValues, double[] yValues, ChartRenderStyle style);
    /// <summary>
    /// Adds the supplied marker to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "size">The size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddMarker(string id, ChartMarkerShape shape, double x, double y, double size, ChartRenderStyle style);
    /// <summary>Adds a custom path marker in data coordinates.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "markerPath">The normalized custom marker path.</param>
    /// <param name = "x">The horizontal data value.</param>
    /// <param name = "y">The vertical data value.</param>
    /// <param name = "size">The positive rendered marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The retained custom marker command.</returns>
    ChartRenderCommand AddCustomMarker(string id, ChartPath markerPath, double x, double y, double size, ChartRenderStyle style);
    /// <summary>
    /// Adds the supplied text to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddText(string id, string text, double x, double y, ChartRenderStyle style);
    /// <summary>
    /// Returns the requested command.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartRenderCommand CommandAt(int index);
    /// <summary>Replaces the style of one retained point or command without changing the series default.</summary>
    /// <param name = "index">The zero-based command index.</param>
    /// <param name = "style">The point-specific rendering style.</param>
    void SetPointStyle(int index, ChartRenderStyle style);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartCartesianSeries Copy();
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartCartesianSeriesKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartCartesianSeriesKind value);
    /// <summary>Returns the current order.</summary>
    /// <returns>The current field value.</returns>
    int GetOrder();
    /// <summary>Returns the current sequence.</summary>
    /// <returns>The current field value.</returns>
    int GetSequence();
    /// <summary>Replaces the current sequence.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSequence(int value);
    /// <summary>Returns the current clip to plot area.</summary>
    /// <returns>The current field value.</returns>
    bool GetClipToPlotArea();
    /// <summary>Returns the current commands.</summary>
    /// <returns>The current field value.</returns>
    ChartRenderCommand[] GetCommands();
    /// <summary>Replaces the current commands.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCommands(ChartRenderCommand[] value);
    /// <summary>Returns the current command count.</summary>
    /// <returns>The current field value.</returns>
    int GetCommandCount();
    /// <summary>Replaces the current command count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCommandCount(int value);
}
