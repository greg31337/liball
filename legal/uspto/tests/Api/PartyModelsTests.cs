using uspto;

namespace uspto.Tests;

public class PartyModelsTests
{
    [Test]
    public void PersonOrganizationAndAddressRetainSourceComponents()
    {
        PatentPerson person = new PatentPerson
        {
            FullName = "Dr. Ada M. Lovelace Jr.",
            FirstName = "Ada",
            MiddleName = "M.",
            LastName = "Lovelace",
            PreferredName = "Ada Lovelace",
            NamePrefix = "Dr.",
            NameSuffix = "Jr."
        };
        PatentOrganization organization = new PatentOrganization
        {
            Name = "Analytical Engines, LLC",
            StandardName = "ANALYTICAL ENGINES LLC",
            OrganizationIdentifier = "org-42",
            OrganizationCategory = "corporation"
        };
        PatentAddress address = new PatentAddress
        {
            NameLineOne = "Analytical Engines, LLC",
            NameLineTwo = "Patent Department",
            AddressLineOne = "100 First Street",
            AddressLineTwo = "Suite 200",
            AddressLineThree = "Building A",
            AddressLineFour = "Mail Stop 3",
            City = "Alexandria",
            GeographicRegionName = "Virginia",
            GeographicRegionCode = "VA",
            PostalCode = "22314",
            CountryCode = "US",
            CountryName = "United States",
            PostalAddressCategory = "commercial",
            EmailAddress = "patents@example.test"
        };

        Assert.Multiple(() =>
        {
            Assert.That(person.FullName, Is.EqualTo("Dr. Ada M. Lovelace Jr."));
            Assert.That(person.FirstName, Is.EqualTo("Ada"));
            Assert.That(person.LastName, Is.EqualTo("Lovelace"));
            Assert.That(person.NameSuffix, Is.EqualTo("Jr."));
            Assert.That(organization.StandardName, Is.EqualTo("ANALYTICAL ENGINES LLC"));
            Assert.That(organization.OrganizationIdentifier, Is.EqualTo("org-42"));
            Assert.That(address.AddressLineFour, Is.EqualTo("Mail Stop 3"));
            Assert.That(address.GeographicRegionCode, Is.EqualTo("VA"));
            Assert.That(address.CountryCode, Is.EqualTo("US"));
            Assert.That(address.EmailAddress, Is.EqualTo("patents@example.test"));
        });
    }

    [Test]
    public void ApplicantAndInventorDistinguishAbsentRoleValues()
    {
        PatentApplicant applicant = new PatentApplicant();
        PatentInventor inventor = new PatentInventor();

        Assert.Multiple(() =>
        {
            Assert.That(applicant.Person, Is.Null);
            Assert.That(applicant.Organization, Is.Null);
            Assert.That(applicant.Addresses, Is.Empty);
            Assert.That(applicant.Sequence, Is.Zero);
            Assert.That(applicant.HasSequence, Is.False);
            Assert.That(applicant.IsInventor, Is.False);
            Assert.That(applicant.HasIsInventor, Is.False);
            Assert.That(inventor.Person, Is.Not.Null);
            Assert.That(inventor.Addresses, Is.Empty);
            Assert.That(inventor.Sequence, Is.Zero);
            Assert.That(inventor.HasSequence, Is.False);
            Assert.That(inventor.IsDeceased, Is.False);
            Assert.That(inventor.HasIsDeceased, Is.False);
        });

        applicant.Person = new PatentPerson { FullName = "Ada Lovelace" };
        applicant.ApplicantCategory = "inventor";
        applicant.Authority = "applicant";
        applicant.ResidenceCountryCode = "GB";
        applicant.Sequence = 1;
        applicant.HasSequence = true;
        applicant.IsInventor = true;
        applicant.HasIsInventor = true;
        inventor.Person.FullName = "Ada Lovelace";
        inventor.ResidenceCountryCode = "GB";
        inventor.Sequence = 1;
        inventor.HasSequence = true;

        Assert.Multiple(() =>
        {
            Assert.That(applicant.Person.FullName, Is.EqualTo("Ada Lovelace"));
            Assert.That(applicant.HasSequence, Is.True);
            Assert.That(applicant.IsInventor, Is.True);
            Assert.That(applicant.HasIsInventor, Is.True);
            Assert.That(inventor.Person.FullName, Is.EqualTo("Ada Lovelace"));
            Assert.That(inventor.ResidenceCountryCode, Is.EqualTo("GB"));
            Assert.That(inventor.HasSequence, Is.True);
        });
    }

