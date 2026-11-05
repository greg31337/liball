namespace countrycodes;

/// <summary>
/// Describes the assignment state of an ISO country code.
/// </summary>
public enum CodeAssignmentStatus
{
    /// <summary>
    /// The code is currently assigned.
    /// </summary>
    Assigned = 0,

    /// <summary>
    /// The code is exceptionally reserved by ISO 3166/MA.
    /// </summary>
    ExceptionallyReserved = 1,

    /// <summary>
    /// The code is transitionally reserved after removal or change.
    /// </summary>
    TransitionallyReserved = 2,

    /// <summary>
    /// The code is reserved without a fixed expiration date.
    /// </summary>
    IndeterminatelyReserved = 3,

    /// <summary>
    /// The code was formerly assigned.
    /// </summary>
    FormerlyAssigned = 4,

    /// <summary>
    /// The code belongs to a range reserved for private use.
    /// </summary>
    UserAssigned = 5,

    /// <summary>
    /// The well-formed code has no known assignment.
    /// </summary>
    Unassigned = 6
}
