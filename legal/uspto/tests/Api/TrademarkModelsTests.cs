using uspto;

namespace uspto.Tests;

public class TrademarkModelsTests
{
    [Test]
    public void CaseStatusModelsStartWithSafeIndependentDefaults()
    {
        TrademarkCaseStatus first = new TrademarkCaseStatus();
        TrademarkCaseStatus second = new TrademarkCaseStatus();
        TrademarkGoodsAndService firstGoods = new TrademarkGoodsAndService();
        TrademarkGoodsAndService secondGoods = new TrademarkGoodsAndService();

        first.Mark.Description = "First mark";
        first.Mark.UnknownFields["futureMark"] = "true";
        first.Correspondence.Address.City = "Alexandria";
        first.Madrid.Registrations = new[] { new TrademarkInternationalRegistration { RegistrationNumber = "1234567" } };
        first.UnknownFields["futureCase"] = "{}";
        firstGoods.FilingBasis.Section1A = true;
        firstGoods.Classes = new[] { new TrademarkClass { Code = "009" } };

        Assert.Multiple(() =>
        {
            Assert.That(second.IdentifierKind, Is.EqualTo(TrademarkIdentifierKind.Unknown));
            Assert.That(second.Mark, Is.Not.SameAs(first.Mark));
            Assert.That(second.Mark.Description, Is.Null);
            Assert.That(second.Mark.DesignSearchClassifications, Is.Empty);
            Assert.That(second.Mark.UnknownFields, Is.Empty);
            Assert.That(second.Status, Is.Not.SameAs(first.Status));
            Assert.That(second.FilingBasis, Is.Not.SameAs(first.FilingBasis));
            Assert.That(second.Correspondence, Is.Not.SameAs(first.Correspondence));
            Assert.That(second.Correspondence.Address, Is.Not.SameAs(first.Correspondence.Address));
            Assert.That(second.Correspondence.Address.City, Is.Null);
            Assert.That(second.CurrentOwners, Is.Empty);
            Assert.That(second.PriorOwners, Is.Empty);
            Assert.That(second.Attorneys, Is.Empty);
            Assert.That(second.ProsecutionEvents, Is.Empty);
            Assert.That(second.GoodsAndServices, Is.Empty);
            Assert.That(second.ForeignRegistrations, Is.Empty);
            Assert.That(second.Madrid, Is.Not.SameAs(first.Madrid));
            Assert.That(second.Madrid.Registrations, Is.Empty);
            Assert.That(second.Provenance, Is.Not.SameAs(first.Provenance));
            Assert.That(second.RawFormat, Is.EqualTo(DocumentFormat.Unknown));
            Assert.That(second.RawResponse, Is.Null);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.UnknownFields, Is.Empty);
            Assert.That(secondGoods.FilingBasis, Is.Not.SameAs(firstGoods.FilingBasis));
            Assert.That(secondGoods.FilingBasis.Section1A, Is.False);
            Assert.That(secondGoods.Classes, Is.Empty);
        });
    }

    [Test]
    public void CaseStatusComposesMarkPartiesEventsGoodsClassesAndMadridData()
    {
        DateTimeOffset filingDate = Date(2020, 1, 15);
        DateTimeOffset statusDate = Date(2025, 2, 20);
        DateTimeOffset eventDate = Date(2024, 8, 12);
        DateTimeOffset firstUseDate = Date(2018, 3, 1);
        DateTimeOffset internationalDate = Date(2021, 6, 5);
        TrademarkCaseStatus result = new TrademarkCaseStatus
        {
            RequestedIdentifier = "SN 90123456",
            CanonicalIdentifier = "sn90123456",
            IdentifierKind = TrademarkIdentifierKind.SerialNumber,
            SerialNumber = "90123456",
            RegistrationNumber = "7654321",
            ReferenceNumber = "A0123456",
            InternationalRegistrationNumber = "1234567",
            Mark = new TrademarkMark
            {
                VerbalElementText = "ACME ORBIT",
                Type = "TRADEMARK",
                DrawingCode = "4",
                DrawingDescription = "STANDARD CHARACTER MARK",
                IsStandardCharacter = true,
                HasIsStandardCharacter = true,
                ColorClaimText = "The color blue is claimed.",
                Description = "The mark consists of ACME ORBIT.",
                DisclaimerText = "No claim is made to ORBIT.",
                DesignSearchClassifications = new[]
                {
                    new TrademarkDesignSearchClassification
                    {
                        Code = "01.01.03",
                        Descriptions = new[] { "Stars with five points" }
                    }
                }
            },
            Status = new TrademarkStatus
            {
                Code = 700,
                HasCode = true,
                CodeText = "700",
                ExternalDescription = "REGISTERED",
                Tm5Code = 510,
                HasTm5Code = true,
                Tm5Description = "Registered",
                FilingDate = filingDate,
                HasFilingDate = true,
                FilingDateText = "2020-01-15",
                StatusDate = statusDate,
                HasStatusDate = true,
                StatusDateText = "2025-02-20",
                RegisterCategory = "PRINCIPAL",
                LawOfficeCode = "LAW OFFICE 101",
                ActiveClassCount = 1,
                HasActiveClassCount = true,
                TotalClassCount = 1,
                HasTotalClassCount = true
            },
            FilingBasis = new TrademarkFilingBasis
            {
                Section1A = true,
                HasSection1A = true,
                Section44D = false,
                HasSection44D = true
            },
            CurrentOwners = new[]
            {
                new TrademarkOwner
                {
                    SequenceNumber = 1,
                    HasSequenceNumber = true,
                    Name = "Acme Holdings LLC",
                    EntityTypeCode = "16",
                    EntityTypeDescription = "Limited Liability Company",
                    CitizenshipCountryCode = "US",
                    IsCurrent = true,
                    HasIsCurrent = true,
                    Address = new TrademarkAddress
                    {
                        Line1 = "100 Main Street",
                        City = "Alexandria",
                        StateOrProvince = "VA",
                        PostalCode = "22314",
                        CountryCode = "US"
                    }
                }
            },
            PriorOwners = new[] { new TrademarkOwner { Name = "Acme Company" } },
            Attorneys = new[]
            {
                new TrademarkAttorney
                {
                    EmployeeNumber = "12345",
                    Name = "Jane Q. Lawyer",
                    Organization = "Example Law LLP",
                    DocketNumber = "ACME-100",
                    EmailAddresses = new[] { "jane@example.test" }
                }
            },
            Correspondence = new TrademarkCorrespondence
            {
                Name = "Jane Q. Lawyer",
                Organization = "Example Law LLP",
                EmailAddresses = new[] { "trademarks@example.test" },
                Address = new TrademarkAddress { City = "Washington", StateOrProvince = "DC" }
            },
            ProsecutionEvents = new[]
            {
                new TrademarkProsecutionEvent
                {
                    Code = "R.PR",
                    Description = "Registered-Principal Register",
                    EntryNumber = 12,
                    HasEntryNumber = true,
                    EventDate = eventDate,
                    HasEventDate = true,
                    EventDateText = "2024-08-12",
                    DocumentIdentifier = "TM-DOC-1",
                    DocumentUri = new Uri("https://tsdr.uspto.gov/document/1")
                }
            },
            GoodsAndServices = new[]
            {
                new TrademarkGoodsAndService
                {
                    SerialNumber = "90123456",
                    PrimeClassCode = "009",
                    Description = "Downloadable computer software",
                    StatusCode = "6",
                    StatusDescription = "ACTIVE",
                    FirstUseDate = firstUseDate,
                    HasFirstUseDate = true,
                    FirstUseDateText = "20180301",
                    Classes = new[]
                    {
                        new TrademarkClass
                        {
                            Category = "international",
                            Code = "009",
                            Description = "Scientific apparatus",
                            IsPrime = true,
                            HasIsPrime = true
                        },
                        new TrademarkClass { Category = "us", Code = "021" }
                    },
                    FilingBasis = new TrademarkFilingBasis { Section1A = true, HasSection1A = true }
                }
            },
            ForeignRegistrations = new[]
            {
                new TrademarkForeignRegistration
                {
                    ApplicationNumber = "EU123456",
                    RegistrationNumber = "EU765432",
                    CountryCode = "EM",
                    IsPriorityClaimed = true,
                    HasIsPriorityClaimed = true,
                    FilingDateText = "2020-01-02"
                }
            },
            Madrid = new TrademarkMadridData
            {
                ReferenceNumber = "A0123456",
                InternationalRegistrationNumber = "1234567",
                StatusCode = 10,
                HasStatusCode = true,
                StatusDescription = "Protected",
                Registrations = new[]
                {
                    new TrademarkInternationalRegistration
                    {
                        RegistrationNumber = "1234567",
                        SerialNumber = "90123456",
                        StatusCode = 10,
                        HasStatusCode = true,
                        RegistrationDate = internationalDate,
                        HasRegistrationDate = true,
                        RegistrationDateText = "2021-06-05",
                        FirstRefusal = false,
                        HasFirstRefusal = true
                    }
                },
                Events = new[]
                {
                    new TrademarkMadridEvent
                    {
                        SerialNumber = "M-1",
                        ActionCode = "DESIGNATION",
                        EventDate = internationalDate,
                        HasEventDate = true,
                        EventDateText = "2021-06-05"
                    }
                },
                Relationships = new[]
                {
                    new TrademarkRelationship
                    {
                        SerialNumber = "90123456",
                        RegistrationNumber = "7654321",
                        ReferenceNumber = "A0123456",
                        InternationalRegistrationNumber = "1234567"
                    }
                }
            },
            IsInternationalData = true,
            HasIsInternationalData = true,
            SearchIdentifier = "tsdr-search-1",
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Tsdr,
                DatasetIdentifier = "TSDR case status",
                RequestIdentifier = "tsdr-request-1",
                IsAuthoritative = true
            },
            RawFormat = DocumentFormat.Json,
            RawResponse = "{\"status\":{\"serialNumber\":90123456}}",
            IsComplete = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.CanonicalIdentifier, Is.EqualTo("sn90123456"));
            Assert.That(result.IdentifierKind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(result.Mark.VerbalElementText, Is.EqualTo("ACME ORBIT"));
            Assert.That(result.Mark.DesignSearchClassifications.Single().Code, Is.EqualTo("01.01.03"));
            Assert.That(result.Status.Code, Is.EqualTo(700));
            Assert.That(result.Status.FilingDate, Is.EqualTo(filingDate));
            Assert.That(result.Status.HasFilingDate, Is.True);
            Assert.That(result.FilingBasis.Section1A, Is.True);
            Assert.That(result.CurrentOwners.Single().Address.PostalCode, Is.EqualTo("22314"));
            Assert.That(result.PriorOwners.Single().Name, Is.EqualTo("Acme Company"));
            Assert.That(result.Attorneys.Single().DocketNumber, Is.EqualTo("ACME-100"));
            Assert.That(result.Correspondence.EmailAddresses, Is.EqualTo(new[] { "trademarks@example.test" }));
            Assert.That(result.ProsecutionEvents.Single().EventDate, Is.EqualTo(eventDate));
            Assert.That(result.GoodsAndServices.Single().Classes.Select(item => item.Code), Is.EqualTo(new[] { "009", "021" }));
            Assert.That(result.GoodsAndServices.Single().FirstUseDate, Is.EqualTo(firstUseDate));
            Assert.That(result.ForeignRegistrations.Single().CountryCode, Is.EqualTo("EM"));
            Assert.That(result.Madrid.Registrations.Single().RegistrationDate, Is.EqualTo(internationalDate));
            Assert.That(result.Madrid.Events.Single().ActionCode, Is.EqualTo("DESIGNATION"));
            Assert.That(result.Madrid.Relationships.Single().ReferenceNumber, Is.EqualTo("A0123456"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.RawResponse, Does.Contain("90123456"));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void UnrecognizedDatesRemainAvailableWithoutClaimingParsedValues()
    {
        TrademarkCaseStatus result = new TrademarkCaseStatus
        {
            Status = new TrademarkStatus
            {
                StatusDateText = "0000-00-00",
                HasStatusDate = false
            },
            GoodsAndServices = new[]
            {
                new TrademarkGoodsAndService
                {
                    FirstUseDateText = "19XX-XX-XX",
                    HasFirstUseDate = false
                }
            },
            Madrid = new TrademarkMadridData
            {
                Registrations = new[]
                {
                    new TrademarkInternationalRegistration
                    {
                        CancellationDateText = "not-reported",
                        HasCancellationDate = false
                    }
                }
            },
            RawFormat = DocumentFormat.Xml,
            RawResponse = "<Trademark><StatusDate>0000-00-00</StatusDate></Trademark>"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status.StatusDateText, Is.EqualTo("0000-00-00"));
            Assert.That(result.Status.StatusDate, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(result.Status.HasStatusDate, Is.False);
            Assert.That(result.GoodsAndServices.Single().FirstUseDateText, Is.EqualTo("19XX-XX-XX"));
            Assert.That(result.GoodsAndServices.Single().HasFirstUseDate, Is.False);
            Assert.That(result.Madrid.Registrations.Single().CancellationDateText, Is.EqualTo("not-reported"));
            Assert.That(result.Madrid.Registrations.Single().HasCancellationDate, Is.False);
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.RawResponse, Does.Contain("0000-00-00"));
        });
    }

    private static DateTimeOffset Date(int year, int month, int day)
    {
        return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }
}