    [Test]
    public void ExaminerAndAssigneeRetainDistinctRoleData()
    {
        PatentExaminer examiner = new PatentExaminer
        {
            Person = new PatentPerson { FullName = "Grace Hopper" },
            ExaminerIdentifier = "examiner-7",
            ArtUnit = "2121",
            TechnologyCenter = "2100",
            IsPrimary = true,
            HasIsPrimary = true
        };
        PatentAssignee assignee = new PatentAssignee
        {
            Organization = new PatentOrganization { Name = "Example Research Corp." },
            Addresses = new PatentAddress[] { new PatentAddress { CountryCode = "US" } },
            Sequence = 2,
            HasSequence = true,
            AssigneeCategory = "original",
            OwnershipText = "assignment"
        };

        Assert.Multiple(() =>
        {
            Assert.That(examiner.Person.FullName, Is.EqualTo("Grace Hopper"));
            Assert.That(examiner.ArtUnit, Is.EqualTo("2121"));
            Assert.That(examiner.TechnologyCenter, Is.EqualTo("2100"));
            Assert.That(examiner.IsPrimary, Is.True);
            Assert.That(examiner.HasIsPrimary, Is.True);
            Assert.That(assignee.Organization?.Name, Is.EqualTo("Example Research Corp."));
            Assert.That(assignee.Addresses[0].CountryCode, Is.EqualTo("US"));
            Assert.That(assignee.Sequence, Is.EqualTo(2));
            Assert.That(assignee.HasSequence, Is.True);
            Assert.That(assignee.OwnershipText, Is.EqualTo("assignment"));
        });
    }

    [Test]
    public void CorrespondenceAndAttorneyPreserveContactAndRegistrationData()
    {
        PatentTelecommunication telephone = new PatentTelecommunication
        {
            Number = "206-555-0100",
            Extension = "42",
            TypeCode = "TEL"
        };
        PatentCorrespondence correspondence = new PatentCorrespondence
        {
            Recipient = new PatentParty
            {
                Organization = new PatentOrganization { Name = "Example IP LLP" }
            },
            Address = new PatentAddress { City = "Seattle", GeographicRegionCode = "WA" },
            Telecommunications = new PatentTelecommunication[] { telephone },
            CustomerNumber = "38106",
            DocketNumber = "ABC-123"
        };
        PatentAttorney attorney = new PatentAttorney
        {
            Person = new PatentPerson { FullName = "Daniel O'Brien" },
            Organization = new PatentOrganization { Name = "Example IP LLP" },
            Addresses = new PatentAddress[] { correspondence.Address },
            Telecommunications = new PatentTelecommunication[] { telephone },
            RegistrationNumber = "65545",
            PractitionerCategory = "ATTNY",
            Status = "ACTIVE",
            IsActive = true,
            HasIsActive = true,
            IsPowerOfAttorney = true,
            HasIsPowerOfAttorney = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(correspondence.Recipient.Organization?.Name, Is.EqualTo("Example IP LLP"));
            Assert.That(correspondence.Address.City, Is.EqualTo("Seattle"));
            Assert.That(correspondence.CustomerNumber, Is.EqualTo("38106"));
            Assert.That(correspondence.Telecommunications[0].Extension, Is.EqualTo("42"));
            Assert.That(attorney.Person.FullName, Is.EqualTo("Daniel O'Brien"));
            Assert.That(attorney.RegistrationNumber, Is.EqualTo("65545"));
            Assert.That(attorney.IsActive, Is.True);
            Assert.That(attorney.HasIsActive, Is.True);
            Assert.That(attorney.IsPowerOfAttorney, Is.True);
            Assert.That(attorney.HasIsPowerOfAttorney, Is.True);
        });
    }

    [Test]
    public void PartyCollectsEveryPatentRole()
    {
        PatentParty party = new PatentParty
        {
            PartyIdentifier = "application-14412875",
            Applicants = new PatentApplicant[] { new PatentApplicant() },
            Inventors = new PatentInventor[] { new PatentInventor() },
            Examiners = new PatentExaminer[] { new PatentExaminer() },
            Assignees = new PatentAssignee[] { new PatentAssignee() },
            Correspondence = new PatentCorrespondence[] { new PatentCorrespondence() },
            Attorneys = new PatentAttorney[] { new PatentAttorney() }
        };

        Assert.Multiple(() =>
        {
            Assert.That(party.PartyIdentifier, Is.EqualTo("application-14412875"));
            Assert.That(party.Applicants, Has.Length.EqualTo(1));
            Assert.That(party.Inventors, Has.Length.EqualTo(1));
            Assert.That(party.Examiners, Has.Length.EqualTo(1));
            Assert.That(party.Assignees, Has.Length.EqualTo(1));
            Assert.That(party.Correspondence, Has.Length.EqualTo(1));
            Assert.That(party.Attorneys, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void MutablePartyDefaultsAreIndependent()
    {
        PatentParty first = new PatentParty();
        PatentParty second = new PatentParty();
        PatentCorrespondence firstCorrespondence = new PatentCorrespondence();
        PatentCorrespondence secondCorrespondence = new PatentCorrespondence();

        first.Addresses = new PatentAddress[] { new PatentAddress { City = "Alexandria" } };
        firstCorrespondence.Address.City = "Seattle";

        Assert.Multiple(() =>
        {
            Assert.That(second.Addresses, Is.Empty);
            Assert.That(firstCorrespondence.Recipient, Is.Not.SameAs(secondCorrespondence.Recipient));
            Assert.That(firstCorrespondence.Address, Is.Not.SameAs(secondCorrespondence.Address));
            Assert.That(secondCorrespondence.Address.City, Is.Null);
        });
    }
}
