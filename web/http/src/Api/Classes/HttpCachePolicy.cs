namespace http;

/// <summary>
/// Evaluates HTTP cache storage, selection, reuse, validation, and invalidation policy.
/// </summary>
public class HttpCachePolicy : IHttpCachePolicy
{
    /// <summary>Creates an HTTP cache-policy service.</summary>
    public HttpCachePolicy()
    {
    }

    /// <summary>Parses Cache-Control field value bytes.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed directives.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    public HttpCacheControl ParseCacheControl(byte[] input)
    {
        CacheControlParseResult parsed = CacheControlCodec.Parse(
            input,
            int.MaxValue,
            int.MaxValue,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw new HttpParseException(
                "The Cache-Control field value is malformed.",
                "invalid-cache-control",
                parsed.ErrorPosition);
        }

        return parsed.Value;
    }

    /// <summary>Evaluates whether a response can be stored.</summary>
    /// <param name="requestContext">The cache request context.</param>
    /// <param name="responseContext">The response candidate context.</param>
    /// <returns>The storage decision.</returns>
    public HttpCacheDecision EvaluateStorage(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        return CacheStorageSemantics.Evaluate(requestContext, responseContext);
    }

    /// <summary>Builds primary and Vary-derived cache keys.</summary>
    /// <param name="requestContext">The cache request context.</param>
    /// <param name="responseContext">The stored response context.</param>
    /// <returns>The cache-key decision.</returns>
    public HttpCacheDecision BuildCacheKey(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        return CacheKeySemantics.Build(requestContext, responseContext);
    }

    /// <summary>Calculates current stored-response age.</summary>
    /// <param name="responseContext">The stored response timestamps and metadata.</param>
    /// <returns>The age calculation result.</returns>
    public HttpCacheDecision CalculateCurrentAge(HttpCacheResponseContext responseContext)
    {
        return CacheAgeSemantics.Calculate(responseContext);
    }

    /// <summary>Calculates explicit or heuristic freshness lifetime.</summary>
    /// <param name="requestContext">The cache policy context.</param>
    /// <param name="responseContext">The stored response metadata.</param>
    /// <returns>The freshness calculation result.</returns>
    public HttpCacheDecision CalculateFreshnessLifetime(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        return CacheFreshnessSemantics.Calculate(requestContext, responseContext);
    }

    /// <summary>Evaluates whether a cached response can satisfy a request.</summary>
    /// <param name="requestContext">The request and reuse policy.</param>
    /// <param name="responseContext">The stored response candidate.</param>
    /// <returns>The resulting cache-policy decision.</returns>
    public HttpCacheDecision EvaluateReuse(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        return CacheReuseSemantics.Evaluate(requestContext, responseContext);
    }

    /// <summary>Builds a conditional revalidation request.</summary>
    /// <param name="requestContext">The original request context.</param>
    /// <param name="responseContext">The stored response context.</param>
    /// <returns>The conditional request.</returns>
    public HttpRequest BuildRevalidationRequest(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        return CacheRevalidationSemantics.Build(requestContext, responseContext);
    }

    /// <summary>Merges a 304 response into matching stored responses.</summary>
    /// <param name="responseContext">The matching stored response context.</param>
    /// <param name="notModifiedResponse">The received 304 response.</param>
    /// <returns>The validation merge result.</returns>
    public HttpCacheValidationResult MergeNotModifiedResponse(
        HttpCacheResponseContext responseContext,
        HttpResponse notModifiedResponse)
    {
        return CacheValidationSemantics.MergeNotModified(
            responseContext,
            notModifiedResponse);
    }

    /// <summary>Evaluates invalidation caused by an unsafe request.</summary>
    /// <param name="request">The unsafe request.</param>
    /// <param name="response">Its received response.</param>
    /// <returns>The invalidation decision.</returns>
    public HttpCacheDecision EvaluateInvalidation(HttpRequest request, HttpResponse response)
    {
        return CacheInvalidationSemantics.Evaluate(request, response);
    }

    /// <summary>Builds a response served from stored metadata and content.</summary>
    /// <param name="requestContext">The current request context.</param>
    /// <param name="responseContext">The selected stored response.</param>
    /// <returns>The cached-response decision.</returns>
    public HttpCacheDecision BuildCachedResponse(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        return CacheResponseSemantics.Build(requestContext, responseContext);
    }
}
