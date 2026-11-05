using System.Net;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PtabInterferenceDecisionOperationTests
{
    [Test]
    public void GetSearchUsesInterferenceRouteAndParsesCompleteTypedRecord()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabInterferenceDecisionOperation operation = new PtabInterferenceDecisionOperation(client, CreateOptions(client));
        SearchRequest request = CreateSearch();
        request.Context.RetainRawResponse = true;

        PtabInterferenceDecisionResponse result = operation.SearchGet(request);

        PtabInterferenceDecision record = result.Decisions[0];
        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/decisions/search"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(3L));
            Assert.That(record.InterferenceNumber, Is.EqualTo("103751"));
            Assert.That(record.RecordCategory, Is.EqualTo("Decision"));
            Assert.That(record.HasLastModifiedAt, Is.True);
            Assert.That(record.Metadata.StyleName, Is.EqualTo("TANG v. DOE"));
            Assert.That(record.Metadata.HasLastModifiedDate, Is.True);
            Assert.That(record.Metadata.FileDownloadUri?.ToString(), Is.EqualTo("/download"));
            Assert.That(record.Metadata.UnknownFields["futureMetadata"], Is.EqualTo("9"));
            Assert.That(record.SeniorParty.Category, Is.EqualTo("Senior"));
            Assert.That(record.SeniorParty.ApplicationNumber, Is.EqualTo("07996817"));
            Assert.That(record.SeniorParty.Counsel.Name, Is.EqualTo("Senior Counsel"));
            Assert.That(record.SeniorParty.HasGrantDate, Is.True);
            Assert.That(record.SeniorParty.HasPublicationDate, Is.True);
            Assert.That(record.SeniorParty.UnknownFields["futureParty"], Is.EqualTo("true"));
            Assert.That(record.JuniorParty.Category, Is.EqualTo("Junior"));
            Assert.That(record.JuniorParty.PatentOwnerName, Is.EqualTo("Junior Owner"));
            Assert.That(record.AdditionalParties, Has.Length.EqualTo(1));
            Assert.That(record.AdditionalParties[0].Category, Is.EqualTo("Additional"));
            Assert.That(record.AdditionalParties[0].Name, Is.EqualTo("Additional Corp."));
            Assert.That(record.Document.DocumentIdentifier, Is.EqualTo("INT-170224750"));
            Assert.That(record.Document.Name, Is.EqualTo("jd103751"));
            Assert.That(record.Document.Title, Is.EqualTo("DECISION-103751"));
            Assert.That(record.Document.Size, Is.EqualTo(45224L));
            Assert.That(record.Document.HasSize, Is.True);
            Assert.That(record.Document.HasFilingDate, Is.True);
            Assert.That(record.Document.DownloadUri?.Host, Is.EqualTo("ptab.uspto.gov"));
            Assert.That(record.Document.OcrText, Does.Contain("opinion"));
            Assert.That(record.Document.UnknownFields["futureDocument"], Is.EqualTo("true"));
            Assert.That(record.Decision.HasIssueDate, Is.True);
            Assert.That(record.Decision.TypeCategory, Is.EqualTo("Decision"));
            Assert.That(record.Decision.InterferenceOutcomeCategory, Is.EqualTo("Interference"));
            Assert.That(record.Decision.StatutesAndRules, Is.EqualTo(new[] { "37 CFR 41.203" }));
            Assert.That(record.Decision.IssueTypes, Is.EqualTo(new[] { "Priority" }));
            Assert.That(record.Provenance.Collection, Is.EqualTo(PtabCollection.InterferenceDecisions));
            Assert.That(record.Document.Provenance.Collection, Is.EqualTo(PtabCollection.InterferenceDecisions));
            Assert.That(record.UnknownFields["futureRecord"], Is.EqualTo("7"));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(6L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("interference-request"));
        });
    }

    [Test]
    public void PostSearchWritesCanonicalInterferenceSearchBody()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { "{\"count\":0,\"patentInterferenceDataBag\":[]}" });
        using HttpClient client = new HttpClient(handler);
        PtabInterferenceDecisionOperation operation = new PtabInterferenceDecisionOperation(client, CreateOptions(client));

        PtabInterferenceDecisionResponse result = operation.SearchPost(CreateSearch());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/decisions/search"));
            Assert.That(handler.Bodies[0], Does.Contain("\"filters\":[{\"name\":\"seniorPartyData.patentOwnerName\",\"value\":[\"TANG\"]}]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"fields\":[\"interferenceNumber\",\"decisionDocumentData.documentIdentifier\",\"decisionDocumentData\"]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(result.Decisions, Is.Empty);
        });
    }

    [Test]
    public void AllPageSearchAggregatesUsingStableDocumentIdentifiers()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[]
        {
            "{\"count\":2,\"totalRecordCount\":3,\"patentInterferenceDataBag\":[{\"interferenceNumber\":\"I-1\",\"decisionDocumentData\":{\"documentIdentifier\":\"D-1\"}},{\"interferenceNumber\":\"I-1\",\"decisionDocumentData\":{\"documentIdentifier\":\"D-2\"}}],\"requestIdentifier\":\"first\"}",
            "{\"count\":1,\"totalRecordCount\":3,\"patentInterferenceDataBag\":[{\"interferenceNumber\":\"I-1\",\"decisionDocumentData\":{\"documentIdentifier\":\"D-3\"}}]}"
        });
        using HttpClient client = new HttpClient(handler);
        PtabInterferenceDecisionOperation operation = new PtabInterferenceDecisionOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Sorts = new SearchSort[] { new SearchSort { Field = "decisionDocumentData.documentIdentifier", Direction = SortDirection.Ascending } },
            FieldSelection = new SearchFieldSelection { Fields = new string[] { "interferenceNumber" } },
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 4,
            MaximumResults = 5
        };

        PtabInterferenceDecisionResponse result = operation.SearchAll(request);

        using JsonDocument first = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument second = JsonDocument.Parse(handler.Bodies[1]!);
        Assert.Multiple(() =>
        {
            Assert.That(first.RootElement.GetProperty("fields")[1].GetString(), Is.EqualTo("decisionDocumentData.documentIdentifier"));
            Assert.That(first.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.Zero);
            Assert.That(second.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(2L));
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new[] { "interferenceNumber" }));
            Assert.That(result.Decisions, Has.Length.EqualTo(3));
            Assert.That(result.Decisions[2].Document.DocumentIdentifier, Is.EqualTo("D-3"));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestIdentifier, Is.EqualTo("first"));
        });
    }

    [Test]
    public void DirectAndByInterferenceRetrievalUseDistinctRoutesAndValidateIdentities()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse(), CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabInterferenceDecisionOperation operation = new PtabInterferenceDecisionOperation(client, CreateOptions(client));

        PtabInterferenceDecision direct = operation.GetDocument("INT-170224750", new RequestContext());
        PtabInterferenceDecisionResponse byInterference = operation.GetByInterference("103751", new RequestContext());
        ParseException documentMismatch = Assert.Throws<ParseException>(() => OdpPtabInterferenceDecisionParser.ParseSingle(CompleteResponse(), "different", CreateMetadata()));
        ParseException interferenceMismatch = Assert.Throws<ParseException>(() => OdpPtabInterferenceDecisionParser.ParseByInterference(CompleteResponse(), "999999", CreateMetadata()));

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/decisions/INT-170224750"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/103751/decisions"));
            Assert.That(direct.Document.DocumentIdentifier, Is.EqualTo("INT-170224750"));
            Assert.That(byInterference.Decisions[0].InterferenceNumber, Is.EqualTo("103751"));
            Assert.That(documentMismatch.DataPath, Is.EqualTo("$.patentInterferenceDataBag[0].decisionDocumentData.documentIdentifier"));
            Assert.That(interferenceMismatch.DataPath, Is.EqualTo("$.patentInterferenceDataBag[0].interferenceNumber"));
        });
    }

    [TestCase(ExportFormat.Csv, UsptoMediaTypes.Csv, "interferences.csv", "csv")]
    [TestCase(ExportFormat.Json, UsptoMediaTypes.Json, "interferences.json", "json")]
    public void ExportUsesInterferenceDownloadRouteAndRequestedFormat(ExportFormat format, string mediaType, string fileName, string formatText)
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "interference export", mediaType, fileName);
        using HttpClient client = new HttpClient(handler);
        PtabInterferenceDecisionExportOperation operation = new PtabInterferenceDecisionExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = format, Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/decisions/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(mediaType));
            Assert.That(handler.RequestBody, Does.Contain("\"format\":\"" + formatText + "\""));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
        });
    }

    [Test]
    public void DirectRetrievalAndExportMapAuthenticationAndServiceErrors()
    {
        RecordingPtabProceedingHandler retrievalHandler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Unauthorized\"}" });
        retrievalHandler.StatusCode = HttpStatusCode.Unauthorized;
        using HttpClient retrievalClient = new HttpClient(retrievalHandler);
        PtabInterferenceDecisionOperation retrievalOperation = new PtabInterferenceDecisionOperation(retrievalClient, CreateOptions(retrievalClient));

        AuthenticationException authentication = Assert.Throws<AuthenticationException>(() => retrievalOperation.GetDocument("INT-MISSING", new RequestContext()));

        RecordingPatentExportHandler exportHandler = new RecordingPatentExportHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":\"Unavailable\"}", UsptoMediaTypes.Json, null);
        using HttpClient exportClient = new HttpClient(exportHandler);
        PtabInterferenceDecisionExportOperation exportOperation = new PtabInterferenceDecisionExportOperation(exportClient, CreateOptions(exportClient));
        using MemoryStream output = new MemoryStream();

        ServiceUnavailableException unavailable = Assert.Throws<ServiceUnavailableException>(() => exportOperation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = ExportFormat.Csv, Output = output }));

        Assert.Multiple(() =>
        {
            Assert.That(authentication.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(authentication.StatusCode, Is.EqualTo(401));
            Assert.That(retrievalHandler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/decisions/INT-MISSING"));
            Assert.That(unavailable.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(unavailable.StatusCode, Is.EqualTo(503));
            Assert.That(exportHandler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/interferences/decisions/search/download"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase("{\"patentInterferenceDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[]}", "$.patentInterferenceDataBag")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[7]}", "$.patentInterferenceDataBag[0]")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"interferenceMetaData\":[]}]}", "$.patentInterferenceDataBag[0].interferenceMetaData")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"interferenceMetaData\":{\"interferenceLastModifiedDate\":\"never\"}}]}", "$.patentInterferenceDataBag[0].interferenceMetaData.interferenceLastModifiedDate")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"seniorPartyData\":{\"grantDate\":\"never\"}}]}", "$.patentInterferenceDataBag[0].seniorPartyData.grantDate")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"additionalPartyDataBag\":[7]}]}", "$.patentInterferenceDataBag[0].additionalPartyDataBag[0]")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"decisionDocumentData\":[]}]}", "$.patentInterferenceDataBag[0].decisionDocumentData")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"decisionDocumentData\":{\"documentSizeQuantity\":-1}}]}", "$.patentInterferenceDataBag[0].decisionDocumentData.documentSizeQuantity")]
    [TestCase("{\"count\":1,\"patentInterferenceDataBag\":[{\"decisionDocumentData\":{\"issueTypeBag\":[7]}}]}", "$.patentInterferenceDataBag[0].decisionDocumentData.issueTypeBag[0]")]
    [TestCase("{\"count\":0,\"patentInterferenceDataBag\":[],\"facets\":[{\"status\":[{\"name\":\"Pending\"}]}]}", "$.facets[0].status[0].quantity")]
    public void MalformedInterferenceResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabInterferenceDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP PTAB interference decision"));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    private static SearchRequest CreateSearch()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "seniorPartyData.patentOwnerName", Operator = SearchOperator.Equal, Value = "TANG" }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "decisionDocumentData.decisionIssueDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "decisionDocumentData.documentIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "interferenceNumber", "decisionDocumentData.documentIdentifier", "decisionDocumentData" }
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
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/interferences/decisions/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        };
    }

    private static string CompleteResponse()
    {
        return "{\"count\":1,\"totalRecordCount\":3,\"patentInterferenceDataBag\":[{\"interferenceNumber\":\"103751\",\"interferenceDocumentCategory\":\"Decision\",\"lastModifiedDateTime\":\"2025-07-28T21:03:54Z\",\"interferenceMetaData\":{\"interferenceStyleName\":\"TANG v. DOE\",\"interferenceLastModifiedDate\":\"2025-05-05\",\"fileDownloadURI\":\"/download\",\"futureMetadata\":9},\"seniorPartyData\":{\"applicationNumberText\":\"07996817\",\"counselName\":\"Senior Counsel\",\"grantDate\":\"2003-09-02\",\"groupArtUnitNumber\":\"1763\",\"inventorName\":\"TANG\",\"realPartyInInterestName\":\"TANG\",\"patentNumber\":\"6614529\",\"patentOwnerName\":\"Senior Owner\",\"technologyCenterNumber\":\"1700\",\"publicationDate\":\"2002-01-15\",\"publicationNumber\":\"US2002XXXXXXA1\",\"futureParty\":true},\"juniorPartyData\":{\"applicationNumberText\":\"08111111\",\"patentOwnerName\":\"Junior Owner\"},\"additionalPartyDataBag\":[{\"applicationNumberText\":\"07999999\",\"inventorName\":\"DOE, JOHN\",\"additionalPartyName\":\"Additional Corp.\",\"patentNumber\":\"6999999\"}],\"decisionDocumentData\":{\"documentFilingDate\":\"2002-02-01\",\"documentIdentifier\":\"INT-170224750\",\"documentName\":\"jd103751\",\"documentSizeQuantity\":45224,\"documentOCRText\":\"The opinion in support of the decision\",\"documentTitleText\":\"DECISION-103751\",\"fileDownloadURI\":\"https://ptab.uspto.gov/doc/INT-170224750\",\"statuteAndRuleBag\":[\"37 CFR 41.203\"],\"decisionIssueDate\":\"2002-02-25\",\"decisionTypeCategory\":\"Decision\",\"issueTypeBag\":[\"Priority\"],\"interferenceOutcomeCategory\":\"Interference\",\"futureDocument\":true},\"futureRecord\":7}],\"facets\":[{\"seniorPartyData.technologyCenterNumber\":[{\"name\":\"1700\",\"quantity\":6}]}],\"requestIdentifier\":\"interference-request\"}";
    }
}
