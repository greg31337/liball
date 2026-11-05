namespace countrycodes;

/// <summary>
/// Identifies why a search result matched its query.
/// </summary>
public enum SearchMatchKind
{
    /// <summary>
    /// The query exactly matched a code.
    /// </summary>
    ExactCode = 0,

    /// <summary>
    /// The query exactly matched a canonical name.
    /// </summary>
    ExactName = 1,

    /// <summary>
    /// The query exactly matched an alias.
    /// </summary>
    ExactAlias = 2,

    /// <summary>
    /// The normalized query exactly matched a normalized value.
    /// </summary>
    Normalized = 3,

    /// <summary>
    /// The query matched the beginning of a value.
    /// </summary>
    Prefix = 4,

    /// <summary>
    /// The query matched a complete token.
    /// </summary>
    Token = 5,

    /// <summary>
    /// The query matched a substring.
    /// </summary>
    Substring = 6,

    /// <summary>
    /// The query matched within the configured fuzzy distance.
    /// </summary>
    Fuzzy = 7,

    /// <summary>
    /// The query matched a localized name.
    /// </summary>
    Localized = 8,

    /// <summary>
    /// The query matched a historical name or identifier.
    /// </summary>
    Historical = 9,

    /// <summary>
    /// The country result was found through a subdivision match.
    /// </summary>
    SubdivisionDerived = 10
}
