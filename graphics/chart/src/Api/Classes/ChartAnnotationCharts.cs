namespace chart;
/// <summary>Provides the public chart annotation charts operations.</summary>
public class ChartAnnotationCharts : IChartAnnotationCharts
{
    /// <summary>Initializes a stateless chart annotation charts facade.</summary>
    public ChartAnnotationCharts()
    {
    }

    /// <summary>Maps an annotation point from its configured coordinate unit into scene pixels.</summary>
    /// <param name = "scene">The retained scene that supplies paper dimensions.</param>
    /// <param name = "point">The point to map.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <returns>The mapped scene point.</returns>
    public ChartPoint MapCoordinatePoint(IChartRenderTarget scene, ChartPoint point, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain)
    {
        return ChartAnnotationChartsOperations.MapCoordinatePoint(scene, point, configuration, plotArea, xDomain, yDomain);
    }

    /// <summary>Maps annotation points from their configured coordinate unit into scene pixels.</summary>
    /// <param name = "scene">The retained scene that supplies paper dimensions.</param>
    /// <param name = "points">The points to map.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <returns>The mapped scene points.</returns>
    public ChartPoint[] MapCoordinatePoints(IChartRenderTarget scene, ChartPoint[] points, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain)
    {
        return ChartAnnotationChartsOperations.MapCoordinatePoints(scene, points, configuration, plotArea, xDomain, yDomain);
    }

    /// <summary>Adds text whose anchor is expressed in the configured coordinate unit.</summary>
    /// <param name = "scene">The retained scene that receives the text.</param>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "text">The annotation text.</param>
    /// <param name = "point">The text anchor.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <param name = "style">The text render style.</param>
    /// <returns>The retained text command.</returns>
    public ChartRenderCommand AddCoordinateText(IChartRenderTarget scene, string id, string text, ChartPoint point, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddCoordinateText(scene, id, text, point, configuration, plotArea, xDomain, yDomain, style);
    }

    /// <summary>Adds an arrow whose endpoints are expressed in the configured coordinate unit.</summary>
    /// <param name = "scene">The retained scene that receives the arrow.</param>
    /// <param name = "id">A stable retained identifier prefix.</param>
    /// <param name = "start">The arrow start.</param>
    /// <param name = "end">The arrow end.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <param name = "headLength">The positive arrow-head length in pixels.</param>
    /// <param name = "headWidth">The positive arrow-head width in pixels.</param>
    /// <param name = "style">The arrow render style.</param>
    public void AddCoordinateArrow(IChartRenderTarget scene, string id, ChartPoint start, ChartPoint end, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, double headLength, double headWidth, ChartRenderStyle style)
    {
        ChartAnnotationChartsOperations.AddCoordinateArrow(scene, id, start, end, configuration, plotArea, xDomain, yDomain, headLength, headWidth, style);
    }

    /// <summary>Adds a reference rectangle expressed in the configured coordinate unit.</summary>
    /// <param name = "scene">The retained scene that receives the shape.</param>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "rect">The source rectangle.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <param name = "radius">The nonnegative corner radius in pixels.</param>
    /// <param name = "style">The shape render style.</param>
    /// <returns>The retained rectangle command.</returns>
    public ChartRenderCommand AddReferenceRectangle(IChartRenderTarget scene, string id, ChartRect rect, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, double radius, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddReferenceRectangle(scene, id, rect, configuration, plotArea, xDomain, yDomain, radius, style);
    }

    /// <summary>Adds a reference ellipse expressed in the configured coordinate unit.</summary>
    /// <param name = "scene">The retained scene that receives the shape.</param>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "center">The source ellipse center.</param>
    /// <param name = "radiusX">The positive horizontal radius in the source coordinate unit.</param>
    /// <param name = "radiusY">The positive vertical radius in the source coordinate unit.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <param name = "style">The shape render style.</param>
    /// <returns>The retained ellipse command.</returns>
    public ChartRenderCommand AddReferenceEllipse(IChartRenderTarget scene, string id, ChartPoint center, double radiusX, double radiusY, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddReferenceEllipse(scene, id, center, radiusX, radiusY, configuration, plotArea, xDomain, yDomain, style);
    }

