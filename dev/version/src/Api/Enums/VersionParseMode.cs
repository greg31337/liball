namespace version;

/// <summary>
/// Defines the supported version parse values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum VersionParseMode
{
    /// <summary>
    /// Uses strict behavior.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Uses loose behavior.
    /// </summary>
    Loose = 1,
    /// <summary>
    /// Uses coerce behavior.
    /// </summary>
    Coerce = 2,
    /// <summary>
    /// Uses partial behavior.
    /// </summary>
    Partial = 3,
    /// <summary>
    /// Uses dialect specific behavior.
    /// </summary>
    DialectSpecific = 4
}
