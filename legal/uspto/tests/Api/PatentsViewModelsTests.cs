using uspto;

namespace uspto.Tests;

public class PatentsViewModelsTests
{
    [Test]
    public void QueryAndResponseDefaultsAreCursorBasedResearchDataAndIndependent()
    {
        PatentsViewQueryRequest firstQuery = new PatentsViewQueryRequest();
        PatentsViewQueryRequest secondQuery = new PatentsViewQueryRequest();
        PatentsViewPatentResponse firstPatents = new PatentsViewPatentResponse();
        PatentsViewPatentResponse secondPatents = new PatentsViewPatentResponse();
        PatentsViewPublicationResponse firstPublications = new PatentsViewPublicationResponse();
        PatentsViewPublicationResponse secondPublications = new PatentsViewPublicationResponse();

        firstQuery.Search.CursorPage.AfterValues = new[] { "10905426" };
        firstQuery.Search.FieldSelection.Fields = new[] { "patent_id" };
        firstPatents.Metadata.Provenance.DatasetIdentifier = "g_patent";
        firstPatents.Metadata.AdditionalFields["future"] = "true";
        firstPublications.Metadata.NextCursorPage.AfterValues = new[] { "US20240000001" };

        Assert.Multiple(() =>
        {
            Assert.That(secondQuery.Search, Is.Not.SameAs(firstQuery.Search));
            Assert.That(secondQuery.Search.CursorPage, Is.Not.SameAs(firstQuery.Search.CursorPage));
            Assert.That(secondQuery.Search.UseCursorPage, Is.True);
            Assert.That(secondQuery.Search.Context.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(secondQuery.Search.CursorPage.AfterValues, Is.Empty);
            Assert.That(secondQuery.Search.FieldSelection.Fields, Is.Empty);
            Assert.That(secondQuery.PadPatentIdentifiers, Is.False);
            Assert.That(secondQuery.ExcludeWithdrawnPatents, Is.True);
            Assert.That(secondPatents.Patents, Is.Empty);
            Assert.That(secondPublications.Publications, Is.Empty);
            Assert.That(secondPatents.Metadata, Is.Not.SameAs(firstPatents.Metadata));
            Assert.That(secondPublications.Metadata, Is.Not.SameAs(firstPublications.Metadata));
            Assert.That(secondPatents.Metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(secondPatents.Metadata.Provenance.IsAuthoritative, Is.False);
            Assert.That(secondPatents.Metadata.AdditionalFields, Is.Empty);
            Assert.That(secondPublications.Metadata.NextCursorPage.AfterValues, Is.Empty);
        });
    }

    [Test]
    public void PatentRetainsCoreApplicationDatesCountsAndResearchProvenance()
    {
        DateTimeOffset filingDate = new DateTimeOffset(2017, 4, 7, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset priorityDate = new DateTimeOffset(2016, 4, 8, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset grantDate = new DateTimeOffset(2021, 2, 2, 0, 0, 0, TimeSpan.Zero);
        PatentsViewPatentApplication application = new PatentsViewPatentApplication
        {
            ApplicationIdentifier = "15481996",
            ApplicationType = "utility",
            FilingDate = filingDate,
            HasFilingDate = true,
            FilingType = "regular",
            SeriesCode = "15",
            Rule47 = false,
            HasRule47 = true
        };
        PatentsViewPatent patent = new PatentsViewPatent
        {
            PatentIdentifier = "10905426",
            Title = "Detachable motor powered surgical instrument",
            Abstract = "A surgical instrument.",
            GrantDate = grantDate,
            HasGrantDate = true,
            GrantYear = 2021,
            HasGrantYear = true,
            PatentType = "utility",
            KindCode = "B2",
            IsWithdrawn = false,
            HasIsWithdrawn = true,
            EarliestApplicationDate = priorityDate,
            HasEarliestApplicationDate = true,
            DescriptionLength = 105400,
            HasDescriptionLength = true,
            UnitedStatesPatentCitationCount = 38,
            HasUnitedStatesPatentCitationCount = true,
            UnitedStatesApplicationCitationCount = 12,
            HasUnitedStatesApplicationCitationCount = true,
            ForeignDocumentCitationCount = 7,
            HasForeignDocumentCitationCount = true,
            TotalDocumentCitationCount = 61,
            HasTotalDocumentCitationCount = true,
            CitedByUnitedStatesPatentCount = 19,
            HasCitedByUnitedStatesPatentCount = true,
            ProcessingDays = 1397,
            HasProcessingDays = true,
            TermExtensionDays = 42,
            HasTermExtensionDays = true,
            Applications = new[] { application },
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.PatentsView,
                DatasetIdentifier = "PatentsView PatentSearch",
                IsAuthoritative = false
            }
        };
        patent.AdditionalFields["gov_interest_statement"] = "Research grant statement";

        Assert.Multiple(() =>
        {
            Assert.That(patent.PatentIdentifier, Is.EqualTo("10905426"));
            Assert.That(patent.GrantDate, Is.EqualTo(grantDate));
            Assert.That(patent.HasGrantDate, Is.True);
            Assert.That(patent.EarliestApplicationDate, Is.EqualTo(priorityDate));
            Assert.That(patent.TotalDocumentCitationCount, Is.EqualTo(61));
            Assert.That(patent.HasTotalDocumentCitationCount, Is.True);
            Assert.That(patent.Applications.Single(), Is.SameAs(application));
            Assert.That(application.ApplicationIdentifier, Is.EqualTo("15481996"));
            Assert.That(application.FilingDate, Is.EqualTo(filingDate));
            Assert.That(application.HasRule47, Is.True);
            Assert.That(application.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(application.Provenance.IsAuthoritative, Is.False);
            Assert.That(patent.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(patent.Provenance.IsAuthoritative, Is.False);
            Assert.That(patent.AdditionalFields["gov_interest_statement"], Is.EqualTo("Research grant statement"));
        });
    }

    [Test]
    public void PublicationPreservesDocumentAndSeriesIdentifiersWithOptionalValueState()
    {
        DateTimeOffset publicationDate = new DateTimeOffset(2024, 1, 4, 0, 0, 0, TimeSpan.Zero);
        PatentsViewPublication publication = new PatentsViewPublication
        {
            DocumentNumber = "US20240001234A1",
            Title = "Energy storage controller",
            Abstract = "A controller for an energy storage system.",
            PublicationDate = publicationDate,
            HasPublicationDate = true,
            PublicationYear = 2024,
            HasPublicationYear = true,
            PublicationType = "utility",
            SeriesCode = "09",
            Rule47 = true,
            HasRule47 = true
        };
        publication.AdditionalFields["future_scalar"] = "001";

        Assert.Multiple(() =>
        {
            Assert.That(publication.DocumentNumber, Is.EqualTo("US20240001234A1"));
            Assert.That(publication.SeriesCode, Is.EqualTo("09"));
            Assert.That(publication.PublicationDate, Is.EqualTo(publicationDate));
            Assert.That(publication.HasPublicationDate, Is.True);
            Assert.That(publication.PublicationYear, Is.EqualTo(2024));
            Assert.That(publication.HasPublicationYear, Is.True);
            Assert.That(publication.Rule47, Is.True);
            Assert.That(publication.HasRule47, Is.True);
            Assert.That(publication.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(publication.Provenance.IsAuthoritative, Is.False);
            Assert.That(publication.AdditionalFields["future_scalar"], Is.EqualTo("001"));
        });
    }

    [Test]
    public void TypedResponsesComposeQueryCountsCursorRawDataAndRecords()
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest
        {
            PadPatentIdentifiers = true,
            ExcludeWithdrawnPatents = false
        };
        request.Search.Sorts = new[]
        {
            new SearchSort { Field = "patent_date", Direction = SortDirection.Descending },
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
        };
        PatentsViewResponseMetadata metadata = new PatentsViewResponseMetadata
        {
            Request = request,
            Count = 1,
            TotalHits = 5000000000L,
            HasTotalHits = true,
            NextCursorPage = new CursorPage
            {
                AfterValues = new[] { "2021-02-02", "10905426" },
                Size = 100
            },
            HasNextCursorPage = true,
            IsComplete = false,
            RequestIdentifier = "research-request",
            RawResponse = "{\"count\":1}",
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.PatentsView,
                DatasetIdentifier = "g_patent",
                FromCache = true,
                IsAuthoritative = false
            }
        };
        PatentsViewPatent patent = new PatentsViewPatent { PatentIdentifier = "10905426" };
        PatentsViewPublication publication = new PatentsViewPublication { DocumentNumber = "US20240001234A1" };
        PatentsViewPatentResponse patentResponse = new PatentsViewPatentResponse
        {
            Patents = new[] { patent },
            Metadata = metadata
        };
        PatentsViewPublicationResponse publicationResponse = new PatentsViewPublicationResponse
        {
            Publications = new[] { publication },
            Metadata = metadata
        };

        Assert.Multiple(() =>
        {
            Assert.That(patentResponse.Patents.Single(), Is.SameAs(patent));
            Assert.That(publicationResponse.Publications.Single(), Is.SameAs(publication));
            Assert.That(patentResponse.Metadata, Is.SameAs(metadata));
            Assert.That(metadata.Request, Is.SameAs(request));
            Assert.That(metadata.TotalHits, Is.EqualTo(5000000000L));
            Assert.That(metadata.HasTotalHits, Is.True);
            Assert.That(metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "2021-02-02", "10905426" }));
            Assert.That(metadata.HasNextCursorPage, Is.True);
            Assert.That(metadata.RawResponse, Is.EqualTo("{\"count\":1}"));
            Assert.That(metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(metadata.Provenance.IsAuthoritative, Is.False);
        });
    }
}
