namespace icc;

/// <summary>
/// Specifies how a profile is arranged when written.
/// </summary>
public enum IccWriteMode
{
    /// <summary>
    /// Produces a deterministic, normalized profile representation.
    /// </summary>
    Canonical = 0,

    /// <summary>
    /// Retains original ordering and encodings where possible.
    /// </summary>
    Preservation = 1
}
