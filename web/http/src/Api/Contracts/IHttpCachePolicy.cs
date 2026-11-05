namespace http;

/// <summary>
/// Evaluates HTTP cache policy for requests and stored responses.
/// </summary>
public interface IHttpCachePolicy
{
    /// <summary>Parses Cache-Control field value bytes.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed directives.</returns>
    HttpCacheControl ParseCacheControl(byte[] input);

    /// <summary>Evaluates whether a response can be stored.</summary>
    /// <param name="requestContext">The cache request context.</param>
    /// <param name="responseContext">The response candidate context.</param>
    /// <returns>The storage decision.</returns>
    HttpCacheDecision EvaluateStorage(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext);

    /// <summary>Builds primary and Vary-derived cache keys.</summary>
    /// <param name="requestContext">The cache request context.</param>
    /// <param name="responseContext">The stored response context.</param>
    /// <returns>The cache-key decision.</returns>
    HttpCacheDecision BuildCacheKey(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext);

    /// <summary>Calculates current stored-response age.</summary>
    /// <param name="responseContext">The stored response timestamps and metadata.</param>
    /// <returns>The age calculation result.</returns>
    HttpCacheDecision CalculateCurrentAge(HttpCacheResponseContext responseContext);

    /// <summary>Calculates explicit or heuristic freshness lifetime.</summary>
    /// <param name="requestContext">The cache policy context.</param>
    /// <param name="responseContext">The stored response metadata.</param>
    /// <returns>The freshness calculation result.</returns>
    HttpCacheDecision CalculateFreshnessLifetime(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext);

    /// <summary>
    /// Evaluates whether a cached response can satisfy a request.
    /// </summary>
    /// <param name="requestContext">The request and reuse policy.</param>
    /// <param name="responseContext">The stored response candidate.</param>
    /// <returns>The resulting cache-policy decision.</returns>
    HttpCacheDecision EvaluateReuse(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext);

    /// <summary>Builds a conditional revalidation request.</summary>
    /// <param name="requestContext">The original request context.</param>
    /// <param name="responseContext">The stored response context.</param>
    /// <returns>The conditional request.</returns>
    HttpRequest BuildRevalidationRequest(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext);

    /// <summary>Merges a 304 response into matching stored responses.</summary>
    /// <param name="responseContext">The matching stored response context.</param>
    /// <param name="notModifiedResponse">The received 304 response.</param>
    /// <returns>The validation merge result.</returns>
    HttpCacheValidationResult MergeNotModifiedResponse(
        HttpCacheResponseContext responseContext,
        HttpResponse notModifiedResponse);

    /// <summary>Evaluates invalidation caused by an unsafe request.</summary>
    /// <param name="request">The unsafe request.</param>
    /// <param name="response">Its received response.</param>
    /// <returns>The invalidation decision.</returns>
    HttpCacheDecision EvaluateInvalidation(HttpRequest request, HttpResponse response);

    /// <summary>Builds a response served from stored metadata and content.</summary>
    /// <param name="requestContext">The current request context.</param>
    /// <param name="responseContext">The selected stored response.</param>
    /// <returns>The cached-response decision.</returns>
    HttpCacheDecision BuildCachedResponse(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext);
}