    /// <summary>Adds a reference polygon expressed in the configured coordinate unit.</summary>
    /// <param name = "scene">The retained scene that receives the shape.</param>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "points">The source polygon points.</param>
    /// <param name = "configuration">The coordinate and scale configuration.</param>
    /// <param name = "plotArea">The rectangle occupied by the plot.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <param name = "style">The shape render style.</param>
    /// <returns>The retained polygon command.</returns>
    public ChartRenderCommand AddReferencePolygon(IChartRenderTarget scene, string id, ChartPoint[] points, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddReferencePolygon(scene, id, points, configuration, plotArea, xDomain, yDomain, style);
    }

    /// <summary>Adds a raw bitmap covering the complete scene.</summary>
    /// <remarks>Add this command before foreground commands so the image remains in the background.</remarks>
    /// <param name = "scene">The retained scene that receives the image.</param>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "bitmap">The source raw bitmap.</param>
    /// <param name = "style">The image opacity and blend style.</param>
    /// <returns>The retained image command.</returns>
    public ChartRenderCommand AddBackgroundImage(IChartRenderTarget scene, string id, RawBitmap bitmap, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddBackgroundImage(scene, id, bitmap, style);
    }

    /// <summary>
    /// Adds the supplied text to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddText(IChartRenderTarget scene, string id, string text, double x, double y, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddText(scene, id, text, x, y, style);
    }

    /// <summary>
    /// Adds the supplied arrow to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <param name = "headLength">The head length.</param>
    /// <param name = "headWidth">The head width.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    public void AddArrow(IChartRenderTarget scene, string id, double x1, double y1, double x2, double y2, double headLength, double headWidth, ChartRenderStyle style)
    {
        ChartAnnotationChartsOperations.AddArrow(scene, id, x1, y1, x2, y2, headLength, headWidth, style);
    }

    /// <summary>
    /// Adds the supplied callout to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "box">The box.</param>
    /// <param name = "anchorX">The anchor x.</param>
    /// <param name = "anchorY">The anchor y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "boxStyle">The rendering style used for box.</param>
    /// <param name = "leaderStyle">The rendering style used for leader.</param>
    /// <param name = "textStyle">The rendering style used for generated text.</param>
    public void AddCallout(IChartRenderTarget scene, string id, string text, ChartRect box, double anchorX, double anchorY, double radius, ChartRenderStyle boxStyle, ChartRenderStyle leaderStyle, ChartRenderStyle textStyle)
    {
        ChartAnnotationChartsOperations.AddCallout(scene, id, text, box, anchorX, anchorY, radius, boxStyle, leaderStyle, textStyle);
    }

    /// <summary>
    /// Adds the supplied rectangle shape to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "rect">The rect.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddRectangleShape(IChartRenderTarget scene, string id, ChartRect rect, double radius, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddRectangleShape(scene, id, rect, radius, style);
    }

    /// <summary>
    /// Adds the supplied circle shape to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddCircleShape(IChartRenderTarget scene, string id, double centerX, double centerY, double radius, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddCircleShape(scene, id, centerX, centerY, radius, style);
    }

    /// <summary>
    /// Adds the supplied ellipse shape to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radiusX">The radius x.</param>
    /// <param name = "radiusY">The radius y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddEllipseShape(IChartRenderTarget scene, string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddEllipseShape(scene, id, centerX, centerY, radiusX, radiusY, style);
    }

    /// <summary>
    /// Adds the supplied polygon shape to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPolygonShape(IChartRenderTarget scene, string id, ChartPoint[] points, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddPolygonShape(scene, id, points, style);
    }

    /// <summary>
    /// Adds the supplied path to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "path">The path containing move, line, curve, and close commands.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPath(IChartRenderTarget scene, string id, ChartPath path, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddPath(scene, id, path, style);
    }

    /// <summary>
    /// Adds the supplied watermark to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddWatermark(IChartRenderTarget scene, string id, string text, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddWatermark(scene, id, text, style);
    }

    /// <summary>
    /// Adds the supplied caption to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "margin">The margin.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddCaption(IChartRenderTarget scene, string id, string text, double margin, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddCaption(scene, id, text, margin, style);
    }

    /// <summary>
    /// Adds the supplied credits to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "margin">The margin.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddCredits(IChartRenderTarget scene, string id, string text, double margin, ChartRenderStyle style)
    {
        return ChartAnnotationChartsOperations.AddCredits(scene, id, text, margin, style);
    }
}
