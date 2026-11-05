namespace languagecodes;

/// <summary>Controls code, name, alias, and generic language lookup behavior.</summary>
public class LanguageLookupOptions
{
    /// <summary>Whether surrounding whitespace may be removed before lookup.</summary>
    public bool TrimWhitespace;

    /// <summary>Whether code and name casing may be normalized before comparison.</summary>
    public bool NormalizeCase;

    /// <summary>Whether deterministic Unicode, whitespace, punctuation, and inverted-name normalization is applied.</summary>
    public bool NormalizeNames;

    /// <summary>Whether retired ISO 639 Set 3 codes participate in lookup.</summary>
    public bool IncludeRetiredCodes;

    /// <summary>The lowercase language code used to filter names, or null to include every name language.</summary>
    public string? NameLanguage;

    /// <summary>The comparison mode used for name search.</summary>
    public LanguageSearchMode SearchMode;

    /// <summary>The maximum number of candidates to return.</summary>
    public int ResultLimit;
}
