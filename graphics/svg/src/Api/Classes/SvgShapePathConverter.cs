namespace svg;

/// <summary>Converts resolved SVG basic shapes to finite absolute path commands.</summary>
public class SvgShapePathConverter : ISvgShapePathConverter
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgShapePathConverter()
    {
    }

    /// <summary>Converts a resolved basic shape without an additional command restriction.</summary>
    /// <param name="shape">The resolved line, rectangle, circle, ellipse, polyline, or polygon.</param>
    /// <returns>The generated path or a stable conversion failure.</returns>
    public SvgShapePathResult ConvertWithShape(SvgShapeResult shape)
    {
        return new SvgShapePathConverter().ConvertWithShapeAndMaximumCommands(shape, int.MaxValue);
    }

    /// <summary>Converts a resolved basic shape within a positive command limit.</summary>
    /// <param name="shape">The resolved line, rectangle, circle, ellipse, polyline, or polygon.</param>
    /// <param name="maximumCommands">The positive maximum generated command count.</param>
    /// <returns>The generated path or a stable conversion failure.</returns>
    public SvgShapePathResult ConvertWithShapeAndMaximumCommands(SvgShapeResult? shape, int maximumCommands)
    {
        if (shape == null || maximumCommands < 1) return new SvgShapePathConverter().Failure(SvgShapePathError.InvalidArgument);
        if (shape.Kind == SvgShapeKind.Line) return new SvgShapePathConverter().ConvertLine(shape, maximumCommands);
        if (shape.Kind == SvgShapeKind.Rectangle) return new SvgShapePathConverter().ConvertRectangle(shape, maximumCommands);
        if (shape.Kind == SvgShapeKind.Circle) return new SvgShapePathConverter().ConvertEllipse(shape, shape.R, shape.R, maximumCommands);
        if (shape.Kind == SvgShapeKind.Ellipse) return new SvgShapePathConverter().ConvertEllipse(shape, shape.Rx, shape.Ry, maximumCommands);
        if (shape.Kind == SvgShapeKind.Polyline) return new SvgShapePathConverter().ConvertPoints(shape, false, maximumCommands);
        if (shape.Kind == SvgShapeKind.Polygon) return new SvgShapePathConverter().ConvertPoints(shape, true, maximumCommands);
        return new SvgShapePathConverter().Failure(SvgShapePathError.UnsupportedShape);
    }
SvgShapePathResult ConvertLine(SvgShapeResult shape, int maximumCommands)
    {
        if (!new SvgShapePathConverter().Finite(shape.X1) || !new SvgShapePathConverter().Finite(shape.Y1) || !new SvgShapePathConverter().Finite(shape.X2) || !new SvgShapePathConverter().Finite(shape.Y2)) return new SvgShapePathConverter().Failure(SvgShapePathError.InvalidArgument);
        if (maximumCommands < 2) return new SvgShapePathConverter().Failure(SvgShapePathError.CommandLimitExceeded);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        builder.MoveTo(shape.X1, shape.Y1);
        builder.LineTo(shape.X2, shape.Y2);
        return new SvgShapePathConverter().Success(builder.Build());
    }
SvgShapePathResult ConvertRectangle(SvgShapeResult shape, int maximumCommands)
    {
        if (!new SvgShapePathConverter().Finite(shape.X) || !new SvgShapePathConverter().Finite(shape.Y) || !new SvgShapePathConverter().Nonnegative(shape.Width) || !new SvgShapePathConverter().Nonnegative(shape.Height) || !new SvgShapePathConverter().Nonnegative(shape.Rx) || !new SvgShapePathConverter().Nonnegative(shape.Ry)) return new SvgShapePathConverter().Failure(SvgShapePathError.InvalidArgument);
        if (shape.Width == 0 || shape.Height == 0) return new SvgShapePathConverter().Empty();
        SvgFiniteResult right = SvgFiniteMath.Add(shape.X, shape.Width);
        SvgFiniteResult bottom = SvgFiniteMath.Add(shape.Y, shape.Height);
        if (!right.Success || !bottom.Success) return new SvgShapePathConverter().Failure(SvgShapePathError.NonFiniteResult);
        double rx = System.Math.Min(shape.Rx, shape.Width / 2);
        double ry = System.Math.Min(shape.Ry, shape.Height / 2);
        if (rx == 0 || ry == 0) return new SvgShapePathConverter().ConvertPlainRectangle(shape, right.Value, bottom.Value, maximumCommands);
        return new SvgShapePathConverter().ConvertRoundedRectangle(shape, right.Value, bottom.Value, rx, ry, maximumCommands);
    }
SvgShapePathResult ConvertPlainRectangle(SvgShapeResult shape, double right, double bottom, int maximumCommands)
    {
        if (maximumCommands < 5) return new SvgShapePathConverter().Failure(SvgShapePathError.CommandLimitExceeded);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        builder.MoveTo(shape.X, shape.Y);
        builder.LineTo(right, shape.Y);
        builder.LineTo(right, bottom);
        builder.LineTo(shape.X, bottom);
        builder.Close();
        return new SvgShapePathConverter().Success(builder.Build());
    }
