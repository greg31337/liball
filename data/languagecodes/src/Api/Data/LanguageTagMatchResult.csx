namespace languagecodes;

/// <summary>Contains the outcome of RFC 4647 filtering or lookup against language tags.</summary>
public class LanguageTagMatchResult
{
    /// <summary>The language-tag matching outcome.</summary>
    public LanguageTagMatchStatus Status;

    /// <summary>The matched caller-provided language tag when one was selected.</summary>
    public string? MatchedTag;

    /// <summary>The zero-based caller collection index, or <c>-1</c> when no tag was selected.</summary>
    public int MatchedIndex = -1;

    /// <summary>The normalized RFC 4647 language range used for matching.</summary>
    public string? NormalizedRange;

    /// <summary>The parsed representation of <see cref="MatchedTag"/> when one was selected.</summary>
    public LanguageTag? ParsedTag;
}
