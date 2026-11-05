namespace cache;

/// <summary>
/// Describes one entry to store during a <see cref="Cache.PutMany"/> operation.
/// </summary>
public class CachePutRequest
{
    /// <summary>Stores the non-null key used for lookup.</summary>
    public object? Key;
    /// <summary>Stores the value to write, which may be <c>null</c>.</summary>
    public object? Value;
    /// <summary>Stores per-entry settings, or <c>null</c> to use cache-level defaults.</summary>
    public CacheEntryOptions? Options;

    /// <summary>Creates an empty request whose fields can be populated by the caller.</summary>
    public CachePutRequest()
    {
        Key = null;
        Value = null;
        Options = null;
    }
}
