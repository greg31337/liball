namespace currencycodes;

/// <summary>
/// Selects the currency-status scope used by catalog and lookup operations.
/// </summary>
public enum CurrencySet
{
    /// <summary>
    /// Include only currently assigned currency designations.
    /// </summary>
    ActiveOnly = 0,

    /// <summary>
    /// Include only withdrawn currency designations.
    /// </summary>
    HistoricalOnly = 1,

    /// <summary>
    /// Include current and withdrawn currency designations.
    /// </summary>
    All = 2
}
