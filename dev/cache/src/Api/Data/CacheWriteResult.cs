namespace cache;

/// <summary>
/// Describes the outcome of a cache write.
/// </summary>
/// <remarks>Instances are returned by <see cref="Cache.Add"/>, <see cref="Cache.Put"/>, <see cref="Cache.PutEntry"/>, and <see cref="Cache.Replace"/>.</remarks>
public class CacheWriteResult
{
    /// <summary>Indicates whether the requested entry was placed into the cache before capacity enforcement.</summary>
    public bool Written;
    /// <summary>Indicates whether the write replaced an entry with the same key.</summary>
    public bool Replaced;
    /// <summary>Stores the requested key.</summary>
    public object? Key;
    /// <summary>Stores the requested value.</summary>
    public object? Value;
    /// <summary>Stores the replaced value when <see cref="Replaced"/> is true.</summary>
    public object? PreviousValue;
    /// <summary>Stores the write outcome.</summary>
    public CacheResultStatus Status;
    /// <summary>Stores <see cref="CacheRemovalReason.Replace"/> for replacements or the rejection reason for rejected writes.</summary>
    public CacheRemovalReason Reason;
    /// <summary>Stores the normalized weight assigned to the written entry.</summary>
    public long Weight;
    /// <summary>Stores the replaced entry's weight when <see cref="Replaced"/> is true.</summary>
    public long PreviousWeight;
    /// <summary>Stores the number of entries evicted while enforcing count and weight limits after the write.</summary>
    public long EvictedCount;

    internal CacheWriteResult()
    {
        Written = false;
        Replaced = false;
        Key = null;
        Value = null;
        PreviousValue = null;
        Status = CacheResultStatus.Unknown;
        Reason = CacheRemovalReason.Unknown;
        Weight = 0L;
        PreviousWeight = 0L;
        EvictedCount = 0L;
    }
}
