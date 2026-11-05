namespace languagecodes;

/// <summary>Identifies the outcome of RFC 4647 language-tag matching.</summary>
public enum LanguageTagMatchStatus
{
    /// <summary>No caller-provided language tag matched.</summary>
    NoMatch = 0,
    /// <summary>A caller-provided language tag matched.</summary>
    Matched = 1,
    /// <summary>The language range is not well formed.</summary>
    InvalidRange = 2,
    /// <summary>A candidate language tag is not well formed.</summary>
    InvalidTag = 3
}
