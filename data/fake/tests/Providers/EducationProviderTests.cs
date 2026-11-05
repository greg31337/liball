using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class EducationProviderTests
{
    [Test]
    public void BundledEducationDataDefinesEveryDimensionAndCorrelation()
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(Fake.Create(6801).Context);

        Assert.Multiple(() =>
        {
            Assert.That(data.Institutions, Has.Length.EqualTo(6));
            Assert.That(data.Subjects, Has.Length.EqualTo(6));
            Assert.That(data.Terms, Has.Length.EqualTo(4));
            Assert.That(data.Grades, Has.Length.EqualTo(9));
            Assert.That(data.StudentIdMasks, Has.Length.EqualTo(3));
            Assert.That(data.Subjects, Has.All.Matches<EducationSubjectData>(subject => subject.Courses.Length >= 3));
            Assert.That(data.Subjects, Has.All.Matches<EducationSubjectData>(subject => subject.Degrees.Length >= 2));
        });
    }

    [Test]
    public void EducationApisAreDeterministicLocaleSelections()
    {
        Fake firstFake = Fake.Create(6802);
        Fake secondFake = Fake.Create(6802);
        EducationLocaleData data = EducationLocaleDataParser.Resolve(firstFake.Context);
        EducationProvider first = firstFake.Education();
        EducationProvider second = secondFake.Education();

        int index = 0;
        while (index < 256)
        {
            string institution = first.Institution();
            string course = first.Course();
            string subject = first.Subject();
            string degree = first.Degree();
            string term = first.Term();
            string grade = first.Grade();
            string studentId = first.StudentId();

            Assert.Multiple(() =>
            {
                Assert.That(institution, Is.EqualTo(second.Institution()));
                Assert.That(course, Is.EqualTo(second.Course()));
                Assert.That(subject, Is.EqualTo(second.Subject()));
                Assert.That(degree, Is.EqualTo(second.Degree()));
                Assert.That(term, Is.EqualTo(second.Term()));
                Assert.That(grade, Is.EqualTo(second.Grade()));
                Assert.That(studentId, Is.EqualTo(second.StudentId()));
                Assert.That(data.Institutions.Select(value => value.Value), Does.Contain(institution));
                Assert.That(data.Subjects.SelectMany(value => value.Courses).Select(value => value.Value), Does.Contain(course));
                Assert.That(data.Subjects.Select(value => value.Value), Does.Contain(subject));
                Assert.That(data.Subjects.SelectMany(value => value.Degrees).Select(value => value.Value), Does.Contain(degree));
                Assert.That(data.Terms.Select(value => value.Value), Does.Contain(term));
                Assert.That(data.Grades.Select(value => value.Value), Does.Contain(grade));
                Assert.That(data.StudentIdMasks.Any(mask => MatchesMask(studentId, mask.Value)), Is.True);
            });
            index++;
        }
    }

    [Test]
    public void StudentProfileKeepsCourseAndDegreeCorrelatedWithSubject()
    {
        Fake value = Fake.Create("en-US", 6803);
        Register(value, "en-US", CorrelatedJson());

        int index = 0;
        while (index < 128)
        {
            StudentProfile profile = value.Education().Record();

            Assert.Multiple(() =>
            {
                Assert.That(profile.Institution, Is.EqualTo("Example Academy"));
                Assert.That(profile.Term, Is.EqualTo("Autumn"));
                Assert.That(profile.Grade, Is.EqualTo("Pass"));
                Assert.That(MatchesMask(profile.StudentId, "ID-####"), Is.True);
            });
            if (profile.Subject == "mathematics")
            {
                Assert.That(profile.Course, Is.EqualTo("Algebra"));
                Assert.That(profile.Degree, Is.EqualTo("Mathematics Degree"));
            }
            else
            {
                Assert.That(profile.Subject, Is.EqualTo("studio art"));
                Assert.That(profile.Course, Is.EqualTo("Painting"));
                Assert.That(profile.Degree, Is.EqualTo("Fine Arts Degree"));
            }
            index++;
        }
    }

    [Test]
    public void StudentProfilesAreNewDeterministicInstances()
    {
        EducationProvider first = Fake.Create(6804).Education();
        EducationProvider second = Fake.Create(6804).Education();

        StudentProfile firstProfile = first.Record();
        StudentProfile secondProfile = second.Record();

        Assert.Multiple(() =>
        {
            Assert.That(secondProfile, Is.Not.SameAs(firstProfile));
            Assert.That(secondProfile.Institution, Is.EqualTo(firstProfile.Institution));
            Assert.That(secondProfile.Course, Is.EqualTo(firstProfile.Course));
            Assert.That(secondProfile.Subject, Is.EqualTo(firstProfile.Subject));
            Assert.That(secondProfile.Degree, Is.EqualTo(firstProfile.Degree));
            Assert.That(secondProfile.Term, Is.EqualTo(firstProfile.Term));
            Assert.That(secondProfile.Grade, Is.EqualTo(firstProfile.Grade));
            Assert.That(secondProfile.StudentId, Is.EqualTo(firstProfile.StudentId));
        });
    }

    [Test]
    public void CallerEducationDataUsesParentFallbackAndNormalization()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 6805);
        options.NormalizationMode = NormalizationMode.Nfc;
        Fake value = Fake.Create(options);
        Register(value, "fr", SingleSubjectJson("Acade\u0301mie Exemple", "mathe\u0301matiques", "Alge\u0300bre"));

        Assert.Multiple(() =>
        {
            Assert.That(value.Education().Institution(), Is.EqualTo("Académie Exemple"));
            Assert.That(value.Education().Subject(), Is.EqualTo("mathématiques"));
            Assert.That(value.Education().Course(), Is.EqualTo("Algèbre"));
            Assert.That(value.Education().Degree(), Is.EqualTo("Licence"));
            Assert.That(value.Education().Term(), Is.EqualTo("Automne"));
            Assert.That(value.Education().Grade(), Is.EqualTo("Réussi"));
            Assert.That(MatchesMask(value.Education().StudentId(), "ETU-####"), Is.True);
            Assert.That(value.Locale().FallbackTrace("education.catalog").SourceLocale, Is.EqualTo("fr"));
        });
    }

    [Test]
    public void EducationStreamIsIndependentFromOtherProviderStreams()
    {
        Fake educationFirst = Fake.Create(6806);
        Fake untouched = Fake.Create(6806);
        Fake personFirst = Fake.Create(6806);
        Fake untouchedEducation = Fake.Create(6806);

        int index = 0;
        while (index < 32)
        {
            educationFirst.Education().Record();
            personFirst.Person().FullName();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(educationFirst.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
            Assert.That(personFirst.Education().Record().StudentId, Is.EqualTo(untouchedEducation.Education().Record().StudentId));
        });
    }

    [Test]
    public void MalformedEducationSchemasProduceDimensionSpecificDiagnostics()
    {
        string valid = SingleSubjectJson("Example Academy", "mathematics", "Algebra");

        AssertInvalid(valid.Replace("\"subjects\":", "\"studyAreas\":"), "requires member 'subjects'");
        AssertInvalid(valid.Replace("\"courses\":[", "\"courses\":[] , \"unused\":["), "courses must contain at least one item");
        AssertInvalid(valid.Replace("\"weight\":1", "\"weight\":-1"), "weight must be finite and nonnegative");
        AssertInvalid(
            valid.Replace(
                "{\"value\":\"Automne\",\"weight\":1}",
                "{\"value\":\"Automne\",\"weight\":1},{\"value\":\"Automne\",\"weight\":1}"),
            "contains duplicate value Automne");
        AssertInvalid(valid.Replace("ETU-####", "ETU-\\\\"), "contains an invalid mask escape");
        AssertInvalid(valid.Replace("ETU-####", "student-fixed"), "must contain at least one mask placeholder");
    }

    [Test]
    public void EducationValuesEnforceOutputLimitsWithoutConsumingEntropy()
    {
        FakeOptions options = FakeOptions.Create("en-US", 6807);
        options.MaximumOutputSize = 10;
        Fake actual = Fake.Create(options);
        Register(actual, "en-US", SingleSubjectJson("Eleven chars", "math", "Algebra"));
        Fake expected = Fake.Create(options);

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => actual.Education().Course()).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            actual.Education().RandomValue.Long(),
            Is.EqualTo(expected.Education().RandomValue.Long()));
    }

    static bool MatchesMask(string value, string mask)
    {
        int valueIndex = 0;
        int maskIndex = 0;
        while (maskIndex < mask.Length && valueIndex < value.Length)
        {
            char expected = mask[maskIndex];
            if (expected == '\\')
            {
                maskIndex++;
                if (maskIndex >= mask.Length || value[valueIndex] != mask[maskIndex])
                    return false;
            }
            else if (expected == '#')
            {
                if (value[valueIndex] < '0' || value[valueIndex] > '9')
                    return false;
            }
            else if (expected == '?')
            {
                char actual = value[valueIndex];
                bool upper = actual >= 'A' && actual <= 'Z';
                bool lower = actual >= 'a' && actual <= 'z';
                if (!upper && !lower)
                    return false;
            }
            else if (expected == '*')
            {
                char actual = value[valueIndex];
                bool digit = actual >= '0' && actual <= '9';
                bool upper = actual >= 'A' && actual <= 'Z';
                bool lower = actual >= 'a' && actual <= 'z';
                if (!digit && !upper && !lower)
                    return false;
            }
            else if (value[valueIndex] != expected)
                return false;
            maskIndex++;
            valueIndex++;
        }
        return maskIndex == mask.Length && valueIndex == value.Length;
    }

    static void AssertInvalid(string json, string message)
    {
        Fake value = Fake.Create("en-US", 6899);
        Register(value, "en-US", json);

        FakeException exception = Assert.Throws<FakeException>(
            () => EducationLocaleDataParser.Resolve(value.Context));

        Assert.That(exception.Message, Does.StartWith("Resolved locale field 'education.catalog'"));
        Assert.That(exception.Message, Does.Contain(message));
    }

    static void Register(Fake value, string locale, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            locale,
            new Dictionary<string, object>
            {
                ["education.catalog"] = document.RootElement.Clone()
            });
    }

    static string CorrelatedJson()
    {
        return """
        {
          "institutions":[{"value":"Example Academy","weight":1}],
          "subjects":[
            {"id":"mathematics","value":"mathematics","weight":1,"courses":[{"value":"Algebra","weight":1}],"degrees":[{"value":"Mathematics Degree","weight":1}]},
            {"id":"studio-art","value":"studio art","weight":1,"courses":[{"value":"Painting","weight":1}],"degrees":[{"value":"Fine Arts Degree","weight":1}]}
          ],
          "terms":[{"value":"Autumn","weight":1}],
          "grades":[{"value":"Pass","weight":1}],
          "studentIdMasks":[{"value":"ID-####","weight":1}]
        }
        """;
    }

    static string SingleSubjectJson(string institution, string subject, string course)
    {
        return $$"""
        {
          "institutions":[{"value":"{{institution}}","weight":1}],
          "subjects":[{"id":"subject","value":"{{subject}}","weight":1,"courses":[{"value":"{{course}}","weight":1}],"degrees":[{"value":"Licence","weight":1}]}],
          "terms":[{"value":"Automne","weight":1}],
          "grades":[{"value":"Réussi","weight":1}],
          "studentIdMasks":[{"value":"ETU-####","weight":1}]
        }
        """;
    }
}
