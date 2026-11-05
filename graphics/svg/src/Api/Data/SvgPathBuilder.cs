namespace svg;

/// <summary>Contains validated path data or a generated path element.</summary>
public class SvgPathBuildResult
{
    /// <summary>Whether the requested conversion succeeded.</summary>
    public bool Success;
    /// <summary>The stable builder failure.</summary>
    public SvgPathBuilderError Error;
    /// <summary>The independently allocated path data, or <c>null</c>.</summary>
    public SvgPathData? PathData;
    /// <summary>The detached or attached generated path element, or <c>null</c>.</summary>
    public SvgNode? Element;
    /// <summary>The stable human-readable result message.</summary>
    public string? Message;

    /// <summary>Creates an explicit path-build result.</summary>
    /// <param name="success">Whether the conversion succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="pathData">The generated path data, or <c>null</c>.</param>
    /// <param name="element">The generated path element, or <c>null</c>.</param>
    /// <param name="message">The result message.</param>
    public SvgPathBuildResult(bool success, SvgPathBuilderError error, SvgPathData? pathData, SvgNode? element, string? message)
    {
        Success = success;
        Error = error;
        PathData = pathData;
        Element = element;
        Message = message;
    }
}

/// <summary>Builds bounded SVG path commands, shapes, and transformed compositions.</summary>
public class SvgPathBuilder
{
    System.Collections.Generic.List<SvgPathCommand> commands;
    bool hasCurrent;
    double currentX;
    double currentY;
    double subpathX;
    double subpathY;
    int subpathIndex;

    /// <summary>The positive maximum command count.</summary>
    public int MaximumCommands;
    /// <summary>The first sticky builder failure, or <c>None</c>.</summary>
    public SvgPathBuilderError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string? Message;
    /// <summary>The most recent path transformation failure, or <c>None</c>.</summary>
    public SvgPathTransformError TransformError;

    /// <summary>Creates an empty path builder with a secure command bound.</summary>
    public SvgPathBuilder()
    {
        MaximumCommands = 1000000;
        commands = new System.Collections.Generic.List<SvgPathCommand>();
        ResetState();
    }

    /// <summary>Creates an empty path builder with an explicit positive command bound.</summary>
    /// <param name="maximumCommands">The positive maximum command count.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the bound is not positive.</exception>
    /// <returns>A new bounded path builder.</returns>
    internal static SvgPathBuilder Create(int maximumCommands)
    {
        if (maximumCommands < 1) throw new System.ArgumentOutOfRangeException("maximumCommands");
        SvgPathBuilder result = new SvgPathBuilder();
        result.MaximumCommands = maximumCommands;
        return result;
    }

    /// <summary>Returns the current command count.</summary>
    /// <returns>The number of authored commands.</returns>
    internal int GetCommandCount()
    {
        return commands.Count;
    }

    /// <summary>Tests whether no builder operation has failed.</summary>
    /// <returns><c>true</c> when the builder has no sticky error.</returns>
    internal bool Succeeded()
    {
        return Error == SvgPathBuilderError.None;
    }

    /// <summary>Clears every command and resets the sticky error.</summary>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Clear()
    {
        commands.Clear();
        ResetState();
        return this;
    }

    /// <summary>Clears the sticky error without changing path geometry.</summary>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder ClearError()
    {
        Error = SvgPathBuilderError.None;
        Message = "";
        TransformError = SvgPathTransformError.None;
        return this;
    }

    /// <summary>Adds an absolute move command.</summary>
    /// <param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder MoveTo(double x, double y) { return Add(SvgPathCommandKind.MoveTo, false, new double[] { x, y }); }

    /// <summary>Adds a relative move command.</summary>
    /// <param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder MoveBy(double x, double y) { return Add(SvgPathCommandKind.MoveTo, true, new double[] { x, y }); }

    /// <summary>Adds an absolute line command.</summary>
    /// <param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder LineTo(double x, double y) { return Add(SvgPathCommandKind.LineTo, false, new double[] { x, y }); }

    /// <summary>Adds a relative line command.</summary>
    /// <param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder LineBy(double x, double y) { return Add(SvgPathCommandKind.LineTo, true, new double[] { x, y }); }

    /// <summary>Adds an absolute horizontal line command.</summary>
    /// <param name="x">The destination x coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder HorizontalTo(double x) { return Add(SvgPathCommandKind.HorizontalLineTo, false, new double[] { x }); }

