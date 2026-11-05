namespace currencycodes;

/// <summary>
/// Identifies the classification of a territory or special entity.
/// </summary>
public enum TerritoryKind
{
    /// <summary>
    /// A sovereign country.
    /// </summary>
    Country = 0,

    /// <summary>
    /// A dependent territory.
    /// </summary>
    DependentTerritory = 1,

    /// <summary>
    /// A supranational organization or area.
    /// </summary>
    Supranational = 2,

    /// <summary>
    /// A monetary institution.
    /// </summary>
    Institution = 3,

    /// <summary>
    /// Another special entity classification.
    /// </summary>
    Other = 4
}
