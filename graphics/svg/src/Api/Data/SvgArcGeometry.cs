namespace svg;

/// <summary>Represents one finite center-parameterized elliptical arc.</summary>
public class SvgArc
{
    /// <summary>The exact arc starting point.</summary>
    public SvgPoint Start;
    /// <summary>The exact arc ending point.</summary>
    public SvgPoint End;
    /// <summary>The ellipse center x coordinate.</summary>
    public double CenterX;
    /// <summary>The ellipse center y coordinate.</summary>
    public double CenterY;
    /// <summary>The corrected nonnegative horizontal radius.</summary>
    public double RadiusX;
    /// <summary>The corrected nonnegative vertical radius.</summary>
    public double RadiusY;
    /// <summary>The ellipse x-axis rotation in radians.</summary>
    public double Rotation;
    /// <summary>The parametric starting angle in radians.</summary>
    public double StartAngle;
    /// <summary>The signed sweep angle in radians.</summary>
    public double SweepAngle;
    /// <summary>Whether a zero radius reduced the arc to a line.</summary>
    public bool IsLine;
    /// <summary>Whether coincident endpoints reduced the arc to one point.</summary>
    public bool IsPoint;

    internal SvgArc(SvgPoint start, SvgPoint end, double centerX, double centerY, double radiusX, double radiusY, double rotation, double startAngle, double sweepAngle, bool isLine, bool isPoint)
    {
        Start = start;
        End = end;
        CenterX = centerX;
        CenterY = centerY;
        RadiusX = radiusX;
        RadiusY = radiusY;
        Rotation = rotation;
        StartAngle = startAngle;
        SweepAngle = sweepAngle;
        IsLine = isLine;
        IsPoint = isPoint;
    }

    /// <summary>Evaluates the arc at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite point or a stable failure.</returns>
    internal SvgPointOperationResult Evaluate(double amount)
    {
        return new SvgArcGeometry().Evaluate(this, amount);
    }

    /// <summary>Evaluates the first derivative with respect to the unit arc parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite derivative vector or a stable failure.</returns>
    internal SvgVectorOperationResult Derivative(double amount)
    {
        return new SvgCurveGeometry().DerivativeWithArcAndAmount(this, amount);
    }

    /// <summary>Splits the arc at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The two ordered arc portions or a stable failure.</returns>
    internal SvgArcSplitResult Split(double amount)
    {
        return new SvgArcGeometry().Split(this, amount);
    }

    /// <summary>Reverses the arc while preserving its geometric locus.</summary>
    /// <returns>The reversed arc or a stable failure.</returns>
    internal SvgArcOperationResult Reverse()
    {
        return new SvgCurveGeometry().ReverseWithArc(this);
    }

    /// <summary>Computes tight axis-aligned bounds including interior extrema.</summary>
    /// <returns>The finite bounds or a stable failure.</returns>
    internal SvgRectOperationResult GetBounds()
    {
        return new SvgCurveGeometry().GetBoundsWithArc(this);
    }

    /// <summary>Adaptively flattens the arc to parameter-ordered points.</summary>
    /// <param name="options">The tolerance and security limits.</param>
    /// <returns>The flattened points or a stable failure.</returns>
    internal SvgFlattenResult Flatten(SvgFlattenOptions options)
    {
        return new SvgCurveFlattener().FlattenWithArcAndOptions(this, options);
    }

    /// <summary>Approximates the arc with ordered cubic Bézier segments of at most ninety degrees.</summary>
    /// <returns>The cubic segments or a stable failure.</returns>
    internal SvgArcCubicResult ToCubics()
    {
        return new SvgArcGeometry().ToCubics(this);
    }
}

/// <summary>Contains a non-throwing endpoint-to-center arc result.</summary>
public class SvgArcOperationResult
{
    /// <summary>Whether the operation produced a finite arc.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The finite arc, or <c>null</c> after failure.</summary>
    public SvgArc? Arc;

    /// <summary>Creates an arc operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="arc">The finite arc, or <c>null</c>.</param>
    public SvgArcOperationResult(bool success, SvgGeometryError error, SvgArc? arc)
    {
        Success = success;
        Error = error;
        Arc = arc;
    }
}

/// <summary>Contains two ordered portions of a split arc.</summary>
public class SvgArcSplitResult
{
    /// <summary>Whether splitting produced two finite arcs.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The first arc portion, or <c>null</c> after failure.</summary>
    public SvgArc? First;
    /// <summary>The second arc portion, or <c>null</c> after failure.</summary>
    public SvgArc? Second;

