namespace currencycodes;

/// <summary>
/// Defines exception-less currency-code normalization operations.
/// </summary>
public interface ICurrencyNormalizer
{
    /// <summary>
    /// Normalizes an alphabetic currency code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    NormalizationResult NormalizeCurrencyAlphabeticCode(string? code);

    /// <summary>
    /// Normalizes one to three ASCII numeric currency-code digits to canonical three-digit text.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    NormalizationResult NormalizeCurrencyNumericCode(string? code);

    /// <summary>
    /// Normalizes an alpha-2 territory code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    NormalizationResult NormalizeTerritoryAlpha2Code(string? code);

    /// <summary>
    /// Normalizes an alpha-3 territory code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    NormalizationResult NormalizeTerritoryAlpha3Code(string? code);
}
