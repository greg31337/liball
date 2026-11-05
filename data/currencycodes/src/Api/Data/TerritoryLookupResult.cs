namespace currencycodes;

/// <summary>
/// Stores the structured outcome of a territory lookup.
/// </summary>
public class TerritoryLookupResult
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
    /// Stores the primary matching territory when one is available.
    /// </summary>
    public TerritoryInfo? Match = null;

    /// <summary>
    /// Stores every matching territory in deterministic order.
    /// </summary>
    public TerritoryInfo[] Matches = new TerritoryInfo[0];
}
