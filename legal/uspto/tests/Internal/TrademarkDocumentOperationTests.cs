using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TrademarkDocumentOperationTests
{
    [Test]
    public void DocumentListUsesPublishedXmlRouteAndParsesTypedMetadata()
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => XmlResponse(DocumentListXml()));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);

        TrademarkDocumentCollection result = operation.GetDocuments("SN 90/123,456", new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casedocs/sn90123456/info"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(handler.TsdrApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(result.RequestedIdentifier, Is.EqualTo("SN 90/123,456"));
            Assert.That(result.CanonicalIdentifier, Is.EqualTo("sn90123456"));
            Assert.That(result.IdentifierKind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(result.Documents, Has.Length.EqualTo(2));
            Assert.That(result.Documents[0].DocumentIdentifier, Is.EqualTo("NOA20070926100613"));
            Assert.That(result.Documents[0].SerialReferenceNumber, Is.EqualTo("90123456"));
            Assert.That(result.Documents[0].Type.Name, Is.EqualTo("NOA"));
            Assert.That(result.Documents[0].Type.CategoryDescription, Is.EqualTo("Outgoing correspondence"));
            Assert.That(result.Documents[0].Type.UnknownFields["futureType"], Is.EqualTo("typed"));
            Assert.That(result.Documents[0].MailDate, Is.EqualTo(new DateTimeOffset(2007, 9, 26, 10, 6, 13, TimeSpan.Zero)));
            Assert.That(result.Documents[0].HasMailDate, Is.True);
            Assert.That(result.Documents[0].ScanDateText, Is.EqualTo("scan pending"));
            Assert.That(result.Documents[0].HasScanDate, Is.False);
            Assert.That(result.Documents[0].BatchIdentifier, Is.EqualTo("batch-7"));
            Assert.That(result.Documents[0].UrlPaths, Is.EqualTo(new[] { "/first", "/second" }));
            Assert.That(result.Documents[0].Pages.Single().PageNumber, Is.EqualTo(1));
            Assert.That(result.Documents[0].Pages.Single().HashDigestIdentifier, Is.EqualTo("sha256:abc"));
            Assert.That(result.Documents[0].Pages.Single().CmsUri?.Query, Does.Contain("%5BREDACTED%5D"));
            Assert.That(result.Documents[0].Pages.Single().UnknownFields["futurePage"], Is.EqualTo("page value"));
            Assert.That(result.Documents[0].UnknownFields["futureDocument"], Is.EqualTo("document value"));
            Assert.That(result.Documents[0].Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Documents[0].Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("document-request"));
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.RawResponse, Does.Contain("[REDACTED]"));
            Assert.That(result.RawResponse, Does.Not.Contain("tsdr-secret"));
            Assert.That(result.UnknownFields["futureEnvelope"], Is.EqualTo("ready"));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void DocumentInfoEscapesOpaqueIdentifierAndParsesNamespacedEnvelope()
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => XmlResponse(SingleDocumentXml()));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);

        TrademarkDocument result = operation.GetDocument("RN 7,654,321", " DOC 1/2 ", new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casedoc/rn7654321/DOC%201%2F2/info"));
            Assert.That(result.RequestedCaseIdentifier, Is.EqualTo("RN 7,654,321"));
            Assert.That(result.CanonicalCaseIdentifier, Is.EqualTo("rn7654321"));
            Assert.That(result.DocumentIdentifier, Is.EqualTo("DOC 1/2"));
            Assert.That(result.DocumentTypeName, Is.EqualTo("Registration certificate"));
            Assert.That(result.Timestamp, Is.EqualTo(new DateTimeOffset(2025, 2, 3, 4, 5, 6, TimeSpan.Zero)));
            Assert.That(result.HasTimestamp, Is.True);
            Assert.That(result.Pages.Single().PageNumber, Is.Zero);
            Assert.That(result.Pages.Single().HasPageNumber, Is.False);
            Assert.That(result.Pages.Single().ScanDateText, Is.EqualTo("unknown"));
            Assert.That(result.Pages.Single().HasScanDate, Is.False);
            Assert.That(result.RawResponse, Does.Contain("Registration certificate"));
            Assert.That(result.Provenance.SourceUri?.AbsolutePath, Is.EqualTo("/root/ts/cd/casedoc/rn7654321/DOC%201%2F2/info"));
        });
    }

    [TestCase(true, DocumentFormat.Pdf, "content.pdf", UsptoMediaTypes.Pdf)]
    [TestCase(true, DocumentFormat.Zip, "content.zip", UsptoMediaTypes.Zip)]
    [TestCase(false, DocumentFormat.Pdf, "download.pdf", UsptoMediaTypes.Pdf)]
    [TestCase(false, DocumentFormat.Zip, "download.zip", UsptoMediaTypes.Zip)]
    public void IndividualContentAndDownloadRoutesStreamStrictRepresentations(bool contentRoute, DocumentFormat format, string routeName, string mediaType)
    {
        byte[] body = Encoding.UTF8.GetBytes("document-" + routeName);
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => BinaryResponse(body, mediaType, "NOA2007." + Extension(format)));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        DownloadResult result;
        if (contentRoute)
        {
            result = operation.GetContent("sn90123456", "NOA2007", format, request);
        }
        else
        {
            result = operation.Download("sn90123456", "NOA2007", format, request);
        }

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casedoc/sn90123456/NOA2007/" + routeName));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(mediaType));
            Assert.That(handler.TsdrApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(handler.OdpApiKeys.Single(), Is.Null);
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.Format, Is.EqualTo(format));
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [Test]
    public void IndividualContentRejectsMismatchedMediaBeforeWriting()
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => BinaryResponse(new byte[] { 1, 2, 3 }, UsptoMediaTypes.Zip, "NOA2007.zip"));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.GetContent("sn90123456", "NOA2007", DocumentFormat.Pdf, new DownloadRequest { Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(UsptoMediaTypes.Tiff, "page.tiff")]
    [TestCase(UsptoMediaTypes.Jpeg, "page.jpeg")]
    [TestCase(UsptoMediaTypes.Png, "page.png")]
    public void PageMediaRouteStreamsRecognizedNativeRasterContent(string mediaType, string fileName)
    {
        byte[] body = new byte[] { 10, 20, 30, 40 };
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => PageResponse(body, mediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest { Output = output };

        DownloadResult result = operation.DownloadPageMedia("REF A0123456", " PAGE DOC/7 ", 12, request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/casedoc/refA0123456/PAGE%20DOC%2F7/12/media"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo("*/*"));
            Assert.That(handler.TsdrApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(handler.OdpApiKeys.Single(), Is.Null);
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.MediaType, Is.EqualTo(mediaType));
            Assert.That(result.Content.FileName, Is.EqualTo(fileName));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Unknown));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(body.Length));
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase(null, "page.tif")]
    [TestCase(UsptoMediaTypes.Pdf, "page.pdf")]
    [TestCase("image/gif", "page.gif")]
    public void PageMediaRejectsMissingOrUnsupportedMediaTypes(string? mediaType, string fileName)
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => PageResponse(new byte[] { 1, 2 }, mediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.DownloadPageMedia("sn90123456", "DOC", 1, new DownloadRequest { Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(UsptoMediaTypes.Tiff, "page.jpg")]
    [TestCase(UsptoMediaTypes.Jpeg, "page.png")]
    [TestCase(UsptoMediaTypes.Png, "page.pdf")]
    public void PageMediaRejectsContradictoryFileNameMetadata(string mediaType, string fileName)
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => PageResponse(new byte[] { 1, 2 }, mediaType, fileName));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.DownloadPageMedia("sn90123456", "DOC", 1, new DownloadRequest { Output = output });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("PAGE_MEDIA_FILENAME_MISMATCH"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void InvalidPageNumbersFailBeforeSending(int pageNumber)
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => PageResponse(new byte[0], UsptoMediaTypes.Png, "page.png"));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.DownloadPageMedia("sn90123456", "DOC", pageNumber, new DownloadRequest { Output = output }));

        Assert.That(exception!.ErrorCode, Is.EqualTo("INVALID_PAGE_NUMBER"));
        Assert.That(handler.RequestUris, Is.Empty);
    }

    [Test]
    public void InvalidRequestsAndCancellationStopBeforeSending()
    {
        RecordingTrademarkDocumentHandler handler = new RecordingTrademarkDocumentHandler((request) => XmlResponse("<Documents />"));
        using HttpClient client = new HttpClient(handler);
        TrademarkDocumentOperation operation = Operation(client);
        using MemoryStream output = new MemoryStream();

        ValidationException invalidCase = Assert.Throws<ValidationException>(() => operation.GetDocuments("sn123", new RequestContext()));
        ValidationException missingDocument = Assert.Throws<ValidationException>(() => operation.GetDocument("sn90123456", " ", new RequestContext()));
        ValidationException invalidFormat = Assert.Throws<ValidationException>(() => operation.GetContent("sn90123456", "DOC", DocumentFormat.Html, new DownloadRequest { Output = output }));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => operation.GetDocument("sn90123456", "DOC", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(invalidCase!.ErrorCode, Is.EqualTo("INVALID_TRADEMARK_SERIAL"));
            Assert.That(missingDocument!.ErrorCode, Is.EqualTo("EMPTY_DOCUMENT_IDENTIFIER"));
            Assert.That(invalidFormat!.ErrorCode, Is.EqualTo("UNSUPPORTED_DOCUMENT_FORMAT"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void MetadataMediaAndRemoteFailuresUseSharedPolicies()
    {
        RecordingTrademarkDocumentHandler mediaHandler = new RecordingTrademarkDocumentHandler((request) => BinaryResponse(Encoding.UTF8.GetBytes("{}"), UsptoMediaTypes.Json, "error.json"));
        using HttpClient mediaClient = new HttpClient(mediaHandler);
        TrademarkDocumentOperation mediaOperation = Operation(mediaClient);
        ApiException media = Assert.Throws<ApiException>(() => mediaOperation.GetDocuments("sn90123456", new RequestContext()));

        RecordingTrademarkDocumentHandler missingHandler = new RecordingTrademarkDocumentHandler((request) => StatusResponse(HttpStatusCode.NotFound, "<error>missing</error>"));
        using HttpClient missingClient = new HttpClient(missingHandler);
        TrademarkDocumentOperation missingOperation = Operation(missingClient);
        NotFoundException missing = Assert.Throws<NotFoundException>(() => missingOperation.GetDocument("sn90123456", "DOC", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(media!.ErrorCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(media.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(missing!.StatusCode, Is.EqualTo(404));
            Assert.That(missing.Service, Is.EqualTo(ApiService.Tsdr));
        });
    }

    [Test]
    public void DocumentAndPageTransferFailuresUseTheirDistinctRateClasses()
    {
        RecordingTrademarkDocumentHandler documentHandler = new RecordingTrademarkDocumentHandler((request) => StatusResponse(HttpStatusCode.ServiceUnavailable, "unavailable"));
        using HttpClient documentClient = new HttpClient(documentHandler);
        TrademarkDocumentOperation documentOperation = Operation(documentClient);
        using MemoryStream documentOutput = new MemoryStream();
        ServiceUnavailableException documentFailure = Assert.Throws<ServiceUnavailableException>(() => documentOperation.Download(
            "sn90123456",
            "DOC",
            DocumentFormat.Pdf,
            new DownloadRequest { Output = documentOutput }));

        RecordingTrademarkDocumentHandler pageHandler = new RecordingTrademarkDocumentHandler((request) => StatusResponse(HttpStatusCode.ServiceUnavailable, "unavailable"));
        using HttpClient pageClient = new HttpClient(pageHandler);
        TrademarkDocumentOperation pageOperation = Operation(pageClient);
        using MemoryStream pageOutput = new MemoryStream();
        ServiceUnavailableException pageFailure = Assert.Throws<ServiceUnavailableException>(() => pageOperation.DownloadPageMedia(
            "sn90123456",
            "DOC",
            1,
            new DownloadRequest { Output = pageOutput }));

        Assert.Multiple(() =>
        {
            Assert.That(documentFailure!.Service, Is.EqualTo(ApiService.TsdrPdfAndZip));
            Assert.That(documentFailure.StatusCode, Is.EqualTo(503));
            Assert.That(pageFailure!.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(pageFailure.StatusCode, Is.EqualTo(503));
            Assert.That(documentOutput.Length, Is.Zero);
            Assert.That(pageOutput.Length, Is.Zero);
        });
    }

    private static TrademarkDocumentOperation Operation(HttpClient client)
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
            OverallTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 3,
                RedirectHopCount = 2
            }
        };
        TrademarkDocumentOperation operation = new TrademarkDocumentOperation(client, options);
        operation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new[] { IPAddress.Parse("8.8.8.8") }));
        return operation;
    }

    private static HttpResponseMessage XmlResponse(string xml)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(xml, Encoding.UTF8, UsptoMediaTypes.Xml);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "document-request");
        return response;
    }

    private static HttpResponseMessage BinaryResponse(byte[] body, string mediaType, string fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        return response;
    }

    private static HttpResponseMessage PageResponse(byte[] body, string? mediaType, string? fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        if (mediaType != null)
        {
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        if (fileName != null)
        {
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        }

        return response;
    }

    private static HttpResponseMessage StatusResponse(HttpStatusCode status, string body)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(body, Encoding.UTF8, UsptoMediaTypes.Xml);
        return response;
    }

    private static string Extension(DocumentFormat format)
    {
        return format == DocumentFormat.Pdf ? "pdf" : "zip";
    }

    private static string DocumentListXml()
    {
        return """
            <td:Documents xmlns:td="urn:tsdr">
              <td:futureEnvelope>ready</td:futureEnvelope>
              <td:Document>
                <td:documentIdentifier>NOA20070926100613</td:documentIdentifier>
                <td:serialReferenceNumber>90123456</td:serialReferenceNumber>
                <td:type>
                  <td:name>NOA</td:name>
                  <td:description>Notice of allowance</td:description>
                  <td:categoryName>Outgoing</td:categoryName>
                  <td:categoryDescription>Outgoing correspondence</td:categoryDescription>
                  <td:extendedDescription>Notice issued by the office</td:extendedDescription>
                  <td:futureType>typed</td:futureType>
                </td:type>
                <td:mailDate>2007-09-26T10:06:13Z</td:mailDate>
                <td:scanDate>scan pending</td:scanDate>
                <td:batchId>batch-7</td:batchId>
                <td:source>TSDR</td:source>
                <td:pages>
                  <td:page>
                    <td:pageNumber>1</td:pageNumber>
                    <td:location>page-1.tif</td:location>
                    <td:hashDigestId>sha256:abc</td:hashDigestId>
                    <td:serialNumber>90123456</td:serialNumber>
                    <td:scanDateTime>2007-09-26T10:07:00Z</td:scanDateTime>
                    <td:docTypeName>NOA</td:docTypeName>
                    <td:cmsUrl>https://cms.example.test/page/1?api_key=tsdr-secret</td:cmsUrl>
                    <td:futurePage>page value</td:futurePage>
                  </td:page>
                </td:pages>
                <td:docTypeName>Notice of allowance</td:docTypeName>
                <td:urlPathList><td:urlPath>/first</td:urlPath><td:urlPath>/second</td:urlPath></td:urlPathList>
                <td:futureDocument>document value</td:futureDocument>
              </td:Document>
              <td:documents><td:documentIdentifier>REG20250203040506</td:documentIdentifier></td:documents>
              <!-- tsdr-secret -->
            </td:Documents>
            """;
    }

    private static string SingleDocumentXml()
    {
        return """
            <td:Response xmlns:td="urn:tsdr">
              <td:Document>
                <td:documentIdentifier>ignored-service-id</td:documentIdentifier>
                <td:timestamp>2025-02-03T04:05:06Z</td:timestamp>
                <td:docTypeName>Registration certificate</td:docTypeName>
                <td:pages><td:page><td:pageNumber>many</td:pageNumber><td:scanDateTime>unknown</td:scanDateTime></td:page></td:pages>
              </td:Document>
            </td:Response>
            """;
    }
}

internal class RecordingTrademarkDocumentHandler : HttpMessageHandler
{
    internal Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> TsdrApiKeys = new List<string?>();
    internal List<string?> OdpApiKeys = new List<string?>();

    internal RecordingTrademarkDocumentHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
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
