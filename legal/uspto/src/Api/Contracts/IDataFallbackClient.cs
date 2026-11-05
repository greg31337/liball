namespace uspto;

/// <summary>
/// Resolves fallback-aware operations using explicit access, freshness, capability, and authority policy.
/// </summary>
public interface IDataFallbackClient
{
    /// <summary>Resolves one fallback-aware source.</summary>
    /// <param name="source">The operation-specific source adapter.</param>
    /// <param name="context">The request context and optional policy override.</param>
    /// <returns>The selected value and ordered attempt details.</returns>
    DataFallbackResult ResolveDataFallback(IDataFallbackSource source, RequestContext context);
}
