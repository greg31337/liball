namespace unicode;

/// <summary>
/// Provides stable diagnostic rule identifiers for PDF Unicode diagnostics.
/// </summary>
public static class UnicodePdfDiagnosticRules
{
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Text Scalar.
    /// </summary>
    public const string InvalidTextScalarRule = UnicodePdfDiagnostics.InvalidTextScalarRule;
    /// <summary>
    /// The stable diagnostic rule identifier for needs Actual Text Preparation.
    /// </summary>
    public const string NeedsActualTextPreparationRule = UnicodePdfDiagnostics.NeedsActualTextPreparationRule;
    /// <summary>
    /// The stable diagnostic rule identifier for requires UTF-16 Be.
    /// </summary>
    public const string RequiresUtf16BeRule = UnicodePdfDiagnostics.RequiresUtf16BeRule;
    /// <summary>
    /// The stable diagnostic rule identifier for missing Unicode Map.
    /// </summary>
    public const string MissingUnicodeMapRule = UnicodePdfDiagnostics.MissingUnicodeMapRule;
    /// <summary>
    /// The stable diagnostic rule identifier for invalid Unicode Map.
    /// </summary>
    public const string InvalidUnicodeMapRule = UnicodePdfDiagnostics.InvalidUnicodeMapRule;
    /// <summary>
    /// The stable diagnostic rule identifier for accessibility text preparation.
    /// </summary>
    public const string NeedsAccessibilityTextPreparationRule = UnicodePdfDiagnostics.NeedsAccessibilityTextPreparationRule;
    /// <summary>
    /// The stable diagnostic rule identifier for prohibited accessibility scalars.
    /// </summary>
    public const string ProhibitedTextScalarRule = UnicodePdfDiagnostics.ProhibitedTextScalarRule;
    /// <summary>
    /// The stable diagnostic rule identifier for explicit replacement scalars.
    /// </summary>
    public const string ReplacementTextScalarRule = UnicodePdfDiagnostics.ReplacementTextScalarRule;
}