    /// <summary>Adds a relative horizontal line command.</summary>
    /// <param name="x">The destination x delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder HorizontalBy(double x) { return Add(SvgPathCommandKind.HorizontalLineTo, true, new double[] { x }); }

    /// <summary>Adds an absolute vertical line command.</summary>
    /// <param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder VerticalTo(double y) { return Add(SvgPathCommandKind.VerticalLineTo, false, new double[] { y }); }

    /// <summary>Adds a relative vertical line command.</summary>
    /// <param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder VerticalBy(double y) { return Add(SvgPathCommandKind.VerticalLineTo, true, new double[] { y }); }

    /// <summary>Adds an absolute cubic Bézier command.</summary>
    /// <param name="control1X">The first control x coordinate.</param><param name="control1Y">The first control y coordinate.</param><param name="control2X">The second control x coordinate.</param><param name="control2Y">The second control y coordinate.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder CubicTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y) { return Add(SvgPathCommandKind.CubicCurveTo, false, new double[] { control1X, control1Y, control2X, control2Y, x, y }); }

    /// <summary>Adds a relative cubic Bézier command.</summary>
    /// <param name="control1X">The first control x delta.</param><param name="control1Y">The first control y delta.</param><param name="control2X">The second control x delta.</param><param name="control2Y">The second control y delta.</param><param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder CubicBy(double control1X, double control1Y, double control2X, double control2Y, double x, double y) { return Add(SvgPathCommandKind.CubicCurveTo, true, new double[] { control1X, control1Y, control2X, control2Y, x, y }); }

    /// <summary>Adds an absolute smooth cubic Bézier command.</summary>
    /// <param name="control2X">The second control x coordinate.</param><param name="control2Y">The second control y coordinate.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder SmoothCubicTo(double control2X, double control2Y, double x, double y) { return Add(SvgPathCommandKind.SmoothCubicCurveTo, false, new double[] { control2X, control2Y, x, y }); }

    /// <summary>Adds a relative smooth cubic Bézier command.</summary>
    /// <param name="control2X">The second control x delta.</param><param name="control2Y">The second control y delta.</param><param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder SmoothCubicBy(double control2X, double control2Y, double x, double y) { return Add(SvgPathCommandKind.SmoothCubicCurveTo, true, new double[] { control2X, control2Y, x, y }); }

    /// <summary>Adds an absolute quadratic Bézier command.</summary>
    /// <param name="controlX">The control x coordinate.</param><param name="controlY">The control y coordinate.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder QuadraticTo(double controlX, double controlY, double x, double y) { return Add(SvgPathCommandKind.QuadraticCurveTo, false, new double[] { controlX, controlY, x, y }); }

    /// <summary>Adds a relative quadratic Bézier command.</summary>
    /// <param name="controlX">The control x delta.</param><param name="controlY">The control y delta.</param><param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder QuadraticBy(double controlX, double controlY, double x, double y) { return Add(SvgPathCommandKind.QuadraticCurveTo, true, new double[] { controlX, controlY, x, y }); }

    /// <summary>Adds an absolute smooth quadratic Bézier command.</summary>
    /// <param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder SmoothQuadraticTo(double x, double y) { return Add(SvgPathCommandKind.SmoothQuadraticCurveTo, false, new double[] { x, y }); }

    /// <summary>Adds a relative smooth quadratic Bézier command.</summary>
    /// <param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder SmoothQuadraticBy(double x, double y) { return Add(SvgPathCommandKind.SmoothQuadraticCurveTo, true, new double[] { x, y }); }

    /// <summary>Adds an absolute endpoint-parameterized elliptical arc.</summary>
    /// <param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param><param name="rotation">The x-axis rotation in degrees.</param><param name="largeArc">Whether the large arc is selected.</param><param name="sweep">Whether the positive-angle sweep is selected.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder ArcTo(double radiusX, double radiusY, double rotation, bool largeArc, bool sweep, double x, double y) { return Add(SvgPathCommandKind.ArcTo, false, ArcValues(radiusX, radiusY, rotation, largeArc, sweep, x, y)); }

    /// <summary>Adds a relative endpoint-parameterized elliptical arc.</summary>
    /// <param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param><param name="rotation">The x-axis rotation in degrees.</param><param name="largeArc">Whether the large arc is selected.</param><param name="sweep">Whether the positive-angle sweep is selected.</param><param name="x">The destination x delta.</param><param name="y">The destination y delta.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder ArcBy(double radiusX, double radiusY, double rotation, bool largeArc, bool sweep, double x, double y) { return Add(SvgPathCommandKind.ArcTo, true, ArcValues(radiusX, radiusY, rotation, largeArc, sweep, x, y)); }

