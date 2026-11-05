using System.Net;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentApplicationRetrievalOperationTests
{
    [Test]
    public void ApplicationRetrievalNormalizesTheDirectPathAndBuildsTheWrapper()
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(
            HttpStatusCode.OK,
            "{\"count\":0,\"patentFileWrapperDataBag\":[]}",
            FixtureLoader.LoadText("patent-applications/retrieval-full.json"));
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentFileWrapper result = operation.GetApplication("14/412,875", new RequestContext());
        using JsonDocument futureMetadata = JsonDocument.Parse(result.Metadata.UnknownFields["futureMetadata"]);
        using JsonDocument assignmentBag = JsonDocument.Parse(result.UnknownSections["assignmentBag"]);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Summary.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Summary.InventionTitle, Is.EqualTo("Example invention"));
            Assert.That(result.Metadata.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Metadata.PatentNumber, Is.EqualTo("12156579"));
            Assert.That(result.Metadata.PublicationNumbers, Is.EqualTo(new string[] { "US 2024/0012345 A1" }));
            Assert.That(result.Metadata.HasFilingDate, Is.True);
            Assert.That(result.Metadata.ApplicationStatusCode, Is.EqualTo(150));
            Assert.That(futureMetadata.RootElement.GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(assignmentBag.RootElement[0].GetProperty("reelNumber").GetString(), Is.EqualTo("1234"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("direct-request"));
        });
    }

    [Test]
    public void SparseRetrievalFixtureKeepsOptionalSectionsEmpty()
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(
            HttpStatusCode.OK,
            string.Empty,
            FixtureLoader.LoadText("patent-applications/retrieval-sparse.json"));
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentFileWrapper result = operation.GetNormalizedApplication("14/412,875", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Summary.InventionTitle, Is.Null);
            Assert.That(result.Metadata.PatentNumber, Is.Null);
            Assert.That(result.Metadata.HasFilingDate, Is.False);
            Assert.That(result.Metadata.PublicationNumbers, Is.Empty);
            Assert.That(result.Metadata.Parties.Applicants, Is.Empty);
            Assert.That(result.Metadata.Parties.Inventors, Is.Empty);
            Assert.That(result.Metadata.Classifications.Cpc, Is.Empty);
            Assert.That(result.Metadata.Classifications.Ipc, Is.Empty);
            Assert.That(result.Metadata.Classifications.Uspc, Is.Empty);
            Assert.That(result.Metadata.Classifications.Wipo, Is.Empty);
            Assert.That(result.Metadata.Classifications.GovernmentInterests, Is.Empty);
            Assert.That(result.PublicationFiles, Is.Empty);
            Assert.That(result.GrantFiles, Is.Empty);
            Assert.That(result.UnknownSections, Is.Empty);
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("sparse-request"));
        });
    }

    [TestCase("US 12,156,579 B2", "applicationMetaData.patentNumber:\"12156579\"")]
    [TestCase("US20240012345A9", "applicationMetaData.earliestPublicationNumber:\"US 2024/0012345 A9\"")]
    [TestCase("PCT/US2025/058371", "applicationNumberText:\"PCTUS2025058371\"")]
    public void GrantPublicationAndPctRetrievalResolveBeforeUsingTheDirectRoute(string identifier, string expectedQuery)
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(
            HttpStatusCode.OK,
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}",
            CompleteApplication("14412875"));
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentFileWrapper result = operation.GetApplication(identifier, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/search"));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "q"), Is.EqualTo(expectedQuery));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "fields"), Is.EqualTo("applicationNumberText"));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "limit"), Is.EqualTo("2"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
        });
    }

    [Test]
    public void ExplicitResolutionReturnsTheCanonicalApplicationWithoutRetrievingIt()
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(
            HttpStatusCode.OK,
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}",
            CompleteApplication("14412875"));
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        string result = operation.ResolveIdentifier("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("14412875"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
        });
    }

    [Test]
    public void DirectRetrievalRejectsAResponseForAnotherApplication()
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(
            HttpStatusCode.OK,
            string.Empty,
            CompleteApplication("15/123,456"));
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        ParseException exception = Assert.Throws<ParseException>(() => operation.GetNormalizedApplication("14412875", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].applicationNumberText"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DirectRetrievalMapsNotFoundWithTheCanonicalApplicationNumber()
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(
            HttpStatusCode.NotFound,
            string.Empty,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-request\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedApplication("14/412,875", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("14412875"));
            Assert.That(exception.StatusCode, Is.EqualTo(404));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-request"));
        });
    }

    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    public void MalformedDirectResponsesHavePreciseDataPaths(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpJsonParser.ParsePatentApplication(json, "14412875", ResponseMetadata()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [Test]
    public void CancellationStopsBeforeIdentifierResolutionOrRetrieval()
    {
        RecordingPatentRetrievalHandler handler = new RecordingPatentRetrievalHandler(HttpStatusCode.OK, string.Empty, CompleteApplication("14412875"));
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RequestContext context = new RequestContext { CancellationToken = cancellation.Token };

        Assert.Throws<OperationCanceledException>(() => operation.GetApplication("US 12,156,579 B2", context));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static string CompleteApplication(string applicationNumber)
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"" + applicationNumber + "\",\"applicationMetaData\":{\"patentNumber\":\"12156579\",\"earliestPublicationNumber\":\"US 2024/0012345 A1\",\"inventionTitle\":\"Example invention\",\"applicationTypeCode\":\"UTL\",\"applicationTypeLabelName\":\"Utility\",\"filingDate\":\"2023-01-02\",\"earliestPublicationDate\":\"2024-01-04\",\"grantDate\":\"2025-03-05\",\"applicationStatusCode\":150,\"applicationStatusDescriptionText\":\"Patented Case\",\"firstApplicantName\":\"Example Applicant\",\"firstInventorName\":\"Example Inventor\",\"groupArtUnitNumber\":\"2814\",\"futureMetadata\":{\"enabled\":true}},\"assignmentBag\":[{\"reelNumber\":\"1234\"}]}],\"requestIdentifier\":\"direct-request\"}";
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

    private static HttpResponseMetadata ResponseMetadata()
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14412875"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        };
    }

    private static string? ReadQueryValue(Uri uri, string name)
    {
        foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]) == name)
            {
                return parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : null;
            }
        }

        return null;
    }
}

internal class RecordingPatentRetrievalHandler : HttpMessageHandler
{
    internal HttpStatusCode DirectStatusCode;
    internal string LookupResponse;
    internal string DirectResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingPatentRetrievalHandler(HttpStatusCode directStatusCode, string lookupResponse, string directResponse)
    {
        DirectStatusCode = directStatusCode;
        LookupResponse = lookupResponse;
        DirectResponse = directResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool isLookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpStatusCode statusCode = isLookup ? HttpStatusCode.OK : DirectStatusCode;
        string body = isLookup ? LookupResponse : DirectResponse;
        HttpResponseMessage response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
