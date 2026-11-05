namespace chart;
/// <summary>Defines substitutable chart render command factory operations.</summary>
public interface IChartRenderCommandFactory
{
    /// <summary>
    /// Creates the requested clear from the supplied data and configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateClear(ChartColor color);
    /// <summary>
    /// Creates the requested line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested polyline from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreatePolyline(string id, ChartPoint[] points, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested polygon from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreatePolygon(string id, ChartPoint[] points, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested rect from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "radiusX">The radius x.</param>
    /// <param name = "radiusY">The radius y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateRect(string id, double x, double y, double width, double height, double radiusX, double radiusY, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested circle from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateCircle(string id, double centerX, double centerY, double radius, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested ellipse from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radiusX">The radius x.</param>
    /// <param name = "radiusY">The radius y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateEllipse(string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested path from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "path">The path containing move, line, curve, and close commands.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartRenderCommand CreatePath(string id, ChartPath path, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested text from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateText(string id, string text, double x, double y, ChartRenderStyle style);
    /// <summary>Creates a retained command from a complete public text block.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "textBlock">The configured public text block.</param>
    /// <param name = "x">The horizontal anchor coordinate.</param>
    /// <param name = "y">The vertical anchor coordinate.</param>
    /// <returns>The retained text command.</returns>
    ChartRenderCommand CreateTextBlock(string id, ChartTextBlock textBlock, double x, double y);
    /// <summary>
    /// Creates the requested marker from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "size">The size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    ChartRenderCommand CreateMarker(string id, ChartMarkerShape shape, double centerX, double centerY, double size, ChartRenderStyle style);
    /// <summary>
    /// Creates the requested custom marker from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "markerPath">The marker path.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "size">The size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartRenderCommand CreateCustomMarker(string id, ChartPath markerPath, double centerX, double centerY, double size, ChartRenderStyle style);
}
