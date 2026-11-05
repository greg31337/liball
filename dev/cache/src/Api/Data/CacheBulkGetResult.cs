namespace cache;

/// <summary>
/// Contains ordered outcomes from a <see cref="Cache.GetMany"/> operation.
/// </summary>
/// <remarks>The result at each index corresponds to the key at the same input index.</remarks>
public class CacheBulkGetResult
{
    /// <summary>Stores the per-key lookup outcomes in input order.</summary>
    public CacheGetResult[] Results;
    /// <summary>Stores the number of processed keys and elements in <see cref="Results"/>.</summary>
    public long Count;

    internal CacheBulkGetResult()
    {
        Results = new CacheGetResult[0];
        Count = 0L;
    }
}
