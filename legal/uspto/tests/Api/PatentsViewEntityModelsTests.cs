using uspto;

namespace uspto.Tests;

public class PatentsViewEntityModelsTests
{
    [Test]
    public void EntityAndResponseDefaultsAreResearchDerivedAndIndependent()
    {
        PatentsViewInventor firstInventor = new PatentsViewInventor();
        PatentsViewInventor secondInventor = new PatentsViewInventor();
        PatentsViewAssignee firstAssignee = new PatentsViewAssignee();
        PatentsViewAssignee secondAssignee = new PatentsViewAssignee();
        PatentsViewGovernmentInterest firstInterest = new PatentsViewGovernmentInterest();
        PatentsViewGovernmentInterest secondInterest = new PatentsViewGovernmentInterest();
        PatentsViewInventorResponse firstResponse = new PatentsViewInventorResponse();
        PatentsViewInventorResponse secondResponse = new PatentsViewInventorResponse();

        firstInventor.ActivityYears = new[] { new PatentsViewEntityActivityYear { Year = 2024, HasYear = true } };
        firstInventor.Crosswalks = new[] { new PatentsViewEntityCrosswalk { PersistentEntityIdentifier = "inv-1" } };
        firstInventor.AdditionalFields["future"] = "value";
        firstAssignee.ActivityYears = new[] { new PatentsViewEntityActivityYear { PatentCount = 4, HasPatentCount = true } };
        firstAssignee.Crosswalks = new[] { new PatentsViewEntityCrosswalk { PersistentEntityIdentifier = "asg-1" } };
        firstInterest.AwardNumbers = new[] { "R01-123" };
        firstInterest.Organizations = new[] { new PatentsViewGovernmentOrganization { FederalAgencyName = "NIH" } };
        firstResponse.Inventors = new[] { firstInventor };
        firstResponse.Metadata.RequestIdentifier = "request-1";

        Assert.Multiple(() =>
        {
            Assert.That(secondInventor.ActivityYears, Is.Empty);
            Assert.That(secondInventor.Crosswalks, Is.Empty);
            Assert.That(secondInventor.AdditionalFields, Is.Empty);
            Assert.That(secondInventor.Provenance, Is.Not.SameAs(firstInventor.Provenance));
            Assert.That(secondInventor.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(secondInventor.Provenance.IsAuthoritative, Is.False);
            Assert.That(secondAssignee.ActivityYears, Is.Empty);
            Assert.That(secondAssignee.Crosswalks, Is.Empty);
            Assert.That(secondAssignee.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(secondInterest.AwardNumbers, Is.Empty);
            Assert.That(secondInterest.Organizations, Is.Empty);
            Assert.That(secondInterest.Provenance, Is.Not.SameAs(firstInterest.Provenance));
            Assert.That(secondResponse.Inventors, Is.Empty);
            Assert.That(secondResponse.Metadata, Is.Not.SameAs(firstResponse.Metadata));
            Assert.That(secondResponse.Metadata.RequestIdentifier, Is.Null);
        });
    }

    [Test]
    public void InventorAndAssigneeRetainDisambiguationLocationActivityAndCrosswalks()
    {
        DateTimeOffset firstSeen = new DateTimeOffset(2015, 3, 10, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset lastSeen = new DateTimeOffset(2024, 8, 20, 0, 0, 0, TimeSpan.Zero);
        PatentsViewLocation location = new PatentsViewLocation
        {
            LocationIdentifier = "loc-001",
            Name = "Alexandria",
            State = "Virginia",
            Country = "US"
        };
        PatentsViewEntityActivityYear activity = new PatentsViewEntityActivityYear
        {
            Year = 2024,
            HasYear = true,
            PatentCount = 3,
            HasPatentCount = true
        };
        PatentsViewEntityCrosswalk inventorCrosswalk = new PatentsViewEntityCrosswalk
        {
            EntityType = "inventor",
            SourceEntityIdentifier = "legacy-inventor-7",
            PersistentEntityIdentifier = "inv-persistent-7",
            DisambiguationVersion = "2024.1",
            IsCurrent = true,
            HasIsCurrent = true
        };
        PatentsViewInventor inventor = new PatentsViewInventor
        {
            InventorIdentifier = "inv-persistent-7",
            ResourceReference = "/api/v1/inventor/inv-persistent-7/",
            FirstName = "Grace",
            LastName = "Hopper",
            GenderCode = "F",
            LastKnownLocation = location,
            FirstSeenDate = firstSeen,
            HasFirstSeenDate = true,
            LastSeenDate = lastSeen,
            HasLastSeenDate = true,
            AssigneeCount = 2,
            HasAssigneeCount = true,
            PatentCount = 11,
            HasPatentCount = true,
            YearsActive = 10,
            HasYearsActive = true,
            ActivityYears = new[] { activity },
            Crosswalks = new[] { inventorCrosswalk }
        };
        PatentsViewAssignee assignee = new PatentsViewAssignee
        {
            AssigneeIdentifier = "asg-persistent-4",
            ResourceReference = "/api/v1/assignee/asg-persistent-4/",
            Organization = "Example Research Corporation",
            AssigneeType = "US Company or Corporation",
            LastKnownLocation = location,
            FirstSeenDate = firstSeen,
            HasFirstSeenDate = true,
            LastSeenDate = lastSeen,
            HasLastSeenDate = true,
            InventorCount = 25,
            HasInventorCount = true,
            PatentCount = 40,
            HasPatentCount = true,
            YearsActive = 10,
            HasYearsActive = true,
            ActivityYears = new[] { activity },
            Crosswalks = new[]
            {
                new PatentsViewEntityCrosswalk
                {
                    EntityType = "assignee",
                    SourceEntityIdentifier = "legacy-assignee-4",
                    PersistentEntityIdentifier = "asg-persistent-4"
                }
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(inventor.InventorIdentifier, Is.EqualTo("inv-persistent-7"));
            Assert.That(inventor.LastKnownLocation, Is.SameAs(location));
            Assert.That(inventor.FirstSeenDate, Is.EqualTo(firstSeen));
            Assert.That(inventor.HasLastSeenDate, Is.True);
            Assert.That(inventor.PatentCount, Is.EqualTo(11));
            Assert.That(inventor.ActivityYears.Single().PatentCount, Is.EqualTo(3));
            Assert.That(inventor.Crosswalks.Single().SourceEntityIdentifier, Is.EqualTo("legacy-inventor-7"));
            Assert.That(inventor.Crosswalks.Single().IsCurrent, Is.True);
            Assert.That(assignee.AssigneeIdentifier, Is.EqualTo("asg-persistent-4"));
            Assert.That(assignee.Organization, Is.EqualTo("Example Research Corporation"));
            Assert.That(assignee.LastKnownLocation, Is.SameAs(location));
            Assert.That(assignee.InventorCount, Is.EqualTo(25));
            Assert.That(assignee.Crosswalks.Single().PersistentEntityIdentifier, Is.EqualTo("asg-persistent-4"));
            Assert.That(assignee.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(assignee.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void ApplicantLocationAndAttorneyPreserveIdentifiersOptionalValuesAndDates()
    {
        DateTimeOffset firstSeen = new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset lastSeen = new DateTimeOffset(2025, 6, 30, 0, 0, 0, TimeSpan.Zero);
        PatentsViewApplicant applicant = new PatentsViewApplicant
        {
            ApplicantIdentifier = "applicant-0008",
            PatentIdentifier = "D0345393",
            DocumentNumber = "US20240001234A1",
            ApplicationIdentifier = "09001234",
            FirstName = "Ada",
            LastName = "Lovelace",
            ApplicantType = "applicant-inventor",
            Designation = "applicant",
            Authority = "inventor",
            LocationIdentifier = "loc-0003",
            Sequence = 1,
            HasSequence = true
        };
        PatentsViewLocation location = new PatentsViewLocation
        {
            LocationIdentifier = "loc-0003",
            Name = "Boston",
            County = "Suffolk",
            CountyFipsCode = "025",
            State = "Massachusetts",
            StateFipsCode = "25",
            Country = "US",
            PlaceType = "city",
            Latitude = 42.3601,
            HasLatitude = true,
            Longitude = -71.0589,
            HasLongitude = true,
            AssigneeCount = 14,
            HasAssigneeCount = true,
            PatentCount = 250,
            HasPatentCount = true,
            InventorCount = 85,
            HasInventorCount = true
        };
        PatentsViewAttorney attorney = new PatentsViewAttorney
        {
            AttorneyIdentifier = "attorney-0012",
            FirstName = "Katherine",
            LastName = "Johnson",
            Organization = "Example IP LLP",
            FirstSeenDate = firstSeen,
            HasFirstSeenDate = true,
            LastSeenDate = lastSeen,
            HasLastSeenDate = true,
            InventorCount = 33,
            HasInventorCount = true,
            PatentCount = 52,
            HasPatentCount = true,
            YearsActive = 6,
            HasYearsActive = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(applicant.ApplicationIdentifier, Is.EqualTo("09001234"));
            Assert.That(applicant.Sequence, Is.EqualTo(1));
            Assert.That(applicant.HasSequence, Is.True);
            Assert.That(applicant.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(location.CountyFipsCode, Is.EqualTo("025"));
            Assert.That(location.Latitude, Is.EqualTo(42.3601));
            Assert.That(location.HasLatitude, Is.True);
            Assert.That(location.Longitude, Is.EqualTo(-71.0589));
            Assert.That(location.HasLongitude, Is.True);
            Assert.That(location.PatentCount, Is.EqualTo(250));
            Assert.That(location.HasPatentCount, Is.True);
            Assert.That(attorney.AttorneyIdentifier, Is.EqualTo("attorney-0012"));
            Assert.That(attorney.FirstSeenDate, Is.EqualTo(firstSeen));
            Assert.That(attorney.LastSeenDate, Is.EqualTo(lastSeen));
            Assert.That(attorney.PatentCount, Is.EqualTo(52));
            Assert.That(attorney.HasYearsActive, Is.True);
            Assert.That(attorney.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void GovernmentInterestAndGrantPublicationCrosswalkRetainSourceRelationships()
    {
        PatentsViewGovernmentOrganization agency = new PatentsViewGovernmentOrganization
        {
            FederalAgencyName = "National Institutes of Health",
            LevelOne = "Department of Health and Human Services",
            LevelTwo = "National Institutes of Health",
            LevelThree = "National Cancer Institute"
        };
        PatentsViewGovernmentInterest interest = new PatentsViewGovernmentInterest
        {
            PatentIdentifier = "10905426",
            DocumentNumber = "US20180300001A1",
            Statement = "This invention was made with government support.",
            AwardNumbers = new[] { "R01-CA123456", "P30-CA654321" },
            Organizations = new[] { agency }
        };
        PatentsViewGrantPublicationCrosswalk crosswalk = new PatentsViewGrantPublicationCrosswalk
        {
            PatentIdentifier = "10905426",
            ApplicationIdentifier = "15481996",
            DocumentNumber = "US20180300001A1",
            IsCurrentDocumentNumber = true,
            HasIsCurrentDocumentNumber = true,
            IsCurrentPatentIdentifier = true,
            HasIsCurrentPatentIdentifier = true
        };
        interest.AdditionalFields["source_table"] = "g_gov_interest";
        crosswalk.AdditionalFields["source_table"] = "g_granted_pregrant_crosswalk";

        Assert.Multiple(() =>
        {
            Assert.That(interest.AwardNumbers, Is.EqualTo(new[] { "R01-CA123456", "P30-CA654321" }));
            Assert.That(interest.Organizations.Single(), Is.SameAs(agency));
            Assert.That(agency.LevelThree, Is.EqualTo("National Cancer Institute"));
            Assert.That(interest.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(interest.AdditionalFields["source_table"], Is.EqualTo("g_gov_interest"));
            Assert.That(crosswalk.PatentIdentifier, Is.EqualTo("10905426"));
            Assert.That(crosswalk.ApplicationIdentifier, Is.EqualTo("15481996"));
            Assert.That(crosswalk.DocumentNumber, Is.EqualTo("US20180300001A1"));
            Assert.That(crosswalk.IsCurrentDocumentNumber, Is.True);
            Assert.That(crosswalk.HasIsCurrentPatentIdentifier, Is.True);
            Assert.That(crosswalk.Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void PatentPublicationAndTypedResponsesComposeEntityRecordsWithoutMergingThem()
    {
        PatentsViewApplicant applicant = new PatentsViewApplicant { ApplicantIdentifier = "applicant-1" };
        PatentsViewInventor inventor = new PatentsViewInventor { InventorIdentifier = "inventor-1" };
        PatentsViewAssignee assignee = new PatentsViewAssignee { AssigneeIdentifier = "assignee-1" };
        PatentsViewAttorney attorney = new PatentsViewAttorney { AttorneyIdentifier = "attorney-1" };
        PatentsViewGovernmentInterest interest = new PatentsViewGovernmentInterest { Statement = "Federal support" };
        PatentsViewGrantPublicationCrosswalk crosswalk = new PatentsViewGrantPublicationCrosswalk
        {
            PatentIdentifier = "1234567",
            DocumentNumber = "US20240001234A1"
        };
        PatentsViewPatent patent = new PatentsViewPatent
        {
            PatentIdentifier = "1234567",
            Applicants = new[] { applicant },
            Inventors = new[] { inventor },
            Assignees = new[] { assignee },
            Attorneys = new[] { attorney },
            GovernmentInterests = new[] { interest },
            PublicationCrosswalks = new[] { crosswalk }
        };
        PatentsViewPublication publication = new PatentsViewPublication
        {
            DocumentNumber = "US20240001234A1",
            Applicants = new[] { applicant },
            Inventors = new[] { inventor },
            Assignees = new[] { assignee },
            GovernmentInterests = new[] { interest },
            GrantCrosswalks = new[] { crosswalk }
        };
        PatentsViewAssigneeResponse assigneeResponse = new PatentsViewAssigneeResponse { Assignees = new[] { assignee } };
        PatentsViewLocationResponse locationResponse = new PatentsViewLocationResponse
        {
            Locations = new[] { new PatentsViewLocation { LocationIdentifier = "location-1" } }
        };
        PatentsViewAttorneyResponse attorneyResponse = new PatentsViewAttorneyResponse { Attorneys = new[] { attorney } };

        Assert.Multiple(() =>
        {
            Assert.That(patent.Applicants.Single(), Is.SameAs(applicant));
            Assert.That(patent.Inventors.Single(), Is.SameAs(inventor));
            Assert.That(patent.Assignees.Single(), Is.SameAs(assignee));
            Assert.That(patent.Attorneys.Single(), Is.SameAs(attorney));
            Assert.That(patent.GovernmentInterests.Single(), Is.SameAs(interest));
            Assert.That(patent.PublicationCrosswalks.Single(), Is.SameAs(crosswalk));
            Assert.That(publication.Applicants.Single(), Is.SameAs(applicant));
            Assert.That(publication.GrantCrosswalks.Single(), Is.SameAs(crosswalk));
            Assert.That(assigneeResponse.Assignees.Single(), Is.SameAs(assignee));
            Assert.That(locationResponse.Locations.Single().LocationIdentifier, Is.EqualTo("location-1"));
            Assert.That(attorneyResponse.Attorneys.Single(), Is.SameAs(attorney));
            Assert.That(assigneeResponse.Metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(locationResponse.Metadata.Provenance.IsAuthoritative, Is.False);
        });
    }
}
