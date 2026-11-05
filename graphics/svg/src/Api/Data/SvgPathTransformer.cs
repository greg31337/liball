namespace svg;

/// <summary>Configures bounded affine path transformation.</summary>
public class SvgPathTransformOptions
{
    /// <summary>The positive maximum transformed command count.</summary>
    public int MaximumCommands;
    /// <summary>Whether exactly representable transformed arcs remain arc commands.</summary>
    public bool PreserveArcs;

    /// <summary>Creates secure deterministic path-transform defaults.</summary>
    public SvgPathTransformOptions()
    {
        MaximumCommands = 1000000;
        PreserveArcs = true;
    }
}

/// <summary>Contains a non-throwing affine path transformation result.</summary>
public class SvgPathTransformResult
{
    /// <summary>Whether transformation produced a finite path.</summary>
    public bool Success;
    /// <summary>The stable transformation failure.</summary>
    public SvgPathTransformError Error;
    /// <summary>The transformed absolute path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;

    /// <summary>Creates a path transformation result.</summary>
    /// <param name="success">Whether transformation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="pathData">The transformed path, or <c>null</c>.</param>
    public SvgPathTransformResult(bool success, SvgPathTransformError error, SvgPathData? pathData)
    {
        Success = success;
        Error = error;
        PathData = pathData;
    }
}
