namespace odf;

/// <summary>
/// Identifies a tracked-change operation.
/// </summary>
public enum OdfChangeKind
{
    /// <summary>
    /// The change kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Inserted content.
    /// </summary>
    Insertion = 1,
    /// <summary>
    /// Deleted content.
    /// </summary>
    Deletion = 2,
    /// <summary>
    /// A formatting change.
    /// </summary>
    FormatChange = 3
}
