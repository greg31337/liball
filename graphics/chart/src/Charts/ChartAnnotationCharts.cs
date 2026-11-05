namespace chart;

/// <summary>
/// Creates annotation chart series, layouts, or retained drawing commands.
/// </summary>
/// <remarks>Builder methods validate array lengths and numeric inputs before producing render-ready objects.</remarks>
internal static class ChartAnnotationChartsOperations
{
    /// <summary>Maps an annotation point from its configured coordinate unit into scene pixels.</summary>
    /// <param name="scene">The retained scene that supplies paper dimensions.</param>
    /// <param name="point">The point to map.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <returns>The mapped scene point.</returns>
    public static ChartPoint MapCoordinatePoint(IChartRenderTarget scene, ChartPoint point, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain)
    {
        ValidateScene(scene);
        if (point == null)
        {
            throw new System.ArgumentNullException("point");
        }

        if (configuration == null)
        {
            throw new System.ArgumentNullException("configuration");
        }

        ChartRect bounds = plotArea;
        if (configuration.GetCoordinateUnit() == ChartCoordinateUnit.Paper)
        {
            bounds = new ChartRect(0.0, 0.0, scene.GetWidth(), scene.GetHeight());
        }

        return configuration.MapPoint(point, bounds, xDomain, yDomain);
    }

    /// <summary>Maps annotation points from their configured coordinate unit into scene pixels.</summary>
    /// <param name="scene">The retained scene that supplies paper dimensions.</param>
    /// <param name="points">The points to map.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <returns>The mapped scene points.</returns>
    public static ChartPoint[] MapCoordinatePoints(IChartRenderTarget scene, ChartPoint[] points, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain)
    {
        ChartRenderCommand.ValidatePoints(points, 1);
        ChartPoint[] mapped = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            mapped[index] = MapCoordinatePoint(scene, points[index], configuration, plotArea, xDomain, yDomain);
            index = index + 1;
        }

