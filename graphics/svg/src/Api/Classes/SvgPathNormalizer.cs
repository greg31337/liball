namespace svg;

/// <summary>Normalizes SVG paths to absolute explicit commands with recomputed bookkeeping.</summary>
public class SvgPathNormalizer : ISvgPathNormalizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathNormalizer()
    {
    }

    /// <summary>Normalizes a path while preserving explicit quadratic commands.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <returns>The normalized path or a stable failure.</returns>
    public SvgPathNormalizeResult NormalizeWithPath(SvgPathData path)
    {
        return new SvgPathNormalizer().NormalizeWithPathAndOptions(path, new SvgPathNormalizeOptions());
    }

    /// <summary>Normalizes a path with explicit conversion and command-limit options.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized path or a stable failure.</returns>
    public SvgPathNormalizeResult NormalizeWithPathAndOptions(SvgPathData? path, SvgPathNormalizeOptions? options)
    {
        if (path == null || options == null || options.MaximumCommands < 1) return new SvgPathNormalizer().Failure(SvgPathNormalizeError.InvalidArgument);
        if (!path.IsValid || path.Commands == null || path.Issues == null || path.Issues.Count != 0) return new SvgPathNormalizer().Failure(SvgPathNormalizeError.InvalidPath);
        if (path.Commands.Count > options.MaximumCommands) return new SvgPathNormalizer().Failure(SvgPathNormalizeError.CommandLimitExceeded);
        SvgPathNormalizerState state = new SvgPathNormalizerState(path, options);
        return state.Run();
    }

    /// <summary>Normalizes a path and converts every quadratic command to a cubic command.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <returns>The normalized cubic path or a stable failure.</returns>
    public SvgPathNormalizeResult NormalizeToCubicsWithPath(SvgPathData path)
    {
        SvgPathNormalizeOptions options = new SvgPathNormalizeOptions();
        options.ConvertQuadraticsToCubics = true;
        return new SvgPathNormalizer().NormalizeWithPathAndOptions(path, options);
    }

    /// <summary>Normalizes a path to cubics within an explicit positive command limit.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <param name="maximumCommands">The positive maximum output command count.</param>
    /// <returns>The normalized cubic path or a stable failure.</returns>
    public SvgPathNormalizeResult NormalizeToCubicsWithPathAndMaximumCommands(SvgPathData path, int maximumCommands)
    {
        SvgPathNormalizeOptions options = new SvgPathNormalizeOptions();
        options.MaximumCommands = maximumCommands;
        options.ConvertQuadraticsToCubics = true;
        return new SvgPathNormalizer().NormalizeWithPathAndOptions(path, options);
    }

    internal SvgPathNormalizeResult Failure(SvgPathNormalizeError error)
    {
        return new SvgPathNormalizeResult(false, error, null);
    }
}
