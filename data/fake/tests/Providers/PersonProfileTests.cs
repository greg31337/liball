using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonProfileTests
{
    [Test]
    public void PersonRecordsAreDeterministicAndInternallyCorrelated()
    {
        Fake firstFake = Fake.Create(6901);
        Fake secondFake = Fake.Create(6901);
        PersonProfileData data = PersonProfileDataParser.Resolve(firstFake.Context);

        int index = 0;
        while (index < 128)
        {
            PersonProfile first = firstFake.Person().Record();
            PersonProfile second = secondFake.Person().Record();
            string usernamePrefix = TextTransforms.Slug(
                first.FirstName,
                '-',
                firstFake.Context.CapturedOptions.MaximumOutputSize)
                + "."
                + TextTransforms.Slug(
                    first.LastName,
                    '-',
                    firstFake.Context.CapturedOptions.MaximumOutputSize)
                + ".";

            Assert.Multiple(() =>
            {
                AssertPersonEqual(first, second);
                Assert.That(first.FullName, Does.Contain(first.FirstName));
                Assert.That(first.FullName, Does.Contain(first.LastName));
                Assert.That(first.Username, Does.StartWith(usernamePrefix));
                Assert.That(first.Username.Substring(first.Username.Length - 4), Does.Match("^[0-9]{4}$"));
                Assert.That(first.Email, Does.StartWith(first.Username + "@example."));
                Assert.That(first.Phone, Does.Match("^\\+1-(415|212|312)-555-01[0-9]{2}$"));
                Assert.That(first.Age, Is.EqualTo(firstFake.Date().Age(first.BirthDate)));
                Assert.That(first.Age, Is.InRange(18, 80));
                Assert.That(first.Locale, Is.EqualTo("en-US"));
                Assert.That(data.Employers.Select(value => value.Value), Does.Contain(first.Employer));
            });
            AssertLocality(first.Address, first.TimeZoneId);
            index++;
        }
    }

    [Test]
    public void HouseholdMembersShareHouseholdContactAndLocation()
    {
        Fake value = Fake.Create(6902);

        int profileIndex = 0;
        while (profileIndex < 64)
        {
            HouseholdProfile household = value.Person().Household();

            Assert.Multiple(() =>
            {
                Assert.That(household.MemberCount, Is.EqualTo(household.Members.Length));
                Assert.That(household.MemberCount, Is.InRange(1, 5));
                Assert.That(
                    household.HouseholdType,
                    Is.EqualTo(household.MemberCount == 1
                        ? "one-person household"
                        : "multi-person household"));
            });
            int memberIndex = 0;
            while (memberIndex < household.Members.Length)
            {
                PersonProfile member = household.Members[memberIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(member.Address, Is.EqualTo(household.Address));
                    Assert.That(member.Phone, Is.EqualTo(household.Phone));
                    Assert.That(member.Locale, Is.EqualTo(household.Locale));
                    Assert.That(member.TimeZoneId, Is.EqualTo(household.TimeZoneId));
                    Assert.That(member.Email, Does.StartWith(member.Username + "@example."));
                    Assert.That(member.Age, Is.InRange(18, 80));
                });
                memberIndex++;
            }
            profileIndex++;
        }
    }

    [Test]
    public void CallerProfileDataUsesParentFallbackAndNormalization()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 6903);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson("Acme\u0301 Exemple", "Cite\u0301 Exemple", 2));

        PersonProfile person = value.Person().Record();
        HouseholdProfile household = value.Person().Household();

        Assert.Multiple(() =>
        {
            Assert.That(person.Employer, Is.EqualTo("Acmé Exemple"));
            Assert.That(person.Address, Does.Contain("Cité Exemple"));
            Assert.That(person.Email, Does.EndWith("@example.com"));
            Assert.That(person.Locale, Is.EqualTo("fr-CA"));
            Assert.That(person.TimeZoneId, Is.EqualTo("Europe/Paris"));
            Assert.That(household.MemberCount, Is.EqualTo(2));
            Assert.That(household.HouseholdType, Is.EqualTo("ménage multiple"));
            Assert.That(value.Locale().FallbackTrace("person.profile").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void ProfileStreamIsIndependentFromPersonAndOtherProviderStreams()
    {
        Fake profileFirst = Fake.Create(6904);
        Fake untouchedPerson = Fake.Create(6904);
        Fake otherFirst = Fake.Create(6904);
        Fake untouchedProfile = Fake.Create(6904);

        int index = 0;
        while (index < 32)
        {
            profileFirst.Person().Record();
            profileFirst.Person().Household();
            otherFirst.Person().FullName();
            otherFirst.Person().Gender();
            otherFirst.Person().JobTitle();
            otherFirst.Education().Record();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(profileFirst.Person().FullName(), Is.EqualTo(untouchedPerson.Person().FullName()));
            Assert.That(profileFirst.Person().Gender(), Is.EqualTo(untouchedPerson.Person().Gender()));
            Assert.That(profileFirst.Person().JobTitle(), Is.EqualTo(untouchedPerson.Person().JobTitle()));
            AssertPersonEqual(otherFirst.Person().Record(), untouchedProfile.Person().Record());
        });
    }

    [Test]
    public void MalformedProfileSchemasProduceFieldSpecificDiagnostics()
    {
        string valid = ValidJson("Example Employer", "Example City", 2);

        AssertInvalid(valid.Replace("\"localities\":", "\"places\":"), "requires member 'localities'");
        AssertInvalid(valid.Replace("Europe/Paris", "Mars/Olympus"), "is not a bundled IANA time-zone identifier");
        AssertInvalid(valid.Replace("example.com", "mail.test"), "must be an RFC 2606 example domain");
        AssertInvalid(valid.Replace("555-01##", "555-12##"), "must use the NANPA fictional 555-01xx range");
        AssertInvalid(valid.Replace("\"value\":2,\"weight\":1", "\"value\":1.5,\"weight\":1"), "must be a positive integer");
        AssertInvalid(
            valid.Replace(
                "{\"value\":\"Example Employer\",\"weight\":1}",
                "{\"value\":\"Example Employer\",\"weight\":1},{\"value\":\"Example Employer\",\"weight\":1}"),
            "duplicates 'Example Employer'");
    }

    [Test]
    public void ProfileDataLimitsFailWithoutConsumingProfileEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6905);
        options.MaximumOutputSize = 10;
        Fake actual = Fake.Create(options);
        Register(actual, "en-US", ValidJson("Eleven chars", "City", 1));
        Fake expected = Fake.Create(options);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => actual.Person().Record()).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            actual.Person().ProfileRandomValue.Long(),
            Is.EqualTo(expected.Person().ProfileRandomValue.Long()));
    }

    static void AssertPersonEqual(PersonProfile actual, PersonProfile expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.SameAs(expected));
            Assert.That(actual.FirstName, Is.EqualTo(expected.FirstName));
            Assert.That(actual.LastName, Is.EqualTo(expected.LastName));
            Assert.That(actual.FullName, Is.EqualTo(expected.FullName));
            Assert.That(actual.BirthDate, Is.EqualTo(expected.BirthDate));
            Assert.That(actual.Age, Is.EqualTo(expected.Age));
            Assert.That(actual.Username, Is.EqualTo(expected.Username));
            Assert.That(actual.Email, Is.EqualTo(expected.Email));
            Assert.That(actual.Phone, Is.EqualTo(expected.Phone));
            Assert.That(actual.Address, Is.EqualTo(expected.Address));
            Assert.That(actual.Employer, Is.EqualTo(expected.Employer));
            Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
            Assert.That(actual.TimeZoneId, Is.EqualTo(expected.TimeZoneId));
        });
    }

    static void AssertLocality(string address, string timeZoneId)
    {
        if (timeZoneId == "America/Los_Angeles")
        {
            Assert.That(address, Does.Contain("Example City, CA 00000"));
        }
        else if (timeZoneId == "America/New_York")
        {
            Assert.That(address, Does.Contain("Sample City, NY 00000"));
        }
        else
        {
            Assert.That(timeZoneId, Is.EqualTo("America/Chicago"));
            Assert.That(address, Does.Contain("Demo City, IL 00000"));
        }
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = Fake.Create("en-US", 6999);
        Register(value, "en-US", json);

        FakeException exception = Assert.Throws<FakeException>(
            () => PersonProfileDataParser.Resolve(value.Context));

        Assert.That(exception.Message, Does.StartWith("Resolved locale field 'person.profile'"));
        Assert.That(exception.Message, Does.Contain(message));
    }

    static void Register(Fake value, string locale, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object>
            {
                ["person.profile"] = document.RootElement.Clone()
            });
    }

    static string ValidJson(string employer, string city, int householdSize)
    {
        return $$"""
        {
          "employers":[{"value":"{{employer}}","weight":1}],
          "localities":[{
            "city":"{{city}}",
            "region":"IDF",
            "postalCode":"00000",
            "timeZoneId":"Europe/Paris",
            "emailDomain":"example.com",
            "weight":1,
            "streets":[{"value":"Rue Exemple","weight":1}],
            "phoneMasks":[{"value":"+1-202-555-01##","weight":1}]
          }],
          "householdSizes":[{"value":{{householdSize}},"weight":1}],
          "householdLabels":{"single":"ménage individuel","multiple":"ménage multiple"}
        }
        """;
    }
}
