using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonProviderTests
{
    [Test]
    public void ComponentApisUseLocaleValuesAndExplicitNamingCategories()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6401);
        options.NormalizationMode = NormalizationMode.None;
        PersonProvider person = Create(
            options,
            LocaleJson(
                "{\"value\":\"E\\u0301lodie\",\"weight\":1,\"sexCategories\":[\"female\"]},{\"value\":\"Milo\",\"weight\":1,\"sexCategories\":[\"male\"]}",
                "{\"value\":\"Noor\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Ms.\",\"weight\":1,\"sexCategories\":[\"female\"]},{\"value\":\"Mr.\",\"weight\":1,\"sexCategories\":[\"male\"]}",
                "{\"value\":\"Jr.\",\"weight\":1,\"sexCategories\":[]}",
                "{given} {family}",
                "{prefix} {given} {family}",
                "{given}"));

        Assert.Multiple(() =>
        {
            Assert.That(person.FirstNameFromSexCategory("female"), Is.EqualTo("E\u0301lodie"));
            Assert.That(person.FirstNameFromSexCategory("male"), Is.EqualTo("Milo"));
            Assert.That(person.MiddleNameFromSexCategory("female"), Is.EqualTo("Noor"));
            Assert.That(person.LastNameFromSexCategory("male"), Is.EqualTo("Arden"));
            Assert.That(person.PrefixFromSexCategory("female"), Is.EqualTo("Ms."));
            Assert.That(person.PrefixFromSexCategory("male"), Is.EqualTo("Mr."));
            Assert.That(person.SuffixFromSexCategory("female"), Is.EqualTo("Jr."));
            Assert.That(person.InitialFromSexCategory("female"), Is.EqualTo("E\u0301"));
        });
    }

    [Test]
    public void FormattedApisPreserveLocaleTemplatesAndComponentOrder()
    {
        PersonProvider person = Create(
            FakeOptions.Create("en-US", 6402),
            LocaleJson(
                "{\"value\":\"Amina\",\"weight\":1,\"sexCategories\":[\"female\"]},{\"value\":\"Milo\",\"weight\":1,\"sexCategories\":[\"male\"]}",
                "{\"value\":\"Noor\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Dr.\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Jr.\",\"weight\":1,\"sexCategories\":[]}",
                "{family}, {given} {middle}",
                "{prefix} {family}, {given} {suffix}",
                "{given} ({family})"));

        Assert.Multiple(() =>
        {
            Assert.That(person.FullNameFromSexCategory("female"), Is.EqualTo("Arden, Amina Noor"));
            Assert.That(person.DisplayNameFromSexCategory("female"), Is.EqualTo("Amina (Arden)"));
            Assert.That(person.FormalNameFromSexCategory("female"), Is.EqualTo("Dr. Arden, Amina Jr."));
            Assert.That(person.NativeNameFromSexCategory("female"), Is.EqualTo("Arden, Amina Noor"));
        });
    }

    [Test]
    public void TransliteratedNamesPreserveTheSelectedNativeNameOrder()
    {
        PersonProvider person = Create(
            FakeOptions.Create("en-US", 6403),
            LocaleJson(
                "{\"value\":\"Αθήνα\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Noor\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Ёж\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Dr.\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Jr.\",\"weight\":1,\"sexCategories\":[]}",
                "{family}, {given}",
                "{prefix} {given} {family}",
                "{given}"));

        Assert.Multiple(() =>
        {
            Assert.That(person.NativeNameFromSexCategory("female"), Is.EqualTo("Ёж, Αθήνα"));
            Assert.That(person.TransliteratedNameFromSexCategory("female"), Is.EqualTo("Yozh, Athina"));
        });
    }

    [Test]
    public void DefaultNameApisAreDeterministicAndNonempty()
    {
        PersonProvider first = Fake.Create(6404).Person();
        PersonProvider second = Fake.Create(6404).Person();

        int index = 0;
        while (index < 32)
        {
            string[] actual = Values(first);
            string[] expected = Values(second);
            Assert.That(actual, Is.EqualTo(expected));
            int valueIndex = 0;
            while (valueIndex < actual.Length)
            {
                Assert.That(actual[valueIndex], Is.Not.Empty);
                valueIndex++;
            }
            index++;
        }
    }

    [Test]
    public void DefaultComponentSelectionUsesOnlyCategoriesWithApplicableValues()
    {
        PersonProvider person = Create(
            FakeOptions.Create("en-US", 6405),
            LocaleJson(
                "{\"value\":\"Avery\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Marie\",\"weight\":1,\"sexCategories\":[\"female\"]}",
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Dr.\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Jr.\",\"weight\":1,\"sexCategories\":[]}",
                "{given} {family}",
                "{prefix} {given} {family}",
                "{given}"));

        int index = 0;
        while (index < 64)
        {
            Assert.That(person.MiddleName(), Is.EqualTo("Marie"));
            index++;
        }
        Assert.That(
            () => person.MiddleNameFromSexCategory("male"),
            Throws.TypeOf<FakeException>());
    }

    [Test]
    public void InvalidExplicitCategoriesAreRejectedBeforeRandomnessIsConsumed()
    {
        Fake actual = Fake.Create(6406);
        Fake expected = Fake.Create(6406);
        PersonProvider person = actual.Person();

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => person.FirstNameFromSexCategory(null)).ParamName,
                Is.EqualTo("sexCategory"));
            Assert.That(
                Assert.Throws<ArgumentException>(() => person.FirstNameFromSexCategory(" ")).ParamName,
                Is.EqualTo("sexCategory"));
            Assert.That(
                Assert.Throws<ArgumentException>(() => person.FullNameFromSexCategory("unknown")).ParamName,
                Is.EqualTo("sexCategory"));
        });
        Assert.That(
            actual.Person().RandomValue.Long(),
            Is.EqualTo(expected.Person().RandomValue.Long()));
    }

    [Test]
    public void EmptyOptionalComponentsFailWithoutConsumingRandomness()
    {
        Fake actual = CreateFake(
            FakeOptions.Create("en-US", 6407),
            LocaleJson(
                "{\"value\":\"Avery\",\"weight\":1,\"sexCategories\":[]}",
                "",
                "{\"value\":\"Arden\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Dr.\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"Jr.\",\"weight\":1,\"sexCategories\":[]}",
                "{given} {family}",
                "{prefix} {given} {family}",
                "{given}"));
        Fake expected = Fake.Create(6407);

        Assert.That(
            () => actual.Person().MiddleName(),
            Throws.TypeOf<FakeException>()
                .With.Message.Contains("no selectable name entry"));
        Assert.That(
            actual.Person().RandomValue.Long(),
            Is.EqualTo(expected.Person().RandomValue.Long()));
    }

    [Test]
    public void FormattedNamesEnforceTheCapturedOutputLimit()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6408);
        options.MaximumOutputSize = 15;
        PersonProvider person = Create(
            options,
            LocaleJson(
                "{\"value\":\"ABCDEFGHIJ\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"M\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"KLMNOPQRST\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"P\",\"weight\":1,\"sexCategories\":[]}",
                "{\"value\":\"S\",\"weight\":1,\"sexCategories\":[]}",
                "{given}{family}",
                "{prefix}{given}",
                "{given}"));

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => person.FullNameFromSexCategory("female"));

        Assert.That(exception.ParamName, Is.EqualTo("personName"));
    }

    static string[] Values(PersonProvider person)
    {
        return new string[]
        {
            person.FirstName(),
            person.MiddleName(),
            person.LastName(),
            person.Prefix(),
            person.Suffix(),
            person.Initial(),
            person.FullName(),
            person.DisplayName(),
            person.FormalName(),
            person.NativeName(),
            person.TransliteratedName()
        };
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

    static string LocaleJson(
        string givenEntries,
        string middleEntries,
        string familyEntries,
        string prefixEntries,
        string suffixEntries,
        string fullTemplate,
        string formalTemplate,
        string informalTemplate)
    {
        return $$"""
        {
          "sexCategories":[
            {"id":"female","weight":1},
            {"id":"male","weight":1}
          ],
          "components":{
            "given":[{{givenEntries}}],
            "middle":[{{middleEntries}}],
            "family":[{{familyEntries}}],
            "prefix":[{{prefixEntries}}],
            "suffix":[{{suffixEntries}}]
          },
          "formats":{
            "full":[{"template":"{{fullTemplate}}","weight":1}],
            "formal":[{"template":"{{formalTemplate}}","weight":1}],
            "informal":[{"template":"{{informalTemplate}}","weight":1}]
          }
        }
        """;
    }
}
