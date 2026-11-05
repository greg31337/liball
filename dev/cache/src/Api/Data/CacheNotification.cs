namespace cache;

/// <summary>
/// Provides entry metadata to a configured removal listener after an entry leaves the cache.
/// </summary>
/// <remarks>The cache creates one notification per removal. The listener runs after internal state changes, and modifying this snapshot does not alter the cache.</remarks>
public class CacheNotification
{
    /// <summary>Stores the normalized diagnostic name of the cache that removed the entry.</summary>
    public string Name;
    /// <summary>Stores the removed entry's key.</summary>
    public object? Key;
    /// <summary>Stores the removed entry's value, which may be <c>null</c>.</summary>
    public object? Value;
    /// <summary>Stores why the entry was removed.</summary>
    public CacheRemovalReason Reason;
    /// <summary>Stores the removed entry's normalized weight.</summary>
    public long Weight;
    /// <summary>Stores the tick at which the entry was created.</summary>
    public long CreatedUtcTicks;
    /// <summary>Stores the entry's last successful access tick.</summary>
    public long LastAccessUtcTicks;
    /// <summary>Stores the tick at which the entry's current value was written.</summary>
    public long LastWriteUtcTicks;
    /// <summary>Stores the cache clock tick at which removal occurred.</summary>
    public long RemovedUtcTicks;

    internal CacheNotification()
    {
        Name = "";
        Key = null;
        Value = null;
        Reason = CacheRemovalReason.Unknown;
        Weight = 0L;
        CreatedUtcTicks = 0L;
        LastAccessUtcTicks = 0L;
        LastWriteUtcTicks = 0L;
        RemovedUtcTicks = 0L;
    }
}