    /// <summary>Creates an arc split result.</summary>
    /// <param name="success">Whether splitting succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="first">The first portion.</param>
    /// <param name="second">The second portion.</param>
    public SvgArcSplitResult(bool success, SvgGeometryError error, SvgArc? first, SvgArc? second)
    {
        Success = success;
        Error = error;
        First = first;
        Second = second;
    }
}

/// <summary>Represents one finite cubic Bézier segment.</summary>
public class SvgCubicBezier
{
    /// <summary>The segment starting point.</summary>
    public SvgPoint Start;
    /// <summary>The first control point.</summary>
    public SvgPoint Control1;
    /// <summary>The second control point.</summary>
    public SvgPoint Control2;
    /// <summary>The segment ending point.</summary>
    public SvgPoint End;

    /// <summary>Creates a cubic segment by copying four finite points.</summary>
    /// <param name="start">The starting point.</param>
    /// <param name="control1">The first control point.</param>
    /// <param name="control2">The second control point.</param>
    /// <param name="end">The ending point.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a point is missing or non-finite.</exception>
    public SvgCubicBezier(SvgPoint start, SvgPoint control1, SvgPoint control2, SvgPoint end)
    {
        if (!SvgArcMath.ValidPoint(start) || !SvgArcMath.ValidPoint(control1) || !SvgArcMath.ValidPoint(control2) || !SvgArcMath.ValidPoint(end)) throw new System.ArgumentOutOfRangeException("start");
        Start = new SvgPoint(start.X, start.Y);
        Control1 = new SvgPoint(control1.X, control1.Y);
        Control2 = new SvgPoint(control2.X, control2.Y);
        End = new SvgPoint(end.X, end.Y);
    }

    /// <summary>Evaluates the cubic at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite point or a stable failure.</returns>
    internal SvgPointOperationResult Evaluate(double amount)
    {
        return new SvgCurveGeometry().EvaluateWithCubicAndAmount(this, amount);
    }

    /// <summary>Evaluates the first derivative with respect to the unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite derivative vector or a stable failure.</returns>
    internal SvgVectorOperationResult Derivative(double amount)
    {
        return new SvgCurveGeometry().DerivativeWithCubicAndAmount(this, amount);
    }

    /// <summary>Splits the cubic at an inclusive unit parameter.</summary>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The two ordered cubic portions or a stable failure.</returns>
    internal SvgCubicSplitResult Split(double amount)
    {
        return new SvgCurveGeometry().SplitWithCubicAndAmount(this, amount);
    }

    /// <summary>Reverses the cubic while preserving its geometric locus.</summary>
    /// <returns>The reversed cubic or a stable failure.</returns>
    internal SvgCubicOperationResult Reverse()
    {
        return new SvgCurveGeometry().ReverseWithCubic(this);
    }

    /// <summary>Computes tight axis-aligned bounds including interior extrema.</summary>
    /// <returns>The finite bounds or a stable failure.</returns>
    internal SvgRectOperationResult GetBounds()
    {
        return new SvgCurveGeometry().GetBoundsWithCubic(this);
    }

    /// <summary>Adaptively flattens the cubic to parameter-ordered points.</summary>
    /// <param name="options">The tolerance and security limits.</param>
    /// <returns>The flattened points or a stable failure.</returns>
    internal SvgFlattenResult Flatten(SvgFlattenOptions options)
    {
        return new SvgCurveFlattener().FlattenWithCubicAndOptions(this, options);
    }
}

/// <summary>Contains an ordered cubic approximation of an arc.</summary>
public class SvgArcCubicResult
{
    /// <summary>Whether conversion produced finite cubic segments.</summary>
    public bool Success;
    /// <summary>The stable geometry failure.</summary>
    public SvgGeometryError Error;
    /// <summary>The ordered cubic segments, empty for a point arc.</summary>
    public System.Collections.Generic.List<SvgCubicBezier>? Curves;

    /// <summary>Creates an arc cubic conversion result.</summary>
    /// <param name="success">Whether conversion succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="curves">The ordered cubic segments.</param>
    public SvgArcCubicResult(bool success, SvgGeometryError error, System.Collections.Generic.List<SvgCubicBezier>? curves)
    {
        Success = success;
        Error = error;
        Curves = curves;
    }
}
