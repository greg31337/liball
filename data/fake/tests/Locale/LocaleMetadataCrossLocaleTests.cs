using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleMetadataCrossLocaleTests
{
    static object[] WaveOneCases = new object[]
    {
        Case("en-US", "English (United States)", "English", "United States", "USD", "+1", "America/New_York", DateOrder.MonthDayYear, "."),
        Case("en-GB", "English (United Kingdom)", "English", "United Kingdom", "GBP", "+44", "Europe/London", DateOrder.DayMonthYear, "."),
        Case("en-CA", "English (Canada)", "English", "Canada", "CAD", "+1", "America/Toronto", DateOrder.YearMonthDay, "."),
        Case("en-AU", "English (Australia)", "English", "Australia", "AUD", "+61", "Australia/Sydney", DateOrder.DayMonthYear, "."),
        Case("de-DE", "Deutsch (Deutschland)", "Deutsch", "Deutschland", "EUR", "+49", "Europe/Berlin", DateOrder.DayMonthYear, ","),
        Case("de-AT", "Deutsch (Österreich)", "Deutsch", "Österreich", "EUR", "+43", "Europe/Vienna", DateOrder.DayMonthYear, ","),
        Case("de-CH", "Deutsch (Schweiz)", "Deutsch", "Schweiz", "CHF", "+41", "Europe/Zurich", DateOrder.DayMonthYear, "."),
        Case("fr-FR", "français (France)", "français", "France", "EUR", "+33", "Europe/Paris", DateOrder.DayMonthYear, ","),
        Case("fr-CA", "français (Canada)", "français", "Canada", "CAD", "+1", "America/Toronto", DateOrder.YearMonthDay, ","),
        Case("es-ES", "español (España)", "español", "España", "EUR", "+34", "Europe/Madrid", DateOrder.DayMonthYear, ","),
        Case("es-MX", "español (México)", "español", "México", "MXN", "+52", "America/Mexico_City", DateOrder.DayMonthYear, "."),
        Case("pt-PT", "português (Portugal)", "português", "Portugal", "EUR", "+351", "Europe/Lisbon", DateOrder.DayMonthYear, ","),
        Case("pt-BR", "português (Brasil)", "português", "Brasil", "BRL", "+55", "America/Sao_Paulo", DateOrder.DayMonthYear, ","),
        Case("it-IT", "italiano (Italia)", "italiano", "Italia", "EUR", "+39", "Europe/Rome", DateOrder.DayMonthYear, ","),
        Case("nl-NL", "Nederlands (Nederland)", "Nederlands", "Nederland", "EUR", "+31", "Europe/Brussels", DateOrder.DayMonthYear, ","),
        Case("pl-PL", "polski (Polska)", "polski", "Polska", "PLN", "+48", "Europe/Warsaw", DateOrder.DayMonthYear, ","),
        Case("cs-CZ", "čeština (Česko)", "čeština", "Česko", "CZK", "+420", "Europe/Prague", DateOrder.DayMonthYear, ","),
        Case("sv-SE", "svenska (Sverige)", "svenska", "Sverige", "SEK", "+46", "Europe/Berlin", DateOrder.YearMonthDay, ","),
        Case("nb-NO", "norsk bokmål (Norge)", "norsk bokmål", "Norge", "NOK", "+47", "Europe/Berlin", DateOrder.DayMonthYear, ","),
        Case("da-DK", "dansk (Danmark)", "dansk", "Danmark", "DKK", "+45", "Europe/Berlin", DateOrder.DayMonthYear, ","),
        Case("fi-FI", "suomi (Suomi)", "suomi", "Suomi", "EUR", "+358", "Europe/Helsinki", DateOrder.DayMonthYear, ",")
    };

    static object[] WaveTwoCases = new object[]
    {
        ScriptCase("ar-EG", "العربية (مصر)", "العربية", "مصر", "Arab", WritingDirection.RightToLeft, "EGP", "+20", "Africa/Cairo", DateOrder.DayMonthYear, "٫"),
        ScriptCase("ar-SA", "العربية (المملكة العربية السعودية)", "العربية", "المملكة العربية السعودية", "Arab", WritingDirection.RightToLeft, "SAR", "+966", "Asia/Riyadh", DateOrder.DayMonthYear, "٫"),
        ScriptCase("fa-IR", "فارسی (ایران)", "فارسی", "ایران", "Arab", WritingDirection.RightToLeft, "IRR", "+98", "Asia/Tehran", DateOrder.YearMonthDay, "٫"),
        ScriptCase("he-IL", "עברית (ישראל)", "עברית", "ישראל", "Hebr", WritingDirection.RightToLeft, "ILS", "+972", "Asia/Jerusalem", DateOrder.DayMonthYear, "."),
        ScriptCase("hi-IN", "हिन्दी (भारत)", "हिन्दी", "भारत", "Deva", WritingDirection.LeftToRight, "INR", "+91", "Asia/Kolkata", DateOrder.DayMonthYear, "."),
        ScriptCase("th-TH", "ไทย (ไทย)", "ไทย", "ไทย", "Thai", WritingDirection.LeftToRight, "THB", "+66", "Asia/Bangkok", DateOrder.DayMonthYear, "."),
        ScriptCase("tr-TR", "Türkçe (Türkiye)", "Türkçe", "Türkiye", "Latn", WritingDirection.LeftToRight, "TRY", "+90", "Europe/Istanbul", DateOrder.DayMonthYear, ","),
        ScriptCase("ru-RU", "русский (Россия)", "русский", "Россия", "Cyrl", WritingDirection.LeftToRight, "RUB", "+7", "Europe/Moscow", DateOrder.DayMonthYear, ","),
        ScriptCase("uk-UA", "українська (Україна)", "українська", "Україна", "Cyrl", WritingDirection.LeftToRight, "UAH", "+380", "Europe/Kyiv", DateOrder.DayMonthYear, ","),
        ScriptCase("el-GR", "Ελληνικά (Ελλάδα)", "Ελληνικά", "Ελλάδα", "Grek", WritingDirection.LeftToRight, "EUR", "+30", "Europe/Athens", DateOrder.DayMonthYear, ","),
        ScriptCase("ja-JP", "日本語 (日本)", "日本語", "日本", "Jpan", WritingDirection.LeftToRight, "JPY", "+81", "Asia/Tokyo", DateOrder.YearMonthDay, "."),
        ScriptCase("ko-KR", "한국어 (대한민국)", "한국어", "대한민국", "Kore", WritingDirection.LeftToRight, "KRW", "+82", "Asia/Seoul", DateOrder.YearMonthDay, "."),
        ScriptCase("zh-CN", "中文（中国大陆）", "中文", "中国大陆", "Hans", WritingDirection.LeftToRight, "CNY", "+86", "Asia/Shanghai", DateOrder.YearMonthDay, "."),
        ScriptCase("zh-TW", "中文（台灣）", "中文", "台灣", "Hant", WritingDirection.LeftToRight, "TWD", "+886", "Asia/Taipei", DateOrder.YearMonthDay, "."),
        ScriptCase("zh-HK", "中文（香港）", "中文", "香港", "Hant", WritingDirection.LeftToRight, "HKD", "+852", "Asia/Hong_Kong", DateOrder.DayMonthYear, "."),
        ScriptCase("id-ID", "Bahasa Indonesia (Indonesia)", "Bahasa Indonesia", "Indonesia", "Latn", WritingDirection.LeftToRight, "IDR", "+62", "Asia/Jakarta", DateOrder.DayMonthYear, ","),
        ScriptCase("vi-VN", "Tiếng Việt (Việt Nam)", "Tiếng Việt", "Việt Nam", "Latn", WritingDirection.LeftToRight, "VND", "+84", "Asia/Ho_Chi_Minh", DateOrder.DayMonthYear, ",")
    };

    [TestCaseSource(nameof(WaveOneCases))]
    public void EveryWaveOneProfileHasExactTypedMetadata(
        string locale,
        string nativeName,
        string languageName,
        string territoryName,
        string currency,
        string callingCode,
        string timeZone,
        DateOrder dateOrder,
        string decimalSeparator)
    {
        LocaleInfo info = Fake.Create(locale, 4510).Locale().Metadata(locale);

        AssertProfile(info, locale, nativeName, languageName, territoryName, "Latn", WritingDirection.LeftToRight, currency, callingCode, timeZone, dateOrder, decimalSeparator);
    }

    [TestCaseSource(nameof(WaveTwoCases))]
    public void EveryWaveTwoProfileHasExactTypedMetadata(
        string locale,
        string nativeName,
        string languageName,
        string territoryName,
        string script,
        WritingDirection direction,
        string currency,
        string callingCode,
        string timeZone,
        DateOrder dateOrder,
        string decimalSeparator)
    {
        LocaleInfo info = Fake.Create(locale, 4511).Locale().Metadata(locale);

        AssertProfile(info, locale, nativeName, languageName, territoryName, script, direction, currency, callingCode, timeZone, dateOrder, decimalSeparator);
    }

    [TestCase("iw-IL", "he-IL", "עברית", "IL")]
    [TestCase("in-ID", "id-ID", "Bahasa Indonesia", "ID")]
    public void MetadataCanonicalizesRegistryAliases(
        string alias,
        string canonical,
        string languageName,
        string territory)
    {
        LocaleInfo info = Fake.Create(canonical, 4512).Locale().Metadata(alias);

        Assert.Multiple(() =>
        {
            Assert.That(info.Tag, Is.EqualTo(canonical));
            Assert.That(info.LanguageName, Is.EqualTo(languageName));
            Assert.That(info.Territory, Is.EqualTo(territory));
            Assert.That(info.ProfileSourceLocale, Is.EqualTo(canonical));
            Assert.That(info.ProfileUsedFallback, Is.False);
        });
    }

    [Test]
    public void MetadataUsesConfiguredProfileThenInvariantAddressFallback()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", "en-US", 4513L);
        options.DataVersion = "test";
        LocaleProvider locale = Fake.Create(options).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "marker", Json("\"exact\""), "exact"),
            Value("en-US", "locale.profile", Profile("English (United States)", "English (United States)", "en", "Latn", "ltr", "US", "USD", "+1", "America/New_York", "mdy", "."), "configured"),
            Value("invariant", "address.catalog", Json("{}"), "invariant")
        };

        LocaleInfo info = locale.MetadataFromLocaleAndValues("fr-CA", values);

        Assert.Multiple(() =>
        {
            Assert.That(info.ProfileSourceLocale, Is.EqualTo("en-US"));
            Assert.That(info.ProfileUsedFallback, Is.True);
            Assert.That(info.ProfileProvenance, Is.EqualTo("configured"));
            Assert.That(info.AddressFormatSourceLocale, Is.EqualTo("invariant"));
            Assert.That(info.AddressFormatUsedFallback, Is.True);
            Assert.That(info.AddressFormatProvenance, Is.EqualTo("invariant"));
        });
    }

    [Test]
    public void StrictMetadataRejectsParentAndInvariantValuesWithoutConsumingThem()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", "en-US", 4514L);
        options.DataVersion = "test";
        options.StrictnessMode = StrictnessMode.Strict;
        LocaleProvider locale = Fake.Create(options).Locale();
        LocaleFieldValue[] values = new LocaleFieldValue[]
        {
            Value("fr-CA", "marker", Json("\"exact\""), "exact"),
            Value("fr", "locale.profile", Profile("French (France)", "français (France)", "fr", "Latn", "ltr", "FR", "EUR", "+33", "Europe/Paris", "dmy", ","), "parent"),
            Value("invariant", "address.catalog", Json("{}"), "invariant")
        };

        LocaleInfo info = locale.MetadataFromLocaleAndValues("fr-CA", values);

        Assert.Multiple(() =>
        {
            Assert.That(info.HasProfile, Is.False);
            Assert.That(info.ProfileSourceLocale, Is.Empty);
            Assert.That(info.AddressFormat, Is.Empty);
            Assert.That(info.AddressFormatSourceLocale, Is.Empty);
        });
    }

    [Test]
    public void MetadataAndCatalogArraysAreIsolatedAcrossCalls()
    {
        LocaleProvider locale = Fake.Create("ja-JP", 4515).Locale();
        LocaleInfo first = locale.Metadata("ja-JP");
        LocaleNameRecord[] firstScripts = locale.Scripts();
        LocaleNameRecord[] firstTerritories = locale.Territories();

        first.TimeZoneCodes[0] = "changed";
        first.TimeZoneNames[0] = "changed";
        first.NativeProviders[0] = "changed";
        first.FallbackProviders[0] = "changed";
        first.AvailableProviders[0] = "changed";
        first.Fields[0] = "changed";
        first.Provenance[0] = "changed";
        firstScripts[0].Name = "changed";
        firstTerritories[0].Name = "changed";

        LocaleInfo second = locale.Metadata("ja-JP");

        Assert.Multiple(() =>
        {
            Assert.That(second.TimeZoneCodes[0], Is.EqualTo("Asia/Tokyo"));
            Assert.That(second.TimeZoneNames[0], Is.EqualTo("Tokyo"));
            Assert.That(second.NativeProviders, Is.EqualTo(new string[] { "date", "locale" }));
            Assert.That(second.FallbackProviders[0], Is.EqualTo("address"));
            Assert.That(second.AvailableProviders[0], Is.EqualTo("address"));
            Assert.That(second.Fields, Does.Not.Contain("changed"));
            Assert.That(second.Provenance, Does.Not.Contain("changed"));
            Assert.That(locale.Scripts(), Has.None.Matches<LocaleNameRecord>(value => value.Name == "changed"));
            Assert.That(locale.Territories(), Has.None.Matches<LocaleNameRecord>(value => value.Name == "changed"));
        });
    }

    [TestCase("\"text\"")]
    [TestCase("{}")]
    [TestCase("{\"englishName\":7}")]
    [TestCase("{\"englishName\":\"English\",\"nativeName\":\"English\",\"language\":\"en\",\"script\":\"Latn\",\"direction\":\"sideways\",\"territory\":\"US\",\"currency\":\"USD\",\"callingCode\":\"+1\",\"timeZones\":[\"America/New_York\"],\"dateOrder\":\"mdy\",\"decimalSeparator\":\".\",\"nativeProviders\":[\"locale\"],\"fallbackProviders\":[]}")]
    public void MalformedRegisteredProfilesFailMetadataAndCatalogOperations(string malformedProfile)
    {
        Fake fake = Fake.Create("en", 4516);
        fake.Locale().RegisterFromLocaleAndFields("en", new Dictionary<string, object>
        {
            ["locale.profile"] = Json(malformedProfile)
        });

        Assert.Throws<FakeException>(() => fake.Locale().Metadata("en"));
        Assert.Throws<FakeException>(() => fake.Locale().Scripts());
        Assert.Throws<FakeException>(() => fake.Locale().Territories());
    }

    [Test]
    public void CatalogsExposeExactCodesNamesAndSourceMetadata()
    {
        ILocaleProvider locale = Fake.Create(4517).Locale();

        LocaleNameRecord[] scripts = locale.Scripts();
        LocaleNameRecord[] territories = locale.Territories();

        Assert.Multiple(() =>
        {
            Assert.That(scripts.Select(value => value.Code), Is.EqualTo(new string[]
            {
                "Arab", "Cyrl", "Deva", "Grek", "Hans", "Hant", "Hebr", "Jpan", "Kore", "Latn", "Thai"
            }));
            Assert.That(scripts.Select(value => value.Name), Is.EqualTo(new string[]
            {
                "Arabic", "Cyrillic", "Devanagari", "Greek", "Simplified Han", "Traditional Han", "Hebrew", "Japanese", "Korean", "Latin", "Thai"
            }));
            Assert.That(territories.Select(value => value.Code), Is.EqualTo(new string[]
            {
                "AT", "AU", "BR", "CA", "CH", "CN", "CZ", "DE", "DK", "EG", "ES", "FI", "FR", "GB", "GR", "HK", "ID", "IL", "IN", "IR", "IT", "JP", "KR", "MX", "NL", "NO", "PL", "PT", "RU", "SA", "SE", "TH", "TR", "TW", "UA", "US", "VN"
            }));
            Assert.That(territories.Single(value => value.Code == "JP").Name, Is.EqualTo("日本"));
            Assert.That(territories.Single(value => value.Code == "SA").Name, Is.EqualTo("المملكة العربية السعودية"));
            Assert.That(scripts, Has.All.Matches<LocaleNameRecord>(value => !value.UsedFallback && value.DataVersion == "1" && value.Provenance.Length > 0));
            Assert.That(territories, Has.All.Matches<LocaleNameRecord>(value => !value.UsedFallback && value.DataVersion == "1" && value.Provenance.Length > 0));
        });
    }

    static void AssertProfile(
        LocaleInfo info,
        string locale,
        string nativeName,
        string languageName,
        string territoryName,
        string script,
        WritingDirection direction,
        string currency,
        string callingCode,
        string timeZone,
        DateOrder dateOrder,
        string decimalSeparator)
    {
        Assert.Multiple(() =>
        {
            Assert.That(info.Tag, Is.EqualTo(locale));
            Assert.That(info.NativeName, Is.EqualTo(nativeName));
            Assert.That(info.EnglishName, Is.Not.Empty);
            Assert.That(info.LanguageName, Is.EqualTo(languageName));
            Assert.That(info.TerritoryName, Is.EqualTo(territoryName));
            Assert.That(info.DefaultScript, Is.EqualTo(script));
            Assert.That(info.ScriptName, Is.Not.Empty);
            Assert.That(info.Direction, Is.EqualTo(direction));
            Assert.That(info.DefaultCurrencyCode, Is.EqualTo(currency));
            Assert.That(info.DefaultCurrencyName, Is.Not.Empty);
            Assert.That(info.CallingCode, Is.EqualTo(callingCode));
            Assert.That(info.TimeZoneCodes, Is.EqualTo(new string[] { timeZone }));
            Assert.That(info.TimeZoneNames, Has.One.Not.Empty);
            Assert.That(info.DateOrder, Is.EqualTo(dateOrder));
            Assert.That(info.DecimalSeparator, Is.EqualTo(decimalSeparator));
            Assert.That(info.ProfileSourceLocale, Is.EqualTo(locale));
            Assert.That(info.ProfileUsedFallback, Is.False);
            Assert.That(info.ProfileDataVersion, Is.EqualTo("1"));
            Assert.That(info.ProfileProvenance, Is.Not.Empty);
            Assert.That(info.AddressFormatSourceLocale, Is.EqualTo("invariant"));
            Assert.That(info.AddressFormatUsedFallback, Is.True);
            Assert.That(info.AddressFormat, Is.Not.Empty);
            Assert.That(info.NativeProviders, Is.EqualTo(new string[] { "date", "locale" }));
            Assert.That(info.FallbackProviders, Has.Length.EqualTo(6));
            Assert.That(info.AvailableProviders, Has.Length.EqualTo(8));
        });
    }

    static object[] Case(
        string locale,
        string nativeName,
        string languageName,
        string territoryName,
        string currency,
        string callingCode,
        string timeZone,
        DateOrder dateOrder,
        string decimalSeparator)
    {
        return new object[]
        {
            locale, nativeName, languageName, territoryName, currency,
            callingCode, timeZone, dateOrder, decimalSeparator
        };
    }

    static object[] ScriptCase(
        string locale,
        string nativeName,
        string languageName,
        string territoryName,
        string script,
        WritingDirection direction,
        string currency,
        string callingCode,
        string timeZone,
        DateOrder dateOrder,
        string decimalSeparator)
    {
        return new object[]
        {
            locale, nativeName, languageName, territoryName, script, direction,
            currency, callingCode, timeZone, dateOrder, decimalSeparator
        };
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
