namespace currencycodes.Tests;

public class CurrentCurrencyTerritoryMappingTests
{
    [Test]
    public void TerritoryLookupReturnsEveryCurrentCurrencyInCanonicalOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] result = store.GetCurrentCurrenciesForTerritorySnapshots(
            " \u2003bg\u00A0 ",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(
                Array.ConvertAll(result, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "BGN", "EUR" }));
            Assert.That(result[0].Status, Is.EqualTo(CurrencyStatus.Historical));
            Assert.That(result[1].Status, Is.EqualTo(CurrencyStatus.Active));
        });
    }

    [Test]
    public void TerritoryLookupAppliesTenderFilteringExplicitly()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] allCurrent = store.GetCurrentCurrenciesForTerritorySnapshots("CH", false);
        CurrencyInfo[] tender = store.GetCurrentCurrenciesForTerritorySnapshots("CH", true);

        Assert.Multiple(() =>
        {
            Assert.That(
                Array.ConvertAll(allCurrent, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "CHE", "CHF", "CHW" }));
            Assert.That(
                Array.ConvertAll(tender, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "CHF" }));
        });
    }

    [Test]
    public void TerritoryLookupSupportsExactStableKeysWithoutAlpha2Codes()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] result = store.GetCurrentCurrenciesForTerritorySnapshots(
            " ISO 4217 No Currency ",
            false);
        CurrencyInfo[] wrongCase = store.GetCurrentCurrenciesForTerritorySnapshots(
            "iso 4217 no currency",
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].AlphabeticCode, Is.EqualTo("XXX"));
            Assert.That(result[0].Kind, Is.EqualTo(CurrencyKind.NoCurrency));
            Assert.That(wrongCase, Is.Empty);
        });
    }

    [Test]
    public void TerritoryLookupReturnsFreshEmptyArraysForZeroResultQueries()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] noTender = store.GetCurrentCurrenciesForTerritorySnapshots("AQ", true);
        CurrencyInfo[] malformed = store.GetCurrentCurrenciesForTerritorySnapshots("B1", false);
        CurrencyInfo[] nullCode = store.GetCurrentCurrenciesForTerritorySnapshots(null, false);
        CurrencyInfo[] unassigned = store.GetCurrentCurrenciesForTerritorySnapshots("AA", false);

        Assert.Multiple(() =>
        {
            Assert.That(noTender, Is.Empty);
            Assert.That(malformed, Is.Empty);
            Assert.That(nullCode, Is.Empty);
            Assert.That(unassigned, Is.Empty);
            Assert.That(noTender, Is.Not.SameAs(malformed));
            Assert.That(malformed, Is.Not.SameAs(nullCode));
            Assert.That(nullCode, Is.Not.SameAs(unassigned));
        });
    }

    [Test]
    public void ReverseLookupReturnsMultipleTerritoriesInTerritoryCatalogOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryInfo[] result = store.GetCurrentTerritoriesForCurrencySnapshots(" eur ", true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(40));
            Assert.That(result[0].CanonicalName, Is.EqualTo("Andorra"));
            Assert.That(result[1].CanonicalName, Is.EqualTo("Austria"));
            Assert.That(result[result.Length - 1].CanonicalName, Is.EqualTo("\u00C5land Islands"));
        });
    }

    [Test]
    public void ReverseLookupAppliesTenderFilteringAndPreservesSpecialEntities()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryInfo[] allCurrent = store.GetCurrentTerritoriesForCurrencySnapshots("XXX", false);
        TerritoryInfo[] tender = store.GetCurrentTerritoriesForCurrencySnapshots("XXX", true);

        Assert.Multiple(() =>
        {
            Assert.That(
                Array.ConvertAll(allCurrent, territory => territory.CanonicalName),
                Is.EqualTo(new[]
                {
                    "Antarctica",
                    "Clipperton Island",
                    "ISO 4217 No Currency",
                    "Unknown Region"
                }));
            Assert.That(tender, Is.Empty);
        });
    }

    [Test]
    public void ReverseLookupReturnsFreshEmptyArraysForInvalidOrUnusedCodes()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryInfo[] nullCode = store.GetCurrentTerritoriesForCurrencySnapshots(null, false);
        TerritoryInfo[] malformed = store.GetCurrentTerritoriesForCurrencySnapshots("US1", false);
        TerritoryInfo[] unassigned = store.GetCurrentTerritoriesForCurrencySnapshots("AAA", false);
        TerritoryInfo[] historical = store.GetCurrentTerritoriesForCurrencySnapshots("ADP", false);

        Assert.Multiple(() =>
        {
            Assert.That(nullCode, Is.Empty);
            Assert.That(malformed, Is.Empty);
            Assert.That(unassigned, Is.Empty);
            Assert.That(historical, Is.Empty);
            Assert.That(nullCode, Is.Not.SameAs(malformed));
            Assert.That(malformed, Is.Not.SameAs(unassigned));
            Assert.That(unassigned, Is.Not.SameAs(historical));
        });
    }

    [Test]
    public void MappingResultsAreDetachedAcrossCallsAndDirections()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyInfo[] firstCurrencies = store.GetCurrentCurrenciesForTerritorySnapshots("CH", false);
        TerritoryInfo[] firstTerritories = store.GetCurrentTerritoriesForCurrencySnapshots("EUR", true);

        firstCurrencies[0].AlphabeticCode = "XXX";
        firstCurrencies[0].IntroductionDate.Year = 1;
        firstTerritories[0].CanonicalName = "changed";
        firstTerritories[0].Alpha2Code = "XX";

        CurrencyInfo[] secondCurrencies = store.GetCurrentCurrenciesForTerritorySnapshots("CH", false);
        TerritoryInfo[] secondTerritories = store.GetCurrentTerritoriesForCurrencySnapshots("EUR", true);

        Assert.Multiple(() =>
        {
            Assert.That(secondCurrencies, Is.Not.SameAs(firstCurrencies));
            Assert.That(secondCurrencies[0], Is.Not.SameAs(firstCurrencies[0]));
            Assert.That(secondCurrencies[0].IntroductionDate, Is.Not.SameAs(firstCurrencies[0].IntroductionDate));
            Assert.That(secondCurrencies[0].AlphabeticCode, Is.EqualTo("CHE"));
            Assert.That(secondTerritories, Is.Not.SameAs(firstTerritories));
            Assert.That(secondTerritories[0], Is.Not.SameAs(firstTerritories[0]));
            Assert.That(secondTerritories[0].CanonicalName, Is.EqualTo("Andorra"));
            Assert.That(secondTerritories[0].Alpha2Code, Is.EqualTo("AD"));
        });
    }
}
