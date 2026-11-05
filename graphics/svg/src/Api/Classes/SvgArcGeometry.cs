namespace svg;

/// <summary>Provides SVG endpoint arc conversion and center-parameter arc algorithms.</summary>
public class SvgArcGeometry : ISvgArcGeometry
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgArcGeometry()
    {
    }

    /// <summary>Converts one SVG endpoint arc to corrected center parameters.</summary>
    /// <param name="start">The finite starting point.</param>
    /// <param name="end">The finite ending point.</param>
    /// <param name="radiusX">The nonnegative authored horizontal radius.</param>
    /// <param name="radiusY">The nonnegative authored vertical radius.</param>
    /// <param name="rotationDegrees">The finite ellipse x-axis rotation in degrees.</param>
    /// <param name="largeArc">Whether the larger sweep is selected.</param>
    /// <param name="sweep">Whether the positive-angle sweep is selected.</param>
    /// <returns>The corrected center arc or a stable failure.</returns>
    public SvgArcOperationResult FromEndpoint(SvgPoint start, SvgPoint end, double radiusX, double radiusY, double rotationDegrees, bool largeArc, bool sweep)
    {
        return SvgArcMath.FromEndpoint(start, end, radiusX, radiusY, rotationDegrees, largeArc, sweep);
    }

    /// <summary>Converts one normalized SVG arc command using its tracked endpoints.</summary>
    /// <param name="command">The absolute arc command.</param>
    /// <returns>The corrected center arc or a stable failure.</returns>
    public SvgArcOperationResult FromCommand(SvgPathCommand? command)
    {
        if (command == null || !command.IsValid || command.Kind != SvgPathCommandKind.ArcTo || command.Relative || command.Values == null || command.Values.Length != 7) return SvgArcMath.ArcFailure(SvgGeometryError.InvalidArgument);
        if (!SvgFiniteMath.IsFinite(command.StartX) || !SvgFiniteMath.IsFinite(command.StartY) || !SvgFiniteMath.IsFinite(command.EndX) || !SvgFiniteMath.IsFinite(command.EndY)) return SvgArcMath.ArcFailure(SvgGeometryError.InvalidArgument);
        if (command.Values[3] != 0 && command.Values[3] != 1 || command.Values[4] != 0 && command.Values[4] != 1) return SvgArcMath.ArcFailure(SvgGeometryError.InvalidArgument);
        SvgPoint start = new SvgPoint(command.StartX, command.StartY);
        SvgPoint end = new SvgPoint(command.EndX, command.EndY);
        return new SvgArcGeometry().FromEndpoint(start, end, command.Values[0], command.Values[1], command.Values[2], command.Values[3] == 1, command.Values[4] == 1);
    }

    /// <summary>Evaluates a center arc at an inclusive unit parameter.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite point or a stable failure.</returns>
    public SvgPointOperationResult Evaluate(SvgArc arc, double amount)
    {
        return SvgArcMath.Evaluate(arc, amount);
    }

    /// <summary>Splits a center arc at an inclusive unit parameter.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The two ordered portions or a stable failure.</returns>
    public SvgArcSplitResult Split(SvgArc arc, double amount)
    {
        return SvgArcMath.Split(arc, amount);
    }

    /// <summary>Approximates a center arc with cubic segments of at most ninety degrees.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <returns>The ordered cubic segments or a stable failure.</returns>
    public SvgArcCubicResult ToCubics(SvgArc arc)
    {
        return SvgArcMath.ToCubics(arc);
    }
}
