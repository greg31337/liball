using System.Net;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PtabAppealDecisionOperationTests
{
    [Test]
    public void GetSearchUsesAppealRouteAndParsesCompleteTypedRecord()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabAppealDecisionOperation operation = new PtabAppealDecisionOperation(client, CreateOptions(client));
        SearchRequest request = CreateSearch();
        request.Context.RetainRawResponse = true;

        PtabAppealDecisionResponse result = operation.SearchGet(request);

        PtabAppealDecision record = result.Decisions[0];
        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/appeals/decisions/search"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(3L));
            Assert.That(record.AppealNumber, Is.EqualTo("2024-100001"));
            Assert.That(record.RecordCategory, Is.EqualTo("Decision"));
            Assert.That(record.HasLastModifiedAt, Is.True);
            Assert.That(record.Metadata.ApplicationTypeCategory, Is.EqualTo("Utility"));
            Assert.That(record.Metadata.HasFilingDate, Is.True);
            Assert.That(record.Metadata.HasDocketNoticeMailedDate, Is.True);
            Assert.That(record.Metadata.HasLastModifiedDate, Is.True);
            Assert.That(record.Metadata.HasLastModifiedAt, Is.True);
            Assert.That(record.Metadata.FileDownloadUri?.ToString(), Is.EqualTo("/download"));
            Assert.That(record.Metadata.UnknownFields["futureMetadata"], Is.EqualTo("9"));
            Assert.That(record.Appellant.ApplicationNumber, Is.EqualTo("17/123456"));
            Assert.That(record.Appellant.PublicationNumber, Is.EqualTo("US20240123456"));
            Assert.That(record.Appellant.RealPartyInInterestName, Is.EqualTo("Appellant LLC"));
            Assert.That(record.Appellant.Counsel.Name, Is.EqualTo("Appeal Counsel"));
            Assert.That(record.Appellant.HasPublicationDate, Is.True);
            Assert.That(record.Appellant.HasGrantDate, Is.True);
            Assert.That(record.ThirdPartyRequester.Name, Is.EqualTo("Requester Inc."));
            Assert.That(record.Examiner.Name, Is.EqualTo("Alex Examiner"));
            Assert.That(record.Examiner.Identifier, Is.EqualTo("12345"));
            Assert.That(record.Examiner.ArtUnit, Is.EqualTo("2100"));
            Assert.That(record.Document.DocumentIdentifier, Is.EqualTo("AP-170224750"));
            Assert.That(record.Document.Size, Is.EqualTo(654321L));
            Assert.That(record.Document.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(record.Decision.AppealOutcomeCategory, Is.EqualTo("Affirmed"));
            Assert.That(record.Decision.StatutesAndRules, Is.EqualTo(new[] { "35 USC 103" }));
            Assert.That(record.Decision.IssueTypes, Is.EqualTo(new[] { "Obviousness" }));
            Assert.That(record.Provenance.Collection, Is.EqualTo(PtabCollection.AppealDecisions));
            Assert.That(record.Document.Provenance.Collection, Is.EqualTo(PtabCollection.AppealDecisions));
            Assert.That(record.UnknownFields["futureRecord"], Is.EqualTo("true"));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(11L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("appeal-request"));
        });
    }

    [Test]
    public void PostSearchWritesCanonicalAppealSearchBody()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { "{\"count\":0,\"patentAppealDataBag\":[]}" });
        using HttpClient client = new HttpClient(handler);
        PtabAppealDecisionOperation operation = new PtabAppealDecisionOperation(client, CreateOptions(client));

        PtabAppealDecisionResponse result = operation.SearchPost(CreateSearch());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/appeals/decisions/search"));
            Assert.That(handler.Bodies[0], Does.Contain("\"filters\":[{\"name\":\"appealMetaData.applicationTypeCategory\",\"value\":[\"Utility\"]}]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"fields\":[\"appealNumber\",\"documentData.documentIdentifier\",\"decisionData\"]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(result.Decisions, Is.Empty);
        });
    }

    [Test]
    public void AllPageSearchAggregatesUsingStableDocumentIdentifiers()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[]
        {
            "{\"count\":2,\"totalRecordCount\":3,\"patentAppealDataBag\":[{\"appealNumber\":\"A-1\",\"documentData\":{\"documentIdentifier\":\"D-1\"}},{\"appealNumber\":\"A-1\",\"documentData\":{\"documentIdentifier\":\"D-2\"}}],\"requestIdentifier\":\"first\"}",
            "{\"count\":1,\"totalRecordCount\":3,\"patentAppealDataBag\":[{\"appealNumber\":\"A-1\",\"documentData\":{\"documentIdentifier\":\"D-3\"}}]}"
        });
        using HttpClient client = new HttpClient(handler);
        PtabAppealDecisionOperation operation = new PtabAppealDecisionOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Sorts = new SearchSort[] { new SearchSort { Field = "documentData.documentIdentifier", Direction = SortDirection.Ascending } },
            FieldSelection = new SearchFieldSelection { Fields = new string[] { "appealNumber" } },
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 4,
            MaximumResults = 5
        };

        PtabAppealDecisionResponse result = operation.SearchAll(request);

        using JsonDocument first = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument second = JsonDocument.Parse(handler.Bodies[1]!);
        Assert.Multiple(() =>
        {
            Assert.That(first.RootElement.GetProperty("fields")[1].GetString(), Is.EqualTo("documentData.documentIdentifier"));
            Assert.That(first.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.Zero);
            Assert.That(second.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(2L));
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new[] { "appealNumber" }));
            Assert.That(result.Decisions, Has.Length.EqualTo(3));
            Assert.That(result.Decisions[2].Document.DocumentIdentifier, Is.EqualTo("D-3"));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestIdentifier, Is.EqualTo("first"));
        });
    }

    [Test]
    public void DirectAndByAppealRetrievalUseDistinctRoutesAndValidateIdentities()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse(), CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabAppealDecisionOperation operation = new PtabAppealDecisionOperation(client, CreateOptions(client));

        PtabAppealDecision direct = operation.GetDocument("AP-170224750", new RequestContext());
        PtabAppealDecisionResponse byAppeal = operation.GetByAppeal("2024-100001", new RequestContext());
        ParseException documentMismatch = Assert.Throws<ParseException>(() => OdpPtabAppealDecisionParser.ParseSingle(CompleteResponse(), "different", CreateMetadata()));
        ParseException appealMismatch = Assert.Throws<ParseException>(() => OdpPtabAppealDecisionParser.ParseByAppeal(CompleteResponse(), "2024-999999", CreateMetadata()));

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/appeals/decisions/AP-170224750"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/appeals/2024-100001/decisions"));
            Assert.That(direct.Document.DocumentIdentifier, Is.EqualTo("AP-170224750"));
            Assert.That(byAppeal.Decisions[0].AppealNumber, Is.EqualTo("2024-100001"));
            Assert.That(documentMismatch.DataPath, Is.EqualTo("$.patentAppealDataBag[0].documentData.documentIdentifier"));
            Assert.That(appealMismatch.DataPath, Is.EqualTo("$.patentAppealDataBag[0].appealNumber"));
        });
    }

    [TestCase(ExportFormat.Csv, UsptoMediaTypes.Csv, "appeals.csv", "csv")]
    [TestCase(ExportFormat.Json, UsptoMediaTypes.Json, "appeals.json", "json")]
    public void ExportUsesAppealDownloadRouteAndRequestedFormat(ExportFormat format, string mediaType, string fileName, string formatText)
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "appeal export", mediaType, fileName);
        using HttpClient client = new HttpClient(handler);
        PtabAppealDecisionExportOperation operation = new PtabAppealDecisionExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = format, Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/appeals/decisions/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(mediaType));
            Assert.That(handler.RequestBody, Does.Contain("\"format\":\"" + formatText + "\""));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
        });
    }

    [Test]
    public void SearchAndByAppealMapRateLimitAndNotFoundErrors()
    {
        RecordingPtabProceedingHandler searchHandler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Too Many Requests\"}" });
        searchHandler.StatusCode = HttpStatusCode.TooManyRequests;
        searchHandler.RetryAfter = "4";
        using HttpClient searchClient = new HttpClient(searchHandler);
        PtabAppealDecisionOperation searchOperation = new PtabAppealDecisionOperation(searchClient, CreateOptions(searchClient));

        RateLimitException rateLimit = Assert.Throws<RateLimitException>(() => searchOperation.SearchGet(new SearchRequest()));

        RecordingPtabProceedingHandler retrievalHandler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Not Found\"}" });
        retrievalHandler.StatusCode = HttpStatusCode.NotFound;
        using HttpClient retrievalClient = new HttpClient(retrievalHandler);
        PtabAppealDecisionOperation retrievalOperation = new PtabAppealDecisionOperation(retrievalClient, CreateOptions(retrievalClient));

        NotFoundException notFound = Assert.Throws<NotFoundException>(() => retrievalOperation.GetByAppeal("2024-999999", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(rateLimit.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(rateLimit.StatusCode, Is.EqualTo(429));
            Assert.That(rateLimit.HasRetryAfterMilliseconds, Is.True);
            Assert.That(rateLimit.RetryAfterMilliseconds, Is.EqualTo(4000L));
            Assert.That(notFound.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
            Assert.That(retrievalHandler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/appeals/2024-999999/decisions"));
        });
    }

    [TestCase("{\"patentAppealDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[]}", "$.patentAppealDataBag")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[7]}", "$.patentAppealDataBag[0]")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"appealMetaData\":[]}]}", "$.patentAppealDataBag[0].appealMetaData")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"appealMetaData\":{\"appealFilingDate\":\"never\"}}]}", "$.patentAppealDataBag[0].appealMetaData.appealFilingDate")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"appellantData\":{\"grantDate\":\"never\"}}]}", "$.patentAppealDataBag[0].appellantData.grantDate")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"thirdPartyRequesterData\":[]}]}", "$.patentAppealDataBag[0].thirdPartyRequesterData")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"examinerData\":[]}]}", "$.patentAppealDataBag[0].examinerData")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"documentData\":{\"documentSizeQuantity\":-1}}]}", "$.patentAppealDataBag[0].documentData.documentSizeQuantity")]
    [TestCase("{\"count\":1,\"patentAppealDataBag\":[{\"decisionData\":{\"issueTypeBag\":[7]}}]}", "$.patentAppealDataBag[0].decisionData.issueTypeBag[0]")]
    [TestCase("{\"count\":0,\"patentAppealDataBag\":[],\"facets\":[{\"status\":[{\"name\":\"Pending\"}]}]}", "$.facets[0].status[0].quantity")]
    public void MalformedAppealResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabAppealDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP PTAB appeal decision"));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    private static SearchRequest CreateSearch()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "appealMetaData.applicationTypeCategory", Operator = SearchOperator.Equal, Value = "Utility" }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "decisionData.decisionIssueDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "documentData.documentIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "appealNumber", "documentData.documentIdentifier", "decisionData" }
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
            Download = new DownloadPolicy { TimeoutMilliseconds = 5000, BufferSize = 8 }
        };
    }

    private static HttpResponseMetadata CreateMetadata()
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/appeals/decisions/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        };
    }

    private static string CompleteResponse()
    {
        return "{\"count\":1,\"totalRecordCount\":3,\"patentAppealDataBag\":[{\"appealNumber\":\"2024-100001\",\"lastModifiedDateTime\":\"2025-07-01T15:20:30Z\",\"appealDocumentCategory\":\"Decision\",\"appealMetaData\":{\"docketNoticeMailedDate\":\"2024-02-01\",\"appealFilingDate\":\"2024-01-15\",\"appealLastModifiedDate\":\"2025-06-30\",\"appealLastModifiedDateTime\":\"2025-06-30T12:30:00Z\",\"applicationTypeCategory\":\"Utility\",\"fileDownloadURI\":\"/download\",\"futureMetadata\":9},\"appellantData\":{\"applicationNumberText\":\"17/123456\",\"counselName\":\"Appeal Counsel\",\"groupArtUnitNumber\":\"2100\",\"inventorName\":\"Ada Inventor\",\"realPartyInInterestName\":\"Appellant LLC\",\"patentNumber\":\"1234567\",\"patentOwnerName\":\"Owner LLC\",\"publicationDate\":\"2023-05-01\",\"publicationNumber\":\"US20240123456\",\"technologyCenterNumber\":\"2100\",\"grantDate\":\"2023-06-01\"},\"thirdPartyRequesterData\":{\"thirdPartyName\":\"Requester Inc.\"},\"examinerData\":{\"examinerName\":\"Alex Examiner\",\"examinerIdentifier\":\"12345\",\"groupArtUnitNumber\":\"2100\",\"technologyCenterNumber\":\"2100\"},\"documentData\":{\"documentFilingDate\":\"2025-01-15\",\"documentIdentifier\":\"AP-170224750\",\"documentName\":\"appeal-decision.pdf\",\"documentSizeQuantity\":654321,\"documentOCRText\":\"appeal decision text\",\"documentTypeDescriptionText\":\"Decision\",\"fileDownloadURI\":\"https://ptab.uspto.gov/doc/AP-170224750\",\"mimeTypeIdentifier\":\"application/pdf\"},\"decisionData\":{\"appealOutcomeCategory\":\"Affirmed\",\"statuteAndRuleBag\":[\"35 USC 103\"],\"decisionIssueDate\":\"2025-01-15\",\"decisionTypeCategory\":\"Decision\",\"issueTypeBag\":[\"Obviousness\"]},\"futureRecord\":true}],\"facets\":[{\"decisionData.appealOutcomeCategory\":[{\"name\":\"Affirmed\",\"quantity\":11}]}],\"requestIdentifier\":\"appeal-request\"}";
    }
}
