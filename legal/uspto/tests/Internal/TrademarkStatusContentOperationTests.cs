using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TrademarkStatusContentOperationTests
{
    [TestCase(true, DocumentFormat.Html, "content.html", UsptoMediaTypes.Html)]
    [TestCase(true, DocumentFormat.Pdf, "content.pdf", UsptoMediaTypes.Pdf)]
    [TestCase(true, DocumentFormat.Zip, "content.zip", UsptoMediaTypes.Zip)]
    [TestCase(false, DocumentFormat.Pdf, "download.pdf", UsptoMediaTypes.Pdf)]
    [TestCase(false, DocumentFormat.Zip, "download.zip", UsptoMediaTypes.Zip)]
    public void PublishedContentAndDownloadRoutesStreamExactRepresentations(bool contentRoute, DocumentFormat format, string routeName, string mediaType)
    {
        byte[] body = Encoding.UTF8.GetBytes("status-" + routeName);
        RecordingTrademarkContentHandler handler = new RecordingTrademarkContentHandler((request) => Response(HttpStatusCode.OK, body, mediaType, "status." + Extension(format)));
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusContentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        DownloadResult result;
        if (contentRoute)
        {
            result = operation.GetContent("SN 90/123,456", format, request);
        }
        else
        {
            result = operation.Download("SN 90/123,456", format, request);
        }

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casestatus/sn90123456/" + routeName));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(mediaType));
            Assert.That(handler.TsdrApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(handler.OdpApiKeys.Single(), Is.Null);
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.MediaType, Is.EqualTo(mediaType));
            Assert.That(result.Content.Format, Is.EqualTo(format));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(body.Length));
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase(DocumentFormat.Pdf, UsptoMediaTypes.Zip)]
    [TestCase(DocumentFormat.Zip, UsptoMediaTypes.Pdf)]
    [TestCase(DocumentFormat.Html, UsptoMediaTypes.Text)]
    public void StrictMediaValidationRejectsEveryMismatchedRepresentation(DocumentFormat requested, string returnedMediaType)
    {
        RecordingTrademarkContentHandler handler = new RecordingTrademarkContentHandler((request) => Response(HttpStatusCode.OK, new byte[] { 1, 2, 3 }, returnedMediaType, "status." + Extension(requested)));
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusContentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.GetContent("rn7654321", requested, new DownloadRequest { Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Unknown).Or.EqualTo(returnedMediaType == UsptoMediaTypes.Pdf ? DocumentFormat.Pdf : returnedMediaType == UsptoMediaTypes.Zip ? DocumentFormat.Zip : DocumentFormat.Text));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void MatchingMediaWithContradictoryFilenameIsRejected()
    {
        RecordingTrademarkContentHandler handler = new RecordingTrademarkContentHandler((request) => Response(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, "wrong.zip"));
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusContentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("refA0123456", DocumentFormat.Pdf, new DownloadRequest { Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("DOCUMENT_FILENAME_FORMAT_MISMATCH"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(true, DocumentFormat.Json, "UNSUPPORTED_STATUS_CONTENT_FORMAT")]
    [TestCase(false, DocumentFormat.Html, "UNSUPPORTED_STATUS_DOWNLOAD_FORMAT")]
    [TestCase(false, DocumentFormat.Xml, "UNSUPPORTED_STATUS_DOWNLOAD_FORMAT")]
    public void UnsupportedRouteFormatsFailBeforeSending(bool contentRoute, DocumentFormat format, string expectedCode)
    {
        RecordingTrademarkContentHandler handler = new RecordingTrademarkContentHandler((request) => Response(HttpStatusCode.OK, new byte[0], UsptoMediaTypes.Pdf, "status.pdf"));
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusContentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException exception;
        if (contentRoute)
        {
            exception = Assert.Throws<ValidationException>(() => operation.GetContent("sn90123456", format, new DownloadRequest { Output = output }));
        }
        else
        {
            exception = Assert.Throws<ValidationException>(() => operation.Download("sn90123456", format, new DownloadRequest { Output = output }));
        }

        Assert.That(exception!.ErrorCode, Is.EqualTo(expectedCode));
        Assert.That(handler.RequestUris, Is.Empty);
    }

    [Test]
    public void InvalidIdentifierAndCancellationStopBeforeSending()
    {
        RecordingTrademarkContentHandler handler = new RecordingTrademarkContentHandler((request) => Response(HttpStatusCode.OK, new byte[0], UsptoMediaTypes.Pdf, "status.pdf"));
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusContentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        ValidationException invalid = Assert.Throws<ValidationException>(() => operation.GetContent("sn123", DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetContent("sn90123456", DocumentFormat.Pdf, new DownloadRequest
        {
            Output = output,
            Context = new RequestContext { CancellationToken = cancellation.Token }
        }));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.ErrorCode, Is.EqualTo("INVALID_TRADEMARK_SERIAL"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [TestCase(true, DocumentFormat.Html, ApiService.Tsdr)]
    [TestCase(true, DocumentFormat.Pdf, ApiService.TsdrPdfAndZip)]
    [TestCase(false, DocumentFormat.Zip, ApiService.TsdrPdfAndZip)]
    public void RemoteFailuresUseTheRepresentationRateClass(bool contentRoute, DocumentFormat format, ApiService expectedService)
    {
        RecordingTrademarkContentHandler handler = new RecordingTrademarkContentHandler((request) =>
        {
            HttpResponseMessage response = Response((HttpStatusCode)429, Encoding.UTF8.GetBytes("{\"detail\":\"slow down\"}"), UsptoMediaTypes.Json, "error.json");
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RetryAfter, "7");
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        TrademarkStatusContentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        RateLimitException exception;
        if (contentRoute)
        {
            exception = Assert.Throws<RateLimitException>(() => operation.GetContent("sn90123456", format, new DownloadRequest { Output = output }));
        }
        else
        {
            exception = Assert.Throws<RateLimitException>(() => operation.Download("sn90123456", format, new DownloadRequest { Output = output }));
        }

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Service, Is.EqualTo(expectedService));
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(7000L));
            Assert.That(exception.HasRetryAfterMilliseconds, Is.True);
            Assert.That(output.Length, Is.Zero);
        });
    }

    private static TrademarkStatusContentOperation Operation(HttpClient client)
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
                BufferSize = 3,
                RedirectHopCount = 2
            }
        };
        TrademarkStatusContentOperation operation = new TrademarkStatusContentOperation(client, options);
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

    private static string Extension(DocumentFormat format)
    {
        if (format == DocumentFormat.Html)
        {
            return "html";
        }

        if (format == DocumentFormat.Pdf)
        {
            return "pdf";
        }

        return "zip";
    }
}

internal class RecordingTrademarkContentHandler : HttpMessageHandler
{
    internal Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> TsdrApiKeys = new List<string?>();
    internal List<string?> OdpApiKeys = new List<string?>();

    internal RecordingTrademarkContentHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
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
