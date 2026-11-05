namespace countrycodes;

/// <summary>
/// Describes the outcome of a catalog lookup.
/// </summary>
public enum LookupStatus
{
    /// <summary>
    /// Exactly one matching record was found.
    /// </summary>
    Found = 0,

    /// <summary>
    /// No matching record was found.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// More than one matching record was found.
    /// </summary>
    Ambiguous = 2,

    /// <summary>
    /// The supplied input is invalid.
    /// </summary>
    InvalidInput = 3
}
