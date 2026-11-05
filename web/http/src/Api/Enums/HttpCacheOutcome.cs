namespace http;

/// <summary>
/// Identifies a cache-policy decision for an HTTP request or response.
/// </summary>
public enum HttpCacheOutcome
{
    /// <summary>
    /// The response can be stored.
    /// </summary>
    Store = 0,

    /// <summary>
    /// The response must not be stored.
    /// </summary>
    DoNotStore = 1,

    /// <summary>
    /// A fresh stored response can be served.
    /// </summary>
    ServeFresh = 2,

    /// <summary>
    /// A stale stored response can be served.
    /// </summary>
    ServeStale = 3,

    /// <summary>
    /// The stored response requires validation.
    /// </summary>
    Validate = 4,

    /// <summary>
    /// An only-if-cached request has no usable stored response.
    /// </summary>
    OnlyIfCachedMiss = 5,

    /// <summary>
    /// One or more stored responses should be invalidated.
    /// </summary>
    Invalidate = 6,

    /// <summary>
    /// The response or cache entry cannot be used for the operation.
    /// </summary>
    Unusable = 7
}
