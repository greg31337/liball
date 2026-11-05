namespace version;

/// <summary>
/// Defines the supported version comparison values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum VersionComparisonMode
{
    /// <summary>
    /// Uses precedence behavior.
    /// </summary>
    Precedence = 0,
    /// <summary>
    /// Uses identity behavior.
    /// </summary>
    Identity = 1,
    /// <summary>
    /// Uses normalized identity behavior.
    /// </summary>
    NormalizedIdentity = 2,
    /// <summary>
    /// Uses dialect behavior.
    /// </summary>
    Dialect = 3,
    /// <summary>
    /// Uses stability aware behavior.
    /// </summary>
    StabilityAware = 4
}
