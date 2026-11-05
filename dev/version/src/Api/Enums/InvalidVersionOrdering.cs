namespace version;

/// <summary>
/// Defines the supported invalid version values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum InvalidVersionOrdering
{
    /// <summary>
    /// Uses reject behavior.
    /// </summary>
    Reject = 0,
    /// <summary>
    /// Uses sort before valid behavior.
    /// </summary>
    SortBeforeValid = 1,
    /// <summary>
    /// Uses sort after valid behavior.
    /// </summary>
    SortAfterValid = 2,
    /// <summary>
    /// Uses treat as equal behavior.
    /// </summary>
    TreatAsEqual = 3
}
