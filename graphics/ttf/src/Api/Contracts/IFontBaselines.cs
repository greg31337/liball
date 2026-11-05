namespace ttf;

/// <summary>Defines access to OpenType BASE baseline axes.</summary>
public interface IFontBaselines
{
    /// <summary>Gets the BASE major version.</summary>
    /// <returns>The major version.</returns>
    int GetMajorVersion();

    /// <summary>Gets the BASE minor version.</summary>
    /// <returns>The minor version.</returns>
    int GetMinorVersion();

    /// <summary>Gets the horizontal baseline axis.</summary>
    /// <returns>The axis, or null when absent.</returns>
    FontBaselineAxis? GetHorizontalAxis();

    /// <summary>Gets the vertical baseline axis.</summary>
    /// <returns>The axis, or null when absent.</returns>
    FontBaselineAxis? GetVerticalAxis();
}
