namespace currencycodes.Tests;

public class CurrencyDataStoreTests
{
    [Test]
    public void CurrencyIndexesResolveAlphabeticNumericAndOfficialNameKeys()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo[] alphabetic = store.FindCurrencyAlphabeticSnapshots("EUR");
        CurrencyInfo[] numeric = store.FindCurrencyNumericSnapshots("032");
        CurrencyInfo[] names = store.FindCurrencyOfficialNameSnapshots("Euro");

        Assert.Multiple(() =>
        {
            Assert.That(alphabetic, Has.Length.EqualTo(2));
            Assert.That(alphabetic[0].AlphabeticCode, Is.EqualTo("EUR"));
            Assert.That(alphabetic[0].Status, Is.EqualTo(CurrencyStatus.Active));
            Assert.That(alphabetic[1].AlphabeticCode, Is.EqualTo("EUR"));
            Assert.That(alphabetic[1].Status, Is.EqualTo(CurrencyStatus.Historical));
            Assert.That(numeric.Length, Is.GreaterThan(1));
            Assert.That(numeric, Has.All.Matches<CurrencyInfo>(currency => currency.NumericCode == "032"));
            Assert.That(names, Has.Length.EqualTo(2));
            Assert.That(names, Has.All.Matches<CurrencyInfo>(currency => currency.OfficialName == "Euro"));
        });
    }

    [Test]
    public void TerritoryIndexesResolveEverySupportedIdentityKey()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryInfo[] stableKey = store.FindTerritoryKeySnapshots("BG");
        TerritoryInfo[] alpha2 = store.FindTerritoryAlpha2Snapshots("BG");
        TerritoryInfo[] alpha3 = store.FindTerritoryAlpha3Snapshots("BGR");
        TerritoryInfo[] numeric = store.FindTerritoryNumericSnapshots("100");
        TerritoryInfo[] names = store.FindTerritoryOfficialNameSnapshots("Bulgaria");

        Assert.Multiple(() =>
        {
            Assert.That(stableKey, Has.Length.EqualTo(1));
            Assert.That(alpha2, Has.Length.EqualTo(1));
            Assert.That(alpha3, Has.Length.EqualTo(1));
            Assert.That(numeric, Has.Length.EqualTo(1));
            Assert.That(names, Has.Length.EqualTo(1));
            Assert.That(stableKey[0].CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(alpha2[0].Alpha3Code, Is.EqualTo("BGR"));
            Assert.That(alpha3[0].NumericCode, Is.EqualTo("100"));
            Assert.That(numeric[0].Alpha2Code, Is.EqualTo("BG"));
            Assert.That(names[0].Kind, Is.EqualTo(TerritoryKind.Country));
        });
    }

    [Test]
    public void UsageIndexesSupportBothTerritoryAndCurrencyDirections()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyUsage[] byTerritory = store.FindUsageTerritorySnapshots("BG");
        CurrencyUsage[] byCurrency = store.FindUsageCurrencySnapshots("EUR");

        Assert.Multiple(() =>
        {
            Assert.That(byTerritory.Length, Is.GreaterThan(1));
            Assert.That(byTerritory, Has.All.Matches<CurrencyUsage>(usage => usage.TerritoryKey == "BG"));
            Assert.That(byTerritory[0].CurrencyCode, Is.EqualTo("BGJ"));
            Assert.That(byTerritory[byTerritory.Length - 1].CurrencyCode, Is.EqualTo("EUR"));
            Assert.That(byCurrency.Length, Is.GreaterThan(1));
            Assert.That(byCurrency, Has.All.Matches<CurrencyUsage>(usage => usage.CurrencyCode == "EUR"));
        });
    }

    [Test]
    public void FormatIndexResolvesTheGeneratedCurrencyRule()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyFormatMetadata[] formats = store.FindFormatCurrencySnapshots("CHF");

        Assert.Multiple(() =>
        {
            Assert.That(formats, Has.Length.EqualTo(1));
            Assert.That(formats[0].CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(formats[0].StandardFormatDigits, Is.EqualTo(2));
            Assert.That(formats[0].CashRoundingIncrement, Is.EqualTo(5));
        });
    }

    [Test]
    public void ExactIndexesUseOrdinalCaseSensitiveKeysAndReturnEmptyMisses()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(store.FindCurrencyAlphabeticSnapshots("usd"), Is.Empty);
            Assert.That(store.FindCurrencyAlphabeticSnapshots("AAA"), Is.Empty);
            Assert.That(store.FindCurrencyAlphabeticSnapshots("ZZZ"), Is.Empty);
            Assert.That(store.FindCurrencyNumericSnapshots("32"), Is.Empty);
            Assert.That(store.FindCurrencyOfficialNameSnapshots("euro"), Is.Empty);
            Assert.That(store.FindTerritoryAlpha2Snapshots("bg"), Is.Empty);
            Assert.That(store.FindTerritoryAlpha3Snapshots("bgr"), Is.Empty);
            Assert.That(store.FindTerritoryNumericSnapshots("1000"), Is.Empty);
            Assert.That(store.FindUsageTerritorySnapshots("missing"), Is.Empty);
            Assert.That(store.FindUsageCurrencySnapshots("missing"), Is.Empty);
            Assert.That(store.FindFormatCurrencySnapshots("missing"), Is.Empty);
        });
    }

    [Test]
    public void SeparateStoreInstancesProduceTheSameDeterministicOrder()
    {
        CurrencyDataStore firstStore = new CurrencyDataStore();
        CurrencyDataStore secondStore = new CurrencyDataStore();

        CurrencyInfo[] firstCurrencies = firstStore.FindCurrencyNumericSnapshots("032");
        CurrencyInfo[] secondCurrencies = secondStore.FindCurrencyNumericSnapshots("032");
        CurrencyUsage[] firstUsages = firstStore.FindUsageCurrencySnapshots("EUR");
        CurrencyUsage[] secondUsages = secondStore.FindUsageCurrencySnapshots("EUR");

        Assert.Multiple(() =>
        {
            Assert.That(
                Array.ConvertAll(firstCurrencies, currency => currency.AlphabeticCode),
                Is.EqualTo(Array.ConvertAll(secondCurrencies, currency => currency.AlphabeticCode)));
            Assert.That(
                Array.ConvertAll(firstUsages, usage => usage.TerritoryKey),
                Is.EqualTo(Array.ConvertAll(secondUsages, usage => usage.TerritoryKey)));
            Assert.That(firstCurrencies, Is.Not.SameAs(secondCurrencies));
            Assert.That(firstUsages, Is.Not.SameAs(secondUsages));
        });
    }
}
