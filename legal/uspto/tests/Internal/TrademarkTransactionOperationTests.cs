using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TrademarkTransactionOperationTests
{
    [Test]
    public void JsonLastUpdateUsesPublishedRouteAndParsesEveryTimestampSection()
    {
        RecordingTrademarkTransactionHandler handler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.OK, JsonLastUpdate(), UsptoMediaTypes.Json));
        using HttpClient client = new HttpClient(handler);
        TrademarkTransactionOperation operation = Operation(client, 10, 1000);

        TrademarkLastUpdateResult result = operation.GetLastUpdate("SN 90/123,456", DocumentFormat.Json, new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/last-update/info.json?sn=90123456"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(result.RequestedIdentifier, Is.EqualTo("SN 90/123,456"));
            Assert.That(result.CanonicalIdentifier, Is.EqualTo("sn90123456"));
            Assert.That(result.Updates.Single().Name, Is.EqualTo("serialNumber"));
            Assert.That(result.Updates.Single().Value, Is.EqualTo("90123456"));
            Assert.That(result.Updates.Single().LastModifiedDate, Is.EqualTo(new DateTimeOffset(2025, 3, 4, 5, 6, 7, TimeSpan.Zero)));
            Assert.That(result.Updates.Single().HasLastModifiedDate, Is.True);
            Assert.That(result.Updates.Single().StatusLastModifiedDateText, Is.EqualTo("status pending"));
            Assert.That(result.Updates.Single().HasStatusLastModifiedDate, Is.False);
            Assert.That(result.Updates.Single().ProsecutionLastModifiedDate, Is.EqualTo(new DateTimeOffset(2025, 3, 3, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.Updates.Single().DocumentsLastModifiedDate, Is.EqualTo(new DateTimeOffset(2025, 3, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.Updates.Single().UnknownFields["futureEntry"], Is.EqualTo("true"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("7"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("transaction-request"));
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.RawResponse, Does.Contain("caseUpdateInfo"));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void XmlLastUpdateUsesNamespacedParsingAndRetainsMalformedDates()
    {
        RecordingTrademarkTransactionHandler handler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.OK, XmlLastUpdate(), UsptoMediaTypes.Xml));
        using HttpClient client = new HttpClient(handler);
        TrademarkTransactionOperation operation = Operation(client, 10, 1000);

        TrademarkLastUpdateResult result = operation.GetLastUpdate("sn90123456", DocumentFormat.Xml, new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/last-update/info.xml?sn=90123456"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(result.Updates, Has.Length.EqualTo(1));
            Assert.That(result.Updates[0].LastModifiedDateText, Is.EqualTo("not recorded"));
            Assert.That(result.Updates[0].HasLastModifiedDate, Is.False);
            Assert.That(result.Updates[0].StatusLastModifiedDate, Is.EqualTo(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.Updates[0].HasStatusLastModifiedDate, Is.True);
            Assert.That(result.Updates[0].ProsecutionLastModifiedDateText, Is.EqualTo("unknown"));
            Assert.That(result.Updates[0].HasProsecutionLastModifiedDate, Is.False);
            Assert.That(result.Updates[0].DocumentsLastModifiedDate, Is.EqualTo(new DateTimeOffset(2025, 1, 4, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.Updates[0].UnknownFields["futureEntry"], Is.EqualTo("xml future"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("ready"));
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void MultiStatusUsesHomogeneousRouteAndParsesMissedAndOversizedMetadata()
    {
        RecordingTrademarkTransactionHandler handler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.OK, MultiStatus(), UsptoMediaTypes.Json));
        using HttpClient client = new HttpClient(handler);
        TrademarkTransactionOperation operation = Operation(client, 3, 1000);

        TrademarkMultiStatusResult result = operation.GetMultiStatus(new string[] { "SN 90/123,456", "90123457", "sn90123458" }, new RequestContext { RetainRawResponse = true });

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/caseMultiStatus/sn?ids=90123456%2C90123457%2C90123458"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("tsdr-secret"));
            Assert.That(result.RequestedIdentifiers, Is.EqualTo(new[] { "SN 90/123,456", "90123457", "sn90123458" }));
            Assert.That(result.CanonicalIdentifiers, Is.EqualTo(new[] { "sn90123456", "sn90123457", "sn90123458" }));
            Assert.That(result.IdentifierKind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(result.Cases, Has.Length.EqualTo(2));
            Assert.That(result.Cases[0].RequestedIdentifier, Is.EqualTo("SN 90/123,456"));
            Assert.That(result.Cases[0].SerialNumber, Is.EqualTo("90123456"));
            Assert.That(result.Cases[0].Mark.VerbalElementText, Is.EqualTo("FIRST MARK"));
            Assert.That(result.Cases[1].RequestedIdentifier, Is.EqualTo("sn90123458"));
            Assert.That(result.Cases[1].SerialNumber, Is.EqualTo("90123458"));
            Assert.That(result.MissedIdentifiers, Is.EqualTo(new[] { "90123457" }));
            Assert.That(result.IsOversized, Is.True);
            Assert.That(result.HasIsOversized, Is.True);
            Assert.That(result.Size, Is.EqualTo(3));
            Assert.That(result.HasSize, Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Cases[0].Provenance.DatasetIdentifier, Is.EqualTo("TSDR multi-case status"));
            Assert.That(result.RawResponse, Does.Contain("transactionList"));
            Assert.That(result.UnknownFields["futureTransaction"], Is.EqualTo("true"));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [TestCase("RN 123", "rn", "0000123", TrademarkIdentifierKind.RegistrationNumber)]
    [TestCase("REF A0123456", "ref", "A0123456", TrademarkIdentifierKind.ReferenceNumber)]
    public void MultiStatusDerivesRegistrationAndReferenceRoutes(string identifier, string routeType, string queryValue, TrademarkIdentifierKind kind)
    {
        RecordingTrademarkTransactionHandler handler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.OK, "{\"transactionList\":[],\"oversized\":false,\"missedElements\":[],\"size\":0}", UsptoMediaTypes.Json));
        using HttpClient client = new HttpClient(handler);
        TrademarkTransactionOperation operation = Operation(client, 2, 1000);

        TrademarkMultiStatusResult result = operation.GetMultiStatus(new string[] { identifier }, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris.Single().AbsoluteUri, Is.EqualTo("https://example.test/root/ts/cd/caseMultiStatus/" + routeType + "?ids=" + queryValue));
            Assert.That(result.IdentifierKind, Is.EqualTo(kind));
        });
    }

    [Test]
    public void InvalidRequestsAndConfiguredBoundsFailBeforeSending()
    {
        RecordingTrademarkTransactionHandler handler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.OK, "{}", UsptoMediaTypes.Json));
        using HttpClient client = new HttpClient(handler);
        TrademarkTransactionOperation operation = Operation(client, 1, 1000);

        ValidationException lastUpdateKind = Assert.Throws<ValidationException>(() => operation.GetLastUpdate("rn123", DocumentFormat.Json, new RequestContext()));
        ValidationException lastUpdateFormat = Assert.Throws<ValidationException>(() => operation.GetLastUpdate("sn90123456", DocumentFormat.Html, new RequestContext()));
        ValidationException empty = Assert.Throws<ValidationException>(() => operation.GetMultiStatus(new string[0], new RequestContext()));
        ValidationException excessive = Assert.Throws<ValidationException>(() => operation.GetMultiStatus(new string[] { "sn90123456", "sn90123457" }, new RequestContext()));
        operation.Options.Limits.MaximumResults = 3;
        ValidationException mixed = Assert.Throws<ValidationException>(() => operation.GetMultiStatus(new string[] { "sn90123456", "rn123" }, new RequestContext()));
        ValidationException duplicate = Assert.Throws<ValidationException>(() => operation.GetMultiStatus(new string[] { "SN 90/123,456", "sn90123456" }, new RequestContext()));
        operation.Options.Limits.MaximumUriCharacters = 20;
        ValidationException uri = Assert.Throws<ValidationException>(() => operation.GetMultiStatus(new string[] { "sn90123456" }, new RequestContext()));
        operation.Options.Limits.MaximumUriCharacters = 1000;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => operation.GetMultiStatus(new string[] { "sn90123456" }, new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(lastUpdateKind!.ErrorCode, Is.EqualTo("LAST_UPDATE_REQUIRES_SERIAL_NUMBER"));
            Assert.That(lastUpdateFormat!.ErrorCode, Is.EqualTo("UNSUPPORTED_FORMAT"));
            Assert.That(empty!.ErrorCode, Is.EqualTo("EMPTY_IDENTIFIERS"));
            Assert.That(excessive!.ErrorCode, Is.EqualTo("TOO_MANY_IDENTIFIERS"));
            Assert.That(mixed!.ErrorCode, Is.EqualTo("MIXED_IDENTIFIER_KINDS"));
            Assert.That(duplicate!.ErrorCode, Is.EqualTo("DUPLICATE_IDENTIFIER"));
            Assert.That(uri!.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void MediaAndRemoteFailuresUseSharedResponsePolicies()
    {
        RecordingTrademarkTransactionHandler mediaHandler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.OK, "{}", UsptoMediaTypes.Xml));
        using HttpClient mediaClient = new HttpClient(mediaHandler);
        TrademarkTransactionOperation mediaOperation = Operation(mediaClient, 2, 1000);
        ApiException media = Assert.Throws<ApiException>(() => mediaOperation.GetMultiStatus(new string[] { "sn90123456" }, new RequestContext()));

        RecordingTrademarkTransactionHandler missingHandler = new RecordingTrademarkTransactionHandler((request) => Response(HttpStatusCode.NotFound, "{\"error\":\"missing\"}", UsptoMediaTypes.Json));
        using HttpClient missingClient = new HttpClient(missingHandler);
        TrademarkTransactionOperation missingOperation = Operation(missingClient, 2, 1000);
        NotFoundException missing = Assert.Throws<NotFoundException>(() => missingOperation.GetLastUpdate("sn90123456", DocumentFormat.Json, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(media!.ErrorCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(media.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(missing!.StatusCode, Is.EqualTo(404));
            Assert.That(missing.Service, Is.EqualTo(ApiService.Tsdr));
        });
    }

    private static TrademarkTransactionOperation Operation(HttpClient client, int maximumResults, int maximumUriCharacters)
    {
        return new TrademarkTransactionOperation(client, new UsptoClientOptions
        {
            TsdrBaseUri = new Uri("https://example.test/root/"),
            TsdrApiKey = "tsdr-secret",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000,
            Limits = new UsptoLimits
            {
                MaximumResults = maximumResults,
                MaximumUriCharacters = maximumUriCharacters
            }
        });
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(body, Encoding.UTF8, mediaType);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "transaction-request");
        return response;
    }

    private static string JsonLastUpdate()
    {
        return "{\"caseUpdateInfo\":[{\"name\":\"serialNumber\",\"value\":\"90123456\",\"caseData\":{\"lastModifiedDate\":\"2025-03-04T05:06:07Z\",\"caseStatusData\":{\"lastModifiedDate\":\"status pending\"},\"caseProsecutionData\":{\"lastModifiedDate\":\"2025-03-03\"},\"caseDocsData\":{\"lastModifiedDate\":\"2025-03-02\"}},\"futureEntry\":true}],\"futureResponse\":7}";
    }

    private static string XmlLastUpdate()
    {
        return """
            <td:CaseUpdateInfoList xmlns:td="urn:tsdr">
              <td:caseUpdateInfo>
                <td:name>serialNumber</td:name>
                <td:value>90123456</td:value>
                <td:caseData>
                  <td:lastModifiedDate>not recorded</td:lastModifiedDate>
                  <td:caseStatusData><td:lastModifiedDate>2025-01-02</td:lastModifiedDate></td:caseStatusData>
                  <td:caseProsecutionData><td:lastModifiedDate>unknown</td:lastModifiedDate></td:caseProsecutionData>
                  <td:caseDocsData><td:lastModifiedDate>2025-01-04</td:lastModifiedDate></td:caseDocsData>
                </td:caseData>
                <td:futureEntry>xml future</td:futureEntry>
              </td:caseUpdateInfo>
              <td:futureResponse>ready</td:futureResponse>
            </td:CaseUpdateInfoList>
            """;
    }

    private static string MultiStatus()
    {
        return "{\"transactionList\":[{\"trademarks\":[{\"status\":{\"serialNumber\":\"90123456\",\"markElement\":\"FIRST MARK\",\"status\":700}}],\"searchId\":\"first\"},{\"trademarks\":[{\"status\":{\"serialNumber\":\"90123458\",\"markElement\":\"THIRD MARK\",\"status\":600}}],\"searchId\":\"third\"}],\"oversized\":true,\"missedElements\":[\"90123457\"],\"size\":\"3\",\"futureTransaction\":true}";
    }
}

internal class RecordingTrademarkTransactionHandler : HttpMessageHandler
{
    internal Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> ApiKeys = new List<string?>();

    internal RecordingTrademarkTransactionHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ResponseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        if (request.Headers.TryGetValues(UsptoHeaders.TsdrApiKey, out IEnumerable<string>? values))
        {
            ApiKeys.Add(values.Single());
        }
        else
        {
            ApiKeys.Add(null);
        }

        HttpResponseMessage response = ResponseFactory(request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
