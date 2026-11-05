using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonNameCaseTests
{
    [Test]
    public void BundledLocaleDefinesEveryDeclaredNameCase()
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(Fake.Create(6501).Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.MononymCases.Length, Is.EqualTo(2));
            Assert.That(data.ParticleCases.Length, Is.EqualTo(2));
            Assert.That(data.PatronymicCases.Length, Is.EqualTo(2));
            Assert.That(data.MultiPartCases.Length, Is.EqualTo(2));
            Assert.That(data.ApostropheCases.Length, Is.EqualTo(2));
            Assert.That(data.HyphenatedCases.Length, Is.EqualTo(2));
            Assert.That(data.DiacriticCases.Length, Is.EqualTo(2));
            Assert.That(data.RightToLeftCases.Length, Is.EqualTo(2));
            Assert.That(data.OptionalComponentCases.Length, Is.EqualTo(2));
            Assert.That(data.LongCases.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void PublicCaseApiProducesEveryRequestedStructure()
    {
        Fake value = Fake.Create(6502);
        PersonProvider person = value.Person();
        string mononym = person.NameCase(PersonNameCase.Mononym);
        string particle = person.NameCase(PersonNameCase.Particle);
        string patronymic = person.NameCase(PersonNameCase.Patronymic);
        string multiPart = person.NameCase(PersonNameCase.MultiPart);
        string apostrophe = person.NameCase(PersonNameCase.Apostrophe);
        string hyphenated = person.NameCase(PersonNameCase.Hyphenated);
        string diacritic = person.NameCase(PersonNameCase.Diacritic);
        string rightToLeft = person.NameCase(PersonNameCase.RightToLeft);
        string optional = person.NameCase(PersonNameCase.OptionalComponentOmitted);
        string longName = person.NameCase(PersonNameCase.Long);

        Assert.Multiple(() =>
        {
            Assert.That(mononym, Does.Not.Contain(" "));
            Assert.That(
                particle.Contains(" van ") || particle.Contains(" de "),
                Is.True);
            Assert.That(
                patronymic.Contains("ovna ") || patronymic.Contains("ovich "),
                Is.True);
            Assert.That(multiPart.Split(' '), Has.Length.EqualTo(3));
            Assert.That(apostrophe, Does.Contain("'"));
            Assert.That(hyphenated, Does.Contain("-"));
            Assert.That(diacritic.Any(character => character > 127), Is.True);
            Assert.That(value.Text().Direction(rightToLeft), Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(optional.Split(' '), Has.Length.EqualTo(2));
            Assert.That(longName.Length, Is.GreaterThan(75));
        });
    }

    [Test]
    public void CaseSelectionIsDeterministicAndReachesEveryWeightedFixture()
    {
        Fake first = Fake.Create(6503);
        Fake second = Fake.Create(6503);
        HashSet<string>[] observed = new HashSet<string>[10];
        int caseIndex = 0;
        while (caseIndex < observed.Length)
        {
            observed[caseIndex] = new HashSet<string>();
            caseIndex++;
        }

        int iteration = 0;
        while (iteration < 128)
        {
            caseIndex = 0;
            while (caseIndex < observed.Length)
            {
                PersonNameCase nameCase = (PersonNameCase)caseIndex;
                string actual = first.Person().NameCase(nameCase);
                string expected = second.Person().NameCase(nameCase);
                Assert.That(actual, Is.EqualTo(expected));
                observed[caseIndex].Add(actual);
                caseIndex++;
            }
            iteration++;
        }

        caseIndex = 0;
        while (caseIndex < observed.Length)
        {
            Assert.That(observed[caseIndex].Count, Is.EqualTo(2));
            caseIndex++;
        }
    }

    [Test]
    public void ExplicitNamingCategorySelectsApplicablePatronymicCases()
    {
        PersonProvider person = Fake.Create(6504).Person();

        Assert.Multiple(() =>
        {
            Assert.That(
                person.NameCaseFromNameCaseAndSexCategory(PersonNameCase.Patronymic, "female"),
                Is.EqualTo("Nadia Omarovna Vale"));
            Assert.That(
                person.NameCaseFromNameCaseAndSexCategory(PersonNameCase.Patronymic, "male"),
                Is.EqualTo("Omar Eliasovich Arden"));
        });
    }

    [Test]
    public void CallerCaseDataHonorsCapturedNormalization()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6505);
        options.NormalizationMode = NormalizationMode.Nfc;
        PersonProvider person = Create(
            options,
            LocaleJson(
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                CasesJson("E\\u0301lodie Arden", "Long Name")));

        Assert.That(
            person.NameCaseFromNameCaseAndSexCategory(PersonNameCase.Diacritic, "female"),
            Is.EqualTo("Élodie Arden"));
    }

    [Test]
    public void MononymLocaleMayOmitFamilyNamesAndOptionalComponents()
    {
        Fake value = CreateFake(
            FakeOptions.Create("en-US", 6506),
            LocaleJson("", ""));

        Assert.Multiple(() =>
        {
            Assert.That(value.Person().FullNameFromSexCategory("female"), Is.EqualTo("Aster"));
            Assert.That(value.Person().FormalNameFromSexCategory("male"), Is.EqualTo("Aster"));
            Assert.That(value.Person().DisplayNameFromSexCategory("female"), Is.EqualTo("Aster"));
            Assert.That(
                () => value.Person().LastName(),
                Throws.TypeOf<FakeException>());
        });
    }

    [Test]
    public void MissingCaseDataAndInvalidArgumentsDoNotConsumeEntropy()
    {
        Fake actual = CreateFake(
            FakeOptions.Create("en-US", 6507),
            LocaleJson(
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                ""));
        Fake expected = Fake.Create(6507);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => actual.Person().NameCase(PersonNameCase.Mononym),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("no selectable name entry"));
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => actual.Person().NameCase((PersonNameCase)99)).ParamName,
                Is.EqualTo("nameCase"));
            Assert.That(
                Assert.Throws<ArgumentException>(
                    () => actual.Person().NameCaseFromNameCaseAndSexCategory(PersonNameCase.Mononym, "unknown")).ParamName,
                Is.EqualTo("sexCategory"));
        });
        Assert.That(
            actual.Person().RandomValue.Long(),
            Is.EqualTo(expected.Person().RandomValue.Long()));
    }

    [Test]
    public void MalformedCaseSectionsProduceCaseSpecificDiagnostics()
    {
        string valid = LocaleJson(
            "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
            CasesJson("Élodie Arden", "Long Name"));

        AssertInvalid(
            valid.Replace("\"mononym\":[", "\"singleName\":["),
            "cases requires member 'mononym'");
        AssertInvalid(
            valid.Replace(
                "\"mononym\":[{\"value\":\"Aster\",\"weight\":1,\"sexCategories\":[]}]",
                "\"mononym\":[]"),
            "cases.mononym must not be empty");
        AssertInvalid(
            valid.Replace(
                "\"sexCategories\":[]}]",
                "\"sexCategories\":[\"unknown\"]}]"),
            "references unknown category 'unknown'");
    }

    [Test]
    public void CaseValuesEnforceTheCapturedOutputLimitWithoutEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6508);
        options.MaximumOutputSize = 15;
        Fake actual = CreateFake(
            options,
            LocaleJson(
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                CasesJson("Accent", "SixteenLettersABC")));
        FakeOptions expectedOptions = FakeOptions.Create("en-US", 6508);
        expectedOptions.MaximumOutputSize = 15;
        Fake expected = Fake.Create(expectedOptions);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => actual.Person().NameCase(PersonNameCase.Long)).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            actual.Person().RandomValue.Long(),
            Is.EqualTo(expected.Person().RandomValue.Long()));
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = CreateFake(FakeOptions.Create("en-US", 6599), json);

        FakeException exception = Assert.Throws<FakeException>(
            () => value.Person().NameCase(PersonNameCase.Mononym));

        Assert.That(exception.Message, Does.Contain(message));
    }

    static PersonProvider Create(FakeOptions options, string json)
    {
        return CreateFake(options, json).Person();
    }

    static Fake CreateFake(FakeOptions options, string json)
    {
        Fake value = Fake.Create(options);
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["person.names"] = document.RootElement.Clone()
            });
        return value;
    }

    static string LocaleJson(string familyEntries, string casesMember)
    {
        return $$"""
        {
          "sexCategories":[
            {"id":"female","weight":1},
            {"id":"male","weight":1}
          ],
          "components":{
            "given":[{"value":"Aster","weight":1,"sexCategories":[]}],
            "middle":[],
            "family":[{{familyEntries}}],
            "prefix":[],
            "suffix":[]
          },
          "formats":{
            "full":[{"template":"{given}","weight":1}],
            "formal":[{"template":"{given}","weight":1}],
            "informal":[{"template":"{given}","weight":1}]
          }{{casesMember}}
        }
        """;
    }

    static string CasesJson(string diacriticValue, string longValue)
    {
        return $$"""
        ,"cases":{
          "mononym":[{"value":"Aster","weight":1,"sexCategories":[]}],
          "particle":[{"value":"A van B","weight":1,"sexCategories":[]}],
          "patronymic":[{"value":"A Bovich C","weight":1,"sexCategories":[]}],
          "multiPart":[{"value":"A B C","weight":1,"sexCategories":[]}],
          "apostrophe":[{"value":"A O'B","weight":1,"sexCategories":[]}],
          "hyphenated":[{"value":"A B-C","weight":1,"sexCategories":[]}],
          "diacritic":[{"value":"{{diacriticValue}}","weight":1,"sexCategories":[]}],
          "rightToLeft":[{"value":"אביב","weight":1,"sexCategories":[]}],
          "optionalComponentOmitted":[{"value":"A B","weight":1,"sexCategories":[]}],
          "long":[{"value":"{{longValue}}","weight":1,"sexCategories":[]}]
        }
        """;
    }
}
