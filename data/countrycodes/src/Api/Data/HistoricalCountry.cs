namespace countrycodes;

/// <summary>
/// Holds a detached public snapshot of a former country assignment.
/// </summary>
public class HistoricalCountry
{
    /// <summary>
    /// The former alpha-2 code when one is available.
    /// </summary>
    public string? Alpha2;

    /// <summary>
    /// The former alpha-3 code when one is available.
    /// </summary>
    public string? Alpha3;

    /// <summary>
    /// The ISO 3166-3 alpha-4 identity.
    /// </summary>
    public string Alpha4;

    /// <summary>
    /// Indicates whether numeric code data is available.
    /// </summary>
    public bool HasNumericValue;

    /// <summary>
    /// The former zero-padded numeric code when one is available.
    /// </summary>
    public string? NumericText;

    /// <summary>
    /// The former numeric code value when <see cref="HasNumericValue"/> is true.
    /// </summary>
    public int NumericValue;

    /// <summary>
    /// The former country name.
    /// </summary>
    public string Name;

    /// <summary>
    /// The canonical withdrawal date when one is available.
    /// </summary>
    public string? WithdrawalDate;

    /// <summary>
    /// The assignment status of the former code.
    /// </summary>
    public CodeAssignmentStatus AssignmentStatus;

    /// <summary>
    /// The predecessor identifiers linked to the historical record.
    /// </summary>
    public string[] PredecessorIdentifiers;

    /// <summary>
    /// The successor identifiers linked to the historical record.
    /// </summary>
    public string[] SuccessorIdentifiers;

    /// <summary>
    /// The country changes linked to the historical record.
    /// </summary>
    public CountryChange[] Changes;

    /// <summary>
    /// Creates a detached historical-country snapshot.
    /// </summary>
    /// <param name="alpha2">The former alpha-2 code, when available.</param>
    /// <param name="alpha3">The former alpha-3 code, when available.</param>
    /// <param name="alpha4">The ISO 3166-3 alpha-4 identity.</param>
    /// <param name="hasNumericValue">Whether numeric code data is available.</param>
    /// <param name="numericText">The zero-padded numeric code, when available.</param>
    /// <param name="numericValue">The numeric value when available.</param>
    /// <param name="name">The former country name.</param>
    /// <param name="withdrawalDate">The withdrawal date, when available.</param>
    /// <param name="assignmentStatus">The former assignment status.</param>
    /// <param name="predecessorIdentifiers">The predecessor identifiers.</param>
    /// <param name="successorIdentifiers">The successor identifiers.</param>
    /// <param name="changes">The linked country changes.</param>
    public HistoricalCountry(
        string? alpha2,
        string? alpha3,
        string alpha4,
        bool hasNumericValue,
        string? numericText,
        int numericValue,
        string name,
        string? withdrawalDate,
        CodeAssignmentStatus assignmentStatus,
        string[] predecessorIdentifiers,
        string[] successorIdentifiers,
        CountryChange[] changes)
    {
        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Alpha4 = alpha4;
        HasNumericValue = hasNumericValue;
        NumericText = numericText;
        NumericValue = numericValue;
        Name = name;
        WithdrawalDate = withdrawalDate;
        AssignmentStatus = assignmentStatus;
        PredecessorIdentifiers = (string[])predecessorIdentifiers.Clone();
        SuccessorIdentifiers = (string[])successorIdentifiers.Clone();
        Changes = CountryChange.CopyArray(changes);
    }

    internal HistoricalCountry Copy()
    {
        return new HistoricalCountry(
            Alpha2,
            Alpha3,
            Alpha4,
            HasNumericValue,
            NumericText,
            NumericValue,
            Name,
            WithdrawalDate,
            AssignmentStatus,
            PredecessorIdentifiers,
            SuccessorIdentifiers,
            Changes);
    }

    internal static HistoricalCountry[] CopyArray(HistoricalCountry[] values)
    {
        HistoricalCountry[] copies = new HistoricalCountry[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copies[index] = values[index].Copy();
            index += 1;
        }

        return copies;
    }
}
