using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class LocaleAuthoritativeReviewTests
{
    static object[] LocaleCases = new object[]
    {
        Case("en-US", "+1", "USD", "America/New_York"),
        Case("en-GB", "+44", "GBP", "Europe/London"),
        Case("en-CA", "+1", "CAD", "America/Toronto"),
        Case("en-AU", "+61", "AUD", "Australia/Sydney"),
        Case("de-DE", "+49", "EUR", "Europe/Berlin"),
        Case("de-AT", "+43", "EUR", "Europe/Vienna"),
        Case("de-CH", "+41", "CHF", "Europe/Zurich"),
        Case("fr-FR", "+33", "EUR", "Europe/Paris"),
        Case("fr-CA", "+1", "CAD", "America/Toronto"),
        Case("es-ES", "+34", "EUR", "Europe/Madrid"),
        Case("es-MX", "+52", "MXN", "America/Mexico_City"),
        Case("pt-PT", "+351", "EUR", "Europe/Lisbon"),
        Case("pt-BR", "+55", "BRL", "America/Sao_Paulo"),
        Case("it-IT", "+39", "EUR", "Europe/Rome"),
        Case("nl-NL", "+31", "EUR", "Europe/Brussels"),
        Case("pl-PL", "+48", "PLN", "Europe/Warsaw"),
        Case("cs-CZ", "+420", "CZK", "Europe/Prague"),
        Case("sv-SE", "+46", "SEK", "Europe/Berlin"),
        Case("nb-NO", "+47", "NOK", "Europe/Berlin"),
        Case("da-DK", "+45", "DKK", "Europe/Berlin"),
        Case("fi-FI", "+358", "EUR", "Europe/Helsinki"),
        Case("ar-EG", "+20", "EGP", "Africa/Cairo"),
        Case("ar-SA", "+966", "SAR", "Asia/Riyadh"),
        Case("fa-IR", "+98", "IRR", "Asia/Tehran"),
        Case("he-IL", "+972", "ILS", "Asia/Jerusalem"),
        Case("hi-IN", "+91", "INR", "Asia/Kolkata"),
        Case("th-TH", "+66", "THB", "Asia/Bangkok"),
        Case("tr-TR", "+90", "TRY", "Europe/Istanbul"),
        Case("ru-RU", "+7", "RUB", "Europe/Moscow"),
        Case("uk-UA", "+380", "UAH", "Europe/Kyiv"),
        Case("el-GR", "+30", "EUR", "Europe/Athens"),
        Case("ja-JP", "+81", "JPY", "Asia/Tokyo"),
        Case("ko-KR", "+82", "KRW", "Asia/Seoul"),
        Case("zh-CN", "+86", "CNY", "Asia/Shanghai"),
        Case("zh-TW", "+886", "TWD", "Asia/Taipei"),
        Case("zh-HK", "+852", "HKD", "Asia/Hong_Kong"),
        Case("id-ID", "+62", "IDR", "Asia/Jakarta"),
        Case("vi-VN", "+84", "VND", "Asia/Ho_Chi_Minh")
    };

    [TestCaseSource(nameof(LocaleCases))]
    public void EveryLocaleRecordsReviewedRegistryMetadata(
        string locale,
        string callingCode,
        string currency,
        string timeZone)
    {
        Fake fake = Fake.Create(locale, 15301);
        JsonElement profile = (JsonElement)fake.Locale().Resolve("locale.profile");
        FallbackTrace profileTrace = fake.Locale().FallbackTrace("locale.profile");

        Assert.Multiple(() =>
        {
            Assert.That(profile.GetProperty("callingCode").GetString(), Is.EqualTo(callingCode));
            Assert.That(profile.GetProperty("currency").GetString(), Is.EqualTo(currency));
            Assert.That(profile.GetProperty("timeZones")[0].GetString(), Is.EqualTo(timeZone));
            Assert.That(fake.Finance().CurrencyFromCode(currency).Code, Is.EqualTo(currency));
            Assert.That(fake.Date().TimeZoneDetailsFromIanaId(timeZone).IanaId, Is.EqualTo(timeZone));
            Assert.That(profileTrace.SourceLocale, Is.EqualTo(locale));
            Assert.That(profileTrace.Provenance, Does.Contain("ITU-T E.164"));
            Assert.That(profileTrace.Provenance, Does.Contain("ISO 4217"));
            Assert.That(profileTrace.Provenance, Does.Contain("IANA Time Zone Database"));
        });
    }

    [TestCaseSource(nameof(LocaleCases))]
    public void EveryLocaleRecordsNativeAndFallbackReviewDecisions(
        string locale,
        string callingCode,
        string currency,
        string timeZone)
    {
        Fake fake = Fake.Create(locale, 15302);
        JsonElement review = (JsonElement)fake.Locale().Resolve("locale.review");
        JsonElement categories = review.GetProperty("categories");
        FallbackTrace reviewTrace = fake.Locale().FallbackTrace("locale.review");
        FallbackTrace dateTrace = fake.Locale().FallbackTrace("date.month.wide");

        Assert.Multiple(() =>
        {
            Assert.That(review.GetProperty("status").GetString(), Is.EqualTo("authoritative-reviewed"));
            Assert.That(review.GetProperty("reviewedOn").GetString(), Is.EqualTo("2026-08-15"));
            Assert.That(categories.EnumerateObject().Count(), Is.EqualTo(6));
            Assert.That(categories.GetProperty("nameOrder").GetProperty("status").GetString(), Is.EqualTo("fallback-only"));
            Assert.That(categories.GetProperty("addressFormat").GetProperty("status").GetString(), Is.EqualTo("fallback-only"));
            Assert.That(categories.GetProperty("phoneMasks").GetProperty("status").GetString(), Is.EqualTo("fallback-only"));
            Assert.That(categories.GetProperty("dates").GetProperty("status").GetString(), Is.EqualTo("native-reviewed"));
            Assert.That(categories.GetProperty("identifiers").GetProperty("status").GetString(), Is.EqualTo("fallback-only"));
            Assert.That(categories.GetProperty("sensitiveTerminology").GetProperty("status").GetString(), Is.EqualTo("reviewed-no-native-terms"));
            Assert.That(reviewTrace.SourceLocale, Is.EqualTo(locale));
            Assert.That(reviewTrace.Provenance, Does.Contain("sensitive terminology"));
            Assert.That(dateTrace.SourceLocale, Is.EqualTo(locale));
            Assert.That(dateTrace.Provenance, Does.Contain("Unicode CLDR 47"));
        });
    }

    static object[] Case(
        string locale,
        string callingCode,
        string currency,
        string timeZone)
    {
        return new object[] { locale, callingCode, currency, timeZone };
    }
}
