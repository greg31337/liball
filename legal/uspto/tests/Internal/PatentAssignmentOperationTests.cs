using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentAssignmentOperationTests
{
    [Test]
    public void RetrievalMapsCompleteLiveStyleAssignmentData()
    {
        RecordingAssignmentHandler handler = new RecordingAssignmentHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentAssignmentHistory result = operation.GetAssignments("15/000,001", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/15000001/assignment"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("15000001"));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Assignments, Has.Length.EqualTo(1));
            Assert.That(result.RequestIdentifier, Is.EqualTo("assignment-record-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("assignment-record-request"));
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Is.EqualTo(handler.RequestUris[0].AbsoluteUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["futureRecord"], Is.EqualTo("2"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("true"));
        });

        PatentAssignment assignment = result.Assignments[0];
        Assert.Multiple(() =>
        {
            Assert.That(assignment.ReelNumber, Is.EqualTo("38323"));
            Assert.That(assignment.FrameNumber, Is.EqualTo("190"));
            Assert.That(assignment.ReelAndFrameNumber, Is.EqualTo("038323/0190"));
            Assert.That(assignment.PageCount, Is.EqualTo(8));
            Assert.That(assignment.HasPageCount, Is.True);
            Assert.That(assignment.IsImageAvailable, Is.True);
            Assert.That(assignment.HasIsImageAvailable, Is.True);
            Assert.That(assignment.DocumentUri, Is.EqualTo(new Uri("https://assignmentcenter.uspto.gov/ipas/search/api/v2/public/download/patent/38323/190")));
            Assert.That(assignment.ReceivedDate, Is.EqualTo(UtcDate(2016, 4, 19)));
            Assert.That(assignment.HasReceivedDate, Is.True);
            Assert.That(assignment.RecordedDate, Is.EqualTo(UtcDate(2016, 4, 19)));
            Assert.That(assignment.HasRecordedDate, Is.True);
            Assert.That(assignment.MailedDate, Is.EqualTo(UtcDate(2016, 4, 20)));
            Assert.That(assignment.HasMailedDate, Is.True);
            Assert.That(assignment.Conveyance, Is.EqualTo("ASSIGNMENT OF ASSIGNOR'S INTEREST"));
            Assert.That(assignment.AttorneyDocketNumber, Is.EqualTo("SAM-53161"));
            Assert.That(assignment.Assignors, Has.Length.EqualTo(2));
            Assert.That(assignment.Assignees, Has.Length.EqualTo(1));
            Assert.That(assignment.Correspondents, Has.Length.EqualTo(1));
            Assert.That(assignment.DomesticRepresentative, Is.Not.Null);
            Assert.That(assignment.Properties, Has.Length.EqualTo(2));
            Assert.That(assignment.UnknownFields["assignmentVersion"], Is.EqualTo("4"));
        });

        PatentAssignmentAssignor assignor = assignment.Assignors[0];
        Assert.Multiple(() =>
        {
            Assert.That(assignor.Name, Is.EqualTo("HEO, JIN-PIL"));
            Assert.That(assignor.ExecutionDate, Is.EqualTo(UtcDate(2016, 1, 5)));
            Assert.That(assignor.HasExecutionDate, Is.True);
            Assert.That(assignor.UnknownFields["assignorSequence"], Is.EqualTo("1"));
            Assert.That(assignment.Assignors[1].Name, Is.EqualTo("YI, IN-SUN"));
            Assert.That(assignment.Assignors[1].HasExecutionDate, Is.False);
        });

        PatentAssignmentAssignee assignee = assignment.Assignees[0];
        Assert.Multiple(() =>
        {
            Assert.That(assignee.Name, Is.EqualTo("SAMSUNG ELECTRONICS CO., LTD"));
            Assert.That(assignee.Address.AddressLineOne, Is.EqualTo("129, SAMSUNG-RO"));
            Assert.That(assignee.Address.AddressLineTwo, Is.EqualTo("YEONGTONG-GU"));
            Assert.That(assignee.Address.City, Is.EqualTo("SUWON-SI"));
            Assert.That(assignee.Address.GeographicRegionName, Is.EqualTo("GYEONGGI-DO"));
            Assert.That(assignee.Address.GeographicRegionCode, Is.EqualTo("KRX"));
            Assert.That(assignee.Address.PostalCode, Is.EqualTo("16677"));
            Assert.That(assignee.Address.CountryCode, Is.EqualTo("KR"));
            Assert.That(assignee.Address.CountryName, Is.EqualTo("KOREA, REPUBLIC OF"));
            Assert.That(assignee.UnknownFields["ownershipPercent"], Is.EqualTo("100"));
        });

        PatentAssignmentCorrespondent correspondent = assignment.Correspondents[0];
        Assert.Multiple(() =>
        {
            Assert.That(correspondent.Name, Is.EqualTo("MUIR PATENT LAW, PLLC"));
            Assert.That(correspondent.Address.AddressLineOne, Is.EqualTo("P.O. BOX 1213"));
            Assert.That(correspondent.Address.AddressLineThree, Is.EqualTo("GREAT FALLS, VA 22066"));
            Assert.That(correspondent.UnknownFields["correspondenceKind"], Is.EqualTo("\"legal\""));
            Assert.That(assignment.DomesticRepresentative?.Name, Is.EqualTo("US REPRESENTATIVE"));
            Assert.That(assignment.DomesticRepresentative?.Address.EmailAddress, Is.EqualTo("representative@example.test"));
            Assert.That(assignment.DomesticRepresentative?.LastIngestedAt, Is.EqualTo(new DateTimeOffset(2025, 4, 23, 2, 47, 58, TimeSpan.Zero)));
            Assert.That(assignment.DomesticRepresentative?.HasLastIngestedAt, Is.True);
        });

        PatentAssignmentProperty firstProperty = assignment.Properties[0];
        PatentAssignmentProperty secondProperty = assignment.Properties[1];
        Assert.Multiple(() =>
        {
            Assert.That(firstProperty.PropertyIdentifier, Is.EqualTo("PROP-1"));
            Assert.That(firstProperty.PropertyType, Is.EqualTo("PATENT"));
            Assert.That(firstProperty.ApplicationNumber, Is.EqualTo("15000001"));
            Assert.That(firstProperty.PatentNumber, Is.EqualTo("11646472"));
            Assert.That(firstProperty.PublicationNumber, Is.EqualTo("US20210210819A1"));
            Assert.That(firstProperty.Title, Is.EqualTo("Battery Cell"));
            Assert.That(firstProperty.UnknownFields["propertyStatus"], Is.EqualTo("\"active\""));
            Assert.That(secondProperty.PropertyIdentifier, Is.EqualTo("PROP-2"));
            Assert.That(secondProperty.PropertyType, Is.EqualTo("APPLICATION"));
            Assert.That(secondProperty.ApplicationNumber, Is.EqualTo("15000002"));
            Assert.That(secondProperty.Title, Is.EqualTo("Related Cell"));
        });
    }

    [Test]
    public void DocumentedSingletonAndArrayShapesUseLegacyGeographicFallbacks()
    {
        string response = """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "15/000,001",
                  "assignmentBag": {
                    "reelNumber": "000123",
                    "frameNumber": "0042",
                    "imageAvailableStatusCode": "N",
                    "assigneeBag": [
                      {
                        "assigneeName": "Legacy Owner",
                        "assigneeAddress": {
                          "countryOrStateCode": "VA",
                          "ictStateCode": "XX",
                          "ictCountryCode": "US"
                        }
                      }
                    ],
                    "correspondenceAddress": [
                      { "name": "First Correspondent" },
                      { "correspondentNameText": "Second Correspondent" }
                    ],
                    "assignmentPropertyBag": {
                      "propertyNumberText": "LEGACY-1",
                      "propertyTypeCode": "PAT",
                      "patentNumberText": "1234567",
                      "title": "Legacy Property"
                    }
                  }
                }
              ]
            }
            """;

        PatentAssignment assignment = Parse(response).Assignments[0];

        Assert.Multiple(() =>
        {
            Assert.That(assignment.ReelNumber, Is.EqualTo("000123"));
            Assert.That(assignment.FrameNumber, Is.EqualTo("0042"));
            Assert.That(assignment.ReelAndFrameNumber, Is.EqualTo("000123/0042"));
            Assert.That(assignment.IsImageAvailable, Is.False);
            Assert.That(assignment.HasIsImageAvailable, Is.True);
            Assert.That(assignment.Assignees[0].Name, Is.EqualTo("Legacy Owner"));
            Assert.That(assignment.Assignees[0].Address.GeographicRegionCode, Is.EqualTo("VA"));
            Assert.That(assignment.Assignees[0].Address.CountryCode, Is.EqualTo("US"));
            Assert.That(assignment.Correspondents, Has.Length.EqualTo(2));
            Assert.That(assignment.Correspondents[0].Name, Is.EqualTo("First Correspondent"));
            Assert.That(assignment.Correspondents[1].Name, Is.EqualTo("Second Correspondent"));
            Assert.That(assignment.Properties, Has.Length.EqualTo(1));
            Assert.That(assignment.Properties[0].PropertyIdentifier, Is.EqualTo("LEGACY-1"));
            Assert.That(assignment.Properties[0].PropertyType, Is.EqualTo("PAT"));
            Assert.That(assignment.Properties[0].PatentNumber, Is.EqualTo("1234567"));
            Assert.That(assignment.Properties[0].Title, Is.EqualTo("Legacy Property"));
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeAssignmentRetrieval()
    {
        RecordingAssignmentHandler handler = new RecordingAssignmentHandler(
            HttpStatusCode.OK,
            EmptyResponse(),
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentAssignmentHistory result = operation.GetAssignments("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/15000001/assignment"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("15000001"));
            Assert.That(result.Assignments, Is.Empty);
        });
    }

    [Test]
    public void SparseAssignmentPreservesAbsentValuesAndModelDefaults()
    {
        PatentAssignmentHistory result = Parse("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\",\"assignmentBag\":[{}]}]}");
        PatentAssignment assignment = result.Assignments[0];
        PatentAssignmentHistory empty = new PatentAssignmentHistory();

        Assert.Multiple(() =>
        {
            Assert.That(assignment.ReelNumber, Is.Null);
            Assert.That(assignment.FrameNumber, Is.Null);
            Assert.That(assignment.ReelAndFrameNumber, Is.Null);
            Assert.That(assignment.HasPageCount, Is.False);
            Assert.That(assignment.HasIsImageAvailable, Is.False);
            Assert.That(assignment.DocumentUri, Is.Null);
            Assert.That(assignment.HasReceivedDate, Is.False);
            Assert.That(assignment.HasRecordedDate, Is.False);
            Assert.That(assignment.HasMailedDate, Is.False);
            Assert.That(assignment.Assignors, Is.Empty);
            Assert.That(assignment.Assignees, Is.Empty);
            Assert.That(assignment.Correspondents, Is.Empty);
            Assert.That(assignment.DomesticRepresentative, Is.Null);
            Assert.That(assignment.Properties, Is.Empty);
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(empty.Assignments, Is.Empty);
            Assert.That(new PatentAssignmentAssignee().Address, Is.Not.Null);
            Assert.That(new PatentAssignmentCorrespondent().Address, Is.Not.Null);
        });
    }

    [TestCase("{}", "$.count")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[false]}", "$.patentFileWrapperDataBag[0]")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"16/000,001\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\",\"assignmentBag\":17}]}", "$.patentFileWrapperDataBag[0].assignmentBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\",\"assignmentBag\":[false]}]}", "$.patentFileWrapperDataBag[0].assignmentBag[0]")]
    public void MalformedContainersReportTheirExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent assignment retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"pageTotalQuantity\":-1", "pageTotalQuantity")]
    [TestCase("\"pageTotalQuantity\":{}", "pageTotalQuantity")]
    [TestCase("\"imageAvailableStatusCode\":\"perhaps\"", "imageAvailableStatusCode")]
    [TestCase("\"assignmentDocumentLocationURI\":\"relative.pdf\"", "assignmentDocumentLocationURI")]
    [TestCase("\"assignmentReceivedDate\":\"yesterday\"", "assignmentReceivedDate")]
    [TestCase("\"assignmentRecordedDate\":{}", "assignmentRecordedDate")]
    [TestCase("\"reelNumber\":{}", "reelNumber")]
    public void MalformedAssignmentValuesReportTheirExactPath(string fragment, string fieldName)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(AssignmentResponse(fragment)));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].assignmentBag[0]." + fieldName));
    }

    [TestCase("\"assignorBag\":{}", "assignorBag")]
    [TestCase("\"assignorBag\":[false]", "assignorBag[0]")]
    [TestCase("\"assignorBag\":[{\"executionDate\":\"never\"}]", "assignorBag[0].executionDate")]
    [TestCase("\"assigneeBag\":{}", "assigneeBag")]
    [TestCase("\"assigneeBag\":[false]", "assigneeBag[0]")]
    [TestCase("\"assigneeBag\":[{\"assigneeAddress\":false}]", "assigneeBag[0].assigneeAddress")]
    [TestCase("\"correspondenceAddress\":17", "correspondenceAddress")]
    [TestCase("\"correspondenceAddress\":[false]", "correspondenceAddress[0]")]
    [TestCase("\"correspondenceAddress\":{\"lastIngestionDateTime\":\"never\"}", "correspondenceAddress.lastIngestionDateTime")]
    [TestCase("\"domesticRepresentative\":false", "domesticRepresentative")]
    [TestCase("\"propertyBag\":17", "propertyBag")]
    [TestCase("\"propertyBag\":[false]", "propertyBag[0]")]
    [TestCase("\"propertyBag\":[{\"propertyIdentifier\":{}}]", "propertyBag[0].propertyIdentifier")]
    public void MalformedNestedValuesReportTheirExactPath(string fragment, string relativePath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(AssignmentResponse(fragment)));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].assignmentBag[0]." + relativePath));
    }

    [Test]
    public void RetrievalMapsNotFoundUsingTheCanonicalApplicationNumber()
    {
        RecordingAssignmentHandler handler = new RecordingAssignmentHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-assignment\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedAssignments("15/000,001", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("15000001"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-assignment"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CancellationStopsBeforeAssignmentRetrieval()
    {
        RecordingAssignmentHandler handler = new RecordingAssignmentHandler(HttpStatusCode.OK, EmptyResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetAssignments("15/000,001", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentAssignmentHistory Parse(string response)
    {
        return OdpPatentAssignmentParser.Parse(response, "15000001", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/15000001/assignment"),
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

    private static string AssignmentResponse(string fragment)
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\",\"assignmentBag\":[{" + fragment + "}]}]}";
    }

    private static string EmptyResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\"}]}";
    }

    private static string CompleteResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "15/000,001",
                  "assignmentBag": [
                    {
                      "assignmentDocumentLocationURI": "https://assignmentcenter.uspto.gov/ipas/search/api/v2/public/download/patent/38323/190",
                      "assignmentReceivedDate": "2016-04-19",
                      "assignmentRecordedDate": "2016-04-19",
                      "assignmentMailedDate": "2016-04-20",
                      "reelAndFrameNumber": "038323/0190",
                      "frameNumber": 190,
                      "reelNumber": 38323,
                      "conveyanceText": "ASSIGNMENT OF ASSIGNOR'S INTEREST",
                      "imageAvailableStatusCode": true,
                      "pageTotalQuantity": 8,
                      "attorneyDocketNumber": "SAM-53161",
                      "assignorBag": [
                        { "assignorName": "HEO, JIN-PIL", "executionDate": "2016-01-05", "assignorSequence": 1 },
                        { "assignorNameText": "YI, IN-SUN" }
                      ],
                      "assigneeBag": [
                        {
                          "assigneeNameText": "SAMSUNG ELECTRONICS CO., LTD",
                          "assigneeAddress": {
                            "addressLineOneText": "129, SAMSUNG-RO",
                            "addressLineTwoText": "YEONGTONG-GU",
                            "cityName": "SUWON-SI",
                            "geographicRegionName": "GYEONGGI-DO",
                            "geographicRegionCode": "KRX",
                            "countryOrStateCode": "OLD",
                            "postalCode": "16677",
                            "ictCountryCode": "KR",
                            "countryName": "KOREA, REPUBLIC OF"
                          },
                          "ownershipPercent": 100
                        }
                      ],
                      "correspondenceAddress": {
                        "correspondentNameText": "MUIR PATENT LAW, PLLC",
                        "addressLineOneText": "P.O. BOX 1213",
                        "addressLineTwoText": "9913 GEORGETOWN PIKE, SUITE 200",
                        "addressLineThreeText": "GREAT FALLS, VA 22066",
                        "correspondenceKind": "legal"
                      },
                      "domesticRepresentative": {
                        "name": "US REPRESENTATIVE",
                        "cityName": "Alexandria",
                        "geographicRegionCode": "VA",
                        "countryName": "UNITED STATES",
                        "emailAddress": "representative@example.test",
                        "lastIngestionDateTime": "2025-04-23T02:47:58Z"
                      },
                      "propertyBag": [
                        {
                          "propertyIdentifier": "PROP-1",
                          "propertyType": "PATENT",
                          "applicationNumberText": "15000001",
                          "patentNumber": "11646472",
                          "publicationNumber": "US20210210819A1",
                          "inventionTitle": "Battery Cell",
                          "propertyStatus": "active"
                        },
                        {
                          "propertyNumber": "PROP-2",
                          "propertyCategory": "APPLICATION",
                          "applicationNumber": "15000002",
                          "propertyTitle": "Related Cell"
                        }
                      ],
                      "assignmentVersion": 4
                    }
                  ],
                  "requestIdentifier": "assignment-record-request",
                  "futureRecord": 2
                }
              ],
              "requestIdentifier": "assignment-root-request",
              "futureResponse": true
            }
            """;
    }
}

internal class RecordingAssignmentHandler : HttpMessageHandler
{
    internal HttpStatusCode AssignmentStatusCode;
    internal string AssignmentResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingAssignmentHandler(HttpStatusCode assignmentStatusCode, string assignmentResponse)
    {
        AssignmentStatusCode = assignmentStatusCode;
        AssignmentResponse = assignmentResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingAssignmentHandler(HttpStatusCode assignmentStatusCode, string assignmentResponse, string lookupResponse)
    {
        AssignmentStatusCode = assignmentStatusCode;
        AssignmentResponse = assignmentResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpResponseMessage response = new HttpResponseMessage(lookup ? HttpStatusCode.OK : AssignmentStatusCode)
        {
            Content = new StringContent(lookup ? LookupResponse : AssignmentResponse, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
