using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleMetadataExpansionTests
{
    [Test]
    public void BundledProfileExposesNamesConventionsAvailabilityAndSources()
    {
        LocaleInfo info = Fake.Create("en-US", 4501).Locale().Metadata("en-US");

        Assert.Multiple(() =>
        {
            Assert.That(info.HasProfile, Is.True);
            Assert.That(info.EnglishName, Is.EqualTo("English (United States)"));
            Assert.That(info.NativeName, Is.EqualTo("English (United States)"));
            Assert.That(info.Language, Is.EqualTo("en"));
            Assert.That(info.LanguageName, Is.EqualTo("English"));
            Assert.That(info.DefaultScript, Is.EqualTo("Latn"));
            Assert.That(info.ScriptName, Is.EqualTo("Latin"));
            Assert.That(info.Territory, Is.EqualTo("US"));
            Assert.That(info.TerritoryName, Is.EqualTo("United States"));
            Assert.That(info.Direction, Is.EqualTo(WritingDirection.LeftToRight));
            Assert.That(info.DefaultCurrencyCode, Is.EqualTo("USD"));
            Assert.That(info.DefaultCurrencyName, Is.EqualTo("US Dollar"));
            Assert.That(info.CallingCode, Is.EqualTo("+1"));
            Assert.That(info.TimeZoneCodes, Is.EqualTo(new string[] { "America/New_York" }));
            Assert.That(info.TimeZoneNames, Is.EqualTo(new string[] { "New York" }));
            Assert.That(info.DateOrder, Is.EqualTo(DateOrder.MonthDayYear));
            Assert.That(info.DecimalSeparator, Is.EqualTo("."));
            Assert.That(info.NativeProviders, Is.EqualTo(new string[] { "date", "locale" }));
            Assert.That(info.FallbackProviders, Is.EqualTo(new string[]
            {
                "address", "education", "internet", "person", "phone", "text"
            }));
            Assert.That(info.AvailableProviders, Is.EqualTo(new string[]
            {
                "address", "date", "education", "internet", "locale", "person", "phone", "text"
            }));
            Assert.That(info.ProfileSourceLocale, Is.EqualTo("en-US"));
            Assert.That(info.ProfileUsedFallback, Is.False);
            Assert.That(info.ProfileDataVersion, Is.EqualTo("1"));
            Assert.That(info.ProfileProvenance, Does.Contain("Unicode CLDR 47 locale metadata"));
            Assert.That(info.AddressFormat, Does.Contain("{buildingNumber}").And.Contain("{country}"));
            Assert.That(info.AddressFormatSourceLocale, Is.EqualTo("invariant"));
            Assert.That(info.AddressFormatUsedFallback, Is.True);
            Assert.That(info.AddressFormatDataVersion, Is.EqualTo("1"));
            Assert.That(info.AddressFormatProvenance, Does.Contain("project-authored"));
        });
    }

    [Test]
    public void RightToLeftProfileUsesNativeNamesAndTypedDirection()
    {
        LocaleInfo info = Fake.Create("ar-SA", 4502).Locale().Metadata("ar-SA");

        Assert.Multiple(() =>
        {
            Assert.That(info.NativeName, Is.EqualTo("العربية (المملكة العربية السعودية)"));
            Assert.That(info.LanguageName, Is.EqualTo("العربية"));
            Assert.That(info.TerritoryName, Is.EqualTo("المملكة العربية السعودية"));
            Assert.That(info.DefaultScript, Is.EqualTo("Arab"));
            Assert.That(info.ScriptName, Is.EqualTo("Arabic"));
            Assert.That(info.Direction, Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(info.DefaultCurrencyCode, Is.EqualTo("SAR"));
            Assert.That(info.CallingCode, Is.EqualTo("+966"));
            Assert.That(info.DateOrder, Is.EqualTo(DateOrder.DayMonthYear));
            Assert.That(info.DecimalSeparator, Is.EqualTo("٫"));
        });
    }

    [Test]
    public void MetadataReportsParentAndInvariantFallbackSources()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", "en-US", 4503L);
        options.DataVersion = "test";
        LocaleProvider locale = Fake.Create(options).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "marker", Json("\"exact\""), "exact source"),
            Value("fr", "locale.profile", Profile("French", "français (France)", "fr", "Latn", "ltr", "FR", "EUR", "+33", "Europe/Paris", "dmy", ","), "parent profile"),
            Value("invariant", "address.catalog", Json("{}"), "generic address")
        };

        LocaleInfo info = locale.MetadataFromLocaleAndValues("fr-CA", values);

        Assert.Multiple(() =>
        {
            Assert.That(info.ProfileSourceLocale, Is.EqualTo("fr"));
            Assert.That(info.ProfileUsedFallback, Is.True);
            Assert.That(info.ProfileProvenance, Is.EqualTo("parent profile"));
            Assert.That(info.LanguageName, Is.EqualTo("français"));
            Assert.That(info.AddressFormatSourceLocale, Is.EqualTo("invariant"));
            Assert.That(info.AddressFormatUsedFallback, Is.True);
            Assert.That(info.AddressFormatProvenance, Is.EqualTo("generic address"));
        });
    }

    [Test]
    public void ScriptAndTerritoryCatalogsAreOrderedDistinctAndIsolated()
    {
        LocaleProvider locale = Fake.Create(4504).Locale();

        LocaleNameRecord[] scripts = locale.Scripts();
        LocaleNameRecord[] territories = locale.Territories();

        Assert.Multiple(() =>
        {
            Assert.That(scripts.Select(value => value.Code), Is.Ordered);
            Assert.That(scripts.Select(value => value.Code), Does.Contain("Arab").And.Contain("Latn").And.Contain("Hant"));
            Assert.That(scripts.Single(value => value.Code == "Cyrl").Name, Is.EqualTo("Cyrillic"));
            Assert.That(territories.Select(value => value.Code), Is.Ordered);
            Assert.That(territories.Select(value => value.Code), Does.Contain("DE").And.Contain("JP").And.Contain("US"));
            Assert.That(territories.Single(value => value.Code == "DE").Name, Is.EqualTo("Deutschland"));
            Assert.That(territories, Has.All.Matches<LocaleNameRecord>(value => value.DataVersion == "1" && value.Provenance.Length > 0));
        });

        scripts[0].Code = "changed";
        territories[0].Name = "changed";

        Assert.That(locale.Scripts()[0].Code, Is.Not.EqualTo("changed"));
        Assert.That(locale.Territories()[0].Name, Is.Not.EqualTo("changed"));
    }

    [Test]
    public void SuppliedCatalogOperationsValidateProfilesAndLimits()
    {
        FakeOptions options = FakeOptions.Create("en-US", 4505L);
        options.DataVersion = "test";
        options.MaximumCount = 2;
        LocaleProvider locale = Fake.Create(options).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("en-US", "locale.profile", Profile("English", "English (United States)", "en", "Latn", "ltr", "US", "USD", "+1", "America/New_York", "mdy", "."), "profile")
        };

        Assert.That(locale.ScriptsFromValues(values).Single().Code, Is.EqualTo("Latn"));
        Assert.That(locale.TerritoriesFromValues(values).Single().Code, Is.EqualTo("US"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => locale.ScriptsFromValues(null)).ParamName, Is.EqualTo("values"));
        Assert.Throws<FakeException>(() => locale.TerritoriesFromValues(new LocaleFieldValue[]
        {
            Value("en-US", "locale.profile", Json("\"not an object\""), "bad profile")
        }));
    }

    static LocaleFieldValue Value(string locale, string field, object value, string provenance)
    {
        return LocaleFieldValue.WithMetadata(locale, field, value, "test", provenance);
    }

    static JsonElement Json(string json)
    {
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            return document.RootElement.Clone();
        }
    }

    static JsonElement Profile(
        string englishName,
        string nativeName,
        string language,
        string script,
        string direction,
        string territory,
        string currency,
        string callingCode,
        string timeZone,
        string dateOrder,
        string decimalSeparator)
    {
        string json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["englishName"] = englishName,
            ["nativeName"] = nativeName,
            ["language"] = language,
            ["script"] = script,
            ["direction"] = direction,
            ["territory"] = territory,
            ["currency"] = currency,
            ["callingCode"] = callingCode,
            ["timeZones"] = new string[] { timeZone },
            ["dateOrder"] = dateOrder,
            ["decimalSeparator"] = decimalSeparator,
            ["nativeProviders"] = new string[] { "locale" },
            ["fallbackProviders"] = new string[] { "address" }
        });
        return Json(json);
    }
}
