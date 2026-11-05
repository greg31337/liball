using uspto;

namespace uspto.Tests;

public class PatentApplicationModelsTests
{
    [Test]
    public void MetadataDefaultsDistinguishMissingScalarsFromZeroAndFalse()
    {
        PatentApplicationMetadata metadata = new PatentApplicationMetadata();
        PatentEntityStatus entity = metadata.EntityStatus;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.ApplicationNumber, Is.Null);
            Assert.That(metadata.PublicationNumbers, Is.Empty);
            Assert.That(metadata.PublicationSequenceNumbers, Is.Empty);
            Assert.That(metadata.PublicationCategories, Is.Empty);
            Assert.That(metadata.PublicationDates, Is.Empty);
            Assert.That(metadata.ApplicationStatusCode, Is.Zero);
            Assert.That(metadata.HasApplicationStatusCode, Is.False);
            Assert.That(metadata.HasFilingDate, Is.False);
            Assert.That(metadata.HasEffectiveFilingDate, Is.False);
            Assert.That(metadata.HasApplicationStatusDate, Is.False);
            Assert.That(metadata.HasGrantDate, Is.False);
            Assert.That(metadata.HasEarliestPublicationDate, Is.False);
            Assert.That(metadata.HasPctPublicationDate, Is.False);
            Assert.That(metadata.HasInternationalRegistrationPublicationDate, Is.False);
            Assert.That(metadata.IsNationalStage, Is.False);
            Assert.That(metadata.HasIsNationalStage, Is.False);
            Assert.That(metadata.IsFirstInventorToFile, Is.False);
            Assert.That(metadata.HasIsFirstInventorToFile, Is.False);
            Assert.That(entity.Category, Is.Null);
            Assert.That(entity.HasIsSmallEntity, Is.False);
            Assert.That(entity.HasIsMicroEntity, Is.False);
        });
    }

    [Test]
    public void SummaryRetainsSearchFieldsAndAdditionalScalars()
    {
        DateTimeOffset filingDate = new DateTimeOffset(2021, 8, 4, 0, 0, 0, TimeSpan.Zero);
        PatentApplicationSummary summary = new PatentApplicationSummary
        {
            ApplicationNumber = "14412875",
            PatentNumber = "9362380",
            PublicationNumber = "US20140167116A1",
            InventionTitle = "Heterojunction bipolar transistor",
            ApplicationTypeCode = "UTL",
            ApplicationTypeLabel = "Utility",
            FilingDate = filingDate,
            HasFilingDate = true,
            ApplicationStatusCode = 150,
            HasApplicationStatusCode = true,
            ApplicationStatusDescription = "Patented Case",
            FirstApplicantName = "Example Semiconductor S.A.",
            FirstInventorName = "Pascal Chevalier",
            ArtUnit = "2814",
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                IsAuthoritative = true
            }
        };
        summary.AdditionalFields.Add("applicationMetaData.customField", "custom-value");

        Assert.Multiple(() =>
        {
            Assert.That(summary.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(summary.PatentNumber, Is.EqualTo("9362380"));
            Assert.That(summary.PublicationNumber, Is.EqualTo("US20140167116A1"));
            Assert.That(summary.FilingDate, Is.EqualTo(filingDate));
            Assert.That(summary.HasFilingDate, Is.True);
            Assert.That(summary.ApplicationStatusCode, Is.EqualTo(150));
            Assert.That(summary.HasApplicationStatusCode, Is.True);
            Assert.That(summary.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(summary.Provenance.IsAuthoritative, Is.True);
            Assert.That(summary.AdditionalFields["applicationMetaData.customField"], Is.EqualTo("custom-value"));
        });
    }

    [Test]
    public void MetadataComposesIdentifiersDatesPartiesAndClassifications()
    {
        DateTimeOffset filing = new DateTimeOffset(2012, 12, 19, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset publication = new DateTimeOffset(2014, 6, 19, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset grant = new DateTimeOffset(2016, 6, 7, 0, 0, 0, TimeSpan.Zero);
        PatentApplicationMetadata metadata = new PatentApplicationMetadata
        {
            ApplicationNumber = "14104993",
            PatentNumber = "9362380",
            PublicationNumbers = new string[] { "US20140167116A1" },
            PublicationSequenceNumbers = new string[] { "0167116" },
            PublicationCategories = new string[] { "Pre-Grant Publications - PGPub" },
            PublicationDates = new DateTimeOffset[] { publication },
            PctApplicationNumber = "PCT/US2013/012345",
            PctPublicationNumber = "WO2014064413A1",
            InternationalRegistrationNumber = "DM/091304",
            InventionTitle = "Heterojunction bipolar transistor",
            ApplicationTypeCode = "UTL",
            ApplicationTypeLabel = "Utility",
            ApplicationTypeCategory = "electronics",
            DocketNumber = "12GR10425US01",
            ConfirmationNumber = "1061",
            CustomerNumber = "38106",
            ArtUnit = "2814",
            TechnologyCenter = "2800",
            FilingDate = filing,
            HasFilingDate = true,
            EarliestPublicationNumber = "US20140167116A1",
            EarliestPublicationDate = publication,
            HasEarliestPublicationDate = true,
            GrantDate = grant,
            HasGrantDate = true,
            ApplicationStatusCode = 150,
            HasApplicationStatusCode = true,
            ApplicationStatusDescription = "Patented Case",
            IsNationalStage = true,
            HasIsNationalStage = true,
            IsFirstInventorToFile = true,
            HasIsFirstInventorToFile = true,
            FirstApplicantName = "Example Semiconductor S.A.",
            FirstInventorName = "Pascal Chevalier",
            EntityStatus = new PatentEntityStatus
            {
                Category = "Small",
                IsSmallEntity = true,
                HasIsSmallEntity = true
            },
            Parties = new PatentParty
            {
                Inventors = new PatentInventor[]
                {
                    new PatentInventor { Person = new PatentPerson { FullName = "Pascal Chevalier" } }
                }
            },
            Classifications = new PatentClassificationData
            {
                Cpc = new PatentCpcClassification[]
                {
                    new PatentCpcClassification { Symbol = "H01L29/66325" }
                }
            }
        };
        metadata.UnknownFields.Add("futureField", "{\"nested\":true}");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.PublicationNumbers, Is.EqualTo(new string[] { "US20140167116A1" }));
            Assert.That(metadata.PublicationSequenceNumbers[0], Is.EqualTo("0167116"));
            Assert.That(metadata.PublicationDates[0], Is.EqualTo(publication));
            Assert.That(metadata.PctApplicationNumber, Is.EqualTo("PCT/US2013/012345"));
            Assert.That(metadata.ConfirmationNumber, Is.EqualTo("1061"));
            Assert.That(metadata.CustomerNumber, Is.EqualTo("38106"));
            Assert.That(metadata.FilingDate, Is.EqualTo(filing));
            Assert.That(metadata.GrantDate, Is.EqualTo(grant));
            Assert.That(metadata.HasGrantDate, Is.True);
            Assert.That(metadata.IsNationalStage, Is.True);
            Assert.That(metadata.HasIsNationalStage, Is.True);
            Assert.That(metadata.EntityStatus.IsSmallEntity, Is.True);
            Assert.That(metadata.EntityStatus.HasIsSmallEntity, Is.True);
            Assert.That(metadata.Parties.Inventors[0].Person.FullName, Is.EqualTo("Pascal Chevalier"));
            Assert.That(metadata.Classifications.Cpc[0].Symbol, Is.EqualTo("H01L29/66325"));
            Assert.That(metadata.UnknownFields["futureField"], Is.EqualTo("{\"nested\":true}"));
        });
    }

    [Test]
    public void FileMetadataPreservesIdentifiersLocationsDatesAndSizes()
    {
        DateTimeOffset created = new DateTimeOffset(2024, 8, 9, 11, 30, 0, TimeSpan.Zero);
        PatentFileMetadata file = new PatentFileMetadata
        {
            Category = "pre-grant publication",
            DocumentIdentifier = "US20140167116A1",
            ProductIdentifier = "APPXML",
            ArchiveFileName = "ipa240801.zip",
            XmlFileName = "ipa240801.xml",
            LocationUri = new Uri("https://bulkdata.uspto.gov/data/patent/application/ipa240801.zip"),
            MediaType = "application/zip",
            Format = DocumentFormat.Zip,
            CreatedAt = created,
            HasCreatedAt = true,
            Size = 123456789L,
            HasSize = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(file.Category, Is.EqualTo("pre-grant publication"));
            Assert.That(file.DocumentIdentifier, Is.EqualTo("US20140167116A1"));
            Assert.That(file.ProductIdentifier, Is.EqualTo("APPXML"));
            Assert.That(file.ArchiveFileName, Is.EqualTo("ipa240801.zip"));
            Assert.That(file.XmlFileName, Is.EqualTo("ipa240801.xml"));
            Assert.That(file.LocationUri?.Host, Is.EqualTo("bulkdata.uspto.gov"));
            Assert.That(file.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(file.CreatedAt, Is.EqualTo(created));
            Assert.That(file.HasCreatedAt, Is.True);
            Assert.That(file.Size, Is.EqualTo(123456789L));
            Assert.That(file.HasSize, Is.True);
        });
    }

    [Test]
    public void WrapperComposesSummaryMetadataFilesAndProvenance()
    {
        PatentFileMetadata publication = new PatentFileMetadata { ProductIdentifier = "APPXML" };
        PatentFileMetadata grant = new PatentFileMetadata { ProductIdentifier = "PTGRXML" };
        PatentFileWrapper wrapper = new PatentFileWrapper
        {
            ApplicationNumber = "14104993",
            Summary = new PatentApplicationSummary { ApplicationNumber = "14104993" },
            Metadata = new PatentApplicationMetadata { ApplicationNumber = "14104993" },
            PublicationFiles = new PatentFileMetadata[] { publication },
            GrantFiles = new PatentFileMetadata[] { grant },
            LastIngestedAt = new DateTimeOffset(2024, 9, 15, 21, 19, 1, TimeSpan.Zero),
            HasLastIngestedAt = true,
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                RequestIdentifier = "request-123"
            }
        };
        wrapper.UnknownSections.Add("assignmentBag", "[]");

        Assert.Multiple(() =>
        {
            Assert.That(wrapper.Summary.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(wrapper.Metadata.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(wrapper.PublicationFiles[0], Is.SameAs(publication));
            Assert.That(wrapper.GrantFiles[0], Is.SameAs(grant));
            Assert.That(wrapper.HasLastIngestedAt, Is.True);
            Assert.That(wrapper.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(wrapper.Provenance.RequestIdentifier, Is.EqualTo("request-123"));
            Assert.That(wrapper.UnknownSections["assignmentBag"], Is.EqualTo("[]"));
        });
    }

    [Test]
    public void FacetsAndResponseRetainCountsPagingAndRawData()
    {
        SearchRequest request = new SearchRequest { Query = "applicationMetaData.applicationTypeCode:UTL" };
        PatentApplicationSummary summary = new PatentApplicationSummary { ApplicationNumber = "14104993" };
        PatentFileWrapper wrapper = new PatentFileWrapper { ApplicationNumber = "14104993" };
        PatentApplicationResponse response = new PatentApplicationResponse
        {
            Request = request,
            Summaries = new PatentApplicationSummary[] { summary },
            Applications = new PatentFileWrapper[] { wrapper },
            Facets = new PatentApplicationFacet[]
            {
                new PatentApplicationFacet
                {
                    Field = "applicationMetaData.applicationTypeCode",
                    Values = new PatentApplicationFacetValue[]
                    {
                        new PatentApplicationFacetValue { Value = "UTL", Count = 44673L }
                    }
                }
            },
            Count = 1,
            Total = 44673L,
            HasTotal = true,
            Page = new OffsetPage { Offset = 25L, Limit = 25 },
            RequestIdentifier = "request-456",
            Provenance = new PatentDataProvenance { Source = DataProvenance.Odp },
            RawResponse = "{\"count\":1}",
            IsComplete = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(response.Request, Is.SameAs(request));
            Assert.That(response.Summaries[0], Is.SameAs(summary));
            Assert.That(response.Applications[0], Is.SameAs(wrapper));
            Assert.That(response.Facets[0].Field, Is.EqualTo("applicationMetaData.applicationTypeCode"));
            Assert.That(response.Facets[0].Values[0].Count, Is.EqualTo(44673L));
            Assert.That(response.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(44673L));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Page.Offset, Is.EqualTo(25L));
            Assert.That(response.RequestIdentifier, Is.EqualTo("request-456"));
            Assert.That(response.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(response.RawResponse, Is.EqualTo("{\"count\":1}"));
            Assert.That(response.IsComplete, Is.True);
        });
    }

    [Test]
    public void ProvenanceDistinguishesSourceTimesCacheAndAuthority()
    {
        DateTimeOffset retrieved = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset updated = new DateTimeOffset(2026, 9, 1, 8, 30, 0, TimeSpan.Zero);
        PatentDataProvenance provenance = new PatentDataProvenance
        {
            Source = DataProvenance.Odp,
            SourceUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14104993"),
            DatasetIdentifier = "patent-file-wrapper",
            DatasetVersion = "v1",
            RequestIdentifier = "request-789",
            RetrievedAt = retrieved,
            HasRetrievedAt = true,
            LastUpdatedAt = updated,
            HasLastUpdatedAt = true,
            FromCache = true,
            IsAuthoritative = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(provenance.SourceUri?.Host, Is.EqualTo("api.uspto.gov"));
            Assert.That(provenance.DatasetIdentifier, Is.EqualTo("patent-file-wrapper"));
            Assert.That(provenance.DatasetVersion, Is.EqualTo("v1"));
            Assert.That(provenance.RequestIdentifier, Is.EqualTo("request-789"));
            Assert.That(provenance.RetrievedAt, Is.EqualTo(retrieved));
            Assert.That(provenance.HasRetrievedAt, Is.True);
            Assert.That(provenance.LastUpdatedAt, Is.EqualTo(updated));
            Assert.That(provenance.HasLastUpdatedAt, Is.True);
            Assert.That(provenance.FromCache, Is.True);
            Assert.That(provenance.IsAuthoritative, Is.True);
        });
    }

    [Test]
    public void MutableApplicationDefaultsAreIndependent()
    {
        PatentApplicationMetadata firstMetadata = new PatentApplicationMetadata();
        PatentApplicationMetadata secondMetadata = new PatentApplicationMetadata();
        PatentApplicationResponse firstResponse = new PatentApplicationResponse();
        PatentApplicationResponse secondResponse = new PatentApplicationResponse();

        firstMetadata.PublicationNumbers = new string[] { "US20140167116A1" };
        firstMetadata.Parties.Inventors = new PatentInventor[] { new PatentInventor() };
        firstMetadata.UnknownFields.Add("one", "1");
        firstResponse.Page.Offset = 100L;
        firstResponse.Provenance.Source = DataProvenance.Odp;

        Assert.Multiple(() =>
        {
            Assert.That(secondMetadata.PublicationNumbers, Is.Empty);
            Assert.That(firstMetadata.EntityStatus, Is.Not.SameAs(secondMetadata.EntityStatus));
            Assert.That(firstMetadata.Parties, Is.Not.SameAs(secondMetadata.Parties));
            Assert.That(secondMetadata.Parties.Inventors, Is.Empty);
            Assert.That(firstMetadata.UnknownFields, Is.Not.SameAs(secondMetadata.UnknownFields));
            Assert.That(secondMetadata.UnknownFields, Is.Empty);
            Assert.That(firstResponse.Page, Is.Not.SameAs(secondResponse.Page));
            Assert.That(secondResponse.Page.Offset, Is.Zero);
            Assert.That(firstResponse.Provenance, Is.Not.SameAs(secondResponse.Provenance));
            Assert.That(secondResponse.Provenance.Source, Is.EqualTo(DataProvenance.Unknown));
        });
    }
}
