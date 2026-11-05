namespace uspto;

/// <summary>
/// Applies guarded online, cache, and local-mirror fallback selection.
/// </summary>
public class DataFallbackClient : IDataFallbackClient
{
    internal DataFallbackCoordinator Coordinator;

    /// <summary>Initializes a fallback client with the configured default policy.</summary>
    /// <param name="policy">The default access, authority, and freshness policy.</param>
    public DataFallbackClient(DataFallbackPolicy policy)
    {
        Coordinator = new DataFallbackCoordinator(policy);
    }

    /// <summary>Resolves one fallback-aware source.</summary>
    /// <param name="source">The operation-specific source adapter.</param>
    /// <param name="context">The request context and optional policy override.</param>
    /// <returns>The selected value and ordered attempt details.</returns>
    public DataFallbackResult ResolveDataFallback(IDataFallbackSource source, RequestContext context)
    {
        return Coordinator.Resolve(source, context, global::System.DateTimeOffset.UtcNow);
    }
}
