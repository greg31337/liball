namespace cache;

/// <summary>
/// Contains ordered outcomes from a <see cref="Cache.RemoveMany"/> operation.
/// </summary>
/// <remarks>The result at each index corresponds to the key at the same input index.</remarks>
public class CacheBulkRemoveResult
{
    /// <summary>Stores the per-key removal outcomes in input order.</summary>
    public CacheRemoveResult[] Results;
    /// <summary>Stores the number of processed keys and elements in <see cref="Results"/>.</summary>
    public long Count;

    internal CacheBulkRemoveResult()
    {
        Results = new CacheRemoveResult[0];
        Count = 0L;
    }
}
