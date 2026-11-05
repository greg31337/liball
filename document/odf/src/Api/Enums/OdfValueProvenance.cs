namespace odf;

/// <summary>
/// Identifies how an OpenDocument value was obtained.
/// </summary>
public enum OdfValueProvenance
{
    /// <summary>
    /// The value was parsed directly from source content.
    /// </summary>
    Parsed = 0,
    /// <summary>
    /// The value was computed.
    /// </summary>
    Computed = 1,
    /// <summary>
    /// The value came from a specification or operation default.
    /// </summary>
    Defaulted = 2,
    /// <summary>
    /// The value was inherited from another definition.
    /// </summary>
    Inherited = 3,
    /// <summary>
    /// The value belongs to foreign extension content.
    /// </summary>
    Foreign = 4,
    /// <summary>
    /// The invalid lexical value was preserved for round trips.
    /// </summary>
    InvalidPreserved = 5,
    /// <summary>
    /// The value was synthesized by the SDK.
    /// </summary>
    Synthesized = 6
}
