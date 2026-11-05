using uspto;

namespace uspto.Tests;

public class PtabModelsTests
{
    [Test]
    public void ProceedingRetainsLifecyclePartiesCounselAndProvenance()
    {
        DateTimeOffset petitionDate = new DateTimeOffset(2024, 1, 30, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset institutionDate = new DateTimeOffset(2024, 8, 20, 0, 0, 0, TimeSpan.Zero);
        PtabTrialProceeding proceeding = new PtabTrialProceeding
        {
            TrialNumber = "IPR2024-00001",
            Metadata = new PtabTrialMetadata
            {
                TrialTypeCode = "IPR",
                StatusCategory = "Instituted",
                PetitionFilingDate = petitionDate,
                HasPetitionFilingDate = true,
                InstitutionDecisionDate = institutionDate,
                HasInstitutionDecisionDate = true
            },
            PatentOwner = new PtabPatentOwner
            {
                ApplicationNumber = "14412875",
                PatentNumber = "US9876543B2",
                Name = "XYZ Holdings LLC",
                RealPartyInInterestName = "ABC Corp.",
                InventorName = "Liam Johnson",
                ArtUnit = "3689",
                TechnologyCenter = "3600",
                Counsel = new PtabCounsel { Name = "Jane Q. Attorney" }
            },
            RegularPetitioner = new PtabPetitioner
            {
                Category = "Regular",
                RealPartyInInterestName = "DEF Inc.",
                Counsel = new PtabCounsel { Name = "John D. Counsel" }
            },
            Respondent = new PtabRespondent
            {
                RealPartyInInterestName = "GHI LLC",
                Counsel = new PtabCounsel { Name = "Mary R. Lawyer" }
            },
            DerivationPetitioner = new PtabPetitioner
            {
                Category = "Derivation",
                ApplicationNumber = "13112233"
            },
            Provenance = new PtabDataProvenance
            {
                Source = DataProvenance.Odp,
                Collection = PtabCollection.TrialProceedings,
                HasCollection = true,
                RequestIdentifier = "request-1",
                IsAuthoritative = true
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(proceeding.TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(proceeding.Metadata.TrialTypeCode, Is.EqualTo("IPR"));
            Assert.That(proceeding.Metadata.PetitionFilingDate, Is.EqualTo(petitionDate));
            Assert.That(proceeding.Metadata.HasPetitionFilingDate, Is.True);
            Assert.That(proceeding.Metadata.InstitutionDecisionDate, Is.EqualTo(institutionDate));
            Assert.That(proceeding.PatentOwner.PatentNumber, Is.EqualTo("US9876543B2"));
            Assert.That(proceeding.PatentOwner.Counsel.Name, Is.EqualTo("Jane Q. Attorney"));
            Assert.That(proceeding.RegularPetitioner.Counsel.Name, Is.EqualTo("John D. Counsel"));
            Assert.That(proceeding.Respondent.Counsel.Name, Is.EqualTo("Mary R. Lawyer"));
            Assert.That(proceeding.DerivationPetitioner.Category, Is.EqualTo("Derivation"));
            Assert.That(proceeding.Provenance.Collection, Is.EqualTo(PtabCollection.TrialProceedings));
            Assert.That(proceeding.Provenance.HasCollection, Is.True);
            Assert.That(proceeding.Provenance.IsAuthoritative, Is.True);
        });
    }

    [Test]
    public void TrialDocumentRetainsFilingContentDownloadAndDecisionData()
    {
        DateTimeOffset filingDate = new DateTimeOffset(2024, 4, 12, 0, 0, 0, TimeSpan.Zero);
        PtabTrialDocument record = new PtabTrialDocument
        {
            RecordCategory = "document",
            TrialNumber = "IPR2024-00001",
            TrialTypeCode = "IPR",
            Document = new PtabDocument
            {
                DocumentIdentifier = "100012345",
                DocumentNumber = 1,
                HasDocumentNumber = true,
                Name = "petition.pdf",
                Category = "Petition",
                FilingPartyCategory = "Petitioner",
                Title = "Petition",
                TypeDescription = "Petition Type",
                FilingDate = filingDate,
                HasFilingDate = true,
                Size = 123456L,
                HasSize = true,
                MimeTypeIdentifier = "application/pdf",
                MediaType = UsptoMediaTypes.Pdf,
                Format = DocumentFormat.Pdf,
                DownloadUri = new Uri("https://ptab.uspto.gov/doc/100012345"),
                OcrText = "petition text",
                Provenance = new PtabDataProvenance
                {
                    Collection = PtabCollection.TrialDocuments,
                    HasCollection = true
                }
            },
            Decision = new PtabDecision
            {
                TypeCategory = "Institution Decision",
                TrialOutcomeCategory = "Instituted"
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(record.TrialNumber, Is.EqualTo("IPR2024-00001"));
            Assert.That(record.Document.DocumentIdentifier, Is.EqualTo("100012345"));
            Assert.That(record.Document.DocumentNumber, Is.EqualTo(1));
            Assert.That(record.Document.HasDocumentNumber, Is.True);
            Assert.That(record.Document.FilingDate, Is.EqualTo(filingDate));
            Assert.That(record.Document.HasFilingDate, Is.True);
            Assert.That(record.Document.Size, Is.EqualTo(123456L));
            Assert.That(record.Document.HasSize, Is.True);
            Assert.That(record.Document.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(record.Document.DownloadUri?.Host, Is.EqualTo("ptab.uspto.gov"));
            Assert.That(record.Document.OcrText, Is.EqualTo("petition text"));
            Assert.That(record.Document.Provenance.Collection, Is.EqualTo(PtabCollection.TrialDocuments));
            Assert.That(record.Decision.TrialOutcomeCategory, Is.EqualTo("Instituted"));
        });
    }

    [Test]
    public void TrialDecisionResponseComposesOutcomeDocumentFacetsAndPaging()
    {
        DateTimeOffset issueDate = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero);
        PtabTrialDecision decision = new PtabTrialDecision
        {
            TrialNumber = "IPR2024-00001",
            Document = new PtabDocument
            {
                DocumentIdentifier = "decision-1",
                OcrText = "final written decision"
            },
            Decision = new PtabDecision
            {
                IssueDate = issueDate,
                HasIssueDate = true,
                TypeCategory = "Final Written Decision",
                TrialOutcomeCategory = "Denied",
                StatutesAndRules = new string[] { "37 CFR 42.100", "35 USC 103" },
                IssueTypes = new string[] { "103", "112" }
            }
        };
        PtabFacet facet = new PtabFacet
        {
            Field = "trialStatusCategory",
            Values = new PtabFacetValue[]
            {
                new PtabFacetValue { Name = "Terminated", Count = 16951L }
            }
        };
        SearchRequest search = new SearchRequest { Query = "trialTypeCode:IPR" };
        PtabTrialDecisionResponse response = new PtabTrialDecisionResponse
        {
            Request = search,
            Decisions = new PtabTrialDecision[] { decision },
            Facets = new PtabFacet[] { facet },
            Count = 1,
            Total = 1L,
            HasTotal = true,
            Page = new OffsetPage { Offset = 0L, Limit = 25 },
            RequestIdentifier = "request-2",
            IsComplete = true,
            Provenance = new PtabDataProvenance
            {
                Source = DataProvenance.Odp,
                Collection = PtabCollection.TrialDecisions,
                HasCollection = true
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(response.Request, Is.SameAs(search));
            Assert.That(response.Decisions[0].Decision.IssueDate, Is.EqualTo(issueDate));
            Assert.That(response.Decisions[0].Decision.HasIssueDate, Is.True);
            Assert.That(response.Decisions[0].Decision.StatutesAndRules, Has.Length.EqualTo(2));
            Assert.That(response.Decisions[0].Decision.IssueTypes, Is.EqualTo(new[] { "103", "112" }));
            Assert.That(response.Decisions[0].Document.OcrText, Does.Contain("decision"));
            Assert.That(response.Facets[0].Field, Is.EqualTo("trialStatusCategory"));
            Assert.That(response.Facets[0].Values[0].Count, Is.EqualTo(16951L));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.Provenance.Collection, Is.EqualTo(PtabCollection.TrialDecisions));
        });
    }

    [Test]
    public void PtabMutableDefaultsAreIndependentAndOptionalValuesStartAbsent()
    {
        PtabTrialProceedingResponse firstProceedings = new PtabTrialProceedingResponse();
        PtabTrialProceedingResponse secondProceedings = new PtabTrialProceedingResponse();
        PtabTrialDocumentResponse firstDocuments = new PtabTrialDocumentResponse();
        PtabTrialDocumentResponse secondDocuments = new PtabTrialDocumentResponse();
        PtabTrialDecisionResponse firstDecisions = new PtabTrialDecisionResponse();
        PtabTrialDecisionResponse secondDecisions = new PtabTrialDecisionResponse();
        PtabTrialProceeding firstRecord = new PtabTrialProceeding();
        PtabTrialProceeding secondRecord = new PtabTrialProceeding();
        PtabDocument document = new PtabDocument();
        PtabDecision decision = new PtabDecision();

        firstProceedings.Request.Query = "trialTypeCode:IPR";
        firstProceedings.Page.Offset = 25L;
        firstDocuments.Facets = new PtabFacet[] { new PtabFacet() };
        firstDecisions.Provenance.HasCollection = true;
        firstRecord.PatentOwner.Counsel.UnknownFields.Add("role", "lead");
        firstRecord.Metadata.UnknownFields.Add("futureDate", "2026-01-01");

        Assert.Multiple(() =>
        {
            Assert.That(firstRecord.HasLastModifiedAt, Is.False);
            Assert.That(firstRecord.Metadata.HasPetitionFilingDate, Is.False);
            Assert.That(firstRecord.Metadata.HasAccordedFilingDate, Is.False);
            Assert.That(firstRecord.Metadata.HasInstitutionDecisionDate, Is.False);
            Assert.That(firstRecord.Metadata.HasLatestDecisionDate, Is.False);
            Assert.That(firstRecord.Metadata.HasTerminationDate, Is.False);
            Assert.That(firstRecord.Metadata.HasLastModifiedAt, Is.False);
            Assert.That(firstRecord.Metadata.HasLastModifiedDate, Is.False);
            Assert.That(document.HasDocumentNumber, Is.False);
            Assert.That(document.HasFilingDate, Is.False);
            Assert.That(document.HasSize, Is.False);
            Assert.That(document.Format, Is.EqualTo(DocumentFormat.Unknown));
            Assert.That(decision.HasIssueDate, Is.False);
            Assert.That(decision.StatutesAndRules, Is.Empty);
            Assert.That(decision.IssueTypes, Is.Empty);
            Assert.That(secondProceedings.Request.Query, Is.Null);
            Assert.That(secondProceedings.Page.Offset, Is.Zero);
            Assert.That(secondDocuments.Facets, Is.Empty);
            Assert.That(secondDecisions.Provenance.HasCollection, Is.False);
            Assert.That(secondRecord.PatentOwner.Counsel.UnknownFields, Is.Empty);
            Assert.That(secondRecord.Metadata.UnknownFields, Is.Empty);
            Assert.That(firstProceedings.Request, Is.Not.SameAs(secondProceedings.Request));
            Assert.That(firstProceedings.Page, Is.Not.SameAs(secondProceedings.Page));
            Assert.That(firstRecord.PatentOwner, Is.Not.SameAs(secondRecord.PatentOwner));
            Assert.That(firstRecord.PatentOwner.Counsel, Is.Not.SameAs(secondRecord.PatentOwner.Counsel));
            Assert.That(firstRecord.Metadata, Is.Not.SameAs(secondRecord.Metadata));
        });
    }
}
