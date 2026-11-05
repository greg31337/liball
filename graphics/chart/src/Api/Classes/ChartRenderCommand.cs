namespace chart;
/// <summary>
/// Represents a render command used by the chart model or rendering pipeline.
/// </summary>
public class ChartRenderCommand : IChartRenderCommand
{
    internal ChartRenderCommandKind Kind;
    internal string Id;
    internal double X1;
    internal double Y1;
    internal double X2;
    internal double Y2;
    internal double Width;
    internal double Height;
    internal double RadiusX;
    internal double RadiusY;
    internal ChartPoint[] Points;
    internal ChartPath Path;
    internal string Text;
    internal ChartMarkerShape MarkerShape;
    internal ChartPath MarkerPath;
    internal double MarkerSize;
    internal ChartRenderStyle Style;
    internal bool HasTextBlock;
    internal ChartTextBlock TextBlock;
    internal double StartAngle;
    internal double EndAngle;
    internal double InnerRadius;
    internal ChartMatrix Transform;
    internal ChartRect ClipBounds;
    internal RawBitmap Image;
    /// <summary>
    /// Initializes a render command from the supplied kind, id, and style.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartRenderCommand(ChartRenderCommandKind kind, string id, ChartRenderStyle style)
    {
        ValidateKind(kind);
        ChartTextBlock.ValidateText(id, "id");
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        Kind = kind;
        Id = id;
        X1 = 0.0;
        Y1 = 0.0;
        X2 = 0.0;
        Y2 = 0.0;
        Width = 0.0;
        Height = 0.0;
        RadiusX = 0.0;
        RadiusY = 0.0;
        Points = new ChartPoint[0];
        Path = new ChartPath();
        Text = "";
        MarkerShape = ChartMarkerShape.Circle;
        MarkerPath = new ChartPath();
        MarkerSize = 1.0;
        Style = style.Copy();
        HasTextBlock = false;
        TextBlock = new ChartTextBlock();
        StartAngle = 0.0;
        EndAngle = 0.0;
        InnerRadius = 0.0;
        Transform = ChartMatrix.Identity();
        ClipBounds = new ChartRect(0.0, 0.0, 0.0, 0.0);
        Image = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
    }

    internal static ChartRenderCommand CreateClear(ChartColor color)
    {
        ChartRenderStyle style = ChartRenderStyle.CreateFill(color);
        return new ChartRenderCommand(ChartRenderCommandKind.Clear, "", style);
    }

