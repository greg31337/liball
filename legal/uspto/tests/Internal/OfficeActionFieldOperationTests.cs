using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class OfficeActionFieldOperationTests
{
    [TestCase("oa_actions", "v1")]
    [TestCase("oa_citations", "v2")]
    [TestCase("oa_rejections", "v2")]
    [TestCase("enriched_cited_reference_metadata", "v3")]
    public void DiscoveryUsesPublishedRouteAndParsesMetadata(string dataset, string version)
    {
        string payload = CompleteResponse(dataset, version, "2020-03-12 11:19:05.0");
        RecordingOfficeActionFieldHandler handler = new RecordingOfficeActionFieldHandler(HttpStatusCode.OK, payload);
        handler.RequestIdentifier = "header-request";
        using HttpClient client = new HttpClient(handler);
        OfficeActionFieldOperation operation = new OfficeActionFieldOperation(client, CreateOptions(client));
        RequestContext context = new RequestContext { RetainRawResponse = true };

        OfficeActionFieldResponse result = operation.Discover(dataset, context);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Get }));
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/oa/" + dataset + "/" + version + "/fields"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.Metadata.DatasetName, Is.EqualTo(dataset));
            Assert.That(result.Metadata.Version, Is.EqualTo(version));
            Assert.That(result.Metadata.Status, Is.EqualTo("PUBLISHED"));
            Assert.That(result.Metadata.ServiceUri?.AbsolutePath, Is.EqualTo("/api/v1/patent/oa/" + dataset + "/" + version + "/fields"));
            Assert.That(result.Metadata.SchemaUri?.Host, Is.EqualTo("data.uspto.gov"));
            Assert.That(result.Metadata.LastUpdatedText, Is.EqualTo("2020-03-12 11:19:05.0"));
            Assert.That(result.Metadata.HasLastUpdatedAt, Is.True);
            Assert.That(result.Metadata.LastUpdatedAt, Is.EqualTo(new DateTimeOffset(2020, 3, 12, 11, 19, 5, TimeSpan.Zero)));
            Assert.That(result.Fields, Is.EqualTo(new string[] { "patentApplicationNumber", "submissionDate" }));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo(dataset));
            Assert.That(result.Provenance.DatasetVersion, Is.EqualTo(version));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.Provenance.HasRetrievedAt, Is.True);
            Assert.That(result.Provenance.HasLastUpdatedAt, Is.True);
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Does.Contain("\"apiKey\":\"[REDACTED]\""));
            Assert.That(result.RawResponse, Does.Contain("\"apiVersionNumber\":\"" + version + "\""));
            Assert.That(result.RawResponse, Does.Not.Contain("test-key"));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.UnknownFields["catalogRevision"], Is.EqualTo("7"));
        });
    }

    [Test]
    public void UnrecognizedLastUpdateIsPreservedWithoutRejectingResponse()
    {
        RecordingOfficeActionFieldHandler handler = new RecordingOfficeActionFieldHandler(
            HttpStatusCode.OK,
            CompleteResponse("oa_actions", "v1", "update pending publication"));
        using HttpClient client = new HttpClient(handler);
        OfficeActionFieldOperation operation = new OfficeActionFieldOperation(client, CreateOptions(client));

        OfficeActionFieldResponse result = operation.Discover("oa_actions", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Metadata.LastUpdatedText, Is.EqualTo("update pending publication"));
            Assert.That(result.Metadata.HasLastUpdatedAt, Is.False);
            Assert.That(result.Provenance.HasLastUpdatedAt, Is.False);
        });
    }

    [TestCase("[]", "$")]
    [TestCase("{\"apiVersionNumber\":\"v1\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":0,\"fields\":[]}", "$.apiKey")]
    [TestCase("{\"apiKey\":\"oa_citations\",\"apiVersionNumber\":\"v1\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":0,\"fields\":[]}", "$.apiKey")]
    [TestCase("{\"apiKey\":\"oa_actions\",\"apiVersionNumber\":\"v2\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":0,\"fields\":[]}", "$.apiVersionNumber")]
    [TestCase("{\"apiKey\":\"oa_actions\",\"apiVersionNumber\":\"v1\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"not a uri\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":0,\"fields\":[]}", "$.apiUrl")]
    [TestCase("{\"apiKey\":\"oa_actions\",\"apiVersionNumber\":\"v1\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":2,\"fields\":[\"one\"]}", "$.fieldCount")]
    [TestCase("{\"apiKey\":\"oa_actions\",\"apiVersionNumber\":\"v1\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":1,\"fields\":[false]}", "$.fields[0]")]
    [TestCase("{\"apiKey\":\"oa_actions\",\"apiVersionNumber\":\"v1\",\"apiStatus\":\"PUBLISHED\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger\",\"fieldCount\":0,\"fields\":[],\"lastDataUpdatedDate\":{}}", "$.lastDataUpdatedDate")]
    public void MalformedMetadataReportsExactPath(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(json));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("Office Action field discovery"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase(null, "REQUIRED")]
    [TestCase("", "REQUIRED")]
    [TestCase("OA_ACTIONS", "UNSUPPORTED_OFFICE_ACTION_DATASET")]
    [TestCase("unknown", "UNSUPPORTED_OFFICE_ACTION_DATASET")]
    public void InvalidDatasetFailsBeforeSending(string? dataset, string expectedCode)
    {
        RecordingOfficeActionFieldHandler handler = new RecordingOfficeActionFieldHandler(HttpStatusCode.OK, CompleteResponse("oa_actions", "v1", null));
        using HttpClient client = new HttpClient(handler);
        OfficeActionFieldOperation operation = new OfficeActionFieldOperation(client, CreateOptions(client));

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.Discover(dataset!, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(handler.Methods, Is.Empty);
        });
    }

    [Test]
    public void RemoteFailureUsesSharedOfficeActionErrorMapping()
    {
        RecordingOfficeActionFieldHandler handler = new RecordingOfficeActionFieldHandler(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");
        using HttpClient client = new HttpClient(handler);
        OfficeActionFieldOperation operation = new OfficeActionFieldOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.Discover("oa_rejections", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.OfficeAction));
            Assert.That(exception.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public void CancellationStopsBeforeDiscoveryRequest()
    {
        RecordingOfficeActionFieldHandler handler = new RecordingOfficeActionFieldHandler(HttpStatusCode.OK, CompleteResponse("oa_actions", "v1", null));
        using HttpClient client = new HttpClient(handler);
        OfficeActionFieldOperation operation = new OfficeActionFieldOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.Discover("oa_actions", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.Methods, Is.Empty);
    }

    private static OfficeActionFieldResponse Parse(string json)
    {
        return OfficeActionFieldParser.Parse(json, "oa_actions", "v1", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/fields"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        }, null);
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        return new UsptoClientOptions
        {
            OfficeActionBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string CompleteResponse(string dataset, string version, string? lastUpdated)
    {
        string lastUpdateField = string.Empty;
        if (lastUpdated != null)
        {
            lastUpdateField = ",\"lastDataUpdatedDate\":\"" + lastUpdated + "\"";
        }

        return "{\"apiKey\":\"" + dataset + "\",\"apiVersionNumber\":\"" + version + "\",\"apiUrl\":\"https://api.uspto.gov/api/v1/patent/oa/" + dataset + "/" + version + "/fields\",\"apiDocumentationUrl\":\"https://data.uspto.gov/swagger/index.html\",\"apiStatus\":\"PUBLISHED\",\"fieldCount\":2,\"fields\":[\"patentApplicationNumber\",\"submissionDate\"]" + lastUpdateField + ",\"requestIdentifier\":\"body-request\",\"catalogRevision\":7}";
    }
}

internal class RecordingOfficeActionFieldHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal string ResponseBody;
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal string? RequestIdentifier;

    internal RecordingOfficeActionFieldHandler(HttpStatusCode statusCode, string responseBody)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifier);
        }

        return Task.FromResult(response);
    }
}
