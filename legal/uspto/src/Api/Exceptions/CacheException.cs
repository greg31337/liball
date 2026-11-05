namespace uspto;

/// <summary>
/// Reports a response-cache read, write, validation, or invalidation failure.
/// </summary>
public class CacheException : global::System.Exception
{
    /// <summary>The credential-free canonical cache key or key digest.</summary>
    public string? CacheKey;

    /// <summary>The local cache entry path, when applicable.</summary>
    public string? CachePath;

    /// <summary>Initializes a cache exception and its local entry context.</summary>
    /// <param name="message">The cache failure message.</param>
    /// <param name="cacheKey">The credential-free canonical key or key digest.</param>
    /// <param name="cachePath">The local cache entry path.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public CacheException(string message, string? cacheKey, string? cachePath, global::System.Exception? innerException)
        : base(message, innerException)
    {
        CacheKey = cacheKey;
        CachePath = cachePath;
    }
}
