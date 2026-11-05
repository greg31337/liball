namespace uspto;

/// <summary>
/// Supplies capability descriptions and values for one fallback-aware operation.
/// </summary>
public interface IDataFallbackSource
{
    /// <summary>Gets the live online capability.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The online capability and provenance.</returns>
    DataFallbackCapability GetOnlineFallbackCapability(RequestContext context);

    /// <summary>Gets the response-cache capability.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The cache capability and provenance.</returns>
    DataFallbackCapability GetCacheFallbackCapability(RequestContext context);

    /// <summary>Gets the local-mirror capability.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The local capability and provenance.</returns>
    DataFallbackCapability GetLocalFallbackCapability(RequestContext context);

    /// <summary>Loads the value from the live online source.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The online value and provenance.</returns>
    DataFallbackValue LoadOnlineFallbackValue(RequestContext context);

    /// <summary>Loads the value from the response cache.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The cached value and provenance.</returns>
    DataFallbackValue LoadCacheFallbackValue(RequestContext context);

    /// <summary>Loads the value from the local mirror.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The local value and provenance.</returns>
    DataFallbackValue LoadLocalFallbackValue(RequestContext context);
}
