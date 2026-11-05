namespace odf;

/// <summary>
/// Identifies a package-entry compression method.
/// </summary>
public enum OdfCompressionMethod
{
    /// <summary>
    /// The compression method is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The entry is stored without compression.
    /// </summary>
    Stored = 1,
    /// <summary>
    /// The entry uses the DEFLATE compression method.
    /// </summary>
    Deflated = 2,
    /// <summary>
    /// The entry uses a recognized but unsupported compression method.
    /// </summary>
    Unsupported = 3
}
