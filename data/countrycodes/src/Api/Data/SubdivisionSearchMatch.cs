namespace countrycodes;

/// <summary>
/// Holds one subdivision search match and its owning country.
/// </summary>
public class SubdivisionSearchMatch
{
    /// <summary>
    /// The detached matching subdivision.
    /// </summary>
    public Subdivision Subdivision;

    /// <summary>
    /// The detached country that owns the subdivision.
    /// </summary>
    public Country Country;

    /// <summary>
    /// The reason the subdivision matched the query.
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
    /// Creates a detached subdivision search match.
    /// </summary>
    /// <param name="subdivision">The matching subdivision.</param>
    /// <param name="country">The owning country.</param>
    /// <param name="kind">The reason for the match.</param>
    /// <param name="sourceText">The source code or text that matched.</param>
    /// <param name="normalizedKey">The normalized ranking key.</param>
    /// <param name="score">The deterministic integer score.</param>
    public SubdivisionSearchMatch(
        Subdivision subdivision,
        Country country,
        SearchMatchKind kind,
        string sourceText,
        string normalizedKey,
        int score)
    {
        Subdivision = subdivision.Copy();
        Country = country.Copy();
        Kind = kind;
        SourceText = sourceText;
        NormalizedKey = normalizedKey;
        Score = score;
    }
}
