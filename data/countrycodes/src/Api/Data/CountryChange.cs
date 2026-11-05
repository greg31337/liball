namespace countrycodes;

/// <summary>
/// Holds a detached public snapshot of an ISO 3166-3 country change.
/// </summary>
public class CountryChange
{
    /// <summary>
    /// The ISO 3166-3 alpha-4 identity of the change.
    /// </summary>
    public string Alpha4;

    /// <summary>
    /// The kind of historical change.
    /// </summary>
    public CountryChangeKind Kind;

    /// <summary>
    /// The predecessor identifiers participating in the change.
    /// </summary>
    public string[] PredecessorIdentifiers;

    /// <summary>
    /// The successor identifiers produced by the change.
    /// </summary>
    public string[] SuccessorIdentifiers;

    /// <summary>
    /// The canonical effective date when one is available.
    /// </summary>
    public string? EffectiveDate;

    /// <summary>
    /// Creates a detached country change snapshot.
    /// </summary>
    /// <param name="alpha4">The ISO 3166-3 alpha-4 identity.</param>
    /// <param name="kind">The kind of change.</param>
    /// <param name="predecessorIdentifiers">The predecessor identifiers.</param>
    /// <param name="successorIdentifiers">The successor identifiers.</param>
    /// <param name="effectiveDate">The effective date, when available.</param>
    public CountryChange(
        string alpha4,
        CountryChangeKind kind,
        string[] predecessorIdentifiers,
        string[] successorIdentifiers,
        string? effectiveDate)
    {
        Alpha4 = alpha4;
        Kind = kind;
        PredecessorIdentifiers = (string[])predecessorIdentifiers.Clone();
        SuccessorIdentifiers = (string[])successorIdentifiers.Clone();
        EffectiveDate = effectiveDate;
    }

    internal CountryChange Copy()
    {
        return new CountryChange(
            Alpha4,
            Kind,
            PredecessorIdentifiers,
            SuccessorIdentifiers,
            EffectiveDate);
    }

    internal static CountryChange[] CopyArray(CountryChange[] values)
    {
        CountryChange[] copies = new CountryChange[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copies[index] = values[index].Copy();
            index += 1;
        }

        return copies;
    }
}
