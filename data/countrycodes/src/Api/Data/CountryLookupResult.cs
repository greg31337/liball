namespace countrycodes;

/// <summary>
/// Holds the outcome of a country lookup.
/// </summary>
public class CountryLookupResult
{
    /// <summary>
    /// The outcome status of the lookup.
    /// </summary>
    public LookupStatus Status;

    /// <summary>
    /// The matching country when exactly one record was found.
    /// </summary>
    public Country? Country;

    /// <summary>
    /// All deterministic candidates for an ambiguous lookup.
    /// </summary>
    public Country[] Candidates;

    /// <summary>
    /// Creates a detached country lookup result.
    /// </summary>
    /// <param name="status">The lookup outcome.</param>
    /// <param name="country">The unique matching country, when available.</param>
    /// <param name="candidates">The ambiguity candidates.</param>
    public CountryLookupResult(
        LookupStatus status,
        Country? country,
        Country[] candidates)
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

        Candidates = global::countrycodes.Country.CopyArray(candidates);
    }
}
