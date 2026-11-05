namespace cache;

/// <summary>
/// Summarizes the entries affected by a clear or expiration cleanup operation.
/// </summary>
public class CacheMaintenanceResult
{
    /// <summary>Stores the maintenance operation outcome.</summary>
    public CacheResultStatus Status;
    /// <summary>Stores the common removal reason applied by the operation.</summary>
    public CacheRemovalReason Reason;
    /// <summary>Stores the total number of entries removed by the operation.</summary>
    public long RemovedCount;
    /// <summary>Stores the number removed because they had expired.</summary>
    public long ExpiredCount;
    /// <summary>Stores the number removed because of capacity eviction.</summary>
    public long EvictedCount;
    /// <summary>Stores the number removed by a clear operation.</summary>
    public long ClearedCount;
    /// <summary>Stores the entry count after the operation.</summary>
    public long CurrentCount;
    /// <summary>Stores the total entry weight after the operation.</summary>
    public long CurrentWeight;

    internal CacheMaintenanceResult()
    {
        Status = CacheResultStatus.Unknown;
        Reason = CacheRemovalReason.Unknown;
        RemovedCount = 0L;
        ExpiredCount = 0L;
        EvictedCount = 0L;
        ClearedCount = 0L;
        CurrentCount = 0L;
        CurrentWeight = 0L;
    }
}
