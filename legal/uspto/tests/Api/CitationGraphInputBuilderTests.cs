using uspto;

namespace uspto.Tests;

public class CitationGraphInputBuilderTests
{
    [Test]
    public void BuilderPairsCanonicalPatentCitationsAndRetainsBothSources()
    {
        OfficeActionCitation official = OfficialPatentCitation("US20190123456A1", IdentifierKind.PatentPublication);
        PatentsViewCitation research = new PatentsViewCitation
        {
            PatentIdentifier = "12345678",
            CitedDocumentNumber = "US20190123456A1",
            CitationType = "us_application",
            CitationCategory = "examiner",
            Provenance = Provenance(DataProvenance.PatentsView, "us_application_citations", false)
        };
        OfficeActionCitationResponse officialResponse = OfficialResponse(official);
        PatentsViewCitationResponse researchResponse = ResearchResponse(research);

        CitationGraphInputSet result = new CitationGraphInputBuilder().BuildCitationGraphInputs(officialResponse, researchResponse);
        CitationGraphInput input = result.Inputs.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.MatchedCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedOfficeActionCount, Is.Zero);
            Assert.That(result.UnmatchedPatentsViewCount, Is.Zero);
            Assert.That(input.TargetIdentifier, Is.EqualTo("US20190123456A1"));
            Assert.That(input.Direction, Is.EqualTo(CitationDirection.Outgoing));
            Assert.That(input.DocumentCategory, Is.EqualTo(CitationDocumentCategory.UnitedStatesApplication));
            Assert.That(input.MatchConfidence, Is.EqualTo(CitationMatchConfidence.Exact));
            Assert.That(input.MatchBasis, Is.EqualTo("canonicalIdentifier"));
            Assert.That(input.Evidence, Has.Length.EqualTo(2));
            Assert.That(input.Evidence[0].Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(input.Evidence[0].CitationCategory, Is.EqualTo("examiner"));
            Assert.That(input.Evidence[0].OfficeActionCitation, Is.SameAs(official));
            Assert.That(input.Evidence[0].Provenance, Is.Not.SameAs(official.Provenance));
            Assert.That(input.Evidence[1].Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(input.Evidence[1].CitationCategory, Is.EqualTo("examiner"));
            Assert.That(input.Evidence[1].PatentsViewCitation, Is.SameAs(research));
            Assert.That(input.Evidence[1].Provenance, Is.Not.SameAs(research.Provenance));
            Assert.That(input.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(input.Provenance.IsAuthoritative, Is.False);
            Assert.That(input.Provenance.FieldSources.Select(item => item.DatasetIdentifier), Is.EqualTo(new[] { "office-action-citations", "us_application_citations" }));
            Assert.That(result.Provenance.FieldSources.Select(item => item.DatasetIdentifier), Is.EqualTo(new[] { "office-action-citations", "patents-view-citations" }));
        });
    }

