using System;

namespace http;

/// <summary>
/// Contains request-side inputs and policy knobs for a cache decision.
/// </summary>
public class HttpCacheRequestContext
{
    /// <summary>The request being evaluated, or <c>null</c> when unavailable.</summary>
    public HttpRequest? Request;
    /// <summary>The parsed request Cache-Control directives.</summary>
    public HttpCacheControl CacheControl = new HttpCacheControl();
    /// <summary>The time at which the request was initiated.</summary>
    public DateTimeOffset RequestTime;
    /// <summary>Indicates that shared-cache rules apply.</summary>
    public bool IsShared;
    /// <summary>Indicates that the cache can contact an origin or forwarding peer.</summary>
    public bool IsConnected = true;
    /// <summary>Indicates that heuristic freshness calculations are permitted.</summary>
    public bool AllowHeuristicFreshness = true;
    /// <summary>The Last-Modified heuristic fraction in thousandths.</summary>
    public int HeuristicFraction = 100;
    /// <summary>The maximum heuristic freshness lifetime in seconds.</summary>
    public long MaximumHeuristicFreshness = 86400;
    /// <summary>Indicates that stale-if-error policy is being evaluated after an error.</summary>
    public bool IsErrorResponse;
    /// <summary>The number of equivalent requests represented by this cache operation.</summary>
    public int CollapsedRequestCount = 1;
}
