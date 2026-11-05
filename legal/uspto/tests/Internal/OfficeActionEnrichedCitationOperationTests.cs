using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class OfficeActionEnrichedCitationOperationTests
{
    [Test]
    public void ParserMapsPatentClaimsCategoriesPassagesQualityAndArtUnitData()
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
                "id": "enriched-2",
                "patentApplicationNumber": 16123456,
                "citedDocumentIdentifier": "US 2019/0123456 A1, Smith",
                "publicationNumber": 20190123456,
                "kindCode": "A1",
                "countryCode": "US",
                "inventorNameText": "SMITH, JANE",
                "officeActionDate": "20240607",
                "officeActionCategory": "CTNF",
                "citationCategoryCode": "X",
                "relatedClaimNumberText": "1, 2,5-7",
                "examinerCitedReferenceIndicator": 1,
                "applicantCitedExaminerReferenceIndicator": "false",
                "nplIndicator": true,
                "workGroupNumber": 1710,
                "groupArtUnitNumber": "1713",
                "techCenter": 1700,
                "qualitySummaryText": "Human reviewed",
                "passageLocationText": ["paragraph 42", 17, "figure 3|column 4"],
                "obsoleteDocumentIdentifier": "IFW-9001",
                "createUserIdentifier": "ETL_SYS",
                "createDateTime": "2024-06-08 09:10:11.0",
                "confidenceScore": 0.98
              }
            ]
          }
        }
        """;

        OfficeActionEnrichedCitationResponse response = Parse(json, new SearchRequest { OffsetPage = new OffsetPage { Offset = 1, Limit = 1 } }, "retained");
        OfficeActionEnrichedCitation citation = response.Citations[0];

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
            Assert.That(response.Provenance.DatasetIdentifier, Is.EqualTo("enriched_cited_reference_metadata"));
            Assert.That(response.Provenance.DatasetVersion, Is.EqualTo("v3"));
            Assert.That(response.RawResponse, Is.EqualTo("retained"));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.UnknownFields["generation"], Is.EqualTo("5"));
            Assert.That(response.UnknownFields["envelope.envelopeRevision"], Is.EqualTo("2"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(citation.Identifier, Is.EqualTo("enriched-2"));
            Assert.That(citation.ApplicationNumber, Is.EqualTo("16123456"));
            Assert.That(citation.CitedDocumentIdentifier, Is.EqualTo("US 2019/0123456 A1, Smith"));
            Assert.That(citation.CitedIdentifier.VerbatimText, Is.EqualTo("US 2019/0123456 A1, Smith"));
            Assert.That(citation.CitedIdentifier.CanonicalPatentIdentifier, Is.Null);
            Assert.That(citation.CitedIdentifier.PatentIdentifierKind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(citation.CitedIdentifier.IsPatentDocument, Is.False);
            Assert.That(citation.CitedIdentifier.IsNonPatentLiterature, Is.True);
            Assert.That(citation.CitedIdentifier.IsClassified, Is.True);
            Assert.That(citation.PublicationNumber, Is.EqualTo("20190123456"));
            Assert.That(citation.KindCode, Is.EqualTo("A1"));
            Assert.That(citation.CountryCode, Is.EqualTo("US"));
            Assert.That(citation.InventorNameText, Is.EqualTo("SMITH, JANE"));
            Assert.That(citation.OfficeActionDate, Is.EqualTo(new DateTimeOffset(2024, 6, 7, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(citation.HasOfficeActionDate, Is.True);
            Assert.That(citation.OfficeActionCategory, Is.EqualTo("CTNF"));
            Assert.That(citation.CitationCategoryCode, Is.EqualTo("X"));
            Assert.That(citation.RelatedClaimNumberText, Is.EqualTo("1, 2,5-7"));
            Assert.That(citation.RelatedClaimNumbers, Is.EqualTo(new[] { "1", "2", "5-7" }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(citation.ExaminerCitedReferenceIndicator, Is.True);
            Assert.That(citation.HasExaminerCitedReferenceIndicator, Is.True);
            Assert.That(citation.ApplicantCitedExaminerReferenceIndicator, Is.False);
            Assert.That(citation.HasApplicantCitedExaminerReferenceIndicator, Is.True);
            Assert.That(citation.NonPatentLiteratureIndicator, Is.True);
            Assert.That(citation.HasNonPatentLiteratureIndicator, Is.True);
            Assert.That(citation.WorkGroupNumber, Is.EqualTo("1710"));
            Assert.That(citation.GroupArtUnitNumber, Is.EqualTo("1713"));
            Assert.That(citation.TechnologyCenter, Is.EqualTo("1700"));
            Assert.That(citation.QualitySummaryText, Is.EqualTo("Human reviewed"));
            Assert.That(citation.PassageLocations, Is.EqualTo(new[] { "paragraph 42", "17", "figure 3|column 4" }));
            Assert.That(citation.ObsoleteDocumentIdentifier, Is.EqualTo("IFW-9001"));
            Assert.That(citation.CreateUserIdentifier, Is.EqualTo("ETL_SYS"));
            Assert.That(citation.CreatedAt, Is.EqualTo(new DateTimeOffset(2024, 6, 8, 9, 10, 11, TimeSpan.Zero)));
            Assert.That(citation.HasCreatedAt, Is.True);
            Assert.That(citation.Provenance.DatasetIdentifier, Is.EqualTo("enriched_cited_reference_metadata"));
            Assert.That(citation.UnknownFields["confidenceScore"], Is.EqualTo("0.98"));
        });
    }

    [Test]
    public void ParserNormalizesStructuredPatentIdentityWithoutChangingSourceCitation()
    {
        OfficeActionEnrichedCitationResponse response = Parse(
            "{\"numFound\":1,\"start\":0,\"docs\":[{\"citedDocumentIdentifier\":\"US 2024/0123456 A1, Smith et al.\",\"publicationNumber\":\"20240123456\",\"kindCode\":\"A1\",\"countryCode\":\"US\",\"nplIndicator\":false}]}",
            new SearchRequest(),
            null);
        OfficeActionEnrichedCitation citation = response.Citations[0];

        Assert.Multiple(() =>
        {
            Assert.That(citation.CitedDocumentIdentifier, Is.EqualTo("US 2024/0123456 A1, Smith et al."));
            Assert.That(citation.CitedIdentifier.VerbatimText, Is.EqualTo("US 2024/0123456 A1, Smith et al."));
            Assert.That(citation.CitedIdentifier.CanonicalPatentIdentifier, Is.EqualTo("US20240123456A1"));
            Assert.That(citation.CitedIdentifier.PatentIdentifierKind, Is.EqualTo(IdentifierKind.PatentPublication));
            Assert.That(citation.CitedIdentifier.IsPatentDocument, Is.True);
            Assert.That(citation.CitedIdentifier.IsNonPatentLiterature, Is.False);
            Assert.That(citation.CitedIdentifier.IsClassified, Is.True);
        });
    }

    [Test]
    public void ParserPreservesAbsentAndNullIndicatorDateAndCollectionState()
    {
        OfficeActionEnrichedCitationResponse response = Parse(
            "{\"numFound\":1,\"start\":0,\"docs\":[{\"id\":\"sparse\",\"relatedClaimNumberText\":null,\"passageLocationText\":null,\"examinerCitedReferenceIndicator\":null,\"nplIndicator\":null,\"officeActionDate\":null}]}",
            new SearchRequest(),
            null);
        OfficeActionEnrichedCitation citation = response.Citations[0];

        Assert.Multiple(() =>
        {
            Assert.That(citation.RelatedClaimNumberText, Is.Null);
            Assert.That(citation.RelatedClaimNumbers, Is.Empty);
            Assert.That(citation.PassageLocations, Is.Empty);
            Assert.That(citation.CitedIdentifier.IsClassified, Is.False);
            Assert.That(citation.HasExaminerCitedReferenceIndicator, Is.False);
            Assert.That(citation.HasApplicantCitedExaminerReferenceIndicator, Is.False);
            Assert.That(citation.HasNonPatentLiteratureIndicator, Is.False);
            Assert.That(citation.HasOfficeActionDate, Is.False);
            Assert.That(citation.HasCreatedAt, Is.False);
        });
    }

    [TestCase("[]", "$")]
    [TestCase("{\"response\":false}", "$.response")]
    [TestCase("{\"response\":{\"start\":0,\"docs\":[]}}", "$.response.numFound")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":{}}}", "$.response.docs")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[false]}}", "$.response.docs[0]")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"publicationNumber\":{}}]}}", "$.response.docs[0].publicationNumber")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"passageLocationText\":[\"paragraph 1\",{}]}]}}", "$.response.docs[0].passageLocationText[1]")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"examinerCitedReferenceIndicator\":\"perhaps\"}]}}", "$.response.docs[0].examinerCitedReferenceIndicator")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"officeActionDate\":\"not-a-date\"}]}}", "$.response.docs[0].officeActionDate")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"createDateTime\":false}]}}", "$.response.docs[0].createDateTime")]
    public void MalformedEnrichedCitationResponsesReportExactPaths(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(json, new SearchRequest(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("enriched Office Action citation search"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [Test]
    public void OnePageSearchPostsEnrichedCitationRouteAndForm()
    {
        string page = Page(1, 0, new string[] { "enriched-1" });
        RecordingOfficeActionEnrichedCitationHandler handler = new RecordingOfficeActionEnrichedCitationHandler(page);
        handler.RequestIdentifier = "enriched-request";
        using HttpClient client = new HttpClient(handler);
        OfficeActionEnrichedCitationOperation operation = new OfficeActionEnrichedCitationOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "citationCategoryCode:X",
            OffsetPage = new OffsetPage { Offset = 0, Limit = 5 },
            Context = new RequestContext { RetainRawResponse = true }
        };

        OfficeActionEnrichedCitationResponse response = operation.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/oa/enriched_cited_reference_metadata/v3/records"));
            Assert.That(handler.Bodies[0], Is.EqualTo("criteria=citationCategoryCode%3AX&rows=5&start=0"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json }));
            Assert.That(handler.ContentTypes, Is.EqualTo(new[] { UsptoMediaTypes.FormUrlEncoded }));
            Assert.That(response.RequestIdentifier, Is.EqualTo("enriched-request"));
            Assert.That(response.Citations[0].Identifier, Is.EqualTo("enriched-1"));
            Assert.That(response.RawResponse, Is.EqualTo(page));
        });
    }

    [Test]
    public void AllPageSearchAggregatesEnrichedCitationPagesAndHonorsResultLimit()
    {
        RecordingOfficeActionEnrichedCitationHandler handler = new RecordingOfficeActionEnrichedCitationHandler(
            Page(5, 0, new string[] { "enriched-1", "enriched-2" }),
            Page(5, 2, new string[] { "enriched-3", "enriched-4" }));
        using HttpClient client = new HttpClient(handler);
        OfficeActionEnrichedCitationOperation operation = new OfficeActionEnrichedCitationOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 5,
            MaximumResults = 3
        };

        OfficeActionEnrichedCitationResponse response = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.Citations.Select(citation => citation.Identifier), Is.EqualTo(new[] { "enriched-1", "enriched-2", "enriched-3" }));
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
        RecordingOfficeActionEnrichedCitationHandler failureHandler = new RecordingOfficeActionEnrichedCitationHandler("{\"error\":\"missing\"}");
        failureHandler.StatusCode = HttpStatusCode.NotFound;
        using HttpClient failureClient = new HttpClient(failureHandler);
        OfficeActionEnrichedCitationOperation failureOperation = new OfficeActionEnrichedCitationOperation(failureClient, CreateOptions(failureClient));
        NotFoundException exception = Assert.Throws<NotFoundException>(() => failureOperation.Search(new SearchRequest()));

        RecordingOfficeActionEnrichedCitationHandler cancellationHandler = new RecordingOfficeActionEnrichedCitationHandler(Page(0, 0, new string[0]));
        using HttpClient cancellationClient = new HttpClient(cancellationHandler);
        OfficeActionEnrichedCitationOperation cancellationOperation = new OfficeActionEnrichedCitationOperation(cancellationClient, CreateOptions(cancellationClient));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.OfficeAction));
            Assert.Throws<OperationCanceledException>(() => cancellationOperation.Search(new SearchRequest { Context = new RequestContext { CancellationToken = cancellation.Token } }));
            Assert.That(cancellationHandler.Bodies, Is.Empty);
        });
    }

    private static OfficeActionEnrichedCitationResponse Parse(string json, SearchRequest request, string? rawResponse)
    {
        return OfficeActionEnrichedCitationParser.Parse(json, request, new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/oa/enriched_cited_reference_metadata/v3/records"),
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
            builder.Append("\",\"patentApplicationNumber\":\"16123456\",\"publicationNumber\":\"20190123456\",\"kindCode\":\"A1\",\"countryCode\":\"US\"}");
        }

        builder.Append("]}}");
        return builder.ToString();
    }
}

internal class RecordingOfficeActionEnrichedCitationHandler : HttpMessageHandler
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

    internal RecordingOfficeActionEnrichedCitationHandler(params string[] responses)
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