    internal static ChartRenderCommand CreateLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style)
    {
        ValidateFinitePair(x1, y1, "line");
        ValidateFinitePair(x2, y2, "line");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Line, id, style);
        command.X1 = x1;
        command.Y1 = y1;
        command.X2 = x2;
        command.Y2 = y2;
        return command;
    }

    internal static ChartRenderCommand CreatePolyline(string id, ChartPoint[] points, ChartRenderStyle style)
    {
        ValidatePoints(points, 2);
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Polyline, id, style);
        command.Points = CopyPoints(points);
        return command;
    }

    internal static ChartRenderCommand CreatePolygon(string id, ChartPoint[] points, ChartRenderStyle style)
    {
        ValidatePoints(points, 3);
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Polygon, id, style);
        command.Points = CopyPoints(points);
        return command;
    }

    internal static ChartRenderCommand CreateRect(string id, double x, double y, double width, double height, double radiusX, double radiusY, ChartRenderStyle style)
    {
        ValidateFinitePair(x, y, "rect");
        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        ChartStyle.ValidateNonNegative(radiusX, "radiusX");
        ChartStyle.ValidateNonNegative(radiusY, "radiusY");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Rect, id, style);
        command.X1 = x;
        command.Y1 = y;
        command.Width = width;
        command.Height = height;
        command.RadiusX = radiusX;
        command.RadiusY = radiusY;
        return command;
    }

    internal static ChartRenderCommand CreateCircle(string id, double centerX, double centerY, double radius, ChartRenderStyle style)
    {
        ValidateFinitePair(centerX, centerY, "circle");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Circle, id, style);
        command.X1 = centerX;
        command.Y1 = centerY;
        command.RadiusX = radius;
        command.RadiusY = radius;
        return command;
    }

    internal static ChartRenderCommand CreateEllipse(string id, double centerX, double centerY, double radiusX, double radiusY, ChartRenderStyle style)
    {
        ValidateFinitePair(centerX, centerY, "ellipse");
        ChartRenderStyle.ValidatePositive(radiusX, "radiusX");
        ChartRenderStyle.ValidatePositive(radiusY, "radiusY");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Ellipse, id, style);
        command.X1 = centerX;
        command.Y1 = centerY;
        command.RadiusX = radiusX;
        command.RadiusY = radiusY;
        return command;
    }

    internal static ChartRenderCommand CreatePath(string id, ChartPath path, ChartRenderStyle style)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Path, id, style);
        command.Path = CopyPath(path);
        return command;
    }

    internal static ChartRenderCommand CreateText(string id, string text, double x, double y, ChartRenderStyle style)
    {
        ChartTextBlock.ValidateText(text, "text");
        ValidateFinitePair(x, y, "text");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Text, id, style);
        command.Text = text;
        command.X1 = x;
        command.Y1 = y;
        return command;
    }

    internal static ChartRenderCommand CreateTextBlock(string id, ChartTextBlock textBlock, double x, double y)
    {
        if (textBlock == null)
        {
            throw new System.ArgumentNullException("textBlock");
        }

        ValidateFinitePair(x, y, "textBlock");
        ChartRenderStyle style = ChartRenderStyle.CreateFill(textBlock.Color);
        style.SetFont(textBlock.FontFamily, textBlock.FontSize);
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Text, id, style);
        command.Text = textBlock.Text;
        command.X1 = x;
        command.Y1 = y;
        command.HasTextBlock = true;
        command.TextBlock = textBlock.Copy();
        return command;
    }

    internal static ChartRenderCommand CreateMarker(string id, ChartMarkerShape shape, double centerX, double centerY, double size, ChartRenderStyle style)
    {
        ValidateMarkerShape(shape);
        ValidateFinitePair(centerX, centerY, "marker");
        ChartRenderStyle.ValidatePositive(size, "size");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Marker, id, style);
        command.MarkerShape = shape;
        command.X1 = centerX;
        command.Y1 = centerY;
        command.MarkerSize = size;
        return command;
    }

    internal static ChartRenderCommand CreateCustomMarker(string id, ChartPath markerPath, double centerX, double centerY, double size, ChartRenderStyle style)
    {
        if (markerPath == null)
        {
            throw new System.ArgumentNullException("markerPath");
        }

        ChartRenderCommand command = CreateMarker(id, ChartMarkerShape.CustomPath, centerX, centerY, size, style);
        command.MarkerPath = CopyPath(markerPath);
        return command;
    }

    internal static ChartRenderCommand CreateArc(string id, double centerX, double centerY, double radius, double startAngle, double endAngle, ChartRenderStyle style)
    {
        ValidateFinitePair(centerX, centerY, "arc");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        ValidateAngles(startAngle, endAngle);
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Arc, id, style);
        command.X1 = centerX;
        command.Y1 = centerY;
        command.RadiusX = radius;
        command.StartAngle = startAngle;
        command.EndAngle = endAngle;
        return command;
    }

    internal static ChartRenderCommand CreateWedge(string id, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartRenderStyle style)
    {
        ValidateFinitePair(centerX, centerY, "wedge");
        ChartStyle.ValidateNonNegative(innerRadius, "innerRadius");
        ChartRenderStyle.ValidatePositive(outerRadius, "outerRadius");
        if (innerRadius >= outerRadius)
        {
            throw new System.ArgumentOutOfRangeException("innerRadius");
        }

        ValidateAngles(startAngle, endAngle);
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Wedge, id, style);
        command.X1 = centerX;
        command.Y1 = centerY;
        command.InnerRadius = innerRadius;
        command.RadiusX = outerRadius;
        command.StartAngle = startAngle;
        command.EndAngle = endAngle;
        return command;
    }

    internal static ChartRenderCommand CreateImage(string id, RawBitmap bitmap, double x, double y, double width, double height, ChartRenderStyle style)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        ValidateFinitePair(x, y, "image");
        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.Image, id, style);
        command.Image = ChartRenderStyle.CopyBitmap(bitmap);
        command.X1 = x;
        command.Y1 = y;
        command.Width = width;
        command.Height = height;
        return command;
    }

    internal static ChartRenderCommand CreateBeginGroup(string id)
    {
        return new ChartRenderCommand(ChartRenderCommandKind.BeginGroup, id, new ChartRenderStyle());
    }

    internal static ChartRenderCommand CreateEndGroup()
    {
        return new ChartRenderCommand(ChartRenderCommandKind.EndGroup, "", new ChartRenderStyle());
    }

    internal static ChartRenderCommand CreatePushTransform(string id, ChartMatrix transform)
    {
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.PushTransform, id, new ChartRenderStyle());
        command.Transform = CopyMatrix(transform);
        return command;
    }

    internal static ChartRenderCommand CreatePopTransform()
    {
        return new ChartRenderCommand(ChartRenderCommandKind.PopTransform, "", new ChartRenderStyle());
    }

    internal static ChartRenderCommand CreatePushClip(string id, ChartRect bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        ChartRenderStyle.ValidatePositive(bounds.Width, "width");
        ChartRenderStyle.ValidatePositive(bounds.Height, "height");
        ValidateFinitePair(bounds.X, bounds.Y, "bounds");
        ChartRenderCommand command = new ChartRenderCommand(ChartRenderCommandKind.PushClip, id, new ChartRenderStyle());
        command.ClipBounds = CopyRect(bounds);
        return command;
    }

    internal static ChartRenderCommand CreatePopClip()
    {
        return new ChartRenderCommand(ChartRenderCommandKind.PopClip, "", new ChartRenderStyle());
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartRenderCommand Copy()
    {
        ChartRenderCommand command = new ChartRenderCommand(Kind, Id, Style);
        command.X1 = X1;
        command.Y1 = Y1;
        command.X2 = X2;
        command.Y2 = Y2;
        command.Width = Width;
        command.Height = Height;
        command.RadiusX = RadiusX;
        command.RadiusY = RadiusY;
        command.Points = CopyPoints(Points);
        command.Path = CopyPath(Path);
        command.Text = Text;
        command.MarkerShape = MarkerShape;
        command.MarkerPath = CopyPath(MarkerPath);
        command.MarkerSize = MarkerSize;
        command.HasTextBlock = HasTextBlock;
        command.TextBlock = TextBlock.Copy();
        command.StartAngle = StartAngle;
        command.EndAngle = EndAngle;
        command.InnerRadius = InnerRadius;
        command.Transform = CopyMatrix(Transform);
        command.ClipBounds = CopyRect(ClipBounds);
        command.Image = ChartRenderStyle.CopyBitmap(Image);
        return command;
    }

    internal static ChartPoint[] CopyPoints(ChartPoint[] points)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        ChartPoint[] copy = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Render points cannot contain null values.");
            }

            copy[index] = points[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static ChartPath CopyPath(ChartPath path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        ChartPath copy = new ChartPath();
        int index = 0;
        while (index < path.Commands.Count)
        {
            ChartPathCommand command = path.Commands[index];
            if (command.Kind == ChartPathCommandKind.MoveTo)
            {
                copy.MoveTo(command.X1, command.Y1);
            }
            else if (command.Kind == ChartPathCommandKind.LineTo)
            {
                copy.LineTo(command.X1, command.Y1);
            }
            else if (command.Kind == ChartPathCommandKind.QuadraticBezierTo)
            {
                copy.QuadraticBezierTo(command.X1, command.Y1, command.X2, command.Y2);
            }
            else if (command.Kind == ChartPathCommandKind.CubicBezierTo)
            {
                copy.CubicBezierTo(command.X1, command.Y1, command.X2, command.Y2, command.X3, command.Y3);
            }
            else if (command.Kind == ChartPathCommandKind.Close)
            {
                copy.Close();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateKind(ChartRenderCommandKind kind)
    {
        int value = (int)kind;
        if (value < 0 || value > 18)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }

    internal static void ValidateAngles(double startAngle, double endAngle)
    {
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        ChartLinearScale.ValidateFinite(endAngle, "endAngle");
        if (startAngle == endAngle)
        {
            throw new System.ArgumentException("Arc angles must describe a nonzero sweep.");
        }
    }

    internal static ChartMatrix CopyMatrix(ChartMatrix matrix)
    {
        return new ChartMatrix(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.OffsetX, matrix.OffsetY);
    }

    internal static ChartRect CopyRect(ChartRect rect)
    {
        return new ChartRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    internal static void ValidateMarkerShape(ChartMarkerShape shape)
    {
        int value = (int)shape;
        if (value < 0 || value > 8)
        {
            throw new System.ArgumentOutOfRangeException("shape");
        }
    }

    internal static void ValidatePoints(ChartPoint[] points, int minimumCount)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        if (points.Length < minimumCount)
        {
            throw new System.ArgumentException("Not enough render points.");
        }

        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Render points cannot contain null values.");
            }

            ValidateFinitePair(points[index].X, points[index].Y, "points");
            index = index + 1;
        }
    }

    internal static void ValidateFinitePair(double x, double y, string name)
    {
        ChartLinearScale.ValidateFinite(x, name);
        ChartLinearScale.ValidateFinite(y, name);
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartRenderCommandKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartRenderCommandKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current x1.</summary>
    /// <returns>The current field value.</returns>
    public double GetX1()
    {
        return X1;
    }

    /// <summary>Replaces the current x1.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX1(double value)
    {
        X1 = value;
    }

    /// <summary>Returns the current y1.</summary>
    /// <returns>The current field value.</returns>
    public double GetY1()
    {
        return Y1;
    }

    /// <summary>Replaces the current y1.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY1(double value)
    {
        Y1 = value;
    }

    /// <summary>Returns the current x2.</summary>
    /// <returns>The current field value.</returns>
    public double GetX2()
    {
        return X2;
    }

    /// <summary>Replaces the current x2.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX2(double value)
    {
        X2 = value;
    }

    /// <summary>Returns the current y2.</summary>
    /// <returns>The current field value.</returns>
    public double GetY2()
    {
        return Y2;
    }

    /// <summary>Replaces the current y2.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY2(double value)
    {
        Y2 = value;
    }

    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    public double GetWidth()
    {
        return Width;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(double value)
    {
        Width = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public double GetHeight()
    {
        return Height;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(double value)
    {
        Height = value;
    }

    /// <summary>Returns the current radius x.</summary>
    /// <returns>The current field value.</returns>
    public double GetRadiusX()
    {
        return RadiusX;
    }

    /// <summary>Replaces the current radius x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRadiusX(double value)
    {
        RadiusX = value;
    }

    /// <summary>Returns the current radius y.</summary>
    /// <returns>The current field value.</returns>
    public double GetRadiusY()
    {
        return RadiusY;
    }

    /// <summary>Replaces the current radius y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRadiusY(double value)
    {
        RadiusY = value;
    }

    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    public ChartPoint[] GetPoints()
    {
        return Points;
    }

    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPoints(ChartPoint[] value)
    {
        Points = value;
    }

    /// <summary>Returns the current path.</summary>
    /// <returns>The current field value.</returns>
    public ChartPath GetPath()
    {
        return Path;
    }

    /// <summary>Replaces the current path.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPath(ChartPath value)
    {
        Path = value;
    }

    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    public string GetText()
    {
        return Text;
    }

    /// <summary>Replaces the current text.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetText(string value)
    {
        Text = value;
    }

    /// <summary>Returns the current marker shape.</summary>
    /// <returns>The current field value.</returns>
    public ChartMarkerShape GetMarkerShape()
    {
        return MarkerShape;
    }

    /// <summary>Replaces the current marker shape.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMarkerShape(ChartMarkerShape value)
    {
        MarkerShape = value;
    }

    /// <summary>Returns the current marker path.</summary>
    /// <returns>The current field value.</returns>
    public ChartPath GetMarkerPath()
    {
        return MarkerPath;
    }

    /// <summary>Replaces the current marker path.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMarkerPath(ChartPath value)
    {
        MarkerPath = value;
    }

    /// <summary>Returns the current marker size.</summary>
    /// <returns>The current field value.</returns>
    public double GetMarkerSize()
    {
        return MarkerSize;
    }

    /// <summary>Replaces the current marker size.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMarkerSize(double value)
    {
        MarkerSize = value;
    }

    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    public ChartRenderStyle GetStyle()
    {
        return Style;
    }

    /// <summary>Replaces the current style.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStyle(ChartRenderStyle value)
    {
        Style = value;
    }

    /// <summary>Returns whether this text command retains a complete text block.</summary>
    /// <returns>The text-block state.</returns>
    public bool GetHasTextBlock()
    {
        return HasTextBlock;
    }

    /// <summary>Returns an independent retained text block.</summary>
    /// <returns>A copied text block.</returns>
    public ChartTextBlock GetTextBlock()
    {
        return TextBlock.Copy();
    }

    /// <summary>Returns the arc start angle in radians.</summary>
    /// <returns>The retained start angle.</returns>
    public double GetStartAngle()
    {
        return StartAngle;
    }

    /// <summary>Replaces the arc start angle in radians.</summary>
    /// <param name = "value">The finite replacement angle.</param>
    public void SetStartAngle(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        StartAngle = value;
    }

    /// <summary>Returns the arc end angle in radians.</summary>
    /// <returns>The retained end angle.</returns>
    public double GetEndAngle()
    {
        return EndAngle;
    }

    /// <summary>Replaces the arc end angle in radians.</summary>
    /// <param name = "value">The finite replacement angle.</param>
    public void SetEndAngle(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        EndAngle = value;
    }

    /// <summary>Returns the inner radius of a wedge.</summary>
    /// <returns>The retained inner radius.</returns>
    public double GetInnerRadius()
    {
        return InnerRadius;
    }

    /// <summary>Replaces the inner radius of a wedge.</summary>
    /// <param name = "value">The nonnegative replacement radius.</param>
    public void SetInnerRadius(double value)
    {
        ChartStyle.ValidateNonNegative(value, "value");
        InnerRadius = value;
    }

    /// <summary>Returns an independent copy of the retained affine transform.</summary>
    /// <returns>The retained transform.</returns>
    public ChartMatrix GetTransform()
    {
        return CopyMatrix(Transform);
    }

    /// <summary>Replaces the retained affine transform.</summary>
    /// <param name = "value">The replacement transform.</param>
    public void SetTransform(ChartMatrix value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        Transform = CopyMatrix(value);
    }

    /// <summary>Returns an independent copy of the retained clip bounds.</summary>
    /// <returns>The retained clip bounds.</returns>
    public ChartRect GetClipBounds()
    {
        return CopyRect(ClipBounds);
    }

    /// <summary>Replaces the retained clip bounds.</summary>
    /// <param name = "value">The replacement clip bounds.</param>
    public void SetClipBounds(ChartRect value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        ClipBounds = CopyRect(value);
    }

    /// <summary>Returns an independent copy of the retained raw image.</summary>
    /// <returns>The retained raw bitmap.</returns>
    public RawBitmap GetImage()
    {
        return ChartRenderStyle.CopyBitmap(Image);
    }

    /// <summary>Replaces the retained raw image.</summary>
    /// <param name = "value">The replacement raw bitmap.</param>
    public void SetImage(RawBitmap value)
    {
        Image = ChartRenderStyle.CopyBitmap(value);
    }
}
