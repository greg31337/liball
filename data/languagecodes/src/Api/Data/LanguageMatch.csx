namespace languagecodes;

/// <summary>Contains one deterministically ranked candidate produced by a language lookup.</summary>
public class LanguageMatch
{
    /// <summary>The candidate language entity.</summary>
    public LanguageCodeEntry? Entry;

    /// <summary>The code, name, or alias value that matched the query.</summary>
    public string? MatchedValue;

    /// <summary>The field or comparison rule that produced the match.</summary>
    public LanguageMatchKind Kind;

    /// <summary>The deterministic rank, where lower values are preferred.</summary>
    public int Rank;
}
