using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentApplicationSearchOperationTests
{
    [Test]
    public void GetSearchWritesSelectedFieldsFiltersRangesFacetsSortsAndPaging()
    {
        RecordingPatentSearchHandler handler = new RecordingPatentSearchHandler(HttpStatusCode.OK, CompleteResponse());
        handler.RequestIdentifier = "header-request";
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));
        SearchRequest request = CreateCompleteRequest();
        request.Context.RetainRawResponse = true;

        PatentApplicationResponse result = operation.SearchGet(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/search"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("fields=applicationNumberText%2CapplicationMetaData.patentNumber"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("filters=applicationMetaData.applicationTypeCode%20UTL%2CDES"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("rangeFilters=applicationMetaData.grantDate%202024-01-01%3A2024-12-31"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("sort=applicationMetaData.filingDate%20desc%2CapplicationNumberText%20asc"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("facets=applicationMetaData.applicationStatusCode"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("offset=25"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("limit=10"));
            Assert.That(handler.ApiKey, Is.EqualTo("test-key"));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Summaries[0].ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
        });
    }

    [Test]
    public void PostSearchWritesTheCanonicalJsonBody()
    {
        RecordingPatentSearchHandler handler = new RecordingPatentSearchHandler(HttpStatusCode.OK, "{\"count\":0,\"patentFileWrapperDataBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));
        SearchRequest request = CreateCompleteRequest();

        PatentApplicationResponse result = operation.SearchPost(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUri?.AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/applications/search"));
            Assert.That(handler.ContentType, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.Body, Does.Contain("\"filters\":[{\"name\":\"applicationMetaData.applicationTypeCode\",\"value\":[\"UTL\",\"DES\"]}]"));
            Assert.That(handler.Body, Does.Contain("\"rangeFilters\":[{\"field\":\"applicationMetaData.grantDate\",\"valueFrom\":\"2024-01-01\",\"valueTo\":\"2024-12-31\"}]"));
            Assert.That(handler.Body, Does.Contain("\"sort\":[{\"field\":\"applicationMetaData.filingDate\",\"order\":\"desc\"},{\"field\":\"applicationNumberText\",\"order\":\"asc\"}]"));
            Assert.That(handler.Body, Does.Contain("\"fields\":[\"applicationNumberText\",\"applicationMetaData.patentNumber\"]"));
            Assert.That(handler.Body, Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(handler.Body, Does.Contain("\"facets\":[\"applicationMetaData.applicationStatusCode\"]"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Summaries, Is.Empty);
        });
    }

    [Test]
    public void GetAndPostProduceEquivalentResultsFromTheFullFixture()
    {
        string fixture = CompleteResponse();
        RecordingPatentSearchHandler getHandler = new RecordingPatentSearchHandler(HttpStatusCode.OK, fixture);
        RecordingPatentSearchHandler postHandler = new RecordingPatentSearchHandler(HttpStatusCode.OK, fixture);
        using HttpClient getClient = new HttpClient(getHandler);
        using HttpClient postClient = new HttpClient(postHandler);
        PatentApplicationSearchOperation getOperation = new PatentApplicationSearchOperation(getClient, CreateOptions(getClient));
        PatentApplicationSearchOperation postOperation = new PatentApplicationSearchOperation(postClient, CreateOptions(postClient));
        SearchRequest getRequest = CreateCompleteRequest();
        SearchRequest postRequest = CreateCompleteRequest();
        getRequest.Context.RetainRawResponse = true;
        postRequest.Context.RetainRawResponse = true;

        PatentApplicationResponse getResult = getOperation.SearchGet(getRequest);
        PatentApplicationResponse postResult = postOperation.SearchPost(postRequest);

        Assert.Multiple(() =>
        {
            Assert.That(getHandler.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(postHandler.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(getResult.Count, Is.EqualTo(postResult.Count));
            Assert.That(getResult.Summaries[0].ApplicationNumber, Is.EqualTo(postResult.Summaries[0].ApplicationNumber));
            Assert.That(getResult.Summaries[0].InventionTitle, Is.EqualTo(postResult.Summaries[0].InventionTitle));
            Assert.That(getResult.Summaries[0].AdditionalFields, Is.EqualTo(postResult.Summaries[0].AdditionalFields));
            Assert.That(getResult.Facets[0].Field, Is.EqualTo(postResult.Facets[0].Field));
            Assert.That(getResult.Facets[0].Values[0].Count, Is.EqualTo(postResult.Facets[0].Values[0].Count));
            Assert.That(getResult.RawResponse, Is.EqualTo(fixture));
            Assert.That(postResult.RawResponse, Is.EqualTo(fixture));
            Assert.That(getResult.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(postResult.RequestIdentifier, Is.EqualTo("body-request"));
        });
    }

    [Test]
    public void SearchMapsRemoteFailuresThroughTheSharedErrorPolicy()
    {
        RecordingPatentSearchHandler handler = new RecordingPatentSearchHandler(HttpStatusCode.TooManyRequests, "{\"error\":\"Too Many Requests\"}");
        handler.RetryAfter = "3";
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));

        RateLimitException exception = Assert.Throws<RateLimitException>(() => operation.SearchGet(new SearchRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.HasRetryAfterMilliseconds, Is.True);
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(3000L));
        });
    }

    [Test]
    public void SuccessfulHttpResponseWithMalformedPayloadThrowsParseException()
    {
        RecordingPatentSearchHandler handler = new RecordingPatentSearchHandler(
            HttpStatusCode.OK,
            "{\"count\":1,\"patentFileWrapperDataBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));

        ParseException failure = Assert.Throws<ParseException>(() => operation.SearchGet(new SearchRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(failure.DataPath, Is.EqualTo("$.patentFileWrapperDataBag"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SearchRequiresARequestContextBeforeSending()
    {
        RecordingPatentSearchHandler handler = new RecordingPatentSearchHandler(HttpStatusCode.OK, "{\"count\":0}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest { Context = null! };

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchPost(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception.FieldName, Is.EqualTo("Context"));
            Assert.That(exception.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    [Test]
    public void ParserReadsSummaryFacetsUnknownFieldsAndBodyRequestIdentifier()
    {
        SearchRequest request = new SearchRequest { OffsetPage = new OffsetPage { Offset = 25L, Limit = 10 } };
        HttpResponseMetadata metadata = CreateMetadata("header-request");

        PatentApplicationResponse result = OdpJsonParser.ParsePatentApplicationSearch(CompleteResponse(), request, metadata, null);

        PatentApplicationSummary summary = result.Summaries[0];
        using JsonDocument futureMetadata = JsonDocument.Parse(summary.AdditionalFields["applicationMetaData.futureMetadata"]);
        using JsonDocument futureRecord = JsonDocument.Parse(summary.AdditionalFields["futureRecord"]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Page, Is.Not.SameAs(request.OffsetPage));
            Assert.That(result.Page.Offset, Is.EqualTo(25L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.SourceUri?.Host, Is.EqualTo("api.uspto.gov"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Provenance.HasRetrievedAt, Is.True);
            Assert.That(summary.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(summary.PatentNumber, Is.EqualTo("9362380"));
            Assert.That(summary.PublicationNumber, Is.EqualTo("US 2014-0167116 A1"));
            Assert.That(summary.InventionTitle, Is.EqualTo("Heterojunction bipolar transistor"));
            Assert.That(summary.ApplicationTypeCode, Is.EqualTo("UTL"));
            Assert.That(summary.ApplicationTypeLabel, Is.EqualTo("Utility"));
            Assert.That(summary.FilingDate, Is.EqualTo(new DateTimeOffset(2012, 12, 19, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(summary.HasFilingDate, Is.True);
            Assert.That(summary.PublicationDate, Is.EqualTo(new DateTimeOffset(2014, 6, 19, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(summary.HasPublicationDate, Is.True);
            Assert.That(summary.GrantDate, Is.EqualTo(new DateTimeOffset(2016, 6, 7, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(summary.HasGrantDate, Is.True);
            Assert.That(summary.ApplicationStatusCode, Is.EqualTo(150));
            Assert.That(summary.HasApplicationStatusCode, Is.True);
            Assert.That(summary.FirstApplicantName, Is.EqualTo("Example Semiconductor S.A."));
            Assert.That(summary.FirstInventorName, Is.EqualTo("Pascal Chevalier"));
            Assert.That(summary.ArtUnit, Is.EqualTo("2814"));
            Assert.That(summary.Provenance, Is.Not.SameAs(result.Provenance));
            Assert.That(summary.Provenance.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(futureMetadata.RootElement.GetProperty("nested").GetBoolean(), Is.True);
            Assert.That(futureRecord.RootElement.GetArrayLength(), Is.EqualTo(2));
            Assert.That(result.Facets[0].Field, Is.EqualTo("applicationMetaData.applicationStatusCode"));
            Assert.That(result.Facets[0].Values[0].Value, Is.EqualTo("150"));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(44673L));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void ParserAllowsSelectedResponsesToOmitIdentifiersAndDates()
    {
        string json = FixtureLoader.LoadText("patent-applications/search-sparse.json");

        PatentApplicationResponse result = OdpJsonParser.ParsePatentApplicationSearch(json, new SearchRequest(), CreateMetadata(null), null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Summaries[0].ApplicationNumber, Is.Null);
            Assert.That(result.Summaries[0].InventionTitle, Is.EqualTo("Selected title"));
            Assert.That(result.Summaries[0].HasFilingDate, Is.False);
            Assert.That(result.Facets, Is.Empty);
        });
    }

    [TestCase("{\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[7]}", "$.patentFileWrapperDataBag[0]")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"invalid\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationMetaData\":{\"filingDate\":\"not-a-date\"}}]}", "$.patentFileWrapperDataBag[0].applicationMetaData.filingDate")]
    public void MalformedSearchRecordsHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpJsonParser.ParsePatentApplicationSearch(json, new SearchRequest(), CreateMetadata(null), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[],\"facets\":{}}", "$.facets")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[],\"facets\":[{\"status\":{}}]}", "$.facets[0].status")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[],\"facets\":[{\"status\":[{\"value\":\"x\"}]}]}", "$.facets[0].status[0].count")]
    public void MalformedFacetsHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpJsonParser.ParsePatentApplicationSearch(json, new SearchRequest(), CreateMetadata(null), null));

        Assert.That(exception.DataPath, Is.EqualTo(path));
    }

    private static SearchRequest CreateCompleteRequest()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter
                {
                    Field = "applicationMetaData.applicationTypeCode",
                    Operator = SearchOperator.In,
                    Values = new string[] { "UTL", "DES" }
                }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange
                {
                    Field = "applicationMetaData.grantDate",
                    LowerValue = "2024-01-01",
                    UpperValue = "2024-12-31"
                }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "applicationMetaData.filingDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "applicationNumberText", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "applicationNumberText", "applicationMetaData.patentNumber" }
            },
            Facets = new SearchFacetRequest[]
            {
                new SearchFacetRequest { Field = "applicationMetaData.applicationStatusCode" }
            },
            OffsetPage = new OffsetPage { Offset = 25L, Limit = 10 }
        };
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

    private static HttpResponseMetadata CreateMetadata(string? requestIdentifier)
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = requestIdentifier
        };
    }

    private static string CompleteResponse()
    {
        return FixtureLoader.LoadText("patent-applications/search-full.json");
    }
}

internal class RecordingPatentSearchHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal string ResponseBody;
    internal HttpMethod? Method;
    internal Uri? RequestUri;
    internal string? ApiKey;
    internal string? Accept;
    internal string? ContentType;
    internal string? Body;
    internal string? RequestIdentifier;
    internal string? RetryAfter;
    internal int CallCount = 0;

    internal RecordingPatentSearchHandler(HttpStatusCode statusCode, string responseBody)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        Method = request.Method;
        RequestUri = request.RequestUri;
        if (request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? keyValues))
        {
            ApiKey = keyValues.Single();
        }

        Accept = request.Headers.Accept.FirstOrDefault()?.MediaType;
        if (request.Content != null)
        {
            ContentType = request.Content.Headers.ContentType?.MediaType;
            Body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifier);
        }

        if (RetryAfter != null)
        {
            response.Headers.RetryAfter = RetryConditionHeaderValue.Parse(RetryAfter);
        }

        return response;
    }
}
