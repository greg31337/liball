namespace version;

/// <summary>
/// Defines the supported cargo resolver values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum CargoResolverVersion
{
    /// <summary>
    /// Uses version1 behavior.
    /// </summary>
    Version1 = 1,
    /// <summary>
    /// Uses version2 behavior.
    /// </summary>
    Version2 = 2,
    /// <summary>
    /// Uses version3 behavior.
    /// </summary>
    Version3 = 3
}


