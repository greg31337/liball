namespace version;

/// <summary>
/// Defines the supported maven dependency scope values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum MavenDependencyScope
{
    /// <summary>
    /// Uses compile behavior.
    /// </summary>
    Compile = 0,
    /// <summary>
    /// Uses provided behavior.
    /// </summary>
    Provided = 1,
    /// <summary>
    /// Uses runtime behavior.
    /// </summary>
    Runtime = 2,
    /// <summary>
    /// Uses test behavior.
    /// </summary>
    Test = 3,
    /// <summary>
    /// Uses system behavior.
    /// </summary>
    System = 4,
    /// <summary>
    /// Uses import behavior.
    /// </summary>
    Import = 5
}
