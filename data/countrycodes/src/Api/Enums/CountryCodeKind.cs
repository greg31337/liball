namespace countrycodes;

/// <summary>
/// Identifies the recognized shape of a country or subdivision code.
/// </summary>
public enum CountryCodeKind
{
    /// <summary>
    /// The code kind is not known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// An ISO 3166-1 two-letter code.
    /// </summary>
    Alpha2 = 1,

    /// <summary>
    /// An ISO 3166-1 three-letter code.
    /// </summary>
    Alpha3 = 2,

    /// <summary>
    /// An ISO 3166-1 three-digit code.
    /// </summary>
    Numeric3 = 3,

    /// <summary>
    /// An ISO 3166-3 four-letter code.
    /// </summary>
    Alpha4 = 4,

    /// <summary>
    /// An ISO 3166-2 subdivision code.
    /// </summary>
    Subdivision = 5
}
