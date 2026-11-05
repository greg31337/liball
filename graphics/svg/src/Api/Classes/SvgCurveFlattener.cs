namespace svg;

/// <summary>Adaptively flattens finite line, Bézier, and elliptical-arc segments.</summary>
public class SvgCurveFlattener : ISvgCurveFlattener
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCurveFlattener()
    {
    }

    /// <summary>Flattens a line using explicit tolerance and limits.</summary>
    /// <param name="line">The finite line.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered endpoints or a stable failure.</returns>
    public SvgFlattenResult FlattenWithLineAndOptions(SvgLineSegment line, SvgFlattenOptions options) { return SvgFlattenMath.Flatten(line, options); }
    /// <summary>Adaptively flattens a quadratic Bézier.</summary>
    /// <param name="quadratic">The finite quadratic.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered points or a stable failure.</returns>
    public SvgFlattenResult FlattenWithQuadraticAndOptions(SvgQuadraticBezier quadratic, SvgFlattenOptions options) { return SvgFlattenMath.Flatten(quadratic, options); }
    /// <summary>Adaptively flattens a cubic Bézier.</summary>
    /// <param name="cubic">The finite cubic.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered points or a stable failure.</returns>
    public SvgFlattenResult FlattenWithCubicAndOptions(SvgCubicBezier cubic, SvgFlattenOptions options) { return SvgFlattenMath.Flatten(cubic, options); }
    /// <summary>Adaptively flattens an elliptical arc.</summary>
    /// <param name="arc">The finite arc.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered points or a stable failure.</returns>
    public SvgFlattenResult FlattenWithArcAndOptions(SvgArc arc, SvgFlattenOptions options) { return SvgFlattenMath.Flatten(arc, options); }
}
