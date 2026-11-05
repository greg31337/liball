namespace countrycodes;

/// <summary>
/// Describes the lifecycle state of an ISO 3166-2 subdivision.
/// </summary>
public enum SubdivisionStatus
{
    /// <summary>
    /// The subdivision is active.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The subdivision is deprecated and may have a replacement.
    /// </summary>
    Deprecated = 1,

    /// <summary>
    /// The subdivision has been withdrawn.
    /// </summary>
    Withdrawn = 2,

    /// <summary>
    /// The subdivision status is not known.
    /// </summary>
    Unknown = 3
}
