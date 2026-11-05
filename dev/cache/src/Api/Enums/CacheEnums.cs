namespace cache;

/// <summary>
/// Specifies how the cache chooses an entry when a capacity limit requires eviction.
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>Evicts the entry that was accessed least recently.</summary>
    Lru = 0,
    /// <summary>Evicts the entry that was created earliest.</summary>
    Fifo = 1,
    /// <summary>Evicts the entry with the lowest access count, using recency to break ties.</summary>
    Lfu = 2,
    /// <summary>Evicts a randomly selected entry from the lowest available priority.</summary>
    Random = 3,
    /// <summary>Uses a recency window with frequency-gated admission into probation and protected main segments.</summary>
    Segmented = 4
}

/// <summary>
/// Specifies an entry's relative protection from capacity eviction.
/// </summary>
public enum CacheEntryPriority
{
    /// <summary>Evicts before normal- and high-priority entries.</summary>
    Low = 0,
    /// <summary>Uses the cache's ordinary eviction ordering.</summary>
    Normal = 1,
    /// <summary>Evicts only after lower-priority candidates have been exhausted.</summary>
    High = 2
}

/// <summary>
/// Describes why an entry was removed or why a write was rejected.
/// </summary>
public enum CacheRemovalReason
{
    /// <summary>No removal reason applies.</summary>
    Unknown = 0,
    /// <summary>The caller explicitly removed the entry.</summary>
    ExplicitRemove = 1,
    /// <summary>A write replaced an entry with the same key.</summary>
    Replace = 2,
    /// <summary>The entry reached its effective expiration boundary.</summary>
    Expired = 3,
    /// <summary>A count or weight limit required eviction.</summary>
    Capacity = 4,
    /// <summary>The cache was cleared.</summary>
    Clear = 5,
    /// <summary>The cache declined to store the supplied key or value.</summary>
    Rejected = 6
}

/// <summary>
/// Summarizes the outcome represented by a cache result object.
/// </summary>
public enum CacheResultStatus
{
    /// <summary>No operation outcome has been assigned.</summary>
    Unknown = 0,
    /// <summary>A live entry was found.</summary>
    Hit = 1,
    /// <summary>No live entry was found.</summary>
    Miss = 2,
    /// <summary>An entry was found at or beyond its expiration boundary and was removed.</summary>
    Expired = 3,
    /// <summary>A loader produced and stored a value.</summary>
    Loaded = 4,
    /// <summary>A new entry was stored.</summary>
    Written = 5,
    /// <summary>An existing entry was replaced.</summary>
    Replaced = 6,
    /// <summary>One or more entries were removed.</summary>
    Removed = 7,
    /// <summary>A requested write was not stored.</summary>
    Rejected = 8,
    /// <summary>All entries present at the time of the operation were cleared.</summary>
    Cleared = 9,
    /// <summary>A refresh-eligible live entry was returned while a background refresh for its key was in progress.</summary>
    Stale = 10
}
