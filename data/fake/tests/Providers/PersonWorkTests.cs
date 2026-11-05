using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonWorkTests
{
    [Test]
    public void BundledWorkDataDefinesEveryJobDimension()
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(Fake.Create(6701).Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.Titles.Length, Is.EqualTo(10));
            Assert.That(data.Descriptors.Length, Is.EqualTo(8));
            Assert.That(data.Areas.Length, Is.EqualTo(8));
            Assert.That(data.Types.Length, Is.EqualTo(6));
            Assert.That(data.Seniorities.Length, Is.EqualTo(7));
            Assert.That(data.Departments.Length, Is.EqualTo(10));
            Assert.That(data.Statuses.Length, Is.EqualTo(6));
            Assert.That(data.SalaryBands.Length, Is.EqualTo(6));
        });
    }

    [Test]
    public void WorkApisAreDeterministicWeightedLocaleSelections()
    {
        Fake firstFake = Fake.Create(6702);
        Fake secondFake = Fake.Create(6702);
        PersonWorkData data = PersonWorkDataParser.Resolve(firstFake.Context);
        PersonProvider first = firstFake.Person();
        PersonProvider second = secondFake.Person();
        HashSet<string>[] observed = new HashSet<string>[8];
        int valueIndex = 0;
        while (valueIndex < observed.Length)
        {
            observed[valueIndex] = new HashSet<string>();
            valueIndex++;
        }

        int index = 0;
        while (index < 256)
        {
            AssertSelection(first.JobTitle(), second.JobTitle(), data.Titles, observed[0]);
            AssertSelection(first.JobDescriptor(), second.JobDescriptor(), data.Descriptors, observed[1]);
            AssertSelection(first.JobArea(), second.JobArea(), data.Areas, observed[2]);
            AssertSelection(first.JobType(), second.JobType(), data.Types, observed[3]);
            AssertSelection(first.JobSeniority(), second.JobSeniority(), data.Seniorities, observed[4]);
            AssertSelection(first.JobDepartment(), second.JobDepartment(), data.Departments, observed[5]);
            AssertSelection(first.JobStatus(), second.JobStatus(), data.Statuses, observed[6]);
            AssertSelection(first.SalaryBand(), second.SalaryBand(), data.SalaryBands, observed[7]);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(observed[0].Count, Is.EqualTo(data.Titles.Length));
            Assert.That(observed[1].Count, Is.EqualTo(data.Descriptors.Length));
            Assert.That(observed[2].Count, Is.EqualTo(data.Areas.Length));
            Assert.That(observed[3].Count, Is.EqualTo(data.Types.Length));
            Assert.That(observed[4].Count, Is.EqualTo(data.Seniorities.Length));
            Assert.That(observed[5].Count, Is.EqualTo(data.Departments.Length));
            Assert.That(observed[6].Count, Is.EqualTo(data.Statuses.Length));
            Assert.That(observed[7].Count, Is.EqualTo(data.SalaryBands.Length));
        });
    }

    [Test]
    public void ZeroWeightWorkValuesAreNeverSelected()
    {
        Fake value = Fake.Create("en-US", 6703);
        Register(value, "en-US", ValidJson("developer", "développeur", true));

        int index = 0;
        while (index < 64)
        {
            Assert.That(value.Person().JobTitle(), Is.EqualTo("developer"));
            index++;
        }
    }

    [Test]
    public void CallerWorkDataUsesParentFallbackAndNormalization()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 6704);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "fr", ValidJson("de\u0301veloppeur", "ignoré", false));

        Assert.Multiple(() =>
        {
            Assert.That(value.Person().JobTitle(), Is.EqualTo("développeur"));
            Assert.That(value.Person().JobDescriptor(), Is.EqualTo("méthodique"));
            Assert.That(value.Person().JobArea(), Is.EqualTo("technologie"));
            Assert.That(value.Person().JobType(), Is.EqualTo("permanent"));
            Assert.That(value.Person().JobSeniority(), Is.EqualTo("confirmé"));
            Assert.That(value.Person().JobDepartment(), Is.EqualTo("ingénierie"));
            Assert.That(value.Person().JobStatus(), Is.EqualTo("actif"));
            Assert.That(value.Person().SalaryBand(), Is.EqualTo("bande A"));
            Assert.That(value.Locale().FallbackTrace("person.work").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void WorkStreamIsIndependentFromNameAndIdentityStreams()
    {
        Fake workFirst = Fake.Create(6705);
        Fake untouched = Fake.Create(6705);
        Fake otherFirst = Fake.Create(6705);
        Fake untouchedWork = Fake.Create(6705);

        int index = 0;
        while (index < 32)
        {
            workFirst.Person().JobTitle();
            workFirst.Person().JobDescriptor();
            workFirst.Person().JobArea();
            workFirst.Person().JobType();
            workFirst.Person().JobSeniority();
            workFirst.Person().JobDepartment();
            workFirst.Person().JobStatus();
            workFirst.Person().SalaryBand();
            otherFirst.Person().FullName();
            otherFirst.Person().Gender();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                workFirst.Person().FullName(),
                Is.EqualTo(untouched.Person().FullName()));
            Assert.That(
                workFirst.Person().Gender(),
                Is.EqualTo(untouched.Person().Gender()));
            Assert.That(
                otherFirst.Person().JobTitle(),
                Is.EqualTo(untouchedWork.Person().JobTitle()));
        });
    }

    [Test]
    public void MalformedWorkSchemasProduceDimensionSpecificDiagnostics()
    {
        string valid = ValidJson("developer", "ignored", false);

        AssertInvalid(
            valid.Replace("\"titles\":", "\"jobTitles\":"),
            "requires member 'titles'");
        AssertInvalid(
            valid.Replace("\"descriptors\":[", "\"descriptors\":[] , \"unused\":["),
            "descriptors must not be empty");
        AssertInvalid(
            valid.Replace("\"weight\":1", "\"weight\":-1"),
            "weight must be finite and nonnegative");
        AssertInvalid(
            valid.Replace(
                "{\"value\":\"technologie\",\"weight\":1}",
                "{\"value\":\"technologie\",\"weight\":1},{\"value\":\"technologie\",\"weight\":1}"),
            "duplicates 'technologie'");
        AssertInvalid(
            valid.Replace(
                "{\"value\":\"permanent\",\"weight\":1}",
                "{\"value\":\"permanent\",\"weight\":0}"),
            "types must contain a positive weight");
    }

    [Test]
    public void WorkValuesEnforceOutputLimitsWithoutConsumingEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6706);
        options.MaximumOutputSize = 10;
        Fake actual = Fake.Create(options);
        Register(actual, "en-US", ValidJson("Eleven chars", "ignored", false));
        Fake expected = Fake.Create(options);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => actual.Person().JobArea()).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            actual.Person().WorkRandomValue.Long(),
            Is.EqualTo(expected.Person().WorkRandomValue.Long()));
    }

    static void AssertSelection(
        string actual,
        string expected,
        PersonWorkValueData[] values,
        HashSet<string> observed)
    {
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(values.Select(value => value.Value), Does.Contain(actual));
        observed.Add(actual);
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = Fake.Create("en-US", 6799);
        Register(value, "en-US", json);

        FakeException exception = Assert.Throws<FakeException>(
            () => PersonWorkDataParser.Resolve(value.Context));

        Assert.That(exception.Message, Does.StartWith("Resolved locale field 'person.work'"));
        Assert.That(exception.Message, Does.Contain(message));
    }

    static void Register(Fake value, string locale, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object>
            {
                ["person.work"] = document.RootElement.Clone()
            });
    }

    static string ValidJson(
        string title,
        string zeroWeightTitle,
        bool includeZeroWeightTitle)
    {
        string additionalTitle = includeZeroWeightTitle
            ? ",{\"value\":\"" + zeroWeightTitle + "\",\"weight\":0}"
            : "";
        return $$"""
        {
          "titles":[{"value":"{{title}}","weight":1}{{additionalTitle}}],
          "descriptors":[{"value":"méthodique","weight":1}],
          "areas":[{"value":"technologie","weight":1}],
          "types":[{"value":"permanent","weight":1}],
          "seniorities":[{"value":"confirmé","weight":1}],
          "departments":[{"value":"ingénierie","weight":1}],
          "statuses":[{"value":"actif","weight":1}],
          "salaryBands":[{"value":"bande A","weight":1}]
        }
        """;
    }
}
