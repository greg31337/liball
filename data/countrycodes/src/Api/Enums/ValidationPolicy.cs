namespace countrycodes;

/// <summary>
/// Selects the catalog rules applied after syntax validation.
/// </summary>
public enum ValidationPolicy
{
    /// <summary>
    /// Validate syntax without requiring catalog membership.
    /// </summary>
    SyntaxOnly = 0,

    /// <summary>
    /// Accept only currently assigned identifiers.
    /// </summary>
    AssignedOnly = 1,

    /// <summary>
    /// Accept any identifier in a known ISO assignment state.
    /// </summary>
    AnyKnownIso = 2,

    /// <summary>
    /// Accept currently assigned and historical identifiers.
    /// </summary>
    AllowHistorical = 3,

    /// <summary>
    /// Accept currently assigned and user-assigned identifiers.
    /// </summary>
    AllowUserAssigned = 4
}
