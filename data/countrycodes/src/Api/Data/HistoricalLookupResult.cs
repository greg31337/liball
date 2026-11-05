namespace countrycodes;

/// <summary>
/// Holds the outcome of a historical-country or country-change lookup.
/// </summary>
public class HistoricalLookupResult
{
    /// <summary>
    /// The outcome status of the lookup.
    /// </summary>
    public LookupStatus Status;

    /// <summary>
    /// The matching current country when exactly one current record was found.
    /// </summary>
    public Country? Country;

    /// <summary>
    /// The matching historical country when exactly one record was found.
    /// </summary>
    public HistoricalCountry? HistoricalCountry;

    /// <summary>
    /// The matching country change when exactly one change was found.
    /// </summary>
    public CountryChange? CountryChange;

    /// <summary>
    /// All deterministic current-country candidates.
    /// </summary>
    public Country[] CountryCandidates;

    /// <summary>
    /// All deterministic historical-country candidates.
    /// </summary>
    public HistoricalCountry[] HistoricalCountryCandidates;

    /// <summary>
    /// All deterministic country-change candidates.
    /// </summary>
    public CountryChange[] CountryChangeCandidates;

    /// <summary>
    /// Creates a detached historical lookup result.
    /// </summary>
    /// <param name="status">The lookup outcome.</param>
    /// <param name="country">The unique current country, when available.</param>
    /// <param name="historicalCountry">The unique historical country, when available.</param>
    /// <param name="countryChange">The unique country change, when available.</param>
    /// <param name="countryCandidates">The current-country candidates.</param>
    /// <param name="historicalCountryCandidates">The historical-country candidates.</param>
    /// <param name="countryChangeCandidates">The country-change candidates.</param>
    public HistoricalLookupResult(
        LookupStatus status,
        Country? country,
        HistoricalCountry? historicalCountry,
        CountryChange? countryChange,
        Country[] countryCandidates,
        HistoricalCountry[] historicalCountryCandidates,
        CountryChange[] countryChangeCandidates)
    {
        Status = status;
        if (country == null)
        {
            Country = null;
        }
        else
        {
            Country = country.Copy();
        }

        if (historicalCountry == null)
        {
            HistoricalCountry = null;
        }
        else
        {
            HistoricalCountry = historicalCountry.Copy();
        }

        if (countryChange == null)
        {
            CountryChange = null;
        }
        else
        {
            CountryChange = countryChange.Copy();
        }

        CountryCandidates = global::countrycodes.Country.CopyArray(countryCandidates);
        HistoricalCountryCandidates = global::countrycodes.HistoricalCountry.CopyArray(
            historicalCountryCandidates);
        CountryChangeCandidates = global::countrycodes.CountryChange.CopyArray(
            countryChangeCandidates);
    }
}
