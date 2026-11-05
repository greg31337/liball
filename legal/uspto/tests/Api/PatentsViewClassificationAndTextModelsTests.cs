using uspto;

namespace uspto.Tests;

public class PatentsViewClassificationAndTextModelsTests
{
    [Test]
    public void ClassificationDefaultsAndResponsesAreIndependentResearchData()
    {
        PatentsViewCpcClassification firstCpc = new PatentsViewCpcClassification();
        PatentsViewCpcClassification secondCpc = new PatentsViewCpcClassification();
        PatentsViewIpcClassification firstIpc = new PatentsViewIpcClassification();
        PatentsViewIpcClassification secondIpc = new PatentsViewIpcClassification();
        PatentsViewUspcClassification firstUspc = new PatentsViewUspcClassification();
        PatentsViewUspcClassification secondUspc = new PatentsViewUspcClassification();
        PatentsViewWipoClassification firstWipo = new PatentsViewWipoClassification();
        PatentsViewWipoClassification secondWipo = new PatentsViewWipoClassification();
        PatentsViewCpcClassificationResponse firstResponse = new PatentsViewCpcClassificationResponse();
        PatentsViewCpcClassificationResponse secondResponse = new PatentsViewCpcClassificationResponse();

        firstCpc.AdditionalFields["future"] = "cpc";
        firstIpc.AdditionalFields["future"] = "ipc";
        firstUspc.AdditionalFields["future"] = "uspc";
        firstWipo.AdditionalFields["future"] = "wipo";
        firstResponse.Classifications = new[] { firstCpc };
        firstResponse.Metadata.RequestIdentifier = "classification-request";

        Assert.Multiple(() =>
        {
            Assert.That(secondCpc.AdditionalFields, Is.Empty);
            Assert.That(secondIpc.AdditionalFields, Is.Empty);
            Assert.That(secondUspc.AdditionalFields, Is.Empty);
            Assert.That(secondWipo.AdditionalFields, Is.Empty);
            Assert.That(secondCpc.Provenance, Is.Not.SameAs(firstCpc.Provenance));
            Assert.That(secondCpc.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(secondIpc.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(secondUspc.Provenance.IsAuthoritative, Is.False);
            Assert.That(secondWipo.Provenance.IsAuthoritative, Is.False);
            Assert.That(secondResponse.Classifications, Is.Empty);
            Assert.That(secondResponse.Metadata, Is.Not.SameAs(firstResponse.Metadata));
            Assert.That(secondResponse.Metadata.RequestIdentifier, Is.Null);
            Assert.That(new PatentsViewIpcClassificationResponse().Classifications, Is.Empty);
            Assert.That(new PatentsViewUspcClassificationResponse().Classifications, Is.Empty);
            Assert.That(new PatentsViewWipoClassificationResponse().Classifications, Is.Empty);
        });
    }

    [Test]
    public void ClassificationModelsRetainSchemeSpecificHierarchyActivityAndAssignments()
    {
        DateTimeOffset firstSeen = new DateTimeOffset(2018, 1, 2, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset lastSeen = new DateTimeOffset(2025, 7, 8, 0, 0, 0, TimeSpan.Zero);
        PatentsViewCpcClassification cpc = new PatentsViewCpcClassification
        {
            ClassificationIdentifier = "A61K2039/892",
            Level = "group",
            Title = "Medicinal preparations",
            Section = "A",
            Class = "A61",
            ClassIdentifier = "A61",
            Subclass = "A61K",
            SubclassIdentifier = "A61K",
            Group = "A61K2039/892",
            GroupIdentifier = "A61K2039/892",
            ClassificationType = "inventional",
            Sequence = 0,
            HasSequence = true,
            FirstSeenDate = firstSeen,
            HasFirstSeenDate = true,
            LastSeenDate = lastSeen,
            HasLastSeenDate = true,
            YearsActive = 8,
            HasYearsActive = true,
            PatentCount = 120,
            HasPatentCount = true,
            AssigneeCount = 35,
            HasAssigneeCount = true,
            InventorCount = 75,
            HasInventorCount = true
        };
        PatentsViewIpcClassification ipc = new PatentsViewIpcClassification
        {
            ClassificationIdentifier = "ipc-01",
            Section = "G",
            Class = "G06",
            Subclass = "G06F",
            MainGroup = "16",
            Subgroup = "10",
            DataSource = "H",
            ClassificationValue = "I",
            SymbolPosition = "F",
            ClassificationLevel = "A",
            ClassificationStatus = "B",
            ActionDate = lastSeen,
            HasActionDate = true,
            Sequence = 2,
            HasSequence = true
        };
        PatentsViewUspcClassification uspc = new PatentsViewUspcClassification
        {
            ClassificationIdentifier = "427/286",
            Level = "subclass",
            Title = "Coating processes",
            MainClass = "427",
            MainClassIdentifier = "427",
            Subclass = "286",
            SubclassIdentifier = "427/286",
            Sequence = 1,
            HasSequence = true,
            PatentCount = 300,
            HasPatentCount = true
        };
        PatentsViewWipoClassification wipo = new PatentsViewWipoClassification
        {
            ClassificationIdentifier = "6",
            SectorTitle = "Electrical engineering",
            FieldTitle = "Computer technology",
            Sequence = 3,
            HasSequence = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(cpc.GroupIdentifier, Is.EqualTo("A61K2039/892"));
            Assert.That(cpc.ClassificationType, Is.EqualTo("inventional"));
            Assert.That(cpc.FirstSeenDate, Is.EqualTo(firstSeen));
            Assert.That(cpc.HasInventorCount, Is.True);
            Assert.That(ipc.ClassificationIdentifier, Is.EqualTo("ipc-01"));
            Assert.That(ipc.Subgroup, Is.EqualTo("10"));
            Assert.That(ipc.ActionDate, Is.EqualTo(lastSeen));
            Assert.That(ipc.HasSequence, Is.True);
            Assert.That(uspc.SubclassIdentifier, Is.EqualTo("427/286"));
            Assert.That(uspc.PatentCount, Is.EqualTo(300));
            Assert.That(wipo.SectorTitle, Is.EqualTo("Electrical engineering"));
            Assert.That(wipo.Sequence, Is.EqualTo(3));
            Assert.That(wipo.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
        });
    }

    [Test]
    public void CitationModelRepresentsPatentApplicationForeignAndOtherReferences()
    {
        DateTimeOffset citationDate = new DateTimeOffset(2020, 5, 1, 0, 0, 0, TimeSpan.Zero);
        PatentsViewCitation patentCitation = new PatentsViewCitation
        {
            PatentIdentifier = "10905426",
            SourceResourceReference = "/api/v1/patent/10905426/",
            CitationType = "us_patent",
            CitedPatentIdentifier = "7861317",
            CitedResourceReference = "/api/v1/patent/7861317/",
            CitationCategory = "examiner",
            CitationDate = citationDate,
            HasCitationDate = true,
            CitationName = "Example patent",
            KindCode = "B2",
            Sequence = 0,
            HasSequence = true
        };
        PatentsViewCitation applicationCitation = new PatentsViewCitation
        {
            PatentIdentifier = "10905426",
            CitationType = "us_application",
            CitedDocumentNumber = "US20180300001A1"
        };
        PatentsViewCitation foreignCitation = new PatentsViewCitation
        {
            PatentIdentifier = "10905426",
            CitationType = "foreign",
            CitationNumber = "EP1234567",
            Country = "EP"
        };
        PatentsViewCitation otherReference = new PatentsViewCitation
        {
            PatentIdentifier = "10905426",
            CitationType = "other",
            ReferenceText = "Hopper, The Education of a Computer, 1952",
            Sequence = 8,
            HasSequence = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(patentCitation.CitedPatentIdentifier, Is.EqualTo("7861317"));
            Assert.That(patentCitation.CitationDate, Is.EqualTo(citationDate));
            Assert.That(patentCitation.HasCitationDate, Is.True);
            Assert.That(patentCitation.HasSequence, Is.True);
            Assert.That(applicationCitation.CitedDocumentNumber, Is.EqualTo("US20180300001A1"));
            Assert.That(foreignCitation.CitationNumber, Is.EqualTo("EP1234567"));
            Assert.That(foreignCitation.Country, Is.EqualTo("EP"));
            Assert.That(otherReference.ReferenceText, Does.Contain("Education of a Computer"));
            Assert.That(otherReference.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(otherReference.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void RelatedApplicationAndTextModelsRetainGrantAndPublicationIdentifiers()
    {
        PatentsViewRelatedApplication related = new PatentsViewRelatedApplication
        {
            PatentIdentifier = "10905426",
            DocumentNumber = "US20180300001A1",
            RelatedText = "Continuation of application 14/000,001."
        };
        PatentsViewClaim claim = new PatentsViewClaim
        {
            PatentIdentifier = "10905426",
            ClaimNumber = "00002",
            Sequence = 1,
            HasSequence = true,
            Text = "2. The instrument of claim 1.",
            IsExemplary = true,
            HasIsExemplary = true,
            DependsOnClaim = "00001"
        };
        PatentsViewSummary summary = new PatentsViewSummary
        {
            DocumentNumber = "US20180300001A1",
            Text = "A detachable surgical instrument is provided."
        };
        PatentsViewDescription description = new PatentsViewDescription
        {
            PatentIdentifier = "10905426",
            Text = "Detailed description text.",
            Length = 27,
            HasLength = true
        };
        PatentsViewDrawingDescription drawing = new PatentsViewDrawingDescription
        {
            DocumentNumber = "US20180300001A1",
            Sequence = 1,
            HasSequence = true,
            Text = "FIG. 1 shows the instrument."
        };

        Assert.Multiple(() =>
        {
            Assert.That(related.RelatedText, Does.StartWith("Continuation"));
            Assert.That(claim.ClaimNumber, Is.EqualTo("00002"));
            Assert.That(claim.DependsOnClaim, Is.EqualTo("00001"));
            Assert.That(claim.IsExemplary, Is.True);
            Assert.That(claim.HasIsExemplary, Is.True);
            Assert.That(summary.DocumentNumber, Is.EqualTo("US20180300001A1"));
            Assert.That(description.Length, Is.EqualTo(27));
            Assert.That(description.HasLength, Is.True);
            Assert.That(drawing.Sequence, Is.EqualTo(1));
            Assert.That(drawing.HasSequence, Is.True);
            Assert.That(drawing.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void TextResponseDefaultsAreIndependentAndComposeTypedRecords()
    {
        PatentsViewCitation citation = new PatentsViewCitation { CitedPatentIdentifier = "7861317" };
        PatentsViewRelatedApplication related = new PatentsViewRelatedApplication { RelatedText = "Continuation" };
        PatentsViewClaim claim = new PatentsViewClaim { ClaimNumber = "00001" };
        PatentsViewSummary summary = new PatentsViewSummary { Text = "Summary" };
        PatentsViewDescription description = new PatentsViewDescription { Text = "Description" };
        PatentsViewDrawingDescription drawing = new PatentsViewDrawingDescription { Text = "Drawing" };
        PatentsViewCitationResponse citationResponse = new PatentsViewCitationResponse { Citations = new[] { citation } };
        PatentsViewRelatedApplicationResponse relatedResponse = new PatentsViewRelatedApplicationResponse { RelatedApplications = new[] { related } };
        PatentsViewClaimResponse claimResponse = new PatentsViewClaimResponse { Claims = new[] { claim } };
        PatentsViewSummaryResponse summaryResponse = new PatentsViewSummaryResponse { Summaries = new[] { summary } };
        PatentsViewDescriptionResponse descriptionResponse = new PatentsViewDescriptionResponse { Descriptions = new[] { description } };
        PatentsViewDrawingDescriptionResponse drawingResponse = new PatentsViewDrawingDescriptionResponse { DrawingDescriptions = new[] { drawing } };

        Assert.Multiple(() =>
        {
            Assert.That(citationResponse.Citations.Single(), Is.SameAs(citation));
            Assert.That(relatedResponse.RelatedApplications.Single(), Is.SameAs(related));
            Assert.That(claimResponse.Claims.Single(), Is.SameAs(claim));
            Assert.That(summaryResponse.Summaries.Single(), Is.SameAs(summary));
            Assert.That(descriptionResponse.Descriptions.Single(), Is.SameAs(description));
            Assert.That(drawingResponse.DrawingDescriptions.Single(), Is.SameAs(drawing));
            Assert.That(new PatentsViewCitationResponse().Citations, Is.Empty);
            Assert.That(new PatentsViewRelatedApplicationResponse().RelatedApplications, Is.Empty);
            Assert.That(new PatentsViewClaimResponse().Claims, Is.Empty);
            Assert.That(new PatentsViewSummaryResponse().Summaries, Is.Empty);
            Assert.That(new PatentsViewDescriptionResponse().Descriptions, Is.Empty);
            Assert.That(new PatentsViewDrawingDescriptionResponse().DrawingDescriptions, Is.Empty);
            Assert.That(citationResponse.Metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
        });
    }

    [Test]
    public void PatentAndPublicationComposeClassificationsCitationsAndTextWithoutSharedDefaults()
    {
        PatentsViewCpcClassification cpc = new PatentsViewCpcClassification { GroupIdentifier = "G06F16/10" };
        PatentsViewIpcClassification ipc = new PatentsViewIpcClassification { ClassificationIdentifier = "ipc-1" };
        PatentsViewUspcClassification uspc = new PatentsViewUspcClassification { SubclassIdentifier = "707/999" };
        PatentsViewWipoClassification wipo = new PatentsViewWipoClassification { ClassificationIdentifier = "6" };
        PatentsViewCitation citation = new PatentsViewCitation { CitationType = "us_patent" };
        PatentsViewRelatedApplication related = new PatentsViewRelatedApplication { RelatedText = "Continuation" };
        PatentsViewClaim claim = new PatentsViewClaim { ClaimNumber = "00001" };
        PatentsViewSummary summary = new PatentsViewSummary { Text = "Summary" };
        PatentsViewDescription description = new PatentsViewDescription { Text = "Description" };
        PatentsViewDrawingDescription drawing = new PatentsViewDrawingDescription { Text = "FIG. 1" };
        PatentsViewPatent patent = new PatentsViewPatent
        {
            CpcClassifications = new[] { cpc },
            IpcClassifications = new[] { ipc },
            UspcClassifications = new[] { uspc },
            WipoClassifications = new[] { wipo },
            Citations = new[] { citation },
            RelatedApplications = new[] { related },
            Claims = new[] { claim },
            Summary = summary,
            Description = description,
            DrawingDescriptions = new[] { drawing }
        };
        PatentsViewPublication publication = new PatentsViewPublication
        {
            CpcClassifications = new[] { cpc },
            IpcClassifications = new[] { ipc },
            UspcClassifications = new[] { uspc },
            WipoClassifications = new[] { wipo },
            Citations = new[] { citation },
            RelatedApplications = new[] { related },
            Claims = new[] { claim },
            Summary = summary,
            Description = description,
            DrawingDescriptions = new[] { drawing }
        };
        PatentsViewPatent emptyPatent = new PatentsViewPatent();
        PatentsViewPublication emptyPublication = new PatentsViewPublication();

        Assert.Multiple(() =>
        {
            Assert.That(patent.CpcClassifications.Single(), Is.SameAs(cpc));
            Assert.That(patent.IpcClassifications.Single(), Is.SameAs(ipc));
            Assert.That(patent.UspcClassifications.Single(), Is.SameAs(uspc));
            Assert.That(patent.WipoClassifications.Single(), Is.SameAs(wipo));
            Assert.That(patent.Citations.Single(), Is.SameAs(citation));
            Assert.That(patent.RelatedApplications.Single(), Is.SameAs(related));
            Assert.That(patent.Claims.Single(), Is.SameAs(claim));
            Assert.That(patent.Summary, Is.SameAs(summary));
            Assert.That(patent.Description, Is.SameAs(description));
            Assert.That(patent.DrawingDescriptions.Single(), Is.SameAs(drawing));
            Assert.That(publication.CpcClassifications.Single(), Is.SameAs(cpc));
            Assert.That(publication.Summary, Is.SameAs(summary));
            Assert.That(emptyPatent.CpcClassifications, Is.Empty);
            Assert.That(emptyPatent.Citations, Is.Empty);
            Assert.That(emptyPatent.Summary, Is.Null);
            Assert.That(emptyPublication.Claims, Is.Empty);
            Assert.That(emptyPublication.DrawingDescriptions, Is.Empty);
            Assert.That(emptyPublication.Description, Is.Null);
        });
    }
}
