using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents an HTTP cache-policy decision.
/// </summary>
public class HttpCacheDecision
{
    /// <summary>
    /// The cache-policy outcome.
    /// </summary>
    public HttpCacheOutcome Outcome;

    /// <summary>
    /// The request-side cache context, or <c>null</c> when not applicable.
    /// </summary>
    public HttpCacheRequestContext? RequestContext;

    /// <summary>
    /// The stored-response cache context, or <c>null</c> when not applicable.
    /// </summary>
    public HttpCacheResponseContext? ResponseContext;

    /// <summary>
    /// The response selected or constructed by the cache decision, or <c>null</c> when none applies.
    /// </summary>
    public HttpResponse? Response;

    /// <summary>Indicates that a calculated current age is available.</summary>
    public bool HasCurrentAge;
    /// <summary>The calculated current age in seconds.</summary>
    public long CurrentAge;
    /// <summary>Indicates that a calculated freshness lifetime is available.</summary>
    public bool HasFreshnessLifetime;
    /// <summary>The calculated freshness lifetime in seconds.</summary>
    public long FreshnessLifetime;
    /// <summary>Indicates that the freshness lifetime was calculated heuristically.</summary>
    public bool IsHeuristicFreshness;
    /// <summary>
    /// Indicates that validation is required before reuse or should proceed while a stale response is served.
    /// </summary>
    public bool RequiresValidation;
    /// <summary>Indicates that a stale response can be served for this decision.</summary>
    public bool CanServeStale;
    /// <summary>The primary cache key, or <c>null</c> when no key was calculated.</summary>
    public string? PrimaryKey;
    /// <summary>The Vary-derived secondary key, or <c>null</c> when no key was calculated.</summary>
    public string? SecondaryKey;
    /// <summary>A caller-readable explanation, or <c>null</c> when omitted.</summary>
    public string? Reason;
    /// <summary>A validation merge result, or <c>null</c> when no merge was performed.</summary>
    public HttpCacheValidationResult? ValidationResult;
    /// <summary>The precondition decision used to construct the response, or <c>null</c>.</summary>
    public HttpPreconditionDecision? PreconditionDecision;
    /// <summary>The range decision used to construct the response, or <c>null</c>.</summary>
    public HttpRangeDecision? RangeDecision;
    /// <summary>The number of equivalent requests represented by this response.</summary>
    public int CollapsedRequestCount = 1;
    /// <summary>Indicates that the response represents more than one equivalent request.</summary>
    public bool WasCollapsed;
    /// <summary>Indicates that the stored response was successfully revalidated.</summary>
    public bool WasRevalidated;
    /// <summary>The normalized cache target URIs the caller should invalidate.</summary>
    public List<string> InvalidationTargets = new List<string>();
}
