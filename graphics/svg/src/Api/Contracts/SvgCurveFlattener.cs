namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCurveFlattener"/>.</summary>
public interface ISvgCurveFlattener
{
    /// <summary>Flattens a line using explicit tolerance and limits.</summary>
    /// <param name="line">The finite line.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered endpoints or a stable failure.</returns>
    SvgFlattenResult FlattenWithLineAndOptions(SvgLineSegment line, SvgFlattenOptions options);
    /// <summary>Adaptively flattens a quadratic Bézier.</summary>
    /// <param name="quadratic">The finite quadratic.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered points or a stable failure.</returns>
    SvgFlattenResult FlattenWithQuadraticAndOptions(SvgQuadraticBezier quadratic, SvgFlattenOptions options);
    /// <summary>Adaptively flattens a cubic Bézier.</summary>
    /// <param name="cubic">The finite cubic.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered points or a stable failure.</returns>
    SvgFlattenResult FlattenWithCubicAndOptions(SvgCubicBezier cubic, SvgFlattenOptions options);
    /// <summary>Adaptively flattens an elliptical arc.</summary>
    /// <param name="arc">The finite arc.</param><param name="options">The flattening options.</param>
    /// <returns>The ordered points or a stable failure.</returns>
    SvgFlattenResult FlattenWithArcAndOptions(SvgArc arc, SvgFlattenOptions options);
}
