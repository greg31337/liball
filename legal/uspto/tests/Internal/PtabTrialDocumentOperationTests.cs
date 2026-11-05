using System.Net;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PtabTrialDocumentOperationTests
{
    [Test]
    public void GetSearchUsesDocumentRouteAndParsesCompleteTypedRecord()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDocumentOperation operation = new PtabTrialDocumentOperation(client, CreateOptions(client));
        SearchRequest request = CreateSearch();
        request.Context.RetainRawResponse = true;

        PtabTrialDocumentResponse result = operation.SearchGet(request);

        PtabTrialDocument record = result.Documents[0];
        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/documents/search"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("fields=trialNumber%2CdocumentData.documentIdentifier%2CdocumentData"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("filters=documentData.documentCategory%20Petition"));
            Assert.That(handler.RequestUris[0].Query, Does.Contain("sort=documentData.documentFilingDate%20desc%2CdocumentData.documentIdentifier%20asc"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Total, Is.EqualTo(4L));
            Assert.That(record.RecordCategory, Is.EqualTo("document"));
            Assert.That(record.TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(record.TrialTypeCode, Is.EqualTo("IPR"));
            Assert.That(record.HasLastModifiedAt, Is.True);
            Assert.That(record.Metadata.StatusCategory, Is.EqualTo("Pending"));
            Assert.That(record.PatentOwner.Counsel.Name, Is.EqualTo("Owner Counsel"));
            Assert.That(record.RegularPetitioner.RealPartyInInterestName, Is.EqualTo("Petitioner Inc."));
            Assert.That(record.Document.DocumentIdentifier, Is.EqualTo("170224751"));
            Assert.That(record.Document.DocumentNumber, Is.EqualTo(7));
            Assert.That(record.Document.HasDocumentNumber, Is.True);
            Assert.That(record.Document.Size, Is.EqualTo(45678L));
            Assert.That(record.Document.HasSize, Is.True);
            Assert.That(record.Document.HasFilingDate, Is.True);
            Assert.That(record.Document.Category, Is.EqualTo("Petition"));
            Assert.That(record.Document.FilingPartyCategory, Is.EqualTo("Petitioner"));
            Assert.That(record.Document.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(record.Document.DownloadUri?.Host, Is.EqualTo("ptab.uspto.gov"));
            Assert.That(record.Document.OcrText, Is.EqualTo("petition OCR text"));
            Assert.That(record.Document.UnknownFields["futureDocument"], Is.EqualTo("[1,2]"));
            Assert.That(record.Decision.TypeCategory, Is.EqualTo("Institution Decision"));
            Assert.That(record.Provenance.Collection, Is.EqualTo(PtabCollection.TrialDocuments));
            Assert.That(record.Document.Provenance.Collection, Is.EqualTo(PtabCollection.TrialDocuments));
            Assert.That(result.Facets[0].Values[0].Name, Is.EqualTo("Petition"));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(33L));
            Assert.That(result.RequestIdentifier, Is.EqualTo("document-request"));
        });
    }

    [Test]
    public void PostSearchWritesCanonicalDocumentSearchBody()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { "{\"count\":0,\"patentTrialDocumentDataBag\":[]}" });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDocumentOperation operation = new PtabTrialDocumentOperation(client, CreateOptions(client));

        PtabTrialDocumentResponse result = operation.SearchPost(CreateSearch());

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods[0], Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/documents/search"));
            Assert.That(handler.Bodies[0], Does.Contain("\"filters\":[{\"name\":\"documentData.documentCategory\",\"value\":[\"Petition\"]}]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"fields\":[\"trialNumber\",\"documentData.documentIdentifier\",\"documentData\"]"));
            Assert.That(handler.Bodies[0], Does.Contain("\"pagination\":{\"offset\":25,\"limit\":10}"));
            Assert.That(result.Documents, Is.Empty);
        });
    }

    [Test]
    public void AllPageSearchAggregatesUsingStableDocumentIdentifiers()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[]
        {
            "{\"count\":2,\"totalRecordCount\":3,\"patentTrialDocumentDataBag\":[{\"trialNumber\":\"IPR-1\",\"documentData\":{\"documentIdentifier\":\"D-1\"}},{\"trialNumber\":\"IPR-1\",\"documentData\":{\"documentIdentifier\":\"D-2\"}}],\"requestIdentifier\":\"first\"}",
            "{\"count\":1,\"totalRecordCount\":3,\"patentTrialDocumentDataBag\":[{\"trialNumber\":\"IPR-1\",\"documentData\":{\"documentIdentifier\":\"D-3\"}}]}"
        });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDocumentOperation operation = new PtabTrialDocumentOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Sorts = new SearchSort[] { new SearchSort { Field = "documentData.documentIdentifier", Direction = SortDirection.Ascending } },
            FieldSelection = new SearchFieldSelection { Fields = new string[] { "trialNumber" } },
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 4,
            MaximumResults = 5
        };

        PtabTrialDocumentResponse result = operation.SearchAll(request);

        using JsonDocument first = JsonDocument.Parse(handler.Bodies[0]!);
        using JsonDocument second = JsonDocument.Parse(handler.Bodies[1]!);
        Assert.Multiple(() =>
        {
            Assert.That(first.RootElement.GetProperty("fields")[1].GetString(), Is.EqualTo("documentData.documentIdentifier"));
            Assert.That(first.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.Zero);
            Assert.That(second.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(2L));
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new[] { "trialNumber" }));
            Assert.That(result.Documents, Has.Length.EqualTo(3));
            Assert.That(result.Documents[2].Document.DocumentIdentifier, Is.EqualTo("D-3"));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestIdentifier, Is.EqualTo("first"));
        });
    }

    [Test]
    public void DirectAndByTrialRetrievalUseDistinctRoutesAndValidateIdentities()
    {
        RecordingPtabProceedingHandler handler = new RecordingPtabProceedingHandler(new string[] { CompleteResponse(), CompleteResponse() });
        using HttpClient client = new HttpClient(handler);
        PtabTrialDocumentOperation operation = new PtabTrialDocumentOperation(client, CreateOptions(client));

        PtabTrialDocument direct = operation.GetDocument("170224751", new RequestContext());
        PtabTrialDocumentResponse byTrial = operation.GetByTrial("IPR2024-00001", new RequestContext());
        ParseException documentMismatch = Assert.Throws<ParseException>(() => OdpPtabTrialDocumentParser.ParseSingle(CompleteResponse(), "different", CreateMetadata()));
        ParseException trialMismatch = Assert.Throws<ParseException>(() => OdpPtabTrialDocumentParser.ParseByTrial(CompleteResponse(), "IPR2024-99999", CreateMetadata()));

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/documents/170224751"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/IPR2024-00001/documents"));
            Assert.That(direct.Document.DocumentIdentifier, Is.EqualTo("170224751"));
            Assert.That(byTrial.Documents[0].TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(documentMismatch.DataPath, Is.EqualTo("$.patentTrialDocumentDataBag[0].documentData.documentIdentifier"));
            Assert.That(trialMismatch.DataPath, Is.EqualTo("$.patentTrialDocumentDataBag[0].trialNumber"));
        });
    }

    [TestCase(ExportFormat.Csv, UsptoMediaTypes.Csv, "documents.csv", "csv")]
    [TestCase(ExportFormat.Json, UsptoMediaTypes.Json, "documents.json", "json")]
    public void ExportUsesDocumentDownloadRouteAndRequestedFormat(ExportFormat format, string mediaType, string fileName, string formatText)
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "document export", mediaType, fileName);
        using HttpClient client = new HttpClient(handler);
        PtabTrialDocumentExportOperation operation = new PtabTrialDocumentExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = format, Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/documents/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(mediaType));
            Assert.That(handler.RequestBody, Does.Contain("\"format\":\"" + formatText + "\""));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
        });
    }

    [Test]
    public void RetrievalAndExportMapTypedRemoteErrors()
    {
        RecordingPtabProceedingHandler retrievalHandler = new RecordingPtabProceedingHandler(new string[] { "{\"error\":\"Not Found\"}" });
        retrievalHandler.StatusCode = HttpStatusCode.NotFound;
        using HttpClient retrievalClient = new HttpClient(retrievalHandler);
        PtabTrialDocumentOperation retrievalOperation = new PtabTrialDocumentOperation(retrievalClient, CreateOptions(retrievalClient));

        NotFoundException notFound = Assert.Throws<NotFoundException>(() => retrievalOperation.GetByTrial("IPR2024-99999", new RequestContext()));

        RecordingPatentExportHandler exportHandler = new RecordingPatentExportHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":\"Unavailable\"}", UsptoMediaTypes.Json, null);
        using HttpClient exportClient = new HttpClient(exportHandler);
        PtabTrialDocumentExportOperation exportOperation = new PtabTrialDocumentExportOperation(exportClient, CreateOptions(exportClient));
        using MemoryStream output = new MemoryStream();

        ServiceUnavailableException unavailable = Assert.Throws<ServiceUnavailableException>(() => exportOperation.Download(new SearchRequest(), new DownloadRequest { ExportFormat = ExportFormat.Json, Output = output }));

        Assert.Multiple(() =>
        {
            Assert.That(notFound.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
            Assert.That(retrievalHandler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/IPR2024-99999/documents"));
            Assert.That(unavailable.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(unavailable.StatusCode, Is.EqualTo(503));
            Assert.That(exportHandler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/trials/documents/search/download"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase("{\"patentTrialDocumentDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[]}", "$.patentTrialDocumentDataBag")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[7]}", "$.patentTrialDocumentDataBag[0]")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[{\"documentData\":[]}]}", "$.patentTrialDocumentDataBag[0].documentData")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[{\"documentData\":{\"documentSizeQuantity\":-1}}]}", "$.patentTrialDocumentDataBag[0].documentData.documentSizeQuantity")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[{\"documentData\":{\"fileDownloadURI\":\"relative.pdf\"}}]}", "$.patentTrialDocumentDataBag[0].documentData.fileDownloadURI")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[{\"decisionData\":[]}]}", "$.patentTrialDocumentDataBag[0].decisionData")]
    [TestCase("{\"count\":1,\"patentTrialDocumentDataBag\":[{\"decisionData\":{\"statuteAndRuleBag\":[7]}}]}", "$.patentTrialDocumentDataBag[0].decisionData.statuteAndRuleBag[0]")]
    [TestCase("{\"count\":0,\"patentTrialDocumentDataBag\":[],\"facets\":[{\"category\":[{\"name\":\"Petition\"}]}]}", "$.facets[0].category[0].quantity")]
    public void MalformedDocumentResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpPtabTrialDocumentParser.ParseSearch(json, new SearchRequest(), CreateMetadata(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP PTAB trial document"));
            Assert.That(exception.DataPath, Is.EqualTo(path));
        });
    }

    private static SearchRequest CreateSearch()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "documentData.documentCategory", Operator = SearchOperator.Equal, Value = "Petition" }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "documentData.documentFilingDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "documentData.documentIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "trialNumber", "documentData.documentIdentifier", "documentData" }
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
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/trials/documents/search"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        };
    }

    private static string CompleteResponse()
    {
        return "{\"count\":1,\"totalRecordCount\":4,\"patentTrialDocumentDataBag\":[{\"trialDocumentCategory\":\"document\",\"lastModifiedDateTime\":\"2025-07-01T15:20:30Z\",\"trialNumber\":\"IPR2024-00001\",\"trialTypeCode\":\"IPR\",\"trialMetaData\":{\"trialStatusCategory\":\"Pending\",\"trialTypeCode\":\"IPR\"},\"patentOwnerData\":{\"patentOwnerName\":\"Patent Owner LLC\",\"counselName\":\"Owner Counsel\"},\"regularPetitionerData\":{\"realPartyInInterestName\":\"Petitioner Inc.\",\"counselName\":\"Petitioner Counsel\"},\"respondentData\":null,\"derivationPetitionerData\":null,\"documentData\":{\"documentCategory\":\"Petition\",\"documentFilingDate\":\"2024-04-12\",\"documentIdentifier\":\"170224751\",\"documentName\":\"petition.pdf\",\"documentNumber\":7,\"documentSizeQuantity\":45678,\"documentOCRText\":\"petition OCR text\",\"documentTitleText\":\"Petition\",\"documentTypeDescriptionText\":\"Petition Type\",\"fileDownloadURI\":\"https://ptab.uspto.gov/doc/170224751\",\"filingPartyCategory\":\"Petitioner\",\"mimeTypeIdentifier\":\"application/pdf\",\"futureDocument\":[1,2]},\"decisionData\":{\"decisionIssueDate\":\"2024-08-20\",\"decisionTypeCategory\":\"Institution Decision\",\"trialOutcomeCategory\":\"Instituted\"},\"futureRecord\":9}],\"facets\":[{\"documentData.documentCategory\":[{\"name\":\"Petition\",\"quantity\":33}]}],\"requestIdentifier\":\"document-request\"}";
    }
}
