namespace countrycodes;

/// <summary>
/// Holds the outcome of a subdivision lookup.
/// </summary>
public class SubdivisionLookupResult
{
    /// <summary>
    /// The outcome status of the lookup.
    /// </summary>
    public LookupStatus Status;

    /// <summary>
    /// The matching subdivision when exactly one record was found.
    /// </summary>
    public Subdivision? Subdivision;

    /// <summary>
    /// All deterministic candidates for an ambiguous lookup.
    /// </summary>
    public Subdivision[] Candidates;

    /// <summary>
    /// Creates a detached subdivision lookup result.
    /// </summary>
    /// <param name="status">The lookup outcome.</param>
    /// <param name="subdivision">The unique matching subdivision, when available.</param>
    /// <param name="candidates">The ambiguity candidates.</param>
    public SubdivisionLookupResult(
        LookupStatus status,
        Subdivision? subdivision,
        Subdivision[] candidates)
    {
        Status = status;
        if (subdivision == null)
        {
            Subdivision = null;
        }
        else
        {
            Subdivision = subdivision.Copy();
        }

        Candidates = global::countrycodes.Subdivision.CopyArray(candidates);
    }
}
