namespace chart;
/// <summary>
/// Receives chart drawing operations without requiring a particular retained or immediate-mode backend.
/// </summary>
/// <remarks>Implement this interface to inspect chart output in tests or to adapt chart builders to another rendering backend.</remarks>
public interface IChartRenderTarget
{
    /// <summary>
    /// Returns the width available to the rendering target.
    /// </summary>
    /// <returns>The width in pixels or chart units.</returns>
    int GetWidth();
    /// <summary>
    /// Returns the height available to the rendering target.
    /// </summary>
    /// <returns>The height in pixels or chart units.</returns>
    int GetHeight();
    /// <summary>
    /// Adds a clear operation to the target.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddClear(ChartColor color);
    /// <summary>
    /// Adds a line operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x1">The starting x coordinate.</param>
    /// <param name = "y1">The starting y coordinate.</param>
    /// <param name = "x2">The ending x coordinate.</param>
    /// <param name = "y2">The ending y coordinate.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style);
    /// <summary>
    /// Adds a polyline operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points traversed by the polyline.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddPolyline(string id, ChartPoint[] points, ChartRenderStyle style);
    /// <summary>
    /// Adds a polygon operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The polygon vertices.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddPolygon(string id, ChartPoint[] points, ChartRenderStyle style);
    /// <summary>
    /// Adds a rectangle operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x">The left coordinate.</param>
    /// <param name = "y">The top coordinate.</param>
    /// <param name = "width">The rectangle width.</param>
    /// <param name = "height">The rectangle height.</param>
    /// <param name = "radiusX">The horizontal corner radius.</param>
    /// <param name = "radiusY">The vertical corner radius.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddRect(string id, double x, double y, double width, double height, double radiusX, double radiusY, ChartRenderStyle style);
    /// <summary>
    /// Adds a circle operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x coordinate.</param>
    /// <param name = "centerY">The center y coordinate.</param>
    /// <param name = "radius">The circle radius.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddCircle(string id, double centerX, double centerY, double radius, ChartRenderStyle style);
    /// <summary>
    /// Adds an ellipse operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x coordinate.</param>
    /// <param name = "centerY">The center y coordinate.</param>
    /// <param name = "radiusX">The horizontal radius.</param>
    /// <param name = "radiusY">The vertical radius.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddEllipse(string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style);
    /// <summary>
    /// Adds a path operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "path">The path containing move, line, curve, and close commands.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddPath(string id, ChartPath path, ChartRenderStyle style);
    /// <summary>
    /// Adds a text operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to render.</param>
    /// <param name = "x">The text x coordinate.</param>
    /// <param name = "y">The text y coordinate.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddText(string id, string text, double x, double y, ChartRenderStyle style);
    /// <summary>
    /// Adds a marker operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "centerX">The center x coordinate.</param>
    /// <param name = "centerY">The center y coordinate.</param>
    /// <param name = "size">The marker size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddMarker(string id, ChartMarkerShape shape, double centerX, double centerY, double size, ChartRenderStyle style);
    /// <summary>
    /// Adds a custom marker operation to the target.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "markerPath">The marker path.</param>
    /// <param name = "centerX">The center x coordinate.</param>
    /// <param name = "centerY">The center y coordinate.</param>
    /// <param name = "size">The marker size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    ChartRenderCommand AddCustomMarker(string id, ChartPath markerPath, double centerX, double centerY, double size, ChartRenderStyle style);
    /// <summary>Adds an open circular arc expressed in radians.</summary>
    /// <param name = "id">A stable retained identifier.</param><param name = "centerX">The center x coordinate.</param><param name = "centerY">The center y coordinate.</param><param name = "radius">The positive radius.</param><param name = "startAngle">The start angle in radians.</param><param name = "endAngle">The end angle in radians.</param><param name = "style">The render style.</param>
    /// <returns>The retained arc command.</returns>
    ChartRenderCommand AddArc(string id, double centerX, double centerY, double radius, double startAngle, double endAngle, ChartRenderStyle style);
    /// <summary>Adds a closed annular wedge expressed in radians.</summary>
    /// <param name = "id">A stable retained identifier.</param><param name = "centerX">The center x coordinate.</param><param name = "centerY">The center y coordinate.</param><param name = "innerRadius">The nonnegative inner radius.</param><param name = "outerRadius">The positive outer radius.</param><param name = "startAngle">The start angle in radians.</param><param name = "endAngle">The end angle in radians.</param><param name = "style">The render style.</param>
    /// <returns>The retained wedge command.</returns>
    ChartRenderCommand AddWedge(string id, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartRenderStyle style);
    /// <summary>Places a raw bitmap in the retained scene.</summary>
    /// <param name = "id">A stable retained identifier.</param><param name = "bitmap">The source raw bitmap.</param><param name = "x">The destination x coordinate.</param><param name = "y">The destination y coordinate.</param><param name = "width">The positive destination width.</param><param name = "height">The positive destination height.</param><param name = "style">The opacity and blend style.</param>
    /// <returns>The retained image command.</returns>
    ChartRenderCommand AddImage(string id, RawBitmap bitmap, double x, double y, double width, double height, ChartRenderStyle style);
    /// <summary>Begins a semantic retained group.</summary>
    /// <param name = "id">A stable retained identifier.</param><returns>The retained group command.</returns>
    ChartRenderCommand BeginGroup(string id);
    /// <summary>Ends the current semantic retained group.</summary>
    /// <returns>The retained group command.</returns>
    ChartRenderCommand EndGroup();
    /// <summary>Pushes an affine transform for following retained commands.</summary>
    /// <param name = "id">A stable retained identifier.</param><param name = "transform">The affine transform.</param><returns>The retained transform command.</returns>
    ChartRenderCommand PushTransform(string id, ChartMatrix transform);
    /// <summary>Restores the preceding affine transform.</summary>
    /// <returns>The retained transform command.</returns>
    ChartRenderCommand PopTransform();
    /// <summary>Pushes a rectangular clip for following retained commands.</summary>
    /// <param name = "id">A stable retained identifier.</param><param name = "bounds">The clip bounds.</param><returns>The retained clip command.</returns>
    ChartRenderCommand PushClip(string id, ChartRect bounds);
    /// <summary>Restores the preceding clip region.</summary>
    /// <returns>The retained clip command.</returns>
    ChartRenderCommand PopClip();
}
