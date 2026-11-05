namespace svg;

/// <summary>Contains a non-throwing basic-shape path conversion result.</summary>
public class SvgShapePathResult
{
    /// <summary>Whether conversion produced a valid generated path.</summary>
    public bool Success;
    /// <summary>The stable conversion failure.</summary>
    public SvgShapePathError Error;
    /// <summary>The generated absolute path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;

    /// <summary>Creates a basic-shape path conversion result.</summary>
    /// <param name="success">Whether conversion succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="pathData">The generated path, or <c>null</c>.</param>
    public SvgShapePathResult(bool success, SvgShapePathError error, SvgPathData? pathData)
    {
        Success = success;
        Error = error;
        PathData = pathData;
    }
}
