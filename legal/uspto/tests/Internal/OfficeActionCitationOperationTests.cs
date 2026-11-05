using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class OfficeActionCitationOperationTests
{
    [Test]
    public void ParserMapsCitationContextFlagsAndDocumentLinkage()
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
                "id": "citation-2",
                "patentApplicationNumber": 16123456,
                "actionTypeCategory": "rejected",
                "legalSectionCode": 103,
                "referenceIdentifier": "US 2019/0123456 A1, Smith",
                "parsedReferenceIdentifier": "US20190123456A1",
                "paragraphNumber": 42,
                "groupArtUnitNumber": 1713,
                "workGroup": "1710",
                "techCenter": 1700,
                "applicantCitedExaminerReferenceIndicator": "false",
                "examinerCitedReferenceIndicator": 1,
                "officeActionCitationReferenceIndicator": true,
                "createUserIdentifier": "ETL_SYS",
                "createDateTime": "2024-06-07 08:09:10.0",
                "obsoleteDocumentIdentifier": "IFW-9001",
                "confidenceLabel": "reviewed"
              }
            ]
          }
        }
        """;

        OfficeActionCitationResponse response = Parse(json, new SearchRequest { OffsetPage = new OffsetPage { Offset = 1, Limit = 1 } }, "retained");
        OfficeActionCitation citation = response.Citations[0];

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
            Assert.That(response.Provenance.DatasetIdentifier, Is.EqualTo("oa_citations"));
            Assert.That(response.Provenance.DatasetVersion, Is.EqualTo("v2"));
            Assert.That(response.RawResponse, Is.EqualTo("retained"));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.UnknownFields["generation"], Is.EqualTo("5"));
            Assert.That(response.UnknownFields["envelope.envelopeRevision"], Is.EqualTo("2"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(citation.Identifier, Is.EqualTo("citation-2"));
            Assert.That(citation.ApplicationNumber, Is.EqualTo("16123456"));
            Assert.That(citation.ActionTypeCategory, Is.EqualTo("rejected"));
            Assert.That(citation.LegalSectionCode, Is.EqualTo("103"));
            Assert.That(citation.ReferenceIdentifier, Is.EqualTo("US 2019/0123456 A1, Smith"));
            Assert.That(citation.ParsedReferenceIdentifier, Is.EqualTo("US20190123456A1"));
            Assert.That(citation.CitedIdentifier.VerbatimText, Is.EqualTo("US 2019/0123456 A1, Smith"));
            Assert.That(citation.CitedIdentifier.CanonicalPatentIdentifier, Is.EqualTo("US20190123456A1"));
            Assert.That(citation.CitedIdentifier.PatentIdentifierKind, Is.EqualTo(IdentifierKind.PatentPublication));
            Assert.That(citation.CitedIdentifier.IsPatentDocument, Is.True);
            Assert.That(citation.CitedIdentifier.IsNonPatentLiterature, Is.False);
            Assert.That(citation.CitedIdentifier.IsClassified, Is.True);
            Assert.That(citation.ParagraphNumber, Is.EqualTo("42"));
            Assert.That(citation.GroupArtUnitNumber, Is.EqualTo("1713"));
            Assert.That(citation.WorkGroup, Is.EqualTo("1710"));
            Assert.That(citation.TechnologyCenter, Is.EqualTo("1700"));
            Assert.That(citation.ApplicantCitedExaminerReferenceIndicator, Is.False);
            Assert.That(citation.HasApplicantCitedExaminerReferenceIndicator, Is.True);
            Assert.That(citation.ExaminerCitedReferenceIndicator, Is.True);
            Assert.That(citation.HasExaminerCitedReferenceIndicator, Is.True);
            Assert.That(citation.OfficeActionCitationReferenceIndicator, Is.True);
            Assert.That(citation.HasOfficeActionCitationReferenceIndicator, Is.True);
            Assert.That(citation.CreateUserIdentifier, Is.EqualTo("ETL_SYS"));
            Assert.That(citation.CreatedAt, Is.EqualTo(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));
            Assert.That(citation.HasCreatedAt, Is.True);
            Assert.That(citation.ObsoleteDocumentIdentifier, Is.EqualTo("IFW-9001"));
            Assert.That(citation.Provenance.DatasetIdentifier, Is.EqualTo("oa_citations"));
            Assert.That(citation.UnknownFields["confidenceLabel"], Is.EqualTo("\"reviewed\""));
        });
    }

    [Test]
    public void ParserPreservesAbsentIndicatorAndDateState()
    {
        OfficeActionCitationResponse response = Parse(
            "{\"numFound\":1,\"start\":0,\"docs\":[{\"id\":\"sparse\",\"referenceIdentifier\":\"NPL article\",\"examinerCitedReferenceIndicator\":null,\"createDateTime\":null}]}",
            new SearchRequest(),
            null);
        OfficeActionCitation citation = response.Citations[0];

        Assert.Multiple(() =>
        {
            Assert.That(citation.ReferenceIdentifier, Is.EqualTo("NPL article"));
            Assert.That(citation.ParsedReferenceIdentifier, Is.Null);
            Assert.That(citation.CitedIdentifier.VerbatimText, Is.EqualTo("NPL article"));
            Assert.That(citation.CitedIdentifier.CanonicalPatentIdentifier, Is.Null);
            Assert.That(citation.CitedIdentifier.PatentIdentifierKind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(citation.CitedIdentifier.IsPatentDocument, Is.False);
            Assert.That(citation.CitedIdentifier.IsNonPatentLiterature, Is.True);
            Assert.That(citation.CitedIdentifier.IsClassified, Is.True);
            Assert.That(citation.HasApplicantCitedExaminerReferenceIndicator, Is.False);
            Assert.That(citation.HasExaminerCitedReferenceIndicator, Is.False);
            Assert.That(citation.HasOfficeActionCitationReferenceIndicator, Is.False);
            Assert.That(citation.HasCreatedAt, Is.False);
        });
    }

    [TestCase("[]", "$")]
    [TestCase("{\"response\":false}", "$.response")]
    [TestCase("{\"response\":{\"start\":0,\"docs\":[]}}", "$.response.numFound")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":{}}}", "$.response.docs")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[false]}}", "$.response.docs[0]")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"referenceIdentifier\":{}}]}}", "$.response.docs[0].referenceIdentifier")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"examinerCitedReferenceIndicator\":\"perhaps\"}]}}", "$.response.docs[0].examinerCitedReferenceIndicator")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"createDateTime\":\"not-a-date\"}]}}", "$.response.docs[0].createDateTime")]
    public void MalformedCitationResponsesReportExactPaths(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(json, new SearchRequest(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("Office Action citation search"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [Test]
    public void OnePageSearchPostsCitationRouteAndForm()
    {
        string page = Page(1, 0, new string[] { "citation-1" });
        RecordingOfficeActionCitationHandler handler = new RecordingOfficeActionCitationHandler(page);
        handler.RequestIdentifier = "citation-request";
        using HttpClient client = new HttpClient(handler);
        OfficeActionCitationOperation operation = new OfficeActionCitationOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "legalSectionCode:103",
            OffsetPage = new OffsetPage { Offset = 0, Limit = 5 },
            Context = new RequestContext { RetainRawResponse = true }
        };

        OfficeActionCitationResponse response = operation.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/oa/oa_citations/v2/records"));
            Assert.That(handler.Bodies[0], Is.EqualTo("criteria=legalSectionCode%3A103&rows=5&start=0"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json }));
            Assert.That(handler.ContentTypes, Is.EqualTo(new[] { UsptoMediaTypes.FormUrlEncoded }));
            Assert.That(response.RequestIdentifier, Is.EqualTo("citation-request"));
            Assert.That(response.Citations[0].Identifier, Is.EqualTo("citation-1"));
            Assert.That(response.RawResponse, Is.EqualTo(page));
        });
    }

    [Test]
    public void AllPageSearchAggregatesCitationPagesAndHonorsResultLimit()
    {
        RecordingOfficeActionCitationHandler handler = new RecordingOfficeActionCitationHandler(
            Page(5, 0, new string[] { "citation-1", "citation-2" }),
            Page(5, 2, new string[] { "citation-3", "citation-4" }));
        using HttpClient client = new HttpClient(handler);
        OfficeActionCitationOperation operation = new OfficeActionCitationOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 5,
            MaximumResults = 3
        };

        OfficeActionCitationResponse response = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.Citations.Select(citation => citation.Identifier), Is.EqualTo(new[] { "citation-1", "citation-2", "citation-3" }));
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
        RecordingOfficeActionCitationHandler failureHandler = new RecordingOfficeActionCitationHandler("{\"error\":\"missing\"}");
        failureHandler.StatusCode = HttpStatusCode.NotFound;
        using HttpClient failureClient = new HttpClient(failureHandler);
        OfficeActionCitationOperation failureOperation = new OfficeActionCitationOperation(failureClient, CreateOptions(failureClient));
        NotFoundException exception = Assert.Throws<NotFoundException>(() => failureOperation.Search(new SearchRequest()));

        RecordingOfficeActionCitationHandler cancellationHandler = new RecordingOfficeActionCitationHandler(Page(0, 0, new string[0]));
        using HttpClient cancellationClient = new HttpClient(cancellationHandler);
        OfficeActionCitationOperation cancellationOperation = new OfficeActionCitationOperation(cancellationClient, CreateOptions(cancellationClient));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.OfficeAction));
            Assert.Throws<OperationCanceledException>(() => cancellationOperation.Search(new SearchRequest { Context = new RequestContext { CancellationToken = cancellation.Token } }));
            Assert.That(cancellationHandler.Bodies, Is.Empty);
        });
    }

    private static OfficeActionCitationResponse Parse(string json, SearchRequest request, string? rawResponse)
    {
        return OfficeActionCitationParser.Parse(json, request, new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/oa/oa_citations/v2/records"),
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
            builder.Append("\",\"patentApplicationNumber\":\"16123456\",\"legalSectionCode\":\"103\",\"referenceIdentifier\":\"US 2019/0123456 A1\",\"obsoleteDocumentIdentifier\":\"IFW-1\"}");
        }

        builder.Append("]}}");
        return builder.ToString();
    }
}

internal class RecordingOfficeActionCitationHandler : HttpMessageHandler
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

    internal RecordingOfficeActionCitationHandler(params string[] responses)
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
