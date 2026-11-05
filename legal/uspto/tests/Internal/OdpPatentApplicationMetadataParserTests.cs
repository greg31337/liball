using uspto;

namespace uspto.Tests;

public class OdpPatentApplicationMetadataParserTests
{
    [Test]
    public void CompleteMetadataMapsValuesPartiesClassificationsAndFiles()
    {
        PatentFileWrapper result = Parse(CompleteResponse());
        PatentApplicationMetadata metadata = result.Metadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.ApplicationNumber, Is.EqualTo("14412875"));
            Assert.That(metadata.PatentNumber, Is.EqualTo("12156579"));
            Assert.That(metadata.PublicationNumbers, Is.EqualTo(new string[] { "US 2024/0012345 A1", "US 2024/0012345 A9" }));
            Assert.That(metadata.PublicationSequenceNumbers, Is.EqualTo(new string[] { "1", "2" }));
            Assert.That(metadata.PublicationCategories, Is.EqualTo(new string[] { "PG-Pub", "Correction" }));
            Assert.That(metadata.PublicationDates, Is.EqualTo(new DateTimeOffset[] { UtcDate(2024, 1, 4), UtcDate(2024, 2, 5) }));
            Assert.That(metadata.PctApplicationNumber, Is.EqualTo("PCTUS2025058371"));
            Assert.That(metadata.PctPublicationNumber, Is.EqualTo("WO2026123456"));
            Assert.That(metadata.InternationalRegistrationNumber, Is.EqualTo("DM/123456"));
            Assert.That(metadata.InventionTitle, Is.EqualTo("Example invention"));
            Assert.That(metadata.ApplicationTypeCode, Is.EqualTo("UTL"));
            Assert.That(metadata.ApplicationTypeLabel, Is.EqualTo("Utility"));
            Assert.That(metadata.ApplicationTypeCategory, Is.EqualTo("Regular"));
            Assert.That(metadata.DocketNumber, Is.EqualTo("ACME-42"));
            Assert.That(metadata.ConfirmationNumber, Is.EqualTo("99"));
            Assert.That(metadata.CustomerNumber, Is.EqualTo("76543"));
            Assert.That(metadata.ArtUnit, Is.EqualTo("2814"));
            Assert.That(metadata.TechnologyCenter, Is.EqualTo("2800"));
            Assert.That(metadata.ApplicationStatusCode, Is.EqualTo(150));
            Assert.That(metadata.HasApplicationStatusCode, Is.True);
            Assert.That(metadata.ApplicationStatusDescription, Is.EqualTo("Patented Case"));
            Assert.That(metadata.IsNationalStage, Is.True);
            Assert.That(metadata.HasIsNationalStage, Is.True);
            Assert.That(metadata.IsFirstInventorToFile, Is.False);
            Assert.That(metadata.HasIsFirstInventorToFile, Is.True);
            Assert.That(metadata.FirstApplicantName, Is.EqualTo("Ada Lovelace"));
            Assert.That(metadata.FirstInventorName, Is.EqualTo("Ada Lovelace"));
            Assert.That(metadata.EarliestPublicationNumber, Is.EqualTo("US 2024/0012345 A1"));
            Assert.That(metadata.UnknownFields["futureMetadata"], Is.EqualTo("{ \"enabled\": true }"));
            Assert.That(result.UnknownSections["futureSection"], Is.EqualTo("{ \"revision\": 2 }"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(metadata.FilingDate, Is.EqualTo(UtcDate(2023, 1, 2)));
            Assert.That(metadata.HasFilingDate, Is.True);
            Assert.That(metadata.EffectiveFilingDate, Is.EqualTo(UtcDate(2022, 12, 20)));
            Assert.That(metadata.HasEffectiveFilingDate, Is.True);
            Assert.That(metadata.ApplicationStatusDate, Is.EqualTo(UtcDate(2025, 3, 6)));
            Assert.That(metadata.HasApplicationStatusDate, Is.True);
            Assert.That(metadata.GrantDate, Is.EqualTo(UtcDate(2025, 3, 5)));
            Assert.That(metadata.HasGrantDate, Is.True);
            Assert.That(metadata.EarliestPublicationDate, Is.EqualTo(UtcDate(2024, 1, 4)));
            Assert.That(metadata.HasEarliestPublicationDate, Is.True);
            Assert.That(metadata.PctPublicationDate, Is.EqualTo(UtcDate(2026, 6, 11)));
            Assert.That(metadata.HasPctPublicationDate, Is.True);
            Assert.That(metadata.InternationalRegistrationPublicationDate, Is.EqualTo(UtcDate(2024, 7, 12)));
            Assert.That(metadata.HasInternationalRegistrationPublicationDate, Is.True);
            Assert.That(result.LastIngestedAt, Is.EqualTo(DateTimeOffset.Parse("2025-04-01T12:30:00Z")));
            Assert.That(result.HasLastIngestedAt, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(metadata.EntityStatus.Category, Is.EqualTo("Small"));
            Assert.That(metadata.EntityStatus.IsSmallEntity, Is.False);
            Assert.That(metadata.EntityStatus.HasIsSmallEntity, Is.True);
            Assert.That(metadata.EntityStatus.IsMicroEntity, Is.True);
            Assert.That(metadata.EntityStatus.HasIsMicroEntity, Is.True);
            Assert.That(metadata.Parties.Applicants, Has.Length.EqualTo(2));
            Assert.That(metadata.Parties.Applicants[0].Person?.FullName, Is.EqualTo("Ada Lovelace"));
            Assert.That(metadata.Parties.Applicants[0].Person?.FirstName, Is.EqualTo("Ada"));
            Assert.That(metadata.Parties.Applicants[0].Person?.MiddleName, Is.EqualTo("Byron"));
            Assert.That(metadata.Parties.Applicants[0].Person?.LastName, Is.EqualTo("Lovelace"));
            Assert.That(metadata.Parties.Applicants[0].Person?.PreferredName, Is.EqualTo("Ada"));
            Assert.That(metadata.Parties.Applicants[0].Person?.NamePrefix, Is.EqualTo("Countess"));
            Assert.That(metadata.Parties.Applicants[0].Person?.NameSuffix, Is.EqualTo("FRA"));
            Assert.That(metadata.Parties.Applicants[0].ResidenceCountryCode, Is.EqualTo("GB"));
            Assert.That(metadata.Parties.Applicants[0].Sequence, Is.EqualTo(2));
            Assert.That(metadata.Parties.Applicants[0].HasSequence, Is.True);
            Assert.That(metadata.Parties.Applicants[0].IsInventor, Is.True);
            Assert.That(metadata.Parties.Applicants[0].HasIsInventor, Is.True);
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].NameLineOne, Is.EqualTo("Ada Lovelace"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].NameLineTwo, Is.EqualTo("Analytical Engine Project"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].AddressLineOne, Is.EqualTo("1 Analytical Engine Way"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].AddressLineTwo, Is.EqualTo("Suite 2"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].AddressLineThree, Is.EqualTo("Westminster"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].AddressLineFour, Is.EqualTo("Greater London"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].City, Is.EqualTo("London"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].GeographicRegionName, Is.EqualTo("England"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].GeographicRegionCode, Is.EqualTo("ENG"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].PostalCode, Is.EqualTo("SW1A 1AA"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].CountryCode, Is.EqualTo("GB"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].CountryName, Is.EqualTo("United Kingdom"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].PostalAddressCategory, Is.EqualTo("Correspondence"));
            Assert.That(metadata.Parties.Applicants[0].Addresses[0].EmailAddress, Is.EqualTo("ada@example.test"));
            Assert.That(metadata.Parties.Applicants[1].Organization?.Name, Is.EqualTo("Analytical Engines Ltd."));
            Assert.That(metadata.Parties.Inventors, Has.Length.EqualTo(1));
            Assert.That(metadata.Parties.Inventors[0].Person.FullName, Is.EqualTo("Ada Lovelace"));
            Assert.That(metadata.Parties.Inventors[0].Person.MiddleName, Is.EqualTo("Byron"));
            Assert.That(metadata.Parties.Inventors[0].Person.PreferredName, Is.EqualTo("Ada"));
            Assert.That(metadata.Parties.Inventors[0].Person.NamePrefix, Is.EqualTo("Countess"));
            Assert.That(metadata.Parties.Inventors[0].Person.NameSuffix, Is.EqualTo("FRA"));
            Assert.That(metadata.Parties.Inventors[0].Sequence, Is.EqualTo(1));
            Assert.That(metadata.Parties.Inventors[0].HasSequence, Is.True);
            Assert.That(metadata.Parties.Inventors[0].IsDeceased, Is.False);
            Assert.That(metadata.Parties.Inventors[0].HasIsDeceased, Is.True);
            Assert.That(metadata.Parties.Examiners[0].Person.FullName, Is.EqualTo("Grace Hopper"));
            Assert.That(metadata.Parties.Examiners[0].ArtUnit, Is.EqualTo("2814"));
            Assert.That(metadata.Parties.Examiners[0].TechnologyCenter, Is.EqualTo("2800"));
            Assert.That(metadata.Parties.Assignees, Is.Empty);
            Assert.That(metadata.Parties.Correspondence, Is.Empty);
            Assert.That(metadata.Parties.Attorneys, Is.Empty);
        });

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Classifications.Cpc, Has.Length.EqualTo(2));
            Assert.That(metadata.Classifications.Cpc[0].Symbol, Is.EqualTo("H01L29/66325"));
            Assert.That(metadata.Classifications.Cpc[0].Section, Is.EqualTo("H"));
            Assert.That(metadata.Classifications.Cpc[0].Class, Is.EqualTo("H01"));
            Assert.That(metadata.Classifications.Cpc[0].Subclass, Is.EqualTo("H01L"));
            Assert.That(metadata.Classifications.Cpc[0].MainGroup, Is.EqualTo("29"));
            Assert.That(metadata.Classifications.Cpc[0].Subgroup, Is.EqualTo("66325"));
            Assert.That(metadata.Classifications.Cpc[1].Symbol, Is.EqualTo("G06F 8/00"));
            Assert.That(metadata.Classifications.Cpc[1].Section, Is.EqualTo("G"));
            Assert.That(metadata.Classifications.Cpc[1].Class, Is.EqualTo("G06"));
            Assert.That(metadata.Classifications.Cpc[1].Subclass, Is.EqualTo("G06F"));
            Assert.That(metadata.Classifications.Cpc[1].MainGroup, Is.EqualTo("8"));
            Assert.That(metadata.Classifications.Cpc[1].Subgroup, Is.EqualTo("00"));
            Assert.That(metadata.Classifications.Cpc[1].Version, Is.EqualTo("2025.01"));
            Assert.That(metadata.Classifications.Cpc[1].Sequence, Is.EqualTo(3));
            Assert.That(metadata.Classifications.Cpc[1].HasSequence, Is.True);
            Assert.That(metadata.Classifications.Cpc[1].IsInventive, Is.True);
            Assert.That(metadata.Classifications.Cpc[1].HasIsInventive, Is.True);
            Assert.That(metadata.Classifications.Cpc[1].IsPrimary, Is.False);
            Assert.That(metadata.Classifications.Cpc[1].HasIsPrimary, Is.True);
            Assert.That(metadata.Classifications.Uspc, Has.Length.EqualTo(1));
            Assert.That(metadata.Classifications.Uspc[0].Class, Is.EqualTo("257"));
            Assert.That(metadata.Classifications.Uspc[0].Subclass, Is.EqualTo("197"));
            Assert.That(metadata.Classifications.Uspc[0].Symbol, Is.EqualTo("257/197"));
            Assert.That(metadata.Classifications.Ipc, Is.Empty);
            Assert.That(metadata.Classifications.Wipo, Is.Empty);
            Assert.That(metadata.Classifications.GovernmentInterests, Is.Empty);
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.PublicationFiles, Has.Length.EqualTo(1));
            Assert.That(result.PublicationFiles[0].Category, Is.EqualTo("PreGrantPublication"));
            Assert.That(result.PublicationFiles[0].DocumentIdentifier, Is.EqualTo("US 2024/0012345 A1"));
            Assert.That(result.PublicationFiles[0].ProductIdentifier, Is.EqualTo("APPXML"));
            Assert.That(result.PublicationFiles[0].ArchiveFileName, Is.EqualTo("application.zip"));
            Assert.That(result.PublicationFiles[0].XmlFileName, Is.EqualTo("application.xml"));
            Assert.That(result.PublicationFiles[0].LocationUri, Is.EqualTo(new Uri("https://data.uspto.gov/application.zip")));
            Assert.That(result.PublicationFiles[0].MediaType, Is.EqualTo("application/zip"));
            Assert.That(result.PublicationFiles[0].Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(result.PublicationFiles[0].CreatedAt, Is.EqualTo(DateTimeOffset.Parse("2024-01-04T09:15:00Z")));
            Assert.That(result.PublicationFiles[0].HasCreatedAt, Is.True);
            Assert.That(result.PublicationFiles[0].Size, Is.EqualTo(4294967296L));
            Assert.That(result.PublicationFiles[0].HasSize, Is.True);
            Assert.That(result.PublicationFiles[0].UnknownFields["futureFileField"], Is.EqualTo("{ \"revision\": 3 }"));
            Assert.That(result.GrantFiles, Has.Length.EqualTo(1));
            Assert.That(result.GrantFiles[0].ProductIdentifier, Is.EqualTo("GRANTXML"));
            Assert.That(result.GrantFiles[0].XmlFileName, Is.EqualTo("grant.xml"));
            Assert.That(result.GrantFiles[0].LocationUri, Is.EqualTo(new Uri("https://data.uspto.gov/grant.xml")));
            Assert.That(result.GrantFiles[0].MediaType, Is.EqualTo("application/xml"));
            Assert.That(result.GrantFiles[0].DocumentIdentifier, Is.EqualTo("12156579"));
            Assert.That(result.GrantFiles[0].Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.GrantFiles[0].CreatedAt, Is.EqualTo(UtcDate(2025, 3, 5)));
            Assert.That(result.GrantFiles[0].HasCreatedAt, Is.True);
            Assert.That(result.GrantFiles[0].HasSize, Is.False);
        });
    }

    [Test]
    public void AbsentOptionalNumericBooleanAndDateValuesRemainDistinguishable()
    {
        string response = FixtureLoader.LoadText("patent-applications/metadata-absent.json");

        PatentFileWrapper result = Parse(response);
        PatentApplicationMetadata metadata = result.Metadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.HasApplicationStatusCode, Is.False);
            Assert.That(metadata.ApplicationStatusCode, Is.Zero);
            Assert.That(metadata.HasIsNationalStage, Is.False);
            Assert.That(metadata.IsNationalStage, Is.False);
            Assert.That(metadata.HasIsFirstInventorToFile, Is.False);
            Assert.That(metadata.IsFirstInventorToFile, Is.False);
            Assert.That(metadata.HasFilingDate, Is.False);
            Assert.That(metadata.HasEffectiveFilingDate, Is.False);
            Assert.That(metadata.HasApplicationStatusDate, Is.False);
            Assert.That(metadata.HasGrantDate, Is.False);
            Assert.That(metadata.HasEarliestPublicationDate, Is.False);
            Assert.That(metadata.HasPctPublicationDate, Is.False);
            Assert.That(metadata.HasInternationalRegistrationPublicationDate, Is.False);
            Assert.That(metadata.EntityStatus.HasIsSmallEntity, Is.False);
            Assert.That(metadata.EntityStatus.IsSmallEntity, Is.False);
            Assert.That(metadata.EntityStatus.HasIsMicroEntity, Is.False);
            Assert.That(metadata.EntityStatus.IsMicroEntity, Is.False);
            Assert.That(metadata.Parties.Applicants[0].HasSequence, Is.False);
            Assert.That(metadata.Parties.Applicants[0].Sequence, Is.Zero);
            Assert.That(metadata.Parties.Applicants[0].HasIsInventor, Is.False);
            Assert.That(metadata.Parties.Inventors[0].HasSequence, Is.False);
            Assert.That(metadata.Parties.Inventors[0].Sequence, Is.Zero);
            Assert.That(metadata.Parties.Inventors[0].HasIsDeceased, Is.False);
            Assert.That(metadata.PublicationNumbers, Is.Empty);
            Assert.That(metadata.PublicationDates, Is.Empty);
            Assert.That(result.PublicationFiles, Is.Empty);
            Assert.That(result.GrantFiles, Is.Empty);
            Assert.That(result.HasLastIngestedAt, Is.False);
        });
    }

    [Test]
    public void PresentZeroValuesAndInconsistentSourceDataArePreserved()
    {
        PatentFileWrapper result = Parse(FixtureLoader.LoadText("patent-applications/metadata-zero-inconsistent.json"));
        PatentApplicationMetadata metadata = result.Metadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.ApplicationStatusCode, Is.Zero);
            Assert.That(metadata.HasApplicationStatusCode, Is.True);
            Assert.That(metadata.IsNationalStage, Is.False);
            Assert.That(metadata.HasIsNationalStage, Is.True);
            Assert.That(metadata.IsFirstInventorToFile, Is.False);
            Assert.That(metadata.HasIsFirstInventorToFile, Is.True);
            Assert.That(metadata.EntityStatus.IsSmallEntity, Is.False);
            Assert.That(metadata.EntityStatus.HasIsSmallEntity, Is.True);
            Assert.That(metadata.EntityStatus.IsMicroEntity, Is.False);
            Assert.That(metadata.EntityStatus.HasIsMicroEntity, Is.True);
            Assert.That(metadata.Parties.Applicants[0].Sequence, Is.Zero);
            Assert.That(metadata.Parties.Applicants[0].HasSequence, Is.True);
            Assert.That(metadata.Parties.Applicants[0].IsInventor, Is.False);
            Assert.That(metadata.Parties.Applicants[0].HasIsInventor, Is.True);
            Assert.That(metadata.Parties.Inventors[0].Sequence, Is.Zero);
            Assert.That(metadata.Parties.Inventors[0].HasSequence, Is.True);
            Assert.That(metadata.Parties.Inventors[0].IsDeceased, Is.False);
            Assert.That(metadata.Parties.Inventors[0].HasIsDeceased, Is.True);
            Assert.That(metadata.Classifications.Cpc[0].Sequence, Is.Zero);
            Assert.That(metadata.Classifications.Cpc[0].HasSequence, Is.True);
            Assert.That(metadata.Classifications.Cpc[0].IsInventive, Is.False);
            Assert.That(metadata.Classifications.Cpc[0].HasIsInventive, Is.True);
            Assert.That(metadata.Classifications.Cpc[0].IsPrimary, Is.False);
            Assert.That(metadata.Classifications.Cpc[0].HasIsPrimary, Is.True);
            Assert.That(result.PublicationFiles[0].Size, Is.Zero);
            Assert.That(result.PublicationFiles[0].HasSize, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(metadata.PublicationNumbers, Has.Length.EqualTo(2));
            Assert.That(metadata.PublicationSequenceNumbers, Is.EqualTo(new string[] { "0" }));
            Assert.That(metadata.PublicationCategories, Has.Length.EqualTo(3));
            Assert.That(metadata.PublicationDates, Is.EqualTo(new DateTimeOffset[] { UtcDate(2024, 1, 4) }));
            Assert.That(metadata.FilingDate, Is.EqualTo(UtcDate(2025, 12, 31)));
            Assert.That(metadata.EffectiveFilingDate, Is.EqualTo(UtcDate(2026, 1, 1)));
            Assert.That(metadata.ApplicationStatusDate, Is.EqualTo(UtcDate(2019, 1, 1)));
            Assert.That(metadata.GrantDate, Is.EqualTo(UtcDate(2020, 1, 1)));
            Assert.That(metadata.EarliestPublicationDate, Is.EqualTo(UtcDate(2024, 1, 1)));
            Assert.That(metadata.PctPublicationDate, Is.EqualTo(UtcDate(2023, 1, 1)));
            Assert.That(metadata.InternationalRegistrationPublicationDate, Is.EqualTo(UtcDate(2022, 1, 1)));
        });
    }

    [TestCase("\"nationalStageIndicator\":\"sometimes\"", "$.patentFileWrapperDataBag[0].applicationMetaData.nationalStageIndicator")]
    [TestCase("\"applicationStatusCode\":{}", "$.patentFileWrapperDataBag[0].applicationMetaData.applicationStatusCode")]
    [TestCase("\"publicationDateBag\":[\"not-a-date\"]", "$.patentFileWrapperDataBag[0].applicationMetaData.publicationDateBag[0]")]
    [TestCase("\"pctApplicationNumber\":\"PCT/XX/bad\"", "$.patentFileWrapperDataBag[0].applicationMetaData.pctApplicationNumber")]
    [TestCase("\"applicantBag\":{}", "$.patentFileWrapperDataBag[0].applicationMetaData.applicantBag")]
    [TestCase("\"inventorBag\":[17]", "$.patentFileWrapperDataBag[0].applicationMetaData.inventorBag[0]")]
    [TestCase("\"cpcClassificationBag\":[false]", "$.patentFileWrapperDataBag[0].applicationMetaData.cpcClassificationBag[0]")]
    public void MalformedMetadataValuesReportTheirExactPath(string metadataFragment, string expectedPath)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"applicationMetaData\":{" + metadataFragment + "}}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
    }

    [TestCase("\"pgpubDocumentMetaData\":17", "$.patentFileWrapperDataBag[0].pgpubDocumentMetaData")]
    [TestCase("\"grantDocumentMetaData\":[{\"fileLocationURI\":\"relative/file.xml\"}]", "$.patentFileWrapperDataBag[0].grantDocumentMetaData[0].fileLocationURI")]
    [TestCase("\"grantDocumentMetaData\":[{\"fileCreateDateTime\":\"never\"}]", "$.patentFileWrapperDataBag[0].grantDocumentMetaData[0].fileCreateDateTime")]
    [TestCase("\"grantDocumentMetaData\":[{\"fileSize\":-1}]", "$.patentFileWrapperDataBag[0].grantDocumentMetaData[0].fileSize")]
    public void MalformedFileMetadataReportsItsExactPath(string wrapperFragment, string expectedPath)
    {
        string response = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/412,875\",\"applicationMetaData\":{}," + wrapperFragment + "}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
    }

    private static PatentFileWrapper Parse(string response)
    {
        HttpResponseMetadata metadata = new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/14412875"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        };
        return OdpJsonParser.ParsePatentApplication(response, "14412875", metadata);
    }

    private static DateTimeOffset UtcDate(int year, int month, int day)
    {
        return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    private static string CompleteResponse()
    {
        return FixtureLoader.LoadText("patent-applications/metadata-complete.json");
    }
}
