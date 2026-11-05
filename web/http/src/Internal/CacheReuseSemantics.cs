namespace http;

internal static class CacheReuseSemantics
{
    internal static HttpCacheDecision Evaluate(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        ValidateContexts(requestContext, responseContext);
        HttpRequest request = GetRequest(requestContext);
        HttpCacheControl requestControl = requestContext.CacheControl;
        HttpResponse? response = responseContext.Response;
        MethodSemantics.Describe(request.Method);

        if (request.Method != HttpMethods.Get && request.Method != HttpMethods.Head)
        {
            return CreateUnavailable(
                requestContext,
                responseContext,
                HttpCacheOutcome.Unusable,
                "The presented request method cannot reuse a stored response.");
        }

        if (response == null)
        {
            return CreateUnavailable(
                requestContext,
                responseContext,
                requestControl.OnlyIfCached ?
                    HttpCacheOutcome.OnlyIfCachedMiss : HttpCacheOutcome.Unusable,
                "No stored response candidate is available.");
        }

        HttpStatusMetadata status = StatusSemantics.Describe(response.StatusCode);
        if (status.StatusClass == HttpStatusClass.Informational ||
            response.StatusCode == HttpStatusCodes.NotModified)
        {
            return CreateUnavailable(
                requestContext,
                responseContext,
                requestControl.OnlyIfCached ?
                    HttpCacheOutcome.OnlyIfCachedMiss : HttpCacheOutcome.Unusable,
                "The stored response status cannot directly satisfy the request.");
        }

        HttpCacheDecision age = CacheAgeSemantics.Calculate(responseContext);
        HttpCacheDecision freshness = CacheFreshnessSemantics.Calculate(
            requestContext,
            responseContext);
        bool isActuallyFresh = freshness.HasFreshnessLifetime &&
            freshness.FreshnessLifetime > age.CurrentAge;
        bool canServeFresh = isActuallyFresh;

        if (requestControl.HasMaxAge && age.CurrentAge > requestControl.MaxAge)
        {
            canServeFresh = false;
        }

        if (requestControl.HasMinFresh &&
            (!canServeFresh ||
            freshness.FreshnessLifetime - age.CurrentAge < requestControl.MinFresh))
        {
            canServeFresh = false;
        }

        bool responseRequiresValidation = responseContext.CacheControl.NoCache &&
            responseContext.CacheControl.NoCacheFields.Count == 0;
        if (requestControl.HasConflicts || requestControl.NoCache ||
            responseRequiresValidation)
        {
            canServeFresh = false;
        }

        if (canServeFresh)
        {
            return CreateCalculated(
                requestContext,
                responseContext,
                age,
                freshness,
                HttpCacheOutcome.ServeFresh,
                false,
                "The stored response is fresh and satisfies the request constraints.");
        }

        HttpCacheControl responseControl = responseContext.CacheControl;
        bool hasStaleCandidate = freshness.HasFreshnessLifetime &&
            !isActuallyFresh;
        long staleness = hasStaleCandidate ?
            age.CurrentAge - freshness.FreshnessLifetime : 0;
        bool requestStaleLimitExceeded = requestControl.HasMaxStale &&
            requestControl.HasMaxStaleValue &&
            staleness > requestControl.MaxStale;
        bool staleProhibited = requestControl.HasConflicts ||
            responseControl.HasConflicts || requestControl.NoCache ||
            responseControl.NoCache || responseControl.MustRevalidate ||
            requestStaleLimitExceeded ||
            requestContext.IsShared &&
            (responseControl.ProxyRevalidate || responseControl.HasSharedMaxAge) ||
            (requestControl.HasMaxAge || requestControl.HasMinFresh) &&
            !requestControl.HasMaxStale;
        bool requestAllowsStale = requestControl.HasMaxStale &&
            (!requestControl.HasMaxStaleValue || staleness <= requestControl.MaxStale);
        bool responseAllowsRevalidation = responseControl.HasStaleWhileRevalidate &&
            staleness <= responseControl.StaleWhileRevalidate;
        bool errorAllowsStale = requestContext.IsErrorResponse &&
            (requestControl.HasStaleIfError || responseControl.HasStaleIfError) &&
            (!requestControl.HasStaleIfError ||
            staleness <= requestControl.StaleIfError) &&
            (!responseControl.HasStaleIfError ||
            staleness <= responseControl.StaleIfError);
        bool disconnectedAllowsStale = !requestContext.IsConnected;
        if (hasStaleCandidate && !staleProhibited &&
            (requestAllowsStale || responseAllowsRevalidation ||
            errorAllowsStale || disconnectedAllowsStale))
        {
            bool revalidateWhileServing = responseAllowsRevalidation &&
                requestContext.IsConnected && !requestContext.IsErrorResponse;
            return CreateCalculated(
                requestContext,
                responseContext,
                age,
                freshness,
                HttpCacheOutcome.ServeStale,
                revalidateWhileServing,
                "The stale response is reusable under an applicable stale-response policy.");
        }

        if (requestControl.OnlyIfCached)
        {
            return CreateCalculated(
                requestContext,
                responseContext,
                age,
                freshness,
                HttpCacheOutcome.OnlyIfCachedMiss,
                false,
                "The stored response cannot satisfy the only-if-cached request without validation.");
        }

        if (!requestContext.IsConnected || requestContext.IsErrorResponse)
        {
            return CreateCalculated(
                requestContext,
                responseContext,
                age,
                freshness,
                HttpCacheOutcome.Unusable,
                false,
                "The stale response cannot be reused and validation is unavailable or has failed.");
        }

        return CreateCalculated(
            requestContext,
            responseContext,
            age,
            freshness,
            HttpCacheOutcome.Validate,
            true,
            "The stored response requires validation before reuse.");
    }

