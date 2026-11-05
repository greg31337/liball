namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgArcGeometry"/>.</summary>
public interface ISvgArcGeometry
{
    /// <summary>Converts one SVG endpoint arc to corrected center parameters.</summary>
    /// <param name="start">The finite starting point.</param>
    /// <param name="end">The finite ending point.</param>
    /// <param name="radiusX">The nonnegative authored horizontal radius.</param>
    /// <param name="radiusY">The nonnegative authored vertical radius.</param>
    /// <param name="rotationDegrees">The finite ellipse x-axis rotation in degrees.</param>
    /// <param name="largeArc">Whether the larger sweep is selected.</param>
    /// <param name="sweep">Whether the positive-angle sweep is selected.</param>
    /// <returns>The corrected center arc or a stable failure.</returns>
    SvgArcOperationResult FromEndpoint(SvgPoint start, SvgPoint end, double radiusX, double radiusY, double rotationDegrees, bool largeArc, bool sweep);
    /// <summary>Converts one normalized SVG arc command using its tracked endpoints.</summary>
    /// <param name="command">The absolute arc command.</param>
    /// <returns>The corrected center arc or a stable failure.</returns>
    SvgArcOperationResult FromCommand(SvgPathCommand? command);
    /// <summary>Evaluates a center arc at an inclusive unit parameter.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The finite point or a stable failure.</returns>
    SvgPointOperationResult Evaluate(SvgArc arc, double amount);
    /// <summary>Splits a center arc at an inclusive unit parameter.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <param name="amount">The finite amount from zero through one.</param>
    /// <returns>The two ordered portions or a stable failure.</returns>
    SvgArcSplitResult Split(SvgArc arc, double amount);
    /// <summary>Approximates a center arc with cubic segments of at most ninety degrees.</summary>
    /// <param name="arc">The finite arc.</param>
    /// <returns>The ordered cubic segments or a stable failure.</returns>
    SvgArcCubicResult ToCubics(SvgArc arc);
}
