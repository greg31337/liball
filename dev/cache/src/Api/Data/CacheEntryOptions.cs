using System;

namespace cache;

/// <summary>
/// Configures expiration, weight, priority, refresh, and null handling for one cache entry.
/// </summary>
/// <remarks>
/// Entry options replace the cache-level expiration defaults for that write. Multiple enabled expiration modes may be combined; the earliest calculated boundary wins.
/// </remarks>
public class CacheEntryOptions
{
    /// <summary>Indicates whether <see cref="AbsoluteExpirationUtcTicks"/> participates in expiration.</summary>
    public bool HasAbsoluteExpiration;
    /// <summary>Stores the fixed UTC tick boundary at which the entry expires.</summary>
    public long AbsoluteExpirationUtcTicks;
    /// <summary>Indicates whether <see cref="TimeToLiveTicks"/> participates in expiration.</summary>
    public bool HasTimeToLive;
    /// <summary>Stores the duration, in .NET ticks after the write, for which the entry remains live.</summary>
    public long TimeToLiveTicks;
    /// <summary>Indicates whether <see cref="SlidingExpirationTicks"/> participates in expiration.</summary>
    public bool HasSlidingExpiration;
    /// <summary>Stores the idle duration, in .NET ticks after the last <see cref="Cache.Get"/>, for which the entry remains live.</summary>
    public long SlidingExpirationTicks;
    /// <summary>Stores the entry's capacity weight when no cache-level weigher is configured.</summary>
    /// <remarks>Values less than or equal to zero are normalized to <c>1</c> when the entry is written.</remarks>
    public long Weight;
    /// <summary>Stores the entry's relative protection from capacity eviction.</summary>
    public CacheEntryPriority Priority;
    /// <summary>Indicates whether <see cref="RefreshAfterWriteTicks"/> is used to decide when a refresh is due.</summary>
    public bool HasRefreshAfterWrite;
    /// <summary>Stores the duration, in .NET ticks after the last write, before <see cref="Cache.NeedsRefresh"/> returns <c>true</c>.</summary>
    public long RefreshAfterWriteTicks;
    /// <summary>Indicates whether this individual write may store a <c>null</c> value.</summary>
    public bool CacheNullValue;

    /// <summary>
    /// Creates entry options with no expiration, weight <c>1</c>, normal priority, and null-value storage enabled.
    /// </summary>
    public CacheEntryOptions()
    {
        HasAbsoluteExpiration = false;
        AbsoluteExpirationUtcTicks = 0L;
        HasTimeToLive = false;
        TimeToLiveTicks = 0L;
        HasSlidingExpiration = false;
        SlidingExpirationTicks = 0L;
        Weight = 1L;
        Priority = CacheEntryPriority.Normal;
        HasRefreshAfterWrite = false;
        RefreshAfterWriteTicks = 0L;
        CacheNullValue = true;
    }

    /// <summary>Creates entry options that do not expire.</summary>
    /// <returns>A new independently mutable options object.</returns>
    internal static CacheEntryOptions NoExpiration()
    {
        return new CacheEntryOptions();
    }

    /// <summary>Creates entry options that expire a fixed duration after the entry is written.</summary>
    /// <param name="ticks">The non-negative duration in .NET ticks. Zero expires at the write boundary.</param>
    /// <returns>A new options object with time-to-live expiration enabled.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>ticks</c> is negative.</exception>
    internal static CacheEntryOptions TimeToLive(long ticks)
    {
        if (ticks < 0L)
        {
            throw new ArgumentOutOfRangeException("ticks");
        }

        CacheEntryOptions options = new CacheEntryOptions();
        options.HasTimeToLive = true;
        options.TimeToLiveTicks = ticks;
        return options;
    }

    /// <summary>Creates entry options that expire after a period without a successful <see cref="Cache.Get"/>.</summary>
    /// <param name="ticks">The non-negative idle duration in .NET ticks. Zero expires immediately.</param>
    /// <returns>A new options object with sliding expiration enabled.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>ticks</c> is negative.</exception>
    internal static CacheEntryOptions SlidingExpiration(long ticks)
    {
        if (ticks < 0L)
        {
            throw new ArgumentOutOfRangeException("ticks");
        }

        CacheEntryOptions options = new CacheEntryOptions();
        options.HasSlidingExpiration = true;
        options.SlidingExpirationTicks = ticks;
        return options;
    }

    /// <summary>Creates entry options that expire at a fixed UTC tick boundary.</summary>
    /// <param name="utcTicks">The non-negative UTC tick value at which the entry expires.</param>
    /// <returns>A new options object with absolute expiration enabled.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>utcTicks</c> is negative.</exception>
    internal static CacheEntryOptions AbsoluteExpiration(long utcTicks)
    {
        if (utcTicks < 0L)
        {
            throw new ArgumentOutOfRangeException("utcTicks");
        }

        CacheEntryOptions options = new CacheEntryOptions();
        options.HasAbsoluteExpiration = true;
        options.AbsoluteExpirationUtcTicks = utcTicks;
        return options;
    }

    /// <summary>Creates entry options that become eligible for refresh after a write-relative duration.</summary>
    /// <remarks>This setting does not expire the entry. Use <see cref="Cache.NeedsRefresh"/> and <see cref="Cache.Refresh"/> explicitly, or enable <see cref="CacheOptions.StaleWhileRefresh"/> for automatic background refresh on reads.</remarks>
    /// <param name="ticks">The non-negative duration in .NET ticks. Zero makes refresh immediately due.</param>
    /// <returns>A new options object with refresh-after-write metadata enabled.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>ticks</c> is negative.</exception>
    internal static CacheEntryOptions RefreshAfterWrite(long ticks)
    {
        if (ticks < 0L)
        {
            throw new ArgumentOutOfRangeException("ticks");
        }

        CacheEntryOptions options = new CacheEntryOptions();
        options.HasRefreshAfterWrite = true;
        options.RefreshAfterWriteTicks = ticks;
        return options;
    }
}