    static void ValidateContexts(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (requestContext == null || responseContext == null ||
            requestContext.Request == null || requestContext.CacheControl == null ||
            responseContext.CacheControl == null ||
            requestContext.CacheControl.NoCacheFields == null ||
            responseContext.CacheControl.NoCacheFields == null)
        {
            throw new HttpProtocolException(
                "The cache reuse context is incomplete.",
                "invalid-cache-reuse-context");
        }

        HttpCacheControl requestControl = requestContext.CacheControl;
        if (requestControl.HasMaxAge && requestControl.MaxAge < 0 ||
            requestControl.HasMinFresh && requestControl.MinFresh < 0 ||
            HasInvalidStaleValues(requestControl) ||
            HasInvalidStaleValues(responseContext.CacheControl))
        {
            throw new HttpProtocolException(
                "Cache freshness and stale-response constraints are invalid.",
                "invalid-cache-reuse-value");
        }
    }

    static bool HasInvalidStaleValues(HttpCacheControl control)
    {
        return control.HasMaxStaleValue && !control.HasMaxStale ||
            control.HasMaxStaleValue && control.MaxStale < 0 ||
            control.HasStaleIfError && control.StaleIfError < 0 ||
            control.HasStaleWhileRevalidate &&
            control.StaleWhileRevalidate < 0;
    }

    static HttpRequest GetRequest(HttpCacheRequestContext requestContext)
    {
        HttpRequest? request = requestContext.Request;
        if (request == null)
        {
            throw new HttpProtocolException(
                "The cache reuse request is missing.",
                "invalid-cache-reuse-context");
        }

        return request;
    }

    static HttpCacheDecision CreateCalculated(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext,
        HttpCacheDecision age,
        HttpCacheDecision freshness,
        HttpCacheOutcome outcome,
        bool requiresValidation,
        string reason)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = outcome;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Response = responseContext.Response;
        result.HasCurrentAge = age.HasCurrentAge;
        result.CurrentAge = age.CurrentAge;
        result.HasFreshnessLifetime = freshness.HasFreshnessLifetime;
        result.FreshnessLifetime = freshness.FreshnessLifetime;
        result.IsHeuristicFreshness = freshness.IsHeuristicFreshness;
        result.RequiresValidation = requiresValidation;
        result.CanServeStale = outcome == HttpCacheOutcome.ServeStale;
        result.Reason = reason;
        return result;
    }

    static HttpCacheDecision CreateUnavailable(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext,
        HttpCacheOutcome outcome,
        string reason)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = outcome;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Response = responseContext.Response;
        result.Reason = reason;
        return result;
    }
}
