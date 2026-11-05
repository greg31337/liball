namespace fake;

/// <summary>Represents a synthetic exchange-rate-shaped value for an ordered currency pair.</summary>
/// <remarks>The value is fixture data, not a current or historical market quote.</remarks>
public class ExchangeRateRecord
{
    /// <summary>The ISO 4217 code for one unit of the base currency.</summary>
    public string BaseCurrencyCode = string.Empty;

    /// <summary>The ISO 4217 code for the quoted currency.</summary>
    public string QuoteCurrencyCode = string.Empty;

    /// <summary>The number of quoted currency units per one base currency unit.</summary>
    public decimal Rate;

    /// <summary>The decimal precision applied to <c>Rate</c>.</summary>
    public int DecimalPlaces;

    /// <summary>The rounding rule applied to <c>Rate</c>.</summary>
    public MoneyRoundingMode RoundingMode;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the value represents a market quote, always <c>false</c>.</summary>
    public bool RepresentsMarketQuote;

    internal ExchangeRateRecord()
    {
    }
}
