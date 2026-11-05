namespace countrycodes;

/// <summary>
/// Selects the broadest matching tier used by a search.
/// </summary>
public enum SearchMode
{
    /// <summary>
    /// Match complete normalized values only.
    /// </summary>
    ExactNormalized = 0,

    /// <summary>
    /// Include matches whose values begin with the query.
    /// </summary>
    Prefix = 1,

    /// <summary>
    /// Include matches containing a complete query token.
    /// </summary>
    Token = 2,

    /// <summary>
    /// Include matches containing the query as a substring.
    /// </summary>
    Substring = 3,

    /// <summary>
    /// Include bounded fuzzy matches.
    /// </summary>
    Fuzzy = 4
}
