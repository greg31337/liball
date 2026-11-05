namespace currencycodes;

/// <summary>
/// Stores the structured outcome of a currency lookup.
/// </summary>
public class CurrencyLookupResult
{
    /// <summary>
    /// Indicates whether the lookup produced at least one match.
    /// </summary>
    public bool Found = false;

    /// <summary>
    /// Identifies the lookup outcome category.
    /// </summary>
    public LookupError Error = LookupError.None;

    /// <summary>
    /// Stores the normalized lookup query when normalization succeeds.
    /// </summary>
    public string NormalizedQuery = string.Empty;

    /// <summary>
    /// Stores the primary matching currency when one is available.
    /// </summary>
    public CurrencyInfo? Match = null;

    /// <summary>
    /// Stores every matching currency in deterministic order.
    /// </summary>
    public CurrencyInfo[] Matches = new CurrencyInfo[0];
}
