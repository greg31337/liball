namespace odf;

/// <summary>
/// Identifies how a schema rule changes between adjacent ODF versions.
/// </summary>
public enum OdfVersionChangeKind
{
    /// <summary>
    /// No change kind is known.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The rule was added in the newer version.
    /// </summary>
    Added = 1,
    /// <summary>
    /// The rule was removed in the newer version.
    /// </summary>
    Removed = 2,
    /// <summary>
    /// The rule differs between the two versions.
    /// </summary>
    Changed = 3
}
