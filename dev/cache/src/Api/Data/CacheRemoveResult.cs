namespace cache;

/// <summary>
/// Describes the outcome of an explicit <see cref="Cache.Remove"/> operation.
/// </summary>
public class CacheRemoveResult
{
    /// <summary>Indicates whether an entry was removed.</summary>
    public bool Removed;
    /// <summary>Stores the requested key or the removed entry's equal stored key.</summary>
    public object? Key;
    /// <summary>Stores the removed value when <see cref="Removed"/> is true.</summary>
    public object? Value;
    /// <summary>Stores <see cref="CacheResultStatus.Removed"/> for success or <see cref="CacheResultStatus.Miss"/> when no entry exists.</summary>
    public CacheResultStatus Status;
    /// <summary>Stores <see cref="CacheRemovalReason.ExplicitRemove"/> when an entry was removed.</summary>
    public CacheRemovalReason Reason;
    /// <summary>Stores the removed entry's normalized weight.</summary>
    public long Weight;

    internal CacheRemoveResult()
    {
        Removed = false;
        Key = null;
        Value = null;
        Status = CacheResultStatus.Unknown;
        Reason = CacheRemovalReason.Unknown;
        Weight = 0L;
    }
}
