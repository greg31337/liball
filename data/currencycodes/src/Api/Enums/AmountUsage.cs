namespace currencycodes;

/// <summary>
/// Selects the currency metadata used for amount rules.
/// </summary>
public enum AmountUsage
{
    /// <summary>
    /// Use the official ISO minor-unit digits.
    /// </summary>
    IsoMinorUnit = 0,

    /// <summary>
    /// Use the standard CLDR formatting digits and increment.
    /// </summary>
    StandardFormat = 1,

    /// <summary>
    /// Use the CLDR cash digits and rounding increment.
    /// </summary>
    Cash = 2
}
