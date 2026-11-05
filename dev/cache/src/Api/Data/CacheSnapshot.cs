namespace cache;

/// <summary>
/// Contains copied keys and values for all entries that were live when <see cref="Cache.Snapshot"/> ran.
/// </summary>
/// <remarks><see cref="Keys"/> and <see cref="Values"/> use matching indices. Their order follows the cache's current dictionary enumeration and is not a policy ordering.</remarks>
public class CacheSnapshot
{
    /// <summary>Stores a copied array of live keys.</summary>
    public object[] Keys;
    /// <summary>Stores a copied array of values aligned with <see cref="Keys"/>.</summary>
    public object?[] Values;
    /// <summary>Stores the number of copied entries.</summary>
    public long Count;
    /// <summary>Stores the total normalized weight of the copied entries.</summary>
    public long Weight;
    /// <summary>Stores the cache clock tick at which the snapshot was taken.</summary>
    public long SnapshotUtcTicks;

    internal CacheSnapshot()
    {
        Keys = new object[0];
        Values = new object?[0];
        Count = 0L;
        Weight = 0L;
        SnapshotUtcTicks = 0L;
    }
}
