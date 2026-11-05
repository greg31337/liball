using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentAttorneyOperationTests
{
    [Test]
    public void RetrievalMapsAttorneysAgentsAndCorrespondence()
    {
        RecordingAttorneyHandler handler = new RecordingAttorneyHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentAttorneyRecord result = operation.GetAttorneys("14/104,993", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14104993/attorney"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Attorneys, Has.Length.EqualTo(2));
            Assert.That(result.Correspondence, Has.Length.EqualTo(3));
            Assert.That(result.RequestIdentifier, Is.EqualTo("attorney-record-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("attorney-record-request"));
            Assert.That(result.Provenance.SourceUri?.AbsoluteUri, Is.EqualTo(handler.RequestUris[0].AbsoluteUri));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["futureRecord"], Is.EqualTo("2"));
            Assert.That(result.UnknownFields["recordFuture"], Is.EqualTo("true"));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("\"root\""));
        });

        PatentAttorney power = result.Attorneys[0];
        Assert.Multiple(() =>
        {
            Assert.That(power.Person.FullName, Is.EqualTo("Dr DANIEL D O'BRIEN Jr."));
            Assert.That(power.Person.FirstName, Is.EqualTo("DANIEL"));
            Assert.That(power.Person.MiddleName, Is.EqualTo("D"));
            Assert.That(power.Person.LastName, Is.EqualTo("O'BRIEN"));
            Assert.That(power.Person.PreferredName, Is.EqualTo("DAN"));
            Assert.That(power.Person.NamePrefix, Is.EqualTo("Dr"));
            Assert.That(power.Person.NameSuffix, Is.EqualTo("Jr."));
            Assert.That(power.RegistrationNumber, Is.EqualTo("65545"));
            Assert.That(power.PractitionerCategory, Is.EqualTo("ATTNY"));
            Assert.That(power.Status, Is.EqualTo("ACTIVE"));
            Assert.That(power.IsActive, Is.True);
            Assert.That(power.HasIsActive, Is.True);
            Assert.That(power.IsPowerOfAttorney, Is.True);
            Assert.That(power.HasIsPowerOfAttorney, Is.True);
            Assert.That(power.CountryCode, Is.EqualTo("US"));
            Assert.That(power.Organization?.Name, Is.EqualTo("SEED IP LAW GROUP LLP"));
            Assert.That(power.Addresses, Has.Length.EqualTo(1));
            Assert.That(power.Addresses[0].NameLineOne, Is.EqualTo("SEED IP LAW GROUP LLP"));
            Assert.That(power.Addresses[0].AddressLineOne, Is.EqualTo("701 FIFTH AVENUE"));
            Assert.That(power.Addresses[0].AddressLineTwo, Is.EqualTo("SUITE 5400"));
            Assert.That(power.Addresses[0].City, Is.EqualTo("SEATTLE"));
            Assert.That(power.Addresses[0].GeographicRegionName, Is.EqualTo("WASHINGTON"));
            Assert.That(power.Addresses[0].GeographicRegionCode, Is.EqualTo("WA"));
            Assert.That(power.Addresses[0].PostalCode, Is.EqualTo("98104"));
            Assert.That(power.Addresses[0].CountryCode, Is.EqualTo("US"));
            Assert.That(power.Telecommunications, Has.Length.EqualTo(2));
            Assert.That(power.Telecommunications[0].Number, Is.EqualTo("206-622-4900"));
            Assert.That(power.Telecommunications[0].Extension, Is.EqualTo("243"));
            Assert.That(power.Telecommunications[0].TypeCode, Is.EqualTo("TEL"));
            Assert.That(power.UnknownFields["practitionerFuture"], Is.EqualTo("7"));
        });

        PatentAttorney agent = result.Attorneys[1];
        Assert.Multiple(() =>
        {
            Assert.That(agent.Person.FullName, Is.EqualTo("TIA S. LEE"));
            Assert.That(agent.PractitionerCategory, Is.EqualTo("AGENT"));
            Assert.That(agent.RegistrationNumber, Is.EqualTo("83376"));
            Assert.That(agent.Status, Is.EqualTo("false"));
            Assert.That(agent.IsActive, Is.False);
            Assert.That(agent.HasIsActive, Is.True);
            Assert.That(agent.IsPowerOfAttorney, Is.False);
            Assert.That(agent.HasIsPowerOfAttorney, Is.True);
            Assert.That(agent.Addresses[0].GeographicRegionCode, Is.EqualTo("CA"));
            Assert.That(agent.Addresses[0].CountryCode, Is.EqualTo("US"));
            Assert.That(agent.Telecommunications[0].TypeCode, Is.EqualTo("EMAIL"));
        });

        PatentCorrespondence first = result.Correspondence[0];
        PatentCorrespondence second = result.Correspondence[1];
        PatentCorrespondence wrapper = result.Correspondence[2];
        Assert.Multiple(() =>
        {
            Assert.That(first.CustomerNumber, Is.EqualTo("38106"));
            Assert.That(first.DocketNumber, Is.EqualTo("CLIENT-42"));
            Assert.That(first.Recipient.Organization?.Name, Is.EqualTo("SEED IP LAW GROUP LLP/STMICROELECTRONICS"));
            Assert.That(first.Address.AddressLineOne, Is.EqualTo("701 FIFTH AVENUE, SUITE 5400"));
            Assert.That(first.Address.GeographicRegionCode, Is.EqualTo("WA"));
            Assert.That(first.Telecommunications, Has.Length.EqualTo(2));
            Assert.That(first.UnknownFields["customerFuture"], Is.EqualTo("\"kept\""));
            Assert.That(second.CustomerNumber, Is.EqualTo("38106"));
            Assert.That(second.Address.AddressLineOne, Is.EqualTo("SECOND OFFICE"));
            Assert.That(second.Telecommunications[1].TypeCode, Is.EqualTo("FAX"));
            Assert.That(wrapper.CustomerNumber, Is.EqualTo("90001"));
            Assert.That(wrapper.DocketNumber, Is.EqualTo("WRAPPER-1"));
            Assert.That(wrapper.Recipient.Organization?.Name, Is.EqualTo("DIRECT CORRESPONDENT"));
            Assert.That(wrapper.Address.GeographicRegionCode, Is.EqualTo("VA"));
            Assert.That(wrapper.Address.CountryCode, Is.EqualTo("US"));
            Assert.That(wrapper.UnknownFields["wrapperFuture"], Is.EqualTo("3"));
        });
    }

    [Test]
    public void StatusAndFieldAliasesAreHandledWithoutLosingRoleInformation()
    {
        string response = """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "14/104,993",
                  "recordAttorney": {
                    "customerNumberCorrespondenceData": {
                      "patronIdentifier": "00042",
                      "organizationName": "Alias Firm",
                      "powerOfAttorneyAddressBag": [],
                      "telecommunicationAddressBag": [
                        { "telephoneNumber": "555-0100", "extension": "9", "telecommunicationTypeCode": "PHONE" }
                      ]
                    },
                    "powerOfAttorneyBag": [
                      { "preferredName": "Preferred Only", "activeIndicator": "Y" }
                    ],
                    "attorneyBag": [
                      {
                        "firstName": "Inactive",
                        "activeIndicator": "INACTIVE",
                        "attorneyAddressBag": [
                          { "countryOrStateCode": "TX", "ictStateCode": "ZZ", "ictCountryCode": "US" }
                        ],
                        "telecommunicationAddressBag": [
                          { "number": "alias@example.test", "typeCode": "EMAIL" }
                        ]
                      }
                    ]
                  }
                }
              ]
            }
            """;

        PatentAttorneyRecord result = Parse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Attorneys[0].Person.FullName, Is.EqualTo("Preferred Only"));
            Assert.That(result.Attorneys[0].IsActive, Is.True);
            Assert.That(result.Attorneys[0].IsPowerOfAttorney, Is.True);
            Assert.That(result.Attorneys[1].IsActive, Is.False);
            Assert.That(result.Attorneys[1].HasIsActive, Is.True);
            Assert.That(result.Attorneys[1].IsPowerOfAttorney, Is.False);
            Assert.That(result.Attorneys[1].Addresses[0].GeographicRegionCode, Is.EqualTo("TX"));
            Assert.That(result.Attorneys[1].Addresses[0].CountryCode, Is.EqualTo("US"));
            Assert.That(result.Attorneys[1].Telecommunications[0].Number, Is.EqualTo("alias@example.test"));
            Assert.That(result.Correspondence, Has.Length.EqualTo(1));
            Assert.That(result.Correspondence[0].CustomerNumber, Is.EqualTo("00042"));
            Assert.That(result.Correspondence[0].Recipient.Organization?.Name, Is.EqualTo("Alias Firm"));
            Assert.That(result.Correspondence[0].Telecommunications[0].Number, Is.EqualTo("555-0100"));
            Assert.That(result.Correspondence[0].Telecommunications[0].Extension, Is.EqualTo("9"));
            Assert.That(result.Correspondence[0].Telecommunications[0].TypeCode, Is.EqualTo("PHONE"));
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeAttorneyRetrieval()
    {
        RecordingAttorneyHandler handler = new RecordingAttorneyHandler(
            HttpStatusCode.OK,
            EmptyResponse(),
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/104,993\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentAttorneyRecord result = operation.GetAttorneys("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/14104993/attorney"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("14104993"));
        });
    }

    [Test]
    public void SparseAttorneyRecordPreservesDefaults()
    {
        PatentAttorneyRecord result = Parse(EmptyResponse());
        PatentAttorneyRecord empty = new PatentAttorneyRecord();
        PatentAttorney attorney = new PatentAttorney();
        PatentCorrespondence correspondence = new PatentCorrespondence();

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Attorneys, Is.Empty);
            Assert.That(result.Correspondence, Is.Empty);
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(empty.Attorneys, Is.Empty);
            Assert.That(empty.Correspondence, Is.Empty);
            Assert.That(empty.UnknownFields, Is.Empty);
            Assert.That(attorney.CountryCode, Is.Null);
            Assert.That(attorney.UnknownFields, Is.Empty);
            Assert.That(correspondence.UnknownFields, Is.Empty);
        });
    }

    [TestCase("{}", "$.count")]
    [TestCase("{\"count\":0,\"patentFileWrapperDataBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[]}", "$.patentFileWrapperDataBag")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[false]}", "$.patentFileWrapperDataBag[0]")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"15/000,001\"}]}", "$.patentFileWrapperDataBag[0].applicationNumberText")]
    [TestCase("{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/104,993\",\"recordAttorney\":false}]}", "$.patentFileWrapperDataBag[0].recordAttorney")]
    public void MalformedEnvelopeReportsItsExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent attorney retrieval"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"powerOfAttorneyBag\":{}", "powerOfAttorneyBag")]
    [TestCase("\"powerOfAttorneyBag\":[false]", "powerOfAttorneyBag[0]")]
    [TestCase("\"attorneyBag\":{}", "attorneyBag")]
    [TestCase("\"attorneyBag\":[false]", "attorneyBag[0]")]
    [TestCase("\"customerNumberCorrespondenceData\":false", "customerNumberCorrespondenceData")]
    [TestCase("\"customerNumberCorrespondenceData\":{\"powerOfAttorneyAddressBag\":{}}", "customerNumberCorrespondenceData.powerOfAttorneyAddressBag")]
    [TestCase("\"customerNumberCorrespondenceData\":{\"powerOfAttorneyAddressBag\":[false]}", "customerNumberCorrespondenceData.powerOfAttorneyAddressBag[0]")]
    [TestCase("\"customerNumberCorrespondenceData\":{\"telecommunicationAddressBag\":{}}", "customerNumberCorrespondenceData.telecommunicationAddressBag")]
    [TestCase("\"customerNumberCorrespondenceData\":{\"telecommunicationAddressBag\":[false]}", "customerNumberCorrespondenceData.telecommunicationAddressBag[0]")]
    public void MalformedRecordContainersReportTheirExactPath(string fragment, string relativePath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(RecordResponse(fragment)));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].recordAttorney." + relativePath));
    }

    [TestCase("\"firstName\":{}", "firstName")]
    [TestCase("\"activeIndicator\":{}", "activeIndicator")]
    [TestCase("\"attorneyAddressBag\":{}", "attorneyAddressBag")]
    [TestCase("\"attorneyAddressBag\":[false]", "attorneyAddressBag[0]")]
    [TestCase("\"attorneyAddressBag\":[{\"cityName\":{}}]", "attorneyAddressBag[0].cityName")]
    [TestCase("\"telecommunicationAddressBag\":{}", "telecommunicationAddressBag")]
    [TestCase("\"telecommunicationAddressBag\":[false]", "telecommunicationAddressBag[0]")]
    [TestCase("\"telecommunicationAddressBag\":[{\"telecommunicationNumber\":{}}]", "telecommunicationAddressBag[0].telecommunicationNumber")]
    public void MalformedPractitionerValuesReportTheirExactPath(string fragment, string relativePath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(RecordResponse("\"attorneyBag\":[{" + fragment + "}]")));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].recordAttorney.attorneyBag[0]." + relativePath));
    }

    [TestCase("\"patronIdentifier\":{}", "patronIdentifier")]
    [TestCase("\"organizationStandardName\":{}", "organizationStandardName")]
    [TestCase("\"powerOfAttorneyAddressBag\":[{\"countryCode\":{}}]", "powerOfAttorneyAddressBag[0].countryCode")]
    [TestCase("\"telecommunicationAddressBag\":[{\"extensionNumber\":{}}]", "telecommunicationAddressBag[0].extensionNumber")]
    public void MalformedCorrespondenceValuesReportTheirExactPath(string fragment, string relativePath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(RecordResponse("\"customerNumberCorrespondenceData\":{" + fragment + "}")));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0].recordAttorney.customerNumberCorrespondenceData." + relativePath));
    }

    [TestCase("{}", "correspondenceAddressBag")]
    [TestCase("[false]", "correspondenceAddressBag[0]")]
    [TestCase("[{\"postalCode\":{}}]", "correspondenceAddressBag[0].postalCode")]
    public void MalformedWrapperCorrespondenceReportsItsExactPath(string value, string relativePath)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/104,993\",\"correspondenceAddressBag\":" + value + "}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.patentFileWrapperDataBag[0]." + relativePath));
    }

    [Test]
    public void RetrievalMapsNotFoundUsingCanonicalApplicationNumber()
    {
        RecordingAttorneyHandler handler = new RecordingAttorneyHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-attorney\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedAttorneys("14/104,993", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("14104993"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-attorney"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CancellationStopsBeforeAttorneyRetrieval()
    {
        RecordingAttorneyHandler handler = new RecordingAttorneyHandler(HttpStatusCode.OK, EmptyResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetAttorneys("14/104,993", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentAttorneyRecord Parse(string response)
    {
        return OdpPatentAttorneyParser.Parse(response, "14104993", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14104993/attorney"),
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

    private static string RecordResponse(string fragment)
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/104,993\",\"recordAttorney\":{" + fragment + "}}]}";
    }

    private static string EmptyResponse()
    {
        return "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/104,993\"}]}";
    }

    private static string CompleteResponse()
    {
        return """
            {
              "count": 1,
              "patentFileWrapperDataBag": [
                {
                  "applicationNumberText": "14/104,993",
                  "recordAttorney": {
                    "customerNumberCorrespondenceData": {
                      "patronIdentifier": 38106,
                      "organizationStandardName": "SEED IP LAW GROUP LLP/STMICROELECTRONICS",
                      "docketNumber": "CLIENT-42",
                      "powerOfAttorneyAddressBag": [
                        {
                          "nameLineOneText": "SEED IP LAW GROUP LLP/STMICROELECTRONICS",
                          "addressLineOneText": "701 FIFTH AVENUE, SUITE 5400",
                          "cityName": "SEATTLE",
                          "geographicRegionName": "WASHINGTON",
                          "geographicRegionCode": "WA",
                          "postalCode": "98104-7092",
                          "countryCode": "US",
                          "countryName": "UNITED STATES"
                        },
                        {
                          "addressLineOneText": "SECOND OFFICE",
                          "cityName": "BELLEVUE",
                          "geographicRegionCode": "WA",
                          "postalCode": "98004",
                          "countryCode": "US"
                        }
                      ],
                      "telecommunicationAddressBag": [
                        { "telecommunicationNumber": "206-622-4900", "extensionNumber": "409", "telecomTypeCode": "TEL" },
                        { "telecommunicationNumber": "206-622-4800", "telecomTypeCode": "FAX" }
                      ],
                      "customerFuture": "kept"
                    },
                    "powerOfAttorneyBag": [
                      {
                        "firstName": "DANIEL",
                        "middleName": "D",
                        "lastName": "O'BRIEN",
                        "namePrefix": "Dr",
                        "nameSuffix": "Jr.",
                        "preferredName": "DAN",
                        "countryCode": "US",
                        "registrationNumber": "65545",
                        "activeIndicator": "ACTIVE",
                        "registeredPractitionerCategory": "ATTNY",
                        "organizationStandardName": "SEED IP LAW GROUP LLP",
                        "attorneyAddressBag": [
                          {
                            "nameLineOneText": "SEED IP LAW GROUP LLP",
                            "addressLineOneText": "701 FIFTH AVENUE",
                            "addressLineTwoText": "SUITE 5400",
                            "cityName": "SEATTLE",
                            "geographicRegionName": "WASHINGTON",
                            "geographicRegionCode": "WA",
                            "countryOrStateCode": "OLD",
                            "postalCode": "98104",
                            "countryCode": "US",
                            "countryName": "UNITED STATES"
                          }
                        ],
                        "telecommunicationAddressBag": [
                          { "telecommunicationNumber": "206-622-4900", "extensionNumber": "243", "telecomTypeCode": "TEL" },
                          { "telecommunicationNumber": "daniel@example.test", "telecomTypeCode": "EMAIL" }
                        ],
                        "practitionerFuture": 7
                      }
                    ],
                    "attorneyBag": [
                      {
                        "firstName": "TIA",
                        "middleName": "S.",
                        "lastName": "LEE",
                        "registrationNumber": "83376",
                        "activeIndicator": false,
                        "registeredPractitionerCategory": "AGENT",
                        "attorneyAddressBag": [
                          { "cityName": "OAKLAND", "geographicRegionCode": "CA", "countryCode": "US" }
                        ],
                        "telecommunicationAddressBag": [
                          { "telecommunicationNumber": "tia@example.test", "telecomTypeCode": "EMAIL" }
                        ]
                      }
                    ],
                    "recordFuture": true
                  },
                  "correspondenceAddressBag": [
                    {
                      "nameLineOneText": "DIRECT CORRESPONDENT",
                      "addressLineOneText": "100 MAIN STREET",
                      "cityName": "ALEXANDRIA",
                      "countryOrStateCode": "VA",
                      "ictCountryCode": "US",
                      "postalCode": "22314",
                      "customerNumber": "90001",
                      "attorneyDocketNumber": "WRAPPER-1",
                      "wrapperFuture": 3
                    }
                  ],
                  "requestIdentifier": "attorney-record-request",
                  "futureRecord": 2
                }
              ],
              "requestIdentifier": "attorney-root-request",
              "futureResponse": "root"
            }
            """;
    }
}

internal class RecordingAttorneyHandler : HttpMessageHandler
{
    internal HttpStatusCode AttorneyStatusCode;
    internal string AttorneyResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingAttorneyHandler(HttpStatusCode attorneyStatusCode, string attorneyResponse)
    {
        AttorneyStatusCode = attorneyStatusCode;
        AttorneyResponse = attorneyResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingAttorneyHandler(HttpStatusCode attorneyStatusCode, string attorneyResponse, string lookupResponse)
    {
        AttorneyStatusCode = attorneyStatusCode;
        AttorneyResponse = attorneyResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpResponseMessage response = new HttpResponseMessage(lookup ? HttpStatusCode.OK : AttorneyStatusCode)
        {
            Content = new StringContent(lookup ? LookupResponse : AttorneyResponse, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
