namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPathNormalizer"/>.</summary>
public interface ISvgPathNormalizer
{
    /// <summary>Normalizes a path while preserving explicit quadratic commands.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <returns>The normalized path or a stable failure.</returns>
    SvgPathNormalizeResult NormalizeWithPath(SvgPathData path);
    /// <summary>Normalizes a path with explicit conversion and command-limit options.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized path or a stable failure.</returns>
    SvgPathNormalizeResult NormalizeWithPathAndOptions(SvgPathData? path, SvgPathNormalizeOptions? options);
    /// <summary>Normalizes a path and converts every quadratic command to a cubic command.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <returns>The normalized cubic path or a stable failure.</returns>
    SvgPathNormalizeResult NormalizeToCubicsWithPath(SvgPathData path);
    /// <summary>Normalizes a path to cubics within an explicit positive command limit.</summary>
    /// <param name="path">The valid mutable path to normalize.</param>
    /// <param name="maximumCommands">The positive maximum output command count.</param>
    /// <returns>The normalized cubic path or a stable failure.</returns>
    SvgPathNormalizeResult NormalizeToCubicsWithPathAndMaximumCommands(SvgPathData path, int maximumCommands);
}
