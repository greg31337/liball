using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PtabDocumentDownloadOperationTests
{
    [TestCase(PtabCollection.TrialDecisions, UsptoMediaTypes.Pdf, "decision.pdf", DocumentFormat.Pdf)]
    [TestCase(PtabCollection.TrialDocuments, UsptoMediaTypes.Xml, "document.xml", DocumentFormat.Xml)]
    [TestCase(PtabCollection.TrialDocuments, UsptoMediaTypes.Zip, "documents.zip", DocumentFormat.Zip)]
    [TestCase(PtabCollection.AppealDecisions, UsptoMediaTypes.Json, "appeal.json", DocumentFormat.Json)]
    [TestCase(PtabCollection.AppealDecisions, UsptoMediaTypes.Text, "appeal.txt", DocumentFormat.Text)]
    [TestCase(PtabCollection.InterferenceDecisions, UsptoMediaTypes.Binary, "interference.bin", DocumentFormat.Unknown)]
    public void DownloadsSupportedPtabCollectionsAndDocumentMedia(PtabCollection collection, string mediaType, string fileName, DocumentFormat format)
    {
        byte[] body = Encoding.UTF8.GetBytes("ptab-document");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) => CreateResponse(HttpStatusCode.OK, body, mediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest
        {
            SourceUri = new Uri("https://ptab.uspto.gov/doc/" + fileName),
            Output = output,
            ExpectedSize = body.Length,
            HasExpectedSize = true
        };

        DownloadResult result = operation.Download(collection, "DOCUMENT-1", request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.MediaType, Is.EqualTo(mediaType));
            Assert.That(result.Content.Format, Is.EqualTo(format));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(handler.RequestUris[0].Host, Is.EqualTo("ptab.uspto.gov"));
            Assert.That(handler.ApiKeys[0], Is.Null);
            Assert.That(handler.Accepts[0], Is.EqualTo("*/*"));
        });
    }

    [Test]
    public void RelativePtabUriResolvesAgainstOdpBaseWithoutMutatingRequest()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) => CreateResponse(HttpStatusCode.OK, new byte[] { 1, 2 }, UsptoMediaTypes.Pdf, "relative.pdf"));
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);
        using MemoryStream output = new MemoryStream();
        Uri relative = new Uri("/download/relative.pdf", UriKind.Relative);
        DownloadRequest request = new DownloadRequest { SourceUri = relative, Output = output };

        DownloadResult result = operation.Download(PtabCollection.AppealDecisions, "DOCUMENT-2", request);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(request.SourceUri, Is.SameAs(relative));
            Assert.That(request.SourceUri?.IsAbsoluteUri, Is.False);
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://api.uspto.gov/download/relative.pdf"));
            Assert.That(handler.ApiKeys[0], Is.EqualTo("test-key"));
        });
    }

    [Test]
    public void ExtensionlessPtabUriUsesResponseMediaTypeAndIdentifierFileName()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(new byte[] { 1, 2, 3 });
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Pdf);
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(PtabCollection.TrialDecisions, "100012345", new DownloadRequest
        {
            SourceUri = new Uri("https://ptab.uspto.gov/doc/100012345"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo("100012345"));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(output.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void UnapprovedRedirectIsRejectedBeforePtabCredentialsOrBytesReachTarget()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://unapproved.example.test/document.pdf");
            response.Content = new ByteArrayContent(Array.Empty<byte>());
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.Download(PtabCollection.TrialDocuments, "DOCUMENT-3", new DownloadRequest
        {
            SourceUri = new Uri("https://ptab.uspto.gov/doc/start.pdf"),
            Output = output
        }));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_NOT_ALLOWED"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.ApiKeys[0], Is.Null);
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(PtabCollection.TrialProceedings)]
    [TestCase((PtabCollection)99)]
    public void CollectionWithoutDocumentsIsRejectedBeforeNetworkAccess(PtabCollection collection)
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) => CreateResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.Download(collection, "DOCUMENT-4", new DownloadRequest
        {
            SourceUri = new Uri("https://ptab.uspto.gov/doc/document.pdf"),
            Output = output
        }));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("UNSUPPORTED_PTAB_DOCUMENT_COLLECTION"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void MissingIdentifierOrRequestIsRejectedBeforeNetworkAccess()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) => CreateResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);

        ValidationException identifier = Assert.Throws<ValidationException>(() => operation.Download(PtabCollection.TrialDecisions, " ", new DownloadRequest()));
        ValidationException request = Assert.Throws<ValidationException>(() => operation.Download(PtabCollection.TrialDecisions, "DOCUMENT-5", null!));

        Assert.Multiple(() =>
        {
            Assert.That(identifier.FieldName, Is.EqualTo("documentIdentifier"));
            Assert.That(request.FieldName, Is.EqualTo("request"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void RemoteDownloadFailureReturnsMetadataWithoutWritingErrorBody()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, requestMessage) => CreateResponse(HttpStatusCode.NotFound, Encoding.UTF8.GetBytes("not found"), UsptoMediaTypes.Json, "error.json"));
        using HttpClient client = new HttpClient(handler);
        PtabDocumentDownloadOperation operation = CreateOperation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(PtabCollection.TrialDocuments, "DOCUMENT-6", new DownloadRequest
        {
            SourceUri = new Uri("https://ptab.uspto.gov/doc/DOCUMENT-6"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("HTTP_STATUS_404"));
            Assert.That(result.Progress.BytesTransferred, Is.Zero);
            Assert.That(output.Length, Is.Zero);
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    private static PtabDocumentDownloadOperation CreateOperation(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://api.uspto.gov/api/v1/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 4,
                RedirectHopCount = 3
            }
        };
        PtabDocumentDownloadOperation operation = new PtabDocumentDownloadOperation(client, options);
        operation.DownloadOperation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        return operation;
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode status, byte[] body, string mediaType, string fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        return response;
    }
}
