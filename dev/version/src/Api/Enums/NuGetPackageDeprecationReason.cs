namespace version;

/// <summary>
/// Defines the supported nuget package deprecation reason values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum NuGetPackageDeprecationReason
{
    /// <summary>
    /// Uses other behavior.
    /// </summary>
    Other = 0,
    /// <summary>
    /// Uses legacy behavior.
    /// </summary>
    Legacy = 1,
    /// <summary>
    /// Uses critical bugs behavior.
    /// </summary>
    CriticalBugs = 2
}
