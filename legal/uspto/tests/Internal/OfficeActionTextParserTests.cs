using uspto;

namespace uspto.Tests;

public class OfficeActionTextParserTests
{
    [Test]
    public void ParseMapsBodySectionsApplicationExaminerAndArtUnitData()
    {
        string json = """
        {
          "requestIdentifier": "body-request",
          "envelopeRevision": 4,
          "response": {
            "numFound": 3,
            "start": 1,
            "processingTime": 7,
            "docs": [
              {
                "id": "office-action-1",
                "patentApplicationNumber": ["16123456"],
                "patentNumber": [12345678],
                "bodyText": ["Full Office Action body."],
                "inventionTitle": ["Battery assembly"],
                "applicationTypeCategory": ["REGULAR"],
                "applicationStatusNumber": "150",
                "patentApplicationConfirmationNumber": 4321,
                "customerNumber": "70001",
                "groupArtUnitNumber": 1713,
                "examinerEmployeeNumber": ["EX123"],
                "workGroup": ["1710"],
                "techCenter": ["1700"],
                "nationalClass": ["429"],
                "nationalSubclass": ["100"],
                "legacyDocumentCodeIdentifier": ["CTNF"],
                "obsoleteDocumentIdentifier": ["IFW-1"],
                "filingDate": "2022-01-03",
                "effectiveFilingDate": "2021-12-20",
                "applicationDeemedWithdrawnDate": null,
                "grantDate": ["2025-02-04"],
                "submissionDate": "2024-05-06 12:30:45.0",
                "lastModifiedTimestamp": "2024-05-07T13:31:46Z",
                "sections.officeActionIdentifier": ["OA-1"],
                "sections.patentApplicationNumber": ["16123456"],
                "sections.summaryText": ["Claims 1-10 are rejected."],
                "sections.section101RejectionText": "Claim 1 is directed to ineligible subject matter.",
                "sections.section101RejectionFormParagraphText": ["7.05"],
                "sections.section102RejectionText": ["Claims 2-3 are anticipated."],
                "sections.section102RejectionFormParagraphText": ["7.15"],
                "sections.section103RejectionText": ["Claims 4-6 are obvious."],
                "sections.section103RejectionFormParagraphText": ["7.21"],
                "sections.section102AndOr103RejectionText": ["Claims 7-8 are rejected."],
                "sections.section102AndOr103RejectionFormParagraphText": ["7.20"],
                "sections.section112RejectionText": ["Claim 9 lacks written description."],
                "sections.section112RejectionFormParagraphText": ["7.30"],
                "sections.withdrawalRejectionText": ["The prior rejection of claim 10 is withdrawn."],
                "sections.terminalDisclaimerStatusText": ["A terminal disclaimer may overcome the rejection."],
                "sections.examinerEmployeeNumber": ["EX123"],
                "sections.groupArtUnitNumber": [1713],
                "sections.techCenterNumber": ["1700"],
                "sections.workGroupNumber": ["1710"],
                "sections.nationalClass": ["429"],
                "sections.nationalSubclass": ["100"],
                "sections.filingDate": ["2022-01-03"],
                "sections.grantDate": ["2025-02-04"],
                "sections.submissionDate": ["2024-05-06"],
                "sections.futurePassage": ["preserved"],
                "recordRevision": 9
              }
            ]
          }
        }
        """;
        SearchRequest request = new SearchRequest { OffsetPage = new OffsetPage { Offset = 1, Limit = 1 } };

        OfficeActionTextResponse response = Parse(json, request, "retained-json");
        OfficeActionTextRecord record = response.Records[0];

        Assert.Multiple(() =>
        {
            Assert.That(response.Request, Is.SameAs(request));
            Assert.That(response.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Start, Is.EqualTo(1));
            Assert.That(response.Page.Offset, Is.EqualTo(1));
            Assert.That(response.Page.Limit, Is.EqualTo(1));
            Assert.That(response.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(response.Provenance.DatasetIdentifier, Is.EqualTo("oa_actions"));
            Assert.That(response.Provenance.DatasetVersion, Is.EqualTo("v1"));
            Assert.That(response.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(response.RawResponse, Is.EqualTo("retained-json"));
            Assert.That(response.IsComplete, Is.False);
            Assert.That(response.UnknownFields["processingTime"], Is.EqualTo("7"));
            Assert.That(response.UnknownFields["envelope.envelopeRevision"], Is.EqualTo("4"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(record.Identifier, Is.EqualTo("office-action-1"));
            Assert.That(record.ApplicationNumbers, Is.EqualTo(new[] { "16123456" }));
            Assert.That(record.PatentNumbers, Is.EqualTo(new[] { "12345678" }));
            Assert.That(record.BodyText, Is.EqualTo(new[] { "Full Office Action body." }));
            Assert.That(record.InventionTitles, Is.EqualTo(new[] { "Battery assembly" }));
            Assert.That(record.ApplicationTypeCategories, Is.EqualTo(new[] { "REGULAR" }));
            Assert.That(record.ApplicationStatusNumber, Is.EqualTo(150));
            Assert.That(record.HasApplicationStatusNumber, Is.True);
            Assert.That(record.ApplicationConfirmationNumber, Is.EqualTo(4321));
            Assert.That(record.HasApplicationConfirmationNumber, Is.True);
            Assert.That(record.CustomerNumber, Is.EqualTo(70001));
            Assert.That(record.HasCustomerNumber, Is.True);
            Assert.That(record.GroupArtUnitNumber, Is.EqualTo(1713));
            Assert.That(record.HasGroupArtUnitNumber, Is.True);
            Assert.That(record.ExaminerEmployeeNumbers, Is.EqualTo(new[] { "EX123" }));
            Assert.That(record.WorkGroups, Is.EqualTo(new[] { "1710" }));
            Assert.That(record.TechnologyCenters, Is.EqualTo(new[] { "1700" }));
            Assert.That(record.NationalClasses, Is.EqualTo(new[] { "429" }));
            Assert.That(record.NationalSubclasses, Is.EqualTo(new[] { "100" }));
            Assert.That(record.LegacyDocumentCodeIdentifiers, Is.EqualTo(new[] { "CTNF" }));
            Assert.That(record.ObsoleteDocumentIdentifiers, Is.EqualTo(new[] { "IFW-1" }));
            Assert.That(record.FilingDate, Is.EqualTo(new DateTimeOffset(2022, 1, 3, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(record.HasFilingDate, Is.True);
            Assert.That(record.EffectiveFilingDate, Is.EqualTo(new DateTimeOffset(2021, 12, 20, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(record.HasEffectiveFilingDate, Is.True);
            Assert.That(record.HasApplicationDeemedWithdrawnDate, Is.False);
            Assert.That(record.GrantDate, Is.EqualTo(new DateTimeOffset(2025, 2, 4, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(record.HasGrantDate, Is.True);
            Assert.That(record.SubmissionDate, Is.EqualTo(new DateTimeOffset(2024, 5, 6, 12, 30, 45, TimeSpan.Zero)));
            Assert.That(record.HasSubmissionDate, Is.True);
            Assert.That(record.LastModifiedAt, Is.EqualTo(new DateTimeOffset(2024, 5, 7, 13, 31, 46, TimeSpan.Zero)));
            Assert.That(record.HasLastModifiedAt, Is.True);
            Assert.That(record.Provenance.DatasetIdentifier, Is.EqualTo("oa_actions"));
            Assert.That(record.UnknownFields["recordRevision"], Is.EqualTo("9"));
        });

        OfficeActionTextSection section = record.Sections;
        Assert.Multiple(() =>
        {
            Assert.That(record.HasSections, Is.True);
            Assert.That(section.OfficeActionIdentifiers, Is.EqualTo(new[] { "OA-1" }));
            Assert.That(section.ApplicationNumbers, Is.EqualTo(new[] { "16123456" }));
            Assert.That(section.SummaryText, Is.EqualTo(new[] { "Claims 1-10 are rejected." }));
            Assert.That(section.Section101RejectionText, Is.EqualTo(new[] { "Claim 1 is directed to ineligible subject matter." }));
            Assert.That(section.Section101RejectionFormParagraphText, Is.EqualTo(new[] { "7.05" }));
            Assert.That(section.Section102RejectionText, Is.EqualTo(new[] { "Claims 2-3 are anticipated." }));
            Assert.That(section.Section102RejectionFormParagraphText, Is.EqualTo(new[] { "7.15" }));
            Assert.That(section.Section103RejectionText, Is.EqualTo(new[] { "Claims 4-6 are obvious." }));
            Assert.That(section.Section103RejectionFormParagraphText, Is.EqualTo(new[] { "7.21" }));
            Assert.That(section.Section102And103RejectionText, Is.EqualTo(new[] { "Claims 7-8 are rejected." }));
            Assert.That(section.Section102And103RejectionFormParagraphText, Is.EqualTo(new[] { "7.20" }));
            Assert.That(section.Section112RejectionText, Is.EqualTo(new[] { "Claim 9 lacks written description." }));
            Assert.That(section.Section112RejectionFormParagraphText, Is.EqualTo(new[] { "7.30" }));
            Assert.That(section.WithdrawalRejectionText, Is.EqualTo(new[] { "The prior rejection of claim 10 is withdrawn." }));
            Assert.That(section.TerminalDisclaimerStatusText, Is.EqualTo(new[] { "A terminal disclaimer may overcome the rejection." }));
            Assert.That(section.ExaminerEmployeeNumbers, Is.EqualTo(new[] { "EX123" }));
            Assert.That(section.GroupArtUnitNumbers, Is.EqualTo(new[] { "1713" }));
            Assert.That(section.TechnologyCenterNumbers, Is.EqualTo(new[] { "1700" }));
            Assert.That(section.WorkGroupNumbers, Is.EqualTo(new[] { "1710" }));
            Assert.That(section.NationalClasses, Is.EqualTo(new[] { "429" }));
            Assert.That(section.NationalSubclasses, Is.EqualTo(new[] { "100" }));
            Assert.That(section.FilingDate, Is.EqualTo(new DateTimeOffset(2022, 1, 3, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(section.HasFilingDate, Is.True);
            Assert.That(section.GrantDate, Is.EqualTo(new DateTimeOffset(2025, 2, 4, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(section.HasGrantDate, Is.True);
            Assert.That(section.SubmissionDate, Is.EqualTo(new DateTimeOffset(2024, 5, 6, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(section.HasSubmissionDate, Is.True);
            Assert.That(section.UnknownFields["futurePassage"], Is.EqualTo("[\"preserved\"]"));
        });
    }

    [Test]
    public void ParseAcceptsDirectEnvelopeAndPreservesMissingValueState()
    {
        string json = """
        {
          "numFound": 1,
          "start": 0,
          "docs": [
            {
              "id": "minimal",
              "patentApplicationNumber": "17000123",
              "applicationStatusNumber": null,
              "filingDate": [],
              "bodyText": "Single body passage"
            }
          ]
        }
        """;

        OfficeActionTextResponse response = Parse(json, new SearchRequest(), null);
        OfficeActionTextRecord record = response.Records[0];

        Assert.Multiple(() =>
        {
            Assert.That(response.Count, Is.EqualTo(1));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(record.ApplicationNumbers, Is.EqualTo(new[] { "17000123" }));
            Assert.That(record.BodyText, Is.EqualTo(new[] { "Single body passage" }));
            Assert.That(record.HasApplicationStatusNumber, Is.False);
            Assert.That(record.HasFilingDate, Is.False);
            Assert.That(record.HasSections, Is.False);
            Assert.That(record.Sections.UnknownFields, Is.Empty);
        });
    }

    [Test]
    public void EmptyPageBeyondTotalIsACompleteResponse()
    {
        OfficeActionTextResponse response = Parse("{\"response\":{\"numFound\":2,\"start\":50,\"docs\":[]}}", new SearchRequest(), null);

        Assert.Multiple(() =>
        {
            Assert.That(response.Records, Is.Empty);
            Assert.That(response.Start, Is.EqualTo(50));
            Assert.That(response.IsComplete, Is.True);
        });
    }

    [TestCase("[]", "$")]
    [TestCase("{\"response\":[]}", "$.response")]
    [TestCase("{\"response\":{\"start\":0,\"docs\":[]}}", "$.response.numFound")]
    [TestCase("{\"response\":{\"numFound\":0,\"start\":-1,\"docs\":[]}}", "$.response.start")]
    [TestCase("{\"response\":{\"numFound\":0,\"start\":0,\"docs\":{}}}", "$.response.docs")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[false]}}", "$.response.docs[0]")]
    [TestCase("{\"response\":{\"numFound\":0,\"start\":0,\"docs\":[{}]}}", "$.response.docs")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"bodyText\":{}}]}}", "$.response.docs[0].bodyText")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"bodyText\":[null]}]}}", "$.response.docs[0].bodyText[0]")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"filingDate\":\"not-a-date\"}]}}", "$.response.docs[0].filingDate")]
    [TestCase("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"groupArtUnitNumber\":{}}]}}", "$.response.docs[0].groupArtUnitNumber")]
    public void MalformedResponsesReportExactPaths(string json, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(json, new SearchRequest(), null));

        Assert.Multiple(() =>
        {
            Assert.That(exception.SourceName, Is.EqualTo("Office Action text search"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    private static OfficeActionTextResponse Parse(string json, SearchRequest request, string? rawResponse)
    {
        return OfficeActionTextParser.Parse(json, request, new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/oa/oa_actions/v1/records"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json,
            RequestIdentifier = "header-request"
        }, rawResponse);
    }
}
