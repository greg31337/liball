namespace version;

/// <summary>
/// Defines the supported nuget target framework values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum NuGetTargetFrameworkKind
{
    /// <summary>
    /// Uses unsupported behavior.
    /// </summary>
    Unsupported = 0,
    /// <summary>
    /// Uses net framework behavior.
    /// </summary>
    NetFramework = 1,
    /// <summary>
    /// Uses net core app behavior.
    /// </summary>
    NetCoreApp = 2,
    /// <summary>
    /// Uses net standard behavior.
    /// </summary>
    NetStandard = 3,
    /// <summary>
    /// Uses net behavior.
    /// </summary>
    Net = 4,
    /// <summary>
    /// Uses portable behavior.
    /// </summary>
    Portable = 5,
    /// <summary>
    /// Uses windows behavior.
    /// </summary>
    Windows = 6,
    /// <summary>
    /// Uses windows phone behavior.
    /// </summary>
    WindowsPhone = 7,
    /// <summary>
    /// Uses silverlight behavior.
    /// </summary>
    Silverlight = 8,
    /// <summary>
    /// Uses uap behavior.
    /// </summary>
    Uap = 9
}
