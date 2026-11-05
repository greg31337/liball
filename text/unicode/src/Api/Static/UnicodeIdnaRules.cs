namespace unicode;

/// <summary>
/// Provides stable diagnostic rule identifiers for IDNA-adjacent Unicode labels.
/// </summary>
public static class UnicodeIdnaRules
{
    /// <summary>The stable diagnostic rule identifier for an empty label.</summary>
    public const string EmptyRule = UnicodeIdna.EmptyRule;
    /// <summary>The stable diagnostic rule identifier for malformed UTF-16.</summary>
    public const string InvalidScalarRule = UnicodeIdna.InvalidScalarRule;
    /// <summary>The stable diagnostic rule identifier for a label separator.</summary>
    public const string LabelSeparatorRule = UnicodeIdna.LabelSeparatorRule;
    /// <summary>The stable diagnostic rule identifier for hyphen placement.</summary>
    public const string HyphenRule = UnicodeIdna.HyphenRule;
    /// <summary>The stable diagnostic rule identifier for a leading combining mark.</summary>
    public const string LeadingMarkRule = UnicodeIdna.LeadingMarkRule;
    /// <summary>The stable diagnostic rule identifier for a disallowed code point.</summary>
    public const string DisallowedCodePointRule = UnicodeIdna.DisallowedCodePointRule;
    /// <summary>The stable diagnostic rule identifier for a contextual rule failure.</summary>
    public const string ContextualRule = UnicodeIdna.ContextualRule;
    /// <summary>The stable diagnostic rule identifier for a bidirectional rule failure.</summary>
    public const string BidiRule = UnicodeIdna.BidiRule;
}
