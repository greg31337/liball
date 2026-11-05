using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class IdentifierResolverTests
{
    [Test]
    public void CanonicalApplicationIdentifiersDoNotRequireOnlineResolution()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string result = resolver.Resolve("14/412,875", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("14412875"));
            Assert.That(lookup.CallCount, Is.Zero);
        });
    }

    [Test]
    public void GrantResolutionUsesTheExactPatentNumberFieldAndCachesTheResult()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationMetaData.patentNumber", "12156579", Match("14412875"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string first = resolver.Resolve("US 12,156,579 B2", new RequestContext());
        string second = resolver.Resolve("US12156579B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("14412875"));
            Assert.That(second, Is.EqualTo("14412875"));
            Assert.That(lookup.CallCount, Is.EqualTo(1));
            Assert.That(lookup.Fields, Is.EqualTo(new string[] { "applicationMetaData.patentNumber" }));
            Assert.That(lookup.Values, Is.EqualTo(new string[] { "12156579" }));
        });
    }

    [Test]
    public void HistoricalGrantResolutionRemovesStoragePaddingButRetainsTheDocumentPrefix()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationMetaData.patentNumber", "D475502", Match("10412345"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string result = resolver.Resolve("US D475,502 S1", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("10412345"));
            Assert.That(lookup.Values, Is.EqualTo(new string[] { "D475502" }));
        });
    }

    [Test]
    public void PublicationResolutionUsesTheExactEarliestPublicationFieldAndPreservesKind()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationMetaData.earliestPublicationNumber", "US 2024/0012345 A9", Match("18123456"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string result = resolver.Resolve("US20240012345A9", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("18123456"));
            Assert.That(lookup.Fields, Is.EqualTo(new string[] { "applicationMetaData.earliestPublicationNumber" }));
            Assert.That(lookup.Values, Is.EqualTo(new string[] { "US 2024/0012345 A9" }));
        });
    }

    [TestCase("PCT/US2025/058371", "PCTUS2025058371")]
    [TestCase("PCT/US07/19317", "PCTUS0719317")]
    public void PctResolutionUsesTheExactApplicationNumberField(string input, string exactValue)
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationNumberText", exactValue, Match("17980669"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string result = resolver.Resolve(input, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("17980669"));
            Assert.That(lookup.Fields, Is.EqualTo(new string[] { "applicationNumberText" }));
            Assert.That(lookup.Values, Is.EqualTo(new string[] { exactValue }));
        });
    }

    [Test]
    public void AmbiguousBareNumberQueriesEveryCandidateAndSelectsTheSoleMatch()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationNumberText", "11646472", EmptyMatch());
        lookup.Add("applicationMetaData.patentNumber", "11646472", Match("15999888"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string result = resolver.Resolve("11646472", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("15999888"));
            Assert.That(lookup.CallCount, Is.EqualTo(2));
            Assert.That(lookup.Fields, Is.EqualTo(new string[] { "applicationNumberText", "applicationMetaData.patentNumber" }));
        });
    }

    [Test]
    public void AmbiguousCandidatesThatResolveToTheSameApplicationAreAccepted()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationNumberText", "11646472", Match("11646472"));
        lookup.Add("applicationMetaData.patentNumber", "11646472", Match("11646472"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        string result = resolver.Resolve("11646472", new RequestContext());

        Assert.That(result, Is.EqualTo("11646472"));
    }

    [Test]
    public void AmbiguousCandidatesThatResolveDifferentlyHaveAStableError()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationNumberText", "11646472", Match("11646472"));
        lookup.Add("applicationMetaData.patentNumber", "11646472", Match("17980669"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        ValidationException? exception = Assert.Throws<ValidationException>(() => resolver.Resolve("11646472", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.ErrorCode, Is.EqualTo("AMBIGUOUS_IDENTIFIER_MATCH"));
            Assert.That(exception.FieldName, Is.EqualTo("identifier"));
        });
    }

    [Test]
    public void ZeroMatchesThrowNotFoundAndAreNotCached()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationMetaData.patentNumber", "12156579", EmptyMatch());
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        NotFoundException? first = Assert.Throws<NotFoundException>(() => resolver.Resolve("US12156579B2", new RequestContext()));
        NotFoundException? second = Assert.Throws<NotFoundException>(() => resolver.Resolve("US12156579B2", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first!.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(first.HasStatusCode, Is.False);
            Assert.That(lookup.CallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void MultipleMatchesThrowAStableValidationError()
    {
        FakeIdentifierResolutionLookup lookup = new FakeIdentifierResolutionLookup();
        lookup.Add("applicationMetaData.patentNumber", "12156579", MultipleMatch("14412875", "18123456"));
        IdentifierResolver resolver = new IdentifierResolver(lookup);

        ValidationException? exception = Assert.Throws<ValidationException>(() => resolver.Resolve("US12156579B2", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.ErrorCode, Is.EqualTo("MULTIPLE_IDENTIFIER_MATCHES"));
            Assert.That(exception.FieldName, Is.EqualTo("identifier"));
        });
    }

    [Test]
    public void OnlineLookupBuildsABoundedExactQueryAndParsesTheApplication()
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.OK, "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}");
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOnlineOptions(client);
        OdpIdentifierResolutionLookup lookup = new OdpIdentifierResolutionLookup(client, options);

        IdentifierResolutionLookupResult result = lookup.LookupExact("applicationMetaData.patentNumber", "12156579", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.MatchCount, Is.EqualTo(1));
            Assert.That(result.ApplicationNumbers, Is.EqualTo(new string[] { "14/412,875" }));
            Assert.That(handler.RequestUri?.AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/applications/search?fields=applicationNumberText&limit=2&offset=0&q=applicationMetaData.patentNumber%3A%2212156579%22"));
            Assert.That(handler.ApiKey, Is.EqualTo("test-key"));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Json));
        });
    }

    [TestCase("US 12,156,579 B2", "https://example.test/root/api/v1/patent/applications/search?fields=applicationNumberText&limit=2&offset=0&q=applicationMetaData.patentNumber%3A%2212156579%22")]
    [TestCase("US20240012345A9", "https://example.test/root/api/v1/patent/applications/search?fields=applicationNumberText&limit=2&offset=0&q=applicationMetaData.earliestPublicationNumber%3A%22US%202024%2F0012345%20A9%22")]
    [TestCase("PCT/US2025/058371", "https://example.test/root/api/v1/patent/applications/search?fields=applicationNumberText&limit=2&offset=0&q=applicationNumberText%3A%22PCTUS2025058371%22")]
    [TestCase("PCT/US07/19317", "https://example.test/root/api/v1/patent/applications/search?fields=applicationNumberText&limit=2&offset=0&q=applicationNumberText%3A%22PCTUS0719317%22")]
    public void OnlineResolutionUsesTheExactQueryForEachIdentifierKind(string identifier, string expectedUri)
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.OK, "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}");
        using HttpClient client = new HttpClient(handler);
        IdentifierResolver resolver = new IdentifierResolver(new OdpIdentifierResolutionLookup(client, CreateOnlineOptions(client)));

        string result = resolver.Resolve(identifier, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("14412875"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.RequestUri?.AbsoluteUri, Is.EqualTo(expectedUri));
            Assert.That(handler.ApiKey, Is.EqualTo("test-key"));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Json));
        });
    }

    [Test]
    public void OnlineResolutionQueriesEveryAmbiguousCandidateAndSelectsTheSoleMatch()
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.OK, "{\"count\":0,\"patentFileWrapperDataBag\":[]}");
        handler.AddResponse(HttpStatusCode.OK, "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/999,888\"}]}", null);
        using HttpClient client = new HttpClient(handler);
        IdentifierResolver resolver = new IdentifierResolver(new OdpIdentifierResolutionLookup(client, CreateOnlineOptions(client)));

        string result = resolver.Resolve("11646472", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("15999888"));
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsoluteUri, Does.EndWith("q=applicationNumberText%3A%2211646472%22"));
            Assert.That(handler.RequestUris[1].AbsoluteUri, Does.EndWith("q=applicationMetaData.patentNumber%3A%2211646472%22"));
        });
    }

    [Test]
    public void OnlineResolutionTurnsAnEmptyMatchIntoNotFoundAndDoesNotCacheIt()
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.OK, "{\"count\":0,\"patentFileWrapperDataBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        IdentifierResolver resolver = new IdentifierResolver(new OdpIdentifierResolutionLookup(client, CreateOnlineOptions(client)));

        NotFoundException? first = Assert.Throws<NotFoundException>(() => resolver.Resolve("US12156579B2", new RequestContext()));
        NotFoundException? second = Assert.Throws<NotFoundException>(() => resolver.Resolve("US 12,156,579 B2", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first!.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(handler.CallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void OnlineResolutionRejectsMultipleMatchesFromTheBoundedLookup()
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.OK, "{\"count\":2,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"},{\"applicationNumberText\":\"18/123,456\"}]}");
        using HttpClient client = new HttpClient(handler);
        IdentifierResolver resolver = new IdentifierResolver(new OdpIdentifierResolutionLookup(client, CreateOnlineOptions(client)));

        ValidationException? exception = Assert.Throws<ValidationException>(() => resolver.Resolve("US12156579B2", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.ErrorCode, Is.EqualTo("MULTIPLE_IDENTIFIER_MATCHES"));
            Assert.That(exception.FieldName, Is.EqualTo("identifier"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PublicationKindCodesRemainDistinctAndSuccessfulResolutionsAreCached()
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.OK, "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"18/123,451\"}]}");
        handler.AddResponse(HttpStatusCode.OK, "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"18/123,459\"}]}", null);
        using HttpClient client = new HttpClient(handler);
        IdentifierResolver resolver = new IdentifierResolver(new OdpIdentifierResolutionLookup(client, CreateOnlineOptions(client)));

        string firstKind = resolver.Resolve("US20240012345A1", new RequestContext());
        string cachedFirstKind = resolver.Resolve("US 2024/0012345 A1", new RequestContext());
        string secondKind = resolver.Resolve("US20240012345A9", new RequestContext());
        string cachedSecondKind = resolver.Resolve("US 2024/0012345 A9", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(firstKind, Is.EqualTo("18123451"));
            Assert.That(cachedFirstKind, Is.EqualTo("18123451"));
            Assert.That(secondKind, Is.EqualTo("18123459"));
            Assert.That(cachedSecondKind, Is.EqualTo("18123459"));
            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsoluteUri, Does.EndWith("q=applicationMetaData.earliestPublicationNumber%3A%22US%202024%2F0012345%20A1%22"));
            Assert.That(handler.RequestUris[1].AbsoluteUri, Does.EndWith("q=applicationMetaData.earliestPublicationNumber%3A%22US%202024%2F0012345%20A9%22"));
        });
    }

    [Test]
    public void OnlineLookupTreatsSearchNotFoundAsZeroMatches()
    {
        RecordingIdentifierHandler handler = new RecordingIdentifierHandler(HttpStatusCode.NotFound, "{\"error\":\"Not Found\"}");
        using HttpClient client = new HttpClient(handler);
        OdpIdentifierResolutionLookup lookup = new OdpIdentifierResolutionLookup(client, CreateOnlineOptions(client));

        IdentifierResolutionLookupResult result = lookup.LookupExact("applicationNumberText", "PCTUS2025058371", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.MatchCount, Is.Zero);
            Assert.That(result.ApplicationNumbers, Is.Empty);
        });
    }

    [Test]
    public void OnlineLookupMapsAuthorizationAndRateFailures()
    {
        RecordingIdentifierHandler authorizationHandler = new RecordingIdentifierHandler(HttpStatusCode.Forbidden, "{\"error\":\"Forbidden\"}");
        using HttpClient authorizationClient = new HttpClient(authorizationHandler);
        OdpIdentifierResolutionLookup authorizationLookup = new OdpIdentifierResolutionLookup(authorizationClient, CreateOnlineOptions(authorizationClient));
        RecordingIdentifierHandler rateHandler = new RecordingIdentifierHandler(HttpStatusCode.TooManyRequests, "{\"error\":\"Too Many Requests\"}");
        rateHandler.RetryAfter = "3";
        using HttpClient rateClient = new HttpClient(rateHandler);
        OdpIdentifierResolutionLookup rateLookup = new OdpIdentifierResolutionLookup(rateClient, CreateOnlineOptions(rateClient));

        IdentifierResolver authorizationResolver = new IdentifierResolver(authorizationLookup);
        IdentifierResolver rateResolver = new IdentifierResolver(rateLookup);

        AuthenticationException? authorization = Assert.Throws<AuthenticationException>(() => authorizationResolver.Resolve("PCT/US2025/058371", new RequestContext()));
        RateLimitException? rate = Assert.Throws<RateLimitException>(() => rateResolver.Resolve("PCT/US2025/058371", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(authorization, Is.Not.Null);
            Assert.That(authorization!.StatusCode, Is.EqualTo(403));
            Assert.That(rate, Is.Not.Null);
            Assert.That(rate!.StatusCode, Is.EqualTo(429));
            Assert.That(rate.HasRetryAfterMilliseconds, Is.True);
            Assert.That(rate.RetryAfterMilliseconds, Is.EqualTo(3000));
        });
    }

    [TestCase("{\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    public void MalformedLookupResponsesHavePreciseDataPaths(string json, string path)
    {
        ParseException? exception = Assert.Throws<ParseException>(() => OdpIdentifierResolutionLookup.ParseResponse(json));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    private static IdentifierResolutionLookupResult EmptyMatch()
    {
        return new IdentifierResolutionLookupResult();
    }

    private static IdentifierResolutionLookupResult Match(string applicationNumber)
    {
        return new IdentifierResolutionLookupResult
        {
            MatchCount = 1,
            ApplicationNumbers = new string[] { applicationNumber }
        };
    }

    private static IdentifierResolutionLookupResult MultipleMatch(string first, string second)
    {
        return new IdentifierResolutionLookupResult
        {
            MatchCount = 2,
            ApplicationNumbers = new string[] { first, second }
        };
    }

    private static UsptoClientOptions CreateOnlineOptions(HttpClient client)
    {
        return new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
    }
}

internal class FakeIdentifierResolutionLookup : IIdentifierResolutionLookup
{
    internal Dictionary<string, IdentifierResolutionLookupResult> Results = new Dictionary<string, IdentifierResolutionLookupResult>(StringComparer.Ordinal);
    internal List<string> Fields = new List<string>();
    internal List<string> Values = new List<string>();
    internal int CallCount = 0;

    internal void Add(string fieldName, string fieldValue, IdentifierResolutionLookupResult result)
    {
        Results.Add(fieldName + "\n" + fieldValue, result);
    }

    public IdentifierResolutionLookupResult LookupExact(string fieldName, string fieldValue, RequestContext context)
    {
        CallCount += 1;
        Fields.Add(fieldName);
        Values.Add(fieldValue);
        string key = fieldName + "\n" + fieldValue;
        if (Results.ContainsKey(key))
        {
            return Results[key];
        }

        return new IdentifierResolutionLookupResult();
    }
}

internal class RecordingIdentifierHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal string ResponseBody;
    internal Uri? RequestUri;
    internal string? ApiKey;
    internal string? Accept;
    internal string? RetryAfter;
    internal int CallCount = 0;
    internal List<Uri> RequestUris = new List<Uri>();
    internal Queue<HttpStatusCode> NextStatusCodes = new Queue<HttpStatusCode>();
    internal Queue<string> NextResponseBodies = new Queue<string>();
    internal Queue<string?> NextRetryAfters = new Queue<string?>();

    internal RecordingIdentifierHandler(HttpStatusCode statusCode, string responseBody)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    internal void AddResponse(HttpStatusCode statusCode, string responseBody, string? retryAfter)
    {
        NextStatusCodes.Enqueue(statusCode);
        NextResponseBodies.Enqueue(responseBody);
        NextRetryAfters.Enqueue(retryAfter);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        RequestUri = request.RequestUri;
        if (RequestUri != null)
        {
            RequestUris.Add(RequestUri);
        }

        if (request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? values))
        {
            ApiKey = values.Single();
        }

        Accept = request.Headers.Accept.FirstOrDefault()?.MediaType;
        HttpStatusCode responseStatusCode = StatusCode;
        string responseBody = ResponseBody;
        string? retryAfter = RetryAfter;
        if (CallCount > 1 && NextStatusCodes.Count > 0)
        {
            responseStatusCode = NextStatusCodes.Dequeue();
            responseBody = NextResponseBodies.Dequeue();
            retryAfter = NextRetryAfters.Dequeue();
        }

        HttpResponseMessage response = new HttpResponseMessage(responseStatusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (retryAfter != null)
        {
            response.Headers.RetryAfter = RetryConditionHeaderValue.Parse(retryAfter);
        }

        return Task.FromResult(response);
    }
}
