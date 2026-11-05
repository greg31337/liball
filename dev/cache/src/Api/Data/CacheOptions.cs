using System;
using System.Collections.Generic;

namespace cache;

/// <summary>
/// Holds cache-wide defaults, capacity rules, callbacks, timing, and diagnostics settings.
/// </summary>
/// <remarks>
/// A <see cref="Cache"/> normalizes and copies this object during construction, so later changes do not reconfigure that cache. A matching <c>Has...</c> field must be enabled for each numeric limit or expiration value to take effect.
/// </remarks>
public class CacheOptions
{
    /// <summary>Stores the diagnostic name reported by <see cref="Cache.Policy"/> and removal notifications.</summary>
    public string? Name;
    /// <summary>Stores the comparer used for key lookup, or <c>null</c> to use the default object comparer.</summary>
    public IEqualityComparer<object>? KeyComparer;
    /// <summary>Indicates whether <see cref="DefaultAbsoluteExpirationUtcTicks"/> applies to writes without per-entry options.</summary>
    public bool HasDefaultAbsoluteExpiration;
    /// <summary>Stores the default fixed UTC tick boundary.</summary>
    public long DefaultAbsoluteExpirationUtcTicks;
    /// <summary>Indicates whether <see cref="DefaultTimeToLiveTicks"/> applies to writes without per-entry options.</summary>
    public bool HasDefaultTimeToLive;
    /// <summary>Stores the default duration, in .NET ticks after a write, before expiration.</summary>
    public long DefaultTimeToLiveTicks;
    /// <summary>Indicates whether <see cref="DefaultSlidingExpirationTicks"/> applies to writes without per-entry options.</summary>
    public bool HasDefaultSlidingExpiration;
    /// <summary>Stores the default idle duration, in .NET ticks after the last successful <see cref="Cache.Get"/>, before expiration.</summary>
    public long DefaultSlidingExpirationTicks;
    /// <summary>Indicates whether <see cref="MaximumEntries"/> limits the entry count.</summary>
    public bool HasMaximumEntries;
    /// <summary>Stores the maximum entry count. The limit is disabled unless it is positive and enabled.</summary>
    public long MaximumEntries;
    /// <summary>Indicates whether <see cref="MaximumWeight"/> limits total entry weight.</summary>
    public bool HasMaximumWeight;
    /// <summary>Stores the maximum total weight. The limit is disabled unless it is positive and enabled.</summary>
    public long MaximumWeight;
    /// <summary>Stores the algorithm used to select entries for capacity eviction.</summary>
    public CacheEvictionPolicy EvictionPolicy;
    /// <summary>Stores the priority assigned to writes that do not provide per-entry options.</summary>
    public CacheEntryPriority DefaultPriority;
    /// <summary>Stores the time source used for expiration and timestamps, or <c>null</c> for a system UTC clock.</summary>
    public ICacheClock? Clock;
    /// <summary>Stores the synchronous value loader used for cache misses and explicit refreshes.</summary>
    /// <remarks>Loader exceptions propagate to the caller that requested the value.</remarks>
    public Func<object, object?>? Loader;
    /// <summary>Stores the callback that calculates an entry's weight from its key and value.</summary>
    /// <remarks>The callback overrides per-entry weights. Its exceptions propagate, and non-positive results normalize to <c>1</c>.</remarks>
    public Func<object, object?, long>? Weigher;
    /// <summary>Stores the callback invoked after an entry is removed from internal state.</summary>
    /// <remarks>Listener exceptions propagate to direct callers; during scheduled cleanup they escape the timer callback.</remarks>
    public Action<CacheNotification>? RemovalListener;
    /// <summary>Indicates whether operation counters are recorded.</summary>
    public bool RecordStats;
    /// <summary>Indicates whether a background timer periodically calls <see cref="Cache.Cleanup"/>.</summary>
    public bool HasCleanupInterval;
    /// <summary>Stores the requested cleanup interval in .NET ticks.</summary>
    public long CleanupIntervalTicks;
    /// <summary>Indicates whether a loader result of <c>null</c> is stored as a found entry.</summary>
    public bool CacheNullLoaderValues;
    /// <summary>Indicates whether reads may return a refresh-eligible live entry while starting one background refresh for its key.</summary>
    public bool StaleWhileRefresh;

    /// <summary>Creates options for an unbounded, non-expiring cache with no callbacks, scheduled cleanup, or recorded statistics.</summary>
    public CacheOptions()
    {
        Name = "";
        KeyComparer = null;
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
        Clock = new CacheClock();
        Loader = null;
        Weigher = null;
        RemovalListener = null;
        RecordStats = false;
        HasCleanupInterval = false;
        CleanupIntervalTicks = 0L;
        CacheNullLoaderValues = false;
        StaleWhileRefresh = false;
    }

    /// <summary>Creates a fresh options object containing the library defaults.</summary>
    /// <returns>A new independently mutable options object.</returns>
    internal static CacheOptions Default()
    {
        return new CacheOptions();
    }
}
