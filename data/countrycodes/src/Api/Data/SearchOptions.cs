namespace countrycodes;

/// <summary>
/// Holds options that control country and subdivision searches.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// The broadest matching tier included by the search.
    /// </summary>
    public SearchMode Mode;

    /// <summary>
    /// The validation policy applied to code-shaped queries.
    /// </summary>
    public ValidationPolicy ValidationPolicy;

    /// <summary>
    /// The preferred canonical locale, or null when localized names are not requested.
    /// </summary>
    public string? Locale;

    /// <summary>
    /// Whether configured locale-parent fallback is permitted.
    /// </summary>
    public bool UseLocaleFallback;

    /// <summary>
    /// Whether reserved country identifiers participate in search.
    /// </summary>
    public bool IncludeReserved;

    /// <summary>
    /// Whether historical names and identifiers participate in search.
    /// </summary>
    public bool IncludeHistorical;

    /// <summary>
    /// Whether user-assigned identifiers participate in search.
    /// </summary>
    public bool IncludeUserAssigned;

    /// <summary>
    /// Whether diacritics are folded while constructing match keys.
    /// </summary>
    public bool FoldDiacritics;

    /// <summary>
    /// Whether punctuation is folded while constructing match keys.
    /// </summary>
    public bool FoldPunctuation;

    /// <summary>
    /// The signed maximum number of results.
    /// </summary>
    public int MaximumResults;

    /// <summary>
    /// Creates country and subdivision search options.
    /// </summary>
    /// <param name="mode">The broadest matching tier.</param>
    /// <param name="validationPolicy">The policy for code-shaped queries.</param>
    /// <param name="locale">The preferred locale, when requested.</param>
    /// <param name="useLocaleFallback">Whether locale fallback is permitted.</param>
    /// <param name="includeReserved">Whether reserved identifiers participate.</param>
    /// <param name="includeHistorical">Whether historical data participates.</param>
    /// <param name="includeUserAssigned">Whether user-assigned identifiers participate.</param>
    /// <param name="foldDiacritics">Whether match keys fold diacritics.</param>
    /// <param name="foldPunctuation">Whether match keys fold punctuation.</param>
    /// <param name="maximumResults">The signed maximum result count.</param>
    public SearchOptions(
        SearchMode mode,
        ValidationPolicy validationPolicy,
        string? locale,
        bool useLocaleFallback,
        bool includeReserved,
        bool includeHistorical,
        bool includeUserAssigned,
        bool foldDiacritics,
        bool foldPunctuation,
        int maximumResults)
    {
        Mode = mode;
        ValidationPolicy = validationPolicy;
        Locale = locale;
        UseLocaleFallback = useLocaleFallback;
        IncludeReserved = includeReserved;
        IncludeHistorical = includeHistorical;
        IncludeUserAssigned = includeUserAssigned;
        FoldDiacritics = foldDiacritics;
        FoldPunctuation = foldPunctuation;
        MaximumResults = maximumResults;
    }
}
