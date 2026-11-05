using uspto;

namespace uspto.Tests;

public class PetitionModelsTests
{
    [Test]
    public void DecisionDefaultsDistinguishMissingValuesAndOwnMutableCollections()
    {
        PetitionDecision first = new PetitionDecision();
        PetitionDecision second = new PetitionDecision();

        first.Inventors = new string[] { "Ada Lovelace" };
        first.IssuesConsidered = new string[] { "Revival" };
        first.Rules = new string[] { "37 CFR 1.137" };
        first.Statutes = new string[] { "35 USC 133" };
        first.Documents = new PetitionDecisionDocument[] { new PetitionDecisionDocument() };
        first.UnknownFields.Add("futureField", "future-value");

        Assert.Multiple(() =>
        {
            Assert.That(first.RecordIdentifier, Is.Null);
            Assert.That(first.CustomerNumber, Is.Null);
            Assert.That(first.PetitionTypeCode, Is.Zero);
            Assert.That(first.HasPetitionTypeCode, Is.False);
            Assert.That(first.HasDecisionDate, Is.False);
            Assert.That(first.HasDecisionMailDate, Is.False);
            Assert.That(first.HasPetitionMailDate, Is.False);
            Assert.That(first.IsFirstInventorToFile, Is.False);
            Assert.That(first.HasIsFirstInventorToFile, Is.False);
            Assert.That(first.CourtActionIndicator, Is.False);
            Assert.That(first.HasCourtActionIndicator, Is.False);
            Assert.That(first.HasLastIngestedAt, Is.False);
            Assert.That(second.Inventors, Is.Empty);
            Assert.That(second.IssuesConsidered, Is.Empty);
            Assert.That(second.Rules, Is.Empty);
            Assert.That(second.Statutes, Is.Empty);
            Assert.That(second.Documents, Is.Empty);
            Assert.That(second.UnknownFields, Is.Empty);
            Assert.That(first.UnknownFields, Is.Not.SameAs(second.UnknownFields));
        });
    }

