using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class Wave1LocalePackTests
{
    static object[] WaveOneCases = new object[]
    {
        Case("en-US", "January", "Sunday", DateOrder.MonthDayYear, "USD"),
        Case("en-GB", "January", "Sunday", DateOrder.DayMonthYear, "GBP"),
        Case("en-CA", "January", "Sunday", DateOrder.YearMonthDay, "CAD"),
        Case("en-AU", "January", "Sunday", DateOrder.DayMonthYear, "AUD"),
        Case("de-DE", "Januar", "Sonntag", DateOrder.DayMonthYear, "EUR"),
        Case("de-AT", "Jänner", "Sonntag", DateOrder.DayMonthYear, "EUR"),
        Case("de-CH", "Januar", "Sonntag", DateOrder.DayMonthYear, "CHF"),
        Case("fr-FR", "janvier", "dimanche", DateOrder.DayMonthYear, "EUR"),
        Case("fr-CA", "janvier", "dimanche", DateOrder.YearMonthDay, "CAD"),
        Case("es-ES", "enero", "domingo", DateOrder.DayMonthYear, "EUR"),
        Case("es-MX", "enero", "domingo", DateOrder.DayMonthYear, "MXN"),
        Case("pt-PT", "janeiro", "domingo", DateOrder.DayMonthYear, "EUR"),
        Case("pt-BR", "janeiro", "domingo", DateOrder.DayMonthYear, "BRL"),
        Case("it-IT", "gennaio", "domenica", DateOrder.DayMonthYear, "EUR"),
        Case("nl-NL", "januari", "zondag", DateOrder.DayMonthYear, "EUR"),
        Case("pl-PL", "styczeń", "niedziela", DateOrder.DayMonthYear, "PLN"),
        Case("cs-CZ", "leden", "neděle", DateOrder.DayMonthYear, "CZK"),
        Case("sv-SE", "januari", "söndag", DateOrder.YearMonthDay, "SEK"),
        Case("nb-NO", "januar", "søndag", DateOrder.DayMonthYear, "NOK"),
        Case("da-DK", "januar", "søndag", DateOrder.DayMonthYear, "DKK"),
        Case("fi-FI", "tammikuu", "sunnuntai", DateOrder.DayMonthYear, "EUR")
    };

    [Test]
    public void SupportedContainsEveryWaveOneRegionalLocale()
    {
        Assert.That(Fake.Create().Locale().Supported(), Is.SupersetOf(new string[]
        {
            "cs-CZ", "da-DK", "de-AT", "de-CH", "de-DE", "en-AU", "en-CA",
            "en-GB", "en-US", "es-ES", "es-MX", "fi-FI", "fr-CA", "fr-FR",
            "it-IT", "nb-NO", "nl-NL", "pl-PL", "pt-BR", "pt-PT", "sv-SE"
        }));
    }

    [TestCaseSource(nameof(WaveOneCases))]
    public void NativeProfilesAndCalendarsWorkWithoutFallback(
        string locale,
        string january,
        string sunday,
        DateOrder order,
        string currency)
    {
        FakeOptions options = FakeOptions.Create(locale, 15101);
        options.StrictnessMode = StrictnessMode.Strict;
        Fake fake = Fake.Create(options);
        JsonElement profile = (JsonElement)fake.Locale().Resolve("locale.profile");

        Assert.Multiple(() =>
        {
            Assert.That(fake.Locale().IsSupported(locale), Is.True);
            Assert.That(fake.Locale().Metadata(locale).Fields, Has.Length.EqualTo(7));
            Assert.That(profile.GetProperty("territory").GetString(), Is.EqualTo(locale.Substring(locale.Length - 2)));
            Assert.That(profile.GetProperty("currency").GetString(), Is.EqualTo(currency));
            Assert.That(profile.GetProperty("script").GetString(), Is.EqualTo("Latn"));
            Assert.That(profile.GetProperty("direction").GetString(), Is.EqualTo("ltr"));
            Assert.That(fake.Date().MonthNameFromMonth(1), Is.EqualTo(january));
            Assert.That(fake.Date().WeekdayNameFromWeekday(DayOfWeek.Sunday), Is.EqualTo(sunday));
            Assert.That(fake.Date().DateOrder(), Is.EqualTo(order));
            Assert.That(() => fake.Address().City(), Throws.TypeOf<FakeException>());
        });
    }

    [TestCaseSource(nameof(WaveOneCases))]
    public void CoverageReportsIntentionalInvariantProviderFallbacks(
        string locale,
        string january,
        string sunday,
        DateOrder order,
        string currency)
    {
        Fake fake = Fake.Create(locale, 15102);
        LocaleCoverage coverage = fake.Locale().Coverage(locale);
        FallbackTrace address = fake.Locale().FallbackTrace("address.catalog");

        Assert.Multiple(() =>
        {
            Assert.That(coverage.NativeProviders, Is.EqualTo(new string[] { "date", "locale" }));
            Assert.That(coverage.FallbackOnlyProviders, Is.EqualTo(new string[]
            {
                "address", "education", "internet", "person", "phone", "text"
            }));
            Assert.That(coverage.NativeFields, Has.Member("locale.profile"));
            Assert.That(coverage.NativeFields, Has.Member("date.order"));
            Assert.That(coverage.FallbackFields, Has.Member("address.catalog"));
            Assert.That(coverage.FallbackFields, Has.Member("person.names"));
            Assert.That(coverage.FallbackFields, Has.Member("phone.catalog"));
            Assert.That(address.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(address.UsedFallback, Is.True);
        });
    }

    static object[] Case(
        string locale,
        string january,
        string sunday,
        DateOrder order,
        string currency)
    {
        return new object[] { locale, january, sunday, order, currency };
    }
}