    [Test]
    public void DuplicateCitationsArePairedOnceAndRetainedInStableSourceOrder()
    {
        OfficeActionCitation official = OfficialPatentCitation("US20190123456A1", IdentifierKind.PatentPublication);
        PatentsViewCitation firstResearch = new PatentsViewCitation
        {
            PatentIdentifier = "source-1",
            CitedDocumentNumber = "US20190123456A1",
            CitationType = "us_application",
            Provenance = Provenance(DataProvenance.PatentsView, "research-first", false)
        };
        PatentsViewCitation duplicateResearch = new PatentsViewCitation
        {
            PatentIdentifier = "source-2",
            CitedDocumentNumber = "US20190123456A1",
            CitationType = "us_application",
            Provenance = Provenance(DataProvenance.PatentsView, "research-duplicate", false)
        };

        CitationGraphInputSet result = new CitationGraphInputBuilder().BuildCitationGraphInputs(
            OfficialResponse(official),
            ResearchResponse(firstResearch, duplicateResearch));

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.MatchedCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedOfficeActionCount, Is.Zero);
            Assert.That(result.UnmatchedPatentsViewCount, Is.EqualTo(1));
            Assert.That(result.Inputs[0].Evidence.Select(item => item.Provenance.DatasetIdentifier), Is.EqualTo(new[] { "office-action-citations", "research-first" }));
            Assert.That(result.Inputs[0].Evidence[1].PatentsViewCitation, Is.SameAs(firstResearch));
            Assert.That(result.Inputs[1].Evidence.Single().PatentsViewCitation, Is.SameAs(duplicateResearch));
            Assert.That(result.Inputs[1].Provenance.FieldSources.Single().DatasetIdentifier, Is.EqualTo("research-duplicate"));
        });
    }

    [Test]
    public void BuilderPreservesNplTextAndUsesNormalizedTextConfidence()
    {
        OfficeActionCitation official = new OfficeActionCitation
        {
            ApplicationNumber = "18123456",
            ReferenceIdentifier = "NPL: Smith  Study",
            CitedIdentifier = new CitedReferenceIdentifier
            {
                VerbatimText = "NPL: Smith  Study",
                IsNonPatentLiterature = true,
                IsClassified = true
            },
            Provenance = Provenance(DataProvenance.Odp, "office-action-citations", true)
        };
        PatentsViewCitation research = new PatentsViewCitation
        {
            PatentIdentifier = "12156579",
            CitationType = "other",
            CitationCategory = "applicant",
            ReferenceText = "npl: smith study",
            Provenance = Provenance(DataProvenance.PatentsView, "other_references", false)
        };

        CitationGraphInputSet result = new CitationGraphInputBuilder().BuildCitationGraphInputs(OfficialResponse(official), ResearchResponse(research));
        CitationGraphInput input = result.Inputs.Single();

        Assert.Multiple(() =>
        {
            Assert.That(input.NonPatentLiterature, Is.EqualTo("NPL: Smith  Study"));
            Assert.That(input.DocumentCategory, Is.EqualTo(CitationDocumentCategory.NonPatentLiterature));
            Assert.That(input.MatchConfidence, Is.EqualTo(CitationMatchConfidence.Medium));
            Assert.That(input.MatchBasis, Is.EqualTo("normalizedNonPatentLiterature"));
            Assert.That(input.Evidence[0].NonPatentLiterature, Is.EqualTo("NPL: Smith  Study"));
            Assert.That(input.Evidence[0].DocumentCategory, Is.EqualTo(CitationDocumentCategory.NonPatentLiterature));
            Assert.That(input.Evidence[1].NonPatentLiterature, Is.EqualTo("npl: smith study"));
            Assert.That(input.Evidence[1].DocumentCategory, Is.EqualTo(CitationDocumentCategory.Other));
        });
    }

    [Test]
    public void BuilderUsesNormalizedIdentifierConfidenceAndKeepsUnmatchedRecordsInStableOrder()
    {
        OfficeActionCitation matchedOfficial = new OfficeActionCitation
        {
            ApplicationNumber = "18123456",
            ReferenceIdentifier = "EP 1234567 A1",
            ParsedReferenceIdentifier = "EP 1234567 A1",
            CitedIdentifier = new CitedReferenceIdentifier
            {
                VerbatimText = "EP 1234567 A1",
                IsPatentDocument = true,
                IsClassified = true
            },
            Provenance = Provenance(DataProvenance.Odp, "office-action-citations", true)
        };
        OfficeActionCitation unmatchedOfficial = OfficialPatentCitation("US7654321B2", IdentifierKind.PatentGrant);
        PatentsViewCitation unmatchedResearch = new PatentsViewCitation
        {
            PatentIdentifier = "100",
            CitedPatentIdentifier = "9999999",
            CitationType = "us_patent",
            Provenance = Provenance(DataProvenance.PatentsView, "us_patent_citations", false)
        };
        PatentsViewCitation matchedResearch = new PatentsViewCitation
        {
            PatentIdentifier = "100",
            CitationNumber = "EP1234567A1",
            CitationType = "foreign",
            Provenance = Provenance(DataProvenance.PatentsView, "foreign_citations", false)
        };

        CitationGraphInputSet result = new CitationGraphInputBuilder().BuildCitationGraphInputs(
            OfficialResponse(matchedOfficial, unmatchedOfficial),
            ResearchResponse(unmatchedResearch, matchedResearch));

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.MatchedCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedOfficeActionCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedPatentsViewCount, Is.EqualTo(1));
            Assert.That(result.Inputs[0].TargetIdentifier, Is.EqualTo("EP 1234567 A1"));
            Assert.That(result.Inputs[0].MatchConfidence, Is.EqualTo(CitationMatchConfidence.High));
            Assert.That(result.Inputs[0].MatchBasis, Is.EqualTo("normalizedIdentifier"));
            Assert.That(result.Inputs[0].DocumentCategory, Is.EqualTo(CitationDocumentCategory.ForeignPatent));
            Assert.That(result.Inputs[1].TargetIdentifier, Is.EqualTo("US7654321B2"));
            Assert.That(result.Inputs[1].Evidence.Single().Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Inputs[2].TargetIdentifier, Is.EqualTo("9999999"));
            Assert.That(result.Inputs[2].Evidence.Single().Source, Is.EqualTo(DataProvenance.PatentsView));
        });
    }

    [Test]
    public void BuilderRejectsMissingResponsesCollectionsAndRecords()
    {
        CitationGraphInputBuilder builder = new CitationGraphInputBuilder();

        ValidationException missingOfficial = Assert.Throws<ValidationException>(() => builder.BuildCitationGraphInputs(null!, new PatentsViewCitationResponse()));
        ValidationException missingResearch = Assert.Throws<ValidationException>(() => builder.BuildCitationGraphInputs(new OfficeActionCitationResponse(), null!));
        ValidationException missingOfficialCollection = Assert.Throws<ValidationException>(() => builder.BuildCitationGraphInputs(new OfficeActionCitationResponse { Citations = null! }, new PatentsViewCitationResponse()));
        ValidationException missingResearchCollection = Assert.Throws<ValidationException>(() => builder.BuildCitationGraphInputs(new OfficeActionCitationResponse(), new PatentsViewCitationResponse { Citations = null! }));
        ValidationException nullOfficial = Assert.Throws<ValidationException>(() => builder.BuildCitationGraphInputs(OfficialResponse((OfficeActionCitation)null!), new PatentsViewCitationResponse()));
        ValidationException nullResearch = Assert.Throws<ValidationException>(() => builder.BuildCitationGraphInputs(new OfficeActionCitationResponse(), ResearchResponse((PatentsViewCitation)null!)));

        Assert.Multiple(() =>
        {
            Assert.That(missingOfficial.FieldName, Is.EqualTo("officeActionCitations"));
            Assert.That(missingResearch.FieldName, Is.EqualTo("patentsViewCitations"));
            Assert.That(missingOfficialCollection.FieldName, Is.EqualTo("officeActionCitations.Citations"));
            Assert.That(missingResearchCollection.FieldName, Is.EqualTo("patentsViewCitations.Citations"));
            Assert.That(nullOfficial.FieldName, Is.EqualTo("officeActionCitations.Citations[0]"));
            Assert.That(nullResearch.FieldName, Is.EqualTo("patentsViewCitations.Citations[0]"));
        });
    }

    [Test]
    public void FactoryExposesBuilderAndModelsHaveStableDefaults()
    {
        using HttpClient transport = new HttpClient(new FactoryTrackingHandler());
        UsptoClientOptions options = new UsptoClientOptions { HttpClient = transport };
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);
        CitationGraphEvidence evidence = new CitationGraphEvidence();
        CitationGraphInput input = new CitationGraphInput();
        CitationGraphInputSet set = new CitationGraphInputSet();

        Assert.Multiple(() =>
        {
            Assert.That(clients.CitationGraphInputBuilder, Is.TypeOf<CitationGraphInputBuilder>());
            Assert.That(evidence.Source, Is.EqualTo(DataProvenance.Unknown));
            Assert.That(evidence.Direction, Is.EqualTo(CitationDirection.Unknown));
            Assert.That(evidence.DocumentCategory, Is.EqualTo(CitationDocumentCategory.Unknown));
            Assert.That(input.MatchConfidence, Is.EqualTo(CitationMatchConfidence.None));
            Assert.That(input.Evidence, Is.Empty);
            Assert.That(set.Inputs, Is.Empty);
            Assert.That(set.Count, Is.Zero);
        });

        clients.Lifetime!.Dispose();
    }

    [Test]
    public void AggregationRetainsAvailableEvidenceAlongsideIndependentSourceFailure()
    {
        OfficeActionCitation official = OfficialPatentCitation("US7654321B2", IdentifierKind.PatentGrant);
        SectionError researchFailure = new SectionError
        {
            Section = "ignored-source-name",
            ExceptionType = nameof(ServiceUnavailableException),
            Message = "PatentsView is transitioning.",
            ErrorCode = "SERVICE_UNAVAILABLE",
            Service = ApiService.PatentsView,
            StatusCode = 503,
            HasStatusCode = true,
            RequestIdentifier = "research-request",
            IsRetryable = true
        };
        CitationGraphAggregationRequest request = new CitationGraphAggregationRequest
        {
            OfficeActionCitations = OfficialResponse(official),
            PatentsViewError = researchFailure
        };

        CitationGraphInputSet result = new CitationGraphInputBuilder().AggregateCitationGraphInputs(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Inputs, Has.Length.EqualTo(1));
            Assert.That(result.Inputs[0].Evidence.Single().OfficeActionCitation, Is.SameAs(official));
            Assert.That(result.LoadedSourceCount, Is.EqualTo(1));
            Assert.That(result.FailedSourceCount, Is.EqualTo(1));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Errors, Has.Length.EqualTo(1));
            Assert.That(result.Errors[0].Section, Is.EqualTo("patentsView"));
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("SERVICE_UNAVAILABLE"));
            Assert.That(result.Errors[0].StatusCode, Is.EqualTo(503));
            Assert.That(result.Errors[0].RequestIdentifier, Is.EqualTo("research-request"));
            Assert.That(result.Errors[0].IsRetryable, Is.True);
            Assert.That(result.Provenance.FieldSources.Select(item => item.FieldPath), Is.EqualTo(new[] { "Inputs.Evidence.OfficeAction" }));
        });
    }

    [Test]
    public void AggregationReportsBothMissingSourcesWithoutClaimingPartialSuccess()
    {
        CitationGraphInputSet result = new CitationGraphInputBuilder().AggregateCitationGraphInputs(new CitationGraphAggregationRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Inputs, Is.Empty);
            Assert.That(result.LoadedSourceCount, Is.Zero);
            Assert.That(result.FailedSourceCount, Is.EqualTo(2));
            Assert.That(result.Errors.Select(item => item.Section), Is.EqualTo(new[] { "officeAction", "patentsView" }));
            Assert.That(result.Errors.Select(item => item.ErrorCode), Is.EqualTo(new[] { "OFFICE_ACTION_SOURCE_UNAVAILABLE", "PATENTSVIEW_SOURCE_UNAVAILABLE" }));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(result.Provenance.FieldSources, Is.Empty);
        });
    }

    [Test]
    public void AggregationRetainsResearchEvidenceWhenOfficeActionSourceFails()
    {
        PatentsViewCitation research = new PatentsViewCitation
        {
            PatentIdentifier = "12156579",
            CitedPatentIdentifier = "7654321",
            CitationType = "us_patent",
            Provenance = Provenance(DataProvenance.PatentsView, "us_patent_citations", false)
        };
        CitationGraphAggregationRequest request = new CitationGraphAggregationRequest
        {
            PatentsViewCitations = ResearchResponse(research),
            OfficeActionError = new SectionError
            {
                ExceptionType = nameof(AuthenticationException),
                ErrorCode = "UNAUTHORIZED",
                Service = ApiService.OfficeAction,
                StatusCode = 401,
                HasStatusCode = true
            }
        };

        CitationGraphInputSet result = new CitationGraphInputBuilder().AggregateCitationGraphInputs(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Inputs, Has.Length.EqualTo(1));
            Assert.That(result.Inputs[0].Evidence.Single().PatentsViewCitation, Is.SameAs(research));
            Assert.That(result.LoadedSourceCount, Is.EqualTo(1));
            Assert.That(result.FailedSourceCount, Is.EqualTo(1));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Errors.Single().Section, Is.EqualTo("officeAction"));
            Assert.That(result.Errors.Single().ErrorCode, Is.EqualTo("UNAUTHORIZED"));
            Assert.That(result.Errors.Single().StatusCode, Is.EqualTo(401));
            Assert.That(result.Provenance.FieldSources.Single().FieldPath, Is.EqualTo("Inputs.Evidence.PatentsView"));
        });
    }

    [Test]
    public void AggregationRejectsConflictingSourceValueAndFailure()
    {
        CitationGraphInputBuilder builder = new CitationGraphInputBuilder();
        CitationGraphAggregationRequest request = new CitationGraphAggregationRequest
        {
            OfficeActionCitations = new OfficeActionCitationResponse(),
            OfficeActionError = new SectionError()
        };

        ValidationException failure = Assert.Throws<ValidationException>(() => builder.AggregateCitationGraphInputs(request));

        Assert.Multiple(() =>
        {
            Assert.That(failure.FieldName, Is.EqualTo("OfficeActionError"));
            Assert.That(failure.ErrorCode, Is.EqualTo("CONFLICTING_SOURCE_OUTCOME"));
        });
    }

    private static OfficeActionCitation OfficialPatentCitation(string identifier, IdentifierKind kind)
    {
        return new OfficeActionCitation
        {
            ApplicationNumber = "18123456",
            ReferenceIdentifier = identifier,
            ParsedReferenceIdentifier = identifier,
            ExaminerCitedReferenceIndicator = true,
            HasExaminerCitedReferenceIndicator = true,
            CitedIdentifier = new CitedReferenceIdentifier
            {
                VerbatimText = identifier,
                CanonicalPatentIdentifier = identifier,
                PatentIdentifierKind = kind,
                IsPatentDocument = true,
                IsClassified = true
            },
            Provenance = Provenance(DataProvenance.Odp, "office-action-citations", true)
        };
    }

    private static OfficeActionCitationResponse OfficialResponse(params OfficeActionCitation[] citations)
    {
        return new OfficeActionCitationResponse
        {
            Citations = citations,
            Provenance = Provenance(DataProvenance.Odp, "office-action-citations", true)
        };
    }

    private static PatentsViewCitationResponse ResearchResponse(params PatentsViewCitation[] citations)
    {
        return new PatentsViewCitationResponse
        {
            Citations = citations,
            Metadata = new PatentsViewResponseMetadata
            {
                Provenance = Provenance(DataProvenance.PatentsView, "patents-view-citations", false)
            }
        };
    }

    private static PatentDataProvenance Provenance(DataProvenance source, string dataset, bool authoritative)
    {
        return new PatentDataProvenance
        {
            Source = source,
            DatasetIdentifier = dataset,
            IsAuthoritative = authoritative
        };
    }
}