    [Test]
    public void DecisionRetainsIdentityStatusDatesIssuesAndProvenance()
    {
        DateTimeOffset decisionDate = new DateTimeOffset(2024, 3, 20, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset decisionMailDate = new DateTimeOffset(2024, 3, 21, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset petitionMailDate = new DateTimeOffset(2022, 9, 12, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset ingestedAt = new DateTimeOffset(2025, 4, 23, 2, 47, 58, TimeSpan.Zero);
        PetitionDecision decision = new PetitionDecision
        {
            RecordIdentifier = "5d899d97-2bba-50a9-b01d-d87b38c70c55",
            ApplicationNumber = "15569871",
            PatentNumber = "11234567",
            FirstApplicantName = "BIOVERATIV THERAPEUTICS INC.",
            InventionTitle = "RECEPTOR TYROSINE KINASE BIOMARKERS",
            CustomerNumber = "23628",
            BusinessEntityStatusCategory = "Small",
            DecisionDate = decisionDate,
            HasDecisionDate = true,
            DecisionMailDate = decisionMailDate,
            HasDecisionMailDate = true,
            PetitionMailDate = petitionMailDate,
            HasPetitionMailDate = true,
            PetitionTypeCode = 551,
            HasPetitionTypeCode = true,
            DecisionTypeCode = "C",
            PetitionTypeDescription = "Petition to revive",
            DecisionTypeDescription = "DENIED",
            FinalDecidingOfficeName = "OFFICE OF PETITIONS",
            IsFirstInventorToFile = true,
            HasIsFirstInventorToFile = true,
            ArtUnit = "1646",
            TechnologyCenter = "1600",
            Inventors = new string[] { "David Clarke", "Daniel Rolfe" },
            CourtActionName = "None",
            CourtActionIndicator = false,
            HasCourtActionIndicator = true,
            IssuesConsidered = new string[] { "New ground of rejection in examiner's answer" },
            ProsecutionStatusDescription = "During examination",
            Rules = new string[] { "37 CFR 1.26(a)", "37 CFR 1.113" },
            Statutes = new string[] { "35 USC 134" },
            LastIngestedAt = ingestedAt,
            HasLastIngestedAt = true,
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                RequestIdentifier = "petition-request",
                IsAuthoritative = true
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(decision.RecordIdentifier, Is.EqualTo("5d899d97-2bba-50a9-b01d-d87b38c70c55"));
            Assert.That(decision.ApplicationNumber, Is.EqualTo("15569871"));
            Assert.That(decision.CustomerNumber, Is.EqualTo("23628"));
            Assert.That(decision.DecisionDate, Is.EqualTo(decisionDate));
            Assert.That(decision.DecisionMailDate, Is.EqualTo(decisionMailDate));
            Assert.That(decision.PetitionMailDate, Is.EqualTo(petitionMailDate));
            Assert.That(decision.PetitionTypeCode, Is.EqualTo(551));
            Assert.That(decision.HasPetitionTypeCode, Is.True);
            Assert.That(decision.PetitionTypeDescription, Is.EqualTo("Petition to revive"));
            Assert.That(decision.DecisionTypeDescription, Is.EqualTo("DENIED"));
            Assert.That(decision.IsFirstInventorToFile, Is.True);
            Assert.That(decision.HasIsFirstInventorToFile, Is.True);
            Assert.That(decision.CourtActionIndicator, Is.False);
            Assert.That(decision.HasCourtActionIndicator, Is.True);
            Assert.That(decision.Inventors, Has.Length.EqualTo(2));
            Assert.That(decision.IssuesConsidered[0], Does.Contain("rejection"));
            Assert.That(decision.Rules, Has.Length.EqualTo(2));
            Assert.That(decision.Statutes, Is.EqualTo(new[] { "35 USC 134" }));
            Assert.That(decision.LastIngestedAt, Is.EqualTo(ingestedAt));
            Assert.That(decision.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(decision.Provenance.IsAuthoritative, Is.True);
        });
    }

    [Test]
    public void DocumentRetainsDownloadRepresentationsAndUnknownFields()
    {
        DateTimeOffset officialDate = new DateTimeOffset(2020, 8, 31, 1, 20, 29, TimeSpan.FromHours(-4));
        PetitionDocumentDownloadAlternative alternative = new PetitionDocumentDownloadAlternative
        {
            MimeTypeIdentifier = "PDF",
            MediaType = "application/pdf",
            Format = DocumentFormat.Pdf,
            DownloadUri = new Uri("https://api.uspto.gov/api/v1/patent/application/documents/16123123/M98QOH0NWFYTX17.pdf"),
            PageCount = 2,
            HasPageCount = true
        };
        alternative.UnknownFields.Add("quality", "original");
        PetitionDecisionDocument document = new PetitionDecisionDocument
        {
            ApplicationNumber = "16123123",
            DocumentIdentifier = "M98QOH0NWFYTX17",
            Code = "PETDEC",
            Description = "Petition Decision",
            DirectionCategory = "OUTGOING",
            OfficialDate = officialDate,
            HasOfficialDate = true,
            DownloadAlternatives = new PetitionDocumentDownloadAlternative[] { alternative }
        };
        document.UnknownFields.Add("sourceSequence", "4");

        Assert.Multiple(() =>
        {
            Assert.That(document.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(document.DocumentIdentifier, Is.EqualTo("M98QOH0NWFYTX17"));
            Assert.That(document.Code, Is.EqualTo("PETDEC"));
            Assert.That(document.HasOfficialDate, Is.True);
            Assert.That(document.OfficialDate, Is.EqualTo(officialDate));
            Assert.That(document.DownloadAlternatives, Has.Length.EqualTo(1));
            Assert.That(document.DownloadAlternatives[0].Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(document.DownloadAlternatives[0].DownloadUri?.Host, Is.EqualTo("api.uspto.gov"));
            Assert.That(document.DownloadAlternatives[0].PageCount, Is.EqualTo(2));
            Assert.That(document.DownloadAlternatives[0].HasPageCount, Is.True);
            Assert.That(document.DownloadAlternatives[0].UnknownFields["quality"], Is.EqualTo("original"));
            Assert.That(document.UnknownFields["sourceSequence"], Is.EqualTo("4"));
        });
    }

    [Test]
    public void ResponseFacetsAndExportComposeSearchAndDecisionData()
    {
        SearchRequest search = new SearchRequest
        {
            Query = "finalDecidingOfficeName:OFFICE\\ OF\\ PETITIONS",
            OffsetPage = new OffsetPage { Offset = 25L, Limit = 25 }
        };
        PetitionDecision decision = new PetitionDecision
        {
            RecordIdentifier = "decision-1",
            Documents = new PetitionDecisionDocument[] { new PetitionDecisionDocument { DocumentIdentifier = "document-1" } }
        };
        PetitionDecisionFacet facet = new PetitionDecisionFacet
        {
            Field = "decisionTypeCodeDescriptionText",
            Values = new PetitionDecisionFacetValue[]
            {
                new PetitionDecisionFacetValue { Value = "DENIED", Count = 19L },
                new PetitionDecisionFacetValue { Value = "GRANTED", Count = 7L }
            }
        };
        PetitionDecisionResponse response = new PetitionDecisionResponse
        {
            Request = search,
            Decisions = new PetitionDecision[] { decision },
            Facets = new PetitionDecisionFacet[] { facet },
            Count = 1,
            Total = 26L,
            HasTotal = true,
            Page = new OffsetPage { Offset = 25L, Limit = 25 },
            RequestIdentifier = "request-1",
            RawResponse = "{\"count\":1}",
            IsComplete = true,
            Provenance = new PatentDataProvenance { Source = DataProvenance.Odp, FromCache = true }
        };
        PetitionDecisionExportRequest export = new PetitionDecisionExportRequest
        {
            Search = search,
            Format = ExportFormat.Csv
        };

        Assert.Multiple(() =>
        {
            Assert.That(response.Request, Is.SameAs(search));
            Assert.That(response.Decisions[0], Is.SameAs(decision));
            Assert.That(response.Decisions[0].Documents[0].DocumentIdentifier, Is.EqualTo("document-1"));
            Assert.That(response.Facets[0].Field, Is.EqualTo("decisionTypeCodeDescriptionText"));
            Assert.That(response.Facets[0].Values.Select(item => item.Count), Is.EqualTo(new long[] { 19L, 7L }));
            Assert.That(response.Total, Is.EqualTo(26L));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Page.Offset, Is.EqualTo(25L));
            Assert.That(response.Provenance.FromCache, Is.True);
            Assert.That(response.RawResponse, Is.EqualTo("{\"count\":1}"));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(export.Search, Is.SameAs(search));
            Assert.That(export.Format, Is.EqualTo(ExportFormat.Csv));
        });
    }

    [Test]
    public void ResponseAndExportDefaultsAreIndependent()
    {
        PetitionDecisionResponse firstResponse = new PetitionDecisionResponse();
        PetitionDecisionResponse secondResponse = new PetitionDecisionResponse();
        PetitionDecisionExportRequest firstExport = new PetitionDecisionExportRequest();
        PetitionDecisionExportRequest secondExport = new PetitionDecisionExportRequest();

        firstResponse.Request.Query = "decisionTypeCodeDescriptionText:DENIED";
        firstResponse.Page.Offset = 10L;
        firstExport.Search.Query = "firstApplicantName:EXAMPLE";

        Assert.Multiple(() =>
        {
            Assert.That(firstResponse.Decisions, Is.Empty);
            Assert.That(firstResponse.Facets, Is.Empty);
            Assert.That(firstResponse.HasTotal, Is.False);
            Assert.That(firstResponse.IsComplete, Is.False);
            Assert.That(secondResponse.Request.Query, Is.Null);
            Assert.That(secondResponse.Page.Offset, Is.Zero);
            Assert.That(firstResponse.Request, Is.Not.SameAs(secondResponse.Request));
            Assert.That(firstResponse.Page, Is.Not.SameAs(secondResponse.Page));
            Assert.That(firstExport.Format, Is.EqualTo(ExportFormat.Json));
            Assert.That(secondExport.Search.Query, Is.Null);
            Assert.That(firstExport.Search, Is.Not.SameAs(secondExport.Search));
        });
    }
}
