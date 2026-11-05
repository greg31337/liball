namespace currencycodes;

/// <summary>
/// Stores public identity and classification data for a currency designation.
/// </summary>
public class CurrencyInfo
{
    /// <summary>
    /// Stores the canonical three-letter alphabetic currency code.
    /// </summary>
    public string AlphabeticCode = string.Empty;

    /// <summary>
    /// Indicates whether a numeric currency code is assigned.
    /// </summary>
    public bool HasNumericCode = false;

    /// <summary>
    /// Stores the canonical three-digit numeric currency code when <see cref="HasNumericCode"/> is <c>true</c>.
    /// </summary>
    public string NumericCode = string.Empty;

    /// <summary>
    /// Stores the integer view of <see cref="NumericCode"/> when <see cref="HasNumericCode"/> is <c>true</c>.
    /// </summary>
    public int NumericCodeValue = 0;

    /// <summary>
    /// Stores the official English currency designation.
    /// </summary>
    public string OfficialName = string.Empty;

    /// <summary>
    /// Indicates whether ISO supplies a minor-unit digit count.
    /// </summary>
    public bool HasIsoMinorUnit = false;

    /// <summary>
    /// Stores the ISO minor-unit digit count when <see cref="HasIsoMinorUnit"/> is <c>true</c>.
    /// </summary>
    public int IsoMinorUnitDigits = 0;

    /// <summary>
    /// Identifies whether the currency designation is current or historical.
    /// </summary>
    public CurrencyStatus Status = CurrencyStatus.Active;

    /// <summary>
    /// Identifies the purpose or classification of the currency designation.
    /// </summary>
    public CurrencyKind Kind = CurrencyKind.Unknown;

    /// <summary>
    /// Indicates whether ISO currency-list data supplies the identity fields.
    /// </summary>
    public bool IdentityFromIso = false;

    /// <summary>
    /// Indicates whether ISO historic-list data supplies the historical status.
    /// </summary>
    public bool HistoricalStatusFromIso = false;

    /// <summary>
    /// Indicates whether ISO fund data supplies the fund classification.
    /// </summary>
    public bool FundClassificationFromIso = false;

    /// <summary>
    /// Indicates whether CLDR supplies enrichment data for the currency.
    /// </summary>
    public bool EnrichedFromCldr = false;

    /// <summary>
    /// Stores the introduction date and its source precision.
    /// </summary>
    public PartialDate IntroductionDate = new PartialDate();

    /// <summary>
    /// Stores the withdrawal date and its source precision.
    /// </summary>
    public PartialDate WithdrawalDate = new PartialDate();
}
