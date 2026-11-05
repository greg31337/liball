namespace svg;

/// <summary>Applies affine transforms to normalized path geometry with conditional arc preservation.</summary>
public class SvgPathTransformer : ISvgPathTransformer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathTransformer()
    {
    }

    /// <summary>Transforms a path using secure deterministic defaults.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="matrix">The finite affine transform.</param>
    /// <returns>The transformed path or a stable failure.</returns>
    public SvgPathTransformResult TransformWithPathAndMatrix(SvgPathData path, SvgMatrix matrix)
    {
        return new SvgPathTransformer().TransformWithPathAndMatrixAndOptions(path, matrix, new SvgPathTransformOptions());
    }

    /// <summary>Transforms a path with explicit arc-preservation and output-limit options.</summary>
    /// <param name="path">The valid source path.</param>
    /// <param name="matrix">The finite affine transform.</param>
    /// <param name="options">The transformation options.</param>
    /// <returns>The transformed path or a stable failure.</returns>
    public SvgPathTransformResult TransformWithPathAndMatrixAndOptions(SvgPathData? path, SvgMatrix matrix, SvgPathTransformOptions? options)
    {
        if (path == null || !new SvgPathTransformer().ValidMatrix(matrix) || options == null || options.MaximumCommands < 1) return new SvgPathTransformer().Failure(SvgPathTransformError.InvalidArgument);
        SvgPathNormalizeOptions normalizeOptions = new SvgPathNormalizeOptions();
        normalizeOptions.MaximumCommands = int.MaxValue;
        SvgPathNormalizeResult normalized = new SvgPathNormalizer().NormalizeWithPathAndOptions(path, normalizeOptions);
        if (!normalized.Success) return new SvgPathTransformer().Failure(new SvgPathTransformer().Map(normalized.Error));
        SvgPathTransformState state = new SvgPathTransformState(normalized.PathData, matrix, options);
        return state.Run();
    }

    internal SvgPathTransformResult Failure(SvgPathTransformError error)
    {
        return new SvgPathTransformResult(false, error, null);
    }
SvgPathTransformError Map(SvgPathNormalizeError error)
    {
        if (error == SvgPathNormalizeError.InvalidArgument) return SvgPathTransformError.InvalidArgument;
        if (error == SvgPathNormalizeError.InvalidPath) return SvgPathTransformError.InvalidPath;
        if (error == SvgPathNormalizeError.CommandLimitExceeded) return SvgPathTransformError.CommandLimitExceeded;
        return SvgPathTransformError.NonFiniteResult;
    }
bool ValidMatrix(SvgMatrix? matrix)
    {
        return matrix != null && SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
}
