namespace dns;

/// <summary>Stores and retrieves bounded DNS resolver cache entries.</summary>
public interface IDnsCache
{
    /// <summary>Looks up and ages one resolver cache key.</summary>
    /// <param name="request">The canonical name, type, and class cache key.</param>
    /// <param name="now">The current UTC and monotonic clock readings.</param>
    /// <returns>The cache entry and its current freshness state.</returns>
    DnsCacheLookup Lookup(DnsResolveRequest request, DnsClockValue now);

    /// <summary>Stores or replaces one cache entry.</summary>
    /// <param name="entry">The positive, negative, or failure cache entry.</param>
    void Store(DnsCacheEntry entry);

    /// <summary>Removes all resolver cache entries.</summary>
    void Flush();

    /// <summary>Removes entries at or below one DNS name.</summary>
    /// <param name="name">The root name of the invalidated subtree.</param>
    void InvalidateSubtree(DnsName name);
}
