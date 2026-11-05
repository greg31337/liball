namespace odf;

/// <summary>
/// Identifies an OpenDocument Format version.
/// </summary>
public enum OdfVersion
{
    /// <summary>
    /// The version is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// OpenDocument Format 1.0.
    /// </summary>
    Version10 = 1,
    /// <summary>
    /// OpenDocument Format 1.1.
    /// </summary>
    Version11 = 2,
    /// <summary>
    /// OpenDocument Format 1.2.
    /// </summary>
    Version12 = 3,
    /// <summary>
    /// OpenDocument Format 1.3.
    /// </summary>
    Version13 = 4,
    /// <summary>
    /// OpenDocument Format 1.4.
    /// </summary>
    Version14 = 5,
    /// <summary>
    /// A syntactically valid version newer than the versions known by this SDK.
    /// </summary>
    Future = 6
}
