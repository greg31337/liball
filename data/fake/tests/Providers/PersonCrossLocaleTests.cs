using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class PersonCrossLocaleTests
{
    [Test]
    public void LocaleTemplatesPreserveWesternEastAsianAndRightToLeftOrdering()
    {
        Fake western = Create(
            "en-GB",
            7001,
            NamesJson("Avery", "Arden", "{given} {family}", ""));
        Fake eastAsian = Create(
            "ja-JP",
            7001,
            NamesJson("春", "山田", "{family}・{given}", ""));
        Fake rightToLeft = Create(
            "ar-EG",
            7001,
            NamesJson("ليلى", "منصور", "{given} {family}", ""));

        Assert.Multiple(() =>
        {
            Assert.That(western.Person().NativeName(), Is.EqualTo("Avery Arden"));
            Assert.That(eastAsian.Person().NativeName(), Is.EqualTo("山田・春"));
            Assert.That(rightToLeft.Person().NativeName(), Is.EqualTo("ليلى منصور"));
            Assert.That(
                rightToLeft.Text().Direction("ليلى منصور"),
                Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(eastAsian.Person().FirstName(), Is.EqualTo("春"));
            Assert.That(eastAsian.Person().LastName(), Is.EqualTo("山田"));
        });
    }

    [Test]
    public void ParentLocaleFallbackPreservesNativeOrderAndProfileCorrelation()
    {
        Fake value = Create(
            "zh-Hant-HK",
            7002,
            "zh-Hant",
            NamesJson("美玲", "陳", "{family}{given}", ""));

        FallbackTrace trace = value.Locale().FallbackTrace("person.names");
        PersonProfile profile = value.Person().Record();

        Assert.Multiple(() =>
        {
            Assert.That(trace.SourceLocale, Is.EqualTo("zh-Hant"));
            Assert.That(trace.UsedFallback, Is.True);
            Assert.That(
                trace.AttemptedLocales,
                Is.EqualTo(new string[] { "zh-Hant-HK", "zh-Hant" }));
            Assert.That(profile.FirstName, Is.EqualTo("美玲"));
            Assert.That(profile.LastName, Is.EqualTo("陳"));
            Assert.That(profile.FullName, Is.EqualTo("陳美玲"));
            Assert.That(profile.Locale, Is.EqualTo("zh-Hant-HK"));
            Assert.That(profile.Email, Does.EndWith("@example.com")
                .Or.EndWith("@example.net")
                .Or.EndWith("@example.org"));
            Assert.That(profile.Age, Is.EqualTo(value.Date().Age(profile.BirthDate)));
        });
    }

    [Test]
    public void EveryDocumentedNameEdgeCaseResolvesAcrossLocales()
    {
        string[] westernExpected = new string[]
        {
            "Aster",
            "Lena van Dijk",
            "Nadia Omarovna Vale",
            "Ana María Sol",
            "D'Arcy Vale",
            "Avery-Jane Vale",
            "Élodie Brontë",
            "ليلى منصور",
            "Avery Vale",
            "Alexandria-Cassandra Montgomery-Worthington the Third of Northbridge"
        };
        string[] rightToLeftExpected = new string[]
        {
            "شمس",
            "ليلى بنت منصور",
            "ليلى منصور أحمد",
            "ليلى نور منصور",
            "ليلى د'منصور",
            "ليلى-نور منصور",
            "لَيْلَى منصور",
            "ليلى منصور",
            "ليلى منصور",
            "ليلى نور الهدى منصور عبد الرحمن من مدينة المثال القديمة البعيدة"
        };
        Fake western = Create(
            "en-GB",
            7003,
            NamesJson(
                "Avery",
                "Arden",
                "{given} {family}",
                CasesJson(westernExpected)));
        Fake rightToLeft = Create(
            "ar-EG",
            7003,
            NamesJson(
                "ليلى",
                "منصور",
                "{given} {family}",
                CasesJson(rightToLeftExpected)));

        int index = 0;
        while (index < westernExpected.Length)
        {
            PersonNameCase nameCase = (PersonNameCase)index;
            Assert.Multiple(() =>
            {
                Assert.That(
                    western.Person().NameCase(nameCase),
                    Is.EqualTo(westernExpected[index]));
                Assert.That(
                    rightToLeft.Person().NameCase(nameCase),
                    Is.EqualTo(rightToLeftExpected[index]));
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(westernExpected[0], Does.Not.Contain(" "));
            Assert.That(westernExpected[1], Does.Contain(" van "));
            Assert.That(westernExpected[2], Does.Contain("ovna"));
            Assert.That(westernExpected[3].Split(' '), Has.Length.EqualTo(3));
            Assert.That(westernExpected[4], Does.Contain("'"));
            Assert.That(westernExpected[5], Does.Contain("-"));
            Assert.That(westernExpected[6].Any(character => character > 127), Is.True);
            Assert.That(
                rightToLeft.Text().Direction(rightToLeftExpected[7]),
                Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(westernExpected[8].Split(' '), Has.Length.EqualTo(2));
            Assert.That(westernExpected[9].Length, Is.GreaterThan(60));
        });
    }

    [Test]
    public void CrossLocaleGenerationRemainsSeedDeterministic()
    {
        Fake first = Create(
            "ja-JP",
            7004,
            NamesJson("春", "山田", "{family} {given}", ""));
        Fake second = Create(
            "ja-JP",
            7004,
            NamesJson("春", "山田", "{family} {given}", ""));

        int index = 0;
        while (index < 32)
        {
            Assert.That(first.Person().FullName(), Is.EqualTo(second.Person().FullName()));
            Assert.That(first.Person().Record().FullName, Is.EqualTo(second.Person().Record().FullName));
            index++;
        }
    }

    static Fake Create(string requestedLocale, long seed, string json)
    {
        return Create(requestedLocale, seed, requestedLocale, json);
    }

    static Fake Create(
        string requestedLocale,
        long seed,
        string dataLocale,
        string json)
    {
        Fake value = Fake.Create(requestedLocale, seed);
        using JsonDocument document = JsonDocument.Parse(json);
        value.Locale().RegisterFromLocaleAndFields(
            dataLocale,
            new Dictionary<string, object>
            {
                ["person.names"] = document.RootElement.Clone()
            });
        return value;
    }

    static string NamesJson(
        string given,
        string family,
        string template,
        string cases)
    {
        return $$"""
        {
          "sexCategories":[{"id":"universal","weight":1}],
          "components":{
            "given":[{"value":"{{given}}","weight":1,"sexCategories":[]}],
            "middle":[],
            "family":[{"value":"{{family}}","weight":1,"sexCategories":[]}],
            "prefix":[],
            "suffix":[]
          },
          "formats":{
            "full":[{"template":"{{template}}","weight":1}],
            "formal":[{"template":"{{template}}","weight":1}],
            "informal":[{"template":"{given}","weight":1}]
          }{{cases}}
        }
        """;
    }

    static string CasesJson(string[] values)
    {
        return $$"""
        ,"cases":{
          "mononym":[{{Entry(values[0])}}],
          "particle":[{{Entry(values[1])}}],
          "patronymic":[{{Entry(values[2])}}],
          "multiPart":[{{Entry(values[3])}}],
          "apostrophe":[{{Entry(values[4])}}],
          "hyphenated":[{{Entry(values[5])}}],
          "diacritic":[{{Entry(values[6])}}],
          "rightToLeft":[{{Entry(values[7])}}],
          "optionalComponentOmitted":[{{Entry(values[8])}}],
          "long":[{{Entry(values[9])}}]
        }
        """;
    }

    static string Entry(string value)
    {
        return JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["value"] = value,
                ["weight"] = 1,
                ["sexCategories"] = Array.Empty<string>()
            });
    }
}
