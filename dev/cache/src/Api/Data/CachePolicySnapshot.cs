namespace cache;

/// <summary>
/// Contains normalized cache policy settings, current capacity measurements, and diagnostic key orderings.
/// </summary>
/// <remarks>The arrays are copies created by <see cref="Cache.Policy"/>. Changing them does not change the cache.</remarks>
public class CachePolicySnapshot
{
    /// <summary>Stores the cache's normalized diagnostic name.</summary>
    public string Name;
    /// <summary>Indicates whether the default absolute expiration is active.</summary>
    public bool HasDefaultAbsoluteExpiration;
    /// <summary>Stores the normalized default fixed UTC expiration boundary.</summary>
    public long DefaultAbsoluteExpirationUtcTicks;
    /// <summary>Indicates whether the default write-relative time to live is active.</summary>
    public bool HasDefaultTimeToLive;
    /// <summary>Stores the normalized default time-to-live duration in .NET ticks.</summary>
    public long DefaultTimeToLiveTicks;
    /// <summary>Indicates whether the default idle expiration is active.</summary>
    public bool HasDefaultSlidingExpiration;
    /// <summary>Stores the normalized default sliding duration in .NET ticks.</summary>
    public long DefaultSlidingExpirationTicks;
    /// <summary>Indicates whether entry count is limited.</summary>
    public bool HasMaximumEntries;
    /// <summary>Stores the normalized maximum entry count.</summary>
    public long MaximumEntries;
    /// <summary>Indicates whether total entry weight is limited.</summary>
    public bool HasMaximumWeight;
    /// <summary>Stores the normalized maximum total weight.</summary>
    public long MaximumWeight;
    /// <summary>Stores the active capacity eviction algorithm.</summary>
    public CacheEvictionPolicy EvictionPolicy;
    /// <summary>Stores the priority assigned to entries written without per-entry options.</summary>
    public CacheEntryPriority DefaultPriority;
    /// <summary>Indicates whether operation statistics are recorded.</summary>
    public bool RecordStats;
    /// <summary>Indicates whether stale-while-refresh reads are enabled.</summary>
    public bool StaleWhileRefresh;
    /// <summary>Indicates whether scheduled expiration cleanup is enabled.</summary>
    public bool HasCleanupInterval;
    /// <summary>Stores the normalized scheduled cleanup interval in .NET ticks.</summary>
    public long CleanupIntervalTicks;
    /// <summary>Stores the number of entries currently held, including expired entries not yet encountered.</summary>
    public long CurrentCount;
    /// <summary>Stores the normalized total weight currently held.</summary>
    public long CurrentWeight;
    /// <summary>Stores all keys from oldest to newest by creation tick, with write version breaking ties.</summary>
    public object[] OldestKeys;
    /// <summary>Stores all keys from hottest to coldest by access count, recency, and write version.</summary>
    public object[] HottestKeys;
    /// <summary>Stores expiring keys from earliest to latest effective expiration boundary.</summary>
    public object[] ExpiringSoonKeys;
    /// <summary>Stores the cache clock tick at which the snapshot was taken.</summary>
    public long SnapshotUtcTicks;

    internal CachePolicySnapshot()
    {
        Name = "";
        HasDefaultAbsoluteExpiration = false;
        DefaultAbsoluteExpirationUtcTicks = 0L;
        HasDefaultTimeToLive = false;
        DefaultTimeToLiveTicks = 0L;
        HasDefaultSlidingExpiration = false;
        DefaultSlidingExpirationTicks = 0L;
        HasMaximumEntries = false;
        MaximumEntries = 0L;
        HasMaximumWeight = false;
        MaximumWeight = 0L;
        EvictionPolicy = CacheEvictionPolicy.Lru;
        DefaultPriority = CacheEntryPriority.Normal;
        RecordStats = false;
        StaleWhileRefresh = false;
        HasCleanupInterval = false;
        CleanupIntervalTicks = 0L;
        CurrentCount = 0L;
        CurrentWeight = 0L;
        OldestKeys = new object[0];
        HottestKeys = new object[0];
        ExpiringSoonKeys = new object[0];
        SnapshotUtcTicks = 0L;
    }
}
