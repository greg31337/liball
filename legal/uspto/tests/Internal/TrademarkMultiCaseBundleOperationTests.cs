using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TrademarkMultiCaseBundleOperationTests
{
    [TestCase(DocumentFormat.Pdf, "pdf", UsptoMediaTypes.Pdf)]
    [TestCase(DocumentFormat.Xml, "xml", UsptoMediaTypes.Xml)]
    [TestCase(DocumentFormat.Zip, "zip", UsptoMediaTypes.Zip)]
    public void PublishedBundleRoutesGroupNormalizedIdentifiersAndStreamExactFormats(DocumentFormat format, string extension, string mediaType)
    {
        byte[] body = Encoding.UTF8.GetBytes("multi-case-" + extension);
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(body, mediaType, "bundle." + extension));
        using HttpClient client = new HttpClient(handler);
        TrademarkMultiCaseBundleOperation operation = Operation(client, 10, 1000);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        DownloadResult result = operation.Download(new string[] { "SN 90/123,456", "90123457", "RN 123", "REF A0123456" }, format, request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casedocs/bundle." + extension + "?ref=A0123456&rn=0000123&sn=90123456%2C90123457"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(mediaType));
            Assert.That(handler.TsdrApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(handler.OdpApiKeys.Single(), Is.Null);
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.Format, Is.EqualTo(format));
            Assert.That(result.Content.MediaType, Is.EqualTo(mediaType));
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [Test]
    public void IdentifierCountLimitAcceptsExactBoundaryAndRejectsOneMore()
    {
        RecordingTrademarkCaseTransferHandler exactHandler = new RecordingTrademarkCaseTransferHandler((request) => Response(new byte[] { 1 }, UsptoMediaTypes.Pdf, "bundle.pdf"));
        using HttpClient exactClient = new HttpClient(exactHandler);
        TrademarkMultiCaseBundleOperation exactOperation = Operation(exactClient, 2, 1000);
        using MemoryStream exactOutput = new MemoryStream();

        DownloadResult exact = exactOperation.Download(new string[] { "sn90123456", "sn90123457" }, DocumentFormat.Pdf, new DownloadRequest { Output = exactOutput });

        RecordingTrademarkCaseTransferHandler excessiveHandler = new RecordingTrademarkCaseTransferHandler((request) => Response(new byte[] { 1 }, UsptoMediaTypes.Pdf, "bundle.pdf"));
        using HttpClient excessiveClient = new HttpClient(excessiveHandler);
        TrademarkMultiCaseBundleOperation excessiveOperation = Operation(excessiveClient, 1, 1000);
        ValidationException excessive = Assert.Throws<ValidationException>(() => excessiveOperation.Download(new string[] { "sn90123456", "sn90123457" }, DocumentFormat.Pdf, new DownloadRequest { Output = new MemoryStream() }));

        Assert.Multiple(() =>
        {
            Assert.That(exact.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(exactHandler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(excessive!.ErrorCode, Is.EqualTo("TOO_MANY_IDENTIFIERS"));
            Assert.That(excessiveHandler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void EncodedUriLimitAcceptsExactBoundaryAndRejectsOneCharacterLess()
    {
        string expectedUri = "https://example.test/root/ts/cd/casedocs/bundle.xml?sn=90123456";
        RecordingTrademarkCaseTransferHandler exactHandler = new RecordingTrademarkCaseTransferHandler((request) => Response(new byte[] { 1 }, UsptoMediaTypes.Xml, "bundle.xml"));
        using HttpClient exactClient = new HttpClient(exactHandler);
        TrademarkMultiCaseBundleOperation exactOperation = Operation(exactClient, 2, expectedUri.Length);
        using MemoryStream exactOutput = new MemoryStream();

        DownloadResult exact = exactOperation.Download(new string[] { "sn90123456" }, DocumentFormat.Xml, new DownloadRequest { Output = exactOutput });

        RecordingTrademarkCaseTransferHandler excessiveHandler = new RecordingTrademarkCaseTransferHandler((request) => Response(new byte[] { 1 }, UsptoMediaTypes.Xml, "bundle.xml"));
        using HttpClient excessiveClient = new HttpClient(excessiveHandler);
        TrademarkMultiCaseBundleOperation excessiveOperation = Operation(excessiveClient, 2, expectedUri.Length - 1);
        ValidationException excessive = Assert.Throws<ValidationException>(() => excessiveOperation.Download(new string[] { "sn90123456" }, DocumentFormat.Xml, new DownloadRequest { Output = new MemoryStream() }));

        Assert.Multiple(() =>
        {
            Assert.That(exact.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(exactHandler.RequestUris.Single().AbsoluteUri, Is.EqualTo(expectedUri));
            Assert.That(excessive!.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(excessive.FieldName, Is.EqualTo("MaximumUriCharacters"));
            Assert.That(excessiveHandler.RequestUris, Is.Empty);
        });
    }

    [TestCase(DocumentFormat.Pdf, UsptoMediaTypes.Zip, "bundle.zip")]
    [TestCase(DocumentFormat.Xml, UsptoMediaTypes.TextXml, "bundle.xml")]
    [TestCase(DocumentFormat.Zip, UsptoMediaTypes.Pdf, "bundle.pdf")]
    public void MultiCaseBundlesRejectEveryMismatchedRepresentation(DocumentFormat format, string returnedMediaType, string fileName)
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(new byte[] { 1, 2 }, returnedMediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        TrademarkMultiCaseBundleOperation operation = Operation(client, 2, 1000);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new string[] { "sn90123456" }, format, new DownloadRequest { Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(DocumentFormat.Pdf, ApiService.TsdrPdfAndZip)]
    [TestCase(DocumentFormat.Xml, ApiService.Tsdr)]
    [TestCase(DocumentFormat.Zip, ApiService.TsdrPdfAndZip)]
    public void ServerRejectedBundleSizeHasAStableErrorAndCorrectRateClass(DocumentFormat format, ApiService expectedService)
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => ErrorResponse(HttpStatusCode.RequestEntityTooLarge, "<error><detail>bundle exceeds service limit</detail></error>"));
        using HttpClient client = new HttpClient(handler);
        TrademarkMultiCaseBundleOperation operation = Operation(client, 2, 1000);
        using MemoryStream output = new MemoryStream();

        ApiException exception = Assert.Throws<ApiException>(() => operation.Download(new string[] { "sn90123456" }, format, new DownloadRequest { Output = output }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Service, Is.EqualTo(expectedService));
            Assert.That(exception.StatusCode, Is.EqualTo(413));
            Assert.That(exception.ErrorCode, Is.EqualTo("BUNDLE_TOO_LARGE"));
            Assert.That(exception.ErrorDetails, Is.EqualTo("bundle exceeds service limit"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void Bundle429PreservesRetryAfterAndTheDownloadRateClass()
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) =>
        {
            HttpResponseMessage response = ErrorResponse((HttpStatusCode)429, "{\"detail\":\"slow down\"}");
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RetryAfter, "12");
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        TrademarkMultiCaseBundleOperation operation = Operation(client, 2, 1000);
        using MemoryStream output = new MemoryStream();

        RateLimitException exception = Assert.Throws<RateLimitException>(() => operation.Download(new string[] { "sn90123456" }, DocumentFormat.Pdf, new DownloadRequest { Output = output }));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Service, Is.EqualTo(ApiService.TsdrPdfAndZip));
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(12000L));
            Assert.That(exception.HasRetryAfterMilliseconds, Is.True);
            Assert.That(RetryClassifier.IsTransientFailure(exception, CancellationToken.None), Is.True);
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void InvalidIdentifierCollectionsFormatsLimitsAndCancellationStopBeforeSending()
    {
        RecordingTrademarkCaseTransferHandler handler = new RecordingTrademarkCaseTransferHandler((request) => Response(new byte[] { 1 }, UsptoMediaTypes.Pdf, "bundle.pdf"));
        using HttpClient client = new HttpClient(handler);
        TrademarkMultiCaseBundleOperation operation = Operation(client, 3, 1000);
        using MemoryStream output = new MemoryStream();

        ValidationException missing = Assert.Throws<ValidationException>(() => operation.Download(null!, DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        ValidationException empty = Assert.Throws<ValidationException>(() => operation.Download(new string[0], DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        ValidationException invalid = Assert.Throws<ValidationException>(() => operation.Download(new string[] { "sn123" }, DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        ValidationException duplicate = Assert.Throws<ValidationException>(() => operation.Download(new string[] { "SN 90/123,456", "sn90123456" }, DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        ValidationException format = Assert.Throws<ValidationException>(() => operation.Download(new string[] { "sn90123456" }, DocumentFormat.Html, new DownloadRequest { Output = output }));
        operation.Options.Limits.MaximumResults = 0;
        ValidationException countLimit = Assert.Throws<ValidationException>(() => operation.Download(new string[] { "sn90123456" }, DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        operation.Options.Limits.MaximumResults = 3;
        operation.Options.Limits.MaximumUriCharacters = 0;
        ValidationException uriLimit = Assert.Throws<ValidationException>(() => operation.Download(new string[] { "sn90123456" }, DocumentFormat.Pdf, new DownloadRequest { Output = output }));
        operation.Options.Limits.MaximumUriCharacters = 1000;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => operation.Download(new string[] { "sn90123456" }, DocumentFormat.Pdf, new DownloadRequest
        {
            Output = output,
            Context = new RequestContext { CancellationToken = cancellation.Token }
        }));

        Assert.Multiple(() =>
        {
            Assert.That(missing!.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(empty!.ErrorCode, Is.EqualTo("EMPTY_IDENTIFIERS"));
            Assert.That(invalid!.ErrorCode, Is.EqualTo("INVALID_TRADEMARK_SERIAL"));
            Assert.That(duplicate!.ErrorCode, Is.EqualTo("DUPLICATE_IDENTIFIER"));
            Assert.That(format!.ErrorCode, Is.EqualTo("UNSUPPORTED_MULTI_CASE_BUNDLE_FORMAT"));
            Assert.That(countLimit!.ErrorCode, Is.EqualTo("INVALID_LIMIT"));
            Assert.That(countLimit.FieldName, Is.EqualTo("MaximumResults"));
            Assert.That(uriLimit!.ErrorCode, Is.EqualTo("INVALID_LIMIT"));
            Assert.That(uriLimit.FieldName, Is.EqualTo("MaximumUriCharacters"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    private static TrademarkMultiCaseBundleOperation Operation(HttpClient client, int maximumResults, int maximumUriCharacters)
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
            Limits = new UsptoLimits
            {
                MaximumResults = maximumResults,
                MaximumUriCharacters = maximumUriCharacters
            },
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 4,
                RedirectHopCount = 2
            }
        };
        TrademarkMultiCaseBundleOperation operation = new TrademarkMultiCaseBundleOperation(client, options);
        operation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new[] { IPAddress.Parse("8.8.8.8") }));
        return operation;
    }

    private static HttpResponseMessage Response(byte[] body, string mediaType, string fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        return response;
    }

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status, string body)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(body, Encoding.UTF8, UsptoMediaTypes.Xml);
        return response;
    }
}
