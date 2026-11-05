namespace languagecodes;

/// <summary>Identifies the deterministic comparison used for language-name search.</summary>
public enum LanguageSearchMode
{
    /// <summary>Search for an exact name.</summary>
    Exact = 0,
    /// <summary>Search for names beginning with the query.</summary>
    Prefix = 1,
    /// <summary>Search for names containing the query.</summary>
    Contains = 2
}
