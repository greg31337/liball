using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentAssociatedDocumentOperationTests
{
    [Test]
    public void RetrievalMapsGrantAndPreGrantArchiveAndXmlMetadata()
    {
        RecordingAssociatedDocumentHandler handler = new RecordingAssociatedDocumentHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentAssociatedDocumentList result = operation.GetAssociatedDocuments("14/412,875", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/associated-documents"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.PublicationFiles, Has.Length.EqualTo(1));
            Assert.That(result.GrantFiles, Has.Length.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("record-request"));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("record-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Is.EqualTo(handler.RequestUris[0].AbsoluteUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["futureRecord"], Is.EqualTo("{ \"revision\": 2 }"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("true"));
        });

        PatentFileMetadata publication = result.PublicationFiles[0];
        Assert.Multiple(() =>
        {
            Assert.That(publication.Category, Is.EqualTo("PreGrantPublication"));
            Assert.That(publication.DocumentIdentifier, Is.EqualTo("US20240012345A1"));
            Assert.That(publication.ProductIdentifier, Is.EqualTo("APPXML"));
            Assert.That(publication.ArchiveFileName, Is.EqualTo("ipa240801.zip"));
            Assert.That(publication.XmlFileName, Is.EqualTo("ipa240801.xml"));
            Assert.That(publication.LocationUri, Is.EqualTo(new Uri("https://bulkdata.uspto.gov/data/patent/application/ipa240801.zip")));
            Assert.That(publication.MediaType, Is.Null);
            Assert.That(publication.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(publication.CreatedAt, Is.EqualTo(new DateTimeOffset(2024, 8, 9, 11, 30, 0, TimeSpan.Zero)));
            Assert.That(publication.HasCreatedAt, Is.True);
            Assert.That(publication.HasSize, Is.False);
            Assert.That(publication.UnknownFields["publicationEdition"], Is.EqualTo("\"A1\""));
        });

        PatentFileMetadata grantArchive = result.GrantFiles[0];
        Assert.Multiple(() =>
        {
            Assert.That(grantArchive.Category, Is.EqualTo("Grant"));
            Assert.That(grantArchive.DocumentIdentifier, Is.EqualTo("12156579"));
            Assert.That(grantArchive.ProductIdentifier, Is.EqualTo("PTGRXML"));
            Assert.That(grantArchive.ArchiveFileName, Is.EqualTo("ipg240102.zip"));
            Assert.That(grantArchive.XmlFileName, Is.EqualTo("ipg240102.xml"));
            Assert.That(grantArchive.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(grantArchive.Size, Is.EqualTo(4294967296L));
            Assert.That(grantArchive.HasSize, Is.True);
        });

        PatentFileMetadata grantXml = result.GrantFiles[1];
        Assert.Multiple(() =>
        {
            Assert.That(grantXml.DocumentIdentifier, Is.EqualTo("12156580"));
            Assert.That(grantXml.ArchiveFileName, Is.Null);
            Assert.That(grantXml.XmlFileName, Is.EqualTo("grant.xml"));
            Assert.That(grantXml.LocationUri, Is.EqualTo(new Uri("https://bulkdata.uspto.gov/data/patent/grant/grant.xml")));
            Assert.That(grantXml.MediaType, Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(grantXml.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(grantXml.HasCreatedAt, Is.False);
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeAssociatedDocumentRetrieval()
    {
        RecordingAssociatedDocumentHandler handler = new RecordingAssociatedDocumentHandler(
            HttpStatusCode.OK,
            EmptyResponse(),
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentAssociatedDocumentList result = operation.GetAssociatedDocuments("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/associated-documents"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.Zero);
        });
    }

    [Test]
    public void SparseResponseUsesHeaderRequestIdentifierAndEmptyDefaults()
    {
        PatentAssociatedDocumentList result = Parse(EmptyResponse());
        PatentAssociatedDocumentList empty = new PatentAssociatedDocumentList();
        PatentFileMetadata file = new PatentFileMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.PublicationFiles, Is.Empty);
            Assert.That(result.GrantFiles, Is.Empty);
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(empty.PublicationFiles, Is.Empty);
            Assert.That(empty.GrantFiles, Is.Empty);
            Assert.That(empty.UnknownFields, Is.Empty);
            Assert.That(file.UnknownFields, Is.Empty);
        });
    }

    [TestCase("{}", "$.count")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[false]}", "$.patentFileWrapperDataBag[0]")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/123,456\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"pgpubDocumentMetaData\":17}]}", "$.patentFileWrapperDataBag[0].pgpubDocumentMetaData")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"grantDocumentMetaData\":[false]}]}", "$.patentFileWrapperDataBag[0].grantDocumentMetaData[0]")]
    public void MalformedContainersReportTheirExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent associated-document retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"fileLocationURI\":\"relative/archive.zip\"", "fileLocationURI")]
    [TestCase("\"fileCreateDateTime\":\"eventually\"", "fileCreateDateTime")]
    [TestCase("\"fileSize\":-1", "fileSize")]
    [TestCase("\"fileSize\":{}", "fileSize")]
    [TestCase("\"xmlFileName\":{}", "xmlFileName")]
    public void MalformedFileMetadataReportsItsExactPath(string fileFragment, string fieldName)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"pgpubDocumentMetaData\":{" + fileFragment + "}}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].pgpubDocumentMetaData." + fieldName));
    }

    [Test]
    public void RetrievalMapsNotFoundUsingTheCanonicalApplicationNumber()
    {
        RecordingAssociatedDocumentHandler handler = new RecordingAssociatedDocumentHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-associated-documents\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedAssociatedDocuments("14/412,875", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("14412875"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-associated-documents"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CancellationStopsBeforeAssociatedDocumentRetrieval()
    {
        RecordingAssociatedDocumentHandler handler = new RecordingAssociatedDocumentHandler(HttpStatusCode.OK, EmptyResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetAssociatedDocuments("14/412,875", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentAssociatedDocumentList Parse(string response)
    {
        return OdpPatentAssociatedDocumentParser.Parse(response, "14412875", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14412875/associated-documents"),
            RequestIdentifier = "header-request",
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
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
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string EmptyResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}";
    }

    private static string CompleteResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "14/412,875",
                  "pgpubDocumentMetaData": {
                    "productIdentifier": "APPXML",
                    "zipFileName": "ipa240801.zip",
                    "xmlFileName": "ipa240801.xml",
                    "fileLocationURI": "https://bulkdata.uspto.gov/data/patent/application/ipa240801.zip",
                    "fileCreateDateTime": "2024-08-09:11:30:00Z",
                    "publicationNumberText": "US20240012345A1",
                    "publicationEdition": "A1"
                  },
                  "grantDocumentMetaData": [
                    {
                      "productIdentifier": "PTGRXML",
                      "zipFileName": "ipg240102.zip",
                      "xmlFileName": "ipg240102.xml",
                      "fileLocationURI": "https://bulkdata.uspto.gov/data/patent/grant/ipg240102.zip",
                      "fileSize": "4294967296",
                      "patentNumber": "12156579"
                    },
                    {
                      "xmlFileName": "grant.xml",
                      "fileLocationURI": "https://bulkdata.uspto.gov/data/patent/grant/grant.xml",
                      "fileMediaType": "application/xml",
                      "patentNumberText": "12156580"
                    }
                  ],
                  "requestIdentifier": "record-request",
                  "futureRecord": { "revision": 2 }
                }
              ],
              "requestIdentifier": "root-request",
              "futureResponse": true
            }
            """;
    }
}

internal class RecordingAssociatedDocumentHandler : HttpMessageHandler
{
    internal HttpStatusCode AssociatedDocumentStatusCode;
    internal string AssociatedDocumentResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingAssociatedDocumentHandler(HttpStatusCode associatedDocumentStatusCode, string associatedDocumentResponse)
    {
        AssociatedDocumentStatusCode = associatedDocumentStatusCode;
        AssociatedDocumentResponse = associatedDocumentResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingAssociatedDocumentHandler(HttpStatusCode associatedDocumentStatusCode, string associatedDocumentResponse, string lookupResponse)
    {
        AssociatedDocumentStatusCode = associatedDocumentStatusCode;
        AssociatedDocumentResponse = associatedDocumentResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpResponseMessage response = new HttpResponseMessage(lookup ? HttpStatusCode.OK : AssociatedDocumentStatusCode)
        {
            Content = new StringContent(lookup ? LookupResponse : AssociatedDocumentResponse, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
