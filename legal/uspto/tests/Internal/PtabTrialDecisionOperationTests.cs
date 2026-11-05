using System.Net;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PtabTrialDecisionOperationTests
{
    [Test]
    public void GetSearchUsesDecisionRouteAndParsesCompleteTypedRecord()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDecisionOperation operation = new PtabTrialDecisionOperation(client, CreateOptions(client));
        SearchRequest request = CreateSearch();
        request.Context.RetainRawResponse = true;

        PtabTrialDecisionResponse result = operation.SearchGet(request);

        PtabTrialDecision record = result.Decisions[0];
        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/decisions/search"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("fields=trialNumber%2CdocumentData.documentIdentifier%2CdecisionData"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("filters=trialMetaData.trialTypeCode%20IPR"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("sort=decisionData.decisionIssueDate%20desc%2CdocumentData.documentIdentifier%20asc"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(3L));
            Assert.That(record.RecordCategory, Is.EqualTo("decision"));
            Assert.That(record.TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(record.TrialTypeCode, Is.EqualTo("IPR"));
            Assert.That(record.HasLastModifiedAt, Is.True);
            Assert.That(record.Metadata.StatusCategory, Is.EqualTo("Terminated"));
            Assert.That(record.PatentOwner.Name, Is.EqualTo("Patent Owner LLC"));
            Assert.That(record.RegularPetitioner.Counsel.Name, Is.EqualTo("Petitioner Counsel"));
            Assert.That(record.Document.DocumentIdentifier, Is.EqualTo("170224750"));
            Assert.That(record.Document.DocumentNumber, Is.EqualTo(12));
            Assert.That(record.Document.HasDocumentNumber, Is.True);
            Assert.That(record.Document.Size, Is.EqualTo(123456L));
            Assert.That(record.Document.HasSize, Is.True);
            Assert.That(record.Document.HasFilingDate, Is.True);
            Assert.That(record.Document.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(record.Document.MediaType, Is.EqualTo(UsptoMediaTypes.Pdf));
            Assert.That(record.Document.DownloadUri?.Host, Is.EqualTo("ptab.uspto.gov"));
            Assert.That(record.Document.OcrText, Does.Contain("written decision"));
            Assert.That(record.Document.UnknownFields["futureDocument"], Is.EqualTo("true"));
            Assert.That(record.Decision.HasIssueDate, Is.True);
            Assert.That(record.Decision.TypeCategory, Is.EqualTo("Final Written Decision"));
            Assert.That(record.Decision.TrialOutcomeCategory, Is.EqualTo("Denied"));
            Assert.That(record.Decision.StatutesAndRules, Is.EqualTo(new[] { "37 CFR 42.100", "35 USC 103" }));
            Assert.That(record.Decision.IssueTypes, Is.EqualTo(new[] { "103", "112" }));
            Assert.That(record.Decision.UnknownFields["futureDecision"], Is.EqualTo("{\"value\":1}"));
            Assert.That(record.Provenance.Collection, Is.EqualTo(PtabCollection.TrialDecisions));
            Assert.That(record.Document.Provenance.Collection, Is.EqualTo(PtabCollection.TrialDecisions));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(41L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("decision-request"));
        });
    }

    [Test]
    public void PostSearchWritesCanonicalDecisionSearchBody()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { "{\"count\":0,\"patentTrialDecisionDataBag\":[]}" });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDecisionOperation operation = new PtabTrialDecisionOperation(client, CreateOptions(client));

        PtabTrialDecisionResponse result = operation.SearchPost(CreateSearch());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/decisions/search"));
            Assert.That(handler.Bodies[0], Does.Contain("\"filters\":[{\"name\":\"trialMetaData.trialTypeCode\",\"value\":[\"IPR\"]}]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"fields\":[\"trialNumber\",\"documentData.documentIdentifier\",\"decisionData\"]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(result.Decisions, Is.Empty);
        });
    }

    [Test]
    public void AllPageSearchAggregatesUsingStableDocumentIdentifiers()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[]
        {
            "{\"count\":2,\"totalRecordCount\":3,\"patentTrialDecisionDataBag\":[{\"trialNumber\":\"IPR-1\",\"documentData\":{\"documentIdentifier\":\"D-1\"}},{\"trialNumber\":\"IPR-1\",\"documentData\":{\"documentIdentifier\":\"D-2\"}}],\"requestIdentifier\":\"first\"}",
            "{\"count\":1,\"totalRecordCount\":3,\"patentTrialDecisionDataBag\":[{\"trialNumber\":\"IPR-1\",\"documentData\":{\"documentIdentifier\":\"D-3\"}}]}"
        });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDecisionOperation operation = new PtabTrialDecisionOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Sorts = new SearchSort[] { new SearchSort { Field = "documentData.documentIdentifier", Direction = SortDirection.Ascending } },
            FieldSelection = new SearchFieldSelection { Fields = new string[] { "trialNumber" } },
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 4,
            MaximumResults = 5
        };

        PtabTrialDecisionResponse result = operation.SearchAll(request);

        using JsonDocument first = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument second = JsonDocument.Parse(handler.Bodies[1]!);
        Assert.Multiple(() =>
        {
            Assert.That(first.RootElement.GetProperty("fields")[1].GetString(), Is.EqualTo("documentData.documentIdentifier"));
            Assert.That(first.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.Zero);
            Assert.That(second.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(2L));
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new[] { "trialNumber" }));
            Assert.That(result.Decisions, Has.Length.EqualTo(3));
            Assert.That(result.Decisions[2].Document.DocumentIdentifier, Is.EqualTo("D-3"));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestIdentifier, Is.EqualTo("first"));
        });
    }

    [Test]
    public void DirectAndByTrialRetrievalUseDistinctRoutesAndValidateIdentities()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse(), CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDecisionOperation operation = new PtabTrialDecisionOperation(client, CreateOptions(client));

        PtabTrialDecision direct = operation.GetDocument("170224750", new RequestContext());
        PtabTrialDecisionResponse byTrial = operation.GetByTrial("IPR2024-00001", new RequestContext());
        ParseException documentMismatch = Assert.Throws<ParseException>(() => OdpPtabTrialDecisionParser.ParseSingle(CompleteResponse(), "different", CreateMetadata()));
        ParseException trialMismatch = Assert.Throws<ParseException>(() => OdpPtabTrialDecisionParser.ParseByTrial(CompleteResponse(), "IPR2024-99999", CreateMetadata()));

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/decisions/170224750"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/IPR2024-00001/decisions"));
            Assert.That(direct.Document.DocumentIdentifier, Is.EqualTo("170224750"));
            Assert.That(byTrial.Decisions[0].TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(documentMismatch.DataPath, Is.EqualTo("$.patentTrialDecisionDataBag[0].documentData.documentIdentifier"));
            Assert.That(trialMismatch.DataPath, Is.EqualTo("$.patentTrialDecisionDataBag[0].trialNumber"));
        });
    }

    [TestCase(ExportFormat.Csv, UsptoMediaTypes.Csv, "decisions.csv", "csv")]
    [TestCase(ExportFormat.Json, UsptoMediaTypes.Json, "decisions.json", "json")]
    public void ExportUsesDecisionDownloadRouteAndRequestedFormat(ExportFormat format, string mediaType, string fileName, string formatText)
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "decision export", mediaType, fileName);
        using HttpClient client = new HttpClient(handler);
        PtabTrialDecisionExportOperation operation = new PtabTrialDecisionExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = format, Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/decisions/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(mediaType));
            Assert.That(handler.RequestBody, Does.Contain("\"format\":\"" + formatText + "\""));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
        });
    }

    [Test]
    public void SearchMapsRateLimitAndRetrievalValidatesBeforeSending()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Too Many Requests\"}" });
        handler.StatusCode = HttpStatusCode.TooManyRequests;
        handler.RetryAfter = "3";
        using HttpClient client = new HttpClient(handler);
        PtabTrialDecisionOperation operation = new PtabTrialDecisionOperation(client, CreateOptions(client));

        RateLimitException rateLimit = Assert.Throws<RateLimitException>(() => operation.SearchGet(new SearchRequest()));
        ValidationException blank = Assert.Throws<ValidationException>(() => operation.GetDocument(" ", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(rateLimit.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(rateLimit.StatusCode, Is.EqualTo(429));
            Assert.That(rateLimit.HasRetryAfterMilliseconds, Is.True);
            Assert.That(rateLimit.RetryAfterMilliseconds, Is.EqualTo(3000L));
            Assert.That(blank.FieldName, Is.EqualTo("documentIdentifier"));
            Assert.That(blank.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(handler.Methods, Has.Count.EqualTo(1));
        });
    }

    [TestCase("{\"patentTrialDecisionDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[]}", "$.patentTrialDecisionDataBag")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[7]}", "$.patentTrialDecisionDataBag[0]")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[{\"documentData\":[]}]}", "$.patentTrialDecisionDataBag[0].documentData")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[{\"documentData\":{\"documentNumber\":-1}}]}", "$.patentTrialDecisionDataBag[0].documentData.documentNumber")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[{\"documentData\":{\"fileDownloadURI\":\"relative.pdf\"}}]}", "$.patentTrialDecisionDataBag[0].documentData.fileDownloadURI")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[{\"decisionData\":[]}]}", "$.patentTrialDecisionDataBag[0].decisionData")]
    [TestCase("{\"count\":1,\"patentTrialDecisionDataBag\":[{\"decisionData\":{\"issueTypeBag\":[7]}}]}", "$.patentTrialDecisionDataBag[0].decisionData.issueTypeBag[0]")]
    [TestCase("{\"count\":0,\"patentTrialDecisionDataBag\":[],\"facets\":[{\"status\":[{\"name\":\"Pending\"}]}]}", "$.facets[0].status[0].quantity")]
    public void MalformedDecisionResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabTrialDecisionParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP PTAB trial decision"));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    private static SearchRequest CreateSearch()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "trialMetaData.trialTypeCode", Operator = SearchOperator.Equal, Value = "IPR" }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "decisionData.decisionIssueDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "documentData.documentIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "trialNumber", "documentData.documentIdentifier", "decisionData" }
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
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/trials/decisions/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        };
    }

    private static string CompleteResponse()
    {
        return "{\"count\":1,\"totalRecordCount\":3,\"patentTrialDecisionDataBag\":[{\"trialDocumentCategory\":\"decision\",\"lastModifiedDateTime\":\"2025-07-01T15:20:30Z\",\"trialNumber\":\"IPR2024-00001\",\"trialTypeCode\":\"IPR\",\"trialMetaData\":{\"trialStatusCategory\":\"Terminated\",\"trialTypeCode\":\"IPR\"},\"patentOwnerData\":{\"patentOwnerName\":\"Patent Owner LLC\",\"counselName\":\"Owner Counsel\"},\"regularPetitionerData\":{\"realPartyInInterestName\":\"Petitioner Inc.\",\"counselName\":\"Petitioner Counsel\"},\"respondentData\":{\"counselName\":\"Respondent Counsel\"},\"derivationPetitionerData\":null,\"documentData\":{\"documentCategory\":\"Decision\",\"documentFilingDate\":\"2025-01-15\",\"documentIdentifier\":\"170224750\",\"documentName\":\"final-decision.pdf\",\"documentNumber\":12,\"documentSizeQuantity\":123456,\"documentOCRText\":\"final written decision text\",\"documentTitleText\":\"Final Written Decision\",\"documentTypeDescriptionText\":\"Decision\",\"fileDownloadURI\":\"https://ptab.uspto.gov/doc/170224750\",\"filingPartyCategory\":\"Board\",\"mimeTypeIdentifier\":\"application/pdf\",\"futureDocument\":true},\"decisionData\":{\"statuteAndRuleBag\":[\"37 CFR 42.100\",\"35 USC 103\"],\"decisionIssueDate\":\"2025-01-15\",\"decisionTypeCategory\":\"Final Written Decision\",\"issueTypeBag\":[\"103\",\"112\"],\"trialOutcomeCategory\":\"Denied\",\"futureDecision\":{\"value\":1}},\"futureRecord\":9}],\"facets\":[{\"trialMetaData.trialStatusCategory\":[{\"name\":\"Terminated\",\"quantity\":41}]}],\"requestIdentifier\":\"decision-request\"}";
    }
}
