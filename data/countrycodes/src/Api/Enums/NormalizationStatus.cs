namespace countrycodes;

/// <summary>
/// Describes the outcome of normalizing an identifier or name.
/// </summary>
public enum NormalizationStatus
{
    /// <summary>
    /// The input was already in canonical form.
    /// </summary>
    AlreadyCanonical = 0,

    /// <summary>
    /// The input was converted to canonical form.
    /// </summary>
    Normalized = 1,

    /// <summary>
    /// The input refers to an identifier with a replacement.
    /// </summary>
    Replaced = 2,

    /// <summary>
    /// The input has multiple possible canonical replacements.
    /// </summary>
    Ambiguous = 3,

    /// <summary>
    /// The input cannot be normalized.
    /// </summary>
    Invalid = 4
}
