namespace currencycodes.Tests;

public class DateAwareCurrencyUsageTests
{
    [Test]
    public void DateLookupUsesInclusiveDayBoundariesAndReturnsCanonicalCodes()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] beforeBoundary = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(1952, 5, 11),
            true);
        CurrencyInfo[] onBoundary = store.GetCurrenciesForTerritoryOnDateSnapshots(
            " bg ",
            new DateOnly(1952, 5, 12),
            true);
        CurrencyInfo[] afterBoundary = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(1952, 5, 13),
            true);

        Assert.Multiple(() =>
        {
            AssertCodes(beforeBoundary, "BGO");
            AssertCodes(onBoundary, "BGM", "BGO");
            AssertCodes(afterBoundary, "BGM");
        });
    }

    [Test]
    public void DateLookupReturnsEveryCurrencyDuringOverlappingTransitionIntervals()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] beforeEuro = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(2025, 12, 31),
            true);
        CurrencyInfo[] overlapStart = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(2026, 1, 1),
            true);
        CurrencyInfo[] overlapEnd = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(2026, 1, 31),
            true);
        CurrencyInfo[] afterLev = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(2026, 2, 1),
            true);

        Assert.Multiple(() =>
        {
            AssertCodes(beforeEuro, "BGN");
            AssertCodes(overlapStart, "BGN", "EUR");
            AssertCodes(overlapEnd, "BGN", "EUR");
            AssertCodes(afterLev, "EUR");
        });
    }

    [Test]
    public void DateLookupUsesMonthEndAndTreatsUnknownStartsAsOpen()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] monthEnd = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "ZW",
            new DateOnly(2006, 9, 30),
            false);
        CurrencyInfo[] nextDay = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "ZW",
            new DateOnly(2006, 10, 1),
            false);
        CurrencyInfo[] tender = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "ZW",
            new DateOnly(2006, 9, 30),
            true);

        Assert.Multiple(() =>
        {
            AssertCodes(monthEnd, "ZWD", "ZWN");
            AssertCodes(nextDay, "ZWD");
            AssertCodes(tender, "ZWD");
        });
    }

    [Test]
    public void DateLookupTreatsFullyUnknownIntervalsAsOpenAndStillFiltersTender()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] all = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "AQ",
            new DateOnly(1, 1, 1),
            false);
        CurrencyInfo[] tender = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "AQ",
            new DateOnly(9999, 12, 31),
            true);

        Assert.Multiple(() =>
        {
            AssertCodes(all, "XXX");
            Assert.That(tender, Is.Empty);
        });
    }

    [Test]
    public void TerritoryHistoryReturnsEveryAssociationWithOriginalDatePrecision()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyUsage[] history = store.GetCurrencyUsageHistoryForTerritorySnapshots(" zw ");
        CurrencyUsage zwn = Array.Find(history, usage => usage.CurrencyCode == "ZWN")!;

        Assert.Multiple(() =>
        {
            Assert.That(
                Array.ConvertAll(history, usage => usage.CurrencyCode),
                Is.EqualTo(new[] { "RHD", "USD", "ZWC", "ZWD", "ZWG", "ZWL", "ZWN", "ZWR" }));
            Assert.That(zwn.SourceStartDate.IsKnown, Is.False);
            Assert.That(zwn.HasInclusiveStartDate, Is.False);
            Assert.That(zwn.SourceEndDate.IsKnown, Is.True);
            Assert.That(zwn.SourceEndDate.Year, Is.EqualTo(2006));
            Assert.That(zwn.SourceEndDate.Month, Is.EqualTo(9));
            Assert.That(zwn.SourceEndDate.Day, Is.Zero);
            Assert.That(zwn.SourceEndDate.Precision, Is.EqualTo(PartialDatePrecision.Month));
            Assert.That(zwn.HasInclusiveEndDate, Is.True);
            Assert.That(zwn.InclusiveEndDate, Is.EqualTo(new DateOnly(2006, 9, 30)));
        });
    }

    [Test]
    public void CurrencyHistoryReturnsEveryTerritoryAssociationInCanonicalUsageOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyUsage[] history = store.GetCurrencyUsageHistoryForCurrencySnapshots(" rur ");

        Assert.That(
            Array.ConvertAll(history, usage => usage.TerritoryKey),
            Is.EqualTo(new[]
            {
                "AM", "AZ", "BY", "GE", "KG", "KZ",
                "MD", "RU", "TJ", "TM", "UA", "UZ"
            }));
    }

    [Test]
    public void DateAndHistoryQueriesReturnFreshEmptyArraysForInvalidOrUnknownKeys()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] malformedDate = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "B1",
            new DateOnly(2026, 1, 1),
            false);
        CurrencyInfo[] unknownDate = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "AA",
            new DateOnly(2026, 1, 1),
            false);
        CurrencyUsage[] nullTerritory = store.GetCurrencyUsageHistoryForTerritorySnapshots(null);
        CurrencyUsage[] unknownTerritory = store.GetCurrencyUsageHistoryForTerritorySnapshots("AA");
        CurrencyUsage[] malformedCurrency = store.GetCurrencyUsageHistoryForCurrencySnapshots("US1");
        CurrencyUsage[] unknownCurrency = store.GetCurrencyUsageHistoryForCurrencySnapshots("AAA");

        Assert.Multiple(() =>
        {
            Assert.That(malformedDate, Is.Empty);
            Assert.That(unknownDate, Is.Empty);
            Assert.That(nullTerritory, Is.Empty);
            Assert.That(unknownTerritory, Is.Empty);
            Assert.That(malformedCurrency, Is.Empty);
            Assert.That(unknownCurrency, Is.Empty);
            Assert.That(malformedDate, Is.Not.SameAs(unknownDate));
            Assert.That(nullTerritory, Is.Not.SameAs(unknownTerritory));
            Assert.That(malformedCurrency, Is.Not.SameAs(unknownCurrency));
        });
    }

    [Test]
    public void DateAndHistoryResultsAreDetachedAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyInfo[] firstDate = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(2026, 1, 1),
            true);
        CurrencyUsage[] firstTerritoryHistory = store.GetCurrencyUsageHistoryForTerritorySnapshots("BG");
        CurrencyUsage[] firstCurrencyHistory = store.GetCurrencyUsageHistoryForCurrencySnapshots("EUR");

        firstDate[0].AlphabeticCode = "XXX";
        firstDate[0].WithdrawalDate.Year = 1;
        firstTerritoryHistory[0].CurrencyCode = "XXX";
        firstTerritoryHistory[0].SourceStartDate.Year = 1;
        firstCurrencyHistory[0].TerritoryKey = "changed";
        firstCurrencyHistory[0].SourceEndDate.Year = 1;

        CurrencyInfo[] secondDate = store.GetCurrenciesForTerritoryOnDateSnapshots(
            "BG",
            new DateOnly(2026, 1, 1),
            true);
        CurrencyUsage[] secondTerritoryHistory = store.GetCurrencyUsageHistoryForTerritorySnapshots("BG");
        CurrencyUsage[] secondCurrencyHistory = store.GetCurrencyUsageHistoryForCurrencySnapshots("EUR");

        Assert.Multiple(() =>
        {
            Assert.That(secondDate, Is.Not.SameAs(firstDate));
            Assert.That(secondDate[0], Is.Not.SameAs(firstDate[0]));
            Assert.That(secondDate[0].WithdrawalDate, Is.Not.SameAs(firstDate[0].WithdrawalDate));
            Assert.That(secondDate[0].AlphabeticCode, Is.EqualTo("BGN"));
            Assert.That(secondTerritoryHistory, Is.Not.SameAs(firstTerritoryHistory));
            Assert.That(secondTerritoryHistory[0], Is.Not.SameAs(firstTerritoryHistory[0]));
            Assert.That(
                secondTerritoryHistory[0].SourceStartDate,
                Is.Not.SameAs(firstTerritoryHistory[0].SourceStartDate));
            Assert.That(secondTerritoryHistory[0].CurrencyCode, Is.EqualTo("BGJ"));
            Assert.That(secondCurrencyHistory, Is.Not.SameAs(firstCurrencyHistory));
            Assert.That(secondCurrencyHistory[0], Is.Not.SameAs(firstCurrencyHistory[0]));
            Assert.That(
                secondCurrencyHistory[0].SourceEndDate,
                Is.Not.SameAs(firstCurrencyHistory[0].SourceEndDate));
            Assert.That(secondCurrencyHistory[0].TerritoryKey, Is.EqualTo("AD"));
        });
    }

    private static void AssertCodes(CurrencyInfo[] currencies, params string[] expected)
    {
        Assert.That(
            Array.ConvertAll(currencies, currency => currency.AlphabeticCode),
            Is.EqualTo(expected));
    }
}