    /// <summary>Closes the active subpath.</summary>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Close() { return Add(SvgPathCommandKind.ClosePath, false, new double[0]); }

    /// <summary>Adds a standalone line shape as a new subpath.</summary>
    /// <param name="x1">The starting x coordinate.</param><param name="y1">The starting y coordinate.</param><param name="x2">The ending x coordinate.</param><param name="y2">The ending y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Line(double x1, double y1, double x2, double y2)
    {
        if (!PrepareShape(2, new double[] { x1, y1, x2, y2 })) return this;
        MoveTo(x1, y1);
        LineTo(x2, y2);
        return this;
    }

    /// <summary>Adds a standalone axis-aligned rectangle.</summary>
    /// <param name="x">The left coordinate.</param><param name="y">The top coordinate.</param><param name="width">The nonnegative width.</param><param name="height">The nonnegative height.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Rectangle(double x, double y, double width, double height)
    {
        return RoundedRectangle(x, y, width, height, 0, 0);
    }

    /// <summary>Adds a standalone rounded axis-aligned rectangle.</summary>
    /// <param name="x">The left coordinate.</param><param name="y">The top coordinate.</param><param name="width">The nonnegative width.</param><param name="height">The nonnegative height.</param><param name="radiusX">The nonnegative corner x radius.</param><param name="radiusY">The nonnegative corner y radius.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder RoundedRectangle(double x, double y, double width, double height, double radiusX, double radiusY)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (!FiniteValues(new double[] { x, y, width, height, radiusX, radiusY }) || width < 0 || height < 0 || radiusX < 0 || radiusY < 0) return Fail(SvgPathBuilderError.InvalidArgument, "Rectangle coordinates and nonnegative dimensions are required.");
        if (width == 0 || height == 0) return this;
        SvgFiniteResult right = SvgFiniteMath.Add(x, width);
        SvgFiniteResult bottom = SvgFiniteMath.Add(y, height);
        if (!right.Success || !bottom.Success) return Fail(SvgPathBuilderError.NonFiniteResult, "Rectangle coordinate arithmetic is not finite.");
        double usedRadiusX = System.Math.Min(radiusX, width / 2);
        double usedRadiusY = System.Math.Min(radiusY, height / 2);
        if (usedRadiusX == 0 || usedRadiusY == 0)
        {
            if (!CanAdd(5)) return this;
            MoveTo(x, y); LineTo(right.Value, y); LineTo(right.Value, bottom.Value); LineTo(x, bottom.Value); Close();
            return this;
        }
        if (!CanAdd(10)) return this;
        double leftArc = x + usedRadiusX;
        double rightArc = right.Value - usedRadiusX;
        double topArc = y + usedRadiusY;
        double bottomArc = bottom.Value - usedRadiusY;
        MoveTo(leftArc, y); LineTo(rightArc, y); ArcTo(usedRadiusX, usedRadiusY, 0, false, true, right.Value, topArc); LineTo(right.Value, bottomArc); ArcTo(usedRadiusX, usedRadiusY, 0, false, true, rightArc, bottom.Value); LineTo(leftArc, bottom.Value); ArcTo(usedRadiusX, usedRadiusY, 0, false, true, x, bottomArc); LineTo(x, topArc); ArcTo(usedRadiusX, usedRadiusY, 0, false, true, leftArc, y); Close();
        return this;
    }

    /// <summary>Adds a standalone circle.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radius">The nonnegative radius.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Circle(double centerX, double centerY, double radius) { return Ellipse(centerX, centerY, radius, radius); }

    /// <summary>Adds a standalone axis-aligned ellipse.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Ellipse(double centerX, double centerY, double radiusX, double radiusY)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (!FiniteValues(new double[] { centerX, centerY, radiusX, radiusY }) || radiusX < 0 || radiusY < 0) return Fail(SvgPathBuilderError.InvalidArgument, "Ellipse coordinates and nonnegative radii are required.");
        if (radiusX == 0 || radiusY == 0) return this;
        SvgFiniteResult right = SvgFiniteMath.Add(centerX, radiusX);
        SvgFiniteResult left = SvgFiniteMath.Subtract(centerX, radiusX);
        if (!right.Success || !left.Success) return Fail(SvgPathBuilderError.NonFiniteResult, "Ellipse coordinate arithmetic is not finite.");
        if (!CanAdd(4)) return this;
        MoveTo(right.Value, centerY); ArcTo(radiusX, radiusY, 0, false, true, left.Value, centerY); ArcTo(radiusX, radiusY, 0, false, true, right.Value, centerY); Close();
        return this;
    }

    /// <summary>Adds a standalone open polyline from interleaved coordinates.</summary>
    /// <param name="coordinates">The x and y coordinate pairs.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Polyline(double[] coordinates) { return PointShape(coordinates, false); }

    /// <summary>Adds a standalone closed polygon from interleaved coordinates.</summary>
    /// <param name="coordinates">The x and y coordinate pairs.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Polygon(double[] coordinates) { return PointShape(coordinates, true); }

    /// <summary>Adds a standalone circular center arc using degree angles.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radius">The nonnegative radius.</param><param name="startAngle">The starting angle in degrees.</param><param name="sweepAngle">The signed sweep angle in degrees.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Arc(double centerX, double centerY, double radius, double startAngle, double sweepAngle)
    {
        return EllipseArc(centerX, centerY, radius, radius, 0, startAngle, sweepAngle);
    }

    /// <summary>Adds a standalone rotated elliptical center arc using degree angles.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param><param name="rotation">The ellipse-axis rotation in degrees.</param><param name="startAngle">The starting angle in degrees.</param><param name="sweepAngle">The signed sweep angle in degrees.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder EllipseArc(double centerX, double centerY, double radiusX, double radiusY, double rotation, double startAngle, double sweepAngle)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (!FiniteValues(new double[] { centerX, centerY, radiusX, radiusY, rotation, startAngle, sweepAngle }) || radiusX < 0 || radiusY < 0) return Fail(SvgPathBuilderError.InvalidArgument, "Center-arc coordinates, angles, and nonnegative radii are required.");
        if (radiusX == 0 || radiusY == 0) return this;
        double segmentValue = System.Math.Ceiling(System.Math.Abs(sweepAngle) / 180);
        if (segmentValue > int.MaxValue - 1) return Fail(SvgPathBuilderError.CommandLimitExceeded, "The center arc requires too many commands.");
        int segments = (int)segmentValue;
        int needed = segments + 1;
        if (!CanAdd(needed)) return this;
        SvgFiniteResult finalAngle = SvgFiniteMath.Add(startAngle, sweepAngle);
        if (!finalAngle.Success) return Fail(SvgPathBuilderError.NonFiniteResult, "Center-arc angle arithmetic is not finite.");
        SvgPoint? start = EllipsePoint(centerX, centerY, radiusX, radiusY, rotation, startAngle);
        if (start == null) return Fail(SvgPathBuilderError.NonFiniteResult, "Center-arc coordinate arithmetic is not finite.");
        System.Collections.Generic.List<SvgPoint> points = new System.Collections.Generic.List<SvgPoint>();
        points.Add(start);
        if (segments == 0)
        {
            MoveTo(start.X, start.Y);
            return this;
        }
        double segmentSweep = sweepAngle / segments;
        int index = 1;
        while (index <= segments)
        {
            SvgFiniteResult amount = SvgFiniteMath.Multiply(segmentSweep, index);
            SvgFiniteResult angleResult = index == segments ? finalAngle : amount.Success ? SvgFiniteMath.Add(startAngle, amount.Value) : amount;
            if (!angleResult.Success) return Fail(SvgPathBuilderError.NonFiniteResult, "Center-arc angle arithmetic is not finite.");
            double angle = angleResult.Value;
            SvgPoint? end = EllipsePoint(centerX, centerY, radiusX, radiusY, rotation, angle);
            if (end == null) return Fail(SvgPathBuilderError.NonFiniteResult, "Center-arc coordinate arithmetic is not finite.");
            points.Add(end);
            index = index + 1;
        }
        MoveTo(points[0].X, points[0].Y);
        index = 1;
        while (index < points.Count)
        {
            SvgPoint end = points[index];
            ArcTo(radiusX, radiusY, rotation, System.Math.Abs(segmentSweep) > 180, sweepAngle > 0, end.X, end.Y);
            index = index + 1;
        }
        return this;
    }

    /// <summary>Appends validated source geometry as absolute commands.</summary>
    /// <param name="path">The valid path to append as independent subpaths.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Append(SvgPathData? path)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (path == null) return Fail(SvgPathBuilderError.InvalidArgument, "A path is required.");
        SvgPathNormalizeOptions options = new SvgPathNormalizeOptions();
        options.MaximumCommands = int.MaxValue;
        SvgPathNormalizeResult normalized = path.NormalizeWithOptions(options);
        if (!normalized.Success) return Fail(SvgPathBuilderError.InvalidPath, "The appended path is invalid.");
        if (!CanAdd((normalized.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)) return this;
        AppendNormalized(normalized.PathData);
        return this;
    }

    /// <summary>Appends another builder's validated geometry as absolute commands.</summary>
    /// <param name="builder">The source builder.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder AppendBuilder(SvgPathBuilder? builder)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (builder == null) return Fail(SvgPathBuilderError.InvalidArgument, "A path builder is required.");
        SvgPathBuildResult built = builder.Build();
        if (!built.Success) return Fail(SvgPathBuilderError.InvalidPath, "The appended path builder is invalid.");
        return Append(built.PathData);
    }

    /// <summary>Reverses each subpath while retaining subpath order and geometric command types.</summary>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Reverse()
    {
        if (Error != SvgPathBuilderError.None) return this;
        SvgPathBuildResult built = Build();
        if (!built.Success) return Fail(built.Error, built.Message);
        SvgPathNormalizeResult normalized = (built.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Normalize();
        if (!normalized.Success) return Fail(SvgPathBuilderError.InvalidPath, "The path cannot be normalized for reversal.");
        SvgPathBuilder reversed = SvgPathBuilder.Create(MaximumCommands);
        int start = 0;
        while (start < (normalized.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            int end = start + 1;
            while (end < normalized.PathData.Commands.Count && normalized.PathData.Commands[end].Kind != SvgPathCommandKind.MoveTo) end = end + 1;
            ReverseSubpath(normalized.PathData, start, end, reversed);
            if (!reversed.Succeeded()) return Fail(reversed.Error, reversed.Message);
            start = end;
        }
        ReplaceWith(reversed);
        return this;
    }

    /// <summary>Applies a finite affine transform using deterministic defaults.</summary>
    /// <param name="matrix">The affine transform.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder Transform(SvgMatrix matrix)
    {
        return TransformWithOptions(matrix, new SvgPathTransformOptions());
    }

    /// <summary>Applies a finite affine transform with explicit options.</summary>
    /// <param name="matrix">The affine transform.</param><param name="options">The arc-preservation and command-limit options.</param>
    /// <returns>This builder.</returns>
    internal SvgPathBuilder TransformWithOptions(SvgMatrix matrix, SvgPathTransformOptions? options)
    {
        if (Error != SvgPathBuilderError.None) return this;
        SvgPathBuildResult built = Build();
        if (!built.Success) return Fail(built.Error, built.Message);
        if (options == null) return Fail(SvgPathBuilderError.InvalidArgument, "Path transform options are required.");
        SvgPathTransformOptions copy = new SvgPathTransformOptions();
        copy.PreserveArcs = options.PreserveArcs;
        copy.MaximumCommands = options.MaximumCommands < MaximumCommands ? options.MaximumCommands : MaximumCommands;
        SvgPathTransformResult transformed = (built.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformWithOptions(matrix, copy);
        if (!transformed.Success)
        {
            TransformError = transformed.Error;
            return Fail(SvgPathBuilderError.TransformFailed, "The affine path transformation failed.");
        }
        SvgPathBuilder replacement = SvgPathBuilder.Create(MaximumCommands);
        replacement.AppendNormalized(transformed.PathData);
        ReplaceWith(replacement);
        return this;
    }

    /// <summary>Validates current settings and command bookkeeping.</summary>
    /// <returns>An independent valid path snapshot or a stable failure.</returns>
    internal SvgPathBuildResult Validate()
    {
        if (Error != SvgPathBuilderError.None) return Failure(Error, Message);
        if (MaximumCommands < 1) return Failure(SvgPathBuilderError.InvalidArgument, "MaximumCommands must be positive.");
        if (commands.Count > MaximumCommands) return Failure(SvgPathBuilderError.CommandLimitExceeded, "The path exceeds MaximumCommands.");
        SvgPathData snapshot = Snapshot();
        SvgPathNormalizeOptions options = new SvgPathNormalizeOptions();
        options.MaximumCommands = MaximumCommands;
        SvgPathNormalizeResult normalized = snapshot.NormalizeWithOptions(options);
        if (!normalized.Success) return Failure(SvgPathBuilderError.InvalidPath, "The path command sequence is invalid.");
        return Success(snapshot, null);
    }

    /// <summary>Builds an independent validated path-data snapshot.</summary>
    /// <returns>The generated path data or a stable failure.</returns>
    internal SvgPathBuildResult Build()
    {
        return Validate();
    }

    /// <summary>Returns an independent validated path-data snapshot, or <c>null</c> after failure.</summary>
    /// <returns>The generated path data, or <c>null</c>.</returns>
    internal SvgPathData? ToPathData()
    {
        return Build().PathData;
    }

    /// <summary>Creates a detached SVG path element with minified path data.</summary>
    /// <returns>The path data and detached element or a stable failure.</returns>
    internal SvgPathBuildResult ToPathElement()
    {
        SvgPathBuildResult built = Build();
        if (!built.Success) return built;
        SvgPathFormatResult formatted = new SvgPathFormatter().FormatMinifiedWithPath((built.PathData ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!formatted.Success) return Failure(SvgPathBuilderError.FormatFailed, "Path-data formatting failed.");
        SvgDocumentApi documents = new SvgDocumentApi();
        SvgNode element = documents.CreateElement(SvgElementNames.Path, "", SvgElementNames.Path, SvgNamespaces.Svg);
        SvgAttribute data = documents.CreateAttribute(SvgAttributeNames.D, "", SvgAttributeNames.D, "", formatted.Text);
        SvgMutationResult mutation = new SvgNodeApi().AppendAttribute(element, data);
        if (!mutation.Success) return Failure(SvgPathBuilderError.MutationFailed, mutation.Message);
        return Success(built.PathData, element);
    }

    /// <summary>Creates and appends a path element to a document builder's current element.</summary>
    /// <param name="builder">The destination document builder.</param>
    /// <returns>The path data and attached element or a stable failure.</returns>
    internal SvgPathBuildResult AppendTo(SvgBuilder? builder)
    {
        if (builder == null) return Failure(SvgPathBuilderError.InvalidArgument, "A destination document builder is required.");
        SvgPathBuildResult built = ToPathElement();
        if (!built.Success) return built;
        builder.Append(built.Element);
        if (!builder.Succeeded()) return Failure(SvgPathBuilderError.MutationFailed, builder.Message);
        return built;
    }

    internal SvgPathBuilder Add(SvgPathCommandKind kind, bool relative, double[] values)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (MaximumCommands < 1) return Fail(SvgPathBuilderError.InvalidArgument, "MaximumCommands must be positive.");
        if (!SvgPathCommandRules.IsKnown(kind) || values == null || values.Length != SvgPathCommandRules.ParameterCount(kind)) return Fail(SvgPathBuilderError.InvalidArgument, "The path command payload is invalid.");
        if (!FiniteValues(values)) return Fail(SvgPathBuilderError.InvalidArgument, "Path command values must be finite.");
        if (commands.Count == 0 && kind != SvgPathCommandKind.MoveTo) return Fail(SvgPathBuilderError.ExpectedMoveTo, "The first path command must be a move command.");
        if (kind != SvgPathCommandKind.MoveTo && !hasCurrent) return Fail(SvgPathBuilderError.MissingCurrentPoint, "The path command requires a current point.");
        if (kind == SvgPathCommandKind.ArcTo && (values[0] < 0 || values[1] < 0)) return Fail(SvgPathBuilderError.InvalidArgument, "Arc radii must be nonnegative.");
        if (!CanAdd(1)) return this;
        SvgPathEndpoint endpoint = Endpoint(kind, relative, values);
        if (!endpoint.Success) return Fail(SvgPathBuilderError.NonFiniteResult, "Relative path coordinate arithmetic is not finite.");
        if (!ControlsAreFinite(kind, relative, values)) return Fail(SvgPathBuilderError.NonFiniteResult, "Relative control-point arithmetic is not finite.");
        int commandSubpath = subpathIndex;
        double commandSubpathX = subpathX;
        double commandSubpathY = subpathY;
        if (kind == SvgPathCommandKind.MoveTo)
        {
            commandSubpath = subpathIndex + 1;
            commandSubpathX = endpoint.X;
            commandSubpathY = endpoint.Y;
        }
        if (kind == SvgPathCommandKind.ClosePath)
        {
            endpoint.X = subpathX;
            endpoint.Y = subpathY;
        }
        int offset = commands.Count;
        SvgPathCommand command = new SvgPathCommand(kind, relative, values, offset, offset + 1, true, currentX, currentY, endpoint.X, endpoint.Y, commandSubpathX, commandSubpathY, commandSubpath);
        commands.Add(command);
        currentX = endpoint.X;
        currentY = endpoint.Y;
        hasCurrent = true;
        if (kind == SvgPathCommandKind.MoveTo)
        {
            subpathIndex = commandSubpath;
            subpathX = endpoint.X;
            subpathY = endpoint.Y;
        }
        return this;
    }

    internal SvgPathBuilder PointShape(double[] coordinates, bool close)
    {
        if (Error != SvgPathBuilderError.None) return this;
        if (coordinates == null || coordinates.Length % 2 != 0 || !FiniteValues(coordinates)) return Fail(SvgPathBuilderError.InvalidArgument, "An even finite coordinate array is required.");
        if (coordinates.Length == 0) return this;
        int pointCount = coordinates.Length / 2;
        int needed = pointCount + (close ? 1 : 0);
        if (!CanAdd(needed)) return this;
        MoveTo(coordinates[0], coordinates[1]);
        int index = 1;
        while (index < pointCount)
        {
            LineTo(coordinates[index * 2], coordinates[index * 2 + 1]);
            index = index + 1;
        }
        if (close) Close();
        return this;
    }

    internal bool PrepareShape(int count, double[] values)
    {
        if (Error != SvgPathBuilderError.None) return false;
        if (!FiniteValues(values))
        {
            Fail(SvgPathBuilderError.InvalidArgument, "Shape coordinates must be finite.");
            return false;
        }
        return CanAdd(count);
    }

    internal bool CanAdd(int count)
    {
        if (count < 0 || MaximumCommands < 1)
        {
            Fail(SvgPathBuilderError.InvalidArgument, "MaximumCommands must be positive.");
            return false;
        }
        if ((long)commands.Count + count > MaximumCommands)
        {
            Fail(SvgPathBuilderError.CommandLimitExceeded, "The path exceeds MaximumCommands.");
            return false;
        }
        return true;
    }

    internal SvgPathEndpoint Endpoint(SvgPathCommandKind kind, bool relative, double[] values)
    {
        if (kind == SvgPathCommandKind.ClosePath) return new SvgPathEndpoint(true, subpathX, subpathY);
        if (kind == SvgPathCommandKind.HorizontalLineTo)
        {
            SvgFiniteResult x = Resolve(values[0], currentX, relative);
            return new SvgPathEndpoint(x.Success, x.Value, currentY);
        }
        if (kind == SvgPathCommandKind.VerticalLineTo)
        {
            SvgFiniteResult y = Resolve(values[0], currentY, relative);
            return new SvgPathEndpoint(y.Success, currentX, y.Value);
        }
        int xIndex = values.Length - 2;
        int yIndex = values.Length - 1;
        SvgFiniteResult resolvedX = Resolve(values[xIndex], currentX, relative);
        SvgFiniteResult resolvedY = Resolve(values[yIndex], currentY, relative);
        return new SvgPathEndpoint(resolvedX.Success && resolvedY.Success, resolvedX.Value, resolvedY.Value);
    }

    internal bool ControlsAreFinite(SvgPathCommandKind kind, bool relative, double[] values)
    {
        if (!relative) return true;
        int count = 0;
        if (kind == SvgPathCommandKind.CubicCurveTo) count = 4;
        else if (kind == SvgPathCommandKind.SmoothCubicCurveTo || kind == SvgPathCommandKind.QuadraticCurveTo) count = 2;
        int index = 0;
        while (index < count)
        {
            double origin = index % 2 == 0 ? currentX : currentY;
            if (!SvgFiniteMath.Add(origin, values[index]).Success) return false;
            index = index + 1;
        }
        return true;
    }

    internal void AppendNormalized(SvgPathData? path)
    {
        int index = 0;
        while (index < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count && Error == SvgPathBuilderError.None)
        {
            SvgPathCommand command = path.Commands[index];
            Add(command.Kind, false, (double[])command.Values.Clone());
            index = index + 1;
        }
    }

    internal static void ReverseSubpath(SvgPathData path, int start, int end, SvgPathBuilder target)
    {
        SvgPathCommand move = path.Commands[start];
        bool closed = end > start + 1 && path.Commands[end - 1].Kind == SvgPathCommandKind.ClosePath;
        int last = closed ? end - 2 : end - 1;
        if (last <= start)
        {
            target.MoveTo(move.EndX, move.EndY);
            if (closed) target.Close();
            return;
        }
        SvgPathCommand final = path.Commands[last];
        target.MoveTo(final.EndX, final.EndY);
        int index = last;
        while (index > start)
        {
            ReverseCommand(path.Commands[index], target);
            index = index - 1;
        }
        if (closed) target.Close();
    }

    internal static void ReverseCommand(SvgPathCommand command, SvgPathBuilder target)
    {
        if (command.Kind == SvgPathCommandKind.LineTo) target.LineTo(command.StartX, command.StartY);
        else if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) target.QuadraticTo(command.Values[0], command.Values[1], command.StartX, command.StartY);
        else if (command.Kind == SvgPathCommandKind.CubicCurveTo) target.CubicTo(command.Values[2], command.Values[3], command.Values[0], command.Values[1], command.StartX, command.StartY);
        else if (command.Kind == SvgPathCommandKind.ArcTo) target.ArcTo(command.Values[0], command.Values[1], command.Values[2], command.Values[3] == 1, command.Values[4] != 1, command.StartX, command.StartY);
    }

    internal void ReplaceWith(SvgPathBuilder replacement)
    {
        commands = replacement.commands;
        hasCurrent = replacement.hasCurrent;
        currentX = replacement.currentX;
        currentY = replacement.currentY;
        subpathX = replacement.subpathX;
        subpathY = replacement.subpathY;
        subpathIndex = replacement.subpathIndex;
        Error = SvgPathBuilderError.None;
        Message = "";
        TransformError = SvgPathTransformError.None;
    }

    internal SvgPathData Snapshot()
    {
        System.Collections.Generic.List<SvgPathCommand> copy = new System.Collections.Generic.List<SvgPathCommand>();
        int index = 0;
        while (index < commands.Count)
        {
            SvgPathCommand command = commands[index];
            copy.Add(new SvgPathCommand(command.Kind, command.Relative, command.Values, command.SourceOffset, command.EndOffset, command.Explicit, command.StartX, command.StartY, command.EndX, command.EndY, command.SubpathStartX, command.SubpathStartY, command.SubpathIndex));
            index = index + 1;
        }
        return new SvgPathData(copy, "", new System.Collections.Generic.List<SvgPathParseIssue>());
    }

    internal void ResetState()
    {
        hasCurrent = false;
        currentX = 0;
        currentY = 0;
        subpathX = 0;
        subpathY = 0;
        subpathIndex = -1;
        Error = SvgPathBuilderError.None;
        Message = "";
        TransformError = SvgPathTransformError.None;
    }

    internal SvgPathBuilder Fail(SvgPathBuilderError error, string? message)
    {
        if (Error == SvgPathBuilderError.None)
        {
            Error = error;
            Message = message;
        }
        return this;
    }

    internal static SvgPathBuildResult Success(SvgPathData? path, SvgNode? element)
    {
        return new SvgPathBuildResult(true, SvgPathBuilderError.None, path, element, "");
    }

    internal static SvgPathBuildResult Failure(SvgPathBuilderError error, string? message)
    {
        return new SvgPathBuildResult(false, error, null, null, message);
    }

    internal static SvgFiniteResult Resolve(double value, double origin, bool relative)
    {
        return relative ? SvgFiniteMath.Add(origin, value) : new SvgFiniteResult(true, SvgFiniteMath.NormalizeZero(value), "");
    }

    internal static bool FiniteValues(double[]? values)
    {
        if (values == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (!SvgFiniteMath.IsFinite(values[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static double[] ArcValues(double radiusX, double radiusY, double rotation, bool largeArc, bool sweep, double x, double y)
    {
        return new double[] { radiusX, radiusY, rotation, largeArc ? 1 : 0, sweep ? 1 : 0, x, y };
    }

    internal static SvgPoint? EllipsePoint(double centerX, double centerY, double radiusX, double radiusY, double rotation, double angle)
    {
        double radians = angle * System.Math.PI / 180;
        double rotationRadians = rotation * System.Math.PI / 180;
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        double rotationCosine = System.Math.Cos(rotationRadians);
        double rotationSine = System.Math.Sin(rotationRadians);
        double x = centerX + radiusX * cosine * rotationCosine - radiusY * sine * rotationSine;
        double y = centerY + radiusX * cosine * rotationSine + radiusY * sine * rotationCosine;
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y)) return null;
        return new SvgPoint(x, y);
    }
}