        return mapped;
    }

    /// <summary>Adds text whose anchor is expressed in the configured coordinate unit.</summary>
    /// <param name="scene">The retained scene that receives the text.</param>
    /// <param name="id">A stable retained identifier.</param>
    /// <param name="text">The annotation text.</param>
    /// <param name="point">The text anchor.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <param name="style">The text render style.</param>
    /// <returns>The retained text command.</returns>
    public static ChartRenderCommand AddCoordinateText(IChartRenderTarget scene, string id, string text, ChartPoint point, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, ChartRenderStyle style)
    {
        ChartPoint mapped = MapCoordinatePoint(scene, point, configuration, plotArea, xDomain, yDomain);
        return AddText(scene, id, text, mapped.X, mapped.Y, style);
    }

    /// <summary>Adds an arrow whose endpoints are expressed in the configured coordinate unit.</summary>
    /// <param name="scene">The retained scene that receives the arrow.</param>
    /// <param name="id">A stable retained identifier prefix.</param>
    /// <param name="start">The arrow start.</param>
    /// <param name="end">The arrow end.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <param name="headLength">The positive arrow-head length in pixels.</param>
    /// <param name="headWidth">The positive arrow-head width in pixels.</param>
    /// <param name="style">The arrow render style.</param>
    public static void AddCoordinateArrow(IChartRenderTarget scene, string id, ChartPoint start, ChartPoint end, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, double headLength, double headWidth, ChartRenderStyle style)
    {
        ChartPoint mappedStart = MapCoordinatePoint(scene, start, configuration, plotArea, xDomain, yDomain);
        ChartPoint mappedEnd = MapCoordinatePoint(scene, end, configuration, plotArea, xDomain, yDomain);
        AddArrow(scene, id, mappedStart.X, mappedStart.Y, mappedEnd.X, mappedEnd.Y, headLength, headWidth, style);
    }

    /// <summary>Adds a reference rectangle expressed in the configured coordinate unit.</summary>
    /// <param name="scene">The retained scene that receives the shape.</param>
    /// <param name="id">A stable retained identifier.</param>
    /// <param name="rect">The source rectangle.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <param name="radius">The nonnegative corner radius in pixels.</param>
    /// <param name="style">The shape render style.</param>
    /// <returns>The retained rectangle command.</returns>
    public static ChartRenderCommand AddReferenceRectangle(IChartRenderTarget scene, string id, ChartRect rect, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, double radius, ChartRenderStyle style)
    {
        ChartAxis.ValidatePlotArea(rect);
        ChartPoint first = MapCoordinatePoint(scene, new ChartPoint(rect.X, rect.Y), configuration, plotArea, xDomain, yDomain);
        ChartPoint second = MapCoordinatePoint(scene, new ChartPoint(rect.X + rect.Width, rect.Y + rect.Height), configuration, plotArea, xDomain, yDomain);
        ChartRect mapped = RectFromCorners(first, second);
        return AddRectangleShape(scene, id, mapped, radius, style);
    }

    /// <summary>Adds a reference ellipse expressed in the configured coordinate unit.</summary>
    /// <param name="scene">The retained scene that receives the shape.</param>
    /// <param name="id">A stable retained identifier.</param>
    /// <param name="center">The source ellipse center.</param>
    /// <param name="radiusX">The positive horizontal radius in the source coordinate unit.</param>
    /// <param name="radiusY">The positive vertical radius in the source coordinate unit.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <param name="style">The shape render style.</param>
    /// <returns>The retained ellipse command.</returns>
    public static ChartRenderCommand AddReferenceEllipse(IChartRenderTarget scene, string id, ChartPoint center, double radiusX, double radiusY, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, ChartRenderStyle style)
    {
        ChartRenderStyle.ValidatePositive(radiusX, "radiusX");
        ChartRenderStyle.ValidatePositive(radiusY, "radiusY");
        ChartPoint mappedCenter = MapCoordinatePoint(scene, center, configuration, plotArea, xDomain, yDomain);
        ChartPoint mappedHorizontal = MapCoordinatePoint(scene, new ChartPoint(center.X + radiusX, center.Y), configuration, plotArea, xDomain, yDomain);
        ChartPoint mappedVertical = MapCoordinatePoint(scene, new ChartPoint(center.X, center.Y + radiusY), configuration, plotArea, xDomain, yDomain);
        double mappedRadiusX = System.Math.Abs(mappedHorizontal.X - mappedCenter.X);
        double mappedRadiusY = System.Math.Abs(mappedVertical.Y - mappedCenter.Y);
        return AddEllipseShape(scene, id, mappedCenter.X, mappedCenter.Y, mappedRadiusX, mappedRadiusY, style);
    }

    /// <summary>Adds a reference polygon expressed in the configured coordinate unit.</summary>
    /// <param name="scene">The retained scene that receives the shape.</param>
    /// <param name="id">A stable retained identifier.</param>
    /// <param name="points">The source polygon points.</param>
    /// <param name="configuration">The coordinate and scale configuration.</param>
    /// <param name="plotArea">The rectangle occupied by the plot.</param>
    /// <param name="xDomain">The horizontal data domain.</param>
    /// <param name="yDomain">The vertical data domain.</param>
    /// <param name="style">The shape render style.</param>
    /// <returns>The retained polygon command.</returns>
    public static ChartRenderCommand AddReferencePolygon(IChartRenderTarget scene, string id, ChartPoint[] points, ChartConfiguration configuration, ChartRect plotArea, ChartRange xDomain, ChartRange yDomain, ChartRenderStyle style)
    {
        ChartRenderCommand.ValidatePoints(points, 3);
        ChartPoint[] mapped = MapCoordinatePoints(scene, points, configuration, plotArea, xDomain, yDomain);
        return AddPolygonShape(scene, id, mapped, style);
    }

    /// <summary>Adds a raw bitmap covering the complete scene.</summary>
    /// <remarks>Add this command before foreground commands so the image remains in the background.</remarks>
    /// <param name="scene">The retained scene that receives the image.</param>
    /// <param name="id">A stable retained identifier.</param>
    /// <param name="bitmap">The source raw bitmap.</param>
    /// <param name="style">The image opacity and blend style.</param>
    /// <returns>The retained image command.</returns>
    public static ChartRenderCommand AddBackgroundImage(IChartRenderTarget scene, string id, RawBitmap bitmap, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddImage(id, bitmap, 0.0, 0.0, scene.GetWidth(), scene.GetHeight(), style);
    }

    /// <summary>
    /// Adds the supplied text to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="text">The text to measure, render, or encode.</param>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddText(IChartRenderTarget scene, string id, string text, double x, double y, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ChartRenderCommand.ValidateFinitePair(x, y, "text");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddText(id, text, x, y, style);
    }

    /// <summary>
    /// Adds the supplied arrow to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="x1">The x1.</param>
    /// <param name="y1">The y1.</param>
    /// <param name="x2">The x2.</param>
    /// <param name="y2">The y2.</param>
    /// <param name="headLength">The head length.</param>
    /// <param name="headWidth">The head width.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    public static void AddArrow(IChartRenderTarget scene, string id, double x1, double y1, double x2, double y2, double headLength, double headWidth, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartPoint[] head = CreateArrowHead(x1, y1, x2, y2, headLength, headWidth);
        scene.AddLine(id + "-line", x1, y1, x2, y2, style);
        scene.AddPolygon(id + "-head", head, style);
    }

    internal static ChartPoint[] CreateArrowHead(double x1, double y1, double x2, double y2, double headLength, double headWidth)
    {
        ChartRenderCommand.ValidateFinitePair(x1, y1, "arrow");
        ChartRenderCommand.ValidateFinitePair(x2, y2, "arrow");
        ChartRenderStyle.ValidatePositive(headLength, "headLength");
        ChartRenderStyle.ValidatePositive(headWidth, "headWidth");

        double dx = x2 - x1;
        double dy = y2 - y1;
        double length = System.Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.0)
        {
            throw new System.ArgumentException("Arrow length cannot be zero.");
        }

        double unitX = dx / length;
        double unitY = dy / length;
        double perpendicularX = 0.0 - unitY;
        double perpendicularY = unitX;
        double halfWidth = headWidth * 0.5;
        double baseX = x2 - unitX * headLength;
        double baseY = y2 - unitY * headLength;

        ChartPoint[] points = new ChartPoint[3];
        points[0] = new ChartPoint(x2, y2);
        points[1] = new ChartPoint(baseX + perpendicularX * halfWidth, baseY + perpendicularY * halfWidth);
        points[2] = new ChartPoint(baseX - perpendicularX * halfWidth, baseY - perpendicularY * halfWidth);
        return points;
    }

    /// <summary>
    /// Adds the supplied callout to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="text">The text to measure, render, or encode.</param>
    /// <param name="box">The box.</param>
    /// <param name="anchorX">The anchor x.</param>
    /// <param name="anchorY">The anchor y.</param>
    /// <param name="radius">The radius in chart units, which must be positive.</param>
    /// <param name="boxStyle">The rendering style used for box.</param>
    /// <param name="leaderStyle">The rendering style used for leader.</param>
    /// <param name="textStyle">The rendering style used for generated text.</param>
    public static void AddCallout(IChartRenderTarget scene, string id, string text, ChartRect box, double anchorX, double anchorY, double radius, ChartRenderStyle boxStyle, ChartRenderStyle leaderStyle, ChartRenderStyle textStyle)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ChartAxis.ValidatePlotArea(box);
        ChartRenderCommand.ValidateFinitePair(anchorX, anchorY, "anchor");
        ChartStyle.ValidateNonNegative(radius, "radius");
        ChartLineAreaChartsOperations.ValidateStyle(boxStyle);
        ChartLineAreaChartsOperations.ValidateStyle(leaderStyle);
        ChartLineAreaChartsOperations.ValidateStyle(textStyle);

        ChartPoint start = CalloutLeaderStart(box, anchorX, anchorY);
        scene.AddRect(id + "-box", box.X, box.Y, box.Width, box.Height, radius, radius, boxStyle);
        scene.AddLine(id + "-leader", start.X, start.Y, anchorX, anchorY, leaderStyle);
        scene.AddText(id + "-text", text, box.X + 4.0, box.Y + box.Height * 0.5, textStyle);
    }

    internal static ChartPoint CalloutLeaderStart(ChartRect box, double anchorX, double anchorY)
    {
        ChartAxis.ValidatePlotArea(box);
        ChartRenderCommand.ValidateFinitePair(anchorX, anchorY, "anchor");

        double centerX = box.X + box.Width * 0.5;
        double centerY = box.Y + box.Height * 0.5;
        double dx = anchorX - centerX;
        double dy = anchorY - centerY;
        double horizontal = System.Math.Abs(dx) * box.Height;
        double vertical = System.Math.Abs(dy) * box.Width;

        if (horizontal > vertical)
        {
            if (dx < 0.0)
            {
                return new ChartPoint(box.X, centerY);
            }

            return new ChartPoint(box.X + box.Width, centerY);
        }

        if (dy < 0.0)
        {
            return new ChartPoint(centerX, box.Y);
        }

        return new ChartPoint(centerX, box.Y + box.Height);
    }

    /// <summary>
    /// Adds the supplied rectangle shape to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="rect">The rect.</param>
    /// <param name="radius">The radius in chart units, which must be positive.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddRectangleShape(IChartRenderTarget scene, string id, ChartRect rect, double radius, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartAxis.ValidatePlotArea(rect);
        ChartStyle.ValidateNonNegative(radius, "radius");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddRect(id, rect.X, rect.Y, rect.Width, rect.Height, radius, radius, style);
    }

    /// <summary>
    /// Adds the supplied circle shape to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="centerX">The center x.</param>
    /// <param name="centerY">The center y.</param>
    /// <param name="radius">The radius in chart units, which must be positive.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddCircleShape(IChartRenderTarget scene, string id, double centerX, double centerY, double radius, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartRenderCommand.ValidateFinitePair(centerX, centerY, "circle");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddCircle(id, centerX, centerY, radius, style);
    }

    /// <summary>
    /// Adds the supplied ellipse shape to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="centerX">The center x.</param>
    /// <param name="centerY">The center y.</param>
    /// <param name="radiusX">The radius x.</param>
    /// <param name="radiusY">The radius y.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddEllipseShape(IChartRenderTarget scene, string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartRenderCommand.ValidateFinitePair(centerX, centerY, "ellipse");
        ChartRenderStyle.ValidatePositive(radiusX, "radiusX");
        ChartRenderStyle.ValidatePositive(radiusY, "radiusY");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddEllipse(id, centerX, centerY, radiusX, radiusY, style);
    }

    /// <summary>
    /// Adds the supplied polygon shape to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="points">The points.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddPolygonShape(IChartRenderTarget scene, string id, ChartPoint[] points, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartRenderCommand.ValidatePoints(points, 3);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddPolygon(id, points, style);
    }

    /// <summary>
    /// Adds the supplied path to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="path">The path containing move, line, curve, and close commands.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddPath(IChartRenderTarget scene, string id, ChartPath path, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ValidatePath(path);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddPath(id, path, style);
    }

    /// <summary>
    /// Adds the supplied watermark to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="text">The text to measure, render, or encode.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddWatermark(IChartRenderTarget scene, string id, string text, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddText(id, text, scene.GetWidth() * 0.5, scene.GetHeight() * 0.5, style);
    }

    /// <summary>
    /// Adds the supplied caption to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="text">The text to measure, render, or encode.</param>
    /// <param name="margin">The margin.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddCaption(IChartRenderTarget scene, string id, string text, double margin, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ValidateMargin(margin);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddText(id, text, margin, scene.GetHeight() - margin, style);
    }

    /// <summary>
    /// Adds the supplied credits to this object.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="text">The text to measure, render, or encode.</param>
    /// <param name="margin">The margin.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public static ChartRenderCommand AddCredits(IChartRenderTarget scene, string id, string text, double margin, ChartRenderStyle style)
    {
        ValidateScene(scene);
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ValidateMargin(margin);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return scene.AddText(id, text, scene.GetWidth() - margin, scene.GetHeight() - margin, style);
    }

    internal static void ValidateScene(IChartRenderTarget scene)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }
    }

    internal static void ValidatePath(ChartPath path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        if (path.Commands.Count == 0)
        {
            throw new System.ArgumentException("Annotation path cannot be empty.");
        }
    }

    internal static void ValidateMargin(double margin)
    {
        ChartLinearScale.ValidateFinite(margin, "margin");
        if (margin < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("margin");
        }
    }

    internal static ChartRect RectFromCorners(ChartPoint first, ChartPoint second)
    {
        double x = System.Math.Min(first.X, second.X);
        double y = System.Math.Min(first.Y, second.Y);
        return new ChartRect(x, y, System.Math.Abs(second.X - first.X), System.Math.Abs(second.Y - first.Y));
    }
}
