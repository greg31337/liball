namespace http.Tests;

public class CacheReuseSemanticsTests
{
    [Test]
    public void FreshResponseCanSatisfyGetAndReportsCalculations()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 30, 120);

        HttpCacheDecision result = CacheReuseSemantics.Evaluate(request, response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(result.HasCurrentAge, Is.True);
            Assert.That(result.CurrentAge, Is.EqualTo(30));
            Assert.That(result.HasFreshnessLifetime, Is.True);
            Assert.That(result.FreshnessLifetime, Is.EqualTo(120));
            Assert.That(result.IsHeuristicFreshness, Is.False);
            Assert.That(result.RequiresValidation, Is.False);
            Assert.That(result.RequestContext, Is.SameAs(request));
            Assert.That(result.ResponseContext, Is.SameAs(response));
            Assert.That(result.Response, Is.SameAs(response.Response));
        });
    }

    [Test]
    public void FreshnessLifetimeMustStrictlyExceedCurrentAge()
    {
        HttpCacheDecision result = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            ResponseContext(HttpStatusCodes.Ok, 120, 120));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(result.RequiresValidation, Is.True);
        });
    }

    [Test]
    public void RequestMaxAgeConstrainsTheStoredResponseAgeInclusively()
    {
        HttpCacheRequestContext acceptedRequest = RequestContext(HttpMethods.Get);
        acceptedRequest.CacheControl.HasMaxAge = true;
        acceptedRequest.CacheControl.MaxAge = 30;
        HttpCacheRequestContext rejectedRequest = RequestContext(HttpMethods.Get);
        rejectedRequest.CacheControl.HasMaxAge = true;
        rejectedRequest.CacheControl.MaxAge = 29;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 30, 120);

        HttpCacheDecision accepted = CacheReuseSemantics.Evaluate(
            acceptedRequest,
            response);
        HttpCacheDecision rejected = CacheReuseSemantics.Evaluate(
            rejectedRequest,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(rejected.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
        });
    }

    [Test]
    public void RequestMinFreshConstrainsRemainingFreshnessInclusively()
    {
        HttpCacheRequestContext acceptedRequest = RequestContext(HttpMethods.Get);
        acceptedRequest.CacheControl.HasMinFresh = true;
        acceptedRequest.CacheControl.MinFresh = 90;
        HttpCacheRequestContext rejectedRequest = RequestContext(HttpMethods.Get);
        rejectedRequest.CacheControl.HasMinFresh = true;
        rejectedRequest.CacheControl.MinFresh = 91;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 30, 120);

        HttpCacheDecision accepted = CacheReuseSemantics.Evaluate(
            acceptedRequest,
            response);
        HttpCacheDecision rejected = CacheReuseSemantics.Evaluate(
            rejectedRequest,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(rejected.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
        });
    }

    [Test]
    public void RequestAndUnqualifiedResponseNoCacheRequireValidation()
    {
        HttpCacheRequestContext requestNoCache = RequestContext(HttpMethods.Get);
        requestNoCache.CacheControl.NoCache = true;
        HttpCacheResponseContext responseNoCache = ResponseContext(
            HttpStatusCodes.Ok,
            30,
            120);
        responseNoCache.CacheControl.NoCache = true;

        HttpCacheDecision requestResult = CacheReuseSemantics.Evaluate(
            requestNoCache,
            ResponseContext(HttpStatusCodes.Ok, 30, 120));
        HttpCacheDecision responseResult = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            responseNoCache);

        Assert.Multiple(() =>
        {
            Assert.That(requestResult.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(requestResult.RequiresValidation, Is.True);
            Assert.That(responseResult.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(responseResult.RequiresValidation, Is.True);
        });
    }

    [Test]
    public void QualifiedResponseNoCacheCanBeReusedWithoutListedFields()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 30, 120);
        response.CacheControl.NoCache = true;
        response.CacheControl.NoCacheFields.Add(HttpHeaderNames.SetCookie);

        HttpCacheDecision result = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            response);

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
    }

    [Test]
    public void MissingFreshnessInformationRequiresValidation()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 30, 120);
        response.CacheControl.HasMaxAge = false;

        HttpCacheDecision result = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(result.HasFreshnessLifetime, Is.False);
            Assert.That(result.RequiresValidation, Is.True);
        });
    }

    [Test]
    public void HeadIsReusableButOtherPresentedMethodsAreNot()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 30, 120);

        HttpCacheDecision head = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Head),
            response);
        HttpCacheDecision post = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Post),
            response);
        HttpCacheDecision put = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Put),
            response);
        HttpCacheRequestContext onlyIfCachedPost = RequestContext(HttpMethods.Post);
        onlyIfCachedPost.CacheControl.OnlyIfCached = true;
        HttpCacheDecision onlyIfCachedPostResult = CacheReuseSemantics.Evaluate(
            onlyIfCachedPost,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(head.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(post.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(put.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(onlyIfCachedPostResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void InformationalAndNotModifiedResponsesCannotDirectlySatisfyRequests()
    {
        HttpCacheDecision informational = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            ResponseContext(HttpStatusCodes.EarlyHints, 0, 120));
        HttpCacheDecision notModified = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            ResponseContext(HttpStatusCodes.NotModified, 0, 120));

        Assert.Multiple(() =>
        {
            Assert.That(informational.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(notModified.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void OnlyIfCachedReturnsFreshResponseOrMissWithoutValidation()
    {
        HttpCacheRequestContext freshRequest = RequestContext(HttpMethods.Get);
        freshRequest.CacheControl.OnlyIfCached = true;
        HttpCacheRequestContext staleRequest = RequestContext(HttpMethods.Get);
        staleRequest.CacheControl.OnlyIfCached = true;
        HttpCacheRequestContext absentRequest = RequestContext(HttpMethods.Get);
        absentRequest.CacheControl.OnlyIfCached = true;
        HttpCacheResponseContext absentResponse = new HttpCacheResponseContext();

        HttpCacheDecision fresh = CacheReuseSemantics.Evaluate(
            freshRequest,
            ResponseContext(HttpStatusCodes.Ok, 30, 120));
        HttpCacheDecision stale = CacheReuseSemantics.Evaluate(
            staleRequest,
            ResponseContext(HttpStatusCodes.Ok, 120, 120));
        HttpCacheDecision absent = CacheReuseSemantics.Evaluate(
            absentRequest,
            absentResponse);

        Assert.Multiple(() =>
        {
            Assert.That(fresh.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(stale.Outcome, Is.EqualTo(HttpCacheOutcome.OnlyIfCachedMiss));
            Assert.That(stale.RequiresValidation, Is.False);
            Assert.That(absent.Outcome, Is.EqualTo(HttpCacheOutcome.OnlyIfCachedMiss));
            Assert.That(absent.Response, Is.Null);
        });
    }

    [Test]
    public void ConflictingRequestDirectivesCannotPermitFreshReuse()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        request.CacheControl.HasConflicts = true;

        HttpCacheDecision result = CacheReuseSemantics.Evaluate(
            request,
            ResponseContext(HttpStatusCodes.Ok, 30, 120));

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
    }

    [Test]
    public void InvalidContextsMethodsStatusesAndConstraintsAreRejected()
    {
        HttpCacheRequestContext missingRequest = new HttpCacheRequestContext();
        HttpCacheRequestContext invalidMethod = RequestContext("BAD METHOD");
        HttpCacheRequestContext negativeMaxAge = RequestContext(HttpMethods.Get);
        negativeMaxAge.CacheControl.HasMaxAge = true;
        negativeMaxAge.CacheControl.MaxAge = -1;
        HttpCacheRequestContext negativeMinFresh = RequestContext(HttpMethods.Get);
        negativeMinFresh.CacheControl.HasMinFresh = true;
        negativeMinFresh.CacheControl.MinFresh = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    missingRequest,
                    ResponseContext(HttpStatusCodes.Ok, 0, 120)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    invalidMethod,
                    ResponseContext(HttpStatusCodes.Ok, 0, 120)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    RequestContext(HttpMethods.Get),
                    ResponseContext(99, 0, 120)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    negativeMaxAge,
                    ResponseContext(HttpStatusCodes.Ok, 0, 120)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    negativeMinFresh,
                    ResponseContext(HttpStatusCodes.Ok, 0, 120)));
        });
    }

    [Test]
    public void MaxStaleAllowsAnyStalenessOrAnInclusiveBound()
    {
        HttpCacheRequestContext unboundedRequest = RequestContext(HttpMethods.Get);
        unboundedRequest.CacheControl.HasMaxStale = true;
        unboundedRequest.CacheControl.MaxStale = -1;
        HttpCacheRequestContext boundaryRequest = RequestContext(HttpMethods.Get);
        boundaryRequest.CacheControl.HasMaxStale = true;
        boundaryRequest.CacheControl.HasMaxStaleValue = true;
        boundaryRequest.CacheControl.MaxStale = 30;
        HttpCacheRequestContext outsideRequest = RequestContext(HttpMethods.Get);
        outsideRequest.CacheControl.HasMaxStale = true;
        outsideRequest.CacheControl.HasMaxStaleValue = true;
        outsideRequest.CacheControl.MaxStale = 29;
        HttpCacheRequestContext disconnectedOutsideRequest = RequestContext(HttpMethods.Get);
        disconnectedOutsideRequest.IsConnected = false;
        disconnectedOutsideRequest.CacheControl.HasMaxStale = true;
        disconnectedOutsideRequest.CacheControl.HasMaxStaleValue = true;
        disconnectedOutsideRequest.CacheControl.MaxStale = 29;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 150, 120);

        HttpCacheDecision unbounded = CacheReuseSemantics.Evaluate(
            unboundedRequest,
            response);
        HttpCacheDecision boundary = CacheReuseSemantics.Evaluate(
            boundaryRequest,
            response);
        HttpCacheDecision outside = CacheReuseSemantics.Evaluate(
            outsideRequest,
            response);
        HttpCacheDecision disconnectedOutside = CacheReuseSemantics.Evaluate(
            disconnectedOutsideRequest,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(unbounded.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(unbounded.CanServeStale, Is.True);
            Assert.That(unbounded.RequiresValidation, Is.False);
            Assert.That(boundary.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(outside.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(disconnectedOutside.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void ZeroStalenessIsInsideAZeroLengthAllowance()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        request.CacheControl.HasMaxStale = true;
        request.CacheControl.HasMaxStaleValue = true;
        request.CacheControl.MaxStale = 0;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 120, 120);

        HttpCacheDecision result = CacheReuseSemantics.Evaluate(request, response);

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
    }

    [Test]
    public void MustRevalidateAlwaysProhibitsStaleReuse()
    {
        HttpCacheRequestContext connected = RequestContext(HttpMethods.Get);
        connected.CacheControl.HasMaxStale = true;
        HttpCacheRequestContext disconnected = RequestContext(HttpMethods.Get);
        disconnected.IsConnected = false;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 150, 120);
        response.CacheControl.MustRevalidate = true;

        HttpCacheDecision connectedResult = CacheReuseSemantics.Evaluate(
            connected,
            response);
        HttpCacheDecision disconnectedResult = CacheReuseSemantics.Evaluate(
            disconnected,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(connectedResult.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(connectedResult.CanServeStale, Is.False);
            Assert.That(disconnectedResult.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(disconnectedResult.CanServeStale, Is.False);
        });
    }

    [Test]
    public void ProxyRevalidateAndSharedMaxAgeOnlyProhibitSharedCaches()
    {
        HttpCacheRequestContext sharedRequest = RequestContext(HttpMethods.Get);
        sharedRequest.IsShared = true;
        sharedRequest.CacheControl.HasMaxStale = true;
        HttpCacheRequestContext privateRequest = RequestContext(HttpMethods.Get);
        privateRequest.CacheControl.HasMaxStale = true;
        HttpCacheResponseContext proxyResponse = ResponseContext(
            HttpStatusCodes.Ok,
            150,
            120);
        proxyResponse.CacheControl.ProxyRevalidate = true;
        HttpCacheResponseContext sharedAgeResponse = ResponseContext(
            HttpStatusCodes.Ok,
            150,
            120);
        sharedAgeResponse.CacheControl.HasSharedMaxAge = true;
        sharedAgeResponse.CacheControl.SharedMaxAge = 120;

        HttpCacheDecision sharedProxy = CacheReuseSemantics.Evaluate(
            sharedRequest,
            proxyResponse);
        HttpCacheDecision privateProxy = CacheReuseSemantics.Evaluate(
            privateRequest,
            proxyResponse);
        HttpCacheDecision sharedAge = CacheReuseSemantics.Evaluate(
            sharedRequest,
            sharedAgeResponse);
        HttpCacheDecision privateAge = CacheReuseSemantics.Evaluate(
            privateRequest,
            sharedAgeResponse);

        Assert.Multiple(() =>
        {
            Assert.That(sharedProxy.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(privateProxy.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(sharedAge.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(privateAge.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
        });
    }

    [Test]
    public void StaleWhileRevalidateUsesAnInclusiveResponseWindow()
    {
        HttpCacheResponseContext boundaryResponse = ResponseContext(
            HttpStatusCodes.Ok,
            150,
            120);
        boundaryResponse.CacheControl.HasStaleWhileRevalidate = true;
        boundaryResponse.CacheControl.StaleWhileRevalidate = 30;
        HttpCacheResponseContext outsideResponse = ResponseContext(
            HttpStatusCodes.Ok,
            151,
            120);
        outsideResponse.CacheControl.HasStaleWhileRevalidate = true;
        outsideResponse.CacheControl.StaleWhileRevalidate = 30;

        HttpCacheDecision boundary = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            boundaryResponse);
        HttpCacheDecision outside = CacheReuseSemantics.Evaluate(
            RequestContext(HttpMethods.Get),
            outsideResponse);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(boundary.CanServeStale, Is.True);
            Assert.That(boundary.RequiresValidation, Is.True);
            Assert.That(outside.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
        });
    }

    [Test]
    public void StaleIfErrorHonorsRequestAndResponseBounds()
    {
        HttpCacheRequestContext requestDirective = RequestContext(HttpMethods.Get);
        requestDirective.IsErrorResponse = true;
        requestDirective.CacheControl.HasStaleIfError = true;
        requestDirective.CacheControl.StaleIfError = 30;
        HttpCacheRequestContext responseDirective = RequestContext(HttpMethods.Get);
        responseDirective.IsErrorResponse = true;
        HttpCacheRequestContext noError = RequestContext(HttpMethods.Get);
        HttpCacheResponseContext boundaryResponse = ResponseContext(
            HttpStatusCodes.Ok,
            150,
            120);
        boundaryResponse.CacheControl.HasStaleIfError = true;
        boundaryResponse.CacheControl.StaleIfError = 30;
        HttpCacheResponseContext outsideResponse = ResponseContext(
            HttpStatusCodes.Ok,
            151,
            120);
        outsideResponse.CacheControl.HasStaleIfError = true;
        outsideResponse.CacheControl.StaleIfError = 30;

        HttpCacheDecision fromRequest = CacheReuseSemantics.Evaluate(
            requestDirective,
            ResponseContext(HttpStatusCodes.Ok, 150, 120));
        HttpCacheDecision fromResponse = CacheReuseSemantics.Evaluate(
            responseDirective,
            boundaryResponse);
        HttpCacheDecision outside = CacheReuseSemantics.Evaluate(
            responseDirective,
            outsideResponse);
        HttpCacheDecision withoutError = CacheReuseSemantics.Evaluate(
            noError,
            boundaryResponse);

        Assert.Multiple(() =>
        {
            Assert.That(fromRequest.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(fromResponse.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(outside.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(withoutError.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
        });
    }

    [Test]
    public void MostRestrictiveStaleIfErrorBoundWins()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        request.IsErrorResponse = true;
        request.CacheControl.HasStaleIfError = true;
        request.CacheControl.StaleIfError = 20;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 145, 120);
        response.CacheControl.HasStaleIfError = true;
        response.CacheControl.StaleIfError = 30;

        HttpCacheDecision result = CacheReuseSemantics.Evaluate(request, response);

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
    }

    [Test]
    public void DisconnectedCacheCanServeStaleUnlessAResponseDirectiveProhibitsIt()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        request.IsConnected = false;
        HttpCacheResponseContext ordinary = ResponseContext(HttpStatusCodes.Ok, 150, 120);
        HttpCacheResponseContext noCache = ResponseContext(HttpStatusCodes.Ok, 150, 120);
        noCache.CacheControl.NoCache = true;

        HttpCacheDecision ordinaryResult = CacheReuseSemantics.Evaluate(
            request,
            ordinary);
        HttpCacheDecision prohibitedResult = CacheReuseSemantics.Evaluate(
            request,
            noCache);

        Assert.Multiple(() =>
        {
            Assert.That(ordinaryResult.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(prohibitedResult.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void RequestFreshnessConstraintsNeedMaxStaleToPermitStaleReuse()
    {
        HttpCacheRequestContext constrained = RequestContext(HttpMethods.Get);
        constrained.CacheControl.HasMaxAge = true;
        constrained.CacheControl.MaxAge = 120;
        HttpCacheRequestContext explicitlyAllowed = RequestContext(HttpMethods.Get);
        explicitlyAllowed.CacheControl.HasMaxAge = true;
        explicitlyAllowed.CacheControl.MaxAge = 120;
        explicitlyAllowed.CacheControl.HasMaxStale = true;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, 150, 120);
        response.CacheControl.HasStaleWhileRevalidate = true;
        response.CacheControl.StaleWhileRevalidate = 60;

        HttpCacheDecision constrainedResult = CacheReuseSemantics.Evaluate(
            constrained,
            response);
        HttpCacheDecision allowedResult = CacheReuseSemantics.Evaluate(
            explicitlyAllowed,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(constrainedResult.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(allowedResult.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
        });
    }

    [Test]
    public void InvalidStaleDirectiveValuesAreRejected()
    {
        HttpCacheRequestContext orphanedBound = RequestContext(HttpMethods.Get);
        orphanedBound.CacheControl.HasMaxStaleValue = true;
        orphanedBound.CacheControl.MaxStale = 1;
        HttpCacheRequestContext negativeMaxStale = RequestContext(HttpMethods.Get);
        negativeMaxStale.CacheControl.HasMaxStale = true;
        negativeMaxStale.CacheControl.HasMaxStaleValue = true;
        negativeMaxStale.CacheControl.MaxStale = -1;
        HttpCacheResponseContext negativeError = ResponseContext(
            HttpStatusCodes.Ok,
            150,
            120);
        negativeError.CacheControl.HasStaleIfError = true;
        negativeError.CacheControl.StaleIfError = -1;
        HttpCacheResponseContext negativeRevalidation = ResponseContext(
            HttpStatusCodes.Ok,
            150,
            120);
        negativeRevalidation.CacheControl.HasStaleWhileRevalidate = true;
        negativeRevalidation.CacheControl.StaleWhileRevalidate = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    orphanedBound,
                    ResponseContext(HttpStatusCodes.Ok, 150, 120)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    negativeMaxStale,
                    ResponseContext(HttpStatusCodes.Ok, 150, 120)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    RequestContext(HttpMethods.Get),
                    negativeError));
            Assert.Throws<HttpProtocolException>(() =>
                CacheReuseSemantics.Evaluate(
                    RequestContext(HttpMethods.Get),
                    negativeRevalidation));
        });
    }

    static HttpCacheRequestContext RequestContext(string method)
    {
        HttpRequest request = new HttpRequest();
        request.Method = method;
        HttpCacheRequestContext context = new HttpCacheRequestContext();
        context.Request = request;
        return context;
    }

    static HttpCacheResponseContext ResponseContext(
        int statusCode,
        long currentAge,
        long freshnessLifetime)
    {
        DateTimeOffset time = new DateTimeOffset(
            2026,
            1,
            2,
            12,
            0,
            0,
            TimeSpan.Zero);
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
        HttpCacheResponseContext context = new HttpCacheResponseContext();
        context.Response = response;
        context.RequestTime = time;
        context.ResponseTime = time;
        context.CurrentTime = time.AddSeconds(currentAge);
        context.HasDate = true;
        context.Date = time;
        context.CacheControl.HasMaxAge = true;
        context.CacheControl.MaxAge = freshnessLifetime;
        return context;
    }
}
