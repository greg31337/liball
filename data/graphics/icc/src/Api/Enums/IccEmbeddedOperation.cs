namespace icc;

/// <summary>
/// Identifies an embedded-profile operation.
/// </summary>
public enum IccEmbeddedOperation
{
    /// <summary>
    /// Identifies carrier detection.
    /// </summary>
    Detect = 0,

    /// <summary>
    /// Identifies embedded-profile enumeration.
    /// </summary>
    Enumerate = 1,

    /// <summary>
    /// Identifies embedded-profile extraction.
    /// </summary>
    Extract = 2,

    /// <summary>
    /// Identifies embedded-profile insertion.
    /// </summary>
    Insert = 3,

    /// <summary>
    /// Identifies embedded-profile replacement.
    /// </summary>
    Replace = 4,

    /// <summary>
    /// Identifies embedded-profile removal.
    /// </summary>
    Remove = 5
}
