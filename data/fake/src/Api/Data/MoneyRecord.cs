namespace fake;

/// <summary>
/// Represents a decimal monetary value and its separate locale-formatted display.
/// </summary>
public class MoneyRecord
{
    /// <summary>The rounded numeric value, independent of its display string.</summary>
    public decimal Amount;

    /// <summary>The three-letter ISO 4217 currency or funds code.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The three-digit ISO 4217 numeric code.</summary>
    public string CurrencyNumericCode = string.Empty;

    /// <summary>The English currency or funds name recorded in the bundled registry.</summary>
    public string CurrencyName = string.Empty;

    /// <summary>The locale symbol, or the alphabetic code when no matching locale symbol applies.</summary>
    public string CurrencySymbol = string.Empty;

    /// <summary>The applicable ISO minor units, or the explicit display precision when none apply.</summary>
    public int DecimalPlaces;

    /// <summary>Whether the registry defines minor units for the selected code.</summary>
    public bool HasMinorUnits;

    /// <summary>The rounding rule applied to <c>Amount</c>.</summary>
    public MoneyRoundingMode RoundingMode;

    /// <summary>The normalized requested locale used for display formatting.</summary>
    public string Locale = string.Empty;

    /// <summary>The locale-formatted representation kept separate from <c>Amount</c>.</summary>
    public string Display = string.Empty;

    internal MoneyRecord()
    {
    }
}
