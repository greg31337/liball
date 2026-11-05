using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentContinuityPriorityOperationTests
{
    [Test]
    public void ContinuityRetrievalMapsParentAndChildRelationships()
    {
        RecordingContinuityPriorityHandler handler = new RecordingContinuityPriorityHandler(HttpStatusCode.OK, CompleteContinuityResponse(), HttpStatusCode.OK, EmptyPriorityResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentContinuity result = operation.GetContinuity("14/412,875", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/continuity"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Parents, Has.Length.EqualTo(1));
            Assert.That(result.Children, Has.Length.EqualTo(1));
            Assert.That(result.RequestIdentifier, Is.EqualTo("continuity-record-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("continuity-record-request"));
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Is.EqualTo(handler.RequestUris[0].AbsoluteUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["futureContinuity"], Is.EqualTo("{ \"revision\": 2 }"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("true"));
        });

        PatentParentContinuity parent = result.Parents[0];
        Assert.Multiple(() =>
        {
            Assert.That(parent.ParentApplicationNumber, Is.EqualTo("12312313"));
            Assert.That(parent.ChildApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(parent.ParentPatentNumber, Is.EqualTo("8968299"));
            Assert.That(parent.RelationshipCode, Is.EqualTo("PRO"));
            Assert.That(parent.RelationshipDescription, Is.EqualTo("Claims priority from a provisional application"));
            Assert.That(parent.ParentApplicationStatusCode, Is.EqualTo(159));
            Assert.That(parent.HasParentApplicationStatusCode, Is.True);
            Assert.That(parent.ParentApplicationStatusDescription, Is.EqualTo("Patent Expired"));
            Assert.That(parent.ParentApplicationFilingDate, Is.EqualTo(new DateTimeOffset(2012, 5, 23, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(parent.HasParentApplicationFilingDate, Is.True);
            Assert.That(parent.IsFirstInventorToFile, Is.True);
            Assert.That(parent.HasIsFirstInventorToFile, Is.True);
            Assert.That(parent.UnknownFields["parentGeneration"], Is.EqualTo("1"));
        });

        PatentChildContinuity child = result.Children[0];
        Assert.Multiple(() =>
        {
            Assert.That(child.ParentApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(child.ChildApplicationNumber, Is.EqualTo("14853719"));
            Assert.That(child.ChildPatentNumber, Is.EqualTo("9704967"));
            Assert.That(child.RelationshipCode, Is.EqualTo("DIV"));
            Assert.That(child.RelationshipDescription, Is.EqualTo("Divisional application"));
            Assert.That(child.ChildApplicationStatusCode, Is.EqualTo(150));
            Assert.That(child.HasChildApplicationStatusCode, Is.True);
            Assert.That(child.ChildApplicationStatusDescription, Is.EqualTo("Patented Case"));
            Assert.That(child.ChildApplicationFilingDate, Is.EqualTo(new DateTimeOffset(2015, 9, 14, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(child.HasChildApplicationFilingDate, Is.True);
            Assert.That(child.IsFirstInventorToFile, Is.False);
            Assert.That(child.HasIsFirstInventorToFile, Is.True);
            Assert.That(child.UnknownFields["childGeneration"], Is.EqualTo("2"));
        });
    }

    [Test]
    public void ForeignPriorityRetrievalMapsDocumentedFieldsAndAliases()
    {
        RecordingContinuityPriorityHandler handler = new RecordingContinuityPriorityHandler(HttpStatusCode.OK, EmptyContinuityResponse(), HttpStatusCode.OK, CompletePriorityResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentForeignPriorityList result = operation.GetForeignPriorities("14/412,875", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/foreign-priority"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Claims, Has.Length.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("priority-request"));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("priority-request"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["priorityRevision"], Is.EqualTo("3"));
        });

        PatentForeignPriorityClaim first = result.Claims[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.OfficeName, Is.EqualTo("FRANCE"));
            Assert.That(first.CountryCode, Is.EqualTo("FR"));
            Assert.That(first.ApplicationNumber, Is.EqualTo("1262321"));
            Assert.That(first.FilingDate, Is.EqualTo(new DateTimeOffset(2012, 12, 19, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(first.HasFilingDate, Is.True);
            Assert.That(first.PrioritySequenceNumber, Is.EqualTo(1));
            Assert.That(first.HasPrioritySequenceNumber, Is.True);
            Assert.That(first.UnknownFields["priorityKind"], Is.EqualTo("\"national\""));
        });

        PatentForeignPriorityClaim second = result.Claims[1];
        Assert.Multiple(() =>
        {
            Assert.That(second.OfficeName, Is.EqualTo("EUROPEAN PATENT OFFICE"));
            Assert.That(second.CountryCode, Is.EqualTo("EP"));
            Assert.That(second.ApplicationNumber, Is.EqualTo("EP14123456"));
            Assert.That(second.FilingDate, Is.EqualTo(new DateTimeOffset(2013, 1, 20, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(second.HasFilingDate, Is.True);
            Assert.That(second.PrioritySequenceNumber, Is.EqualTo(2));
            Assert.That(second.HasPrioritySequenceNumber, Is.True);
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeContinuityRetrieval()
    {
        RecordingContinuityPriorityHandler handler = new RecordingContinuityPriorityHandler(
            HttpStatusCode.OK,
            EmptyContinuityResponse(),
            HttpStatusCode.OK,
            EmptyPriorityResponse(),
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentContinuity result = operation.GetContinuity("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14412875/continuity"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14412875"));
        });
    }

    [Test]
    public void SparseRecordsPreserveAbsentValuesAndModelDefaults()
    {
        PatentContinuity continuity = ParseContinuity("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"parentContinuityBag\":[{}],\"childContinuityBag\":[{}]}]}");
        PatentForeignPriorityList priorities = ParsePriorities("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"foreignPriorityBag\":[{}]}]}");
        PatentParentContinuity parent = continuity.Parents[0];
        PatentChildContinuity child = continuity.Children[0];
        PatentForeignPriorityClaim claim = priorities.Claims[0];

        Assert.Multiple(() =>
        {
            Assert.That(parent.ParentApplicationNumber, Is.Null);
            Assert.That(parent.HasParentApplicationStatusCode, Is.False);
            Assert.That(parent.HasParentApplicationFilingDate, Is.False);
            Assert.That(parent.HasIsFirstInventorToFile, Is.False);
            Assert.That(child.ChildApplicationNumber, Is.Null);
            Assert.That(child.HasChildApplicationStatusCode, Is.False);
            Assert.That(child.HasChildApplicationFilingDate, Is.False);
            Assert.That(child.HasIsFirstInventorToFile, Is.False);
            Assert.That(claim.OfficeName, Is.Null);
            Assert.That(claim.CountryCode, Is.Null);
            Assert.That(claim.ApplicationNumber, Is.Null);
            Assert.That(claim.HasFilingDate, Is.False);
            Assert.That(claim.HasPrioritySequenceNumber, Is.False);
            Assert.That(continuity.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(priorities.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(new PatentContinuity().Parents, Is.Empty);
            Assert.That(new PatentContinuity().Children, Is.Empty);
            Assert.That(new PatentForeignPriorityList().Claims, Is.Empty);
        });
    }

    [TestCase("{}", "$.count")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[false]}", "$.patentFileWrapperDataBag[0]")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/123,456\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"parentContinuityBag\":{}}]}", "$.patentFileWrapperDataBag[0].parentContinuityBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"childContinuityBag\":[false]}]}", "$.patentFileWrapperDataBag[0].childContinuityBag[0]")]
    public void MalformedContinuityContainersReportTheirExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => ParseContinuity(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent continuity retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"parentApplicationNumberText\":\"not-an-application\"", "parentApplicationNumberText")]
    [TestCase("\"parentApplicationStatusCode\":-1", "parentApplicationStatusCode")]
    [TestCase("\"parentApplicationStatusCode\":159.5", "parentApplicationStatusCode")]
    [TestCase("\"parentApplicationFilingDate\":\"yesterday\"", "parentApplicationFilingDate")]
    [TestCase("\"firstInventorToFileIndicator\":\"perhaps\"", "firstInventorToFileIndicator")]
    [TestCase("\"claimParentageTypeCode\":{}", "claimParentageTypeCode")]
    public void MalformedParentValuesReportTheirExactPath(string fragment, string fieldName)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"parentContinuityBag\":[{" + fragment + "}]}]}";

        ParseException exception = Assert.Throws<ParseException>(() => ParseContinuity(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].parentContinuityBag[0]." + fieldName));
    }

    [TestCase("\"childApplicationNumberText\":\"not-an-application\"", "childApplicationNumberText")]
    [TestCase("\"childApplicationStatusCode\":-1", "childApplicationStatusCode")]
    [TestCase("\"childApplicationStatusCode\":150.5", "childApplicationStatusCode")]
    [TestCase("\"childFilingDate\":{}", "childFilingDate")]
    public void MalformedChildValuesReportTheirExactPath(string fragment, string fieldName)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"childContinuityBag\":[{" + fragment + "}]}]}";

        ParseException exception = Assert.Throws<ParseException>(() => ParseContinuity(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].childContinuityBag[0]." + fieldName));
    }

    [TestCase("{}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"foreignPriorityBag\":{}}]}", "$.patentFileWrapperDataBag[0].foreignPriorityBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"foreignPriorityBag\":[false]}]}", "$.patentFileWrapperDataBag[0].foreignPriorityBag[0]")]
    public void MalformedPriorityContainersReportTheirExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => ParsePriorities(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent foreign-priority retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"filingDate\":\"yesterday\"", "filingDate")]
    [TestCase("\"priorityClaimSequenceNumber\":-1", "priorityClaimSequenceNumber")]
    [TestCase("\"priorityClaimSequenceNumber\":1.5", "priorityClaimSequenceNumber")]
    [TestCase("\"applicationNumberText\":{}", "applicationNumberText")]
    [TestCase("\"ipOfficeName\":{}", "ipOfficeName")]
    public void MalformedPriorityValuesReportTheirExactPath(string fragment, string fieldName)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"foreignPriorityBag\":[{" + fragment + "}]}]}";

        ParseException exception = Assert.Throws<ParseException>(() => ParsePriorities(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].foreignPriorityBag[0]." + fieldName));
    }

    [Test]
    public void EndpointFailuresUseTheCanonicalApplicationNumber()
    {
        RecordingContinuityPriorityHandler handler = new RecordingContinuityPriorityHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-continuity\"}",
            HttpStatusCode.Forbidden,
            "{\"message\":\"forbidden\",\"requestIdentifier\":\"priority-denied\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException continuity = Assert.Throws<NotFoundException>(() => operation.GetNormalizedContinuity("14/412,875", new RequestContext()));
        AuthenticationException priority = Assert.Throws<AuthenticationException>(() => operation.GetNormalizedForeignPriorities("14/412,875", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(continuity.ResourceIdentifier, Is.EqualTo("14412875"));
            Assert.That(continuity.RequestIdentifier, Is.EqualTo("missing-continuity"));
            Assert.That(priority.RequestIdentifier, Is.EqualTo("priority-denied"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void CancellationStopsBeforeEitherEndpoint()
    {
        RecordingContinuityPriorityHandler handler = new RecordingContinuityPriorityHandler(HttpStatusCode.OK, EmptyContinuityResponse(), HttpStatusCode.OK, EmptyPriorityResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RequestContext context = new RequestContext { CancellationToken = cancellation.Token };

        Assert.Throws<OperationCanceledException>(() => operation.GetContinuity("14/412,875", context));
        Assert.Throws<OperationCanceledException>(() => operation.GetForeignPriorities("14/412,875", context));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentContinuity ParseContinuity(string response)
    {
        return OdpPatentContinuityPriorityParser.ParseContinuity(response, "14412875", ResponseMetadata("continuity"));
    }

    private static PatentForeignPriorityList ParsePriorities(string response)
    {
        return OdpPatentContinuityPriorityParser.ParseForeignPriorities(response, "14412875", ResponseMetadata("foreign-priority"));
    }

    private static HttpResponseMetadata ResponseMetadata(string endpoint)
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14412875/" + endpoint),
            RequestIdentifier = "header-request",
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        };
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

    private static string EmptyContinuityResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}";
    }

    private static string EmptyPriorityResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\"}]}";
    }

    private static string CompleteContinuityResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "14/412,875",
                  "parentContinuityBag": [
                    {
                      "firstInventorToFileIndicator": "Y",
                      "parentApplicationStatusCode": "159",
                      "parentPatentNumber": "8968299",
                      "parentApplicationStatusDescriptionText": "Patent Expired",
                      "parentApplicationFilingDate": "2012-05-23",
                      "parentApplicationNumberText": "12/312,313",
                      "childApplicationNumberText": "14/412,875",
                      "claimParentageTypeCode": "PRO",
                      "claimParentageTypeCodeDescriptionText": "Claims priority from a provisional application",
                      "parentGeneration": 1
                    }
                  ],
                  "childContinuityBag": [
                    {
                      "childApplicationStatusCode": 150.0,
                      "parentApplicationNumberText": "14/412,875",
                      "childApplicationNumberText": "14/853,719",
                      "childApplicationStatusDescriptionText": "Patented Case",
                      "childFilingDate": "2015-09-14",
                      "firstInventorToFileIndicator": false,
                      "childPatentNumber": "9704967",
                      "claimParentageTypeCode": "DIV",
                      "claimParentageTypeCodeDescriptionText": "Divisional application",
                      "childGeneration": 2
                    }
                  ],
                  "requestIdentifier": "continuity-record-request",
                  "futureContinuity": { "revision": 2 }
                }
              ],
              "requestIdentifier": "continuity-root-request",
              "futureResponse": true
            }
            """;
    }

    private static string CompletePriorityResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "14/412,875",
                  "foreignPriorityBag": [
                    {
                      "ipOfficeName": "FRANCE",
                      "ipOfficeCode": "FR",
                      "filingDate": "2012-12-19",
                      "applicationNumberText": "1262321",
                      "priorityClaimSequenceNumber": "1",
                      "priorityKind": "national"
                    },
                    {
                      "foreignPriorityCountryName": "EUROPEAN PATENT OFFICE",
                      "foreignPriorityCountryCode": "EP",
                      "foreignPriorityDate": "2013-01-20",
                      "foreignApplicationNumberText": "EP14123456",
                      "foreignPrioritySequenceNumber": 2.0
                    }
                  ],
                  "requestIdentifier": "priority-request",
                  "priorityRevision": 3
                }
              ]
            }
            """;
    }
}

internal class RecordingContinuityPriorityHandler : HttpMessageHandler
{
    internal HttpStatusCode ContinuityStatusCode;
    internal string ContinuityResponse;
    internal HttpStatusCode PriorityStatusCode;
    internal string PriorityResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingContinuityPriorityHandler(HttpStatusCode continuityStatusCode, string continuityResponse, HttpStatusCode priorityStatusCode, string priorityResponse)
    {
        ContinuityStatusCode = continuityStatusCode;
        ContinuityResponse = continuityResponse;
        PriorityStatusCode = priorityStatusCode;
        PriorityResponse = priorityResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingContinuityPriorityHandler(HttpStatusCode continuityStatusCode, string continuityResponse, HttpStatusCode priorityStatusCode, string priorityResponse, string lookupResponse)
    {
        ContinuityStatusCode = continuityStatusCode;
        ContinuityResponse = continuityResponse;
        PriorityStatusCode = priorityStatusCode;
        PriorityResponse = priorityResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        bool priority = request.RequestUri.AbsolutePath.EndsWith("/foreign-priority", StringComparison.Ordinal);
        HttpStatusCode statusCode = lookup ? HttpStatusCode.OK : priority ? PriorityStatusCode : ContinuityStatusCode;
        string content = lookup ? LookupResponse : priority ? PriorityResponse : ContinuityResponse;
        HttpResponseMessage response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
