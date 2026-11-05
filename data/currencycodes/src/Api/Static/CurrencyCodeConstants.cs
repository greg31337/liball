namespace currencycodes;

/// <summary>
/// Defines the canonical currency and territory code lengths and supported fraction scale.
/// </summary>
public static class CurrencyCodeConstants
{
    /// <summary>
    /// Stores the required alphabetic currency code length.
    /// </summary>
    public const int CurrencyAlphabeticCodeLength = 3;

    /// <summary>
    /// Stores the canonical numeric currency code length.
    /// </summary>
    public const int CurrencyNumericCodeLength = 3;

    /// <summary>
    /// Stores the alpha-2 territory code length.
    /// </summary>
    public const int TerritoryAlpha2CodeLength = 2;

    /// <summary>
    /// Stores the alpha-3 territory code length.
    /// </summary>
    public const int TerritoryAlpha3CodeLength = 3;

    /// <summary>
    /// Stores the canonical numeric territory code length.
    /// </summary>
    public const int TerritoryNumericCodeLength = 3;

    /// <summary>
    /// Stores the maximum fraction digit count supported by <see cref="decimal"/>.
    /// </summary>
    public const int MaximumFractionDigits = 28;
}
