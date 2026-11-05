using System;
using System.Collections.Generic;

namespace cache;

/// <summary>
/// Builds a <see cref="Cache"/> through chainable configuration methods.
/// </summary>
/// <remarks>
/// The builder owns one mutable <see cref="CacheOptions"/> instance. <see cref="Build"/> passes its current settings to a cache, which normalizes and copies them.
/// </remarks>
public class CacheBuilder : ICacheBuilder
{
    internal CacheOptions _options;

    /// <summary>Creates a builder initialized with the default <see cref="CacheOptions"/> settings.</summary>
    public CacheBuilder()
    {
        _options = new CacheOptions();
    }

    /// <summary>Creates a builder initialized with default settings.</summary>
    /// <returns>A new cache builder.</returns>
    internal static CacheBuilder Create()
    {
        return new CacheBuilder();
    }

    /// <summary>Replaces the builder's current settings.</summary>
    /// <param name="options">The settings to use, or <c>null</c> to restore library defaults.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithOptions(CacheOptions? options)
    {
        if (options == null)
        {
            _options = new CacheOptions();
        }
        else
        {
            _options = options;
        }
        return this;
    }

    /// <summary>Sets the diagnostic name copied into policy snapshots.</summary>
    /// <param name="name">The cache name. A <c>null</c> value is normalized to an empty string when built.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithName(string? name)
    {
        _options.Name = name;
        return this;
    }

    /// <summary>Sets the comparer used to match cache keys.</summary>
    /// <param name="comparer">The comparer, or <c>null</c> to use <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithKeyComparer(IEqualityComparer<object>? comparer)
    {
        _options.KeyComparer = comparer;
        return this;
    }

    /// <summary>Sets the time source used for expiration and timestamps.</summary>
    /// <param name="clock">The clock, or <c>null</c> to use a system UTC clock.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithClock(ICacheClock? clock)
    {
        _options.Clock = clock;
        return this;
    }

    /// <summary>Enables a maximum number of stored entries.</summary>
    /// <param name="maximumEntries">The requested limit. Non-positive values disable the limit when the cache is built.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithMaximumEntries(long maximumEntries)
    {
        _options.HasMaximumEntries = true;
        _options.MaximumEntries = maximumEntries;
        return this;
    }

    /// <summary>Enables a maximum total entry weight.</summary>
    /// <param name="maximumWeight">The requested weight limit. Non-positive values disable the limit when the cache is built.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithMaximumWeight(long maximumWeight)
    {
        _options.HasMaximumWeight = true;
        _options.MaximumWeight = maximumWeight;
        return this;
    }

    /// <summary>Sets the algorithm used to select entries when a count or weight limit requires eviction.</summary>
    /// <param name="policy">The requested policy. Unrecognized values normalize to <see cref="CacheEvictionPolicy.Lru"/> when built.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithEvictionPolicy(CacheEvictionPolicy policy)
    {
        _options.EvictionPolicy = policy;
        return this;
    }

    /// <summary>Sets the default write-relative expiration duration.</summary>
    /// <param name="ticks">The duration in .NET ticks. Negative values disable this default when built; zero expires at the write boundary.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithTimeToLive(long ticks)
    {
        _options.HasDefaultTimeToLive = true;
        _options.DefaultTimeToLiveTicks = ticks;
        return this;
    }

    /// <summary>Sets the default idle expiration duration.</summary>
    /// <param name="ticks">The idle duration in .NET ticks. Negative values disable this default when built; zero expires immediately.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithSlidingExpiration(long ticks)
    {
        _options.HasDefaultSlidingExpiration = true;
        _options.DefaultSlidingExpirationTicks = ticks;
        return this;
    }

    /// <summary>Sets the default fixed UTC expiration boundary.</summary>
    /// <param name="utcTicks">The UTC tick boundary. Negative values disable this default when built.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithAbsoluteExpiration(long utcTicks)
    {
        _options.HasDefaultAbsoluteExpiration = true;
        _options.DefaultAbsoluteExpirationUtcTicks = utcTicks;
        return this;
    }

    /// <summary>Sets the priority assigned to entries that do not supply per-entry options.</summary>
    /// <param name="priority">The default eviction priority. Unrecognized values normalize to <see cref="CacheEntryPriority.Normal"/>.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithDefaultPriority(CacheEntryPriority priority)
    {
        _options.DefaultPriority = priority;
        return this;
    }

    /// <summary>Sets the synchronous callback used by <see cref="Cache.GetOrLoad"/> and <see cref="Cache.Refresh"/>.</summary>
    /// <remarks>Loader exceptions propagate to the caller. Concurrent misses for the same key share one in-progress load.</remarks>
    /// <param name="loader">The loader callback, or <c>null</c> to disable loading.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithLoader(Func<object, object?>? loader)
    {
        _options.Loader = loader;
        return this;
    }

    /// <summary>Controls whether a loader result of <c>null</c> is stored.</summary>
    /// <param name="enabled"><c>true</c> to cache null loader results; otherwise they remain misses.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithCacheNullLoaderValues(bool enabled)
    {
        _options.CacheNullLoaderValues = enabled;
        return this;
    }

    /// <summary>Controls whether a refresh-eligible live entry may be returned while one background refresh for its key runs.</summary>
    /// <param name="enabled"><c>true</c> to enable stale-while-refresh reads.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithStaleWhileRefresh(bool enabled)
    {
        _options.StaleWhileRefresh = enabled;
        return this;
    }

    /// <summary>Sets a callback that computes entry weight from its key and value.</summary>
    /// <remarks>The callback overrides per-entry weights. Its exceptions propagate, and results less than or equal to zero normalize to <c>1</c>.</remarks>
    /// <param name="weigher">The weight callback, or <c>null</c> to use each entry's configured weight.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithWeigher(Func<object, object?, long>? weigher)
    {
        _options.Weigher = weigher;
        return this;
    }

    /// <summary>Sets a callback that receives removal notifications.</summary>
    /// <remarks>Notifications are delivered after internal state changes and outside the cache lock. Listener exceptions propagate to direct callers; during scheduled cleanup they escape the timer callback.</remarks>
    /// <param name="listener">The listener callback, or <c>null</c> to disable notifications.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithRemovalListener(Action<CacheNotification>? listener)
    {
        _options.RemovalListener = listener;
        return this;
    }

    /// <summary>Enables operation counters and derived rates in <see cref="Cache.Stats"/>.</summary>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithStats()
    {
        _options.RecordStats = true;
        return this;
    }

    /// <summary>Enables periodic removal of expired entries.</summary>
    /// <param name="ticks">The requested interval in .NET ticks. Non-positive values disable scheduling when built; positive sub-millisecond values run at one millisecond.</param>
    /// <returns>This builder for further configuration.</returns>
    public ICacheBuilder WithCleanupInterval(long ticks)
    {
        _options.HasCleanupInterval = true;
        _options.CleanupIntervalTicks = ticks;
        return this;
    }

    /// <summary>Creates a cache from the builder's current settings.</summary>
    /// <returns>A new cache whose normalized configuration is independent of later builder changes.</returns>
    public Cache Build()
    {
        return new Cache(_options);
    }
}
