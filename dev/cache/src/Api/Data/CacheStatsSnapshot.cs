namespace cache;

/// <summary>
/// Contains point-in-time cache counters, rates, size, and weight.
/// </summary>
/// <remarks>Operation counters and rates are populated only when <see cref="CacheOptions.RecordStats"/> is enabled. Size and weight are always reported.</remarks>
public class CacheStatsSnapshot
{
    /// <summary>Stores the number of successful get and peek lookups.</summary>
    public long Hits;
    /// <summary>Stores the number of null-key, absent, or expired get and peek lookups.</summary>
    public long Misses;
    /// <summary>Stores the number of writes that inserted a previously absent key.</summary>
    public long Puts;
    /// <summary>Stores the number of writes that replaced an existing key.</summary>
    public long Updates;
    /// <summary>Stores the number of successful explicit removals.</summary>
    public long Removes;
    /// <summary>Stores the number of expired entries removed by reads, writes, snapshots, cleanup, or the cleanup timer.</summary>
    public long Expirations;
    /// <summary>Stores the number of entries removed to satisfy count or weight limits.</summary>
    public long Evictions;
    /// <summary>Stores the number of clear operations, regardless of how many entries each removed.</summary>
    public long Clears;
    /// <summary>Stores the number of loader calls that returned normally.</summary>
    public long LoadSuccesses;
    /// <summary>Stores the number of recorded loader failures.</summary>
    /// <remarks>Loader exceptions still propagate to the caller after the failure is recorded.</remarks>
    public long LoadFailures;
    /// <summary>Stores the combined cache-clock tick duration of successful loader calls.</summary>
    public long TotalLoadDurationTicks;
    /// <summary>Stores the number of entries currently held, including expired entries not yet encountered by maintenance.</summary>
    public long CurrentSize;
    /// <summary>Stores the normalized total weight currently held.</summary>
    public long CurrentWeight;
    /// <summary>Stores hits divided by hits plus misses, or <c>0</c> when no lookup was recorded.</summary>
    public double HitRate;
    /// <summary>Stores successful loads divided by successful plus failed loads, or <c>0</c> when no load was recorded.</summary>
    public double LoadSuccessRate;

    internal CacheStatsSnapshot()
    {
        Hits = 0L;
        Misses = 0L;
        Puts = 0L;
        Updates = 0L;
        Removes = 0L;
        Expirations = 0L;
        Evictions = 0L;
        Clears = 0L;
        LoadSuccesses = 0L;
        LoadFailures = 0L;
        TotalLoadDurationTicks = 0L;
        CurrentSize = 0L;
        CurrentWeight = 0L;
        HitRate = 0.0;
        LoadSuccessRate = 0.0;
    }
}
