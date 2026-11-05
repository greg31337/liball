namespace languagecodes;

/// <summary>Contains the complete outcome of a language lookup without requiring exception handling.</summary>
public class LanguageLookupResult
{
    /// <summary>The lookup outcome.</summary>
    public LanguageLookupStatus Status;

    /// <summary>The selected entity when the lookup has one resolvable result.</summary>
    public LanguageCodeEntry? Entry;

    /// <summary>All deterministically ranked candidates retained by the lookup.</summary>
    public LanguageMatch[] Candidates = new LanguageMatch[0];

    /// <summary>The query after the explicitly enabled normalization steps were applied.</summary>
    public string? NormalizedQuery;
}
