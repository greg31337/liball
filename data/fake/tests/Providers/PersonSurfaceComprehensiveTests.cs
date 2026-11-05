using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonSurfaceComprehensiveTests
{
    [Test]
    public void ProfilesPreserveAllComponentsAcrossLatinJapaneseAndRightToLeftOrders()
    {
        Fake latin = Create(
            "en-US",
            7201,
            NamesJson(
                "Amina",
                "Noor",
                "Linden",
                "Vale",
                "Petrovna",
                "Mariamovna",
                "Dr.",
                "Jr.",
                "{prefix} {given} {middle} {patronymic} {matronymic} {family} {maiden} {suffix}"));
        Fake japanese = Create(
            "ja-JP",
            7201,
            NamesJson(
                "春",
                "美",
                "山田",
                "佐藤",
                "太郎子",
                "花子子",
                "氏",
                "様",
                "{prefix}{family}{maiden}・{given}{middle}・{patronymic}{matronymic}{suffix}"));
        Fake arabic = Create(
            "ar-EG",
            7201,
            NamesJson(
                "ليلى",
                "نور",
                "منصور",
                "حداد",
                "أحمد",
                "مريم",
                "د.",
                "الثاني",
                "{prefix} {given} {patronymic} {matronymic} {middle} {family} {maiden} {suffix}"));

        PersonProfile latinProfile = latin.Person().Record();
        PersonProfile japaneseProfile = japanese.Person().Record();
        PersonProfile arabicProfile = arabic.Person().Record();

        Assert.Multiple(() =>
        {
            Assert.That(
                latinProfile.FullName,
                Is.EqualTo("Dr. Amina Noor Petrovna Mariamovna Linden Vale Jr."));
            Assert.That(latinProfile.Initials, Is.EqualTo("ANPMLV"));
            AssertComponents(
                latinProfile,
                "Amina",
                "Noor",
                "Linden",
                "Vale",
                "Petrovna",
                "Mariamovna",
                "Dr.",
                "Jr.");

            Assert.That(
                japaneseProfile.FullName,
                Is.EqualTo("氏山田佐藤・春美・太郎子花子子様"));
            Assert.That(japaneseProfile.Initials, Is.EqualTo("山佐春美太花"));
            AssertComponents(
                japaneseProfile,
                "春",
                "美",
                "山田",
                "佐藤",
                "太郎子",
                "花子子",
                "氏",
                "様");

            Assert.That(
                arabicProfile.FullName,
                Is.EqualTo("د. ليلى أحمد مريم نور منصور حداد الثاني"));
            Assert.That(arabicProfile.Initials, Is.EqualTo("لأمنمح"));
            Assert.That(
                arabic.Text().Direction(arabicProfile.NativeName),
                Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(arabicProfile.TransliteratedName, Does.Not.Match("[^\\x00-\\x7F]"));
            AssertComponents(
                arabicProfile,
                "ليلى",
                "نور",
                "منصور",
                "حداد",
                "أحمد",
                "مريم",
                "د.",
                "الثاني");
        });
    }

    [Test]
    public void LegacyPacksFallbackForScalarComponentsButProfilesExposeOmissionsAsEmpty()
    {
        Fake scalar = CreateLegacy(7202, "{given} {family}");
        Fake profileFake = CreateLegacy(7202, "{given} {family}");

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Person().MaidenName(), Is.EqualTo("Arden"));
            Assert.That(scalar.Person().Patronymic(), Is.EqualTo("Noor"));
            Assert.That(scalar.Person().Matronymic(), Is.EqualTo("Noor"));
        });

        PersonProfile profile = profileFake.Person().Record();

        Assert.Multiple(() =>
        {
            Assert.That(profile.FullName, Is.EqualTo("Avery Arden"));
            Assert.That(profile.Initials, Is.EqualTo("AA"));
            Assert.That(profile.MiddleName, Is.Empty);
            Assert.That(profile.MaidenName, Is.Empty);
            Assert.That(profile.Patronymic, Is.Empty);
            Assert.That(profile.Matronymic, Is.Empty);
            Assert.That(profile.Prefix, Is.Empty);
            Assert.That(profile.Suffix, Is.Empty);
        });
    }

    [Test]
    public void MononymAndLongTemplatesRemainCorrelatedWithoutTruncation()
    {
        Fake mononymFake = CreateLegacy(7203, "{given}");
        string longGiven = new string('A', 220);
        string longMiddle = new string('B', 180);
        string longFamily = new string('C', 220);
        Fake longFake = Create(
            "en-US",
            7203,
            NamesJson(
                longGiven,
                longMiddle,
                longFamily,
                "Vale",
                "Petrovna",
                "Mariamovna",
                "Dr.",
                "Jr.",
                "{given} {middle} {family}"));

        PersonProfile mononym = mononymFake.Person().Record();
        PersonProfile longProfile = longFake.Person().Record();
        string explicitMononym = Fake.Create(7204).Person().NameCase(PersonNameCase.Mononym);
        string explicitLong = Fake.Create(7204).Person().NameCase(PersonNameCase.Long);

        Assert.Multiple(() =>
        {
            Assert.That(mononym.FullName, Is.EqualTo("Avery"));
            Assert.That(mononym.Initials, Is.EqualTo("A"));
            Assert.That(mononym.MiddleName, Is.Empty);
            Assert.That(explicitMononym, Does.Not.Contain(" "));
            Assert.That(longProfile.FullName, Is.EqualTo(longGiven + " " + longMiddle + " " + longFamily));
            Assert.That(longProfile.FullName.Length, Is.EqualTo(622));
            Assert.That(longProfile.Initials, Is.EqualTo("ABC"));
            Assert.That(longProfile.Username, Does.StartWith(longGiven.ToLowerInvariant() + "."));
            Assert.That(explicitLong.Length, Is.GreaterThan(60));
        });
    }

    [Test]
    public void NamingCategoriesRemainApplicableAndIndependentFromIdentityText()
    {
        Fake nameFirst = CreateCategories(7205);
        Fake untouchedIdentity = CreateCategories(7205);
        Fake identityFirst = CreateCategories(7205);
        Fake untouchedNames = CreateCategories(7205);

        Assert.Multiple(() =>
        {
            Assert.That(
                nameFirst.Person().MaidenNameFromSexCategory("female"),
                Is.EqualTo("Vale-F"));
            Assert.That(
                nameFirst.Person().PatronymicFromSexCategory("male"),
                Is.EqualTo("Petrov-M"));
            Assert.That(
                nameFirst.Person().MatronymicFromSexCategory("female"),
                Is.EqualTo("Mariam-F"));
            Assert.That(nameFirst.Person().InitialsFromCount(3), Is.EqualTo("AFA"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                nameFirst.Person().Gender(),
                Is.EqualTo(untouchedIdentity.Person().Gender()));
            Assert.That(
                nameFirst.Person().Pronouns().Display,
                Is.EqualTo(untouchedIdentity.Person().Pronouns().Display));
        });

        identityFirst.Person().Gender();
        identityFirst.Person().Pronouns();

        Assert.That(
            identityFirst.Person().MaidenNameFromSexCategory("male"),
            Is.EqualTo(untouchedNames.Person().MaidenNameFromSexCategory("male")));
    }

    [Test]
    public void ExactAgesHoldAcrossReferenceDateBoundariesAndSeeds()
    {
        FakeOptions firstOptions = FakeOptions.Create("en-US", 7206);
        firstOptions.ReferenceTime = new DateTimeOffset(
            2024,
            2,
            29,
            23,
            59,
            59,
            TimeSpan.FromHours(-5));
        FakeOptions secondOptions = FakeOptions.Create("en-US", 7206);
        secondOptions.ReferenceTime = firstOptions.ReferenceTime;
        PersonProvider first = Fake.Create(firstOptions).Person();
        PersonProvider second = Fake.Create(secondOptions).Person();
        int[] ages = new int[] { 0, 1, 18, 37, 80, 120 };

        int index = 0;
        while (index < ages.Length)
        {
            System.DateOnly actual = first.BirthDateFromAge(ages[index]);
            System.DateOnly expected = second.BirthDateFromAge(ages[index]);
            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(first.Age(actual), Is.EqualTo(ages[index]));
            });
            index++;
        }

        BirthDateAge pair = first.BirthDateAndAge(64, 64);
        Assert.Multiple(() =>
        {
            Assert.That(pair.Age, Is.EqualTo(64));
            Assert.That(first.Age(pair.BirthDate), Is.EqualTo(64));
            Assert.That(
                () => first.Age(new System.DateOnly(2024, 3, 1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ProfilesKeepContactAndIdentifierDataReservedInEverySafetyMode()
    {
        int mode = 0;
        while (mode <= 2)
        {
            FakeOptions options = FakeOptions.Create("en-US", 7210 + mode);
            options.SafetyMode = (SafetyMode)mode;
            Fake value = Fake.Create(options);
            PersonProfile profile = value.Person().Record();
            string domain = profile.Email.Substring(profile.Email.IndexOf('@') + 1);

            Assert.Multiple(() =>
            {
                Assert.That(profile.Email, Does.StartWith(profile.Username + "@"));
                Assert.That(
                    domain,
                    Is.EqualTo("example.com")
                        .Or.EqualTo("example.net")
                        .Or.EqualTo("example.org"));
                Assert.That(profile.Phone, Does.Match("^\\+1-(415|212|312)-555-01[0-9]{2}$"));
                Assert.That(profile.Address, Does.Contain("00000"));
                Assert.That(profile.Identifiers.Username, Is.EqualTo(profile.Username));
                Assert.That(profile.Identifiers.Email, Is.EqualTo(profile.Email));
                Assert.That(profile.Identifiers.EmailIsReservedExample, Is.True);
                Assert.That(profile.Identifiers.IsSynthetic, Is.True);
                Assert.That(profile.Identifiers.RepresentsReachableContact, Is.False);
                Assert.That(value.Identifier().ValidateUuid4(profile.Identifiers.ProfileId), Is.True);
                Assert.That(profile.AvatarDataUri, Does.StartWith("data:image/svg+xml;base64,"));
            });
            mode++;
        }
    }

    [Test]
    public void NewPersonSurfacesExposeParentAndInvariantFallbackTraces()
    {
        Fake value = Create(
            "fr-CA",
            7214,
            "fr",
            NamesJson(
                "Élodie",
                "Noor",
                "Bernard",
                "Moreau",
                "Petrovna",
                "Mariamovna",
                "Dre",
                "II",
                "{given} {patronymic} {family}"));

        PersonProfile profile = value.Person().Record();
        PersonWorkProfile work = value.Person().Work();
        StudentProfile education = value.Person().Education();

        Assert.Multiple(() =>
        {
            Assert.That(profile.FullName, Is.EqualTo("Élodie Petrovna Bernard"));
            Assert.That(work.Title, Is.Not.Empty);
            Assert.That(education.Institution, Is.Not.Empty);
            AssertTrace(value, "person.names", "fr", true);
            AssertTrace(value, "person.identity", "invariant", true);
            AssertTrace(value, "person.profile", "invariant", true);
            AssertTrace(value, "person.work", "invariant", true);
            AssertTrace(value, "education.catalog", "invariant", true);
        });
    }

    [Test]
    public void EveryNewScalarAndRecordRemainsSeedReproducible()
    {
        Fake first = Create(
            "ar-EG",
            7215,
            NamesJson(
                "ليلى",
                "نور",
                "منصور",
                "حداد",
                "أحمد",
                "مريم",
                "د.",
                "الثاني",
                "{given} {patronymic} {family}"));
        Fake second = Create(
            "ar-EG",
            7215,
            NamesJson(
                "ليلى",
                "نور",
                "منصور",
                "حداد",
                "أحمد",
                "مريم",
                "د.",
                "الثاني",
                "{given} {patronymic} {family}"));

        int index = 0;
        while (index < 16)
        {
            Assert.Multiple(() =>
            {
                Assert.That(first.Person().MaidenName(), Is.EqualTo(second.Person().MaidenName()));
                Assert.That(first.Person().Patronymic(), Is.EqualTo(second.Person().Patronymic()));
                Assert.That(first.Person().Matronymic(), Is.EqualTo(second.Person().Matronymic()));
                Assert.That(first.Person().InitialsFromCount(5), Is.EqualTo(second.Person().InitialsFromCount(5)));
                Assert.That(first.Person().NativeFirstName(), Is.EqualTo(second.Person().NativeFirstName()));
                Assert.That(first.Person().TransliteratedLastName(), Is.EqualTo(second.Person().TransliteratedLastName()));
                Assert.That(first.Person().BirthDate(), Is.EqualTo(second.Person().BirthDate()));
            });

            AssertProfileEqual(first.Person().Record(), second.Person().Record());
            AssertWorkEqual(first.Person().Work(), second.Person().Work());
            AssertEducationEqual(first.Person().Education(), second.Person().Education());
            index++;
        }
    }

    static void AssertComponents(
        PersonProfile profile,
        string given,
        string middle,
        string family,
        string maiden,
        string patronymic,
        string matronymic,
        string prefix,
        string suffix)
    {
        Assert.That(profile.FirstName, Is.EqualTo(given));
        Assert.That(profile.MiddleName, Is.EqualTo(middle));
        Assert.That(profile.LastName, Is.EqualTo(family));
        Assert.That(profile.MaidenName, Is.EqualTo(maiden));
        Assert.That(profile.Patronymic, Is.EqualTo(patronymic));
        Assert.That(profile.Matronymic, Is.EqualTo(matronymic));
        Assert.That(profile.Prefix, Is.EqualTo(prefix));
        Assert.That(profile.Suffix, Is.EqualTo(suffix));
        Assert.That(profile.NativeName, Is.EqualTo(profile.FullName));
    }

    static void AssertTrace(
        Fake value,
        string field,
        string sourceLocale,
        bool usedFallback)
    {
        FallbackTrace trace = value.Locale().FallbackTrace(field);
        Assert.That(trace.SourceLocale, Is.EqualTo(sourceLocale));
        Assert.That(trace.UsedFallback, Is.EqualTo(usedFallback));
    }

    static void AssertProfileEqual(PersonProfile actual, PersonProfile expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.FullName, Is.EqualTo(expected.FullName));
            Assert.That(actual.MiddleName, Is.EqualTo(expected.MiddleName));
            Assert.That(actual.MaidenName, Is.EqualTo(expected.MaidenName));
            Assert.That(actual.Patronymic, Is.EqualTo(expected.Patronymic));
            Assert.That(actual.Matronymic, Is.EqualTo(expected.Matronymic));
            Assert.That(actual.Initials, Is.EqualTo(expected.Initials));
            Assert.That(actual.TransliteratedName, Is.EqualTo(expected.TransliteratedName));
            Assert.That(actual.Pronouns.Display, Is.EqualTo(expected.Pronouns.Display));
            Assert.That(actual.BirthDate, Is.EqualTo(expected.BirthDate));
            Assert.That(actual.Age, Is.EqualTo(expected.Age));
            Assert.That(actual.Identifiers.ProfileId, Is.EqualTo(expected.Identifiers.ProfileId));
            Assert.That(actual.Identifiers.Email, Is.EqualTo(expected.Identifiers.Email));
            Assert.That(actual.AvatarDataUri, Is.EqualTo(expected.AvatarDataUri));
            Assert.That(actual.FavoriteColor, Is.EqualTo(expected.FavoriteColor));
            Assert.That(actual.Language, Is.EqualTo(expected.Language));
            Assert.That(actual.TimeZoneId, Is.EqualTo(expected.TimeZoneId));
        });
    }

    static void AssertWorkEqual(PersonWorkProfile actual, PersonWorkProfile expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo(expected.Title));
            Assert.That(actual.Descriptor, Is.EqualTo(expected.Descriptor));
            Assert.That(actual.Area, Is.EqualTo(expected.Area));
            Assert.That(actual.Type, Is.EqualTo(expected.Type));
            Assert.That(actual.Seniority, Is.EqualTo(expected.Seniority));
            Assert.That(actual.Department, Is.EqualTo(expected.Department));
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.SalaryBand, Is.EqualTo(expected.SalaryBand));
        });
    }

    static void AssertEducationEqual(StudentProfile actual, StudentProfile expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Institution, Is.EqualTo(expected.Institution));
            Assert.That(actual.Course, Is.EqualTo(expected.Course));
            Assert.That(actual.Subject, Is.EqualTo(expected.Subject));
            Assert.That(actual.Degree, Is.EqualTo(expected.Degree));
            Assert.That(actual.Term, Is.EqualTo(expected.Term));
            Assert.That(actual.Grade, Is.EqualTo(expected.Grade));
            Assert.That(actual.StudentId, Is.EqualTo(expected.StudentId));
        });
    }

    static Fake Create(string locale, long seed, string json)
    {
        return Create(locale, seed, locale, json);
    }

    static Fake Create(
        string locale,
        long seed,
        string dataLocale,
        string json)
    {
        Fake value = Fake.Create(locale, seed);
        Register(value, dataLocale, json);
        return value;
    }

    static Fake CreateLegacy(long seed, string template)
    {
        return Create(
            "en-US",
            seed,
            $$"""
            {
              "sexCategories":[{"id":"universal","weight":1}],
              "components":{
                "given":[{{Entry("Avery", new string[0])}}],
                "middle":[{{Entry("Noor", new string[0])}}],
                "family":[{{Entry("Arden", new string[0])}}],
                "prefix":[{{Entry("Dr.", new string[0])}}],
                "suffix":[{{Entry("Jr.", new string[0])}}]
              },
              "formats":{
                "full":[{"template":{{JsonSerializer.Serialize(template)}},"weight":1}],
                "formal":[{"template":"{given} {family}","weight":1}],
                "informal":[{"template":"{given}","weight":1}]
              }
            }
            """);
    }

    static Fake CreateCategories(long seed)
    {
        string json = """
        {
          "sexCategories":[
            {"id":"female","weight":1},
            {"id":"male","weight":1}
          ],
          "components":{
            "given":[
              {"value":"Amina","weight":1,"sexCategories":["female"]},
              {"value":"Milo","weight":1,"sexCategories":["male"]}
            ],
            "middle":[{"value":"Faye","weight":1,"sexCategories":["female"]}],
            "family":[{"value":"Arden","weight":1,"sexCategories":[]}],
            "maiden":[
              {"value":"Vale-F","weight":1,"sexCategories":["female"]},
              {"value":"Vale-M","weight":1,"sexCategories":["male"]}
            ],
            "patronymic":[
              {"value":"Petrov-F","weight":1,"sexCategories":["female"]},
              {"value":"Petrov-M","weight":1,"sexCategories":["male"]}
            ],
            "matronymic":[
              {"value":"Mariam-F","weight":1,"sexCategories":["female"]},
              {"value":"Mariam-M","weight":1,"sexCategories":["male"]}
            ],
            "prefix":[],
            "suffix":[]
          },
          "formats":{
            "full":[{"template":"{given} {family}","weight":1}],
            "formal":[{"template":"{given} {family}","weight":1}],
            "informal":[{"template":"{given}","weight":1}]
          }
        }
        """;
        return Create("en-US", seed, json);
    }

    static string NamesJson(
        string given,
        string middle,
        string family,
        string maiden,
        string patronymic,
        string matronymic,
        string prefix,
        string suffix,
        string template)
    {
        return $$"""
        {
          "sexCategories":[{"id":"universal","weight":1}],
          "components":{
            "given":[{{Entry(given, new string[0])}}],
            "middle":[{{Entry(middle, new string[0])}}],
            "family":[{{Entry(family, new string[0])}}],
            "maiden":[{{Entry(maiden, new string[0])}}],
            "patronymic":[{{Entry(patronymic, new string[0])}}],
            "matronymic":[{{Entry(matronymic, new string[0])}}],
            "prefix":[{{Entry(prefix, new string[0])}}],
            "suffix":[{{Entry(suffix, new string[0])}}]
          },
          "formats":{
            "full":[{"template":{{JsonSerializer.Serialize(template)}},"weight":1}],
            "formal":[{"template":"{prefix} {given} {family} {suffix}","weight":1}],
            "informal":[{"template":"{given}","weight":1}]
          }
        }
        """;
    }

    static string Entry(string value, string[] categories)
    {
        return JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["value"] = value,
                ["weight"] = 1,
                ["sexCategories"] = categories
            });
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
}