SvgShapePathResult ConvertRoundedRectangle(SvgShapeResult shape, double right, double bottom, double rx, double ry, int maximumCommands)
    {
        if (maximumCommands < 10) return new SvgShapePathConverter().Failure(SvgShapePathError.CommandLimitExceeded);
        SvgFiniteResult leftArcEnd = SvgFiniteMath.Add(shape.X, rx);
        SvgFiniteResult rightArcStart = SvgFiniteMath.Subtract(right, rx);
        SvgFiniteResult topArcEnd = SvgFiniteMath.Add(shape.Y, ry);
        SvgFiniteResult bottomArcStart = SvgFiniteMath.Subtract(bottom, ry);
        if (!leftArcEnd.Success || !rightArcStart.Success || !topArcEnd.Success || !bottomArcStart.Success) return new SvgShapePathConverter().Failure(SvgShapePathError.NonFiniteResult);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        builder.MoveTo(leftArcEnd.Value, shape.Y);
        builder.LineTo(rightArcStart.Value, shape.Y);
        builder.ArcTo(rx, ry, right, topArcEnd.Value);
        builder.LineTo(right, bottomArcStart.Value);
        builder.ArcTo(rx, ry, rightArcStart.Value, bottom);
        builder.LineTo(leftArcEnd.Value, bottom);
        builder.ArcTo(rx, ry, shape.X, bottomArcStart.Value);
        builder.LineTo(shape.X, topArcEnd.Value);
        builder.ArcTo(rx, ry, leftArcEnd.Value, shape.Y);
        builder.Close();
        return new SvgShapePathConverter().Success(builder.Build());
    }
SvgShapePathResult ConvertEllipse(SvgShapeResult shape, double rx, double ry, int maximumCommands)
    {
        if (!new SvgShapePathConverter().Finite(shape.Cx) || !new SvgShapePathConverter().Finite(shape.Cy) || !new SvgShapePathConverter().Nonnegative(rx) || !new SvgShapePathConverter().Nonnegative(ry)) return new SvgShapePathConverter().Failure(SvgShapePathError.InvalidArgument);
        if (rx == 0 || ry == 0) return new SvgShapePathConverter().Empty();
        SvgFiniteResult right = SvgFiniteMath.Add(shape.Cx, rx);
        SvgFiniteResult left = SvgFiniteMath.Subtract(shape.Cx, rx);
        SvgFiniteResult bottom = SvgFiniteMath.Add(shape.Cy, ry);
        SvgFiniteResult top = SvgFiniteMath.Subtract(shape.Cy, ry);
        if (!right.Success || !left.Success || !bottom.Success || !top.Success) return new SvgShapePathConverter().Failure(SvgShapePathError.NonFiniteResult);
        if (maximumCommands < 4) return new SvgShapePathConverter().Failure(SvgShapePathError.CommandLimitExceeded);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        builder.MoveTo(right.Value, shape.Cy);
        builder.ArcTo(rx, ry, left.Value, shape.Cy);
        builder.ArcTo(rx, ry, right.Value, shape.Cy);
        builder.Close();
        return new SvgShapePathConverter().Success(builder.Build());
    }
SvgShapePathResult ConvertPoints(SvgShapeResult shape, bool close, int maximumCommands)
    {
        if (shape.Points == null || shape.PointCount < 0 || shape.PointCount > shape.Points.Length / 2 || shape.Points.Length != shape.PointCount * 2) return new SvgShapePathConverter().Failure(SvgShapePathError.InvalidArgument);
        int coordinateIndex = 0;
        while (coordinateIndex < shape.Points.Length)
        {
            if (!new SvgShapePathConverter().Finite(shape.Points[coordinateIndex])) return new SvgShapePathConverter().Failure(SvgShapePathError.InvalidArgument);
            coordinateIndex = coordinateIndex + 1;
        }
        if (shape.PointCount == 0) return new SvgShapePathConverter().Empty();
        long commandCount = shape.PointCount;
        if (close) commandCount = commandCount + 1;
        if (commandCount > maximumCommands) return new SvgShapePathConverter().Failure(SvgShapePathError.CommandLimitExceeded);
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        builder.MoveTo(shape.Points[0], shape.Points[1]);
        int pointIndex = 1;
        while (pointIndex < shape.PointCount)
        {
            int offset = pointIndex * 2;
            builder.LineTo(shape.Points[offset], shape.Points[offset + 1]);
            pointIndex = pointIndex + 1;
        }
        if (close) builder.Close();
        return new SvgShapePathConverter().Success(builder.Build());
    }
bool Finite(double value)
    {
        return SvgFiniteMath.IsFinite(value);
    }
bool Nonnegative(double value)
    {
        return SvgFiniteMath.IsFinite(value) && value >= 0;
    }
SvgShapePathResult Empty()
    {
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder();
        return new SvgShapePathConverter().Success(builder.Build());
    }
SvgShapePathResult Success(SvgPathData path)
    {
        return new SvgShapePathResult(true, SvgShapePathError.None, path);
    }
SvgShapePathResult Failure(SvgShapePathError error)
    {
        return new SvgShapePathResult(false, error, null);
    }
}
