using System.Text;

namespace http.Tests;

public class CacheStorageSemanticsTests
{
    [Test]
    public void DefaultCacheableStatusCanBeStoredHeuristically()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get, false);
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, true);

        HttpCacheDecision result = CacheStorageSemantics.Evaluate(request, response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(result.RequestContext, Is.SameAs(request));
            Assert.That(result.ResponseContext, Is.SameAs(response));
            Assert.That(result.Response, Is.SameAs(response.Response));
            Assert.That(result.IsHeuristicFreshness, Is.True);
        });
    }

    [Test]
    public void ExplicitFreshnessAllowsOtherwiseNonDefaultStatus()
    {
        HttpCacheResponseContext explicitResponse = ResponseContext(
            HttpStatusCodes.Found,
            true);
        explicitResponse.CacheControl.HasMaxAge = true;
        explicitResponse.CacheControl.MaxAge = 60;
        HttpCacheResponseContext unmarked = ResponseContext(HttpStatusCodes.Found, true);

        HttpCacheDecision explicitResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            explicitResponse);
        HttpCacheDecision unmarkedResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            unmarked);

        Assert.Multiple(() =>
        {
            Assert.That(explicitResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(explicitResult.IsHeuristicFreshness, Is.False);
            Assert.That(unmarkedResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void OnlyMethodsWithDefinedCachingSemanticsCanBeStored()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, true);
        response.CacheControl.Public = true;

        HttpCacheDecision getResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            response);
        HttpCacheDecision headResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Head, false),
            response);
        HttpCacheDecision putResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Put, false),
            response);
        HttpCacheDecision extensionResult = CacheStorageSemantics.Evaluate(
            RequestContext("CUSTOM", false),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(getResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(headResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(putResult.Outcome, Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(extensionResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void PostRequiresExplicitFreshnessAndMatchingContentLocation()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Post, false);
        HttpCacheResponseContext matching = ResponseContext(HttpStatusCodes.Ok, true);
        matching.CacheControl.HasMaxAge = true;
        matching.CacheControl.MaxAge = 60;
        AddField(
            matching.Response!.Headers,
            HttpHeaderNames.ContentLocation,
            "/items/1");

        HttpCacheResponseContext noFreshness = ResponseContext(HttpStatusCodes.Ok, true);
        noFreshness.CacheControl.Public = true;
        AddField(
            noFreshness.Response!.Headers,
            HttpHeaderNames.ContentLocation,
            "/items/1");

        HttpCacheResponseContext different = ResponseContext(HttpStatusCodes.Ok, true);
        different.CacheControl.HasMaxAge = true;
        different.CacheControl.MaxAge = 60;
        AddField(
            different.Response!.Headers,
            HttpHeaderNames.ContentLocation,
            "/other");

        HttpCacheDecision matchingResult = CacheStorageSemantics.Evaluate(
            request,
            matching);
        HttpCacheDecision noFreshnessResult = CacheStorageSemantics.Evaluate(
            request,
            noFreshness);
        HttpCacheDecision differentResult = CacheStorageSemantics.Evaluate(
            request,
            different);

        Assert.Multiple(() =>
        {
            Assert.That(matchingResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(noFreshnessResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(differentResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void InformationalResponsesCannotBeStored()
    {
        HttpCacheDecision result = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            ResponseContext(HttpStatusCodes.EarlyHints, true));

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.DoNotStore));
    }

    [Test]
    public void RequestAndResponseNoStoreAreEnforced()
    {
        HttpCacheRequestContext requestNoStore = RequestContext(HttpMethods.Get, false);
        requestNoStore.CacheControl.NoStore = true;
        HttpCacheResponseContext ordinary = ResponseContext(HttpStatusCodes.Ok, true);
        HttpCacheResponseContext responseNoStore = ResponseContext(
            HttpStatusCodes.Ok,
            true);
        responseNoStore.CacheControl.NoStore = true;

        HttpCacheDecision requestResult = CacheStorageSemantics.Evaluate(
            requestNoStore,
            ordinary);
        HttpCacheDecision responseResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            responseNoStore);

        Assert.Multiple(() =>
        {
            Assert.That(requestResult.Outcome, Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(responseResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void MustUnderstandOverridesNoStoreOnlyForUnderstoodStatuses()
    {
        HttpCacheResponseContext understood = ResponseContext(HttpStatusCodes.Ok, true);
        understood.CacheControl.MustUnderstand = true;
        understood.CacheControl.NoStore = true;
        HttpCacheResponseContext unknown = ResponseContext(299, true);
        unknown.CacheControl.MustUnderstand = true;
        unknown.CacheControl.NoStore = true;
        unknown.CacheControl.Public = true;

        HttpCacheDecision understoodResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            understood);
        HttpCacheDecision unknownResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            unknown);

        Assert.Multiple(() =>
        {
            Assert.That(understoodResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(unknownResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void SharedAndPrivateCachesApplyPrivateDifferently()
    {
        HttpCacheResponseContext unqualified = ResponseContext(HttpStatusCodes.Ok, true);
        unqualified.CacheControl.Private = true;
        HttpCacheResponseContext qualified = ResponseContext(HttpStatusCodes.Ok, true);
        qualified.CacheControl.Private = true;
        qualified.CacheControl.PrivateFields.Add("Set-Cookie");

        HttpCacheDecision sharedUnqualified = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, true),
            unqualified);
        HttpCacheDecision sharedQualified = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, true),
            qualified);
        HttpCacheDecision privateResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            unqualified);

        Assert.Multiple(() =>
        {
            Assert.That(sharedUnqualified.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(sharedQualified.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(privateResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
        });
    }

    [Test]
    public void AuthenticatedSharedStorageRequiresAnAllowingDirective()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get, true);
        AddField(request.Request!.Headers, HttpHeaderNames.Authorization, "scheme token");
        HttpCacheResponseContext ordinary = ResponseContext(HttpStatusCodes.Ok, true);
        HttpCacheResponseContext publicResponse = ResponseContext(HttpStatusCodes.Ok, true);
        publicResponse.CacheControl.Public = true;
        HttpCacheResponseContext sharedMaxAge = ResponseContext(HttpStatusCodes.Ok, true);
        sharedMaxAge.CacheControl.HasSharedMaxAge = true;
        sharedMaxAge.CacheControl.SharedMaxAge = 30;
        HttpCacheResponseContext mustRevalidate = ResponseContext(HttpStatusCodes.Ok, true);
        mustRevalidate.CacheControl.MustRevalidate = true;

        HttpCacheDecision ordinaryResult = CacheStorageSemantics.Evaluate(request, ordinary);
        HttpCacheDecision publicResult = CacheStorageSemantics.Evaluate(
            request,
            publicResponse);
        HttpCacheDecision sharedMaxAgeResult = CacheStorageSemantics.Evaluate(
            request,
            sharedMaxAge);
        HttpCacheDecision mustRevalidateResult = CacheStorageSemantics.Evaluate(
            request,
            mustRevalidate);

        Assert.Multiple(() =>
        {
            Assert.That(ordinaryResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(publicResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(sharedMaxAgeResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(mustRevalidateResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
        });
    }

    [Test]
    public void IncompleteStorageIsLimitedToGet200AndUnderstoodPartialResponses()
    {
        HttpCacheResponseContext incompleteOk = ResponseContext(HttpStatusCodes.Ok, false);
        HttpCacheResponseContext incompleteNotFound = ResponseContext(
            HttpStatusCodes.NotFound,
            false);
        HttpCacheResponseContext partial = ResponseContext(
            HttpStatusCodes.PartialContent,
            false);
        partial.Response!.IsComplete = true;
        AddField(
            partial.Response!.Headers,
            HttpHeaderNames.ContentRange,
            "bytes 0-4/10");
        HttpCacheResponseContext unsupportedPartial = ResponseContext(
            HttpStatusCodes.PartialContent,
            false);
        unsupportedPartial.Response!.IsComplete = true;
        AddField(
            unsupportedPartial.Response!.Headers,
            HttpHeaderNames.ContentRange,
            "items 0-4/10");
        HttpCacheResponseContext truncatedPartial = ResponseContext(
            HttpStatusCodes.PartialContent,
            false);
        AddField(
            truncatedPartial.Response!.Headers,
            HttpHeaderNames.ContentRange,
            "bytes 0-4/10");
        truncatedPartial.Response.IsComplete = false;

        HttpCacheDecision incompleteOkResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            incompleteOk);
        HttpCacheDecision incompleteHeadResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Head, false),
            incompleteOk);
        HttpCacheDecision incompleteNotFoundResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            incompleteNotFound);
        HttpCacheDecision partialResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            partial);
        HttpCacheDecision unsupportedResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            unsupportedPartial);
        HttpCacheDecision truncatedResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            truncatedPartial);

        Assert.Multiple(() =>
        {
            Assert.That(incompleteOkResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(incompleteHeadResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(incompleteNotFoundResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(partialResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(unsupportedResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(truncatedResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void MultipartPartialResponseIsRecognizedWithoutTopLevelContentRange()
    {
        HttpCacheResponseContext response = ResponseContext(
            HttpStatusCodes.PartialContent,
            true);
        AddField(
            response.Response!.Headers,
            HttpHeaderNames.ContentType,
            "multipart/byteranges; boundary=example");

        HttpCacheDecision result = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, true),
            response);

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
    }

    [Test]
    public void PublicWithoutFreshnessIsHeuristicButUnknownExtensionsAreIgnored()
    {
        HttpCacheResponseContext publicResponse = ResponseContext(
            HttpStatusCodes.Found,
            true);
        publicResponse.CacheControl.Public = true;
        AddExtension(publicResponse.CacheControl, "community", "group");
        HttpCacheResponseContext extensionOnly = ResponseContext(
            HttpStatusCodes.Found,
            true);
        AddExtension(extensionOnly.CacheControl, "community", "group");
        HttpCacheResponseContext extensionNoStore = ResponseContext(
            HttpStatusCodes.Ok,
            true);
        extensionNoStore.CacheControl.NoStore = true;
        AddExtension(extensionNoStore.CacheControl, "override", "yes");

        HttpCacheDecision publicResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            publicResponse);
        HttpCacheDecision extensionOnlyResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            extensionOnly);
        HttpCacheDecision extensionNoStoreResult = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            extensionNoStore);

        Assert.Multiple(() =>
        {
            Assert.That(publicResult.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(publicResult.IsHeuristicFreshness, Is.True);
            Assert.That(extensionOnlyResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
            Assert.That(extensionNoStoreResult.Outcome,
                Is.EqualTo(HttpCacheOutcome.DoNotStore));
        });
    }

    [Test]
    public void ConflictingCacheControlDirectivesPreventStorage()
    {
        HttpCacheResponseContext response = ResponseContext(HttpStatusCodes.Ok, true);
        response.CacheControl.HasConflicts = true;

        HttpCacheDecision result = CacheStorageSemantics.Evaluate(
            RequestContext(HttpMethods.Get, false),
            response);

        Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.DoNotStore));
    }

    static HttpCacheRequestContext RequestContext(string method, bool shared)
    {
        HttpRequest request = new HttpRequest();
        request.Method = method;
        request.Target = "/items/1";
        request.TargetForm = HttpRequestTargetForm.Origin;
        request.Scheme = "https";
        request.Authority = "example.test";
        HttpCacheRequestContext context = new HttpCacheRequestContext();
        context.Request = request;
        context.IsShared = shared;
        return context;
    }

    static HttpCacheResponseContext ResponseContext(int statusCode, bool complete)
    {
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
        response.IsComplete = complete;
        HttpCacheResponseContext context = new HttpCacheResponseContext();
        context.Response = response;
        context.IsComplete = complete;
        return context;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static void AddExtension(HttpCacheControl control, string name, string value)
    {
        HttpParameter extension = new HttpParameter();
        extension.Name = name;
        extension.HasValue = true;
        extension.Value = Encoding.ASCII.GetBytes(value);
        control.Extensions.Add(extension);
    }
}
