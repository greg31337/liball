using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentsViewCapabilityOperationTests
{
    [Test]
    public void SuccessfulProbeUsesConfiguredEndpointCredentialAndMinimalQuery()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, SuccessBody());
        handler.RequestIdentifiers.Enqueue("pv-request-1");
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        RequestContext context = new RequestContext
        {
            Service = ApiService.Tsdr,
            RetainRawResponse = true,
            MaximumRawResponseBytes = 4096L,
            CorrelationIdentifier = "caller-correlation"
        };
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);

        PatentsViewCapabilityResult result = operation.Probe(context);
        string decodedQuery = Uri.UnescapeDataString(handler.RequestUris.Single()!.Query);
        RateLimitObservation rate = operation.RateLimiter.GetObservation(ApiService.PatentsView);

        Assert.Multiple(() =>
        {
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris.Single()!.AbsolutePath, Is.EqualTo("/custom/root/api/v1/patent"));
            Assert.That(decodedQuery, Does.Contain("q={\"_eq\":{\"patent_id\":\"0\"}}"));
            Assert.That(decodedQuery, Does.Contain("f=[\"patent_id\"]"));
            Assert.That(decodedQuery, Does.Contain("s=[{\"patent_id\":\"asc\"}]"));
            Assert.That(decodedQuery, Does.Contain("o={\"size\":1}"));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("patentsview-secret"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(result.CapabilityName, Is.EqualTo("PatentsView PatentSearch"));
            Assert.That(result.BaseUri, Is.EqualTo(options.PatentsViewBaseUri));
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.SupportsOnlineSearch, Is.True);
            Assert.That(result.RequiresApiKey, Is.True);
            Assert.That(result.HasStatusCode, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.RequestIdentifier, Is.EqualTo("pv-request-1"));
            Assert.That(result.AttemptCount, Is.EqualTo(1));
            Assert.That(result.FromCache, Is.False);
            Assert.That(result.RawResponse, Is.EqualTo(SuccessBody()));
            Assert.That(result.IsBulkDiscoveryAvailable, Is.True);
            Assert.That(result.BulkDatasetFamilies, Is.EqualTo(new[] { DatasetFamily.PatentsViewGrant, DatasetFamily.PatentsViewPreGrant, DatasetFamily.PatentsViewAnnualized, DatasetFamily.PatentsViewSorted }));
            Assert.That(result.BulkProductSearch.Query, Is.EqualTo("PatentsView"));
            Assert.That(result.BulkProductSearch.Context.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(result.BulkProductSearch.Context.CorrelationIdentifier, Is.EqualTo("caller-correlation"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(result.Provenance.SourceUri, Is.EqualTo(options.PatentsViewBaseUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.Provenance.HasRetrievedAt, Is.True);
            Assert.That(rate.Used, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProbeAppliesUriLimitToConfiguredBasePathBeforeSending()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, SuccessBody());
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.PatentsViewBaseUri = new Uri("https://example.test/a/long/configured/base/path/");
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);
        using HttpRequestMessage completeRequest = operation.CreateProbeRequest();
        options.Limits.MaximumUriCharacters = completeRequest.RequestUri!.OriginalString.Length - 1;

        ValidationException failure = Assert.Throws<ValidationException>(() => operation.Probe(new RequestContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    [Test]
    public void TransientFailureIsRetriedWithFreshCredentialAndRatePermit()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":true}");
        handler.StatusCodes.Enqueue(HttpStatusCode.OK);
        handler.ResponseBodies.Enqueue(SuccessBody());
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider { CurrentKey = "first-key" };
        handler.ProviderToRotate = provider;
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.PatentsViewApiKey = null;
        options.ApiKeyProvider = provider;
        options.Retry.AttemptCount = 2;
        options.Retry.InitialDelayMilliseconds = 5;
        options.Retry.MaximumDelayMilliseconds = 5;
        options.Retry.BudgetMilliseconds = 10;
        options.Retry.UseJitter = false;
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);
        RecordingRetryDelay delay = new RecordingRetryDelay();
        operation.RetryEngine = new RetryEngine(options.Retry, delay, new FixedRetryRandom(0.5));

        PatentsViewCapabilityResult result = operation.Probe(new RequestContext());
        RateLimitObservation rate = operation.RateLimiter.GetObservation(ApiService.PatentsView);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.AttemptCount, Is.EqualTo(2));
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "first-key", "second-key" }));
            Assert.That(provider.CallCount, Is.EqualTo(2));
            Assert.That(delay.Waits, Is.EqualTo(new long[] { 5L }));
            Assert.That(rate.Used, Is.EqualTo(2));
        });
    }

    [Test]
    public void GoneEndpointReturnsTransitionStateAndBulkAlternative()
    {
        const string secret = "gone-endpoint-secret";
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.Gone, $"{{\"error\":true,\"message\":\"do not expose {secret}\"}}");
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.PatentsViewApiKey = secret;
        options.Retry.AttemptCount = 1;
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);

        PatentsViewCapabilityResult result = operation.Probe(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.SupportsOnlineSearch, Is.False);
            Assert.That(result.IsTransitionUnavailable, Is.True);
            Assert.That(result.UnavailabilityCode, Is.EqualTo("PATENTSVIEW_TRANSITION_UNAVAILABLE"));
            Assert.That(result.UnavailabilityReason, Does.Contain("may have moved"));
            Assert.That(result.UnavailabilityReason, Does.Not.Contain(secret));
            Assert.That(result.StatusCode, Is.EqualTo(410));
            Assert.That(result.HasStatusCode, Is.True);
            Assert.That(result.AttemptCount, Is.EqualTo(1));
            Assert.That(result.IsBulkDiscoveryAvailable, Is.True);
            Assert.That(result.BulkProductSearch.Context.Service, Is.EqualTo(ApiService.Odp));
        });
    }

    [Test]
    public void MissingEndpointAndRejectedCredentialProduceDistinctCapabilityStates()
    {
        PatentsViewCapabilityHandler missingHandler = new PatentsViewCapabilityHandler(HttpStatusCode.NotFound, "{\"error\":true}");
        missingHandler.RequestIdentifiers.Enqueue("missing-route");
        using HttpClient missingClient = new HttpClient(missingHandler);
        UsptoClientOptions missingOptions = CreateOptions(missingClient);
        PatentsViewCapabilityResult missing = new PatentsViewCapabilityOperation(missingClient, missingOptions).Probe(new RequestContext());

        PatentsViewCapabilityHandler authenticationHandler = new PatentsViewCapabilityHandler(HttpStatusCode.Unauthorized, "{\"error\":true}");
        authenticationHandler.RequestIdentifiers.Enqueue("rejected-key");
        using HttpClient authenticationClient = new HttpClient(authenticationHandler);
        UsptoClientOptions authenticationOptions = CreateOptions(authenticationClient);
        PatentsViewCapabilityResult authentication = new PatentsViewCapabilityOperation(authenticationClient, authenticationOptions).Probe(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(missing.UnavailabilityCode, Is.EqualTo("PATENTSVIEW_TRANSITION_UNAVAILABLE"));
            Assert.That(missing.IsTransitionUnavailable, Is.True);
            Assert.That(missing.StatusCode, Is.EqualTo(404));
            Assert.That(missing.RequestIdentifier, Is.EqualTo("missing-route"));
            Assert.That(authentication.UnavailabilityCode, Is.EqualTo("PATENTSVIEW_AUTHENTICATION_FAILED"));
            Assert.That(authentication.IsTransitionUnavailable, Is.False);
            Assert.That(authentication.StatusCode, Is.EqualTo(401));
            Assert.That(authentication.RequestIdentifier, Is.EqualTo("rejected-key"));
            Assert.That(authentication.IsBulkDiscoveryAvailable, Is.True);
        });
    }

    [Test]
    public void MalformedAndSemanticallyInvalidSuccessBodiesReportInvalidCapability()
    {
        PatentsViewCapabilityHandler malformedHandler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, "{\"error\":");
        using HttpClient malformedClient = new HttpClient(malformedHandler);
        PatentsViewCapabilityResult malformed = new PatentsViewCapabilityOperation(malformedClient, CreateOptions(malformedClient)).Probe(new RequestContext());

        PatentsViewCapabilityHandler invalidHandler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, "{\"error\":true,\"patents\":[]}");
        using HttpClient invalidClient = new HttpClient(invalidHandler);
        PatentsViewCapabilityResult invalid = new PatentsViewCapabilityOperation(invalidClient, CreateOptions(invalidClient)).Probe(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsAvailable, Is.False);
            Assert.That(malformed.UnavailabilityCode, Is.EqualTo("PATENTSVIEW_INVALID_RESPONSE"));
            Assert.That(malformed.UnavailabilityReason, Does.Contain("malformed JSON"));
            Assert.That(invalid.IsAvailable, Is.False);
            Assert.That(invalid.UnavailabilityCode, Is.EqualTo("PATENTSVIEW_INVALID_RESPONSE"));
            Assert.That(invalid.StatusCode, Is.EqualTo(200));
            Assert.That(invalid.HasStatusCode, Is.True);
        });
    }

    [Test]
    public void RateLimitedEndpointReturnsRetryMetadataAfterPolicyIsExhausted()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.TooManyRequests, "{\"error\":true}");
        handler.RetryAfterSeconds = 7;
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Retry.AttemptCount = 1;
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);

        PatentsViewCapabilityResult result = operation.Probe(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.UnavailabilityCode, Is.EqualTo("PATENTSVIEW_RATE_LIMITED"));
            Assert.That(result.StatusCode, Is.EqualTo(429));
            Assert.That(result.IsRetryable, Is.True);
            Assert.That(result.HasRetryAfterMilliseconds, Is.True);
            Assert.That(result.RetryAfterMilliseconds, Is.EqualTo(7000L));
        });
    }

    [Test]
    public void TransientFailuresStopAtTheConfiguredAttemptCount()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":true}");
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Retry.AttemptCount = 3;
        options.Retry.InitialDelayMilliseconds = 5;
        options.Retry.MaximumDelayMilliseconds = 5;
        options.Retry.BudgetMilliseconds = 10;
        options.Retry.UseJitter = false;
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);
        RecordingRetryDelay delay = new RecordingRetryDelay();
        operation.RetryEngine = new RetryEngine(options.Retry, delay, new FixedRetryRandom(0.5));

        PatentsViewCapabilityResult result = operation.Probe(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.IsRetryable, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(503));
            Assert.That(result.AttemptCount, Is.EqualTo(3));
            Assert.That(handler.CallCount, Is.EqualTo(3));
            Assert.That(delay.Waits, Is.EqualTo(new long[] { 5L, 5L }));
        });
    }

    [Test]
    public void MemoryCacheAvoidsASecondNetworkAndRateLimitUse()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, SuccessBody());
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Cache.Mode = CacheMode.Memory;
        options.Cache.LifetimeSeconds = 60;
        options.Cache.MaximumMemoryEntries = 4;
        options.Cache.MaximumEntryBytes = 4096L;
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(client, options);

        PatentsViewCapabilityResult first = operation.Probe(new RequestContext());
        first.BulkDatasetFamilies[0] = DatasetFamily.Unknown;
        first.BulkProductSearch.Query = "changed";
        PatentsViewCapabilityResult second = operation.Probe(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(first.FromCache, Is.False);
            Assert.That(second.IsAvailable, Is.True);
            Assert.That(second.FromCache, Is.True);
            Assert.That(second.AttemptCount, Is.Zero);
            Assert.That(second.Provenance.FromCache, Is.True);
            Assert.That(second.BulkDatasetFamilies[0], Is.EqualTo(DatasetFamily.PatentsViewGrant));
            Assert.That(second.BulkProductSearch.Query, Is.EqualTo("PatentsView"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(operation.RateLimiter.GetObservation(ApiService.PatentsView).Used, Is.EqualTo(1));
        });
    }

    [Test]
    public void FileCacheIsReusableByANewProbeOperation()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-pv-capability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            PatentsViewCapabilityHandler firstHandler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, SuccessBody());
            using HttpClient firstClient = new HttpClient(firstHandler);
            UsptoClientOptions firstOptions = CreateOptions(firstClient);
            ConfigureFileCache(firstOptions, directory);
            PatentsViewCapabilityResult first = new PatentsViewCapabilityOperation(firstClient, firstOptions).Probe(new RequestContext());

            PatentsViewCapabilityHandler secondHandler = new PatentsViewCapabilityHandler(HttpStatusCode.InternalServerError, "{\"error\":true}");
            using HttpClient secondClient = new HttpClient(secondHandler);
            UsptoClientOptions secondOptions = CreateOptions(secondClient);
            ConfigureFileCache(secondOptions, directory);
            PatentsViewCapabilityResult second = new PatentsViewCapabilityOperation(secondClient, secondOptions).Probe(new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(first.IsAvailable, Is.True);
                Assert.That(first.FromCache, Is.False);
                Assert.That(firstHandler.CallCount, Is.EqualTo(1));
                Assert.That(second.IsAvailable, Is.True);
                Assert.That(second.FromCache, Is.True);
                Assert.That(secondHandler.CallCount, Is.Zero);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MissingCredentialAndUnsafeBaseUriAreRejectedBeforeTransport()
    {
        PatentsViewCapabilityHandler handler = new PatentsViewCapabilityHandler(HttpStatusCode.OK, SuccessBody());
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions missingCredentialOptions = CreateOptions(client);
        missingCredentialOptions.PatentsViewApiKey = null;
        PatentsViewCapabilityOperation missingCredential = new PatentsViewCapabilityOperation(client, missingCredentialOptions);
        AuthenticationException authentication = Assert.Throws<AuthenticationException>(() => missingCredential.Probe(new RequestContext()));

        UsptoClientOptions unsafeOptions = CreateOptions(client);
        unsafeOptions.PatentsViewBaseUri = new Uri("http://patents.example.test/");
        PatentsViewCapabilityOperation unsafeEndpoint = new PatentsViewCapabilityOperation(client, unsafeOptions);
        ValidationException validation = Assert.Throws<ValidationException>(() => unsafeEndpoint.Probe(new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(authentication!.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(authentication.Message, Does.Not.Contain("patentsview-secret"));
            Assert.That(validation!.FieldName, Is.EqualTo("PatentsViewBaseUri"));
            Assert.That(validation.ErrorCode, Is.EqualTo("HTTPS_REQUIRED"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            PatentsViewBaseUri = new Uri("https://example.test/custom/root/"),
            PatentsViewApiKey = "patentsview-secret"
        };
        options.Retry.AttemptCount = 1;
        return options;
    }

    private static void ConfigureFileCache(UsptoClientOptions options, string directory)
    {
        options.Cache.Mode = CacheMode.File;
        options.Cache.Directory = directory;
        options.Cache.LifetimeSeconds = 60;
        options.Cache.MaximumEntryBytes = 4096L;
    }

    private static string SuccessBody()
    {
        return "{\"error\":false,\"count\":0,\"total_hits\":0,\"patents\":[]}";
    }
}

internal class PatentsViewCapabilityHandler : HttpMessageHandler
{
    internal Queue<HttpStatusCode> StatusCodes = new Queue<HttpStatusCode>();
    internal Queue<string> ResponseBodies = new Queue<string>();
    internal Queue<string> RequestIdentifiers = new Queue<string>();
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri?> RequestUris = new List<Uri?>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal RotatingApiKeyProvider? ProviderToRotate = null;
    internal int RetryAfterSeconds = 0;
    internal int CallCount = 0;

    internal PatentsViewCapabilityHandler(HttpStatusCode statusCode, string responseBody)
    {
        StatusCodes.Enqueue(statusCode);
        ResponseBodies.Enqueue(responseBody);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount += 1;
        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri);
        ApiKeys.Add(request.Headers.Contains(UsptoHeaders.PatentsViewApiKey) ? request.Headers.GetValues(UsptoHeaders.PatentsViewApiKey).Single() : null);
        Accepts.Add(request.Headers.Accept.SingleOrDefault()?.MediaType);
        if (ProviderToRotate != null && CallCount == 1)
        {
            ProviderToRotate.CurrentKey = "second-key";
        }

        HttpStatusCode statusCode = StatusCodes.Count == 1 ? StatusCodes.Peek() : StatusCodes.Dequeue();
        string responseBody = ResponseBodies.Count == 1 ? ResponseBodies.Peek() : ResponseBodies.Dequeue();
        HttpResponseMessage response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromSeconds(60)
        };
        if (RequestIdentifiers.Count != 0)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifiers.Dequeue());
        }

        if (RetryAfterSeconds != 0)
        {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(RetryAfterSeconds));
        }

        return Task.FromResult(response);
    }
}
