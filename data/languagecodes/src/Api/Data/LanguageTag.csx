namespace languagecodes;

/// <summary>Contains every ordered component of a parsed RFC 5646 language tag.</summary>
public class LanguageTag
{
    /// <summary>The complete language tag supplied by the caller.</summary>
    public string? Value;

    /// <summary>The complete tag with normalized separators and recommended casing.</summary>
    public string? NormalizedValue;

    /// <summary>The primary language subtag, absent for a private-use-only tag.</summary>
    public string? Language;

    /// <summary>The ordered extended-language subtags.</summary>
    public string[] Extlangs = new string[0];

    /// <summary>The script subtag when present.</summary>
    public string? Script;

    /// <summary>The region subtag when present.</summary>
    public string? Region;

    /// <summary>The ordered variant subtags.</summary>
    public string[] Variants = new string[0];

    /// <summary>The ordered singleton-led extension sequences, with each sequence retained as one hyphenated string.</summary>
    public string[] Extensions = new string[0];

    /// <summary>The ordered private-use subtags following the <c>x</c> singleton.</summary>
    public string[] PrivateUse = new string[0];

    /// <summary>Whether the complete tag consists only of a private-use sequence.</summary>
    public bool IsPrivateUseOnly;

    /// <summary>Whether the complete value is an IANA grandfathered tag.</summary>
    public bool IsGrandfathered;

    /// <summary>Whether the complete value is an IANA redundant tag.</summary>
    public bool IsRedundant;
}
