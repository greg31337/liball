namespace unicode;

/// <summary>
/// Provides stable diagnostic rule identifiers for PRECIS-style strings.
/// </summary>
public static class UnicodePrecisRules
{
    /// <summary>
    /// The stable diagnostic rule identifier for empty.
    /// </summary>
    public const string EmptyRule = UnicodePrecis.EmptyRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Scalar.
    /// </summary>
    public const string InvalidScalarRule = UnicodePrecis.InvalidScalarRule;
    /// <summary>
    /// The stable diagnostic rule identifier for disallowed Space.
    /// </summary>
    public const string DisallowedSpaceRule = UnicodePrecis.DisallowedSpaceRule;
    /// <summary>
    /// The stable diagnostic rule identifier for disallowed Control.
    /// </summary>
    public const string DisallowedControlRule = UnicodePrecis.DisallowedControlRule;
    /// <summary>
    /// The stable diagnostic rule identifier for disallowed Identifier.
    /// </summary>
    public const string DisallowedIdentifierRule = UnicodePrecis.DisallowedIdentifierRule;
    /// <summary>
    /// The stable diagnostic rule identifier for needs Preparation.
    /// </summary>
    public const string NeedsPreparationRule = UnicodePrecis.NeedsPreparationRule;
    /// <summary>
    /// The stable diagnostic rule identifier for a disallowed code point.
    /// </summary>
    public const string DisallowedCodePointRule = UnicodePrecis.DisallowedCodePointRule;
    /// <summary>
    /// The stable diagnostic rule identifier for a contextual rule failure.
    /// </summary>
    public const string ContextualRule = UnicodePrecis.ContextualRule;
    /// <summary>
    /// The stable diagnostic rule identifier for a bidirectional rule failure.
    /// </summary>
    public const string BidiRule = UnicodePrecis.BidiRule;
}
