namespace countrycodes;

/// <summary>
/// Defines country search operations.
/// </summary>
public interface ICountrySearch
{
    /// <summary>
    /// Searches for countries using explicit matching options.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="options">The matching options.</param>
    /// <returns>Detached country matches in deterministic rank order.</returns>
    CountrySearchMatch[] SearchCountries(string query, SearchOptions options);

    /// <summary>
    /// Searches for subdivisions using explicit matching options.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="options">The matching options.</param>
    /// <returns>Detached subdivision matches in deterministic rank order.</returns>
    SubdivisionSearchMatch[] SearchSubdivisions(string query, SearchOptions options);

    /// <summary>
    /// Searches for subdivisions within one country using explicit matching options.
    /// </summary>
    /// <param name="countryAlpha2">The owning country's alpha-2 code.</param>
    /// <param name="query">The search query.</param>
    /// <param name="options">The matching options.</param>
    /// <returns>Detached subdivision matches in deterministic rank order.</returns>
    SubdivisionSearchMatch[] SearchSubdivisionsInCountry(
        string countryAlpha2,
        string query,
        SearchOptions options);

    /// <summary>
    /// Discovers countries through matching subdivisions.
    /// </summary>
    /// <param name="query">The subdivision search query.</param>
    /// <param name="options">The matching options.</param>
    /// <returns>Detached subdivision-derived country matches in deterministic rank order.</returns>
    CountrySearchMatch[] DiscoverCountriesBySubdivision(
        string query,
        SearchOptions options);
}
