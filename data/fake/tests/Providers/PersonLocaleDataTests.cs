using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonLocaleDataTests
{
    [Test]
    public void BundledInvariantDataDefinesEveryPersonNameDimension()
    {
        Fake value = Fake.Create(6301);

        PersonLocaleData data = PersonLocaleDataParser.Resolve(value.Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.SexCategories.Select(category => category.Id), Is.EqualTo(new string[] { "female", "male" }));
            Assert.That(data.SexCategories.Select(category => category.Weight), Is.EqualTo(new double[] { 1.0, 1.0 }));
            Assert.That(data.GivenNames.Length, Is.EqualTo(14));
            Assert.That(data.MiddleNames.Length, Is.EqualTo(5));
            Assert.That(data.FamilyNames.Length, Is.EqualTo(8));
            Assert.That(data.Prefixes.Length, Is.EqualTo(4));
            Assert.That(data.Suffixes.Length, Is.EqualTo(4));
            Assert.That(data.FullFormats.Length, Is.EqualTo(2));
            Assert.That(data.FormalFormats.Length, Is.EqualTo(2));
            Assert.That(data.InformalFormats.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void FormatComponentsPreserveDeclaredNameOrder()
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(Fake.Create(6302).Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.FullFormats[0].Template, Is.EqualTo("{given} {family}"));
            Assert.That(data.FullFormats[0].Components, Is.EqualTo(new string[] { "given", "family" }));
            Assert.That(data.FullFormats[1].Components, Is.EqualTo(new string[] { "given", "middle", "family" }));
            Assert.That(data.FormalFormats[0].Components, Is.EqualTo(new string[] { "prefix", "given", "family" }));
            Assert.That(data.FormalFormats[1].Components, Is.EqualTo(new string[] { "prefix", "given", "middle", "family", "suffix" }));
            Assert.That(data.InformalFormats[0].Components, Is.EqualTo(new string[] { "given" }));
        });
    }

    [Test]
    public void WeightedCategoryAndComponentSelectionIsDeterministicAndApplicable()
    {
        Fake firstFake = Fake.Create(6303);
        Fake secondFake = Fake.Create(6303);
        PersonLocaleData firstData = PersonLocaleDataParser.Resolve(firstFake.Context);
        PersonLocaleData secondData = PersonLocaleDataParser.Resolve(secondFake.Context);
        FakeRandom first = firstFake.Person().RandomValue;
        FakeRandom second = secondFake.Person().RandomValue;
        HashSet<string> categories = new HashSet<string>();
        HashSet<string> givenNames = new HashSet<string>();
        HashSet<string> formats = new HashSet<string>();

        int index = 0;
        while (index < 1024)
        {
            string firstCategory = firstData.ChooseSexCategory(first);
            string secondCategory = secondData.ChooseSexCategory(second);
            PersonNameEntryData firstGiven = firstData.ChooseEntry(first, firstData.GivenNames, firstCategory);
            PersonNameEntryData secondGiven = secondData.ChooseEntry(second, secondData.GivenNames, secondCategory);
            PersonNameFormatData firstFormat = firstData.ChooseFormat(first, firstData.FullFormats);
            PersonNameFormatData secondFormat = secondData.ChooseFormat(second, secondData.FullFormats);

            Assert.That(firstCategory, Is.EqualTo(secondCategory));
            Assert.That(firstGiven.Value, Is.EqualTo(secondGiven.Value));
            Assert.That(firstFormat.Template, Is.EqualTo(secondFormat.Template));
            Assert.That(firstGiven.AppliesTo(firstCategory), Is.True);
            categories.Add(firstCategory);
            givenNames.Add(firstGiven.Value);
            formats.Add(firstFormat.Template);
            index++;
        }

        Assert.That(categories, Is.EquivalentTo(new string[] { "female", "male" }));
        Assert.That(givenNames.Count, Is.EqualTo(14));
        Assert.That(formats.Count, Is.EqualTo(2));
    }

    [Test]
    public void CategorySpecificPrefixesRemainSeparatedFromUniversalPrefixes()
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(Fake.Create(6304).Context);
        PersonNameEntryData ms = data.Prefixes.Single(entry => entry.Value == "Ms.");
        PersonNameEntryData mr = data.Prefixes.Single(entry => entry.Value == "Mr.");
        PersonNameEntryData doctor = data.Prefixes.Single(entry => entry.Value == "Dr.");

        Assert.Multiple(() =>
        {
            Assert.That(ms.AppliesTo("female"), Is.True);
            Assert.That(ms.AppliesTo("male"), Is.False);
            Assert.That(mr.AppliesTo("female"), Is.False);
            Assert.That(mr.AppliesTo("male"), Is.True);
            Assert.That(doctor.AppliesTo("female"), Is.True);
            Assert.That(doctor.AppliesTo("male"), Is.True);
        });
    }

    [Test]
    public void CallerLocaleDataOverridesInvariantThroughParentFallback()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 6305);
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson("Élodie", "Bernard"));

        PersonLocaleData data = PersonLocaleDataParser.Resolve(value.Context);
        FallbackTrace trace = value.Locale().FallbackTrace("person.names");

        Assert.Multiple(() =>
        {
            Assert.That(data.GivenNames.Select(entry => entry.Value), Is.EqualTo(new string[] { "Élodie" }));
            Assert.That(data.FamilyNames.Select(entry => entry.Value), Is.EqualTo(new string[] { "Bernard" }));
            Assert.That(trace.SourceLocale, Is.EqualTo("fr"));
            Assert.That(trace.UsedFallback, Is.True);
        });
    }

    [Test]
    public void ParsedPersonValuesHonorCapturedUnicodeNormalization()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6306);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "en-US", ValidJson("E\u0301lodie", "O\u0301 Briain"));

        PersonLocaleData data = PersonLocaleDataParser.Resolve(value.Context);

        Assert.That(data.GivenNames[0].Value, Is.EqualTo("Élodie"));
        Assert.That(data.FamilyNames[0].Value, Is.EqualTo("Ó Briain"));
    }

    [Test]
    public void MalformedPersonSchemasProduceFieldSpecificDiagnostics()
    {
        string valid = ValidJson("Avery", "Arden");

        AssertInvalid(
            valid.Replace("\"components\":", "\"componentData\":"),
            "requires member 'components'");
        AssertInvalid(
            valid.Replace(
                "{\"id\":\"male\",\"weight\":1}",
                "{\"id\":\"female\",\"weight\":1}"),
            "duplicates 'female'");
        AssertInvalid(
            valid.Replace("\"sexCategories\":[]", "\"sexCategories\":[\"unknown\"]"),
            "references unknown category 'unknown'");
        AssertInvalid(
            valid.Replace("\"value\":\"Avery\",\"weight\":1", "\"value\":\"Avery\",\"weight\":-1"),
            "must be finite and nonnegative");
        AssertInvalid(
            valid.Replace("{given} {family}", "{given} {unknown}"),
            "unknown component 'unknown'");
        AssertInvalid(
            valid.Replace(
                "\"prefix\":[{\"value\":\"Dr.\",\"weight\":1,\"sexCategories\":[]}]",
                "\"prefix\":[]"),
            "uses empty component 'prefix'");
        AssertInvalid(
            valid
                .Replace(
                    "\"middle\":[]",
                    "\"middle\":[{\"value\":\"Marie\",\"weight\":1,\"sexCategories\":[\"female\"]}]")
                .Replace("{given} {family}", "{given} {middle} {family}"),
            "middle has no positive weight for sex category 'male'");
    }

    [Test]
    public void ResolvingAndRejectingPersonDataDoesNotConsumeEntropy()
    {
        Fake actualFake = Fake.Create(6307);
        Fake expectedFake = Fake.Create(6307);
        PersonLocaleData data = PersonLocaleDataParser.Resolve(actualFake.Context);

        Assert.That(
            () => data.ChooseEntry(actualFake.Person().RandomValue, data.GivenNames, "unknown"),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            actualFake.Person().RandomValue.Long(),
            Is.EqualTo(expectedFake.Person().RandomValue.Long()));
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = Fake.Create("en-US", 6399);
        Register(value, "en-US", json);

        FakeException exception = Assert.Throws<FakeException>(
            () => PersonLocaleDataParser.Resolve(value.Context));

        Assert.That(exception.Message, Does.StartWith("Resolved locale field 'person.names'"));
        Assert.That(exception.Message, Does.Contain(message));
    }

    static void Register(Fake value, string locale, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object>
            {
                ["person.names"] = document.RootElement.Clone()
            });
    }

    static string ValidJson(string given, string family)
    {
        return $$"""
        {
          "sexCategories":[
            {"id":"female","weight":1},
            {"id":"male","weight":1}
          ],
          "components":{
            "given":[{"value":"{{given}}","weight":1,"sexCategories":[]}],
            "middle":[],
            "family":[{"value":"{{family}}","weight":1,"sexCategories":[]}],
            "prefix":[{"value":"Dr.","weight":1,"sexCategories":[]}],
            "suffix":[]
          },
          "formats":{
            "full":[{"template":"{given} {family}","weight":1}],
            "formal":[{"template":"{prefix} {given} {family}","weight":1}],
            "informal":[{"template":"{given}","weight":1}]
          }
        }
        """;
    }
}
