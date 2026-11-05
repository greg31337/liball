using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentTermAdjustmentOperationTests
{
    [Test]
    public void RetrievalMapsCompleteAdjustmentAndHistory()
    {
        RecordingAdjustmentHandler handler = new RecordingAdjustmentHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentTermAdjustment result = operation.GetTermAdjustment("17/248,024", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/17248024/adjustment"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("17248024"));
            Assert.That(result.AdjustmentTotalDays, Is.EqualTo(304));
            Assert.That(result.HasAdjustmentTotalDays, Is.True);
            Assert.That(result.ADelayDays, Is.EqualTo(304));
            Assert.That(result.HasADelayDays, Is.True);
            Assert.That(result.BDelayDays, Is.Zero);
            Assert.That(result.HasBDelayDays, Is.True);
            Assert.That(result.CDelayDays, Is.EqualTo(2));
            Assert.That(result.HasCDelayDays, Is.True);
            Assert.That(result.ApplicantDelayDays, Is.EqualTo(28));
            Assert.That(result.HasApplicantDelayDays, Is.True);
            Assert.That(result.NonOverlappingDelayDays, Is.EqualTo(320.5));
            Assert.That(result.HasNonOverlappingDelayDays, Is.True);
            Assert.That(result.OverlappingDelayDays, Is.EqualTo(16.25));
            Assert.That(result.HasOverlappingDelayDays, Is.True);
            Assert.That(result.IpOfficeAdjustmentDelayDays, Is.EqualTo(-2.5));
            Assert.That(result.HasIpOfficeAdjustmentDelayDays, Is.True);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.History, Has.Length.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("adjustment-record-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("adjustment-record-request"));
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Is.EqualTo(handler.RequestUris[0].AbsoluteUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["adjustmentFuture"], Is.EqualTo("{ \"revision\": 2 }"));
            Assert.That(result.UnknownFields["futureRecord"], Is.EqualTo("true"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("3"));
        });

        PatentTermAdjustmentHistoryEntry first = result.History[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.EventDate, Is.EqualTo(UtcDate(2023, 5, 9)));
            Assert.That(first.HasEventDate, Is.True);
            Assert.That(first.Description, Is.EqualTo("PTA 36 Months"));
            Assert.That(first.EventSequenceNumber, Is.EqualTo(51.5));
            Assert.That(first.HasEventSequenceNumber, Is.True);
            Assert.That(first.OriginatingEventSequenceNumber, Is.EqualTo(0.5));
            Assert.That(first.HasOriginatingEventSequenceNumber, Is.True);
            Assert.That(first.ApplicantDelayDays, Is.Zero);
            Assert.That(first.HasApplicantDelayDays, Is.True);
            Assert.That(first.IpOfficeDelayDays, Is.EqualTo(304));
            Assert.That(first.HasIpOfficeDelayDays, Is.True);
            Assert.That(first.Code, Is.EqualTo("PTA"));
            Assert.That(first.UnknownFields["historyFuture"], Is.EqualTo("\"kept\""));
        });

        PatentTermAdjustmentHistoryEntry second = result.History[1];
        Assert.Multiple(() =>
        {
            Assert.That(second.EventDate, Is.EqualTo(UtcDate(2023, 4, 1)));
            Assert.That(second.HasEventDate, Is.True);
            Assert.That(second.Description, Is.EqualTo("Export to Final Data Capture"));
            Assert.That(second.EventSequenceNumber, Is.EqualTo(50.0));
            Assert.That(second.HasEventSequenceNumber, Is.True);
            Assert.That(second.HasOriginatingEventSequenceNumber, Is.False);
            Assert.That(second.HasApplicantDelayDays, Is.False);
            Assert.That(second.HasIpOfficeDelayDays, Is.False);
            Assert.That(second.Code, Is.EqualTo("PTE"));
        });
    }

    [Test]
    public void DocumentedOverlapNamesAndNumericStringsAreSupported()
    {
        string response = """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "17/248,024",
                  "patentTermAdjustmentData": {
                    "adjustmentTotalQuantity": "10",
                    "aDelayQuantity": "12",
                    "bDelayQuantity": "3",
                    "cDelayQuantity": "-1",
                    "applicantDayDelayQuantity": "4",
                    "nonOverlappingDayQuantity": 14.75,
                    "overlappingDayQuantity": 0.25,
                    "ipOfficeAdjustmentDayQuantity": 1.5,
                    "patentTermAdjustmentHistoryDataBag": [
                      {
                        "eventDate": "05/09/2023",
                        "eventSequenceNumber": 0.5,
                        "originatingEventSequenceNumber": 0,
                        "applicantDayDelayQuantity": "4",
                        "ipOfficeDayDelayQuantity": "-1",
                        "ptaPteCode": "PTE"
                      }
                    ]
                  }
                }
              ]
            }
            """;

        PatentTermAdjustment result = Parse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.AdjustmentTotalDays, Is.EqualTo(10));
            Assert.That(result.ADelayDays, Is.EqualTo(12));
            Assert.That(result.BDelayDays, Is.EqualTo(3));
            Assert.That(result.CDelayDays, Is.EqualTo(-1));
            Assert.That(result.ApplicantDelayDays, Is.EqualTo(4));
            Assert.That(result.NonOverlappingDelayDays, Is.EqualTo(14.75));
            Assert.That(result.OverlappingDelayDays, Is.EqualTo(0.25));
            Assert.That(result.IpOfficeAdjustmentDelayDays, Is.EqualTo(1.5));
            Assert.That(result.History[0].EventDate, Is.EqualTo(UtcDate(2023, 5, 9)));
            Assert.That(result.History[0].ApplicantDelayDays, Is.EqualTo(4));
            Assert.That(result.History[0].IpOfficeDelayDays, Is.EqualTo(-1));
            Assert.That(result.History[0].Code, Is.EqualTo("PTE"));
        });
    }

    [Test]
    public void CurrentOverlapNamesTakePrecedenceOverDocumentedAliases()
    {
        string response = AdjustmentResponse("\"nonOverlappingDayDelayQuantity\":1.25,\"nonOverlappingDayQuantity\":99,\"overlappingDayDelayQuantity\":2.5,\"overlappingDayQuantity\":88");

        PatentTermAdjustment result = Parse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.NonOverlappingDelayDays, Is.EqualTo(1.25));
            Assert.That(result.OverlappingDelayDays, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeAdjustmentRetrieval()
    {
        RecordingAdjustmentHandler handler = new RecordingAdjustmentHandler(
            HttpStatusCode.OK,
            EmptyResponse(),
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"17/248,024\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentTermAdjustment result = operation.GetTermAdjustment("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/17248024/adjustment"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("17248024"));
        });
    }

    [Test]
    public void AbsentAndNullQuantitiesRetainExplicitMissingState()
    {
        string response = AdjustmentResponse("\"adjustmentTotalQuantity\":null,\"aDelayQuantity\":null,\"patentTermAdjustmentHistoryDataBag\":[{}]");

        PatentTermAdjustment result = Parse(response);
        PatentTermAdjustmentHistoryEntry entry = result.History[0];
        PatentTermAdjustment empty = new PatentTermAdjustment();

        Assert.Multiple(() =>
        {
            Assert.That(result.AdjustmentTotalDays, Is.Zero);
            Assert.That(result.HasAdjustmentTotalDays, Is.False);
            Assert.That(result.HasADelayDays, Is.False);
            Assert.That(result.HasBDelayDays, Is.False);
            Assert.That(result.HasCDelayDays, Is.False);
            Assert.That(result.HasApplicantDelayDays, Is.False);
            Assert.That(result.HasNonOverlappingDelayDays, Is.False);
            Assert.That(result.HasOverlappingDelayDays, Is.False);
            Assert.That(result.HasIpOfficeAdjustmentDelayDays, Is.False);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(entry.HasEventDate, Is.False);
            Assert.That(entry.HasEventSequenceNumber, Is.False);
            Assert.That(entry.HasOriginatingEventSequenceNumber, Is.False);
            Assert.That(entry.HasApplicantDelayDays, Is.False);
            Assert.That(entry.HasIpOfficeDelayDays, Is.False);
            Assert.That(entry.UnknownFields, Is.Empty);
            Assert.That(empty.History, Is.Empty);
            Assert.That(empty.UnknownFields, Is.Empty);
        });
    }

    [Test]
    public void MissingAdjustmentObjectProducesAnEmptyTypedResult()
    {
        PatentTermAdjustment result = Parse(EmptyResponse());

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("17248024"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.History, Is.Empty);
            Assert.That(result.HasAdjustmentTotalDays, Is.False);
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
        });
    }

    [TestCase("{}", "$.count")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[false]}", "$.patentFileWrapperDataBag[0]")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"16/248,024\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"17/248,024\",\"patentTermAdjustmentData\":false}]}", "$.patentFileWrapperDataBag[0].patentTermAdjustmentData")]
    public void MalformedEnvelopeReportsItsExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent-term-adjustment retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"adjustmentTotalQuantity\":{}", "adjustmentTotalQuantity")]
    [TestCase("\"aDelayQuantity\":1.5", "aDelayQuantity")]
    [TestCase("\"bDelayQuantity\":\"not-an-integer\"", "bDelayQuantity")]
    [TestCase("\"cDelayQuantity\":false", "cDelayQuantity")]
    [TestCase("\"applicantDayDelayQuantity\":[]", "applicantDayDelayQuantity")]
    [TestCase("\"nonOverlappingDayDelayQuantity\":\"1.5\"", "nonOverlappingDayDelayQuantity")]
    [TestCase("\"overlappingDayQuantity\":{}", "overlappingDayQuantity")]
    [TestCase("\"ipOfficeAdjustmentDelayQuantity\":1e9999", "ipOfficeAdjustmentDelayQuantity")]
    [TestCase("\"patentTermAdjustmentHistoryDataBag\":{}", "patentTermAdjustmentHistoryDataBag")]
    [TestCase("\"patentTermAdjustmentHistoryDataBag\":[false]", "patentTermAdjustmentHistoryDataBag[0]")]
    public void MalformedAdjustmentValuesReportTheirExactPath(string fragment, string relativePath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(AdjustmentResponse(fragment)));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].patentTermAdjustmentData." + relativePath));
    }

    [TestCase("\"eventDate\":\"never\"", "eventDate")]
    [TestCase("\"eventDescriptionText\":{}", "eventDescriptionText")]
    [TestCase("\"eventSequenceNumber\":\"1.5\"", "eventSequenceNumber")]
    [TestCase("\"eventSequenceNumber\":1e9999", "eventSequenceNumber")]
    [TestCase("\"originatingEventSequenceNumber\":false", "originatingEventSequenceNumber")]
    [TestCase("\"applicantDayDelayQuantity\":1.5", "applicantDayDelayQuantity")]
    [TestCase("\"ipOfficeDayDelayQuantity\":{}", "ipOfficeDayDelayQuantity")]
    [TestCase("\"ptaPTECode\":[]", "ptaPTECode")]
    public void MalformedHistoryValuesReportTheirExactPath(string fragment, string relativePath)
    {
        string response = AdjustmentResponse("\"patentTermAdjustmentHistoryDataBag\":[{" + fragment + "}]");

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].patentTermAdjustmentData.patentTermAdjustmentHistoryDataBag[0]." + relativePath));
    }

    [Test]
    public void RetrievalMapsNotFoundUsingCanonicalApplicationNumber()
    {
        RecordingAdjustmentHandler handler = new RecordingAdjustmentHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-adjustment\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedTermAdjustment("17/248,024", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("17248024"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-adjustment"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CancellationStopsBeforeAdjustmentRetrieval()
    {
        RecordingAdjustmentHandler handler = new RecordingAdjustmentHandler(HttpStatusCode.OK, EmptyResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetTermAdjustment("17/248,024", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentTermAdjustment Parse(string response)
    {
        return OdpPatentTermAdjustmentParser.Parse(response, "17248024", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/17248024/adjustment"),
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

    private static DateTimeOffset UtcDate(int year, int month, int day)
    {
        return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    private static string EmptyResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"17/248,024\"}]}";
    }

    private static string AdjustmentResponse(string fragment)
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"17/248,024\",\"patentTermAdjustmentData\":{" + fragment + "}}]}";
    }

    private static string CompleteResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "17/248,024",
                  "patentTermAdjustmentData": {
                    "aDelayQuantity": 304,
                    "adjustmentTotalQuantity": 304,
                    "applicantDayDelayQuantity": 28,
                    "bDelayQuantity": 0,
                    "cDelayQuantity": 2,
                    "nonOverlappingDayDelayQuantity": 320.5,
                    "nonOverlappingDayQuantity": 999,
                    "overlappingDayDelayQuantity": 16.25,
                    "overlappingDayQuantity": 999,
                    "ipOfficeAdjustmentDelayQuantity": -2.5,
                    "patentTermAdjustmentHistoryDataBag": [
                      {
                        "applicantDayDelayQuantity": 0,
                        "eventDate": "2023-05-09",
                        "eventDescriptionText": "PTA 36 Months",
                        "eventSequenceNumber": 51.5,
                        "ipOfficeDayDelayQuantity": 304,
                        "originatingEventSequenceNumber": 0.5,
                        "ptaPTECode": "PTA",
                        "historyFuture": "kept"
                      },
                      {
                        "eventDate": "2023-04-01",
                        "eventDescriptionText": "Export to Final Data Capture",
                        "eventSequenceNumber": 50,
                        "ptaPteCode": "PTE"
                      }
                    ],
                    "adjustmentFuture": { "revision": 2 }
                  },
                  "requestIdentifier": "adjustment-record-request",
                  "futureRecord": true
                }
              ],
              "requestIdentifier": "adjustment-root-request",
              "futureResponse": 3
            }
            """;
    }
}

internal class RecordingAdjustmentHandler : HttpMessageHandler
{
    internal HttpStatusCode AdjustmentStatusCode;
    internal string AdjustmentResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingAdjustmentHandler(HttpStatusCode adjustmentStatusCode, string adjustmentResponse)
    {
        AdjustmentStatusCode = adjustmentStatusCode;
        AdjustmentResponse = adjustmentResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingAdjustmentHandler(HttpStatusCode adjustmentStatusCode, string adjustmentResponse, string lookupResponse)
    {
        AdjustmentStatusCode = adjustmentStatusCode;
        AdjustmentResponse = adjustmentResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpResponseMessage response = new HttpResponseMessage(lookup ? HttpStatusCode.OK : AdjustmentStatusCode)
        {
            Content = new StringContent(lookup ? LookupResponse : AdjustmentResponse, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
