namespace fake;

/// <summary>
/// Selects whether missing locale data may fall back to another locale.
/// </summary>
public enum StrictnessMode
{
    /// <summary>
    /// Allows the configured deterministic locale fallback chain.
    /// </summary>
    AllowFallback = 0,

    /// <summary>
    /// Rejects missing exact-locale fields.
    /// </summary>
    Strict = 1
}
