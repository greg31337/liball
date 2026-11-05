using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PetitionDecisionTransferOperationTests
{
    [Test]
    public void AllPageSearchAggregatesPetitionsAndAddsTheStableSelectedField()
    {
        PetitionPageHandler handler = new PetitionPageHandler(new string[]
        {
            "{\"count\":2,\"totalRecordCount\":3,\"petitionDecisionDataBag\":[{\"petitionDecisionRecordIdentifier\":\"decision-1\"},{\"petitionDecisionRecordIdentifier\":\"decision-2\"}],\"facets\":[{\"decisionTypeCodeDescriptionText\":[{\"value\":\"DENIED\",\"count\":2}]}],\"requestIdentifier\":\"first-request\"}",
            "{\"count\":1,\"totalRecordCount\":3,\"petitionDecisionDataBag\":[{\"petitionDecisionRecordIdentifier\":\"decision-3\"}]}"
        });
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionOperation operation = new PetitionDecisionOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "petitionDecisionRecordIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "decisionDate" }
            },
            OffsetPage = new OffsetPage { Offset = 0L, Limit = 2 },
            MaximumPages = 5,
            MaximumResults = 5
        };

        PetitionDecisionResponse result = operation.SearchAll(request);

        using JsonDocument firstBody = JsonDocument.Parse(handler.RequestBodies[0]);
        using JsonDocument secondBody = JsonDocument.Parse(handler.RequestBodies[1]);
        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestBodies, Has.Count.EqualTo(2));
            Assert.That(firstBody.RootElement.GetProperty("fields")[0].GetString(), Is.EqualTo("decisionDate"));
            Assert.That(firstBody.RootElement.GetProperty("fields")[1].GetString(), Is.EqualTo("petitionDecisionRecordIdentifier"));
            Assert.That(firstBody.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.Zero);
            Assert.That(secondBody.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(2L));
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new string[] { "decisionDate" }));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Decisions, Has.Length.EqualTo(3));
            Assert.That(result.Decisions[2].RecordIdentifier, Is.EqualTo("decision-3"));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Total, Is.EqualTo(3L));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Facets[0].Values[0].Value, Is.EqualTo("DENIED"));
            Assert.That(result.RequestIdentifier, Is.EqualTo("first-request"));
        });
    }

    [Test]
    public void CsvExportUsesThePetitionRouteAndExportRequestFormat()
    {
        const string csv = "petitionDecisionRecordIdentifier,decisionDate\r\ndecision-1,2024-01-01\r\n";
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, csv, UsptoMediaTypes.Csv, "petition-results.csv");
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionExportOperation operation = new PetitionDecisionExportOperation(client, CreateOptions(client));
        PetitionDecisionExportRequest export = new PetitionDecisionExportRequest
        {
            Format = ExportFormat.Csv,
            Search = new SearchRequest { Query = "decisionTypeCodeDescriptionText:DENIED" }
        };
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest
        {
            ExportFormat = ExportFormat.Json,
            Output = output
        };

        DownloadResult result = operation.Download(export, download);

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/petition/decisions/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Csv));
            Assert.That(body.RootElement.GetProperty("format").GetString(), Is.EqualTo("csv"));
            Assert.That(body.RootElement.GetProperty("q").GetString(), Is.EqualTo("decisionTypeCodeDescriptionText:DENIED"));
            Assert.That(Encoding.UTF8.GetString(output.ToArray()), Is.EqualTo(csv));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(result.Content.FileName, Is.EqualTo("petition-results.csv"));
        });
    }

    [Test]
    public void JsonExportUsesThePetitionRouteAndDefaultFilename()
    {
        const string json = "{\"petitionDecisionDataBag\":[{\"petitionDecisionRecordIdentifier\":\"decision-1\"}]}";
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, json, UsptoMediaTypes.Json, null);
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionExportOperation operation = new PetitionDecisionExportOperation(client, CreateOptions(client));
        PetitionDecisionExportRequest export = new PetitionDecisionExportRequest
        {
            Format = ExportFormat.Json,
            Search = new SearchRequest
            {
                Filters = new SearchFilter[]
                {
                    new SearchFilter { Field = "decisionTypeCodeDescriptionText", Operator = SearchOperator.Equal, Value = "GRANTED" }
                }
            }
        };
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(export, new DownloadRequest { Output = output });

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/root/api/v1/petition/decisions/search/download"));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(body.RootElement.GetProperty("format").GetString(), Is.EqualTo("json"));
            Assert.That(body.RootElement.GetProperty("filters")[0].GetProperty("name").GetString(), Is.EqualTo("decisionTypeCodeDescriptionText"));
            Assert.That(output.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes(json)));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.Content.FileName, Is.EqualTo("petition-decision-search.json"));
        });
    }

    [Test]
    public void RetrievalExportAndDocumentFailuresRemainTypedAndDoNotWriteOutput()
    {
        RecordingPetitionHandler retrievalHandler = new RecordingPetitionHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-petition\"}");
        using HttpClient retrievalClient = new HttpClient(retrievalHandler);
        PetitionDecisionOperation decisions = new PetitionDecisionOperation(retrievalClient, CreateOptions(retrievalClient));
        NotFoundException missing = Assert.Throws<NotFoundException>(() => decisions.Get("missing-record", true, new RequestContext()));

        RecordingPatentExportHandler exportHandler = new RecordingPatentExportHandler(
            HttpStatusCode.ServiceUnavailable,
            "{\"message\":\"unavailable\"}",
            UsptoMediaTypes.Json,
            null);
        using HttpClient exportClient = new HttpClient(exportHandler);
        PetitionDecisionExportOperation exports = new PetitionDecisionExportOperation(exportClient, CreateOptions(exportClient));
        using MemoryStream exportOutput = new MemoryStream();
        ServiceUnavailableException unavailable = Assert.Throws<ServiceUnavailableException>(() => exports.Download(
            new PetitionDecisionExportRequest { Format = ExportFormat.Json },
            new DownloadRequest { Output = exportOutput }));

        RecordingPatentDocumentDownloadHandler documentHandler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.NotFound);
            response.Content = new StringContent("{\"message\":\"missing document\"}", Encoding.UTF8, UsptoMediaTypes.Json);
            return response;
        });
        using HttpClient documentClient = new HttpClient(documentHandler);
        PetitionDocumentDownloadOperation documents = new PetitionDocumentDownloadOperation(documentClient, CreateOptions(documentClient));
        documents.DownloadOperation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        using MemoryStream documentOutput = new MemoryStream();
        DownloadResult documentFailure = documents.Download("missing-document", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/petitions/missing.pdf"),
            Output = documentOutput
        });

        Assert.Multiple(() =>
        {
            Assert.That(missing.ResourceIdentifier, Is.EqualTo("petition decision retrieval"));
            Assert.That(missing.RequestIdentifier, Is.EqualTo("missing-petition"));
            Assert.That(unavailable.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(unavailable.StatusCode, Is.EqualTo(503));
            Assert.That(unavailable.HasStatusCode, Is.True);
            Assert.That(exportOutput.Length, Is.Zero);
            Assert.That(documentFailure.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(documentFailure.FailureCode, Is.EqualTo("HTTP_STATUS_404"));
            Assert.That(documentOutput.Length, Is.Zero);
        });
    }

    [Test]
    public void ExportRejectsAnUnsupportedPetitionFormatBeforeSending()
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "{}", UsptoMediaTypes.Json, "petition.json");
        using HttpClient client = new HttpClient(handler);
        PetitionDecisionExportOperation operation = new PetitionDecisionExportOperation(client, CreateOptions(client));
        PetitionDecisionExportRequest export = new PetitionDecisionExportRequest { Format = (ExportFormat)99 };
        using MemoryStream output = new MemoryStream();

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.Download(export, new DownloadRequest { Output = output }));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("UNSUPPORTED_FORMAT"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    [Test]
    public void PetitionDocumentDownloadUsesSecureStreamingAndStripsCredentialsFromNonOfficialHosts()
    {
        byte[] document = Encoding.UTF8.GetBytes("petition-pdf");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => CreateDocumentResponse(document, "petition.pdf"));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.PermittedDownloadHosts = new string[] { "download.example.test" };
        PetitionDocumentDownloadOperation operation = new PetitionDocumentDownloadOperation(client, options);
        operation.DownloadOperation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest
        {
            SourceUri = new Uri("https://download.example.test/petitions/decision-1.pdf"),
            Output = output
        };

        DownloadResult result = operation.Download("document-1", download);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.ApiKeys[0], Is.Null);
            Assert.That(handler.Accepts[0], Is.EqualTo("*/*"));
            Assert.That(output.ToArray(), Is.EqualTo(document));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo("petition.pdf"));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Pdf));
        });
    }

    [Test]
    public void PetitionDocumentDownloadRejectsAnUnsafeResponseFileNameWithoutWriting()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => CreateDocumentResponse(new byte[] { 1, 2, 3 }, "../petition.pdf"));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.PermittedDownloadHosts = new string[] { "download.example.test" };
        PetitionDocumentDownloadOperation operation = new PetitionDocumentDownloadOperation(client, options);
        operation.DownloadOperation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest
        {
            SourceUri = new Uri("https://download.example.test/petitions/decision-1.pdf"),
            Output = output
        };

        DownloadResult result = operation.Download("document-1", download);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("INVALID_DOCUMENT_FILENAME"));
            Assert.That(output.Length, Is.Zero);
        });
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
                BufferSize = 8,
                RedirectHopCount = 3
            }
        };
    }

    private static HttpResponseMessage CreateDocumentResponse(byte[] body, string fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Pdf);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        return response;
    }
}

internal class PetitionPageHandler : HttpMessageHandler
{
    internal string[] ResponseBodies;
    internal List<string> RequestBodies = new List<string>();

    internal PetitionPageHandler(string[] responseBodies)
    {
        ResponseBodies = responseBodies;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
        int responseIndex = RequestBodies.Count;
        RequestBodies.Add(requestBody);
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ResponseBodies[responseIndex], Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return response;
    }
}
