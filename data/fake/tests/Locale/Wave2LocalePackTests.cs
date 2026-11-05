using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class Wave2LocalePackTests
{
    static object[] WaveTwoCases = new object[]
    {
        Case("ar-EG", "يناير", "الأحد", DateOrder.DayMonthYear, "EGP", "Arab", "rtl"),
        Case("ar-SA", "يناير", "الأحد", DateOrder.DayMonthYear, "SAR", "Arab", "rtl"),
        Case("fa-IR", "ژانویه", "یکشنبه", DateOrder.YearMonthDay, "IRR", "Arab", "rtl"),
        Case("he-IL", "ינואר", "יום ראשון", DateOrder.DayMonthYear, "ILS", "Hebr", "rtl"),
        Case("hi-IN", "जनवरी", "रविवार", DateOrder.DayMonthYear, "INR", "Deva", "ltr"),
        Case("th-TH", "มกราคม", "วันอาทิตย์", DateOrder.DayMonthYear, "THB", "Thai", "ltr"),
        Case("tr-TR", "Ocak", "Pazar", DateOrder.DayMonthYear, "TRY", "Latn", "ltr"),
        Case("ru-RU", "январь", "воскресенье", DateOrder.DayMonthYear, "RUB", "Cyrl", "ltr"),
        Case("uk-UA", "січень", "неділя", DateOrder.DayMonthYear, "UAH", "Cyrl", "ltr"),
        Case("el-GR", "Ιανουάριος", "Κυριακή", DateOrder.DayMonthYear, "EUR", "Grek", "ltr"),
        Case("ja-JP", "1月", "日曜日", DateOrder.YearMonthDay, "JPY", "Jpan", "ltr"),
        Case("ko-KR", "1월", "일요일", DateOrder.YearMonthDay, "KRW", "Kore", "ltr"),
        Case("zh-CN", "一月", "星期日", DateOrder.YearMonthDay, "CNY", "Hans", "ltr"),
        Case("zh-TW", "1月", "星期日", DateOrder.YearMonthDay, "TWD", "Hant", "ltr"),
        Case("zh-HK", "1月", "星期日", DateOrder.DayMonthYear, "HKD", "Hant", "ltr"),
        Case("id-ID", "Januari", "Minggu", DateOrder.DayMonthYear, "IDR", "Latn", "ltr"),
        Case("vi-VN", "Tháng 1", "Chủ Nhật", DateOrder.DayMonthYear, "VND", "Latn", "ltr")
    };

    [Test]
    public void SupportedContainsEveryWaveTwoScriptAndOrderingLocale()
    {
        Assert.That(Fake.Create().Locale().Supported(), Is.SupersetOf(new string[]
        {
            "ar-EG", "ar-SA", "fa-IR", "he-IL", "hi-IN", "th-TH", "tr-TR",
            "ru-RU", "uk-UA", "el-GR", "ja-JP", "ko-KR", "zh-CN", "zh-TW",
            "zh-HK", "id-ID", "vi-VN"
        }));
    }

    [TestCaseSource(nameof(WaveTwoCases))]
    public void NativeScriptProfilesAndCalendarsWorkWithoutFallback(
        string locale,
        string january,
        string sunday,
        DateOrder order,
        string currency,
        string script,
        string direction)
    {
        FakeOptions options = FakeOptions.Create(locale, 15201);
        options.StrictnessMode = StrictnessMode.Strict;
        Fake fake = Fake.Create(options);
        JsonElement profile = (JsonElement)fake.Locale().Resolve("locale.profile");

        Assert.Multiple(() =>
        {
            Assert.That(fake.Locale().IsSupported(locale), Is.True);
            Assert.That(fake.Locale().Metadata(locale).Fields, Has.Length.EqualTo(7));
            Assert.That(profile.GetProperty("territory").GetString(), Is.EqualTo(locale.Substring(locale.Length - 2)));
            Assert.That(profile.GetProperty("currency").GetString(), Is.EqualTo(currency));
            Assert.That(profile.GetProperty("script").GetString(), Is.EqualTo(script));
            Assert.That(profile.GetProperty("direction").GetString(), Is.EqualTo(direction));
            Assert.That(profile.GetProperty("calendar").GetString(), Is.EqualTo("gregory"));
            Assert.That(fake.Date().MonthNameFromMonth(1), Is.EqualTo(january));
            Assert.That(fake.Date().WeekdayNameFromWeekday(DayOfWeek.Sunday), Is.EqualTo(sunday));
            Assert.That(fake.Date().DateOrder(), Is.EqualTo(order));
            Assert.That(() => fake.Person().FirstName(), Throws.TypeOf<FakeException>());
        });
    }

    [TestCaseSource(nameof(WaveTwoCases))]
    public void CoverageReportsNativeDateAndIntentionalProviderFallbacks(
        string locale,
        string january,
        string sunday,
        DateOrder order,
        string currency,
        string script,
        string direction)
    {
        Fake fake = Fake.Create(locale, 15202);
        LocaleCoverage coverage = fake.Locale().Coverage(locale);
        FallbackTrace person = fake.Locale().FallbackTrace("person.names");

        Assert.Multiple(() =>
        {
            Assert.That(coverage.NativeProviders, Is.EqualTo(new string[] { "date", "locale" }));
            Assert.That(coverage.FallbackOnlyProviders, Is.EqualTo(new string[]
            {
                "address", "education", "internet", "person", "phone", "text"
            }));
            Assert.That(coverage.NativeFields, Has.Member("locale.profile"));
            Assert.That(coverage.NativeFields, Has.Member("date.month.wide"));
            Assert.That(coverage.FallbackFields, Has.Member("address.catalog"));
            Assert.That(coverage.FallbackFields, Has.Member("person.names"));
            Assert.That(coverage.FallbackFields, Has.Member("phone.catalog"));
            Assert.That(person.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(person.UsedFallback, Is.True);
        });
    }

    static object[] Case(
        string locale,
        string january,
        string sunday,
        DateOrder order,
        string currency,
        string script,
        string direction)
    {
        return new object[]
        {
            locale,
            january,
            sunday,
            order,
            currency,
            script,
            direction
        };
    }
}
