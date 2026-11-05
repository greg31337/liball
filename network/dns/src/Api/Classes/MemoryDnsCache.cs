using System.Collections.Generic;

namespace dns;

/// <summary>Stores bounded resolver results in process memory.</summary>
public class MemoryDnsCache : IDnsCache
{
    internal object SyncRoot = new object();
    internal List<DnsCacheItem> Entries = new List<DnsCacheItem>();
    internal int MaximumEntryCount = 1024;
    internal long NextAccessSequence;

    /// <summary>Creates an empty bounded in-memory DNS cache.</summary>
    public MemoryDnsCache()
    {
    }

    /// <summary>Looks up and ages one resolver cache key.</summary>
    /// <param name="request">The canonical name, type, and class cache key.</param>
    /// <param name="now">The current UTC and monotonic clock readings.</param>
    /// <returns>The cache entry and its current freshness state.</returns>
    public DnsCacheLookup Lookup(DnsResolveRequest request, DnsClockValue now)
    {
        lock (this.SyncRoot)
        {
            return CacheIndex.Lookup(request, now, this);
        }
    }

    /// <summary>Stores or replaces one cache entry.</summary>
    /// <param name="entry">The positive, negative, or failure cache entry.</param>
    public void Store(DnsCacheEntry entry)
    {
        lock (this.SyncRoot)
        {
            CacheIndex.Store(entry, this);
        }
    }

    /// <summary>Removes all resolver cache entries.</summary>
    public void Flush()
    {
        lock (this.SyncRoot)
        {
            CacheIndex.Flush(this);
        }
    }

    /// <summary>Removes entries at or below one DNS name.</summary>
    /// <param name="name">The root name of the invalidated subtree.</param>
    public void InvalidateSubtree(DnsName name)
    {
        lock (this.SyncRoot)
        {
            CacheIndex.InvalidateSubtree(name, this);
        }
    }
}
