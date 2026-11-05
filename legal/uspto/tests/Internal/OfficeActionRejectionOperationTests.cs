using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class OfficeActionRejectionOperationTests
{
    [Test]
    public void ParserMapsClaimsFlagsAnomaliesPrecedentAndAllowedState()
    {
        string json = """
        {
          "requestIdentifier": "body-request",
          "envelopeRevision": 2,
          "response": {
            "numFound": 2,
            "start": 1,
            "generation": 5,
            "docs": [
              {
                "id": "rejection-2",
                "patentApplicationNumber": 16123456,
                "legacyDocumentCodeIdentifier": "CTNF",
                "actionTypeCategory": "rejected",
                "legalSectionCode": 103,
                "groupArtUnitNumber": 1713,
                "nationalClass": 435,
                "nationalSubclass": "6.1",
                "paragraphNumber": 42,
                "obsoleteDocumentIdentifier": "IFW-9001",
                "createUserIdentifier": "ETL_SYS",
                "claimNumberArrayDocument": ["1, 2", 3, "5-7"],
                "submissionDate": "20240607",
                "createDateTime": "2024-06-08 09:10:11.0",
                "hasRej101": "false",
                "hasRej102": 0,
                "hasRej103": 1,
                "hasRej112": true,
                "hasRejDP": false,
                "cite103Max": "4",
                "cite103EQ1": 0,
                "cite103GT3": "1",
                "closingMissing": 1,
                "rejectFormMissmatch": "0",
                "formParagraphMissing": 1,
                "headerMissing": "0",
                "bilskiIndicator": 1,
                "mayoIndicator": "false",
                "aliceIndicator": true,
                "myriadIndicator": 0,
                "allowedClaimIndicator": "true",
                "confidenceLabel": "reviewed"
              }
            ]
          }
        }
        """;

        OfficeActionRejectionResponse response = Parse(json, new SearchRequest { OffsetPage = new OffsetPage { Offset = 1, Limit = 1 } }, "retained");
        OfficeActionRejection rejection = response.Rejections[0];

        Assert.Multiple(() =>
        {
            Assert.That(response.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(2));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Start, Is.EqualTo(1));
            Assert.That(response.Page.Offset, Is.EqualTo(1));
            Assert.That(response.Page.Limit, Is.EqualTo(1));
            Assert.That(response.Request.OffsetPage.Offset, Is.EqualTo(1));
            Assert.That(response.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(response.Provenance.DatasetIdentifier, Is.EqualTo("oa_rejections"));
            Assert.That(response.Provenance.DatasetVersion, Is.EqualTo("v2"));
            Assert.That(response.RawResponse, Is.EqualTo("retained"));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.UnknownFields["generation"], Is.EqualTo("5"));
            Assert.That(response.UnknownFields["envelope.envelopeRevision"], Is.EqualTo("2"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(rejection.Identifier, Is.EqualTo("rejection-2"));
            Assert.That(rejection.ApplicationNumber, Is.EqualTo("16123456"));
            Assert.That(rejection.LegacyDocumentCodeIdentifier, Is.EqualTo("CTNF"));
            Assert.That(rejection.ActionTypeCategory, Is.EqualTo("rejected"));
            Assert.That(rejection.LegalSectionCode, Is.EqualTo("103"));
            Assert.That(rejection.GroupArtUnitNumber, Is.EqualTo("1713"));
            Assert.That(rejection.NationalClass, Is.EqualTo("435"));
            Assert.That(rejection.NationalSubclass, Is.EqualTo("6.1"));
            Assert.That(rejection.ParagraphNumber, Is.EqualTo("42"));
            Assert.That(rejection.ObsoleteDocumentIdentifier, Is.EqualTo("IFW-9001"));
            Assert.That(rejection.CreateUserIdentifier, Is.EqualTo("ETL_SYS"));
            Assert.That(rejection.ClaimNumbers, Is.EqualTo(new[] { "1", "2", "3", "5-7" }));
            Assert.That(rejection.SubmissionDate, Is.EqualTo(new DateTimeOffset(2024, 6, 7, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(rejection.HasSubmissionDate, Is.True);
            Assert.That(rejection.CreatedAt, Is.EqualTo(new DateTimeOffset(2024, 6, 8, 9, 10, 11, TimeSpan.Zero)));
            Assert.That(rejection.HasCreatedAt, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(rejection.HasSection101Rejection, Is.False);
            Assert.That(rejection.HasSection101RejectionState, Is.True);
            Assert.That(rejection.HasSection102Rejection, Is.False);
            Assert.That(rejection.HasSection102RejectionState, Is.True);
            Assert.That(rejection.HasSection103Rejection, Is.True);
            Assert.That(rejection.HasSection103RejectionState, Is.True);
            Assert.That(rejection.HasSection112Rejection, Is.True);
            Assert.That(rejection.HasSection112RejectionState, Is.True);
            Assert.That(rejection.HasDoublePatentingRejection, Is.False);
            Assert.That(rejection.HasDoublePatentingRejectionState, Is.True);
            Assert.That(rejection.MaximumSection103CitationCount, Is.EqualTo(4));
            Assert.That(rejection.HasMaximumSection103CitationCount, Is.True);
            Assert.That(rejection.SingleCitationSection103Indicator, Is.Zero);
            Assert.That(rejection.HasSingleCitationSection103Indicator, Is.True);
            Assert.That(rejection.MoreThanThreeCitationsSection103Indicator, Is.EqualTo(1));
            Assert.That(rejection.HasMoreThanThreeCitationsSection103Indicator, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(rejection.ClosingMissingIndicator, Is.EqualTo(1));
            Assert.That(rejection.RejectionFormMismatchIndicator, Is.Zero);
            Assert.That(rejection.FormParagraphMissingIndicator, Is.EqualTo(1));
            Assert.That(rejection.HeaderMissingIndicator, Is.Zero);
            Assert.That(rejection.HasClosingMissingIndicator, Is.True);
            Assert.That(rejection.HasRejectionFormMismatchIndicator, Is.True);
            Assert.That(rejection.HasFormParagraphMissingIndicator, Is.True);
            Assert.That(rejection.HasHeaderMissingIndicator, Is.True);
            Assert.That(rejection.BilskiIndicator, Is.True);
            Assert.That(rejection.HasBilskiIndicator, Is.True);
            Assert.That(rejection.MayoIndicator, Is.False);
            Assert.That(rejection.HasMayoIndicator, Is.True);
            Assert.That(rejection.AliceIndicator, Is.True);
            Assert.That(rejection.HasAliceIndicator, Is.True);
            Assert.That(rejection.MyriadIndicator, Is.False);
            Assert.That(rejection.HasMyriadIndicator, Is.True);
            Assert.That(rejection.AllowedClaimIndicator, Is.True);
            Assert.That(rejection.HasAllowedClaimIndicator, Is.True);
            Assert.That(rejection.Provenance.DatasetIdentifier, Is.EqualTo("oa_rejections"));
            Assert.That(rejection.UnknownFields["confidenceLabel"], Is.EqualTo("\"reviewed\""));
        });
    }

    [Test]
    public void ParserPreservesAbsentAndNullIndicatorState()
    {
        OfficeActionRejectionResponse response = Parse(
            "{\"numFound\":1,\"start\":0,\"docs\":[{\"id\":\"sparse\",\"hasRej101\":null,\"cite103Max\":null,\"allowedClaimIndicator\":null,\"submissionDate\":null}]}",
            new SearchRequest(),
            null);
        OfficeActionRejection rejection = response.Rejections[0];

        Assert.Multiple(() =>
        {
            Assert.That(rejection.ClaimNumbers, Is.Empty);
            Assert.That(rejection.HasSection101RejectionState, Is.False);
            Assert.That(rejection.HasSection102RejectionState, Is.False);
            Assert.That(rejection.HasSection103RejectionState, Is.False);
            Assert.That(rejection.HasSection112RejectionState, Is.False);
            Assert.That(rejection.HasDoublePatentingRejectionState, Is.False);
            Assert.That(rejection.HasMaximumSection103CitationCount, Is.False);
            Assert.That(rejection.HasSingleCitationSection103Indicator, Is.False);
            Assert.That(rejection.HasMoreThanThreeCitationsSection103Indicator, Is.False);
            Assert.That(rejection.HasClosingMissingIndicator, Is.False);
            Assert.That(rejection.HasRejectionFormMismatchIndicator, Is.False);
            Assert.That(rejection.HasFormParagraphMissingIndicator, Is.False);
            Assert.That(rejection.HasHeaderMissingIndicator, Is.False);
            Assert.That(rejection.HasBilskiIndicator, Is.False);
            Assert.That(rejection.HasMayoIndicator, Is.False);
            Assert.That(rejection.HasAliceIndicator, Is.False);
            Assert.That(rejection.HasMyriadIndicator, Is.False);
            Assert.That(rejection.HasAllowedClaimIndicator, Is.False);
            Assert.That(rejection.HasSubmissionDate, Is.False);
            Assert.That(rejection.HasCreatedAt, Is.False);
        });
    }

    [TestCase("[]", "$")]
    [TestCase("{\"response\":false}", "$.response")]
    [TestCase("{\"response\":{\"start\":0,\"docs\":[]}}", "$.response.numFound")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":{}}}", "$.response.docs")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[false]}}", "$.response.docs[0]")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"patentApplicationNumber\":{}}]}}", "$.response.docs[0].patentApplicationNumber")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"claimNumberArrayDocument\":[\"1\",false]}]}}", "$.response.docs[0].claimNumberArrayDocument[1]")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"hasRej103\":\"perhaps\"}]}}", "$.response.docs[0].hasRej103")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"closingMissing\":1.5}]}}", "$.response.docs[0].closingMissing")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"submissionDate\":\"not-a-date\"}]}}", "$.response.docs[0].submissionDate")]
    public void MalformedRejectionResponsesReportExactPaths(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(json, new SearchRequest(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("Office Action rejection search"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [Test]
    public void OnePageSearchPostsRejectionRouteAndForm()
    {
        string page = Page(1, 0, new string[] { "rejection-1" });
        RecordingOfficeActionRejectionHandler handler = new RecordingOfficeActionRejectionHandler(page);
        handler.RequestIdentifier = "rejection-request";
        using HttpClient client = new HttpClient(handler);
        OfficeActionRejectionOperation operation = new OfficeActionRejectionOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "hasRej103:1",
            OffsetPage = new OffsetPage { Offset = 0, Limit = 5 },
            Context = new RequestContext { RetainRawResponse = true }
        };

        OfficeActionRejectionResponse response = operation.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/oa/oa_rejections/v2/records"));
            Assert.That(handler.Bodies[0], Is.EqualTo("criteria=hasRej103%3A1&rows=5&start=0"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json }));
            Assert.That(handler.ContentTypes, Is.EqualTo(new[] { UsptoMediaTypes.FormUrlEncoded }));
            Assert.That(response.RequestIdentifier, Is.EqualTo("rejection-request"));
            Assert.That(response.Rejections[0].Identifier, Is.EqualTo("rejection-1"));
            Assert.That(response.RawResponse, Is.EqualTo(page));
        });
    }

    [Test]
    public void AllPageSearchAggregatesRejectionPagesAndHonorsResultLimit()
    {
        RecordingOfficeActionRejectionHandler handler = new RecordingOfficeActionRejectionHandler(
            Page(5, 0, new string[] { "rejection-1", "rejection-2" }),
            Page(5, 2, new string[] { "rejection-3", "rejection-4" }));
        using HttpClient client = new HttpClient(handler);
        OfficeActionRejectionOperation operation = new OfficeActionRejectionOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 5,
            MaximumResults = 3
        };

        OfficeActionRejectionResponse response = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.Rejections.Select(rejection => rejection.Identifier), Is.EqualTo(new[] { "rejection-1", "rejection-2", "rejection-3" }));
            Assert.That(response.Count, Is.EqualTo(3));
            Assert.That(response.Total, Is.EqualTo(5));
            Assert.That(response.IsComplete, Is.False);
            Assert.That(handler.Starts, Is.EqualTo(new long[] { 0, 2 }));
            Assert.That(handler.Rows, Is.EqualTo(new int[] { 2, 2 }));
        });
    }

    [Test]
    public void RemoteFailureAndCancellationUseSharedPolicies()
    {
        RecordingOfficeActionRejectionHandler failureHandler = new RecordingOfficeActionRejectionHandler("{\"error\":\"missing\"}");
        failureHandler.StatusCode = HttpStatusCode.NotFound;
        using HttpClient failureClient = new HttpClient(failureHandler);
        OfficeActionRejectionOperation failureOperation = new OfficeActionRejectionOperation(failureClient, CreateOptions(failureClient));
        NotFoundException exception = Assert.Throws<NotFoundException>(() => failureOperation.Search(new SearchRequest()));

        RecordingOfficeActionRejectionHandler cancellationHandler = new RecordingOfficeActionRejectionHandler(Page(0, 0, new string[0]));
        using HttpClient cancellationClient = new HttpClient(cancellationHandler);
        OfficeActionRejectionOperation cancellationOperation = new OfficeActionRejectionOperation(cancellationClient, CreateOptions(cancellationClient));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.OfficeAction));
            Assert.Throws<OperationCanceledException>(() => cancellationOperation.Search(new SearchRequest { Context = new RequestContext { CancellationToken = cancellation.Token } }));
            Assert.That(cancellationHandler.Bodies, Is.Empty);
        });
    }

    private static OfficeActionRejectionResponse Parse(string json, SearchRequest request, string? rawResponse)
    {
        return OfficeActionRejectionParser.Parse(json, request, new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/oa/oa_rejections/v2/records"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        }, rawResponse);
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        return new UsptoClientOptions
        {
            OfficeActionBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string Page(long total, long start, string[] identifiers)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"response\":{\"numFound\":");
        builder.Append(total);
        builder.Append(",\"start\":");
        builder.Append(start);
        builder.Append(",\"docs\":[");
        for (int index = 0; index < identifiers.Length; index += 1)
        {
            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"id\":\"");
            builder.Append(identifiers[index]);
            builder.Append("\",\"patentApplicationNumber\":\"16123456\",\"hasRej103\":true,\"claimNumberArrayDocument\":[\"1,2\"],\"obsoleteDocumentIdentifier\":\"IFW-1\"}");
        }

        builder.Append("]}}");
        return builder.ToString();
    }
}

internal class RecordingOfficeActionRejectionHandler : HttpMessageHandler
{
    internal Queue<string> Responses = new Queue<string>();
    internal HttpStatusCode StatusCode = HttpStatusCode.OK;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string> Bodies = new List<string>();
    internal List<long> Starts = new List<long>();
    internal List<int> Rows = new List<int>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> ContentTypes = new List<string?>();
    internal string? RequestIdentifier;

    internal RecordingOfficeActionRejectionHandler(params string[] responses)
    {
        foreach (string response in responses)
        {
            Responses.Enqueue(response);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);
        string body = await request.Content!.ReadAsStringAsync(cancellationToken);
        Bodies.Add(body);
        Dictionary<string, string> form = ReadForm(body);
        Starts.Add(long.Parse(form["start"]));
        Rows.Add(int.Parse(form["rows"]));
        string responseBody = Responses.Count == 0 ? PageFallback() : Responses.Dequeue();
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifier);
        }

        return response;
    }

    private static Dictionary<string, string> ReadForm(string body)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            string value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static string PageFallback()
    {
        return "{\"response\":{\"numFound\":0,\"start\":0,\"docs\":[]}}";
    }
}
