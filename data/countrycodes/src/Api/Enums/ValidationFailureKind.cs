namespace countrycodes;

/// <summary>
/// Identifies a stable category of validation failure.
/// </summary>
public enum ValidationFailureKind
{
    /// <summary>
    /// The input is null or empty.
    /// </summary>
    NullOrEmpty = 0,

    /// <summary>
    /// The input has an invalid length.
    /// </summary>
    InvalidLength = 1,

    /// <summary>
    /// The input contains a character outside the permitted set.
    /// </summary>
    InvalidCharacter = 2,

    /// <summary>
    /// The input does not have the required structure.
    /// </summary>
    InvalidShape = 3,

    /// <summary>
    /// The well-formed input is not known to the catalog.
    /// </summary>
    Unknown = 4,

    /// <summary>
    /// The input is reserved and is rejected by the selected policy.
    /// </summary>
    Reserved = 5,

    /// <summary>
    /// The input was formerly assigned and is rejected by the selected policy.
    /// </summary>
    FormerlyAssigned = 6,

    /// <summary>
    /// The input is user-assigned and is rejected by the selected policy.
    /// </summary>
    UserAssignedDisallowed = 7,

    /// <summary>
    /// A subdivision uses a country prefix unknown to the catalog.
    /// </summary>
    UnknownCountryPrefix = 8,

    /// <summary>
    /// The input is deprecated.
    /// </summary>
    Deprecated = 9,

    /// <summary>
    /// The input identifies more than one candidate.
    /// </summary>
    Ambiguous = 10,

    /// <summary>
    /// The locale is valid but unsupported by the catalog.
    /// </summary>
    UnsupportedLocale = 11
}
