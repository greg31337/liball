namespace icc;

/// <summary>
/// Identifies how transform selection may fall back from a requested intent.
/// </summary>
public enum IccIntentFallback
{
    /// <summary>
    /// Requires an exact rendering-intent transform.
    /// </summary>
    None = 0,

    /// <summary>
    /// Permits a compatible colorimetric-intent fallback.
    /// </summary>
    AllowColorimetric = 1,

    /// <summary>
    /// Permits any standards-defined available-intent fallback.
    /// </summary>
    AllowAny = 2
}
