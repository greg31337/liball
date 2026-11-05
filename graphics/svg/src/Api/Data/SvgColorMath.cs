namespace svg;

/// <summary>Contains a non-throwing color operation result.</summary>
public class SvgColorOperationResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success;

    /// <summary>The stable failure kind.</summary>
    public SvgColorOperationError Error;

    /// <summary>The resulting color, or <c>null</c> after failure.</summary>
    public SvgColor? Color;

    /// <summary>Creates a color operation result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="color">The resulting color, or <c>null</c>.</param>
    public SvgColorOperationResult(bool success, SvgColorOperationError error, SvgColor? color)
    {
        Success = success;
        Error = error;
        Color = color;
    }
}
