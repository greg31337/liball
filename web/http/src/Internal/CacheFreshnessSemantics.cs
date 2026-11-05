using System;

namespace http;

internal static class CacheFreshnessSemantics
{
    internal static HttpCacheDecision Calculate(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        ValidateContexts(requestContext, responseContext);
        HttpCacheControl control = responseContext.CacheControl;
        if (control.HasConflicts)
        {
            return CreateLifetime(
                requestContext,
                responseContext,
                0,
                false,
                "Conflicting freshness directives make the response immediately stale.");
        }

        if (requestContext.IsShared && control.HasSharedMaxAge)
        {
            return CreateLifetime(
                requestContext,
                responseContext,
                control.SharedMaxAge,
                false,
                "The shared freshness lifetime comes from s-maxage.");
        }

        if (control.HasMaxAge)
        {
            return CreateLifetime(
                requestContext,
                responseContext,
                control.MaxAge,
                false,
                "The freshness lifetime comes from max-age.");
        }

        if (responseContext.HasExpires)
        {
            DateTimeOffset reference = responseContext.HasDate ?
                responseContext.Date : responseContext.ResponseTime;
            long lifetime = ElapsedSeconds(responseContext.Expires, reference);
            return CreateLifetime(
                requestContext,
                responseContext,
                lifetime,
                false,
                "The freshness lifetime comes from Expires and Date metadata.");
        }

        if (!CanUseHeuristic(requestContext, responseContext) ||
            !responseContext.HasLastModified)
        {
            return CreateUnavailable(requestContext, responseContext);
        }

        DateTimeOffset heuristicReference = responseContext.HasDate ?
            responseContext.Date : responseContext.ResponseTime;
        long ageSinceModification = ElapsedSeconds(
            heuristicReference,
            responseContext.LastModified);
        long heuristic = MultiplyFraction(
            ageSinceModification,
            requestContext.HeuristicFraction);
        if (heuristic > requestContext.MaximumHeuristicFreshness)
        {
            heuristic = requestContext.MaximumHeuristicFreshness;
        }

        return CreateLifetime(
            requestContext,
            responseContext,
            heuristic,
            true,
            "The freshness lifetime is a bounded Last-Modified heuristic.");
    }

    static void ValidateContexts(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (requestContext == null || responseContext == null ||
            responseContext.Response == null || responseContext.CacheControl == null)
        {
            throw new HttpProtocolException(
                "The freshness calculation context is incomplete.",
                "invalid-cache-freshness-context");
        }

        HttpCacheControl control = responseContext.CacheControl;
        if (control.HasMaxAge && control.MaxAge < 0 ||
            control.HasSharedMaxAge && control.SharedMaxAge < 0 ||
            requestContext.HeuristicFraction < 0 ||
            requestContext.HeuristicFraction > 1000 ||
            requestContext.MaximumHeuristicFreshness < 0)
        {
            throw new HttpProtocolException(
                "Freshness values and heuristic bounds must be non-negative and valid.",
                "invalid-cache-freshness-value");
        }
    }

    static bool CanUseHeuristic(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (!requestContext.AllowHeuristicFreshness)
        {
            return false;
        }

        HttpResponse? response = responseContext.Response;
        if (response == null)
        {
            return false;
        }

        HttpStatusMetadata status = StatusSemantics.Describe(response.StatusCode);
        if (status.IsCacheableByDefault || responseContext.CacheControl.Public)
        {
            return true;
        }

        return !requestContext.IsShared && responseContext.CacheControl.Private;
    }

    static long ElapsedSeconds(DateTimeOffset later, DateTimeOffset earlier)
    {
        long laterTicks = later.UtcDateTime.Ticks;
        long earlierTicks = earlier.UtcDateTime.Ticks;
        if (laterTicks <= earlierTicks)
        {
            return 0;
        }

        return (laterTicks - earlierTicks) / TimeSpan.TicksPerSecond;
    }

    static long MultiplyFraction(long value, int thousandths)
    {
        long whole = value / 1000;
        long remainder = value % 1000;
        return whole * thousandths + remainder * thousandths / 1000;
    }

    static HttpCacheDecision CreateLifetime(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext,
        long lifetime,
        bool heuristic,
        string reason)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.Store;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Response = responseContext.Response;
        result.HasFreshnessLifetime = true;
        result.FreshnessLifetime = lifetime;
        result.IsHeuristicFreshness = heuristic;
        result.Reason = reason;
        return result;
    }

    static HttpCacheDecision CreateUnavailable(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.Store;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Response = responseContext.Response;
        result.Reason = "No explicit or applicable heuristic freshness lifetime is available.";
        return result;
    }
}
