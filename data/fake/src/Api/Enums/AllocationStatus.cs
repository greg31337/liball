namespace fake;

/// <summary>
/// Describes what the library knows about value allocation or reservation.
/// </summary>
public enum AllocationStatus
{
    /// <summary>
    /// Allocation was not checked and is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// External allocation does not apply to this value contract.
    /// </summary>
    NotApplicable = 1,

    /// <summary>
    /// The value belongs to a project-defined fictional fixture namespace, not an external registry reservation.
    /// </summary>
    FictionalReserved = 2
}
