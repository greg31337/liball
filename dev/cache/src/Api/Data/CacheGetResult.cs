namespace cache;

/// <summary>
/// Describes a point-in-time outcome from a get, peek, load, or refresh operation.
/// </summary>
/// <remarks>Instances are created by <see cref="Cache"/>. A value can legitimately be <c>null</c> when <see cref="Found"/> is true, so use the flag rather than testing <see cref="Value"/>.</remarks>
public class CacheGetResult
{
    /// <summary>Indicates whether the result contains a live cached value.</summary>
    public bool Found;
    /// <summary>Stores the requested key, including on misses when one was supplied.</summary>
    public object? Key;
    /// <summary>Stores the cached value when <see cref="Found"/> is true.</summary>
    public object? Value;
    /// <summary>Stores the operation outcome.</summary>
    public CacheResultStatus Status;
    /// <summary>Stores the associated removal reason for an expired result, or <see cref="CacheRemovalReason.Unknown"/> otherwise.</summary>
    public CacheRemovalReason Reason;
    /// <summary>Indicates whether a loader was invoked for this result.</summary>
    public bool Loaded;
    /// <summary>Indicates whether the live entry has a fixed or sliding expiration boundary.</summary>
    public bool HasExpiration;
    /// <summary>Stores the clock tick at which the current entry was created.</summary>
    public long CreatedUtcTicks;
    /// <summary>Stores the entry's last-access tick, updated by successful <see cref="Cache.Get"/> calls but not peeks.</summary>
    public long LastAccessUtcTicks;
    /// <summary>Stores the clock tick at which the current entry value was written.</summary>
    public long LastWriteUtcTicks;
    /// <summary>Stores the earliest effective expiration tick, or <c>0</c> when no expiration applies.</summary>
    public long ExpirationUtcTicks;

    internal CacheGetResult()
    {
        Found = false;
        Key = null;
        Value = null;
        Status = CacheResultStatus.Unknown;
        Reason = CacheRemovalReason.Unknown;
        Loaded = false;
        HasExpiration = false;
        CreatedUtcTicks = 0L;
        LastAccessUtcTicks = 0L;
        LastWriteUtcTicks = 0L;
        ExpirationUtcTicks = 0L;
    }
}
