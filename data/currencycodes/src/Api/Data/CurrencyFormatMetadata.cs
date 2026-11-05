namespace currencycodes;

/// <summary>
/// Stores ISO minor-unit and CLDR standard and cash formatting rules for a currency.
/// </summary>
public class CurrencyFormatMetadata
{
    /// <summary>
    /// Stores the canonical alphabetic currency code.
    /// </summary>
    public string CurrencyCode = string.Empty;

    /// <summary>
    /// Indicates whether ISO minor-unit metadata is available.
    /// </summary>
    public bool HasIsoMinorUnit = false;

    /// <summary>
    /// Stores the ISO minor-unit digit count when <see cref="HasIsoMinorUnit"/> is <c>true</c>.
    /// </summary>
    public int IsoMinorUnitDigits = 0;

    /// <summary>
    /// Stores ten raised to <see cref="IsoMinorUnitDigits"/> when <see cref="HasIsoMinorUnit"/> is <c>true</c>.
    /// </summary>
    public decimal IsoMinorUnitFactor = 0m;

    /// <summary>
    /// Indicates whether a resolved CLDR standard-format rule is available.
    /// </summary>
    public bool HasStandardFormat = false;

    /// <summary>
    /// Stores the standard-format fraction digit count when <see cref="HasStandardFormat"/> is <c>true</c>.
    /// </summary>
    public int StandardFormatDigits = 0;

    /// <summary>
    /// Stores the standard-format integer rounding increment at the selected scale.
    /// </summary>
    public int StandardRoundingIncrement = 0;

    /// <summary>
    /// Stores ten raised to <see cref="StandardFormatDigits"/> when <see cref="HasStandardFormat"/> is <c>true</c>.
    /// </summary>
    public decimal StandardFormatFactor = 0m;

    /// <summary>
    /// Indicates whether a resolved CLDR cash rule is available.
    /// </summary>
    public bool HasCash = false;

    /// <summary>
    /// Stores the cash fraction digit count when <see cref="HasCash"/> is <c>true</c>.
    /// </summary>
    public int CashDigits = 0;

    /// <summary>
    /// Stores the cash integer rounding increment at the selected scale.
    /// </summary>
    public int CashRoundingIncrement = 0;

    /// <summary>
    /// Stores ten raised to <see cref="CashDigits"/> when <see cref="HasCash"/> is <c>true</c>.
    /// </summary>
    public decimal CashFactor = 0m;

    /// <summary>
    /// Stores the pinned source version used to produce the resolved metadata.
    /// </summary>
    public string SourceVersion = string.Empty;
}
