namespace svg;

internal class SvgPreservedArcCalculation
{
    internal bool Eligible;
    internal double RadiusX;
    internal double RadiusY;
    internal double RotationDegrees;
    internal bool LargeArc;
    internal bool Sweep;

    internal SvgPreservedArcCalculation(bool eligible, double radiusX, double radiusY, double rotationDegrees, bool largeArc, bool sweep)
    {
        Eligible = eligible;
        RadiusX = radiusX;
        RadiusY = radiusY;
        RotationDegrees = rotationDegrees;
        LargeArc = largeArc;
        Sweep = sweep;
    }
}

internal class SvgPathTransformState
{
    SvgPathData? source;
    SvgMatrix? matrix;
    SvgPathTransformOptions options;
    SvgGeneratedPathBuilder builder;

    internal SvgPathTransformState(SvgPathData? path, SvgMatrix? transform, SvgPathTransformOptions transformOptions)
    {
        source = path;
        matrix = transform;
        options = transformOptions;
        builder = new SvgGeneratedPathBuilder();
    }

    internal SvgPathTransformResult Run()
    {
        int index = 0;
        while (index < (source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgPathTransformError error = TransformCommand(source.Commands[index]);
            if (error != SvgPathTransformError.None) return new SvgPathTransformer().Failure(error);
            index = index + 1;
        }
        return new SvgPathTransformResult(true, SvgPathTransformError.None, builder.Build());
    }

    SvgPathTransformError TransformCommand(SvgPathCommand command)
    {
        if (command.Kind == SvgPathCommandKind.ClosePath)
        {
            if (!CanAdd(1)) return SvgPathTransformError.CommandLimitExceeded;
            builder.Close();
            return SvgPathTransformError.None;
        }
        if (command.Kind == SvgPathCommandKind.ArcTo) return TransformArc(command);
        if (command.Kind == SvgPathCommandKind.MoveTo || command.Kind == SvgPathCommandKind.LineTo)
        {
            SvgPointTransformResult end = Point(command.EndX, command.EndY);
            if (!end.Success) return Map(end.Error);
            if (!CanAdd(1)) return SvgPathTransformError.CommandLimitExceeded;
            if (command.Kind == SvgPathCommandKind.MoveTo) builder.MoveTo((end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
            else builder.LineTo((end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
            return SvgPathTransformError.None;
        }
        if (command.Kind == SvgPathCommandKind.QuadraticCurveTo)
        {
            SvgPointTransformResult control = Point(command.Values[0], command.Values[1]);
            SvgPointTransformResult end = Point(command.EndX, command.EndY);
            if (!control.Success || !end.Success) return TransformFailure(control, end);
            if (!CanAdd(1)) return SvgPathTransformError.CommandLimitExceeded;
            builder.QuadraticTo((control.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, control.Point.Y, (end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
            return SvgPathTransformError.None;
        }
        if (command.Kind == SvgPathCommandKind.CubicCurveTo)
        {
            SvgPointTransformResult control1 = Point(command.Values[0], command.Values[1]);
            SvgPointTransformResult control2 = Point(command.Values[2], command.Values[3]);
            SvgPointTransformResult end = Point(command.EndX, command.EndY);
            if (!control1.Success || !control2.Success || !end.Success) return TransformFailure(control1, control2, end);
            if (!CanAdd(1)) return SvgPathTransformError.CommandLimitExceeded;
            builder.CubicTo((control1.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, control1.Point.Y, (control2.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, control2.Point.Y, (end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
            return SvgPathTransformError.None;
        }
        return SvgPathTransformError.InvalidPath;
    }

    SvgPathTransformError TransformArc(SvgPathCommand command)
    {
        SvgArcOperationResult converted = new SvgArcGeometry().FromCommand(command);
        if (!converted.Success) return Map(converted.Error);
        SvgPointTransformResult end = Point(command.EndX, command.EndY);
        if (!end.Success) return Map(end.Error);
        if ((converted.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsPoint) return SvgPathTransformError.None;
        if (converted.Arc.IsLine)
        {
            if (!CanAdd(1)) return SvgPathTransformError.CommandLimitExceeded;
            builder.LineTo((end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
            return SvgPathTransformError.None;
        }
        if (options.PreserveArcs)
        {
            SvgPreservedArcCalculation preserved = Preserve(converted.Arc, command);
            if (preserved.Eligible)
            {
                if (!CanAdd(1)) return SvgPathTransformError.CommandLimitExceeded;
                builder.ArcToWithParameters(preserved.RadiusX, preserved.RadiusY, preserved.RotationDegrees, preserved.LargeArc, preserved.Sweep, (end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
                return SvgPathTransformError.None;
            }
        }
        return TransformArcCubics(converted.Arc);
    }

    SvgPathTransformError TransformArcCubics(SvgArc arc)
    {
        SvgArcCubicResult cubics = arc.ToCubics();
        if (!cubics.Success) return Map(cubics.Error);
        if (!CanAdd((cubics.Curves ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)) return SvgPathTransformError.CommandLimitExceeded;
        int index = 0;
        while (index < cubics.Curves.Count)
        {
            SvgCubicBezier cubic = cubics.Curves[index];
            SvgPointTransformResult control1 = Point(cubic.Control1.X, cubic.Control1.Y);
            SvgPointTransformResult control2 = Point(cubic.Control2.X, cubic.Control2.Y);
            SvgPointTransformResult end = Point(cubic.End.X, cubic.End.Y);
            if (!control1.Success || !control2.Success || !end.Success) return TransformFailure(control1, control2, end);
            builder.CubicTo((control1.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, control1.Point.Y, (control2.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, control2.Point.Y, (end.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, end.Point.Y);
            index = index + 1;
        }
        return SvgPathTransformError.None;
    }

    SvgPreservedArcCalculation Preserve(SvgArc arc, SvgPathCommand command)
    {
        double cosine = System.Math.Cos(arc.Rotation);
        double sine = System.Math.Sin(arc.Rotation);
        double axisXX = arc.RadiusX * cosine;
        double axisXY = arc.RadiusX * sine;
        double axisYX = -arc.RadiusY * sine;
        double axisYY = arc.RadiusY * cosine;
        if (!SvgFiniteMath.IsFinite(axisXX) || !SvgFiniteMath.IsFinite(axisXY) || !SvgFiniteMath.IsFinite(axisYX) || !SvgFiniteMath.IsFinite(axisYY)) return NotPreserved();
        SvgVectorTransformResult first = (matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformVector(new SvgVector(axisXX, axisXY));
        SvgVectorTransformResult second = matrix.TransformVector(new SvgVector(axisYX, axisYY));
        if (!first.Success || !second.Success) return NotPreserved();
        SvgScalarOperationResult firstLength = (first.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length();
        SvgScalarOperationResult secondLength = (second.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length();
        if (!firstLength.Success || !secondLength.Success || firstLength.Value == 0 || secondLength.Value == 0) return NotPreserved();
        double firstX = first.Vector.X / firstLength.Value;
        double firstY = first.Vector.Y / firstLength.Value;
        double secondX = second.Vector.X / secondLength.Value;
        double secondY = second.Vector.Y / secondLength.Value;
        double dot = firstX * secondX + firstY * secondY;
        if (!SvgFiniteMath.IsFinite(dot) || System.Math.Abs(dot) > 1e-12) return NotPreserved();
        double orientation = firstX * secondY - firstY * secondX;
        if (!SvgFiniteMath.IsFinite(orientation) || System.Math.Abs(orientation) < 1e-12) return NotPreserved();
        double transformedSweep = orientation > 0 ? arc.SweepAngle : -arc.SweepAngle;
        double absoluteSweep = System.Math.Abs(transformedSweep);
        bool largeArc = System.Math.Abs(absoluteSweep - System.Math.PI) <= 1e-12 ? command.Values[3] == 1 : absoluteSweep > System.Math.PI;
        bool sweep = transformedSweep > 0;
        double rotationDegrees = System.Math.Atan2(first.Vector.Y, first.Vector.X) * 180 / System.Math.PI;
        if (!SvgFiniteMath.IsFinite(rotationDegrees)) return NotPreserved();
        return new SvgPreservedArcCalculation(true, firstLength.Value, secondLength.Value, SvgFiniteMath.NormalizeZero(rotationDegrees), largeArc, sweep);
    }

    SvgPointTransformResult Point(double x, double y)
    {
        return (matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(x, y));
    }

    bool CanAdd(int count)
    {
        return count >= 0 && builder.Commands.Count <= options.MaximumCommands - count;
    }

    static SvgPreservedArcCalculation NotPreserved()
    {
        return new SvgPreservedArcCalculation(false, 0, 0, 0, false, false);
    }

    static SvgPathTransformError TransformFailure(SvgPointTransformResult first, SvgPointTransformResult second)
    {
        if (first.Error == SvgGeometryError.InvalidArgument || second.Error == SvgGeometryError.InvalidArgument) return SvgPathTransformError.InvalidArgument;
        return SvgPathTransformError.NonFiniteResult;
    }

    static SvgPathTransformError TransformFailure(SvgPointTransformResult first, SvgPointTransformResult second, SvgPointTransformResult third)
    {
        if (first.Error == SvgGeometryError.InvalidArgument || second.Error == SvgGeometryError.InvalidArgument || third.Error == SvgGeometryError.InvalidArgument) return SvgPathTransformError.InvalidArgument;
        return SvgPathTransformError.NonFiniteResult;
    }

    static SvgPathTransformError Map(SvgGeometryError error)
    {
        if (error == SvgGeometryError.InvalidArgument) return SvgPathTransformError.InvalidArgument;
        return SvgPathTransformError.NonFiniteResult;
    }
}
