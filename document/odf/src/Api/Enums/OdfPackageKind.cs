namespace odf;

/// <summary>
/// Identifies the physical representation of an OpenDocument document.
/// </summary>
public enum OdfPackageKind
{
    /// <summary>
    /// The representation is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A ZIP-based OpenDocument package.
    /// </summary>
    Package = 1,
    /// <summary>
    /// A flat XML OpenDocument document.
    /// </summary>
    Flat = 2
}
