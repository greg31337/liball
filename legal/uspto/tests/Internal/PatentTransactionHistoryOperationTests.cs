using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentTransactionHistoryOperationTests
{
    [Test]
    public void RetrievalNormalizesTheApplicationAndMapsCompleteEvents()
    {
        RecordingTransactionHandler handler = new RecordingTransactionHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentTransactionHistory result = operation.GetTransactions("14/412,875", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/transactions"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Events, Has.Length.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("transaction-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Is.EqualTo(handler.RequestUris[0].AbsoluteUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.LastModifiedAt, Is.EqualTo(new DateTimeOffset(2025, 4, 1, 12, 30, 0, TimeSpan.Zero)));
            Assert.That(result.HasLastModifiedAt, Is.True);
            Assert.That(result.Provenance.LastUpdatedAt, Is.EqualTo(result.LastModifiedAt));
            Assert.That(result.Provenance.HasLastUpdatedAt, Is.True);
            Assert.That(result.UnknownSections["futureSection"], Is.EqualTo("{\"revision\":2}"));
        });

        PatentTransaction first = result.Events[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.EventSequenceNumber, Is.EqualTo(3));
            Assert.That(first.HasEventSequenceNumber, Is.True);
            Assert.That(first.EventDate, Is.EqualTo(new DateTimeOffset(2018, 10, 18, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(first.HasEventDate, Is.True);
            Assert.That(first.EventCode, Is.EqualTo("ELC_RVW"));
            Assert.That(first.Description, Is.EqualTo("Electronic Review"));
            Assert.That(first.Source, Is.EqualTo("Patent Center"));
            Assert.That(first.LastModifiedAt, Is.EqualTo(new DateTimeOffset(2025, 3, 5, 8, 45, 0, TimeSpan.Zero)));
            Assert.That(first.HasLastModifiedAt, Is.True);
            Assert.That(first.UnknownFields["futureEvent"], Is.EqualTo("{\"value\":true}"));
        });

        PatentTransaction second = result.Events[1];
        Assert.Multiple(() =>
        {
            Assert.That(second.EventSequenceNumber, Is.EqualTo(4));
            Assert.That(second.HasEventSequenceNumber, Is.True);
            Assert.That(second.EventCode, Is.EqualTo("7"));
            Assert.That(second.Source, Is.EqualTo("Legacy PALM"));
            Assert.That(second.LastModifiedAt, Is.EqualTo(new DateTimeOffset(2025, 3, 6, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(second.HasLastModifiedAt, Is.True);
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeTransactionRetrieval()
    {
        RecordingTransactionHandler handler = new RecordingTransactionHandler(
            HttpStatusCode.OK,
            CompleteResponse(),
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentTransactionHistory result = operation.GetTransactions("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/transactions"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
        });
    }

    [Test]
    public void MissingOptionalEventValuesPreserveTheirAbsentState()
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"eventDataBag\":[{}]}]}";

        PatentTransactionHistory result = Parse(response);
        PatentTransaction transaction = result.Events[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.HasLastModifiedAt, Is.False);
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(transaction.EventSequenceNumber, Is.Zero);
            Assert.That(transaction.HasEventSequenceNumber, Is.False);
            Assert.That(transaction.EventDate, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(transaction.HasEventDate, Is.False);
            Assert.That(transaction.EventCode, Is.Null);
            Assert.That(transaction.Description, Is.Null);
            Assert.That(transaction.Source, Is.Null);
            Assert.That(transaction.HasLastModifiedAt, Is.False);
            Assert.That(transaction.UnknownFields, Is.Empty);
        });
    }

    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/123,456\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"eventDataBag\":{}}]}", "$.patentFileWrapperDataBag[0].eventDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"eventDataBag\":[false]}]}", "$.patentFileWrapperDataBag[0].eventDataBag[0]")]
    public void MalformedResponseContainersReportTheirExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent transaction retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"eventSequenceNumber\":-1", "eventSequenceNumber")]
    [TestCase("\"eventSequenceNumber\":{}", "eventSequenceNumber")]
    [TestCase("\"eventDate\":\"not-a-date\"", "eventDate")]
    [TestCase("\"eventCode\":{}", "eventCode")]
    [TestCase("\"eventSource\":{}", "eventSource")]
    [TestCase("\"eventLastModifiedDateTime\":\"yesterday\"", "eventLastModifiedDateTime")]
    public void MalformedEventValuesReportTheirExactPath(string eventFragment, string fieldName)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"eventDataBag\":[{" + eventFragment + "}]}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].eventDataBag[0]." + fieldName));
    }

    [Test]
    public void InvalidHistoryLastModifiedTimeReportsItsExactPath()
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"lastIngestionDateTime\":\"later\"}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].lastIngestionDateTime"));
    }

    [Test]
    public void RetrievalMapsNotFoundUsingTheCanonicalApplicationNumber()
    {
        RecordingTransactionHandler handler = new RecordingTransactionHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-transaction\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedTransactions("14/412,875", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("14412875"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-transaction"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CancellationStopsBeforeTransactionRetrieval()
    {
        RecordingTransactionHandler handler = new RecordingTransactionHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetTransactions("14/412,875", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentTransactionHistory Parse(string response)
    {
        return OdpPatentTransactionParser.Parse(response, "14412875", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14412875/transactions"),
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

    private static string CompleteResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"eventDataBag\":[{\"eventSequenceNumber\":\"3\",\"eventDate\":\"2018-10-18\",\"eventCode\":\"ELC_RVW\",\"eventDescriptionText\":\"Electronic Review\",\"eventSourceText\":\"Patent Center\",\"eventLastModifiedDateTime\":\"2025-03-05T08:45:00Z\",\"futureEvent\":{\"value\":true}},{\"sequenceNumber\":4,\"eventCode\":7,\"source\":\"Legacy PALM\",\"lastModifiedDate\":\"2025-03-06\"}],\"lastIngestionDateTime\":\"2025-04-01T12:30:00Z\",\"futureSection\":{\"revision\":2}}],\"requestIdentifier\":\"transaction-request\"}";
    }
}

internal class RecordingTransactionHandler : HttpMessageHandler
{
    internal HttpStatusCode TransactionStatusCode;
    internal string TransactionResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingTransactionHandler(HttpStatusCode transactionStatusCode, string transactionResponse)
    {
        TransactionStatusCode = transactionStatusCode;
        TransactionResponse = transactionResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingTransactionHandler(HttpStatusCode transactionStatusCode, string transactionResponse, string lookupResponse)
    {
        TransactionStatusCode = transactionStatusCode;
        TransactionResponse = transactionResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpResponseMessage response = new HttpResponseMessage(lookup ? HttpStatusCode.OK : TransactionStatusCode)
        {
            Content = new StringContent(lookup ? LookupResponse : TransactionResponse, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
