namespace cache;

/// <summary>
/// Contains ordered outcomes from a <see cref="Cache.PutMany"/> operation.
/// </summary>
/// <remarks>The result at each index corresponds to the request at the same input index.</remarks>
public class CacheBulkWriteResult
{
    /// <summary>Stores the per-request write outcomes in input order.</summary>
    public CacheWriteResult[] Results;
    /// <summary>Stores the number of processed requests and elements in <see cref="Results"/>.</summary>
    public long Count;

    internal CacheBulkWriteResult()
    {
        Results = new CacheWriteResult[0];
        Count = 0L;
    }
}
