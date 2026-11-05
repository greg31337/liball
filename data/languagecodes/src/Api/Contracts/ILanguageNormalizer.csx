namespace languagecodes;

/// <summary>Defines deterministic ISO code conversion and BCP 47 language-tag normalization.</summary>
public interface ILanguageNormalizer
{
    /// <summary>Normalizes an ISO language code using explicit casing, retirement, and macrolanguage policies.</summary>
    /// <param name="code">The language code to normalize.</param>
    /// <param name="options">The normalization policies.</param>
    /// <returns>The normalization outcome and ordered audit trail.</returns>
    LanguageNormalizationResult NormalizeCode(string? code, LanguageNormalizationOptions options);

    /// <summary>Converts a recognized ISO language code to the target form selected in the options.</summary>
    /// <param name="code">The language code to convert.</param>
    /// <param name="options">The target-form and normalization policies.</param>
    /// <returns>The conversion outcome, including unavailable-target diagnostics.</returns>
    LanguageNormalizationResult ConvertCode(string? code, LanguageNormalizationOptions options);

    /// <summary>Normalizes and canonicalizes a BCP 47 language tag using explicit IANA policies.</summary>
    /// <param name="tag">The language tag to normalize.</param>
    /// <param name="options">The language-tag normalization policies.</param>
    /// <returns>The normalization outcome and ordered audit trail.</returns>
    LanguageNormalizationResult NormalizeTag(string? tag, LanguageNormalizationOptions options);
}
