namespace chart;
/// <summary>
/// Represents a render scene used by the chart model or rendering pipeline.
/// </summary>
/// <remarks>A scene retains drawing commands in insertion order and can replay them to bitmap or SVG output.</remarks>
public class ChartRenderScene : IChartRenderTarget, IChartRenderScene
{
    internal int Width;
    internal int Height;
    internal ChartRenderCommand[] Commands;
    internal int CommandCount;
    /// <summary>
    /// Initializes a render scene from the supplied width and height.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartRenderScene(int width, int height)
    {
        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        Width = width;
        Height = height;
        Commands = new ChartRenderCommand[8];
        CommandCount = 0;
    }

    /// <summary>
    /// Returns the width available to the rendering target.
    /// </summary>
    /// <returns>The width in pixels or chart units.</returns>
    public int GetWidth()
    {
        return Width;
    }

    /// <summary>
    /// Returns the height available to the rendering target.
    /// </summary>
    /// <returns>The height in pixels or chart units.</returns>
    public int GetHeight()
    {
        return Height;
    }

    /// <summary>
    /// Adds the supplied command to this object.
    /// </summary>
    /// <param name = "command">The command.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartRenderCommand AddCommand(ChartRenderCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        EnsureCapacity(CommandCount + 1);
        Commands[CommandCount] = command.Copy();
        CommandCount = CommandCount + 1;
        return Commands[CommandCount - 1];
    }

    /// <summary>
    /// Adds the supplied clear to this object.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddClear(ChartColor color)
    {
        return AddCommand(ChartRenderCommand.CreateClear(color));
    }

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
    public ChartRenderCommand AddLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateLine(id, x1, y1, x2, y2, style));
    }

    /// <summary>
    /// Adds the supplied polyline to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPolyline(string id, ChartPoint[] points, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreatePolyline(id, points, style));
    }

    /// <summary>
    /// Adds the supplied polygon to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "points">The points.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPolygon(string id, ChartPoint[] points, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreatePolygon(id, points, style));
    }

    /// <summary>
    /// Adds the supplied rect to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "radiusX">The radius x.</param>
    /// <param name = "radiusY">The radius y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddRect(string id, double x, double y, double width, double height, double radiusX, double radiusY, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateRect(id, x, y, width, height, radiusX, radiusY, style));
    }

    /// <summary>
    /// Adds the supplied circle to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddCircle(string id, double centerX, double centerY, double radius, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateCircle(id, centerX, centerY, radius, style));
    }

    /// <summary>
    /// Adds the supplied ellipse to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radiusX">The radius x.</param>
    /// <param name = "radiusY">The radius y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddEllipse(string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateEllipse(id, centerX, centerY, radiusX, radiusY, style));
    }

    /// <summary>
    /// Adds the supplied path to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "path">The path containing move, line, curve, and close commands.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPath(string id, ChartPath path, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreatePath(id, path, style));
    }

    /// <summary>
    /// Adds the supplied text to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddText(string id, string text, double x, double y, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateText(id, text, x, y, style));
    }

    /// <summary>Adds a complete public text block with wrapping, alignment, rotation, clipping, direction, and rich spans.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "textBlock">The configured public text block.</param>
    /// <param name = "x">The horizontal anchor coordinate.</param>
    /// <param name = "y">The vertical anchor coordinate.</param>
    /// <returns>The retained text command.</returns>
    public ChartRenderCommand AddTextBlock(string id, ChartTextBlock textBlock, double x, double y)
    {
        return AddCommand(ChartRenderCommand.CreateTextBlock(id, textBlock, x, y));
    }

    /// <summary>
    /// Adds the supplied marker to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "size">The size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddMarker(string id, ChartMarkerShape shape, double centerX, double centerY, double size, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateMarker(id, shape, centerX, centerY, size, style));
    }

    /// <summary>
    /// Adds the supplied custom marker to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "markerPath">The marker path.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "size">The size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddCustomMarker(string id, ChartPath markerPath, double centerX, double centerY, double size, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateCustomMarker(id, markerPath, centerX, centerY, size, style));
    }

    /// <summary>Adds an open circular arc expressed in radians.</summary>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "centerX">The center x coordinate.</param>
    /// <param name = "centerY">The center y coordinate.</param>
    /// <param name = "radius">The positive radius.</param>
    /// <param name = "startAngle">The start angle in radians.</param>
    /// <param name = "endAngle">The end angle in radians.</param>
    /// <param name = "style">The render style.</param>
    /// <returns>The retained arc command.</returns>
    public ChartRenderCommand AddArc(string id, double centerX, double centerY, double radius, double startAngle, double endAngle, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateArc(id, centerX, centerY, radius, startAngle, endAngle, style));
    }

    /// <summary>Adds a closed annular wedge expressed in radians.</summary>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "centerX">The center x coordinate.</param>
    /// <param name = "centerY">The center y coordinate.</param>
    /// <param name = "innerRadius">The nonnegative inner radius.</param>
    /// <param name = "outerRadius">The positive outer radius.</param>
    /// <param name = "startAngle">The start angle in radians.</param>
    /// <param name = "endAngle">The end angle in radians.</param>
    /// <param name = "style">The render style.</param>
    /// <returns>The retained wedge command.</returns>
    public ChartRenderCommand AddWedge(string id, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateWedge(id, centerX, centerY, innerRadius, outerRadius, startAngle, endAngle, style));
    }

    /// <summary>Places a raw bitmap in the retained scene.</summary>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "bitmap">The source raw bitmap.</param>
    /// <param name = "x">The destination x coordinate.</param>
    /// <param name = "y">The destination y coordinate.</param>
    /// <param name = "width">The positive destination width.</param>
    /// <param name = "height">The positive destination height.</param>
    /// <param name = "style">The opacity and blend style.</param>
    /// <returns>The retained image command.</returns>
    public ChartRenderCommand AddImage(string id, RawBitmap bitmap, double x, double y, double width, double height, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateImage(id, bitmap, x, y, width, height, style));
    }

    /// <summary>Begins a semantic retained group.</summary>
    /// <param name = "id">A stable retained identifier.</param>
    /// <returns>The retained group command.</returns>
    public ChartRenderCommand BeginGroup(string id)
    {
        return AddCommand(ChartRenderCommand.CreateBeginGroup(id));
    }

    /// <summary>Ends the current semantic retained group.</summary>
    /// <returns>The retained group command.</returns>
    public ChartRenderCommand EndGroup()
    {
        return AddCommand(ChartRenderCommand.CreateEndGroup());
    }

    /// <summary>Pushes an affine transform for following retained commands.</summary>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "transform">The affine transform.</param>
    /// <returns>The retained transform command.</returns>
    public ChartRenderCommand PushTransform(string id, ChartMatrix transform)
    {
        return AddCommand(ChartRenderCommand.CreatePushTransform(id, transform));
    }

    /// <summary>Restores the preceding affine transform.</summary>
    /// <returns>The retained transform command.</returns>
    public ChartRenderCommand PopTransform()
    {
        return AddCommand(ChartRenderCommand.CreatePopTransform());
    }

    /// <summary>Pushes a rectangular clip for following retained commands.</summary>
    /// <param name = "id">A stable retained identifier.</param>
    /// <param name = "bounds">The clip bounds.</param>
    /// <returns>The retained clip command.</returns>
    public ChartRenderCommand PushClip(string id, ChartRect bounds)
    {
        return AddCommand(ChartRenderCommand.CreatePushClip(id, bounds));
    }

    /// <summary>Restores the preceding clip region.</summary>
    /// <returns>The retained clip command.</returns>
    public ChartRenderCommand PopClip()
    {
        return AddCommand(ChartRenderCommand.CreatePopClip());
    }

    /// <summary>
    /// Returns the requested command.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartRenderCommand CommandAt(int index)
    {
        if (index < 0 || index >= CommandCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Commands[index];
    }

    /// <summary>
    /// Creates an independent copy of the commands.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartRenderCommand[] CopyCommands()
    {
        ChartRenderCommand[] copy = new ChartRenderCommand[CommandCount];
        int index = 0;
        while (index < CommandCount)
        {
            copy[index] = Commands[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartRenderScene Copy()
    {
        ChartRenderScene scene = new ChartRenderScene(Width, Height);
        int index = 0;
        while (index < CommandCount)
        {
            scene.AddCommand(Commands[index]);
            index = index + 1;
        }

        return scene;
    }

    /// <summary>
    /// Replays the retained scene into a new top-down RGBA bitmap.
    /// </summary>
    /// <returns>The converted raw bitmap.</returns>
    public RawBitmap ToRawBitmap()
    {
        RawBitmap bitmap = new RawBitmap(Width, Height, ChartPixelFormat.Rgba32);
        RenderRaster(bitmap);
        return bitmap;
    }

    /// <summary>
    /// Replays the retained scene into a complete SVG document string.
    /// </summary>
    /// <returns>The converted string.</returns>
    public string ToSvgString()
    {
        SvgWriter writer = new SvgWriter(Width, Height);
        RenderSvg(writer);
        return writer.ToSvgString();
    }

    /// <summary>
    /// Creates a deterministic text trace of the retained drawing commands.
    /// </summary>
    /// <returns>The converted string.</returns>
    public string ToTraceString()
    {
        return ChartDiagnosticsOperations.RenderTrace(this);
    }

    /// <summary>
    /// Renders raster using the supplied data and configuration.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderRaster(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        if (bitmap.Width != Width || bitmap.Height != Height)
        {
            throw new System.ArgumentException("Bitmap size must match render scene size.");
        }

        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        RenderToRasterCanvas(canvas);
    }

    internal void RenderToRasterCanvas(ChartRasterCanvas canvas)
    {
        if (canvas == null)
        {
            throw new System.ArgumentNullException("canvas");
        }

        ChartMatrix currentTransform = ChartMatrix.Identity();
        ChartMatrix[] transformStack = new ChartMatrix[CommandCount + 1];
        int transformDepth = 0;
        int[] clipXStack = new int[CommandCount + 1];
        int[] clipYStack = new int[CommandCount + 1];
        int[] clipWidthStack = new int[CommandCount + 1];
        int[] clipHeightStack = new int[CommandCount + 1];
        int clipDepth = 0;
        int originalClipX = canvas.ClipX;
        int originalClipY = canvas.ClipY;
        int originalClipWidth = canvas.ClipWidth;
        int originalClipHeight = canvas.ClipHeight;
        int index = 0;
        while (index < CommandCount)
        {
            ChartRenderCommand command = Commands[index];
            if (command.Kind == ChartRenderCommandKind.PushTransform)
            {
                transformStack[transformDepth] = currentTransform;
                transformDepth = transformDepth + 1;
                currentTransform = command.Transform.Multiply(currentTransform);
            }
            else if (command.Kind == ChartRenderCommandKind.PopTransform)
            {
                if (transformDepth <= 0)
                {
                    throw new System.InvalidOperationException("No retained transform exists to restore.");
                }

                transformDepth = transformDepth - 1;
                currentTransform = transformStack[transformDepth];
            }
            else if (command.Kind == ChartRenderCommandKind.PushClip)
            {
                clipXStack[clipDepth] = canvas.ClipX;
                clipYStack[clipDepth] = canvas.ClipY;
                clipWidthStack[clipDepth] = canvas.ClipWidth;
                clipHeightStack[clipDepth] = canvas.ClipHeight;
                clipDepth = clipDepth + 1;
                ChartRect transformedClip = TransformBounds(command.ClipBounds, currentTransform);
                ChartRect existingClip = new ChartRect(canvas.ClipX, canvas.ClipY, canvas.ClipWidth, canvas.ClipHeight);
                canvas.SetClipRect(existingClip.Intersection(transformedClip));
            }
            else if (command.Kind == ChartRenderCommandKind.PopClip)
            {
                if (clipDepth <= 0)
                {
                    throw new System.InvalidOperationException("No retained clip exists to restore.");
                }

                clipDepth = clipDepth - 1;
                canvas.SetClip(clipXStack[clipDepth], clipYStack[clipDepth], clipWidthStack[clipDepth], clipHeightStack[clipDepth]);
            }
            else if (command.Kind != ChartRenderCommandKind.BeginGroup && command.Kind != ChartRenderCommandKind.EndGroup)
            {
                RenderCommandRaster(canvas, TransformCommand(command, currentTransform));
            }

            index = index + 1;
        }

        if (transformDepth != 0 || clipDepth != 0)
        {
            throw new System.InvalidOperationException("Retained transform and clip commands must be balanced.");
        }

        canvas.SetClip(originalClipX, originalClipY, originalClipWidth, originalClipHeight);
    }

    internal void RenderSvg(SvgWriter writer)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        ValidateSvgGroups();
        int index = 0;
        while (index < CommandCount)
        {
            RenderCommandSvg(writer, Commands[index]);
            index = index + 1;
        }
    }

    internal void ValidateSvgGroups()
    {
        ChartRenderCommandKind[] groups = new ChartRenderCommandKind[CommandCount];
        int depth = 0;
        int index = 0;
        while (index < CommandCount)
        {
            ChartRenderCommandKind kind = Commands[index].Kind;
            if (kind == ChartRenderCommandKind.BeginGroup || kind == ChartRenderCommandKind.PushTransform || kind == ChartRenderCommandKind.PushClip)
            {
                groups[depth] = kind;
                depth = depth + 1;
            }
            else if (kind == ChartRenderCommandKind.EndGroup)
            {
                if (depth <= 0 || groups[depth - 1] != ChartRenderCommandKind.BeginGroup)
                {
                    throw new System.InvalidOperationException("Retained SVG groups must be balanced and properly nested.");
                }

                depth = depth - 1;
            }
            else if (kind == ChartRenderCommandKind.PopTransform)
            {
                if (depth <= 0 || groups[depth - 1] != ChartRenderCommandKind.PushTransform)
                {
                    throw new System.InvalidOperationException("Retained SVG transforms must be balanced and properly nested.");
                }

                depth = depth - 1;
            }
            else if (kind == ChartRenderCommandKind.PopClip)
            {
                if (depth <= 0 || groups[depth - 1] != ChartRenderCommandKind.PushClip)
                {
                    throw new System.InvalidOperationException("Retained SVG clips must be balanced and properly nested.");
                }

                depth = depth - 1;
            }

            index = index + 1;
        }

        if (depth != 0)
        {
            throw new System.InvalidOperationException("Retained SVG groups, transforms, and clips must be balanced.");
        }
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Commands.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Commands.Length, capacity);
        ChartRenderCommand[] commands = new ChartRenderCommand[newCapacity];
        System.Array.Copy(Commands, commands, CommandCount);
        Commands = commands;
    }

    internal static ChartRenderCommand TransformCommand(ChartRenderCommand source, ChartMatrix transform)
    {
        if (source.Kind == ChartRenderCommandKind.Clear)
        {
            return source;
        }

        if (source.Kind == ChartRenderCommandKind.Arc || source.Kind == ChartRenderCommandKind.Wedge)
        {
            return TransformCommand(CreateArcPath(source), transform);
        }

        ChartRenderCommand command = source.Copy();
        if (command.Style.HasFillGradient)
        {
            command.Style.FillGradientStart = transform.TransformPoint(command.Style.FillGradientStart);
            command.Style.FillGradientEnd = transform.TransformPoint(command.Style.FillGradientEnd);
        }
        if (command.Style.HasStrokeGradient)
        {
            command.Style.StrokeGradientStart = transform.TransformPoint(command.Style.StrokeGradientStart);
            command.Style.StrokeGradientEnd = transform.TransformPoint(command.Style.StrokeGradientEnd);
        }
        if (source.Kind == ChartRenderCommandKind.Line)
        {
            ChartPoint start = transform.TransformPoint(new ChartPoint(source.X1, source.Y1));
            ChartPoint end = transform.TransformPoint(new ChartPoint(source.X2, source.Y2));
            command.X1 = start.X;
            command.Y1 = start.Y;
            command.X2 = end.X;
            command.Y2 = end.Y;
        }
        else if (source.Kind == ChartRenderCommandKind.Polyline || source.Kind == ChartRenderCommandKind.Polygon)
        {
            command.Points = TransformPoints(source.Points, transform);
        }
        else if (source.Kind == ChartRenderCommandKind.Path)
        {
            command.Path = TransformPath(source.Path, transform);
        }
        else if (source.Kind == ChartRenderCommandKind.Image)
        {
            command.Transform = ChartRenderCommand.CopyMatrix(transform);
        }
        else if (source.Kind == ChartRenderCommandKind.Rect)
        {
            ChartRect bounds = TransformBounds(new ChartRect(source.X1, source.Y1, source.Width, source.Height), transform);
            command.X1 = bounds.X;
            command.Y1 = bounds.Y;
            command.Width = bounds.Width;
            command.Height = bounds.Height;
            double scaleX = System.Math.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12);
            double scaleY = System.Math.Sqrt(transform.M21 * transform.M21 + transform.M22 * transform.M22);
            command.RadiusX = source.RadiusX * scaleX;
            command.RadiusY = source.RadiusY * scaleY;
        }
        else if (source.Kind == ChartRenderCommandKind.Circle || source.Kind == ChartRenderCommandKind.Ellipse)
        {
            ChartPoint center = transform.TransformPoint(new ChartPoint(source.X1, source.Y1));
            double scaleX = System.Math.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12);
            double scaleY = System.Math.Sqrt(transform.M21 * transform.M21 + transform.M22 * transform.M22);
            command.X1 = center.X;
            command.Y1 = center.Y;
            command.RadiusX = source.RadiusX * scaleX;
            command.RadiusY = source.RadiusY * scaleY;
            if (source.Kind == ChartRenderCommandKind.Circle && command.RadiusX != command.RadiusY)
            {
                command.Kind = ChartRenderCommandKind.Ellipse;
            }
        }
        else if (source.Kind == ChartRenderCommandKind.Text || source.Kind == ChartRenderCommandKind.Marker)
        {
            ChartPoint anchor = transform.TransformPoint(new ChartPoint(source.X1, source.Y1));
            command.X1 = anchor.X;
            command.Y1 = anchor.Y;
            if (source.Kind == ChartRenderCommandKind.Marker)
            {
                double scaleX = System.Math.Sqrt(transform.M11 * transform.M11 + transform.M12 * transform.M12);
                double scaleY = System.Math.Sqrt(transform.M21 * transform.M21 + transform.M22 * transform.M22);
                command.MarkerSize = source.MarkerSize * (scaleX + scaleY) * 0.5;
            }
        }

        return command;
    }

    internal static ChartPoint[] TransformPoints(ChartPoint[] points, ChartMatrix transform)
    {
        ChartPoint[] result = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            result[index] = transform.TransformPoint(points[index]);
            index = index + 1;
        }

        return result;
    }

    internal static ChartPath TransformPath(ChartPath path, ChartMatrix transform)
    {
        ChartPath result = new ChartPath();
        int index = 0;
        while (index < path.Commands.Count)
        {
            ChartPathCommand command = path.Commands[index];
            if (command.Kind == ChartPathCommandKind.MoveTo)
            {
                ChartPoint point = transform.TransformPoint(new ChartPoint(command.X1, command.Y1));
                result.MoveTo(point.X, point.Y);
            }
            else if (command.Kind == ChartPathCommandKind.LineTo)
            {
                ChartPoint point = transform.TransformPoint(new ChartPoint(command.X1, command.Y1));
                result.LineTo(point.X, point.Y);
            }
            else if (command.Kind == ChartPathCommandKind.QuadraticBezierTo)
            {
                ChartPoint control = transform.TransformPoint(new ChartPoint(command.X1, command.Y1));
                ChartPoint end = transform.TransformPoint(new ChartPoint(command.X2, command.Y2));
                result.QuadraticBezierTo(control.X, control.Y, end.X, end.Y);
            }
            else if (command.Kind == ChartPathCommandKind.CubicBezierTo)
            {
                ChartPoint control1 = transform.TransformPoint(new ChartPoint(command.X1, command.Y1));
                ChartPoint control2 = transform.TransformPoint(new ChartPoint(command.X2, command.Y2));
                ChartPoint end = transform.TransformPoint(new ChartPoint(command.X3, command.Y3));
                result.CubicBezierTo(control1.X, control1.Y, control2.X, control2.Y, end.X, end.Y);
            }
            else if (command.Kind == ChartPathCommandKind.Close)
            {
                result.Close();
            }

            index = index + 1;
        }

        return result;
    }

    internal static ChartRect TransformBounds(ChartRect bounds, ChartMatrix transform)
    {
        ChartPoint first = transform.TransformPoint(new ChartPoint(bounds.X, bounds.Y));
        ChartPoint second = transform.TransformPoint(new ChartPoint(bounds.X + bounds.Width, bounds.Y));
        ChartPoint third = transform.TransformPoint(new ChartPoint(bounds.X, bounds.Y + bounds.Height));
        ChartPoint fourth = transform.TransformPoint(new ChartPoint(bounds.X + bounds.Width, bounds.Y + bounds.Height));
        double left = System.Math.Min(System.Math.Min(first.X, second.X), System.Math.Min(third.X, fourth.X));
        double right = System.Math.Max(System.Math.Max(first.X, second.X), System.Math.Max(third.X, fourth.X));
        double top = System.Math.Min(System.Math.Min(first.Y, second.Y), System.Math.Min(third.Y, fourth.Y));
        double bottom = System.Math.Max(System.Math.Max(first.Y, second.Y), System.Math.Max(third.Y, fourth.Y));
        return new ChartRect(left, top, right - left, bottom - top);
    }

    internal static ChartPath CreateRoundedRectanglePath(double x, double y, double width, double height, double radiusX, double radiusY)
    {
        double rx = System.Math.Min(System.Math.Abs(radiusX), width * 0.5);
        double ry = System.Math.Min(System.Math.Abs(radiusY), height * 0.5);
        if (rx == 0.0)
        {
            rx = System.Math.Min(ry, width * 0.5);
        }
        if (ry == 0.0)
        {
            ry = System.Math.Min(rx, height * 0.5);
        }

        ChartPath path = new ChartPath();
        path.MoveTo(x + rx, y);
        path.LineTo(x + width - rx, y);
        path.QuadraticBezierTo(x + width, y, x + width, y + ry);
        path.LineTo(x + width, y + height - ry);
        path.QuadraticBezierTo(x + width, y + height, x + width - rx, y + height);
        path.LineTo(x + rx, y + height);
        path.QuadraticBezierTo(x, y + height, x, y + height - ry);
        path.LineTo(x, y + ry);
        path.QuadraticBezierTo(x, y, x + rx, y);
        path.Close();
        return path;
    }

    internal static void RenderCommandRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (canvas == null)
        {
            throw new System.ArgumentNullException("canvas");
        }

        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        if (ShouldRenderWithMask(command))
        {
            RenderCommandWithMask(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Clear)
        {
            canvas.Clear(command.Style.RasterFillColor());
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Line)
        {
            RenderLineRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polyline)
        {
            RenderPolylineRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polygon)
        {
            RenderPolygonRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Rect)
        {
            RenderRectRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Circle)
        {
            RenderCircleRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Ellipse)
        {
            RenderEllipseRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Path)
        {
            RenderPathRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Text)
        {
            RenderTextRaster(canvas, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Arc || command.Kind == ChartRenderCommandKind.Wedge)
        {
            RenderPathRaster(canvas, CreateArcPath(command));
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Image)
        {
            canvas.DrawTransformedImage(command.Image, command.X1, command.Y1, command.Width, command.Height, command.Transform, command.Style.Opacity, command.Style.Quality, command.Style.BlendMode);
            return;
        }

        if (command.Kind != ChartRenderCommandKind.Marker)
        {
            return;
        }

        RenderMarkerRaster(canvas, command);
    }

    internal static bool ShouldRenderWithMask(ChartRenderCommand command)
    {
        if (command.Kind == ChartRenderCommandKind.Clear || command.Kind == ChartRenderCommandKind.Image)
        {
            return false;
        }

        if (command.Kind == ChartRenderCommandKind.Text && (command.HasTextBlock || command.Style.HasTextOutline || command.Style.HasTextShadow))
        {
            return false;
        }

        if (command.Kind == ChartRenderCommandKind.Text)
        {
            return command.Style.HasFillGradient || command.Style.HasStrokeGradient || command.Style.HasFillPattern || command.Style.HasStrokePattern;
        }

        if (command.Kind == ChartRenderCommandKind.BeginGroup || command.Kind == ChartRenderCommandKind.EndGroup || command.Kind == ChartRenderCommandKind.PushTransform || command.Kind == ChartRenderCommandKind.PopTransform || command.Kind == ChartRenderCommandKind.PushClip || command.Kind == ChartRenderCommandKind.PopClip)
        {
            return false;
        }

        return command.Style.Quality == ChartRenderQuality.Antialiased || command.Style.HasFillGradient || command.Style.HasStrokeGradient || command.Style.HasFillPattern || command.Style.HasStrokePattern;
    }

    internal static void RenderCommandWithMask(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        int factor = 1;
        if (command.Style.Quality == ChartRenderQuality.Antialiased)
        {
            factor = 4;
        }

        bool rendered = false;
        if (HasVisibleFill(command.Style))
        {
            RenderMaskComponent(canvas, command, true, factor);
            rendered = true;
        }

        if (HasVisibleStroke(command.Style))
        {
            RenderMaskComponent(canvas, command, false, factor);
            rendered = true;
        }

        if (!rendered && (command.Kind == ChartRenderCommandKind.Marker || command.Kind == ChartRenderCommandKind.Text))
        {
            RenderMaskComponent(canvas, command, true, factor);
        }
    }

    internal static void RenderMaskComponent(ChartRasterCanvas canvas, ChartRenderCommand command, bool fill, int factor)
    {
        ChartRect bounds = RasterCommandBounds(canvas, command);
        if (bounds.Width <= 0.0 || bounds.Height <= 0.0)
        {
            return;
        }

        int boundsX = (int)bounds.X;
        int boundsY = (int)bounds.Y;
        int boundsWidth = (int)bounds.Width;
        int boundsHeight = (int)bounds.Height;
        long widthValue = (long)boundsWidth * factor;
        long heightValue = (long)boundsHeight * factor;
        if (widthValue > int.MaxValue || heightValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("factor");
        }

        int maskWidth = (int)widthValue;
        int maskHeight = (int)heightValue;
        RawBitmap mask = new RawBitmap(maskWidth, maskHeight, ChartPixelFormat.Rgba32);
        ChartRasterCanvas maskCanvas = new ChartRasterCanvas(mask);
        ChartMatrix scale = new ChartMatrix(factor, 0.0, 0.0, factor, 0.0 - boundsX * factor, 0.0 - boundsY * factor);
        ChartRenderCommand scaled = TransformCommand(command, scale);
        scaled.Style = CreateMaskStyle(command.Style, fill, factor);
        RenderCommandRaster(maskCanvas, scaled);
        CompositeMask(canvas, mask, command.Style, fill, factor, boundsX, boundsY);
    }

    internal static ChartRect RasterCommandBounds(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        double left = canvas.ClipX;
        double top = canvas.ClipY;
        double right = canvas.ClipX + canvas.ClipWidth;
        double bottom = canvas.ClipY + canvas.ClipHeight;
        if (command.Kind == ChartRenderCommandKind.Line)
        {
            left = System.Math.Min(command.X1, command.X2);
            top = System.Math.Min(command.Y1, command.Y2);
            right = System.Math.Max(command.X1, command.X2);
            bottom = System.Math.Max(command.Y1, command.Y2);
        }
        else if (command.Kind == ChartRenderCommandKind.Polyline || command.Kind == ChartRenderCommandKind.Polygon)
        {
            left = ChartRasterCanvas.PolylineMinimumX(command.Points);
            top = ChartRasterCanvas.PolylineMinimumY(command.Points);
            right = ChartRasterCanvas.PolylineMaximumX(command.Points);
            bottom = ChartRasterCanvas.PolylineMaximumY(command.Points);
        }
        else if (command.Kind == ChartRenderCommandKind.Rect)
        {
            left = command.X1;
            top = command.Y1;
            right = command.X1 + command.Width;
            bottom = command.Y1 + command.Height;
        }
        else if (command.Kind == ChartRenderCommandKind.Circle)
        {
            left = command.X1 - command.RadiusX;
            top = command.Y1 - command.RadiusX;
            right = command.X1 + command.RadiusX;
            bottom = command.Y1 + command.RadiusX;
        }
        else if (command.Kind == ChartRenderCommandKind.Ellipse)
        {
            left = command.X1 - command.RadiusX;
            top = command.Y1 - command.RadiusY;
            right = command.X1 + command.RadiusX;
            bottom = command.Y1 + command.RadiusY;
        }
        else if (command.Kind == ChartRenderCommandKind.Arc || command.Kind == ChartRenderCommandKind.Wedge)
        {
            left = command.X1 - command.RadiusX;
            top = command.Y1 - command.RadiusX;
            right = command.X1 + command.RadiusX;
            bottom = command.Y1 + command.RadiusX;
        }
        else if (command.Kind == ChartRenderCommandKind.Marker)
        {
            double half = command.MarkerSize * 0.5;
            left = command.X1 - half;
            top = command.Y1 - half;
            right = command.X1 + half;
            bottom = command.Y1 + half;
        }
        else if (command.Kind == ChartRenderCommandKind.Text)
        {
            ChartBitmapFont font = ChartBitmapFont.FiveBySeven();
            left = command.X1;
            top = command.Y1;
            right = command.X1 + font.TextWidth(command.Text, command.Style.TextScale);
            bottom = command.Y1 + font.TextHeight(command.Text, command.Style.TextScale);
        }
        else if (command.Kind == ChartRenderCommandKind.Path)
        {
            ChartPathPolyline[] polylines = command.Path.Flatten(0.25);
            bool found = false;
            int index = 0;
            while (index < polylines.Length)
            {
                if (polylines[index].Points.Length > 0)
                {
                    double pathLeft = ChartRasterCanvas.PolylineMinimumX(polylines[index].Points);
                    double pathTop = ChartRasterCanvas.PolylineMinimumY(polylines[index].Points);
                    double pathRight = ChartRasterCanvas.PolylineMaximumX(polylines[index].Points);
                    double pathBottom = ChartRasterCanvas.PolylineMaximumY(polylines[index].Points);
                    if (!found)
                    {
                        left = pathLeft;
                        top = pathTop;
                        right = pathRight;
                        bottom = pathBottom;
                        found = true;
                    }
                    else
                    {
                        left = System.Math.Min(left, pathLeft);
                        top = System.Math.Min(top, pathTop);
                        right = System.Math.Max(right, pathRight);
                        bottom = System.Math.Max(bottom, pathBottom);
                    }
                }

                index = index + 1;
            }
            if (!found)
            {
                return new ChartRect(0.0, 0.0, 0.0, 0.0);
            }
        }

        double padding = 2.0;
        if (HasVisibleStroke(command.Style))
        {
            padding = padding + command.Style.StrokeWidth * System.Math.Max(1.0, command.Style.MiterLimit) * 0.5;
        }
        int pixelLeft = System.Math.Max(canvas.ClipX, (int)System.Math.Floor(left - padding));
        int pixelTop = System.Math.Max(canvas.ClipY, (int)System.Math.Floor(top - padding));
        int pixelRight = System.Math.Min(canvas.ClipX + canvas.ClipWidth, (int)System.Math.Ceiling(right + padding));
        int pixelBottom = System.Math.Min(canvas.ClipY + canvas.ClipHeight, (int)System.Math.Ceiling(bottom + padding));
        if (pixelRight <= pixelLeft || pixelBottom <= pixelTop)
        {
            return new ChartRect(pixelLeft, pixelTop, 0.0, 0.0);
        }

        return new ChartRect(pixelLeft, pixelTop, pixelRight - pixelLeft, pixelBottom - pixelTop);
    }

    internal static ChartRenderStyle CreateMaskStyle(ChartRenderStyle source, bool fill, int factor)
    {
        ChartRenderStyle style = source.Copy();
        style.HasFillGradient = false;
        style.HasStrokeGradient = false;
        style.HasFillPattern = false;
        style.HasStrokePattern = false;
        style.Opacity = 1.0;
        style.FillOpacity = 1.0;
        style.StrokeOpacity = 1.0;
        style.BlendMode = ChartBlendMode.Source;
        style.Quality = ChartRenderQuality.Aliased;
        style.HasTextOutline = false;
        style.HasTextShadow = false;
        style.TextScale = source.TextScale * factor;
        if (fill)
        {
            style.HasFill = true;
            style.FillNone = false;
            style.FillColor = ChartColor.White();
            style.HasStroke = false;
            style.StrokeNone = true;
        }
        else
        {
            style.HasFill = false;
            style.FillNone = true;
            style.HasStroke = true;
            style.StrokeNone = false;
            style.StrokeColor = ChartColor.White();
            style.StrokeWidth = source.StrokeWidth * factor;
            style.DashArray = ScaleLengths(source.DashArray, factor);
        }

        return style;
    }

    internal static double[] ScaleLengths(double[] values, int factor)
    {
        double[] result = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index] * factor;
            index = index + 1;
        }

        return result;
    }

    internal static void CompositeMask(ChartRasterCanvas canvas, RawBitmap mask, ChartRenderStyle style, bool fill, int factor, int boundsX, int boundsY)
    {
        int y = boundsY;
        while (y < boundsY + mask.Height / factor)
        {
            int x = boundsX;
            while (x < boundsX + mask.Width / factor)
            {
                double coverage = MaskCoverage(mask, x - boundsX, y - boundsY, factor);
                if (coverage > 0.0)
                {
                    ChartColor paint = RasterPaintColor(style, fill, x + 0.5, y + 0.5);
                    double opacity = style.Opacity * coverage;
                    if (fill)
                    {
                        opacity = opacity * style.FillOpacity;
                    }
                    else
                    {
                        opacity = opacity * style.StrokeOpacity;
                    }

                    canvas.DrawPixel(x, y, ChartRenderStyle.ApplyOpacity(paint, opacity), style.BlendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal static double MaskCoverage(RawBitmap mask, int x, int y, int factor)
    {
        int total = 0;
        int sampleY = 0;
        while (sampleY < factor)
        {
            int sampleX = 0;
            while (sampleX < factor)
            {
                total = total + mask.GetPixel(x * factor + sampleX, y * factor + sampleY).A;
                sampleX = sampleX + 1;
            }

            sampleY = sampleY + 1;
        }

        return total / (255.0 * factor * factor);
    }

    internal static ChartColor RasterPaintColor(ChartRenderStyle style, bool fill, double x, double y)
    {
        if (fill && style.HasFillGradient)
        {
            return GradientPaintColor(style.FillGradientStart, style.FillGradientEnd, style.FillGradientColors, style.FillGradientOffsets, x, y);
        }
        if (!fill && style.HasStrokeGradient)
        {
            return GradientPaintColor(style.StrokeGradientStart, style.StrokeGradientEnd, style.StrokeGradientColors, style.StrokeGradientOffsets, x, y);
        }
        if (fill && style.HasFillPattern)
        {
            return PatternPaintColor(style.FillPattern, x, y);
        }
        if (!fill && style.HasStrokePattern)
        {
            return PatternPaintColor(style.StrokePattern, x, y);
        }
        if (fill)
        {
            return style.FillColor;
        }

        return style.StrokeColor;
    }

    internal static ChartColor GradientPaintColor(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets, double x, double y)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = dx * dx + dy * dy;
        double offset = 0.0;
        if (lengthSquared > 0.0)
        {
            offset = ((x - start.X) * dx + (y - start.Y) * dy) / lengthSquared;
        }

        return ChartRasterCanvas.GradientColor(colors, offsets, offset);
    }

    internal static ChartColor PatternPaintColor(RawBitmap pattern, double x, double y)
    {
        int patternX = (int)System.Math.Floor(x) % pattern.Width;
        int patternY = (int)System.Math.Floor(y) % pattern.Height;
        if (patternX < 0)
        {
            patternX = patternX + pattern.Width;
        }
        if (patternY < 0)
        {
            patternY = patternY + pattern.Height;
        }

        return pattern.GetPixel(patternX, patternY);
    }

    internal static void RenderCommandSvg(SvgWriter writer, ChartRenderCommand command)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        writer.ClearNextSemantic();
        if (command.Kind == ChartRenderCommandKind.BeginGroup)
        {
            string groupId = writer.ResolveId(command.Id, "group");
            writer.BeginRetainedGroup(command.Id, groupId);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.PushTransform)
        {
            string transformId = writer.ResolveId(command.Id, "transform");
            writer.BeginStructuralGroup(transformId, SvgWriter.Matrix(command.Transform), null, "chart-transform");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.PushClip)
        {
            string clipGroupId = writer.ResolveId(command.Id, "clip");
            string clipId = writer.ResolveDerivedId(clipGroupId, "clip");
            writer.WriteClipPathRect(clipId, command.ClipBounds.X, command.ClipBounds.Y, command.ClipBounds.Width, command.ClipBounds.Height);
            SvgStyle clipStyle = new SvgStyle();
            clipStyle.SetClipPath(clipId);
            writer.BeginStructuralGroup(clipGroupId, "", clipStyle, "chart-clip");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.EndGroup || command.Kind == ChartRenderCommandKind.PopTransform || command.Kind == ChartRenderCommandKind.PopClip)
        {
            writer.EndGroup();
            return;
        }

        string elementId = "";
        if (command.Kind != ChartRenderCommandKind.Clear || command.Style.HasFillGradient || command.Style.HasFillPattern || command.Style.HasStrokeGradient || command.Style.HasStrokePattern)
        {
            elementId = writer.ResolveId(command.Id, SvgIdPrefix(command.Kind));
        }
        SvgStyle style = PrepareSvgStyle(writer, command, elementId);
        writer.SetNextSemantic(command.Id);
        if (command.Kind == ChartRenderCommandKind.Clear)
        {
            writer.WriteRect(0.0, 0.0, writer.Width, writer.Height, 0.0, 0.0, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Line)
        {
            writer.WriteLine(command.X1, command.Y1, command.X2, command.Y2, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polyline)
        {
            writer.WritePolyline(command.Points, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polygon)
        {
            writer.WritePolygon(command.Points, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Rect)
        {
            writer.WriteRect(command.X1, command.Y1, command.Width, command.Height, command.RadiusX, command.RadiusY, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Circle)
        {
            writer.WriteCircle(command.X1, command.Y1, command.RadiusX, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Ellipse)
        {
            writer.WriteEllipse(command.X1, command.Y1, command.RadiusX, command.RadiusY, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Path)
        {
            writer.WritePath(command.Path, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Text)
        {
            if (command.HasTextBlock)
            {
                RenderTextBlockSvg(writer, command, style, elementId);
                return;
            }

            writer.WriteText(command.Text, command.X1, command.Y1, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Arc || command.Kind == ChartRenderCommandKind.Wedge)
        {
            ChartRenderCommand pathCommand = CreateArcPath(command);
            writer.WritePath(pathCommand.Path, style, elementId, "");
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Image)
        {
            writer.WriteImage(command.Image, command.X1, command.Y1, command.Width, command.Height, command.Style.Opacity, elementId, "");
            return;
        }

        if (command.Kind != ChartRenderCommandKind.Marker)
        {
            return;
        }

        WriteMarkerSvg(writer, command, style, elementId);
    }

    internal static SvgStyle PrepareSvgStyle(SvgWriter writer, ChartRenderCommand command, string elementId)
    {
        SvgStyle style = command.Style.ToSvgStyle();
        if (command.Style.HasFillGradient)
        {
            string paintId = writer.ResolveDerivedId(elementId, "fill-gradient");
            writer.WriteLinearGradient(paintId, command.Style.FillGradientStart.X, command.Style.FillGradientStart.Y, command.Style.FillGradientEnd.X, command.Style.FillGradientEnd.Y, command.Style.FillGradientColors, command.Style.FillGradientOffsets);
            style.SetFillReference(paintId);
        }
        else if (command.Style.HasFillPattern)
        {
            string paintId = writer.ResolveDerivedId(elementId, "fill-pattern");
            writer.BeginPattern(paintId, command.Style.FillPattern.Width, command.Style.FillPattern.Height);
            writer.WriteImage(command.Style.FillPattern, 0.0, 0.0, command.Style.FillPattern.Width, command.Style.FillPattern.Height, 1.0, "", "");
            writer.EndPattern();
            style.SetFillReference(paintId);
        }

        if (command.Style.HasStrokeGradient)
        {
            string paintId = writer.ResolveDerivedId(elementId, "stroke-gradient");
            writer.WriteLinearGradient(paintId, command.Style.StrokeGradientStart.X, command.Style.StrokeGradientStart.Y, command.Style.StrokeGradientEnd.X, command.Style.StrokeGradientEnd.Y, command.Style.StrokeGradientColors, command.Style.StrokeGradientOffsets);
            style.SetStrokeReference(paintId, command.Style.StrokeWidth);
        }
        else if (command.Style.HasStrokePattern)
        {
            string paintId = writer.ResolveDerivedId(elementId, "stroke-pattern");
            writer.BeginPattern(paintId, command.Style.StrokePattern.Width, command.Style.StrokePattern.Height);
            writer.WriteImage(command.Style.StrokePattern, 0.0, 0.0, command.Style.StrokePattern.Width, command.Style.StrokePattern.Height, 1.0, "", "");
            writer.EndPattern();
            style.SetStrokeReference(paintId, command.Style.StrokeWidth);
        }

        return style;
    }

    internal static string SvgIdPrefix(ChartRenderCommandKind kind)
    {
        if (kind == ChartRenderCommandKind.Clear)
        {
            return "clear";
        }
        if (kind == ChartRenderCommandKind.Line)
        {
            return "line";
        }
        if (kind == ChartRenderCommandKind.Polyline)
        {
            return "polyline";
        }
        if (kind == ChartRenderCommandKind.Polygon)
        {
            return "polygon";
        }
        if (kind == ChartRenderCommandKind.Rect)
        {
            return "rect";
        }
        if (kind == ChartRenderCommandKind.Circle)
        {
            return "circle";
        }
        if (kind == ChartRenderCommandKind.Ellipse)
        {
            return "ellipse";
        }
        if (kind == ChartRenderCommandKind.Path || kind == ChartRenderCommandKind.Arc || kind == ChartRenderCommandKind.Wedge)
        {
            return "path";
        }
        if (kind == ChartRenderCommandKind.Text)
        {
            return "text";
        }
        if (kind == ChartRenderCommandKind.Marker)
        {
            return "marker";
        }
        if (kind == ChartRenderCommandKind.Image)
        {
            return "image";
        }

        return "element";
    }

    internal static ChartRenderCommand CreateArcPath(ChartRenderCommand command)
    {
        ChartPath path = new ChartPath();
        double sweep = command.EndAngle - command.StartAngle;
        int segments = (int)System.Math.Ceiling(System.Math.Abs(sweep) / (System.Math.PI / 24.0));
        if (segments < 1)
        {
            segments = 1;
        }

        int index = 0;
        while (index <= segments)
        {
            double angle = command.StartAngle + sweep * index / segments;
            double x = command.X1 + System.Math.Cos(angle) * command.RadiusX;
            double y = command.Y1 + System.Math.Sin(angle) * command.RadiusX;
            if (index == 0)
            {
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }

            index = index + 1;
        }

        if (command.Kind == ChartRenderCommandKind.Wedge)
        {
            if (command.InnerRadius == 0.0)
            {
                path.LineTo(command.X1, command.Y1);
            }
            else
            {
                int reverse = segments;
                while (reverse >= 0)
                {
                    double angle = command.StartAngle + sweep * reverse / segments;
                    double x = command.X1 + System.Math.Cos(angle) * command.InnerRadius;
                    double y = command.Y1 + System.Math.Sin(angle) * command.InnerRadius;
                    path.LineTo(x, y);
                    reverse = reverse - 1;
                }
            }

            path.Close();
        }

        ChartRenderCommand result = ChartRenderCommand.CreatePath(command.Id, path, command.Style);
        return result;
    }

    internal static void RenderLineRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (HasVisibleStroke(command.Style))
        {
            ChartPoint[] points = new ChartPoint[]
            {
                new ChartPoint(command.X1, command.Y1),
                new ChartPoint(command.X2, command.Y2)
            };
            canvas.DrawPolylineWithStyle(points, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineCap, command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
        }
    }

    internal static void RenderPolylineRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (HasVisibleStroke(command.Style))
        {
            canvas.DrawPolylineWithStyle(command.Points, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineCap, command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
        }
    }

    internal static void RenderPolygonRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (HasVisibleFill(command.Style))
        {
            canvas.FillPolygon(command.Points, command.Style.RasterFillColor(), command.Style.FillRule, command.Style.BlendMode);
        }

        if (HasVisibleStroke(command.Style))
        {
            canvas.StrokePolygonWithStyle(command.Points, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
        }
    }

    internal static void RenderRectRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        ChartRect rect = new ChartRect(command.X1, command.Y1, command.Width, command.Height);
        bool rounded = command.RadiusX > 0.0 || command.RadiusY > 0.0;
        ChartPath roundedPath = new ChartPath();
        if (rounded)
        {
            roundedPath = CreateRoundedRectanglePath(command.X1, command.Y1, command.Width, command.Height, command.RadiusX, command.RadiusY);
        }
        if (HasVisibleFill(command.Style))
        {
            if (rounded)
            {
                canvas.FillPath(roundedPath, command.Style.RasterFillColor(), command.Style.FillRule, command.Style.BlendMode);
            }
            else if (command.Style.HasFillGradient)
            {
                canvas.FillRectangleGradient(rect, command.Style.FillGradientStart, command.Style.FillGradientEnd, command.Style.FillGradientColors, command.Style.FillGradientOffsets, command.Style.Opacity * command.Style.FillOpacity, command.Style.BlendMode);
            }
            else if (command.Style.HasFillPattern)
            {
                canvas.FillRectanglePattern(rect, command.Style.FillPattern, command.Style.Opacity * command.Style.FillOpacity, command.Style.BlendMode);
            }
            else
            {
                canvas.FillRectangle(rect, command.Style.RasterFillColor(), command.Style.BlendMode);
            }
        }

        if (HasVisibleStroke(command.Style))
        {
            if (rounded)
            {
                canvas.StrokePathWithStyle(roundedPath, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineCap, command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, 0.25, command.Style.BlendMode);
            }
            else
            {
                canvas.StrokeRectangleWithStyle(rect, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
            }
        }
    }

    internal static void RenderCircleRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (HasVisibleFill(command.Style))
        {
            canvas.FillCircle(command.X1, command.Y1, command.RadiusX, command.Style.RasterFillColor(), command.Style.BlendMode);
        }

        if (HasVisibleStroke(command.Style))
        {
            canvas.StrokeEllipseWithStyle(command.X1, command.Y1, command.RadiusX, command.RadiusX, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
        }
    }

    internal static void RenderEllipseRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (HasVisibleFill(command.Style))
        {
            canvas.FillEllipse(command.X1, command.Y1, command.RadiusX, command.RadiusY, command.Style.RasterFillColor(), command.Style.BlendMode);
        }

        if (HasVisibleStroke(command.Style))
        {
            canvas.StrokeEllipseWithStyle(command.X1, command.Y1, command.RadiusX, command.RadiusY, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
        }
    }

    internal static void RenderPathRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (HasVisibleFill(command.Style))
        {
            canvas.FillPath(command.Path, command.Style.RasterFillColor(), command.Style.FillRule, command.Style.BlendMode);
        }

        if (HasVisibleStroke(command.Style))
        {
            canvas.StrokePathWithStyle(command.Path, command.Style.StrokeWidth, command.Style.RasterStrokeColor(), command.Style.LineCap, command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, 0.25, command.Style.BlendMode);
        }
    }

    internal static void RenderTextRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        if (command.HasTextBlock)
        {
            RenderTextBlockRaster(canvas, command);
            return;
        }

        int x = RoundToInt(command.X1);
        int y = RoundToInt(command.Y1);
        if (command.Style.HasTextShadow)
        {
            int shadowX = x + RoundToInt(command.Style.TextShadowOffsetX);
            int shadowY = y + RoundToInt(command.Style.TextShadowOffsetY);
            ChartColor shadowColor = ChartRenderStyle.ApplyOpacity(command.Style.TextShadowColor, command.Style.Opacity);
            canvas.DrawText(command.Text, shadowX, shadowY, ChartBitmapFont.FiveBySeven(), command.Style.TextScale, shadowColor, command.Style.BlendMode);
        }

        if (command.Style.HasTextOutline)
        {
            int radius = RoundToInt(command.Style.TextOutlineWidth);
            if (radius < 1)
            {
                radius = 1;
            }

            ChartColor outlineColor = ChartRenderStyle.ApplyOpacity(command.Style.TextOutlineColor, command.Style.Opacity);
            int offsetY = -radius;
            while (offsetY <= radius)
            {
                int offsetX = -radius;
                while (offsetX <= radius)
                {
                    if (offsetX != 0 || offsetY != 0)
                    {
                        canvas.DrawText(command.Text, x + offsetX, y + offsetY, ChartBitmapFont.FiveBySeven(), command.Style.TextScale, outlineColor, command.Style.BlendMode);
                    }

                    offsetX = offsetX + 1;
                }

                offsetY = offsetY + 1;
            }
        }

        ChartColor color = TextRasterColor(command.Style);
        canvas.DrawText(command.Text, x, y, ChartBitmapFont.FiveBySeven(), command.Style.TextScale, color, command.Style.BlendMode);
    }

    internal static void RenderTextBlockRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        ChartTextBlock block = command.TextBlock;
        int anchorX = RoundToInt(command.X1);
        int anchorY = RoundToInt(command.Y1);
        ChartTextLayout layout = block.LayoutBuiltIn(anchorX, anchorY, command.Style.TextScale);
        int clipX = canvas.ClipX;
        int clipY = canvas.ClipY;
        int clipWidth = canvas.ClipWidth;
        int clipHeight = canvas.ClipHeight;
        if (layout.HasClip)
        {
            ChartRect existingClip = new ChartRect(clipX, clipY, clipWidth, clipHeight);
            canvas.SetClipRect(existingClip.Intersection(layout.ClipBounds));
        }

        if (block.RichSpanCount > 0 && block.MaximumWidth == 0 && layout.Lines.Length == 1)
        {
            RenderRichTextBlockRaster(canvas, command, layout);
        }
        else if (block.RotationDegrees == 0.0)
        {
            ChartColor color = TextRasterColor(command.Style);
            canvas.DrawTextLayout(layout, ChartBitmapFont.FiveBySeven(), command.Style.TextScale, color, command.Style.BlendMode);
        }
        else
        {
            ChartColor color = TextRasterColor(command.Style);
            double radians = block.RotationDegrees * System.Math.PI / 180.0;
            canvas.DrawRotatedTextLayout(layout, ChartBitmapFont.FiveBySeven(), command.Style.TextScale, color, command.Style.BlendMode, radians, command.X1, command.Y1);
        }

        canvas.SetClip(clipX, clipY, clipWidth, clipHeight);
    }

    internal static void RenderRichTextBlockRaster(ChartRasterCanvas canvas, ChartRenderCommand command, ChartTextLayout layout)
    {
        ChartTextBlock block = command.TextBlock;
        ChartBitmapFont font = ChartBitmapFont.FiveBySeven();
        int scale = command.Style.TextScale;
        int cursorX = layout.Lines[0].X;
        if (block.Direction == ChartTextDirection.RightToLeft)
        {
            int reverse = block.RichSpanCount - 1;
            while (reverse >= 0)
            {
                cursorX = RenderRichTextSpanRaster(canvas, command, layout.Lines[0], block.RichSpans[reverse], cursorX, font, scale, block.Direction);
                reverse = reverse - 1;
            }

            return;
        }

        int index = 0;
        while (index < block.RichSpanCount)
        {
            cursorX = RenderRichTextSpanRaster(canvas, command, layout.Lines[0], block.RichSpans[index], cursorX, font, scale, block.Direction);
            index = index + 1;
        }
    }

    internal static int RenderRichTextSpanRaster(ChartRasterCanvas canvas, ChartRenderCommand command, ChartTextLayoutLine line, ChartTextSpan span, int cursorX, ChartBitmapFont font, int scale, ChartTextDirection direction)
    {
        string prepared = ChartTextLayoutOperations.PrepareBuiltInText(span.Text, direction);
        int width = font.TextWidth(prepared, scale);
        ChartColor color = TextRasterColor(span.Style);
        if (command.TextBlock.RotationDegrees == 0.0)
        {
            canvas.DrawText(prepared, cursorX, line.Y, font, scale, color, span.Style.BlendMode);
        }
        else
        {
            ChartTextLayoutLine richLine = new ChartTextLayoutLine(prepared, cursorX, line.Y, width, line.BaselineY);
            ChartTextLayout richLayout = new ChartTextLayout(new ChartTextLayoutLine[] { richLine }, cursorX, line.Y, width, command.Style.TextScale * font.GlyphHeight);
            double radians = command.TextBlock.RotationDegrees * System.Math.PI / 180.0;
            canvas.DrawRotatedTextLayout(richLayout, font, scale, color, span.Style.BlendMode, radians, command.X1, command.Y1);
        }

        return cursorX + prepared.Length * font.GlyphAdvance(scale);
    }

    internal static void RenderTextBlockSvg(SvgWriter writer, ChartRenderCommand command, SvgStyle style, string elementId)
    {
        ChartTextBlock block = command.TextBlock;
        int anchorX = RoundToInt(command.X1);
        int anchorY = RoundToInt(command.Y1);
        ChartTextLayout layout = block.LayoutBuiltIn(anchorX, anchorY, command.Style.TextScale);
        string transform = "";
        if (block.RotationDegrees != 0.0)
        {
            transform = SvgWriter.Rotate(block.RotationDegrees, command.X1, command.Y1);
        }

        if (layout.HasClip)
        {
            string clipId = writer.ResolveDerivedId(elementId, "clip");
            writer.WriteClipPathRect(clipId, layout.ClipBounds.X, layout.ClipBounds.Y, layout.ClipBounds.Width, layout.ClipBounds.Height);
            style.SetClipPath(clipId);
        }

        if (block.RichSpanCount > 0 && block.MaximumWidth == 0 && layout.Lines.Length == 1)
        {
            writer.WriteRichText(block.RichSpans, block.RichSpanCount, layout.Lines[0].X, layout.Lines[0].BaselineY, style, elementId, transform, block.Direction);
            return;
        }

        int index = 0;
        while (index < layout.Lines.Length)
        {
            string lineId = elementId;
            if (layout.Lines.Length > 1)
            {
                lineId = writer.ResolveDerivedId(elementId, "line-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            writer.WriteText(layout.Lines[index].Text, layout.Lines[index].X, layout.Lines[index].BaselineY, style, lineId, transform);
            index = index + 1;
        }
    }

    internal static void RenderMarkerRaster(ChartRasterCanvas canvas, ChartRenderCommand command)
    {
        ChartColor color = MarkerRasterColor(command.Style);
        if (command.MarkerShape == ChartMarkerShape.CustomPath)
        {
            if (HasVisibleFill(command.Style))
            {
                canvas.DrawCustomMarker(command.X1, command.Y1, command.MarkerSize, command.MarkerPath, color, command.Style.FillRule, command.Style.BlendMode);
            }
            else if (HasVisibleStroke(command.Style))
            {
                canvas.StrokeCustomMarkerWithStyle(command.X1, command.Y1, command.MarkerSize, command.MarkerPath, command.Style.StrokeWidth, color, command.Style.LineCap, command.Style.LineJoin, command.Style.DashArray, command.Style.MiterLimit, command.Style.BlendMode);
            }

            return;
        }

        canvas.DrawMarker(command.X1, command.Y1, command.MarkerSize, command.MarkerShape, color, command.Style.BlendMode);
    }

    internal static void WriteMarkerSvg(SvgWriter writer, ChartRenderCommand command, SvgStyle style, string elementId)
    {
        double half = command.MarkerSize * 0.5;
        if (command.MarkerShape == ChartMarkerShape.Circle)
        {
            writer.WriteCircle(command.X1, command.Y1, half, style, elementId, "");
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.Square)
        {
            writer.WriteRect(command.X1 - half, command.Y1 - half, command.MarkerSize, command.MarkerSize, 0.0, 0.0, style, elementId, "");
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.Diamond)
        {
            writer.WritePolygon(new ChartPoint[] { new ChartPoint(command.X1, command.Y1 - half), new ChartPoint(command.X1 + half, command.Y1), new ChartPoint(command.X1, command.Y1 + half), new ChartPoint(command.X1 - half, command.Y1) }, style, elementId, "");
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.Triangle)
        {
            writer.WritePolygon(new ChartPoint[] { new ChartPoint(command.X1, command.Y1 - half), new ChartPoint(command.X1 + half, command.Y1 + half), new ChartPoint(command.X1 - half, command.Y1 + half) }, style, elementId, "");
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.Star)
        {
            writer.WritePolygon(ChartRasterCanvas.CreateStarPoints(command.X1, command.Y1, half, half * 0.45), style, elementId, "");
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.Line)
        {
            writer.WriteLine(command.X1 - half, command.Y1, command.X1 + half, command.Y1, style, elementId, "");
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.CustomPath)
        {
            writer.WritePath(command.MarkerPath, style, elementId, SvgWriter.Translate(command.X1, command.Y1) + " " + SvgWriter.Scale(command.MarkerSize, command.MarkerSize));
            return;
        }

        WriteCrossMarkerSvg(writer, command, style, elementId);
    }

    internal static void WriteCrossMarkerSvg(SvgWriter writer, ChartRenderCommand command, SvgStyle style, string elementId)
    {
        double half = command.MarkerSize * 0.5;
        if (command.MarkerShape == ChartMarkerShape.Cross)
        {
            writer.WriteLine(command.X1 - half, command.Y1 - half, command.X1 + half, command.Y1 + half, style, writer.ResolveDerivedId(elementId, "a"), "");
            writer.WriteLine(command.X1 - half, command.Y1 + half, command.X1 + half, command.Y1 - half, style, writer.ResolveDerivedId(elementId, "b"), "");
            return;
        }

        writer.WriteLine(command.X1 - half, command.Y1, command.X1 + half, command.Y1, style, writer.ResolveDerivedId(elementId, "h"), "");
        writer.WriteLine(command.X1, command.Y1 - half, command.X1, command.Y1 + half, style, writer.ResolveDerivedId(elementId, "v"), "");
    }

    internal static bool HasVisibleFill(ChartRenderStyle style)
    {
        bool visiblePaint = style.FillColor.A > 0 || style.HasFillGradient || style.HasFillPattern;
        return style.HasFill && !style.FillNone && visiblePaint && style.Opacity > 0.0 && style.FillOpacity > 0.0;
    }

    internal static bool HasVisibleStroke(ChartRenderStyle style)
    {
        bool visiblePaint = style.StrokeColor.A > 0 || style.HasStrokeGradient || style.HasStrokePattern;
        return style.HasStroke && !style.StrokeNone && visiblePaint && style.StrokeWidth > 0.0 && style.Opacity > 0.0 && style.StrokeOpacity > 0.0;
    }

    internal static ChartColor TextRasterColor(ChartRenderStyle style)
    {
        if (HasVisibleFill(style))
        {
            return style.RasterFillColor();
        }

        if (HasVisibleStroke(style))
        {
            return style.RasterStrokeColor();
        }

        return ChartRenderStyle.ApplyOpacity(ChartColor.Black(), style.Opacity);
    }

    internal static ChartColor MarkerRasterColor(ChartRenderStyle style)
    {
        if (HasVisibleFill(style))
        {
            return style.RasterFillColor();
        }

        if (HasVisibleStroke(style))
        {
            return style.RasterStrokeColor();
        }

        return ChartRenderStyle.ApplyOpacity(ChartColor.Black(), style.Opacity);
    }

    internal static int RoundToInt(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double rounded = System.Math.Round(value);
        if (rounded < int.MinValue || rounded > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return (int)rounded;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(int value)
    {
        Width = value;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(int value)
    {
        Height = value;
    }

    /// <summary>Returns the current commands.</summary>
    /// <returns>The current field value.</returns>
    public ChartRenderCommand[] GetCommands()
    {
        return Commands;
    }

    /// <summary>Replaces the current commands.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCommands(ChartRenderCommand[] value)
    {
        Commands = value;
    }

    /// <summary>Returns the current command count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCommandCount()
    {
        return CommandCount;
    }

    /// <summary>Replaces the current command count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCommandCount(int value)
    {
        CommandCount = value;
    }
}
