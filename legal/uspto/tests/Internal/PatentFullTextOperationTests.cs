using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentFullTextOperationTests
{
    [Test]
    public void SelectionPrefersDirectGrantXmlAndSkipsArchiveOnlyMetadata()
    {
        PatentAssociatedDocumentList documents = new PatentAssociatedDocumentList
        {
            ApplicationNumber = "18123456",
            GrantFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata { LocationUri = new Uri("https://bulkdata.uspto.gov/grants/week.zip"), Format = DocumentFormat.Zip },
                new PatentFileMetadata { LocationUri = new Uri("https://api.uspto.gov/download/opaque"), Format = DocumentFormat.Xml, DocumentIdentifier = "opaque" },
                new PatentFileMetadata { LocationUri = new Uri("https://bulkdata.uspto.gov/grants/patent.xml"), DocumentIdentifier = "direct" }
            },
            PublicationFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata { LocationUri = new Uri("https://bulkdata.uspto.gov/applications/publication.xml"), Format = DocumentFormat.Xml }
            }
        };

        PatentFullTextFileSelection selection = PatentFullTextOperation.SelectFile(documents);

        Assert.Multiple(() =>
        {
            Assert.That(selection.DocumentCategory, Is.EqualTo("grant"));
            Assert.That(selection.ExpectedRootName, Is.EqualTo("us-patent-grant"));
            Assert.That(selection.File.DocumentIdentifier, Is.EqualTo("direct"));
        });
    }

    [Test]
    public void SelectionUsesXmlFileNameForAnExtensionlessDownloadLocation()
    {
        PatentAssociatedDocumentList documents = new PatentAssociatedDocumentList
        {
            ApplicationNumber = "18123456",
            GrantFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata
                {
                    LocationUri = new Uri("https://download.example.test/document/123"),
                    XmlFileName = "US123.xml"
                }
            }
        };

        PatentFullTextFileSelection selection = PatentFullTextOperation.SelectFile(documents);

        Assert.Multiple(() =>
        {
            Assert.That(selection.File, Is.SameAs(documents.GrantFiles[0]));
            Assert.That(selection.DocumentCategory, Is.EqualTo("grant"));
            Assert.That(selection.ExpectedRootName, Is.EqualTo("us-patent-grant"));
        });
    }

    [Test]
    public void XmlFileNameDoesNotTurnAnArchiveLocationIntoADirectXmlCandidate()
    {
        PatentAssociatedDocumentList documents = new PatentAssociatedDocumentList
        {
            ApplicationNumber = "18123456",
            GrantFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata
                {
                    LocationUri = new Uri("https://download.example.test/week.zip"),
                    XmlFileName = "US123.xml"
                }
            }
        };

        NotFoundException exception = Assert.Throws<NotFoundException>(() => PatentFullTextOperation.SelectFile(documents));

        Assert.That(exception.ResourceIdentifier, Is.EqualTo("18123456"));
    }

    [Test]
    public void RetrievalDownloadsParsesAndAttributesSelectedGrantXml()
    {
        string xml = "<!DOCTYPE us-patent-grant SYSTEM \"us-patent-grant-v47.dtd\"><us-patent-grant dtd-version=\"v4.7\" file=\"US123.XML\"><us-bibliographic-data-grant><invention-title>Downloaded grant</invention-title></us-bibliographic-data-grant><claims><claim num=\"1\"><claim-text>1. A downloaded claim.</claim-text></claim></claims></us-patent-grant>";
        FullTextOperationHandler handler = new FullTextOperationHandler(HttpStatusCode.OK, Encoding.UTF8.GetBytes(xml), AssociatedResponse());
        using HttpClient client = new HttpClient(handler);
        PatentFullTextOperation operation = CreateOperation(client);

        PatentFullText result = operation.Get("18/123,456", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/18123456/associated-documents"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/grant.xml"));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json, "*/*" }));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string?[] { "test-key", null }));
            Assert.That(result.DocumentCategory, Is.EqualTo("grant"));
            Assert.That(result.BibliographicData.InventionTitle, Is.EqualTo("Downloaded grant"));
            Assert.That(result.Claims[0].Number, Is.EqualTo("1"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo("PTGRXML"));
            Assert.That(result.Provenance.DatasetVersion, Is.EqualTo("v4.7"));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("associated-request"));
            Assert.That(result.Provenance.HasRetrievedAt, Is.True);
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Does.Not.Contain("signed-secret"));
            Assert.That(result.Provenance.SourceUri?.AbsolutePath, Is.EqualTo("/grant.xml"));
        });
    }

    [Test]
    public void PublicationXmlIsUsedWhenGrantMetadataContainsOnlyAnArchive()
    {
        string xml = "<us-patent-application dtd-version=\"v4.6\"><us-bibliographic-data-application><invention-title>Published text</invention-title></us-bibliographic-data-application></us-patent-application>";
        FullTextOperationHandler handler = new FullTextOperationHandler(HttpStatusCode.OK, Encoding.UTF8.GetBytes(xml), "{}");
        using HttpClient client = new HttpClient(handler);
        PatentFullTextOperation operation = CreateOperation(client);
        PatentAssociatedDocumentList documents = new PatentAssociatedDocumentList
        {
            ApplicationNumber = "18123456",
            GrantFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata { LocationUri = new Uri("https://download.example.test/grant.zip"), Format = DocumentFormat.Zip }
            },
            PublicationFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata
                {
                    DocumentIdentifier = "US20240012345A1",
                    ProductIdentifier = "APPXML",
                    XmlFileName = "application.xml",
                    LocationUri = new Uri("https://download.example.test/application.xml"),
                    Format = DocumentFormat.Xml
                }
            }
        };

        PatentFullText result = operation.GetFromAssociatedDocuments(documents, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(result.RootElementName, Is.EqualTo("us-patent-application"));
            Assert.That(result.DocumentCategory, Is.EqualTo("published application"));
            Assert.That(result.BibliographicData.InventionTitle, Is.EqualTo("Published text"));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo("APPXML"));
            Assert.That(result.Provenance.DatasetVersion, Is.EqualTo("v4.6"));
        });
    }

    [Test]
    public void DownloadedRootMustMatchTheSelectedMetadataCategory()
    {
        string xml = "<us-patent-application><us-bibliographic-data-application/></us-patent-application>";
        FullTextOperationHandler handler = new FullTextOperationHandler(HttpStatusCode.OK, Encoding.UTF8.GetBytes(xml), "{}");
        using HttpClient client = new HttpClient(handler);
        PatentFullTextOperation operation = CreateOperation(client);
        PatentAssociatedDocumentList documents = GrantDocuments("https://download.example.test/grant.xml");

        ParseException exception = Assert.Throws<ParseException>(() => operation.GetFromAssociatedDocuments(documents, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(exception.SourceName, Is.EqualTo("grant.xml"));
            Assert.That(exception.DataPath, Is.EqualTo("/us-patent-application"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void MissingDirectXmlAndFailedDownloadsProduceTypedFailures()
    {
        PatentAssociatedDocumentList archiveOnly = new PatentAssociatedDocumentList
        {
            ApplicationNumber = "18123456",
            RequestIdentifier = "missing-request",
            GrantFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata { LocationUri = new Uri("https://download.example.test/grant.zip"), Format = DocumentFormat.Zip }
            }
        };
        NotFoundException missing = Assert.Throws<NotFoundException>(() => PatentFullTextOperation.SelectFile(archiveOnly));

        FullTextOperationHandler handler = new FullTextOperationHandler(HttpStatusCode.ServiceUnavailable, Encoding.UTF8.GetBytes("unavailable"), "{}");
        using HttpClient client = new HttpClient(handler);
        PatentFullTextOperation operation = CreateOperation(client);
        DownloadException failed = Assert.Throws<DownloadException>(() => operation.GetFromAssociatedDocuments(GrantDocuments("https://download.example.test/grant.xml"), new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(missing.ResourceIdentifier, Is.EqualTo("18123456"));
            Assert.That(missing.RequestIdentifier, Is.EqualTo("missing-request"));
            Assert.That(failed.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(failed.BytesTransferred, Is.Zero);
            Assert.That(failed.StatusCode, Is.EqualTo(503));
            Assert.That(failed.HasStatusCode, Is.True);
            Assert.That(failed.IsRetryable, Is.True);
            Assert.That(failed.SourceUri?.AbsolutePath, Is.EqualTo("/grant.xml"));
        });
    }

    [Test]
    public void UnapprovedMetadataHostIsRejectedBeforeAFullTextRequestIsSent()
    {
        FullTextOperationHandler handler = new FullTextOperationHandler(HttpStatusCode.OK, Encoding.UTF8.GetBytes("<us-patent-grant/>"), "{}");
        using HttpClient client = new HttpClient(handler);
        PatentFullTextOperation operation = CreateOperation(client);

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.GetFromAssociatedDocuments(GrantDocuments("https://unapproved.example.test/grant.xml"), new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_NOT_ALLOWED"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    private static PatentFullTextOperation CreateOperation(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            PermittedDownloadHosts = new[] { "download.example.test" },
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 32,
                RedirectHopCount = 3,
                MaximumBytes = 100000,
                HasMaximumBytes = true
            }
        };
        PatentFullTextOperation operation = new PatentFullTextOperation(client, options);
        operation.DownloadOperation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new[] { IPAddress.Parse("8.8.8.8") }));
        return operation;
    }

    private static PatentAssociatedDocumentList GrantDocuments(string location)
    {
        return new PatentAssociatedDocumentList
        {
            ApplicationNumber = "18123456",
            GrantFiles = new PatentFileMetadata[]
            {
                new PatentFileMetadata
                {
                    DocumentIdentifier = "123",
                    ProductIdentifier = "PTGRXML",
                    XmlFileName = "grant.xml",
                    LocationUri = new Uri(location),
                    Format = DocumentFormat.Xml
                }
            }
        };
    }

    private static string AssociatedResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "18/123,456",
                  "grantDocumentMetaData": [
                    {
                      "productIdentifier": "PTGRXML",
                      "zipFileName": "week.zip",
                      "xmlFileName": "week.xml",
                      "fileLocationURI": "https://download.example.test/week.zip",
                      "patentNumber": "123"
                    },
                    {
                      "productIdentifier": "PTGRXML",
                      "xmlFileName": "grant.xml",
                      "fileMediaType": "application/xml",
                      "fileLocationURI": "https://download.example.test/grant.xml?X-Amz-Signature=signed-secret",
                      "patentNumber": "123"
                    }
                  ],
                  "pgpubDocumentMetaData": {
                    "productIdentifier": "APPXML",
                    "xmlFileName": "application.xml",
                    "fileLocationURI": "https://download.example.test/application.xml",
                    "publicationNumberText": "US20240012345A1"
                  },
                  "requestIdentifier": "associated-request"
                }
              ]
            }
            """;
    }
}

internal class FullTextOperationHandler : HttpMessageHandler
{
    internal HttpStatusCode DownloadStatus;
    internal byte[] DownloadBody;
    internal string AssociatedJson;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal FullTextOperationHandler(HttpStatusCode downloadStatus, byte[] downloadBody, string associatedJson)
    {
        DownloadStatus = downloadStatus;
        DownloadBody = downloadBody;
        AssociatedJson = associatedJson;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        string? apiKey = null;
        if (request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? values))
        {
            apiKey = values.Single();
        }

        ApiKeys.Add(apiKey);
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        HttpResponseMessage response;
        if (request.RequestUri!.AbsolutePath.EndsWith("/associated-documents", StringComparison.Ordinal))
        {
            response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(AssociatedJson, Encoding.UTF8, UsptoMediaTypes.Json);
        }
        else
        {
            response = new HttpResponseMessage(DownloadStatus);
            response.Content = new ByteArrayContent(DownloadBody);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Xml);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "grant.xml" };
        }

        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
