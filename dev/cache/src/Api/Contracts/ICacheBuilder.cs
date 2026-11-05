using System;
using System.Collections.Generic;

namespace cache;

/// <summary>
/// Defines chainable configuration operations for building a cache.
/// </summary>
public interface ICacheBuilder
{
    /// <summary>Replaces the builder's current settings.</summary>
    /// <param name="options">The settings to use, or <c>null</c> to restore library defaults.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithOptions(CacheOptions? options);

    /// <summary>Sets the diagnostic name copied into policy snapshots.</summary>
    /// <param name="name">The cache name.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithName(string? name);

    /// <summary>Sets the comparer used to match cache keys.</summary>
    /// <param name="comparer">The comparer, or <c>null</c> to use the default comparer.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithKeyComparer(IEqualityComparer<object>? comparer);

    /// <summary>Sets the time source used for expiration and timestamps.</summary>
    /// <param name="clock">The clock, or <c>null</c> to use system UTC time.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithClock(ICacheClock? clock);

    /// <summary>Enables a maximum number of stored entries.</summary>
    /// <param name="maximumEntries">The requested entry limit.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithMaximumEntries(long maximumEntries);

    /// <summary>Enables a maximum total entry weight.</summary>
    /// <param name="maximumWeight">The requested weight limit.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithMaximumWeight(long maximumWeight);

    /// <summary>Sets the algorithm used to select entries for capacity eviction.</summary>
    /// <param name="policy">The requested eviction policy.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithEvictionPolicy(CacheEvictionPolicy policy);

    /// <summary>Sets the default write-relative expiration duration.</summary>
    /// <param name="ticks">The duration in .NET ticks.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithTimeToLive(long ticks);

    /// <summary>Sets the default idle expiration duration.</summary>
    /// <param name="ticks">The duration in .NET ticks.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithSlidingExpiration(long ticks);

    /// <summary>Sets the default fixed UTC expiration boundary.</summary>
    /// <param name="utcTicks">The UTC expiration boundary.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithAbsoluteExpiration(long utcTicks);

    /// <summary>Sets the default entry priority.</summary>
    /// <param name="priority">The requested priority.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithDefaultPriority(CacheEntryPriority priority);

    /// <summary>Sets the synchronous callback used to load missing values.</summary>
    /// <param name="loader">The loader callback, or <c>null</c> to disable loading.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithLoader(Func<object, object?>? loader);

    /// <summary>Controls whether null loader results are stored.</summary>
    /// <param name="enabled"><c>true</c> to store null loader results.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithCacheNullLoaderValues(bool enabled);

    /// <summary>Controls whether refresh-eligible live entries may be returned while a background refresh runs.</summary>
    /// <param name="enabled"><c>true</c> to enable stale-while-refresh reads.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithStaleWhileRefresh(bool enabled);

    /// <summary>Sets the callback used to calculate entry weights.</summary>
    /// <param name="weigher">The weight callback, or <c>null</c> to use configured weights.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithWeigher(Func<object, object?, long>? weigher);

    /// <summary>Sets the callback that receives removal notifications.</summary>
    /// <param name="listener">The listener callback, or <c>null</c> to disable notifications.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithRemovalListener(Action<CacheNotification>? listener);

    /// <summary>Enables operation statistics.</summary>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithStats();

    /// <summary>Enables periodic expiration cleanup.</summary>
    /// <param name="ticks">The requested interval in .NET ticks.</param>
    /// <returns>The builder for further configuration.</returns>
    ICacheBuilder WithCleanupInterval(long ticks);

    /// <summary>Builds a cache from the current settings.</summary>
    /// <returns>A new cache.</returns>
    Cache Build();
}
