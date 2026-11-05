namespace countrycodes;

/// <summary>
/// Defines country-code normalization operations.
/// </summary>
public interface ICountryCodeNormalizer
{
    /// <summary>
    /// Normalizes an alpha-2 country code.
    /// </summary>
    /// <param name="value">The alpha-2 value to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryAlpha2(string value, bool permissive);

    /// <summary>
    /// Normalizes an alpha-2 country code span.
    /// </summary>
    /// <param name="value">The alpha-2 span to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryAlpha2Span(
        global::System.ReadOnlySpan<char> value,
        bool permissive);

    /// <summary>
    /// Normalizes an alpha-3 country code.
    /// </summary>
    /// <param name="value">The alpha-3 value to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryAlpha3(string value, bool permissive);

    /// <summary>
    /// Normalizes an alpha-3 country code span.
    /// </summary>
    /// <param name="value">The alpha-3 span to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryAlpha3Span(
        global::System.ReadOnlySpan<char> value,
        bool permissive);

    /// <summary>
    /// Normalizes a zero-padded numeric-3 country code.
    /// </summary>
    /// <param name="value">The numeric-3 value to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryNumericCode(string value, bool permissive);

    /// <summary>
    /// Normalizes a zero-padded numeric-3 country code span.
    /// </summary>
    /// <param name="value">The numeric-3 span to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryNumericCodeSpan(
        global::System.ReadOnlySpan<char> value,
        bool permissive);

    /// <summary>
    /// Normalizes an integer numeric country value to numeric-3 text.
    /// </summary>
    /// <param name="value">The integer numeric value to normalize.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryNumericValue(int value);

    /// <summary>
    /// Normalizes an ISO 3166-3 alpha-4 code.
    /// </summary>
    /// <param name="value">The alpha-4 value to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryAlpha4(string value, bool permissive);

    /// <summary>
    /// Normalizes an ISO 3166-3 alpha-4 code span.
    /// </summary>
    /// <param name="value">The alpha-4 span to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeCountryAlpha4Span(
        global::System.ReadOnlySpan<char> value,
        bool permissive);

    /// <summary>
    /// Normalizes an automatically detected country identifier.
    /// </summary>
    /// <param name="value">The country identifier to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeAnyCountryIdentifier(string value, bool permissive);

    /// <summary>
    /// Normalizes an automatically detected country identifier span.
    /// </summary>
    /// <param name="value">The country identifier span to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeAnyCountryIdentifierSpan(
        global::System.ReadOnlySpan<char> value,
        bool permissive);

    /// <summary>
    /// Normalizes a full subdivision code.
    /// </summary>
    /// <param name="value">The subdivision code to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeSubdivisionCode(string value, bool permissive);

    /// <summary>
    /// Normalizes a full subdivision code span.
    /// </summary>
    /// <param name="value">The subdivision code span to normalize.</param>
    /// <param name="permissive">Whether permissive normalization rules are enabled.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeSubdivisionCodeSpan(
        global::System.ReadOnlySpan<char> value,
        bool permissive);

    /// <summary>
    /// Normalizes a BCP 47 locale tag.
    /// </summary>
    /// <param name="value">The locale tag to normalize.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeLocaleTag(string value);

    /// <summary>
    /// Normalizes a display name into an invariant matching key.
    /// </summary>
    /// <param name="value">The display name to normalize.</param>
    /// <param name="foldDiacritics">Whether diacritics are folded.</param>
    /// <param name="foldPunctuation">Whether punctuation is folded.</param>
    /// <returns>The structured normalization outcome.</returns>
    CodeNormalizationResult NormalizeNameKey(
        string value,
        bool foldDiacritics,
        bool foldPunctuation);
}
