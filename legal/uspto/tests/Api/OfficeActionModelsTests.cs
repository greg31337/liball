using uspto;

namespace uspto.Tests;

public class OfficeActionModelsTests
{
    [Test]
    public void ProsecutionTimelineModelsStartWithSafeIndependentDefaults()
    {
        ProsecutionTimeline first = new ProsecutionTimeline();
        ProsecutionTimeline second = new ProsecutionTimeline();
        ProsecutionTimelineEvent firstEvent = new ProsecutionTimelineEvent();
        ProsecutionTimelineEvent secondEvent = new ProsecutionTimelineEvent();

        first.Events = new[] { firstEvent };
        first.Errors = new[] { new SectionError { Section = "documents" } };
        first.Provenance.DatasetIdentifier = "derived";
        firstEvent.Provenance.DatasetIdentifier = "office-actions";

        Assert.Multiple(() =>
        {
            Assert.That(second.Events, Is.Empty);
            Assert.That(second.Errors, Is.Empty);
            Assert.That(second.Count, Is.Zero);
            Assert.That(second.MatchedOfficeActionCount, Is.Zero);
            Assert.That(second.UnmatchedOfficeActionCount, Is.Zero);
            Assert.That(second.LoadedSectionCount, Is.Zero);
            Assert.That(second.FailedSectionCount, Is.Zero);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.HasPartialSuccess, Is.False);
            Assert.That(second.Provenance, Is.Not.SameAs(first.Provenance));
            Assert.That(second.Provenance.DatasetIdentifier, Is.Null);
            Assert.That(second.OfficeActionProvenance, Is.Not.SameAs(first.OfficeActionProvenance));
            Assert.That(second.TransactionProvenance, Is.Not.SameAs(first.TransactionProvenance));
            Assert.That(second.DocumentProvenance, Is.Not.SameAs(first.DocumentProvenance));
            Assert.That(secondEvent.TransactionMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.None));
            Assert.That(secondEvent.DocumentMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.None));
            Assert.That(secondEvent.IsDerivedMatch, Is.False);
            Assert.That(secondEvent.OfficeAction, Is.Null);
            Assert.That(secondEvent.Transaction, Is.Null);
            Assert.That(secondEvent.Document, Is.Null);
            Assert.That(secondEvent.Provenance, Is.Not.SameAs(firstEvent.Provenance));
            Assert.That(secondEvent.DerivationProvenance.Source, Is.EqualTo(DataProvenance.Unknown));
        });
    }

    [Test]
    public void FieldResponsesStartWithSafeIndependentDefaults()
    {
        OfficeActionFieldResponse first = new OfficeActionFieldResponse();
        OfficeActionFieldResponse second = new OfficeActionFieldResponse();

        first.Fields = new[] { "patentApplicationNumber" };
        first.UnknownFields["futureField"] = "true";
        first.Metadata.DatasetName = "oa_actions";

        Assert.Multiple(() =>
        {
            Assert.That(first.Metadata, Is.Not.SameAs(second.Metadata));
            Assert.That(first.UnknownFields, Is.Not.SameAs(second.UnknownFields));
            Assert.That(second.Metadata.DatasetName, Is.Null);
            Assert.That(second.Metadata.HasLastUpdatedAt, Is.False);
            Assert.That(second.Metadata.LastUpdatedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(second.Fields, Is.Empty);
            Assert.That(second.Count, Is.Zero);
            Assert.That(second.RequestIdentifier, Is.Null);
            Assert.That(second.RawResponse, Is.Null);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.UnknownFields, Is.Empty);
        });
    }

    [Test]
    public void FieldResponseRetainsDatasetSchemaCountAndRawAndParsedUpdateValues()
    {
        DateTimeOffset lastUpdated = new DateTimeOffset(2026, 8, 27, 14, 30, 5, TimeSpan.Zero);
        OfficeActionFieldResponse response = new OfficeActionFieldResponse
        {
            Metadata = new OfficeActionDatasetMetadata
            {
                DatasetName = "oa_citations",
                Version = "v2",
                Status = "PUBLISHED",
                ServiceUri = new Uri("https://api.uspto.gov/api/v1/patent/oa/oa_citations/v2/fields"),
                SchemaUri = new Uri("https://api.uspto.gov/swagger/oa-citations"),
                LastUpdatedText = "2026-08-27 14:30:05.0",
                LastUpdatedAt = lastUpdated,
                HasLastUpdatedAt = true
            },
            Fields = new[] { "patentApplicationNumber", "referenceIdentifier" },
            Count = 2,
            RequestIdentifier = "fields-request",
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                DatasetIdentifier = "oa_citations",
                DatasetVersion = "v2",
                LastUpdatedAt = lastUpdated,
                HasLastUpdatedAt = true,
                IsAuthoritative = true
            },
            RawResponse = "{\"apiKey\":\"oa_citations\"}",
            IsComplete = true,
            UnknownFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["futureMetadata"] = "{\"enabled\":true}"
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(response.Metadata.DatasetName, Is.EqualTo("oa_citations"));
            Assert.That(response.Metadata.Version, Is.EqualTo("v2"));
            Assert.That(response.Metadata.Status, Is.EqualTo("PUBLISHED"));
            Assert.That(response.Metadata.ServiceUri?.AbsolutePath, Is.EqualTo("/api/v1/patent/oa/oa_citations/v2/fields"));
            Assert.That(response.Metadata.SchemaUri?.AbsolutePath, Is.EqualTo("/swagger/oa-citations"));
            Assert.That(response.Metadata.LastUpdatedText, Is.EqualTo("2026-08-27 14:30:05.0"));
            Assert.That(response.Metadata.LastUpdatedAt, Is.EqualTo(lastUpdated));
            Assert.That(response.Metadata.HasLastUpdatedAt, Is.True);
            Assert.That(response.Fields, Is.EqualTo(new[] { "patentApplicationNumber", "referenceIdentifier" }));
            Assert.That(response.Count, Is.EqualTo(2));
            Assert.That(response.Provenance.DatasetIdentifier, Is.EqualTo("oa_citations"));
            Assert.That(response.Provenance.DatasetVersion, Is.EqualTo("v2"));
            Assert.That(response.Provenance.HasLastUpdatedAt, Is.True);
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.UnknownFields.Keys, Is.EqualTo(new[] { "futureMetadata" }));
        });
    }

    [Test]
    public void UnparseableLastUpdateCanRemainRawWithoutClaimingTypedValue()
    {
        OfficeActionDatasetMetadata metadata = new OfficeActionDatasetMetadata
        {
            LastUpdatedText = "March 2026 refresh",
            HasLastUpdatedAt = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(metadata.LastUpdatedText, Is.EqualTo("March 2026 refresh"));
            Assert.That(metadata.LastUpdatedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(metadata.HasLastUpdatedAt, Is.False);
        });
    }

    [Test]
    public void TextModelsStartWithIndependentSafeDefaults()
    {
        OfficeActionTextResponse first = new OfficeActionTextResponse();
        OfficeActionTextResponse second = new OfficeActionTextResponse();
        OfficeActionTextRecord record = new OfficeActionTextRecord();

        first.Records = new[] { record };
        first.UnknownFields["future"] = "true";
        record.Sections.UnknownFields["futureSection"] = "1";

        Assert.Multiple(() =>
        {
            Assert.That(second.Records, Is.Empty);
            Assert.That(second.Count, Is.Zero);
            Assert.That(second.HasTotal, Is.False);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.UnknownFields, Is.Empty);
            Assert.That(first.UnknownFields, Is.Not.SameAs(second.UnknownFields));
            Assert.That(record.ApplicationNumbers, Is.Empty);
            Assert.That(record.BodyText, Is.Empty);
            Assert.That(record.HasApplicationStatusNumber, Is.False);
            Assert.That(record.HasApplicationConfirmationNumber, Is.False);
            Assert.That(record.HasCustomerNumber, Is.False);
            Assert.That(record.HasGroupArtUnitNumber, Is.False);
            Assert.That(record.HasFilingDate, Is.False);
            Assert.That(record.HasSections, Is.False);
            Assert.That(record.Sections.SummaryText, Is.Empty);
            Assert.That(record.Sections.Section101RejectionText, Is.Empty);
            Assert.That(record.Sections.Section102RejectionText, Is.Empty);
            Assert.That(record.Sections.Section103RejectionText, Is.Empty);
            Assert.That(record.Sections.Section112RejectionText, Is.Empty);
            Assert.That(record.Sections.UnknownFields["futureSection"], Is.EqualTo("1"));
        });
    }

    [Test]
    public void CitationModelsStartWithIndependentSafeDefaults()
    {
        OfficeActionCitationResponse first = new OfficeActionCitationResponse();
        OfficeActionCitationResponse second = new OfficeActionCitationResponse();
        OfficeActionCitation citation = new OfficeActionCitation();

        first.Citations = new[] { citation };
        first.UnknownFields["future"] = "true";
        citation.UnknownFields["source"] = "\"future\"";

        Assert.Multiple(() =>
        {
            Assert.That(second.Citations, Is.Empty);
            Assert.That(second.Count, Is.Zero);
            Assert.That(second.HasTotal, Is.False);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.UnknownFields, Is.Empty);
            Assert.That(first.UnknownFields, Is.Not.SameAs(second.UnknownFields));
            Assert.That(citation.Identifier, Is.Null);
            Assert.That(citation.ReferenceIdentifier, Is.Null);
            Assert.That(citation.ParsedReferenceIdentifier, Is.Null);
            Assert.That(citation.CitedIdentifier.VerbatimText, Is.Null);
            Assert.That(citation.CitedIdentifier.CanonicalPatentIdentifier, Is.Null);
            Assert.That(citation.CitedIdentifier.PatentIdentifierKind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(citation.CitedIdentifier.IsClassified, Is.False);
            Assert.That(citation.HasApplicantCitedExaminerReferenceIndicator, Is.False);
            Assert.That(citation.HasExaminerCitedReferenceIndicator, Is.False);
            Assert.That(citation.HasOfficeActionCitationReferenceIndicator, Is.False);
            Assert.That(citation.HasCreatedAt, Is.False);
            Assert.That(citation.UnknownFields["source"], Is.EqualTo("\"future\""));
        });
    }

    [Test]
    public void RejectionModelsStartWithIndependentSafeDefaults()
    {
        OfficeActionRejectionResponse first = new OfficeActionRejectionResponse();
        OfficeActionRejectionResponse second = new OfficeActionRejectionResponse();
        OfficeActionRejection rejection = new OfficeActionRejection();

        first.Rejections = new[] { rejection };
        first.UnknownFields["future"] = "true";
        rejection.UnknownFields["source"] = "\"future\"";

        Assert.Multiple(() =>
        {
            Assert.That(second.Rejections, Is.Empty);
            Assert.That(second.Count, Is.Zero);
            Assert.That(second.HasTotal, Is.False);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.UnknownFields, Is.Empty);
            Assert.That(first.UnknownFields, Is.Not.SameAs(second.UnknownFields));
            Assert.That(rejection.Identifier, Is.Null);
            Assert.That(rejection.ClaimNumbers, Is.Empty);
            Assert.That(rejection.HasSubmissionDate, Is.False);
            Assert.That(rejection.HasCreatedAt, Is.False);
            Assert.That(rejection.HasSection101RejectionState, Is.False);
            Assert.That(rejection.HasMaximumSection103CitationCount, Is.False);
            Assert.That(rejection.HasClosingMissingIndicator, Is.False);
            Assert.That(rejection.HasBilskiIndicator, Is.False);
            Assert.That(rejection.HasAllowedClaimIndicator, Is.False);
            Assert.That(rejection.UnknownFields["source"], Is.EqualTo("\"future\""));
        });
    }

    [Test]
    public void EnrichedCitationModelsStartWithIndependentSafeDefaults()
    {
        OfficeActionEnrichedCitationResponse first = new OfficeActionEnrichedCitationResponse();
        OfficeActionEnrichedCitationResponse second = new OfficeActionEnrichedCitationResponse();
        OfficeActionEnrichedCitation citation = new OfficeActionEnrichedCitation();

        first.Citations = new[] { citation };
        first.UnknownFields["future"] = "true";
        citation.UnknownFields["source"] = "\"future\"";

        Assert.Multiple(() =>
        {
            Assert.That(second.Citations, Is.Empty);
            Assert.That(second.Count, Is.Zero);
            Assert.That(second.HasTotal, Is.False);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.UnknownFields, Is.Empty);
            Assert.That(first.UnknownFields, Is.Not.SameAs(second.UnknownFields));
            Assert.That(citation.Identifier, Is.Null);
            Assert.That(citation.CitedIdentifier.VerbatimText, Is.Null);
            Assert.That(citation.CitedIdentifier.CanonicalPatentIdentifier, Is.Null);
            Assert.That(citation.CitedIdentifier.IsClassified, Is.False);
            Assert.That(citation.RelatedClaimNumbers, Is.Empty);
            Assert.That(citation.PassageLocations, Is.Empty);
            Assert.That(citation.HasOfficeActionDate, Is.False);
            Assert.That(citation.HasCreatedAt, Is.False);
            Assert.That(citation.HasExaminerCitedReferenceIndicator, Is.False);
            Assert.That(citation.HasApplicantCitedExaminerReferenceIndicator, Is.False);
            Assert.That(citation.HasNonPatentLiteratureIndicator, Is.False);
            Assert.That(citation.UnknownFields["source"], Is.EqualTo("\"future\""));
        });
    }
}
