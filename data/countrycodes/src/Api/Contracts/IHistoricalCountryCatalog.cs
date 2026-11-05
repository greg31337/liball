namespace countrycodes;

/// <summary>
/// Defines historical country catalog operations.
/// </summary>
public interface IHistoricalCountryCatalog
{
    /// <summary>
    /// Finds a historical country by a former code.
    /// </summary>
    /// <param name="value">The former code to find.</param>
    /// <returns>The structured historical lookup outcome.</returns>
    HistoricalLookupResult FindHistoricalCountryByFormerCode(string value);

    /// <summary>
    /// Finds historical countries by a former name.
    /// </summary>
    /// <param name="value">The former name to find.</param>
    /// <returns>The structured historical lookup outcome.</returns>
    HistoricalLookupResult FindHistoricalCountryByName(string value);

    /// <summary>
    /// Finds an ISO 3166-3 country change by alpha-4 identity.
    /// </summary>
    /// <param name="value">The alpha-4 identity to find.</param>
    /// <returns>The structured historical lookup outcome.</returns>
    HistoricalLookupResult FindCountryChangeByAlpha4(string value);

    /// <summary>
    /// Gets all recorded changes involving a current or former identifier.
    /// </summary>
    /// <param name="value">The country identifier whose history is requested.</param>
    /// <returns>Detached country changes in chronological order.</returns>
    CountryChange[] GetCountryChangeHistory(string value);

    /// <summary>
    /// Resolves all deterministic current and historical successors of an identifier.
    /// </summary>
    /// <param name="value">The former identifier to resolve.</param>
    /// <returns>The structured successor resolution outcome.</returns>
    HistoricalLookupResult ResolveHistoricalSuccessors(string value);

    /// <summary>
    /// Gets all historical country records.
    /// </summary>
    /// <returns>Detached historical country records in stable alpha-4 order.</returns>
    HistoricalCountry[] GetHistoricalCountries();

    /// <summary>
    /// Gets historical country changes of the requested kind.
    /// </summary>
    /// <param name="kind">The country change kind to select.</param>
    /// <returns>Detached country change records in stable code order.</returns>
    CountryChange[] GetCountryChangesByKind(CountryChangeKind kind);
}
