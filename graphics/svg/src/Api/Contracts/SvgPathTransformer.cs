namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathTransformer"/>.</summary>
public interface ISvgPathTransformer
{
    /// <summary>Transforms a path using secure deterministic defaults.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="matrix">The finite affine transform.</param>
    /// <returns>The transformed path or a stable failure.</returns>
    SvgPathTransformResult TransformWithPathAndMatrix(SvgPathData path, SvgMatrix matrix);
    /// <summary>Transforms a path with explicit arc-preservation and output-limit options.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="matrix">The finite affine transform.</param>
    /// <param name="options">The transformation options.</param>
    /// <returns>The transformed path or a stable failure.</returns>
    SvgPathTransformResult TransformWithPathAndMatrixAndOptions(SvgPathData? path, SvgMatrix matrix, SvgPathTransformOptions? options);
}
