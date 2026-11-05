using System.Net;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PtabProceedingOperationTests
{
    [Test]
    public void GetSearchWritesStructuredOptionsAndParsesTypedProceeding()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabProceedingOperation operation = new PtabProceedingOperation(client, CreateOptions(client));
        SearchRequest request = CreateCompleteRequest();
        request.Context.RetainRawResponse = true;

        PtabTrialProceedingResponse result = operation.SearchGet(request);

        PtabTrialProceeding proceeding = result.Proceedings[0];
        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/proceedings/search"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("fields=trialNumber%2CtrialMetaData.trialStatusCategory"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("filters=trialMetaData.trialTypeCode%20IPR"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("rangeFilters=trialMetaData.petitionFilingDate%202024-01-01%3A2024-12-31"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("sort=trialMetaData.petitionFilingDate%20desc%2CtrialNumber%20asc"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("facets=trialMetaData.trialStatusCategory"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("offset=25"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("limit=10"));
            Assert.That(handler.ApiKeys[0], Is.EqualTo("test-key"));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(2L));
            Assert.That(proceeding.TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(proceeding.HasLastModifiedAt, Is.True);
            Assert.That(proceeding.Metadata.TrialTypeCode, Is.EqualTo("102"));
            Assert.That(proceeding.Metadata.StatusCategory, Is.EqualTo("Instituted"));
            Assert.That(proceeding.Metadata.HasPetitionFilingDate, Is.True);
            Assert.That(proceeding.Metadata.PetitionFilingDate, Is.EqualTo(new DateTimeOffset(2024, 1, 30, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(proceeding.Metadata.HasAccordedFilingDate, Is.True);
            Assert.That(proceeding.Metadata.AccordedFilingDate, Is.EqualTo(new DateTimeOffset(2024, 2, 15, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(proceeding.Metadata.HasInstitutionDecisionDate, Is.True);
            Assert.That(proceeding.Metadata.InstitutionDecisionDate, Is.EqualTo(new DateTimeOffset(2024, 8, 20, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(proceeding.Metadata.HasLatestDecisionDate, Is.True);
            Assert.That(proceeding.Metadata.LatestDecisionDate, Is.EqualTo(new DateTimeOffset(2025, 1, 12, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(proceeding.Metadata.HasTerminationDate, Is.True);
            Assert.That(proceeding.Metadata.TerminationDate, Is.EqualTo(new DateTimeOffset(2025, 5, 5, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(proceeding.Metadata.HasLastModifiedAt, Is.True);
            Assert.That(proceeding.Metadata.LastModifiedAt, Is.EqualTo(new DateTimeOffset(2025, 5, 5, 10, 22, 11, TimeSpan.Zero)));
            Assert.That(proceeding.Metadata.HasLastModifiedDate, Is.True);
            Assert.That(proceeding.Metadata.LastModifiedDate, Is.EqualTo(new DateTimeOffset(2025, 5, 5, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(proceeding.PatentOwner.Name, Is.EqualTo("XYZ Holdings LLC"));
            Assert.That(proceeding.PatentOwner.Counsel.Name, Is.EqualTo("Jane Q. Attorney"));
            Assert.That(proceeding.PatentOwner.GrantDate.Year, Is.EqualTo(2019));
            Assert.That(proceeding.RegularPetitioner.Category, Is.EqualTo("Regular"));
            Assert.That(proceeding.RegularPetitioner.RealPartyInInterestName, Is.EqualTo("DEF Inc."));
            Assert.That(proceeding.Respondent.Counsel.Name, Is.EqualTo("Mary R. Lawyer"));
            Assert.That(proceeding.DerivationPetitioner.Category, Is.EqualTo("Derivation"));
            Assert.That(proceeding.DerivationPetitioner.ApplicationNumber, Is.EqualTo("13112233"));
            Assert.That(proceeding.UnknownFields["futureRecord"], Is.EqualTo("{\"version\":2}"));
            Assert.That(proceeding.Provenance, Is.Not.SameAs(result.Provenance));
            Assert.That(result.Provenance.Collection, Is.EqualTo(PtabCollection.TrialProceedings));
            Assert.That(result.Facets[0].Field, Is.EqualTo("trialMetaData.trialTypeCode"));
            Assert.That(result.Facets[0].Values[0].Name, Is.EqualTo("IPR"));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(16951L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("body-request"));
        });
    }

    [Test]
    public void SparseProceedingsPreserveAbsentValuesAndPartyAliases()
    {
        const string json = "{\"count\":2,\"patentTrialProceedingDataBag\":[{\"trialNumber\":\"DER2024-00001\",\"trialMetaData\":{},\"patentOwnerData\":{\"realPartyName\":\"Owner Alias\",\"techCenterNumber\":\"3600\",\"futureParty\":{\"version\":1}},\"regularPetitionerData\":{\"realPartyName\":\"Petitioner Alias\"},\"respondentData\":{},\"derivationPetitionerData\":{\"grantDate\":null}},{\"trialNumber\":\"IPR2024-00002\",\"lastModifiedDateTime\":null,\"trialMetaData\":null,\"patentOwnerData\":null,\"regularPetitionerData\":null,\"respondentData\":null,\"derivationPetitionerData\":null}]}";

        PtabTrialProceedingResponse result = OdpPtabProceedingParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null);
        PtabTrialProceeding aliased = result.Proceedings[0];
        PtabTrialProceeding sparse = result.Proceedings[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.HasTotal, Is.False);
            Assert.That(result.Total, Is.Zero);
            Assert.That(result.Facets, Is.Empty);
            Assert.That(aliased.PatentOwner.RealPartyInInterestName, Is.EqualTo("Owner Alias"));
            Assert.That(aliased.PatentOwner.TechnologyCenter, Is.EqualTo("3600"));
            Assert.That(aliased.PatentOwner.UnknownFields["futureParty"], Is.EqualTo("{\"version\":1}"));
            Assert.That(aliased.RegularPetitioner.RealPartyInInterestName, Is.EqualTo("Petitioner Alias"));
            Assert.That(aliased.RegularPetitioner.Category, Is.EqualTo("Regular"));
            Assert.That(aliased.DerivationPetitioner.Category, Is.EqualTo("Derivation"));
            Assert.That(aliased.DerivationPetitioner.HasGrantDate, Is.False);
            Assert.That(sparse.HasLastModifiedAt, Is.False);
            Assert.That(sparse.Metadata.HasPetitionFilingDate, Is.False);
            Assert.That(sparse.Metadata.UnknownFields, Is.Empty);
            Assert.That(sparse.PatentOwner.Name, Is.Null);
            Assert.That(sparse.RegularPetitioner.Category, Is.Null);
            Assert.That(sparse.Respondent.Counsel.Name, Is.Null);
            Assert.That(sparse.DerivationPetitioner.Category, Is.Null);
            Assert.That(sparse.UnknownFields, Is.Empty);
        });
    }

    [Test]
    public void PostSearchWritesCanonicalPtabBody()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { "{\"count\":0,\"patentTrialProceedingDataBag\":[]}" });
        using HttpClient client = new HttpClient(handler);
        PtabProceedingOperation operation = new PtabProceedingOperation(client, CreateOptions(client));

        PtabTrialProceedingResponse result = operation.SearchPost(CreateCompleteRequest());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/proceedings/search"));
            Assert.That(handler.Bodies[0], Does.Contain("\"filters\":[{\"name\":\"trialMetaData.trialTypeCode\",\"value\":[\"IPR\"]}]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"rangeFilters\":[{\"field\":\"trialMetaData.petitionFilingDate\",\"valueFrom\":\"2024-01-01\",\"valueTo\":\"2024-12-31\"}]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(result.Proceedings, Is.Empty);
        });
    }

    [Test]
    public void AllPageSearchAggregatesWithStableTrialNumberSelection()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[]
        {
            "{\"count\":2,\"totalRecordCount\":3,\"patentTrialProceedingDataBag\":[{\"trialNumber\":\"IPR-1\"},{\"trialNumber\":\"IPR-2\"}],\"requestIdentifier\":\"first\"}",
            "{\"count\":1,\"totalRecordCount\":3,\"patentTrialProceedingDataBag\":[{\"trialNumber\":\"IPR-3\"}]}"
        });
        using HttpClient client = new HttpClient(handler);
        PtabProceedingOperation operation = new PtabProceedingOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Sorts = new SearchSort[] { new SearchSort { Field = "trialNumber", Direction = SortDirection.Ascending } },
            FieldSelection = new SearchFieldSelection { Fields = new string[] { "trialMetaData.trialStatusCategory" } },
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 4,
            MaximumResults = 5
        };

        PtabTrialProceedingResponse result = operation.SearchAll(request);

        using JsonDocument first = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument second = JsonDocument.Parse(handler.Bodies[1]!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.Bodies, Has.Count.EqualTo(2));
            Assert.That(first.RootElement.GetProperty("fields")[1].GetString(), Is.EqualTo("trialNumber"));
            Assert.That(first.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.Zero);
            Assert.That(second.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(2L));
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new[] { "trialMetaData.trialStatusCategory" }));
            Assert.That(result.Proceedings, Has.Length.EqualTo(3));
            Assert.That(result.Proceedings[2].TrialNumber, Is.EqualTo("IPR-3"));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestIdentifier, Is.EqualTo("first"));
        });
    }

    [Test]
    public void DirectLookupEscapesTrialNumberAndValidatesReturnedIdentity()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabProceedingOperation operation = new PtabProceedingOperation(client, CreateOptions(client));

        PtabTrialProceeding result = operation.Get("IPR2024-00001", new RequestContext());
        ParseException mismatch = Assert.Throws<ParseException>(() => OdpPtabProceedingParser.ParseSingle(CompleteResponse(), "IPR2024-99999", CreateMetadata()));

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/proceedings/IPR2024-00001"));
            Assert.That(result.TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(mismatch.DataPath, Is.EqualTo("$.patentTrialProceedingDataBag[0].trialNumber"));
        });
    }

    [TestCase(ExportFormat.Csv, UsptoMediaTypes.Csv, "proceedings.csv", "csv")]
    [TestCase(ExportFormat.Json, UsptoMediaTypes.Json, "proceedings.json", "json")]
    public void ExportUsesThePtabProceedingDownloadRouteAndRequestedFormat(ExportFormat format, string mediaType, string fileName, string formatText)
    {
        const string content = "trial export";
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, content, mediaType, fileName);
        using HttpClient client = new HttpClient(handler);
        PtabProceedingExportOperation operation = new PtabProceedingExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest { ExportFormat = format, Output = output };

        DownloadResult result = operation.Download(new SearchRequest { Query = "trialMetaData.trialTypeCode:IPR" }, download);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/proceedings/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(mediaType));
            Assert.That(handler.RequestBody, Does.Contain("\"format\":\"" + formatText + "\""));
            Assert.That(Encoding.UTF8.GetString(output.ToArray()), Is.EqualTo(content));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
        });
    }

    [Test]
    public void SearchLookupAndExportMapTypedRemoteErrors()
    {
        RecordingPtabProceedingHandler searchHandler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Too Many Requests\"}" });
        searchHandler.StatusCode = HttpStatusCode.TooManyRequests;
        searchHandler.RetryAfter = "2";
        using HttpClient searchClient = new HttpClient(searchHandler);
        PtabProceedingOperation searchOperation = new PtabProceedingOperation(searchClient, CreateOptions(searchClient));

        RateLimitException rateLimit = Assert.Throws<RateLimitException>(() => searchOperation.SearchGet(new SearchRequest()));

        RecordingPtabProceedingHandler lookupHandler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Not Found\"}" });
        lookupHandler.StatusCode = HttpStatusCode.NotFound;
        using HttpClient lookupClient = new HttpClient(lookupHandler);
        PtabProceedingOperation lookupOperation = new PtabProceedingOperation(lookupClient, CreateOptions(lookupClient));

        NotFoundException notFound = Assert.Throws<NotFoundException>(() => lookupOperation.Get("IPR2024-99999", new RequestContext()));

        RecordingPatentExportHandler exportHandler = new RecordingPatentExportHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":\"Unavailable\"}", UsptoMediaTypes.Json, null);
        using HttpClient exportClient = new HttpClient(exportHandler);
        PtabProceedingExportOperation exportOperation = new PtabProceedingExportOperation(exportClient, CreateOptions(exportClient));
        using MemoryStream output = new MemoryStream();

        ServiceUnavailableException unavailable = Assert.Throws<ServiceUnavailableException>(() => exportOperation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = ExportFormat.Csv, Output = output }));

        Assert.Multiple(() =>
        {
            Assert.That(rateLimit.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(rateLimit.StatusCode, Is.EqualTo(429));
            Assert.That(rateLimit.HasRetryAfterMilliseconds, Is.True);
            Assert.That(rateLimit.RetryAfterMilliseconds, Is.EqualTo(2000L));
            Assert.That(notFound.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
            Assert.That(unavailable.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(unavailable.StatusCode, Is.EqualTo(503));
            Assert.That(exportHandler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/proceedings/search/download"));
        });
    }

    [TestCase("{\"patentTrialProceedingDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentTrialProceedingDataBag\":[]}", "$.patentTrialProceedingDataBag")]
    [TestCase("{\"count\":1,\"patentTrialProceedingDataBag\":[7]}", "$.patentTrialProceedingDataBag[0]")]
    [TestCase("{\"count\":1,\"patentTrialProceedingDataBag\":[{\"trialMetaData\":{\"petitionFilingDate\":\"bad\"}}]}", "$.patentTrialProceedingDataBag[0].trialMetaData.petitionFilingDate")]
    [TestCase("{\"count\":1,\"patentTrialProceedingDataBag\":[{\"patentOwnerData\":[]}]}", "$.patentTrialProceedingDataBag[0].patentOwnerData")]
    [TestCase("{\"count\":0,\"patentTrialProceedingDataBag\":[],\"facets\":[{\"status\":[{\"name\":\"Pending\"}]}]}", "$.facets[0].status[0].quantity")]
    public void MalformedProceedingResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabProceedingParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.SourceName, Is.EqualTo("ODP PTAB proceeding"));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    private static SearchRequest CreateCompleteRequest()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "trialMetaData.trialTypeCode", Operator = SearchOperator.Equal, Value = "IPR" }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange { Field = "trialMetaData.petitionFilingDate", LowerValue = "2024-01-01", UpperValue = "2024-12-31" }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "trialMetaData.petitionFilingDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "trialNumber", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "trialNumber", "trialMetaData.trialStatusCategory" }
            },
            Facets = new SearchFacetRequest[]
            {
                new SearchFacetRequest { Field = "trialMetaData.trialStatusCategory" }
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
            OverallTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 8
            }
        };
    }

    private static HttpResponseMetadata CreateMetadata()
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/proceedings/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        };
    }

    private static string CompleteResponse()
    {
        return "{\"count\":1,\"totalRecordCount\":2,\"patentTrialProceedingDataBag\":[{\"trialNumber\":\"IPR2024-00001\",\"lastModifiedDateTime\":\"2025-07-17T20:24:01Z\",\"trialMetaData\":{\"accordedFilingDate\":\"2024-02-15\",\"institutionDecisionDate\":\"2024-08-20\",\"latestDecisionDate\":\"2025-01-12\",\"petitionFilingDate\":\"2024-01-30\",\"terminationDate\":\"2025-05-05\",\"trialLastModifiedDateTime\":\"2025-05-05T10:22:11Z\",\"trialLastModifiedDate\":\"2025-05-05\",\"trialStatusCategory\":\"Instituted\",\"trialTypeCode\":\"102\",\"futureMetadata\":true},\"patentOwnerData\":{\"applicationNumberText\":\"14412875\",\"counselName\":\"Jane Q. Attorney\",\"grantDate\":\"2019-09-10\",\"groupArtUnitNumber\":\"3689\",\"inventorName\":\"Liam Johnson\",\"realPartyInInterestName\":\"ABC Corp.\",\"patentNumber\":\"US9876543B2\",\"patentOwnerName\":\"XYZ Holdings LLC\",\"technologyCenterNumber\":\"3600\"},\"regularPetitionerData\":{\"counselName\":\"John D. Counsel\",\"realPartyInInterestName\":\"DEF Inc.\"},\"respondentData\":{\"counselName\":\"Mary R. Lawyer\",\"realPartyInInterestName\":\"GHI LLC\"},\"derivationPetitionerData\":{\"applicationNumberText\":\"13112233\",\"counselName\":\"Alex M. Advocate\"},\"futureRecord\":{\"version\":2}}],\"facets\":[{\"trialMetaData.trialTypeCode\":[{\"name\":\"IPR\",\"quantity\":16951}]}],\"requestIdentifier\":\"body-request\"}";
    }
}

internal class RecordingPtabProceedingHandler : HttpMessageHandler
{
    internal string[] ResponseBodies;
    internal HttpStatusCode StatusCode = HttpStatusCode.OK;
    internal string? RetryAfter;
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> Bodies = new List<string?>();
    internal List<string?> ApiKeys = new List<string?>();

    internal RecordingPtabProceedingHandler(string[] responseBodies)
    {
        ResponseBodies = responseBodies;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int index = Methods.Count;
        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri!);
        string? body = null;
        if (request.Content != null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        Bodies.Add(body);
        string? apiKey = null;
        if (request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? values))
        {
            apiKey = values.Single();
        }

        ApiKeys.Add(apiKey);
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBodies[index], Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (RetryAfter != null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", RetryAfter);
        }

        return response;
    }
}
