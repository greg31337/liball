using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonCompletionTests
{
    [Test]
    public void LocaleNameComponentsSupportNativeTransliteratedAndPluralAccess()
    {
        Fake value = CreateWithNames(7101);
        PersonProvider person = value.Person();

        Assert.Multiple(() =>
        {
            Assert.That(person.MaidenName(), Is.EqualTo("Смирнова"));
            Assert.That(person.Patronymic(), Is.EqualTo("Петровна"));
            Assert.That(person.Matronymic(), Is.EqualTo("Анновна"));
            Assert.That(person.NativeFirstName(), Is.EqualTo("Александра"));
            Assert.That(person.NativeMiddleName(), Is.EqualTo("Мария"));
            Assert.That(person.NativeLastName(), Is.EqualTo("Иванова"));
            Assert.That(person.NativeMaidenName(), Is.EqualTo("Смирнова"));
            Assert.That(person.NativePatronymic(), Is.EqualTo("Петровна"));
            Assert.That(person.NativeMatronymic(), Is.EqualTo("Анновна"));
            Assert.That(person.TransliteratedFirstName(), Is.EqualTo("Aleksandra"));
            Assert.That(person.TransliteratedMiddleName(), Is.EqualTo("Mariya"));
            Assert.That(person.TransliteratedLastName(), Is.EqualTo("Ivanova"));
            Assert.That(person.TransliteratedMaidenName(), Is.EqualTo("Smirnova"));
            Assert.That(person.TransliteratedPatronymic(), Is.EqualTo("Petrovna"));
            Assert.That(person.TransliteratedMatronymic(), Is.EqualTo("Annovna"));
            Assert.That(person.Initials(), Is.EqualTo("АИ"));
            Assert.That(person.InitialsFromCount(4), Is.EqualTo("АММИ"));
        });
    }

    [Test]
    public void InitialAndLifeConveniencesHonorBoundsAndCapturedAge()
    {
        FakeOptions options = FakeOptions.Create("en-US", 7102);
        options.ReferenceTime = new DateTimeOffset(
            2026,
            8,
            25,
            12,
            0,
            0,
            TimeSpan.Zero);
        Fake first = Fake.Create(options);
        Fake second = Fake.Create(options);

        Assert.That(first.Person().InitialsFromCount(0), Is.Empty);
        Assert.That(
            first.Person().RandomValue.Long(),
            Is.EqualTo(second.Person().RandomValue.Long()));

        System.DateOnly exact = first.Person().BirthDateFromAge(37);
        BirthDateAge pair = first.Person().BirthDateAndAge(20, 25);

        Assert.Multiple(() =>
        {
            Assert.That(first.Person().Age(exact), Is.EqualTo(37));
            Assert.That(pair.Age, Is.InRange(20, 25));
            Assert.That(first.Person().Age(pair.BirthDate), Is.EqualTo(pair.Age));
            Assert.That(
                () => first.Person().BirthDateFromAge(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void WorkAndEducationBridgesReturnDeterministicCorrelatedRecords()
    {
        PersonProvider first = Fake.Create(7103).Person();
        PersonProvider second = Fake.Create(7103).Person();

        PersonWorkProfile firstWork = first.Work();
        PersonWorkProfile secondWork = second.Work();
        StudentProfile firstEducation = first.Education();
        StudentProfile secondEducation = second.Education();

        Assert.Multiple(() =>
        {
            Assert.That(firstWork.Title, Is.EqualTo(secondWork.Title).And.Not.Empty);
            Assert.That(firstWork.Descriptor, Is.EqualTo(secondWork.Descriptor).And.Not.Empty);
            Assert.That(firstWork.Area, Is.EqualTo(secondWork.Area).And.Not.Empty);
            Assert.That(firstWork.Type, Is.EqualTo(secondWork.Type).And.Not.Empty);
            Assert.That(firstWork.Seniority, Is.EqualTo(secondWork.Seniority).And.Not.Empty);
            Assert.That(firstWork.Department, Is.EqualTo(secondWork.Department).And.Not.Empty);
            Assert.That(firstWork.Status, Is.EqualTo(secondWork.Status).And.Not.Empty);
            Assert.That(firstWork.SalaryBand, Is.EqualTo(secondWork.SalaryBand).And.Not.Empty);
            Assert.That(firstEducation.Institution, Is.EqualTo(secondEducation.Institution));
            Assert.That(firstEducation.Subject, Is.EqualTo(secondEducation.Subject));
            Assert.That(firstEducation.Course, Is.EqualTo(secondEducation.Course));
            Assert.That(firstEducation.Degree, Is.EqualTo(secondEducation.Degree));
            Assert.That(firstEducation.StudentId, Is.EqualTo(secondEducation.StudentId));
        });
    }

    [Test]
    public void ExpandedProfileExposesCorrelatedOptionalAndSyntheticFields()
    {
        Fake first = CreateWithNames(7104);
        Fake second = CreateWithNames(7104);

        PersonProfile actual = first.Person().Record();
        PersonProfile expected = second.Person().Record();

        Assert.Multiple(() =>
        {
            Assert.That(actual.FirstName, Is.EqualTo("Александра"));
            Assert.That(actual.MiddleName, Is.EqualTo("Мария"));
            Assert.That(actual.LastName, Is.EqualTo("Иванова"));
            Assert.That(actual.MaidenName, Is.EqualTo("Смирнова"));
            Assert.That(actual.Patronymic, Is.EqualTo("Петровна"));
            Assert.That(actual.Matronymic, Is.EqualTo("Анновна"));
            Assert.That(actual.Prefix, Is.EqualTo("Доктор"));
            Assert.That(actual.Suffix, Is.EqualTo("мл."));
            Assert.That(actual.Initials, Is.EqualTo("АПАМИС"));
            Assert.That(actual.NativeName, Is.EqualTo(actual.FullName));
            Assert.That(actual.TransliteratedName, Is.EqualTo(expected.TransliteratedName).And.Not.Empty);
            Assert.That(actual.TransliteratedName, Does.Not.Match("[^\\x00-\\x7F]"));
            Assert.That(actual.Pronouns.Display, Is.EqualTo(expected.Pronouns.Display).And.Not.Empty);
            Assert.That(actual.Identifiers.ProfileId, Is.EqualTo(expected.Identifiers.ProfileId));
            Assert.That(first.Identifier().ValidateUuid4(actual.Identifiers.ProfileId), Is.True);
            Assert.That(actual.Identifiers.Username, Is.EqualTo(actual.Username));
            Assert.That(actual.Identifiers.Email, Is.EqualTo(actual.Email));
            Assert.That(actual.Identifiers.EmailIsReservedExample, Is.True);
            Assert.That(actual.Identifiers.IsSynthetic, Is.True);
            Assert.That(actual.Identifiers.RepresentsReachableContact, Is.False);
            Assert.That(actual.AvatarDataUri, Is.EqualTo(expected.AvatarDataUri));
            Assert.That(actual.AvatarDataUri, Does.StartWith("data:image/svg+xml;base64,"));
            Assert.That(actual.FavoriteColor, Is.EqualTo(expected.FavoriteColor).And.Not.Empty);
            Assert.That(actual.Locale, Is.EqualTo("ru"));
            Assert.That(actual.Language, Is.EqualTo("ru"));
            Assert.That(actual.TimeZoneId, Is.EqualTo(expected.TimeZoneId).And.Not.Empty);
        });
    }

    static Fake CreateWithNames(long seed)
    {
        Fake value = Fake.Create("ru", seed);
        using JsonDocument document = JsonDocument.Parse(NamesJson());
        value.Locale().RegisterFromLocaleAndFields(
            "ru",
            new Dictionary<string, object>
            {
                ["person.names"] = document.RootElement.Clone()
            });
        return value;
    }

    static string NamesJson()
    {
        return """
        {
          "sexCategories":[{"id":"universal","weight":1}],
          "components":{
            "given":[{"value":"Александра","weight":1,"sexCategories":[]}],
            "middle":[{"value":"Мария","weight":1,"sexCategories":[]}],
            "family":[{"value":"Иванова","weight":1,"sexCategories":[]}],
            "maiden":[{"value":"Смирнова","weight":1,"sexCategories":[]}],
            "patronymic":[{"value":"Петровна","weight":1,"sexCategories":[]}],
            "matronymic":[{"value":"Анновна","weight":1,"sexCategories":[]}],
            "prefix":[{"value":"Доктор","weight":1,"sexCategories":[]}],
            "suffix":[{"value":"мл.","weight":1,"sexCategories":[]}]
          },
          "formats":{
            "full":[{"template":"{prefix} {given} {patronymic} {matronymic} {middle} {family} {maiden} {suffix}","weight":1}],
            "formal":[{"template":"{prefix} {given} {family}","weight":1}],
            "informal":[{"template":"{given}","weight":1}]
          }
        }
        """;
    }
}
