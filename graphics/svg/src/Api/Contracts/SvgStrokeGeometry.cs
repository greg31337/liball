namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgStrokeGeometry"/>.</summary>
public interface ISvgStrokeGeometry
{
    /// <summary>Tests containment in transformed output coordinates.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="point">The finite output-space query point.</param>
    /// <param name="options">The stroke style, transform, and vector effect.</param>
    /// <returns>The stroke-containment result or a stable failure.</returns>
    SvgStrokeContainmentResult Contains(SvgPathData path, SvgPoint? point, SvgStrokeGeometryOptions options);
    /// <summary>Computes bounds in transformed output coordinates.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="options">The stroke style, transform, and vector effect.</param>
    /// <returns>The finite stroke bounds or a stable failure.</returns>
    SvgStrokeBoundsResult GetBounds(SvgPathData path, SvgStrokeGeometryOptions options);
}
