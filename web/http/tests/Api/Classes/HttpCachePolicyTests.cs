using System.Text;

namespace http.Tests;

public class HttpCachePolicyTests
{
    const string BaseDate = "Sun, 06 Nov 1994 08:49:37 GMT";

    readonly IHttpCachePolicy policy = new HttpCachePolicy();

    [Test]
    public void CacheControlParsingExposesEverySupportedDirective()
    {
        HttpCacheControl result = policy.ParseCacheControl(Encoding.ASCII.GetBytes(
            "no-cache=\"Set-Cookie\", no-store, no-transform, only-if-cached, " +
            "must-revalidate, proxy-revalidate, must-understand, " +
            "private=\"Authorization\", public, immutable, max-age=60, " +
            "s-maxage=30, max-stale=15, min-fresh=5, stale-if-error=20, " +
            "stale-while-revalidate=10, example=\"value\""));

        Assert.Multiple(() =>
        {
            Assert.That(result.NoCache, Is.True);
            Assert.That(result.NoCacheFields, Is.EqualTo(new[] { "Set-Cookie" }));
            Assert.That(result.NoStore, Is.True);
            Assert.That(result.NoTransform, Is.True);
            Assert.That(result.OnlyIfCached, Is.True);
            Assert.That(result.MustRevalidate, Is.True);
            Assert.That(result.ProxyRevalidate, Is.True);
            Assert.That(result.MustUnderstand, Is.True);
            Assert.That(result.Private, Is.True);
            Assert.That(result.PrivateFields, Is.EqualTo(new[] { "Authorization" }));
            Assert.That(result.Public, Is.True);
            Assert.That(result.Immutable, Is.True);
            Assert.That(result.HasMaxAge, Is.True);
            Assert.That(result.MaxAge, Is.EqualTo(60));
            Assert.That(result.HasSharedMaxAge, Is.True);
            Assert.That(result.SharedMaxAge, Is.EqualTo(30));
            Assert.That(result.HasMaxStale, Is.True);
            Assert.That(result.HasMaxStaleValue, Is.True);
            Assert.That(result.MaxStale, Is.EqualTo(15));
            Assert.That(result.HasMinFresh, Is.True);
            Assert.That(result.MinFresh, Is.EqualTo(5));
            Assert.That(result.HasStaleIfError, Is.True);
            Assert.That(result.StaleIfError, Is.EqualTo(20));
            Assert.That(result.HasStaleWhileRevalidate, Is.True);
            Assert.That(result.StaleWhileRevalidate, Is.EqualTo(10));
            Assert.That(result.Extensions, Has.Count.EqualTo(1));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("example"));
            Assert.That(Encoding.ASCII.GetString(result.Extensions[0].Value),
                Is.EqualTo("value"));
        });
    }

    [Test]
    public void CacheControlParsingHandlesOptionalConflictingAndInvalidValues()
    {
        HttpCacheControl optional = policy.ParseCacheControl(
            Encoding.ASCII.GetBytes("max-stale"));
        HttpCacheControl conflicting = policy.ParseCacheControl(
            Encoding.ASCII.GetBytes("max-age=10, max-age=20"));
        HttpCacheControl saturated = policy.ParseCacheControl(
            Encoding.ASCII.GetBytes("max-age=999999999999999999999999999"));

        Assert.Multiple(() =>
        {
            Assert.That(optional.HasMaxStale, Is.True);
            Assert.That(optional.HasMaxStaleValue, Is.False);
            Assert.That(conflicting.HasConflicts, Is.True);
            Assert.That(conflicting.MaxAge, Is.EqualTo(10));
            Assert.That(saturated.MaxAge, Is.EqualTo(long.MaxValue));
            Assert.Throws<HttpParseException>(() => policy.ParseCacheControl(
                Encoding.ASCII.GetBytes("max-age=\"unterminated")));
        });
    }

    [Test]
    public void StorageCoversDefaultExplicitMethodAndNoStoreRules()
    {
        HttpCacheResponseContext ordinary = StorageResponse(HttpStatusCodes.Ok, true);
        HttpCacheResponseContext redirect = StorageResponse(HttpStatusCodes.Found, true);
        HttpCacheResponseContext explicitRedirect = StorageResponse(
            HttpStatusCodes.Found,
            true);
        explicitRedirect.CacheControl.HasMaxAge = true;
        explicitRedirect.CacheControl.MaxAge = 60;
        HttpCacheRequestContext requestNoStore = RequestContext(HttpMethods.Get, false);
        requestNoStore.CacheControl.NoStore = true;
        HttpCacheResponseContext responseNoStore = StorageResponse(
            HttpStatusCodes.Ok,
            true);
        responseNoStore.CacheControl.NoStore = true;

        Assert.Multiple(() =>
        {
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), ordinary).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Head, false), ordinary).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Put, false), ordinary).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), redirect).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), explicitRedirect).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(requestNoStore, ordinary).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), responseNoStore).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void StorageCoversPostSharedAuthorizationPrivateAndCompletenessRules()
    {
        HttpCacheRequestContext post = RequestContext(HttpMethods.Post, false);
        HttpCacheResponseContext matchingPost = StorageResponse(HttpStatusCodes.Ok, true);
        matchingPost.CacheControl.HasMaxAge = true;
        matchingPost.CacheControl.MaxAge = 60;
        AddField(matchingPost.Response!.Headers, HttpHeaderNames.ContentLocation, "/items/1");
        HttpCacheResponseContext unmatchedPost = StorageResponse(HttpStatusCodes.Ok, true);
        unmatchedPost.CacheControl.HasMaxAge = true;
        unmatchedPost.CacheControl.MaxAge = 60;
        AddField(unmatchedPost.Response!.Headers, HttpHeaderNames.ContentLocation, "/other");

        HttpCacheRequestContext authorized = RequestContext(HttpMethods.Get, true);
        AddField(authorized.Request!.Headers, HttpHeaderNames.Authorization, "Bearer token");
        HttpCacheResponseContext publicResponse = StorageResponse(HttpStatusCodes.Ok, true);
        publicResponse.CacheControl.Public = true;
        HttpCacheResponseContext privateResponse = StorageResponse(HttpStatusCodes.Ok, true);
        privateResponse.CacheControl.Private = true;
        HttpCacheResponseContext qualifiedPrivate = StorageResponse(HttpStatusCodes.Ok, true);
        qualifiedPrivate.CacheControl.Private = true;
        qualifiedPrivate.CacheControl.PrivateFields.Add("set-cookie");
        HttpCacheResponseContext incomplete = StorageResponse(HttpStatusCodes.Ok, false);

        Assert.Multiple(() =>
        {
            Assert.That(policy.EvaluateStorage(post, matchingPost).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(post, unmatchedPost).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(authorized,
                StorageResponse(HttpStatusCodes.Ok, true)).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(authorized, publicResponse).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, true), privateResponse).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, true), qualifiedPrivate).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), incomplete).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Head, false), incomplete).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void StorageCoversMustUnderstandPartialAndConflictRules()
    {
        HttpCacheResponseContext understood = StorageResponse(HttpStatusCodes.Ok, true);
        understood.CacheControl.NoStore = true;
        understood.CacheControl.MustUnderstand = true;
        HttpCacheResponseContext unknown = StorageResponse(299, true);
        unknown.CacheControl.NoStore = true;
        unknown.CacheControl.MustUnderstand = true;
        unknown.CacheControl.Public = true;
        HttpCacheResponseContext partial = StorageResponse(
            HttpStatusCodes.PartialContent,
            false);
        partial.Response!.IsComplete = true;
        AddField(partial.Response.Headers, HttpHeaderNames.ContentRange, "bytes 0-4/10");
        HttpCacheResponseContext conflict = StorageResponse(HttpStatusCodes.Ok, true);
        conflict.CacheControl.HasConflicts = true;

        Assert.Multiple(() =>
        {
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), understood).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), unknown).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), partial).Outcome,
                Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(policy.EvaluateStorage(
                RequestContext(HttpMethods.Get, false), conflict).Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void CacheKeysNormalizeTargetsAndVarySelectingFields()
    {
        HttpCacheResponseContext response = StorageResponse(HttpStatusCodes.Ok, true);
        AddField(response.Response!.Headers, HttpHeaderNames.Vary,
            "X-Mode, Accept-Encoding");
        HttpCacheRequestContext origin = RequestContext(HttpMethods.Get, false);
        origin.Request!.Target = "/a/./x/../%7e?q=%41%2f";
        origin.Request.Scheme = "HTTP";
        origin.Request.Authority = null;
        origin.Request.Headers.Fields[0].Value =
            Encoding.ASCII.GetBytes("%65XAMPLE.test:080");
        AddField(origin.Request.Headers, HttpHeaderNames.AcceptEncoding, "gzip, br");
        AddField(origin.Request.Headers, "X-Mode", " fast ");
        HttpCacheRequestContext absolute = RequestContext(HttpMethods.Get, false);
        absolute.Request!.Target = "http://example.test/a/~?q=A%2F";
        absolute.Request.Scheme = null;
        absolute.Request.Authority = null;
        AddField(absolute.Request.Headers, "accept-encoding", "gzip");
        AddField(absolute.Request.Headers, "accept-encoding", "br");
        AddField(absolute.Request.Headers, "x-mode", "fast");

        HttpCacheDecision first = policy.BuildCacheKey(origin, response);
        HttpCacheDecision second = policy.BuildCacheKey(absolute, response);

        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(first.PrimaryKey,
                Is.EqualTo("GET http://example.test/a/~?q=A%2F"));
            Assert.That(first.PrimaryKey, Is.EqualTo(second.PrimaryKey));
            Assert.That(first.SecondaryKey, Is.EqualTo(second.SecondaryKey));
            Assert.That(first.SecondaryKey, Does.StartWith("V1;2;"));
        });
    }

    [Test]
    public void CacheKeysRejectVaryWildcardAndUnsupportedMethods()
    {
        HttpCacheResponseContext wildcard = StorageResponse(HttpStatusCodes.Ok, true);
        AddField(wildcard.Response!.Headers, HttpHeaderNames.Vary, "*");

        Assert.Multiple(() =>
        {
            Assert.That(policy.BuildCacheKey(
                RequestContext(HttpMethods.Get, false), wildcard).Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(policy.BuildCacheKey(
                RequestContext(HttpMethods.Put, false),
                StorageResponse(HttpStatusCodes.Ok, true)).Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void CurrentAgeUsesEveryComponentClampsSkewAndSaturates()
    {
        DateTimeOffset responseTime = BaseTime().AddSeconds(5);
        HttpCacheResponseContext complete = TimedResponse(HttpStatusCodes.Ok);
        complete.RequestTime = responseTime.AddSeconds(-5);
        complete.ResponseTime = responseTime;
        complete.HasDate = true;
        complete.Date = responseTime.AddSeconds(-15);
        complete.HasAge = true;
        complete.Age = 7;
        complete.CurrentTime = responseTime.AddSeconds(20);
        HttpCacheResponseContext skew = TimedResponse(HttpStatusCodes.Ok);
        skew.RequestTime = responseTime.AddSeconds(20);
        skew.ResponseTime = responseTime;
        skew.HasDate = true;
        skew.Date = responseTime.AddSeconds(30);
        skew.HasAge = true;
        skew.Age = 8;
        skew.CurrentTime = responseTime.AddSeconds(-10);
        HttpCacheResponseContext saturated = TimedResponse(HttpStatusCodes.Ok);
        saturated.HasAge = true;
        saturated.Age = long.MaxValue;
        saturated.CurrentTime = saturated.ResponseTime.AddSeconds(1);

        Assert.Multiple(() =>
        {
            Assert.That(policy.CalculateCurrentAge(complete).CurrentAge, Is.EqualTo(35));
            Assert.That(policy.CalculateCurrentAge(skew).CurrentAge, Is.EqualTo(8));
            Assert.That(policy.CalculateCurrentAge(saturated).CurrentAge,
                Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void FreshnessUsesSharedMaxAgeMaxAgeExpiresAndHeuristicsInOrder()
    {
        HttpCacheResponseContext explicitResponse = TimedResponse(HttpStatusCodes.Ok);
        explicitResponse.CacheControl.HasSharedMaxAge = true;
        explicitResponse.CacheControl.SharedMaxAge = 30;
        explicitResponse.CacheControl.HasMaxAge = true;
        explicitResponse.CacheControl.MaxAge = 60;
        explicitResponse.HasExpires = true;
        explicitResponse.Expires = explicitResponse.Date.AddSeconds(90);
        HttpCacheResponseContext expires = TimedResponse(HttpStatusCodes.Ok);
        expires.HasExpires = true;
        expires.Expires = expires.Date.AddSeconds(75);
        HttpCacheResponseContext heuristic = TimedResponse(HttpStatusCodes.Ok);
        heuristic.HasLastModified = true;
        heuristic.LastModified = heuristic.Date.AddSeconds(-1000000);

        Assert.Multiple(() =>
        {
            Assert.That(policy.CalculateFreshnessLifetime(
                BareRequest(true), explicitResponse).FreshnessLifetime,
                Is.EqualTo(30));
            Assert.That(policy.CalculateFreshnessLifetime(
                BareRequest(false), explicitResponse).FreshnessLifetime,
                Is.EqualTo(60));
            Assert.That(policy.CalculateFreshnessLifetime(
                BareRequest(false), expires).FreshnessLifetime,
                Is.EqualTo(75));
            HttpCacheDecision heuristicResult = policy.CalculateFreshnessLifetime(
                BareRequest(false), heuristic);
            Assert.That(heuristicResult.FreshnessLifetime, Is.EqualTo(86400));
            Assert.That(heuristicResult.IsHeuristicFreshness, Is.True);
        });
    }

    [Test]
    public void FreshnessCoversDisabledIneligibleFutureAndConflictingHeuristics()
    {
        HttpCacheRequestContext disabled = BareRequest(false);
        disabled.AllowHeuristicFreshness = false;
        HttpCacheResponseContext candidate = TimedResponse(HttpStatusCodes.Ok);
        candidate.HasLastModified = true;
        candidate.LastModified = candidate.Date.AddDays(-1);
        HttpCacheResponseContext ineligible = TimedResponse(HttpStatusCodes.Found);
        ineligible.HasLastModified = true;
        ineligible.LastModified = ineligible.Date.AddDays(-1);
        HttpCacheResponseContext future = TimedResponse(HttpStatusCodes.Ok);
        future.HasLastModified = true;
        future.LastModified = future.Date.AddMinutes(1);
        HttpCacheResponseContext conflict = TimedResponse(HttpStatusCodes.Ok);
        conflict.CacheControl.HasMaxAge = true;
        conflict.CacheControl.MaxAge = 60;
        conflict.CacheControl.HasConflicts = true;

        Assert.Multiple(() =>
        {
            Assert.That(policy.CalculateFreshnessLifetime(disabled, candidate)
                .HasFreshnessLifetime, Is.False);
            Assert.That(policy.CalculateFreshnessLifetime(
                BareRequest(false), ineligible).HasFreshnessLifetime, Is.False);
            Assert.That(policy.CalculateFreshnessLifetime(
                BareRequest(false), future).FreshnessLifetime, Is.Zero);
            Assert.That(policy.CalculateFreshnessLifetime(
                BareRequest(false), conflict).FreshnessLifetime, Is.Zero);
        });
    }

    [Test]
    public void ReuseCoversFreshnessRequestConstraintsAndOnlyIfCached()
    {
        HttpCacheResponseContext response = ReusableResponse(30, 120);
        HttpCacheRequestContext maxAge = RequestContext(HttpMethods.Get, false);
        maxAge.CacheControl.HasMaxAge = true;
        maxAge.CacheControl.MaxAge = 20;
        HttpCacheRequestContext minFresh = RequestContext(HttpMethods.Get, false);
        minFresh.CacheControl.HasMinFresh = true;
        minFresh.CacheControl.MinFresh = 100;
        HttpCacheRequestContext noCache = RequestContext(HttpMethods.Get, false);
        noCache.CacheControl.NoCache = true;
        HttpCacheRequestContext onlyIfCached = RequestContext(HttpMethods.Get, false);
        onlyIfCached.CacheControl.OnlyIfCached = true;

        Assert.Multiple(() =>
        {
            Assert.That(policy.EvaluateReuse(
                RequestContext(HttpMethods.Get, false), response).Outcome,
                Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(policy.EvaluateReuse(maxAge, response).Outcome,
                Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(policy.EvaluateReuse(minFresh, response).Outcome,
                Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(policy.EvaluateReuse(noCache, response).Outcome,
                Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(policy.EvaluateReuse(
                onlyIfCached, ReusableResponse(120, 120)).Outcome,
                Is.EqualTo(HttpCacheOutcome.OnlyIfCachedMiss));
        });
    }

    [Test]
    public void ReuseCoversMaxStaleRevalidationAndDisconnectedBranches()
    {
        HttpCacheResponseContext stale = ReusableResponse(150, 120);
        HttpCacheRequestContext permitted = RequestContext(HttpMethods.Get, false);
        permitted.CacheControl.HasMaxStale = true;
        permitted.CacheControl.HasMaxStaleValue = true;
        permitted.CacheControl.MaxStale = 30;
        HttpCacheRequestContext outside = RequestContext(HttpMethods.Get, false);
        outside.CacheControl.HasMaxStale = true;
        outside.CacheControl.HasMaxStaleValue = true;
        outside.CacheControl.MaxStale = 29;
        HttpCacheRequestContext disconnected = RequestContext(HttpMethods.Get, false);
        disconnected.IsConnected = false;
        HttpCacheResponseContext prohibited = ReusableResponse(150, 120);
        prohibited.CacheControl.MustRevalidate = true;

        Assert.Multiple(() =>
        {
            Assert.That(policy.EvaluateReuse(permitted, stale).Outcome,
                Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(policy.EvaluateReuse(outside, stale).Outcome,
                Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(policy.EvaluateReuse(disconnected, stale).Outcome,
                Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(policy.EvaluateReuse(disconnected, prohibited).Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void ReuseCoversSharedAndExtensionStaleWindows()
    {
        HttpCacheRequestContext shared = RequestContext(HttpMethods.Get, true);
        shared.CacheControl.HasMaxStale = true;
        HttpCacheRequestContext privateRequest = RequestContext(HttpMethods.Get, false);
        privateRequest.CacheControl.HasMaxStale = true;
        HttpCacheResponseContext proxy = ReusableResponse(150, 120);
        proxy.CacheControl.ProxyRevalidate = true;
        HttpCacheResponseContext whileRevalidate = ReusableResponse(150, 120);
        whileRevalidate.CacheControl.HasStaleWhileRevalidate = true;
        whileRevalidate.CacheControl.StaleWhileRevalidate = 30;
        HttpCacheRequestContext onError = RequestContext(HttpMethods.Get, false);
        onError.IsErrorResponse = true;
        onError.CacheControl.HasStaleIfError = true;
        onError.CacheControl.StaleIfError = 30;

        HttpCacheDecision background = policy.EvaluateReuse(
            RequestContext(HttpMethods.Get, false), whileRevalidate);
        Assert.Multiple(() =>
        {
            Assert.That(policy.EvaluateReuse(shared, proxy).Outcome,
                Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(policy.EvaluateReuse(privateRequest, proxy).Outcome,
                Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(background.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(background.RequiresValidation, Is.True);
            Assert.That(policy.EvaluateReuse(onError, ReusableResponse(150, 120)).Outcome,
                Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(policy.EvaluateReuse(onError, ReusableResponse(151, 120)).Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void RevalidationBuildsOrdinaryStrongWeakAndDateValidators()
    {
        HttpCacheRequestContext ordinary = RequestContext(HttpMethods.Get, false);
        HttpCacheResponseContext validators = ReusableResponse(120, 120);
        AddField(validators.Response!.Headers, HttpHeaderNames.ETag, "W/\"v1\"");
        AddField(validators.Response.Headers, HttpHeaderNames.LastModified, BaseDate);
        HttpRequest ordinaryResult = policy.BuildRevalidationRequest(
            ordinary,
            validators);

        HttpCacheRequestContext strongRange = RequestContext(HttpMethods.Get, false);
        AddField(strongRange.Request!.Headers, HttpHeaderNames.Range, "bytes=0-9");
        HttpCacheResponseContext strong = ReusableResponse(120, 120);
        AddField(strong.Response!.Headers, HttpHeaderNames.ETag, "\"strong\"");
        HttpRequest strongResult = policy.BuildRevalidationRequest(strongRange, strong);

        HttpCacheRequestContext dateRange = RequestContext(HttpMethods.Get, false);
        AddField(dateRange.Request!.Headers, HttpHeaderNames.Range, "bytes=0-9");
        HttpCacheResponseContext dated = ReusableResponse(120, 120);
        dated.HasLastModified = true;
        dated.LastModified = BaseTime();
        HttpRequest dateResult = policy.BuildRevalidationRequest(dateRange, dated);

        Assert.Multiple(() =>
        {
            Assert.That(Value(ordinaryResult.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("W/\"v1\""));
            Assert.That(Value(ordinaryResult.Headers, HttpHeaderNames.IfModifiedSince),
                Is.EqualTo(BaseDate));
            Assert.That(Value(strongResult.Headers, HttpHeaderNames.IfRange),
                Is.EqualTo("\"strong\""));
            Assert.That(Value(dateResult.Headers, HttpHeaderNames.IfRange),
                Is.EqualTo(BaseDate));
            Assert.That(Value(ordinary.Request!.Headers, HttpHeaderNames.IfNoneMatch),
                Is.Empty);
        });
    }

    [Test]
    public void NotModifiedMergeSelectsMatchingResponsesAndProtectsContentMetadata()
    {
        HttpCacheResponseContext first = ValidationContext("\"same\"");
        HttpCacheResponseContext second = ValidationContext("\"same\"");
        HttpCacheResponseContext different = ValidationContext("\"other\"");
        first.MatchingResponseContexts.Add(second);
        first.MatchingResponseContexts.Add(different);
        AddField(first.Response!.Headers, HttpHeaderNames.ContentLength, "3");
        AddField(first.Response.Headers, "X-State", "old");
        AddField(second.Response!.Headers, "X-State", "old");
        AddField(different.Response!.Headers, "X-State", "different");
        HttpResponse validation = NotModified("\"same\"");
        AddField(validation.Headers, HttpHeaderNames.ContentLength, "999");
        AddField(validation.Headers, "X-State", "new");

        HttpCacheValidationResult result = policy.MergeNotModifiedResponse(
            first,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.True);
            Assert.That(result.AffectedResponses,
                Is.EqualTo(new[] { first.Response, second.Response }));
            Assert.That(Value(first.Response.Headers, "X-State"), Is.EqualTo("new"));
            Assert.That(Value(second.Response.Headers, "X-State"), Is.EqualTo("new"));
            Assert.That(Value(different.Response.Headers, "X-State"),
                Is.EqualTo("different"));
            Assert.That(Value(first.Response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo("3"));
            Assert.That(first.Response.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void NotModifiedMergeLeavesMismatchedValidatorsUntouched()
    {
        HttpCacheResponseContext stored = ValidationContext("\"stored\"");
        AddField(stored.Response!.Headers, "X-State", "old");
        HttpResponse validation = NotModified("\"other\"");
        AddField(validation.Headers, "X-State", "new");

        HttpCacheValidationResult result = policy.MergeNotModifiedResponse(
            stored,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.False);
            Assert.That(result.AffectedResponses, Is.Empty);
            Assert.That(Value(stored.Response.Headers, "X-State"), Is.EqualTo("old"));
        });
    }

    [Test]
    public void InvalidationCoversSafeErrorSuccessfulAndSameOriginTargets()
    {
        HttpRequest unsafeRequest = AbsoluteRequest(
            HttpMethods.Put,
            "HTTPS://Example.TEST:443/a/./item");
        HttpResponse success = Response(HttpStatusCodes.NoContent);
        AddField(success.Headers, HttpHeaderNames.Location, "/next#part");
        AddField(success.Headers, HttpHeaderNames.ContentLocation,
            "https://other.test/content");

        HttpCacheDecision invalidated = policy.EvaluateInvalidation(
            unsafeRequest,
            success);

        Assert.Multiple(() =>
        {
            Assert.That(invalidated.Outcome, Is.EqualTo(HttpCacheOutcome.Invalidate));
            Assert.That(invalidated.InvalidationTargets, Is.EqualTo(new[]
            {
                "https://example.test/a/item",
                "https://example.test/next"
            }));
            Assert.That(policy.EvaluateInvalidation(
                AbsoluteRequest(HttpMethods.Get, "https://example.test/item"),
                Response(HttpStatusCodes.Ok)).Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(policy.EvaluateInvalidation(
                AbsoluteRequest(HttpMethods.Delete, "https://example.test/item"),
                Response(HttpStatusCodes.InternalServerError)).Outcome,
                Is.EqualTo(HttpCacheOutcome.Unusable));
        });
    }

    [Test]
    public void CachedResponseBuildsFullHeadAndConditionalResponses()
    {
        HttpCacheResponseContext stored = CachedResponse(
            Encoding.ASCII.GetBytes("0123456789"));
        AddField(stored.Response!.Headers, HttpHeaderNames.ETag, "\"tag\"");
        AddField(stored.Response.Headers, HttpHeaderNames.ContentLength, "10");
        AddField(stored.Response.Headers, HttpHeaderNames.Connection, "X-Hop");
        AddField(stored.Response.Headers, "X-Hop", "remove");
        HttpCacheDecision full = policy.BuildCachedResponse(
            RequestContext(HttpMethods.Get, false),
            stored);
        HttpCacheDecision head = policy.BuildCachedResponse(
            RequestContext(HttpMethods.Head, false),
            stored);
        HttpCacheRequestContext conditional = RequestContext(HttpMethods.Get, false);
        AddField(conditional.Request!.Headers, HttpHeaderNames.IfNoneMatch, "W/\"tag\"");
        HttpCacheDecision notModified = policy.BuildCachedResponse(conditional, stored);

        Assert.Multiple(() =>
        {
            Assert.That(full.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(full.Response!.Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("0123456789")));
            Assert.That(Value(full.Response.Headers, HttpHeaderNames.Age),
                Is.EqualTo("30"));
            Assert.That(Value(full.Response.Headers, "X-Hop"), Is.Empty);
            Assert.That(head.Response!.Content, Is.Empty);
            Assert.That(notModified.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(notModified.Response.Content, Is.Empty);
        });
    }

    [Test]
    public void CachedResponseBuildsSingleMultipleAndUnsatisfiableRanges()
    {
        HttpCacheRequestContext singleRequest = RequestContext(HttpMethods.Get, false);
        AddField(singleRequest.Request!.Headers, HttpHeaderNames.Range, "bytes=2-5");
        HttpCacheRequestContext multipleRequest = RequestContext(HttpMethods.Get, false);
        AddField(multipleRequest.Request!.Headers, HttpHeaderNames.Range, "bytes=0-1,8-9");
        HttpCacheRequestContext missingRequest = RequestContext(HttpMethods.Get, false);
        AddField(missingRequest.Request!.Headers, HttpHeaderNames.Range, "bytes=20-30");

        HttpCacheDecision single = policy.BuildCachedResponse(
            singleRequest,
            CachedResponse(Encoding.ASCII.GetBytes("0123456789")));
        HttpCacheDecision multiple = policy.BuildCachedResponse(
            multipleRequest,
            CachedResponse(Encoding.ASCII.GetBytes("0123456789")));
        HttpCacheDecision missing = policy.BuildCachedResponse(
            missingRequest,
            CachedResponse(Encoding.ASCII.GetBytes("0123456789")));

        Assert.Multiple(() =>
        {
            Assert.That(single.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.PartialContent));
            Assert.That(Encoding.ASCII.GetString(single.Response.Content),
                Is.EqualTo("2345"));
            Assert.That(Value(single.Response.Headers, HttpHeaderNames.ContentRange),
                Is.EqualTo("bytes 2-5/10"));
            Assert.That(multiple.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.PartialContent));
            Assert.That(Value(multiple.Response.Headers, HttpHeaderNames.ContentType),
                Does.StartWith("multipart/byteranges; boundary="));
            Assert.That(missing.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.RangeNotSatisfiable));
            Assert.That(Value(missing.Response.Headers, HttpHeaderNames.ContentRange),
                Is.EqualTo("bytes */10"));
        });
    }

    [Test]
    public void CachedIncompleteRangeRequiresRevalidation()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get, false);
        AddField(request.Request!.Headers, HttpHeaderNames.Range, "bytes=0-1");
        HttpCacheResponseContext stored = CachedResponse(new byte[] { 1, 2, 3 });
        stored.IsComplete = false;

        HttpCacheDecision result = policy.BuildCachedResponse(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(result.RequiresValidation, Is.True);
            Assert.That(result.Response, Is.Null);
        });
    }

    static HttpCacheRequestContext RequestContext(string method, bool shared)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = "/items/1";
        request.TargetForm = HttpRequestTargetForm.Origin;
        request.Scheme = "https";
        request.Authority = "example.test";
        AddField(request.Headers, HttpHeaderNames.Host, "example.test");
        HttpCacheRequestContext context = new HttpCacheRequestContext();
        context.Request = request;
        context.IsShared = shared;
        context.RequestTime = BaseTime();
        return context;
    }

    static HttpCacheRequestContext BareRequest(bool shared)
    {
        HttpCacheRequestContext context = new HttpCacheRequestContext();
        context.IsShared = shared;
        return context;
    }

    static HttpCacheResponseContext StorageResponse(int statusCode, bool complete)
    {
        HttpCacheResponseContext context = TimedResponse(statusCode);
        context.Response!.IsComplete = complete;
        context.IsComplete = complete;
        return context;
    }

    static HttpCacheResponseContext TimedResponse(int statusCode)
    {
        DateTimeOffset time = BaseTime();
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = statusCode;
        HttpCacheResponseContext context = new HttpCacheResponseContext();
        context.Response = response;
        context.RequestTime = time;
        context.ResponseTime = time;
        context.StoredTime = time;
        context.CurrentTime = time;
        context.HasDate = true;
        context.Date = time;
        return context;
    }

    static HttpCacheResponseContext ReusableResponse(long age, long freshness)
    {
        HttpCacheResponseContext context = TimedResponse(HttpStatusCodes.Ok);
        context.CurrentTime = context.ResponseTime.AddSeconds(age);
        context.CacheControl.HasMaxAge = true;
        context.CacheControl.MaxAge = freshness;
        return context;
    }

    static HttpCacheResponseContext ValidationContext(string entityTag)
    {
        HttpCacheResponseContext context = TimedResponse(HttpStatusCodes.Ok);
        context.Response!.Content = new byte[] { 1, 2, 3 };
        context.Response.IsComplete = true;
        context.IsComplete = true;
        AddField(context.Response.Headers, HttpHeaderNames.ETag, entityTag);
        return context;
    }

    static HttpResponse NotModified(string entityTag)
    {
        HttpResponse response = Response(HttpStatusCodes.NotModified);
        AddField(response.Headers, HttpHeaderNames.ETag, entityTag);
        return response;
    }

    static HttpCacheResponseContext CachedResponse(byte[] content)
    {
        HttpCacheResponseContext context = ReusableResponse(30, 120);
        context.Response!.Content = content;
        context.Response.IsComplete = true;
        context.IsComplete = true;
        AddField(context.Response.Headers, HttpHeaderNames.Date, BaseDate);
        return context;
    }

    static HttpRequest AbsoluteRequest(string method, string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        request.Scheme = "https";
        AddField(request.Headers, HttpHeaderNames.Host, "example.test");
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
        return response;
    }

    static DateTimeOffset BaseTime()
    {
        return DateTimeOffset.Parse("1994-11-06T08:49:37+00:00");
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static string Value(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        return fields.Count == 0 ? "" : Encoding.ASCII.GetString(fields[0].Value);
    }
}
