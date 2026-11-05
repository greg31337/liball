namespace uspto;

/// <summary>
/// Defines where reusable API responses are cached.
/// </summary>
public enum CacheMode
{
    /// <summary>Response caching is disabled.</summary>
    Disabled = 0,

    /// <summary>Responses are cached only in process memory.</summary>
    Memory = 1,

    /// <summary>Responses are cached only in the configured file store.</summary>
    File = 2,

    /// <summary>Responses are cached in memory and in the configured file store.</summary>
    MemoryAndFile = 3
}
