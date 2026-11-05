namespace chart;
/// <summary>Creates and converts chart render command values.</summary>
public class ChartRenderCommandFactory : IChartRenderCommandFactory
{
    /// <summary>Initializes a stateless chart render command factory facade.</summary>
    public ChartRenderCommandFactory()
    {
    }

    /// <summary>
    /// Creates the requested clear from the supplied data and configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns>The newly created render command.</returns>
    public ChartRenderCommand CreateClear(ChartColor color)
    {
        return ChartRenderCommand.CreateClear(color);
    }

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
    public ChartRenderCommand CreateLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateLine(id, x1, y1, x2, y2, style);
    }

    /// <summary>
    /// Creates the requested polyline from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    public ChartRenderCommand CreatePolyline(string id, ChartPoint[] points, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreatePolyline(id, points, style);
    }

    /// <summary>
    /// Creates the requested polygon from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    public ChartRenderCommand CreatePolygon(string id, ChartPoint[] points, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreatePolygon(id, points, style);
    }

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
    public ChartRenderCommand CreateRect(string id, double x, double y, double width, double height, double radiusX, double radiusY, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateRect(id, x, y, width, height, radiusX, radiusY, style);
    }

    /// <summary>
    /// Creates the requested circle from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    public ChartRenderCommand CreateCircle(string id, double centerX, double centerY, double radius, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateCircle(id, centerX, centerY, radius, style);
    }

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
    public ChartRenderCommand CreateEllipse(string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateEllipse(id, centerX, centerY, radiusX, radiusY, style);
    }

    /// <summary>
    /// Creates the requested path from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "path">The path containing move, line, curve, and close commands.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartRenderCommand CreatePath(string id, ChartPath path, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreatePath(id, path, style);
    }

    /// <summary>
    /// Creates the requested text from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render command.</returns>
    public ChartRenderCommand CreateText(string id, string text, double x, double y, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateText(id, text, x, y, style);
    }

    /// <summary>Creates a retained command from a complete public text block.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "textBlock">The configured public text block.</param>
    /// <param name = "x">The horizontal anchor coordinate.</param>
    /// <param name = "y">The vertical anchor coordinate.</param>
    /// <returns>The retained text command.</returns>
    public ChartRenderCommand CreateTextBlock(string id, ChartTextBlock textBlock, double x, double y)
    {
        return ChartRenderCommand.CreateTextBlock(id, textBlock, x, y);
    }

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
    public ChartRenderCommand CreateMarker(string id, ChartMarkerShape shape, double centerX, double centerY, double size, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateMarker(id, shape, centerX, centerY, size, style);
    }

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
    public ChartRenderCommand CreateCustomMarker(string id, ChartPath markerPath, double centerX, double centerY, double size, ChartRenderStyle style)
    {
        return ChartRenderCommand.CreateCustomMarker(id, markerPath, centerX, centerY, size, style);
    }
}
