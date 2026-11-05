namespace countrycodes;

/// <summary>
/// Holds one country search match.
/// </summary>
public class CountrySearchMatch
{
    /// <summary>
    /// The detached country record.
    /// </summary>
    public Country Country;

    /// <summary>
    /// The reason the country matched the query.
    /// </summary>
    public SearchMatchKind Kind;

    /// <summary>
    /// The source code or display text that matched.
    /// </summary>
    public string SourceText;

    /// <summary>
    /// The normalized key used for ranking.
    /// </summary>
    public string NormalizedKey;

    /// <summary>
    /// The deterministic integer search score.
    /// </summary>
    public int Score;

    /// <summary>
    /// Creates a detached country search match.
    /// </summary>
    /// <param name="country">The matching country.</param>
    /// <param name="kind">The reason for the match.</param>
    /// <param name="sourceText">The source code or text that matched.</param>
    /// <param name="normalizedKey">The normalized ranking key.</param>
    /// <param name="score">The deterministic integer score.</param>
    public CountrySearchMatch(
        Country country,
        SearchMatchKind kind,
        string sourceText,
        string normalizedKey,
        int score)
    {
        Country = country.Copy();
        Kind = kind;
        SourceText = sourceText;
        NormalizedKey = normalizedKey;
        Score = score;
    }
}
