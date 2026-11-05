namespace countrycodes;

/// <summary>
/// Defines UN M49 geographic region catalog operations.
/// </summary>
public interface IGeographicRegionCatalog
{
    /// <summary>
    /// Gets all aggregate UN M49 geographic regions.
    /// </summary>
    /// <returns>Detached regions in stable M49-code order.</returns>
    GeographicRegion[] GetGeographicRegions();

    /// <summary>
    /// Gets geographic regions at the requested hierarchy level.
    /// </summary>
    /// <param name="level">The hierarchy level to select.</param>
    /// <returns>Detached region records in stable M49-code order.</returns>
    GeographicRegion[] GetGeographicRegionsByLevel(GeographicRegionLevel level);

    /// <summary>
    /// Finds an aggregate geographic region by UN M49 code.
    /// </summary>
    /// <param name="m49Code">The zero-padded UN M49 code.</param>
    /// <returns>The detached region, or null when it is not present.</returns>
    GeographicRegion? FindGeographicRegionByM49(string m49Code);

    /// <summary>
    /// Gets the ancestry of an aggregate geographic region.
    /// </summary>
    /// <param name="m49Code">The zero-padded UN M49 region code.</param>
    /// <returns>Detached ancestors ordered from parent to world.</returns>
    GeographicRegion[] GetGeographicRegionAncestry(string m49Code);

    /// <summary>
    /// Gets the geographic ancestry assigned to a country.
    /// </summary>
    /// <param name="countryAlpha2">The country's canonical alpha-2 code.</param>
    /// <returns>Detached regions ordered from world to the most specific region.</returns>
    GeographicRegion[] GetGeographicRegionsForCountry(string countryAlpha2);

    /// <summary>
    /// Gets countries belonging to a geographic region and its descendants.
    /// </summary>
    /// <param name="m49Code">The zero-padded UN M49 region code.</param>
    /// <returns>Detached countries in stable alpha-2 order.</returns>
    Country[] GetCountriesForGeographicRegion(string m49Code);
}
