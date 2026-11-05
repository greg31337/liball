namespace version;

/// <summary>
/// Defines the supported cargo dependency values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum CargoDependencyKind
{
    /// <summary>
    /// Uses normal behavior.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Uses development behavior.
    /// </summary>
    Development = 1,
    /// <summary>
    /// Uses build behavior.
    /// </summary>
    Build = 2
}


