namespace countrycodes;

/// <summary>
/// Identifies a level in the UN M49 geographic hierarchy.
/// </summary>
public enum GeographicRegionLevel
{
    /// <summary>
    /// The world root.
    /// </summary>
    World = 0,

    /// <summary>
    /// A geographic region.
    /// </summary>
    Region = 1,

    /// <summary>
    /// A geographic subregion.
    /// </summary>
    Subregion = 2,

    /// <summary>
    /// An intermediate geographic region.
    /// </summary>
    IntermediateRegion = 3,

    /// <summary>
    /// A country or area leaf in the hierarchy.
    /// </summary>
    CountryOrArea = 4
}
