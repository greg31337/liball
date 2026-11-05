namespace unicode;

/// <summary>
/// Provides stable diagnostic rule identifiers for Unicode identifiers.
/// </summary>
public static class UnicodeIdentifierRules
{
    /// <summary>
    /// The stable diagnostic rule identifier for empty.
    /// </summary>
    public const string EmptyRule = UnicodeIdentifiers.EmptyRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Start.
    /// </summary>
    public const string InvalidStartRule = UnicodeIdentifiers.InvalidStartRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Continue.
    /// </summary>
    public const string InvalidContinueRule = UnicodeIdentifiers.InvalidContinueRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Scalar.
    /// </summary>
    public const string InvalidScalarRule = UnicodeIdentifiers.InvalidScalarRule;
    /// <summary>
    /// The stable diagnostic rule identifier for pattern Syntax.
    /// </summary>
    public const string PatternSyntaxRule = UnicodeIdentifiers.PatternSyntaxRule;
    /// <summary>
    /// The stable diagnostic rule identifier for pattern White Space.
    /// </summary>
    public const string PatternWhiteSpaceRule = UnicodeIdentifiers.PatternWhiteSpaceRule;
    /// <summary>
    /// The stable diagnostic rule identifier for needs Normalization.
    /// </summary>
    public const string NeedsNormalizationRule = UnicodeIdentifiers.NeedsNormalizationRule;
}

