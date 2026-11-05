using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonIdentityTests
{
    [Test]
    public void BundledIdentityDataDefinesEveryIndependentDimension()
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(Fake.Create(6601).Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.Genders.Length, Is.EqualTo(5));
            Assert.That(data.Pronouns.Length, Is.EqualTo(4));
            Assert.That(data.Biographies.Length, Is.EqualTo(6));
            Assert.That(data.ZodiacSigns.Length, Is.EqualTo(12));
            Assert.That(data.MaritalStatuses.Length, Is.EqualTo(6));
            Assert.That(data.Relationships.Length, Is.EqualTo(10));
            Assert.That(data.HouseholdRoles.Length, Is.EqualTo(6));
            Assert.That(data.HouseholdTypes.Length, Is.EqualTo(6));
            Assert.That(
                data.DemographicCategories(),
                Is.EqualTo(new string[]
                {
                    "age-group",
                    "community-setting",
                    "household-size-band"
                }));
            Assert.That(
                data.Genders.Select(value => value.Value),
                Is.Not.EquivalentTo(new string[] { "female", "male" }));
        });
    }

    [Test]
    public void ScalarIdentityApisAreDeterministicLocaleSelections()
    {
        Fake firstFake = Fake.Create(6602);
        Fake secondFake = Fake.Create(6602);
        PersonIdentityData data = PersonIdentityDataParser.Resolve(firstFake.Context);
        PersonProvider first = firstFake.Person();
        PersonProvider second = secondFake.Person();

        int index = 0;
        while (index < 64)
        {
            AssertSelection(first.Gender(), second.Gender(), data.Genders);
            AssertSelection(first.Biography(), second.Biography(), data.Biographies);
            AssertSelection(first.ZodiacSign(), second.ZodiacSign(), data.ZodiacSigns);
            AssertSelection(first.MaritalStatus(), second.MaritalStatus(), data.MaritalStatuses);
            AssertSelection(first.Relationship(), second.Relationship(), data.Relationships);
            AssertSelection(first.HouseholdRole(), second.HouseholdRole(), data.HouseholdRoles);
            AssertSelection(first.HouseholdType(), second.HouseholdType(), data.HouseholdTypes);
            index++;
        }
    }

    [Test]
    public void PronounSetsKeepEveryGrammaticalFormCorrelated()
    {
        PersonProvider person = Fake.Create(6603).Person();
        Dictionary<string, string[]> expected = new Dictionary<string, string[]>
        {
            ["she/her"] = new string[] { "she", "her", "her", "hers", "herself" },
            ["he/him"] = new string[] { "he", "him", "his", "his", "himself" },
            ["they/them"] = new string[] { "they", "them", "their", "theirs", "themself" },
            ["ze/hir"] = new string[] { "ze", "hir", "hir", "hirs", "hirself" }
        };
        HashSet<string> observed = new HashSet<string>();

        int index = 0;
        while (index < 128)
        {
            PronounSet value = person.Pronouns();
            Assert.That(expected.ContainsKey(value.Display), Is.True);
            Assert.That(
                new string[]
                {
                    value.Subject,
                    value.Object,
                    value.PossessiveAdjective,
                    value.PossessivePronoun,
                    value.Reflexive
                },
                Is.EqualTo(expected[value.Display]));
            observed.Add(value.Display);
            index++;
        }

        Assert.That(observed, Is.EquivalentTo(expected.Keys));
    }

    [Test]
    public void IdentityAndNameRandomStreamsAreIndependent()
    {
        Fake identityFirst = Fake.Create(6604);
        Fake untouchedName = Fake.Create(6604);
        Fake nameFirst = Fake.Create(6604);
        Fake untouchedIdentity = Fake.Create(6604);

        int index = 0;
        while (index < 32)
        {
            identityFirst.Person().Gender();
            identityFirst.Person().Pronouns();
            identityFirst.Person().Biography();
            identityFirst.Person().ZodiacSign();
            identityFirst.Person().MaritalStatus();
            identityFirst.Person().Relationship();
            identityFirst.Person().HouseholdRole();
            identityFirst.Person().HouseholdType();
            nameFirst.Person().FullName();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                identityFirst.Person().FullName(),
                Is.EqualTo(untouchedName.Person().FullName()));
            Assert.That(
                nameFirst.Person().Gender(),
                Is.EqualTo(untouchedIdentity.Person().Gender()));
        });
    }

    [Test]
    public void DemographicsAreCategoryAddressableAndCaseInsensitive()
    {
        PersonProvider person = Fake.Create(6605).Person();
        string[] categories = person.DemographicCategories();
        categories[0] = "changed";
        HashSet<string> ageGroups = new HashSet<string>();
        HashSet<string> settings = new HashSet<string>();
        HashSet<string> householdSizes = new HashSet<string>();

        int index = 0;
        while (index < 128)
        {
            ageGroups.Add(person.Demographic("AGE-GROUP"));
            settings.Add(person.Demographic("community-setting"));
            householdSizes.Add(person.Demographic("household-size-band"));
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                person.DemographicCategories(),
                Is.EqualTo(new string[]
                {
                    "age-group",
                    "community-setting",
                    "household-size-band"
                }));
            Assert.That(ageGroups, Is.EquivalentTo(new string[] { "child", "adolescent", "adult", "older adult" }));
            Assert.That(settings, Is.EquivalentTo(new string[] { "urban", "suburban", "rural", "remote" }));
            Assert.That(householdSizes, Is.EquivalentTo(new string[] { "one person", "two people", "three to four people", "five or more people" }));
        });
    }

    [Test]
    public void CallerIdentityDataUsesFallbackAndCapturedNormalization()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 6606);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson("non-binaire", "E\\u0301crit des histoires."));

        Assert.Multiple(() =>
        {
            Assert.That(value.Person().Gender(), Is.EqualTo("non-binaire"));
            Assert.That(value.Person().Biography(), Is.EqualTo("Écrit des histoires."));
            Assert.That(value.Person().Pronouns().Display, Is.EqualTo("iel/ellui"));
            Assert.That(value.Person().Demographic("age-group"), Is.EqualTo("adulte"));
            Assert.That(value.Locale().FallbackTrace("person.identity").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void IdentityLabelsNeverBecomeNameSelectionCategories()
    {
        Fake value = Fake.Create("en-US", 6607);
        Register(value, "en-US", ValidJson("identity-only", "Writes stories."));

        Assert.Multiple(() =>
        {
            Assert.That(value.Person().Gender(), Is.EqualTo("identity-only"));
            Assert.That(value.Person().FirstNameFromSexCategory("female"), Is.Not.Empty);
            Assert.That(
                () => value.Person().FirstNameFromSexCategory("identity-only"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void MalformedIdentitySchemasProduceFieldSpecificDiagnostics()
    {
        string valid = ValidJson("identity", "Writes stories.");

        AssertInvalid(
            valid.Replace("\"genders\":", "\"genderValues\":"),
            "requires member 'genders'");
        AssertInvalid(
            valid.Replace("\"reflexive\":\"ellui-même\",", ""),
            "requires member 'reflexive'");
        AssertInvalid(
            valid.Replace("\"weight\":1", "\"weight\":-1"),
            "weight must be finite and nonnegative");
        AssertInvalid(
            valid.Replace("\"id\":\"age-group\"", "\"id\":\"Age Group\""),
            "must use lowercase ASCII letters and internal hyphens");
        AssertInvalid(
            valid.Replace(
                "\"demographics\":[",
                "\"demographics\":[{\"id\":\"age-group\",\"values\":[{\"value\":\"duplicate\",\"weight\":1}]},"),
            "duplicates 'age-group'");
    }

    [Test]
    public void InvalidDemographicCategoryDoesNotConsumeIdentityEntropy()
    {
        Fake actual = Fake.Create(6608);
        Fake expected = Fake.Create(6608);

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(
                    () => actual.Person().Demographic(null)).ParamName,
                Is.EqualTo("category"));
            Assert.That(
                Assert.Throws<ArgumentException>(
                    () => actual.Person().Demographic("unknown")).ParamName,
                Is.EqualTo("category"));
        });
        Assert.That(
            actual.Person().Gender(),
            Is.EqualTo(expected.Person().Gender()));
    }

    [Test]
    public void IdentityValuesEnforceTheCapturedOutputLimitWithoutEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6609);
        options.MaximumOutputSize = 10;
        Fake actual = Fake.Create(options);
        Register(actual, "en-US", ValidJson("identity", "Eleven chars"));
        Fake expected = Fake.Create(options);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => actual.Person().Gender()).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            actual.Person().IdentityRandomValue.Long(),
            Is.EqualTo(expected.Person().IdentityRandomValue.Long()));
    }

    static void AssertSelection(
        string actual,
        string expected,
        PersonIdentityValueData[] values)
    {
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(values.Select(value => value.Value), Does.Contain(actual));
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = Fake.Create("en-US", 6699);
        Register(value, "en-US", json);

        FakeException exception = Assert.Throws<FakeException>(
            () => PersonIdentityDataParser.Resolve(value.Context));

        Assert.That(exception.Message, Does.StartWith("Resolved locale field 'person.identity'"));
        Assert.That(exception.Message, Does.Contain(message));
    }

    static void Register(Fake value, string locale, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object>
            {
                ["person.identity"] = document.RootElement.Clone()
            });
    }

    static string ValidJson(string gender, string biography)
    {
        return $$"""
        {
          "genders":[{"value":"{{gender}}","weight":1}],
          "pronouns":[{
            "subject":"iel",
            "object":"ellui",
            "possessiveAdjective":"son",
            "possessivePronoun":"le sien",
            "reflexive":"ellui-même",
            "display":"iel/ellui",
            "weight":1
          }],
          "biographies":[{"value":"{{biography}}","weight":1}],
          "zodiacSigns":[{"value":"Bélier","weight":1}],
          "maritalStatuses":[{"value":"célibataire","weight":1}],
          "relationships":[{"value":"ami","weight":1}],
          "householdRoles":[{"value":"membre","weight":1}],
          "householdTypes":[{"value":"ménage partagé","weight":1}],
          "demographics":[{
            "id":"age-group",
            "values":[{"value":"adulte","weight":1}]
          }]
        }
        """;
    }
}
