namespace countrycodes;

/// <summary>
/// Identifies the kind of change represented by an ISO 3166-3 record.
/// </summary>
public enum CountryChangeKind
{
    /// <summary>
    /// A significant country name or code change.
    /// </summary>
    SignificantChange = 0,

    /// <summary>
    /// Two or more predecessors merged.
    /// </summary>
    Merge = 1,

    /// <summary>
    /// A predecessor split into multiple successors.
    /// </summary>
    Split = 2,

    /// <summary>
    /// Another or unknown kind of change.
    /// </summary>
    OtherOrUnknown = 3
}
