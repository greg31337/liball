namespace version;

/// <summary>
/// Defines the supported version values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum VersionRelation
{
    /// <summary>
    /// Uses less than behavior.
    /// </summary>
    LessThan = -1,
    /// <summary>
    /// Uses equal to behavior.
    /// </summary>
    EqualTo = 0,
    /// <summary>
    /// Uses greater than behavior.
    /// </summary>
    GreaterThan = 1
}
