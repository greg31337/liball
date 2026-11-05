namespace svg;

/// <summary>Configures bounded absolute path normalization.</summary>
public class SvgPathNormalizeOptions
{
    /// <summary>The positive maximum output command count.</summary>
    public int MaximumCommands;
    /// <summary>Whether explicit quadratic commands are converted to cubic commands.</summary>
    public bool ConvertQuadraticsToCubics;

    /// <summary>Creates deterministic normalization defaults.</summary>
    public SvgPathNormalizeOptions()
    {
        MaximumCommands = 1000000;
        ConvertQuadraticsToCubics = false;
    }
}

/// <summary>Contains a non-throwing path normalization result.</summary>
public class SvgPathNormalizeResult
{
    /// <summary>Whether normalization produced a valid path.</summary>
    public bool Success;
    /// <summary>The stable normalization failure.</summary>
    public SvgPathNormalizeError Error;
    /// <summary>The normalized path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;

    /// <summary>Creates a path normalization result.</summary>
    /// <param name="success">Whether normalization succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="pathData">The normalized path, or <c>null</c>.</param>
    public SvgPathNormalizeResult(bool success, SvgPathNormalizeError error, SvgPathData? pathData)
    {
        Success = success;
        Error = error;
        PathData = pathData;
    }
}
