namespace version;

/// <summary>
/// Defines the supported range values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum RangeOperator
{
    /// <summary>
    /// Uses equal behavior.
    /// </summary>
    Equal = 0,
    /// <summary>
    /// Uses not equal behavior.
    /// </summary>
    NotEqual = 1,
    /// <summary>
    /// Uses greater than behavior.
    /// </summary>
    GreaterThan = 2,
    /// <summary>
    /// Uses greater than or equal behavior.
    /// </summary>
    GreaterThanOrEqual = 3,
    /// <summary>
    /// Uses less than behavior.
    /// </summary>
    LessThan = 4,
    /// <summary>
    /// Uses less than or equal behavior.
    /// </summary>
    LessThanOrEqual = 5
}
