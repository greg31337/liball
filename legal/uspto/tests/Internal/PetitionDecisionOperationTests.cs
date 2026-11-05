using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PetitionDecisionOperationTests
{
    [Test]
    public void GetSearchWritesAllStructuredOptionsAndParsesTypedResponse()
    {
        RecordingPetitionHandler handler = new RecordingPetitionHandler(HttpStatusCode.OK, CompleteResponse());
        handler.HeaderRequestIdentifier = "header-request";
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionOperation operation = new PetitionDecisionOperation(client, CreateOptions(client));
        SearchRequest request = CreateCompleteRequest();
        request.Context.RetainRawResponse = true;

        PetitionDecisionResponse result = operation.SearchGet(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/petition/decisions/search"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("fields=petitionDecisionRecordIdentifier%2CdecisionDate"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("filters=decisionTypeCodeDescriptionText%20DENIED%2CGRANTED"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("rangeFilters=petitionMailDate%202021-01-01%3A2025-01-01"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("sort=petitionMailDate%20desc%2CpetitionDecisionRecordIdentifier%20asc"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("facets=businessEntityStatusCategory"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("offset=25"));
            Assert.That(handler.RequestUri?.Query, Does.Contain("limit=10"));
            Assert.That(handler.ApiKey, Is.EqualTo("test-key"));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Decisions[0].RecordIdentifier, Is.EqualTo("5d899d97-2bba-50a9-b01d-d87b38c70c55"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
        });
    }

    [Test]
    public void PostSearchWritesCanonicalPetitionSearchBody()
    {
        RecordingPetitionHandler handler = new RecordingPetitionHandler(HttpStatusCode.OK, "{\"count\":0,\"petitionDecisionDataBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionOperation operation = new PetitionDecisionOperation(client, CreateOptions(client));

        PetitionDecisionResponse result = operation.SearchPost(CreateCompleteRequest());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUri?.AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/petition/decisions/search"));
            Assert.That(handler.ContentType, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.Body, Does.Contain("\"filters\":[{\"name\":\"decisionTypeCodeDescriptionText\",\"value\":[\"DENIED\",\"GRANTED\"]}]"));
            Assert.That(handler.Body, Does.Contain("\"rangeFilters\":[{\"field\":\"petitionMailDate\",\"valueFrom\":\"2021-01-01\",\"valueTo\":\"2025-01-01\"}]"));
            Assert.That(handler.Body, Does.Contain("\"sort\":[{\"field\":\"petitionMailDate\",\"order\":\"desc\"},{\"field\":\"petitionDecisionRecordIdentifier\",\"order\":\"asc\"}]"));
            Assert.That(handler.Body, Does.Contain("\"fields\":[\"petitionDecisionRecordIdentifier\",\"decisionDate\"]"));
            Assert.That(handler.Body, Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(handler.Body, Does.Contain("\"facets\":[\"businessEntityStatusCategory\"]"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Decisions, Is.Empty);
        });
    }

    [Test]
    public void ParserRetainsCompleteDecisionDocumentsFacetsAliasesAndUnknownFields()
    {
        HttpResponseMetadata metadata = CreateMetadata("header-request", "/api/v1/petition/decisions/search");

        PetitionDecisionResponse result = OdpPetitionDecisionParser.ParseSearch(CompleteResponse(), CreateCompleteRequest(), metadata, "raw-response");

        PetitionDecision decision = result.Decisions[0];
        PetitionDecisionDocument document = decision.Documents[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(22L));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.Page.Offset, Is.EqualTo(25L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.EqualTo("raw-response"));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(decision.ApplicationNumber, Is.EqualTo("15569871"));
            Assert.That(decision.CustomerNumber, Is.EqualTo("23628"));
            Assert.That(decision.PetitionTypeCode, Is.EqualTo(551));
            Assert.That(decision.HasPetitionTypeCode, Is.True);
            Assert.That(decision.DecisionTypeCode, Is.EqualTo("C"));
            Assert.That(decision.PetitionTypeDescription, Is.EqualTo("Petition to revive"));
            Assert.That(decision.DecisionTypeDescription, Is.EqualTo("DENIED"));
            Assert.That(decision.DecisionDate, Is.EqualTo(new DateTimeOffset(2024, 3, 20, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(decision.HasDecisionDate, Is.True);
            Assert.That(decision.HasDecisionMailDate, Is.True);
            Assert.That(decision.HasPetitionMailDate, Is.True);
            Assert.That(decision.IsFirstInventorToFile, Is.True);
            Assert.That(decision.HasIsFirstInventorToFile, Is.True);
            Assert.That(decision.CourtActionIndicator, Is.False);
            Assert.That(decision.HasCourtActionIndicator, Is.True);
            Assert.That(decision.Inventors, Is.EqualTo(new[] { "David Clarke", "Daniel Rolfe" }));
            Assert.That(decision.IssuesConsidered, Is.EqualTo(new[] { "Review of Technology Center", "Revival", "{\"futureIssue\":true}" }));
            Assert.That(decision.Rules, Is.EqualTo(new[] { "37 CFR 1.26(a)", "37 CFR 1.113" }));
            Assert.That(decision.Statutes, Is.EqualTo(new[] { "35 USC 134" }));
            Assert.That(decision.UnknownFields["futureDecision"], Is.EqualTo("{\"nested\":true}"));
            Assert.That(decision.Provenance, Is.Not.SameAs(result.Provenance));
        });

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentIdentifier, Is.EqualTo("M98QOH0NWFYTX17"));
            Assert.That(document.Code, Is.EqualTo("PETDEC"));
            Assert.That(document.HasOfficialDate, Is.True);
            Assert.That(document.DownloadAlternatives, Has.Length.EqualTo(1));
            Assert.That(document.DownloadAlternatives[0].Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(document.DownloadAlternatives[0].MediaType, Is.EqualTo(UsptoMediaTypes.Pdf));
            Assert.That(document.DownloadAlternatives[0].DownloadUri?.Host, Is.EqualTo("api.uspto.gov"));
            Assert.That(document.DownloadAlternatives[0].PageCount, Is.EqualTo(2));
            Assert.That(document.DownloadAlternatives[0].HasPageCount, Is.True);
            Assert.That(document.DownloadAlternatives[0].UnknownFields["futureOption"], Is.EqualTo("9"));
            Assert.That(document.UnknownFields["futureDocument"], Is.EqualTo("true"));
            Assert.That(result.Facets[0].Field, Is.EqualTo("decisionTypeCodeDescriptionText"));
            Assert.That(result.Facets[0].Values[0].Value, Is.EqualTo("DENIED"));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(19L));
        });
    }

    [Test]
    public void DocumentAlternativesNormalizeSymbolicParameterizedAndTextXmlMediaTypes()
    {
        string response = "{\"count\":1,\"petitionDecisionDataBag\":[{\"documentBag\":[{\"downloadOptionBag\":[" +
            "{\"mimeTypeIdentifier\":\"PDF\"}," +
            "{\"mimeTypeIdentifier\":\"application/pdf; version=1.7\"}," +
            "{\"mimeTypeIdentifier\":\"text/xml; charset=utf-8\"}," +
            "{\"mimeTypeIdentifier\":\"ZIP\"}]}]}]}";

        PetitionDecisionResponse result = OdpPetitionDecisionParser.ParseSearch(
            response,
            new SearchRequest(),
            CreateMetadata(null, "/api/v1/petition/decisions/search"),
            null);
        PetitionDocumentDownloadAlternative[] alternatives = result.Decisions[0].Documents[0].DownloadAlternatives;

        Assert.Multiple(() =>
        {
            Assert.That(alternatives.Select(item => item.MediaType), Is.EqualTo(new[]
            {
                UsptoMediaTypes.Pdf,
                UsptoMediaTypes.Pdf,
                UsptoMediaTypes.TextXml,
                UsptoMediaTypes.Zip
            }));
            Assert.That(alternatives.Select(item => item.Format), Is.EqualTo(new[]
            {
                DocumentFormat.Pdf,
                DocumentFormat.Pdf,
                DocumentFormat.Xml,
                DocumentFormat.Zip
            }));
        });
    }

    [TestCase(true, "true")]
    [TestCase(false, "false")]
    public void DirectRetrievalWritesDocumentChoiceAndValidatesReturnedIdentity(bool includeDocuments, string expectedValue)
    {
        const string recordIdentifier = "5d899d97-2bba-50a9-b01d-d87b38c70c55";
        RecordingPetitionHandler handler = new RecordingPetitionHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionOperation operation = new PetitionDecisionOperation(client, CreateOptions(client));

        PetitionDecision result = operation.Get(recordIdentifier, includeDocuments, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/petition/decisions/5d899d97-2bba-50a9-b01d-d87b38c70c55"));
            Assert.That(handler.RequestUri?.Query, Is.EqualTo("?includeDocuments=" + expectedValue));
            Assert.That(result.RecordIdentifier, Is.EqualTo(recordIdentifier));
            Assert.That(result.Documents, Has.Length.EqualTo(1));
            Assert.That(result.Provenance.SourceUri?.AbsolutePath, Is.EqualTo(handler.RequestUri?.AbsolutePath));
        });

        ParseException mismatch = Assert.Throws<ParseException>(() => OdpPetitionDecisionParser.ParseSingle(CompleteResponse(), "different-record", CreateMetadata(null, "/api/v1/petition/decisions/different-record")));
        Assert.That(mismatch.DataPath, Is.EqualTo("$.petitionDecisionDataBag[0].petitionDecisionRecordIdentifier"));
    }

    [Test]
    public void SearchMapsRemoteErrorsAndRetrievalValidatesBeforeSending()
    {
        RecordingPetitionHandler handler = new RecordingPetitionHandler(HttpStatusCode.TooManyRequests, "{\"error\":\"Too Many Requests\"}");
        handler.RetryAfter = "2";
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionOperation operation = new PetitionDecisionOperation(client, CreateOptions(client));

        RateLimitException exception = Assert.Throws<RateLimitException>(() => operation.SearchGet(new SearchRequest()));
        ValidationException blank = Assert.Throws<ValidationException>(() => operation.Get(" ", false, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(2000L));
            Assert.That(blank.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [TestCase("{\"petitionDecisionDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"petitionDecisionDataBag\":[]}", "$.petitionDecisionDataBag")]
    [TestCase("{\"count\":1,\"petitionDecisionDataBag\":[7]}", "$.petitionDecisionDataBag[0]")]
    [TestCase("{\"count\":1,\"petitionDecisionDataBag\":[{\"decisionDate\":\"not-a-date\"}]}", "$.petitionDecisionDataBag[0].decisionDate")]
    [TestCase("{\"count\":1,\"petitionDecisionDataBag\":[{\"documentBag\":{}}]}", "$.petitionDecisionDataBag[0].documentBag")]
    [TestCase("{\"count\":1,\"petitionDecisionDataBag\":[{\"documentBag\":[{\"downloadOptionBag\":[{\"documentURI\":\"not a uri\"}]}]}]}", "$.petitionDecisionDataBag[0].documentBag[0].downloadOptionBag[0].documentURI")]
    public void MalformedDecisionResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPetitionDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(null, "/api/v1/petition/decisions/search"), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.SourceName, Is.EqualTo("ODP petition decision"));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    [TestCase("{\"count\":0,\"petitionDecisionDataBag\":[],\"facets\":{}}", "$.facets")]
    [TestCase("{\"count\":0,\"petitionDecisionDataBag\":[],\"facets\":[{\"status\":{}}]}", "$.facets[0].status")]
    [TestCase("{\"count\":0,\"petitionDecisionDataBag\":[],\"facets\":[{\"status\":[{\"value\":\"DENIED\"}]}]}", "$.facets[0].status[0].count")]
    public void MalformedPetitionFacetsHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPetitionDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(null, "/api/v1/petition/decisions/search"), null));

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
                    Field = "decisionTypeCodeDescriptionText",
                    Operator = SearchOperator.In,
                    Values = new string[] { "DENIED", "GRANTED" }
                }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange
                {
                    Field = "petitionMailDate",
                    LowerValue = "2021-01-01",
                    UpperValue = "2025-01-01"
                }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "petitionMailDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "petitionDecisionRecordIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "petitionDecisionRecordIdentifier", "decisionDate" }
            },
            Facets = new SearchFacetRequest[]
            {
                new SearchFacetRequest { Field = "businessEntityStatusCategory" }
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

    private static HttpResponseMetadata CreateMetadata(string? requestIdentifier, string path)
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov" + path),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = requestIdentifier
        };
    }

    private static string CompleteResponse()
    {
        return """
            {"count":1,"totalRecordCount":"22","petitionDecisionDataBag":[{"petitionDecisionRecordIdentifier":"5d899d97-2bba-50a9-b01d-d87b38c70c55","applicationNumberText":15569871,"patentNumber":11234567,"firstApplicantName":"BIOVERATIV THERAPEUTICS INC.","inventionTitle":"RECEPTOR TYROSINE KINASE BIOMARKERS","customerNumber":23628,"businessEntityStatusCategory":"Small","decisionDate":"2024-03-20","decisionMailDate":"2024-03-21","petitionMailDate":"2022-09-12","decisionPetitionTypeCode":"551","decisionTypeCode":"C","decisionPetitionTypeCodeDescriptionText":"Petition to revive","decisionTypeCodeDescriptionText":"DENIED","finalDecidingOfficeName":"OFFICE OF PETITIONS","firstInventorToFileIndicator":true,"groupArtUnitNumber":1646,"technologyCenter":"1600","inventorBag":["David Clarke",{"inventorNameText":"Daniel Rolfe"}],"actionTakenByCourtName":"None","courtActionIndicator":false,"petitionIssueConsideredTextBag":["Review of Technology Center",{"petitionIssueConsideredText":"Revival"},{"futureIssue":true}],"prosecutionStatusCodeDescriptionText":"During examination","ruleBag":["37 CFR 1.26(a)","37 CFR 1.113"],"statuteBag":["35 USC 134"],"lastIngestionDateTime":"2025-04-23T02:47:58Z","documentBag":[{"applicationNumberText":"16123123","officialDate":"2020-08-31T01:20:29-04:00","documentIdentifier":"M98QOH0NWFYTX17","documentCode":"PETDEC","documentCodeDescriptionText":"Petition Decision","documentDirectionCategory":"OUTGOING","downloadOptionBag":[{"mimeTypeIdentifier":"PDF","documentURI":"https://api.uspto.gov/api/v1/patent/application/documents/16123123/M98QOH0NWFYTX17.pdf","pageTotalQuantity":"2","futureOption":9}],"futureDocument":true}],"futureDecision":{"nested":true}}],"facets":[{"decisionTypeCodeDescriptionText":[{"value":"DENIED","count":"19"}]}],"requestIdentifier":"body-request"}
            """;
    }
}

internal class RecordingPetitionHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal string ResponseBody;
    internal HttpMethod? Method;
    internal Uri? RequestUri;
    internal string? ApiKey;
    internal string? Accept;
    internal string? ContentType;
    internal string? Body;
    internal string? HeaderRequestIdentifier;
    internal string? RetryAfter;
    internal int CallCount = 0;

    internal RecordingPetitionHandler(HttpStatusCode statusCode, string responseBody)
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
        if (HeaderRequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, HeaderRequestIdentifier);
        }

        if (RetryAfter != null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", RetryAfter);
        }

        return response;
    }
}
