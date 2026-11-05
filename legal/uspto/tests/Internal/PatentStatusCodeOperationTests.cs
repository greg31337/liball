using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentStatusCodeOperationTests
{
    [Test]
    public void GetSearchWritesQueryPagingAndMapsDescriptions()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(HttpStatusCode.OK, CompletePage());
        handler.RequestIdentifier = "header-request";
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "applicationStatusDescriptionText:Action*",
            OffsetPage = new OffsetPage { Offset = 1, Limit = 2 },
            Context = new RequestContext { RetainRawResponse = true }
        };

        PatentStatusCodeResponse result = operation.SearchGet(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Get }));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/status-codes"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("q=applicationStatusDescriptionText%3AAction%2A"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("offset=1"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("limit=2"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Total, Is.EqualTo(3));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.Page.Offset, Is.EqualTo(1));
            Assert.That(result.Page.Limit, Is.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.EqualTo(CompletePage()));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.UnknownFields["catalogVersion"], Is.EqualTo("4"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCodes[0].Code, Is.EqualTo(40));
            Assert.That(result.StatusCodes[0].Description, Is.EqualTo("Non Final Action Counted, Not Yet Mailed"));
            Assert.That(result.StatusCodes[0].UnknownFields["statusCategory"], Is.EqualTo("\"office-action\""));
            Assert.That(result.StatusCodes[1].Code, Is.EqualTo(41));
            Assert.That(result.DescriptionsByCode[40], Is.EqualTo("Non Final Action Counted, Not Yet Mailed"));
            Assert.That(result.DescriptionsByCode[41], Is.EqualTo("Non Final Action Mailed"));
        });
    }

    [Test]
    public void PostSearchWritesOnlySupportedStatusCodeFields()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(HttpStatusCode.OK, "{\"count\":0,\"statusCodeBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "applicationStatusCode:>100",
            OffsetPage = new OffsetPage { Offset = 5, Limit = 10 }
        };

        PatentStatusCodeResponse result = operation.SearchPost(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Post }));
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/status-codes"));
            Assert.That(handler.ContentTypes, Is.EqualTo(new string?[] { UsptoMediaTypes.Json }));
            Assert.That(handler.Bodies[0], Does.Contain("applicationStatusCode:\\u003E100"));
            Assert.That(handler.Bodies[0], Does.Contain("\"pagination\":{\"offset\":5,\"limit\":10}"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.StatusCodes, Is.Empty);
            Assert.That(result.DescriptionsByCode, Is.Empty);
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void GetCatalogAggregatesEveryPageAndBuildsCompleteMap()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(
            HttpStatusCode.OK,
            Page(5, "{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":\"Missassigned Application Number\"},{\"applicationStatusCode\":3,\"applicationStatusDescriptionText\":\"Proceedings Terminated\"}"),
            Page(5, "{\"applicationStatusCode\":11,\"applicationStatusDescriptionText\":\"Hague Application under International Review\"},{\"applicationStatusCode\":12,\"applicationStatusDescriptionText\":\"Pre-Exam Transmittal Review Complete\"}"),
            Page(5, "{\"applicationStatusCode\":16,\"applicationStatusDescriptionText\":\"Waiting for LR clearance\"}"));
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Offset = 0, Limit = 2 },
            MaximumPages = 4,
            MaximumResults = 10,
            Context = new RequestContext { RetainRawResponse = true }
        };

        PatentStatusCodeResponse result = operation.SearchAllGet(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Get, HttpMethod.Get, HttpMethod.Get }));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("offset=0"));
            Assert.That(handler.RequestUris[1].Query, Does.Contain("offset=2"));
            Assert.That(handler.RequestUris[2].Query, Does.Contain("offset=4"));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Count, Is.EqualTo(5));
            Assert.That(result.Total, Is.EqualTo(5));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.StatusCodes.Select(status => status.Code), Is.EqualTo(new int[] { 1, 3, 11, 12, 16 }));
            Assert.That(result.DescriptionsByCode[16], Is.EqualTo("Waiting for LR clearance"));
            Assert.That(result.RawResponse, Is.EqualTo(Page(5, "{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":\"Missassigned Application Number\"},{\"applicationStatusCode\":3,\"applicationStatusDescriptionText\":\"Proceedings Terminated\"}")));
        });
    }

    [Test]
    public void DefaultCatalogAggregationUsesPostRequests()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(
            HttpStatusCode.OK,
            Page(3, "{\"applicationStatusCode\":60,\"applicationStatusDescriptionText\":\"Final Rejection Counted, Not Yet Mailed\"},{\"applicationStatusCode\":61,\"applicationStatusDescriptionText\":\"Final Rejection Mailed\"}"),
            Page(3, "{\"applicationStatusCode\":90,\"applicationStatusDescriptionText\":\"Allowed -- Notice of Allowance Not Yet Mailed\"}"));
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest { OffsetPage = new OffsetPage { Limit = 2 } };

        PatentStatusCodeResponse result = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Post, HttpMethod.Post }));
            Assert.That(handler.Bodies[0], Does.Contain("\"offset\":0"));
            Assert.That(handler.Bodies[1], Does.Contain("\"offset\":2"));
            Assert.That(result.StatusCodes, Has.Length.EqualTo(3));
            Assert.That(result.DescriptionsByCode[90], Does.StartWith("Allowed"));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void CatalogStopsAtCallerResultLimit()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(
            HttpStatusCode.OK,
            Page(5, "{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":\"One\"},{\"applicationStatusCode\":2,\"applicationStatusDescriptionText\":\"Two\"}"),
            Page(5, "{\"applicationStatusCode\":3,\"applicationStatusDescriptionText\":\"Three\"}"));
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumResults = 3
        };

        PatentStatusCodeResponse result = operation.SearchAllPost(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Has.Count.EqualTo(2));
            Assert.That(handler.Bodies[1], Does.Contain("\"limit\":2"));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.DescriptionsByCode.Keys, Is.EqualTo(new int[] { 1, 2, 3 }));
        });
    }

    [TestCase("{\"statusCodeBag\":[]}", "$.count")]
    [TestCase("{\"count\":-1,\"statusCodeBag\":[]}", "$.count")]
    [TestCase("{\"count\":1}", "$.statusCodeBag")]
    [TestCase("{\"count\":1,\"statusCodeBag\":{}}", "$.statusCodeBag")]
    [TestCase("{\"count\":1,\"statusCodeBag\":[false]}", "$.statusCodeBag[0]")]
    [TestCase("{\"count\":1,\"statusCodeBag\":[{}]}", "$.statusCodeBag[0].applicationStatusCode")]
    [TestCase("{\"count\":1,\"statusCodeBag\":[{\"applicationStatusCode\":{},\"applicationStatusDescriptionText\":\"x\"}]}", "$.statusCodeBag[0].applicationStatusCode")]
    [TestCase("{\"count\":1,\"statusCodeBag\":[{\"applicationStatusCode\":1}]}", "$.statusCodeBag[0].applicationStatusDescriptionText")]
    [TestCase("{\"count\":1,\"statusCodeBag\":[{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":{}}]}", "$.statusCodeBag[0].applicationStatusDescriptionText")]
    [TestCase("{\"count\":2,\"statusCodeBag\":[{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":\"One\"},{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":\"Again\"}]}", "$.statusCodeBag[1].applicationStatusCode")]
    public void MalformedResponsesReportExactPaths(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(json, new SearchRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent status-code search"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [Test]
    public void PageCannotExtendBeyondReportedTotal()
    {
        SearchRequest request = new SearchRequest { OffsetPage = new OffsetPage { Offset = 2, Limit = 2 } };

        ParseException exception = Assert.Throws<ParseException>(() => Parse(Page(2, "{\"applicationStatusCode\":1,\"applicationStatusDescriptionText\":\"One\"}"), request));

        Assert.That(exception.DataPath, Is.EqualTo("$.statusCodeBag"));
    }

    [Test]
    public void RemoteFailureUsesSharedErrorMapping()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(HttpStatusCode.TooManyRequests, "{\"error\":\"Too Many Requests\"}");
        handler.RetryAfter = "2";
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));

        RateLimitException exception = Assert.Throws<RateLimitException>(() => operation.SearchGet(new SearchRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(2000));
        });
    }

    [Test]
    public void UnsupportedStructuredOptionsFailBeforeSending()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(HttpStatusCode.OK, "{\"count\":0,\"statusCodeBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Filters = new SearchFilter[] { new SearchFilter { Field = "applicationStatusCode", Operator = SearchOperator.Equal, Value = "60" } }
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchPost(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("UNSUPPORTED_STATUS_CODE_SEARCH_OPTION"));
            Assert.That(handler.Methods, Is.Empty);
        });
    }

    [Test]
    public void CancellationStopsBeforeSearch()
    {
        RecordingStatusCodeHandler handler = new RecordingStatusCodeHandler(HttpStatusCode.OK, "{\"count\":0,\"statusCodeBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentStatusCodeOperation operation = new PatentStatusCodeOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SearchRequest request = new SearchRequest { Context = new RequestContext { CancellationToken = cancellation.Token } };

        Assert.Throws<OperationCanceledException>(() => operation.SearchGet(request));

        Assert.That(handler.Methods, Is.Empty);
    }

    [Test]
    public void ModelDefaultsAreUsableEmptyCollections()
    {
        PatentStatusCodeResponse response = new PatentStatusCodeResponse();
        PatentStatusCode statusCode = new PatentStatusCode();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCodes, Is.Empty);
            Assert.That(response.DescriptionsByCode, Is.Empty);
            Assert.That(response.UnknownFields, Is.Empty);
            Assert.That(statusCode.UnknownFields, Is.Empty);
        });
    }

    private static PatentStatusCodeResponse Parse(string json, SearchRequest request)
    {
        return OdpPatentStatusCodeParser.Parse(json, request, new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/status-codes"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        }, null);
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
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

    private static string CompletePage()
    {
        return "{\"count\":3,\"statusCodeBag\":[{\"applicationStatusCode\":40,\"applicationStatusDescriptionText\":\"Non Final Action Counted, Not Yet Mailed\",\"statusCategory\":\"office-action\"},{\"applicationStatusCode\":41,\"applicationStatusDescriptionText\":\"Non Final Action Mailed\"}],\"requestIdentifier\":\"body-request\",\"catalogVersion\":4}";
    }

    private static string Page(int total, string records)
    {
        return "{\"count\":" + total + ",\"statusCodeBag\":[" + records + "]}";
    }
}

internal class RecordingStatusCodeHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal Queue<string> Responses = new Queue<string>();
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> ContentTypes = new List<string?>();
    internal List<string> Bodies = new List<string>();
    internal string? RequestIdentifier;
    internal string? RetryAfter;

    internal RecordingStatusCodeHandler(HttpStatusCode statusCode, params string[] responses)
    {
        StatusCode = statusCode;
        int index;
        for (index = 0; index < responses.Length; index += 1)
        {
            Responses.Enqueue(responses[index]);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        string body = string.Empty;
        string? contentType = null;
        if (request.Content != null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType?.MediaType;
        }

        Bodies.Add(body);
        ContentTypes.Add(contentType);
        string responseBody = Responses.Count == 0 ? "{\"count\":0,\"statusCodeBag\":[]}" : Responses.Dequeue();
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifier);
        }

        if (RetryAfter != null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", RetryAfter);
        }

        return response;
    }
}
