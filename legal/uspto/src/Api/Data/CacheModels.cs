namespace uspto;

/// <summary>
/// Configures reusable response caching.
/// </summary>
public class CachePolicy
{
    /// <summary>The cache stores that may be used.</summary>
    public CacheMode Mode = CacheMode.Disabled;

    /// <summary>The file-cache directory, or <c>null</c> when no directory is configured.</summary>
    public string? Directory = null;

    /// <summary>The fallback lifetime of a successful cache entry in seconds.</summary>
    public int LifetimeSeconds = UsptoDefaults.CacheLifetimeSeconds;

    /// <summary>The maximum number of entries retained by the memory cache.</summary>
    public int MaximumMemoryEntries = UsptoDefaults.MemoryCacheEntryCount;

    /// <summary>The maximum body size eligible for caching in bytes.</summary>
    public long MaximumEntryBytes = UsptoDefaults.MaximumResponseBytes;

    /// <summary>An optional maximum size for all file-cache entries in bytes.</summary>
    public long MaximumFileBytes = 0L;

    /// <summary>Whether <see cref="MaximumFileBytes"/> contains a configured limit.</summary>
    public bool HasMaximumFileBytes = false;

    /// <summary>Whether HTTP cache directives and validators are honored.</summary>
    public bool HonorHttpCaching = true;
}
