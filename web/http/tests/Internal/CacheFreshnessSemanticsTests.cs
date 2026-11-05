namespace http.Tests;

public class CacheFreshnessSemanticsTests
{
    [Test]
    public void SharedMaxAgeTakesPrecedenceForSharedCaches()
    {
        HttpCacheRequestContext request = RequestContext(true);
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.CacheControl.HasSharedMaxAge = true;
        response.CacheControl.SharedMaxAge = 30;
        response.CacheControl.HasMaxAge = true;
        response.CacheControl.MaxAge = 60;
        response.HasExpires = true;
        response.Expires = response.Date.AddSeconds(90);

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            request,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(result.HasFreshnessLifetime, Is.True);
            Assert.That(result.FreshnessLifetime, Is.EqualTo(30));
            Assert.That(result.IsHeuristicFreshness, Is.False);
            Assert.That(result.RequestContext, Is.SameAs(request));
            Assert.That(result.ResponseContext, Is.SameAs(response));
            Assert.That(result.Response, Is.SameAs(response.Response));
        });
    }

    [Test]
    public void PrivateCacheIgnoresSharedMaxAgeAndUsesMaxAge()
    {
        HttpCacheRequestContext request = RequestContext(false);
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.CacheControl.HasSharedMaxAge = true;
        response.CacheControl.SharedMaxAge = 30;
        response.CacheControl.HasMaxAge = true;
        response.CacheControl.MaxAge = 60;

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            request,
            response);

        Assert.That(result.FreshnessLifetime, Is.EqualTo(60));
    }

    [Test]
    public void MaxAgeTakesPrecedenceOverExpires()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.CacheControl.HasMaxAge = true;
        response.CacheControl.MaxAge = 60;
        response.HasExpires = true;
        response.Expires = response.Date.AddSeconds(90);

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            response);

        Assert.That(result.FreshnessLifetime, Is.EqualTo(60));
    }

    [Test]
    public void ExpiresUsesDateOrResponseReceiptTimeAndClampsPastValues()
    {
        HttpCacheResponseContext withDate = ResponseContext(HttpStatusCodes.Ok);
        withDate.HasExpires = true;
        withDate.Expires = withDate.Date.AddSeconds(75);
        HttpCacheResponseContext withoutDate = ResponseContext(HttpStatusCodes.Ok);
        withoutDate.HasDate = false;
        withoutDate.HasExpires = true;
        withoutDate.Expires = withoutDate.ResponseTime.AddSeconds(45);
        HttpCacheResponseContext expired = ResponseContext(HttpStatusCodes.Ok);
        expired.HasExpires = true;
        expired.Expires = expired.Date.AddSeconds(-1);

        HttpCacheDecision dateResult = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            withDate);
        HttpCacheDecision receiptResult = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            withoutDate);
        HttpCacheDecision expiredResult = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            expired);

        Assert.Multiple(() =>
        {
            Assert.That(dateResult.FreshnessLifetime, Is.EqualTo(75));
            Assert.That(receiptResult.FreshnessLifetime, Is.EqualTo(45));
            Assert.That(expiredResult.FreshnessLifetime, Is.Zero);
            Assert.That(expiredResult.HasFreshnessLifetime, Is.True);
        });
    }

    [Test]
    public void ExpiresComparisonUsesUtcInstantsInsteadOfLocalOffsets()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.Date = new DateTimeOffset(
            2026,
            1,
            2,
            7,
            0,
            0,
            TimeSpan.FromHours(-5));
        response.HasExpires = true;
        response.Expires = new DateTimeOffset(
            2026,
            1,
            2,
            13,
            1,
            0,
            TimeSpan.FromHours(1));

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            response);

        Assert.That(result.FreshnessLifetime, Is.EqualTo(60));
    }

    [Test]
    public void LastModifiedHeuristicUsesConfiguredFractionAndMaximum()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.HasLastModified = true;
        response.LastModified = response.Date.AddSeconds(-1000000);
        HttpCacheRequestContext boundedRequest = RequestContext(false);
        HttpCacheRequestContext customRequest = RequestContext(false);
        customRequest.HeuristicFraction = 250;
        customRequest.MaximumHeuristicFreshness = 300000;

        HttpCacheDecision bounded = CacheFreshnessSemantics.Calculate(
            boundedRequest,
            response);
        HttpCacheDecision custom = CacheFreshnessSemantics.Calculate(
            customRequest,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.FreshnessLifetime, Is.EqualTo(86400));
            Assert.That(bounded.IsHeuristicFreshness, Is.True);
            Assert.That(custom.FreshnessLifetime, Is.EqualTo(250000));
            Assert.That(custom.IsHeuristicFreshness, Is.True);
        });
    }

    [Test]
    public void ExplicitFreshnessIsUsedWhenHeuristicsAreDisabled()
    {
        HttpCacheRequestContext request = RequestContext(false);
        request.AllowHeuristicFreshness = false;
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.CacheControl.HasMaxAge = true;
        response.CacheControl.MaxAge = 120;
        response.HasLastModified = true;
        response.LastModified = response.Date.AddDays(-30);

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            request,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.FreshnessLifetime, Is.EqualTo(120));
            Assert.That(result.IsHeuristicFreshness, Is.False);
        });
    }

    [Test]
    public void HeuristicsRequireEligibilityAndLastModifiedMetadata()
    {
        HttpCacheResponseContext noMetadata = ResponseContext(HttpStatusCodes.Ok);
        HttpCacheResponseContext ordinaryFound = ResponseContext(HttpStatusCodes.Found);
        ordinaryFound.HasLastModified = true;
        ordinaryFound.LastModified = ordinaryFound.Date.AddDays(-1);
        HttpCacheResponseContext publicFound = ResponseContext(HttpStatusCodes.Found);
        publicFound.CacheControl.Public = true;
        publicFound.HasLastModified = true;
        publicFound.LastModified = publicFound.Date.AddDays(-1);
        HttpCacheResponseContext privateFound = ResponseContext(HttpStatusCodes.Found);
        privateFound.CacheControl.Private = true;
        privateFound.HasLastModified = true;
        privateFound.LastModified = privateFound.Date.AddDays(-1);

        HttpCacheDecision noMetadataResult = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            noMetadata);
        HttpCacheDecision ordinaryResult = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            ordinaryFound);
        HttpCacheDecision publicResult = CacheFreshnessSemantics.Calculate(
            RequestContext(true),
            publicFound);
        HttpCacheDecision privateResult = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            privateFound);
        HttpCacheDecision sharedPrivateResult = CacheFreshnessSemantics.Calculate(
            RequestContext(true),
            privateFound);

        Assert.Multiple(() =>
        {
            Assert.That(noMetadataResult.HasFreshnessLifetime, Is.False);
            Assert.That(ordinaryResult.HasFreshnessLifetime, Is.False);
            Assert.That(publicResult.HasFreshnessLifetime, Is.True);
            Assert.That(publicResult.IsHeuristicFreshness, Is.True);
            Assert.That(privateResult.HasFreshnessLifetime, Is.True);
            Assert.That(sharedPrivateResult.HasFreshnessLifetime, Is.False);
        });
    }

    [Test]
    public void FutureLastModifiedValueProducesZeroHeuristicLifetime()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.HasLastModified = true;
        response.LastModified = response.Date.AddMinutes(1);

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasFreshnessLifetime, Is.True);
            Assert.That(result.FreshnessLifetime, Is.Zero);
            Assert.That(result.IsHeuristicFreshness, Is.True);
        });
    }

    [Test]
    public void ConflictingFreshnessDirectivesProduceExplicitZeroLifetime()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok);
        response.CacheControl.HasConflicts = true;
        response.CacheControl.HasMaxAge = true;
        response.CacheControl.MaxAge = 60;

        HttpCacheDecision result = CacheFreshnessSemantics.Calculate(
            RequestContext(false),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasFreshnessLifetime, Is.True);
            Assert.That(result.FreshnessLifetime, Is.Zero);
            Assert.That(result.IsHeuristicFreshness, Is.False);
        });
    }

    [Test]
    public void InvalidDirectiveOrHeuristicConfigurationIsRejected()
    {
        HttpCacheResponseContext invalidAge = ResponseContext(HttpStatusCodes.Ok);
        invalidAge.CacheControl.HasMaxAge = true;
        invalidAge.CacheControl.MaxAge = -1;
        HttpCacheRequestContext invalidFraction = RequestContext(false);
        invalidFraction.HeuristicFraction = 1001;
        HttpCacheRequestContext invalidMaximum = RequestContext(false);
        invalidMaximum.MaximumHeuristicFreshness = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                CacheFreshnessSemantics.Calculate(
                    RequestContext(false),
                    invalidAge));
            Assert.Throws<HttpProtocolException>(() =>
                CacheFreshnessSemantics.Calculate(
                    invalidFraction,
                    ResponseContext(HttpStatusCodes.Ok)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheFreshnessSemantics.Calculate(
                    invalidMaximum,
                    ResponseContext(HttpStatusCodes.Ok)));
        });
    }

    static HttpCacheRequestContext RequestContext(bool shared)
    {
        HttpCacheRequestContext context = new HttpCacheRequestContext();
        context.IsShared = shared;
        return context;
    }

    static HttpCacheResponseContext ResponseContext(int statusCode)
    {
        DateTimeOffset date = new DateTimeOffset(
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
        context.ResponseTime = date;
        context.HasDate = true;
        context.Date = date;
        return context;
    }
}
