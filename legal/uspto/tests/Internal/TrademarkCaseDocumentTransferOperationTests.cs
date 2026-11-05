using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TrademarkCaseDocumentTransferOperationTests
{
    [TestCase(1, DocumentFormat.Pdf, "content.pdf", UsptoMediaTypes.Pdf, "case.pdf")]
    [TestCase(1, DocumentFormat.Zip, "content.zip", UsptoMediaTypes.Zip, "case.zip")]
    [TestCase(2, DocumentFormat.Pdf, "download.pdf", UsptoMediaTypes.Pdf, "case.pdf")]
    [TestCase(2, DocumentFormat.Zip, "download.zip", UsptoMediaTypes.Zip, "case.zip")]
    [TestCase(3, DocumentFormat.Xml, "bundle", UsptoMediaTypes.Xml, "case-bundle.xml")]
    public void PublishedSingleCaseRoutesStreamStrictRepresentations(int operationKind, DocumentFormat format, string routeName, string mediaType, string fileName)
    {
        byte[] body = Encoding.UTF8.GetBytes("case-documents-" + routeName);
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(HttpStatusCode.OK, body, mediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        TrademarkCaseDocumentTransferOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        DownloadResult result;
        if (operationKind == 1)
        {
            result = operation.GetContent("RN 7,654,321", format, request);
        }
        else if (operationKind == 2)
        {
            result = operation.DownloadDocuments("RN 7,654,321", format, request);
        }
        else
        {
            result = operation.DownloadBundle("RN 7,654,321", request);
        }

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casedocs/rn7654321/" + routeName));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(mediaType));
            Assert.That(handler.TsdrApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(handler.OdpApiKeys.Single(), Is.Null);
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.MediaType, Is.EqualTo(mediaType));
            Assert.That(result.Content.Format, Is.EqualTo(format));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(body.Length));
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase(1, DocumentFormat.Pdf, UsptoMediaTypes.Zip, "wrong.zip")]
    [TestCase(2, DocumentFormat.Zip, UsptoMediaTypes.Pdf, "wrong.pdf")]
    [TestCase(3, DocumentFormat.Xml, UsptoMediaTypes.TextXml, "wrong.xml")]
    public void EverySingleCaseRouteRejectsMismatchedMedia(int operationKind, DocumentFormat format, string returnedMediaType, string fileName)
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(HttpStatusCode.OK, new byte[] { 1, 2, 3 }, returnedMediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        TrademarkCaseDocumentTransferOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        DownloadResult result;
        if (operationKind == 1)
        {
            result = operation.GetContent("sn90123456", format, request);
        }
        else if (operationKind == 2)
        {
            result = operation.DownloadDocuments("sn90123456", format, request);
        }
        else
        {
            result = operation.DownloadBundle("sn90123456", request);
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(true, DocumentFormat.Xml)]
    [TestCase(true, DocumentFormat.Html)]
    [TestCase(false, DocumentFormat.Json)]
    public void UnsupportedAllDocumentFormatsFailBeforeSending(bool contentRoute, DocumentFormat format)
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(HttpStatusCode.OK, new byte[0], UsptoMediaTypes.Pdf, "case.pdf"));
        using HttpClient client = new HttpClient(handler);
        TrademarkCaseDocumentTransferOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException exception;
        if (contentRoute)
        {
            exception = Assert.Throws<ValidationException>(() => operation.GetContent("sn90123456", format, new DownloadRequest { Output = output }));
        }
        else
        {
            exception = Assert.Throws<ValidationException>(() => operation.DownloadDocuments("sn90123456", format, new DownloadRequest { Output = output }));
        }

        Assert.That(exception!.ErrorCode, Is.EqualTo("UNSUPPORTED_CASE_DOCUMENT_FORMAT"));
        Assert.That(handler.RequestUris, Is.Empty);
    }

    [Test]
    public void InvalidIdentifierNullRequestAndCancellationStopBeforeSending()
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(HttpStatusCode.OK, new byte[0], UsptoMediaTypes.Xml, "bundle.xml"));
        using HttpClient client = new HttpClient(handler);
        TrademarkCaseDocumentTransferOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException invalid = Assert.Throws<ValidationException>(() => operation.DownloadBundle("sn123", new DownloadRequest { Output = output }));
        ValidationException missing = Assert.Throws<ValidationException>(() => operation.DownloadBundle("sn90123456", null!));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => operation.DownloadBundle("sn90123456", new DownloadRequest
        {
            Output = output,
            Context = new RequestContext { CancellationToken = cancellation.Token }
        }));

        Assert.Multiple(() =>
        {
            Assert.That(invalid!.ErrorCode, Is.EqualTo("INVALID_TRADEMARK_SERIAL"));
            Assert.That(missing!.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [TestCase(1, DocumentFormat.Pdf, ApiService.TsdrPdfAndZip)]
    [TestCase(2, DocumentFormat.Zip, ApiService.TsdrPdfAndZip)]
    [TestCase(3, DocumentFormat.Xml, ApiService.Tsdr)]
    public void RemoteFailuresUseTheRepresentationRateClass(int operationKind, DocumentFormat format, ApiService expectedService)
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(HttpStatusCode.ServiceUnavailable, Encoding.UTF8.GetBytes("unavailable"), UsptoMediaTypes.Text, "error.txt"));
        using HttpClient client = new HttpClient(handler);
        TrademarkCaseDocumentTransferOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        ServiceUnavailableException exception;
        if (operationKind == 1)
        {
            exception = Assert.Throws<ServiceUnavailableException>(() => operation.GetContent("sn90123456", format, request));
        }
        else if (operationKind == 2)
        {
            exception = Assert.Throws<ServiceUnavailableException>(() => operation.DownloadDocuments("sn90123456", format, request));
        }
        else
        {
            exception = Assert.Throws<ServiceUnavailableException>(() => operation.DownloadBundle("sn90123456", request));
        }

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Service, Is.EqualTo(expectedService));
            Assert.That(exception.StatusCode, Is.EqualTo(503));
            Assert.That(RetryClassifier.IsTransientFailure(exception, CancellationToken.None), Is.True);
            Assert.That(output.Length, Is.Zero);
        });
    }

    private static TrademarkCaseDocumentTransferOperation Operation(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            TsdrBaseUri = new Uri("https://example.test/root/"),
            TsdrApiKey = "tsdr-secret",
            OdpApiKey = "odp-secret",
            HttpClient = client,
            PermittedDownloadHosts = new[] { "example.test" },
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 4,
                RedirectHopCount = 2
            }
        };
        TrademarkCaseDocumentTransferOperation operation = new TrademarkCaseDocumentTransferOperation(client, options);
        operation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new[] { IPAddress.Parse("8.8.8.8") }));
        return operation;
    }

    private static HttpResponseMessage Response(HttpStatusCode status, byte[] body, string mediaType, string fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        return response;
    }
}

internal class RecordingTrademarkCaseTransferHandler : HttpMessageHandler
{
    internal Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> TsdrApiKeys = new List<string?>();
    internal List<string?> OdpApiKeys = new List<string?>();

    internal RecordingTrademarkCaseTransferHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ResponseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        TsdrApiKeys.Add(Header(request, UsptoHeaders.TsdrApiKey));
        OdpApiKeys.Add(Header(request, UsptoHeaders.OdpApiKey));
        HttpResponseMessage response = ResponseFactory(request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }

    private static string? Header(HttpRequestMessage request, string name)
    {
        if (request.Headers.TryGetValues(name, out IEnumerable<string>? values))
        {
            return values.Single();
        }

        return null;
    }
}
