namespace currencycodes.Tests;

public class TerritoryAndUsageCatalogEnumerationTests
{
    [Test]
    public void TerritoryCatalogContainsEveryGeneratedRowInCanonicalOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryInfo[] catalog = store.GetTerritoryCatalogSnapshots();

        Assert.That(catalog, Has.Length.EqualTo(GeneratedTerritoryRows.Rows.Length));
        for (int index = 0; index < catalog.Length; index++)
        {
            TerritoryRow expected = GeneratedTerritoryRows.Rows[index];
            TerritoryInfo actual = catalog[index];
            Assert.Multiple(() =>
            {
                Assert.That(actual.CanonicalName, Is.EqualTo(expected.CanonicalName), index.ToString());
                Assert.That(actual.HasAlpha2Code, Is.EqualTo(expected.HasAlpha2Code), index.ToString());
                Assert.That(actual.Alpha2Code, Is.EqualTo(expected.Alpha2Code), index.ToString());
                Assert.That(actual.HasAlpha3Code, Is.EqualTo(expected.HasAlpha3Code), index.ToString());
                Assert.That(actual.Alpha3Code, Is.EqualTo(expected.Alpha3Code), index.ToString());
                Assert.That(actual.HasNumericCode, Is.EqualTo(expected.HasNumericCode), index.ToString());
                Assert.That(actual.NumericCode, Is.EqualTo(expected.NumericCode), index.ToString());
                Assert.That(actual.NumericCodeValue, Is.EqualTo(expected.NumericCodeValue), index.ToString());
                Assert.That(actual.Kind, Is.EqualTo(expected.Kind), index.ToString());
            });
        }
    }

    [Test]
    public void UsageCatalogContainsEveryGeneratedRowInCanonicalOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyUsage[] catalog = store.GetCurrencyUsageCatalogSnapshots();

        Assert.That(catalog, Has.Length.EqualTo(GeneratedCurrencyUsageRows.Rows.Length));
        for (int index = 0; index < catalog.Length; index++)
        {
            CurrencyUsageRow expected = GeneratedCurrencyUsageRows.Rows[index];
            CurrencyUsage actual = catalog[index];
            Assert.Multiple(() =>
            {
                Assert.That(actual.TerritoryKey, Is.EqualTo(expected.TerritoryKey), index.ToString());
                Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode), index.ToString());
                AssertPartialDate(actual.SourceStartDate, expected.SourceStartDate, index);
                AssertPartialDate(actual.SourceEndDate, expected.SourceEndDate, index);
                Assert.That(actual.HasInclusiveStartDate, Is.EqualTo(expected.HasInclusiveStartDate), index.ToString());
                Assert.That(actual.InclusiveStartDate, Is.EqualTo(expected.InclusiveStartDate), index.ToString());
                Assert.That(actual.HasInclusiveEndDate, Is.EqualTo(expected.HasInclusiveEndDate), index.ToString());
                Assert.That(actual.InclusiveEndDate, Is.EqualTo(expected.InclusiveEndDate), index.ToString());
                Assert.That(actual.IsTender, Is.EqualTo(expected.IsTender), index.ToString());
                Assert.That(actual.IsCurrent, Is.EqualTo(expected.IsCurrent), index.ToString());
                Assert.That(actual.FromIsoListOne, Is.EqualTo(expected.FromIsoListOne), index.ToString());
                Assert.That(actual.FromIsoListThree, Is.EqualTo(expected.FromIsoListThree), index.ToString());
                Assert.That(actual.FromCldr, Is.EqualTo(expected.FromCldr), index.ToString());
            });
        }
    }

    [Test]
    public void CatalogCallsReturnDetachedArraysObjectsAndUsageDates()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        TerritoryInfo[] firstTerritories = store.GetTerritoryCatalogSnapshots();
        CurrencyUsage[] firstUsages = store.GetCurrencyUsageCatalogSnapshots();
        TerritoryInfo firstTerritory = firstTerritories[0];
        CurrencyUsage firstUsage = firstUsages[0];
        string expectedTerritoryName = firstTerritory.CanonicalName;
        string expectedUsageTerritory = firstUsage.TerritoryKey;
        string expectedUsageCurrency = firstUsage.CurrencyCode;
        int expectedStartYear = firstUsage.SourceStartDate.Year;
        int expectedEndYear = firstUsage.SourceEndDate.Year;

        firstTerritory.CanonicalName = "changed";
        firstTerritory.Alpha2Code = "XX";
        firstUsage.TerritoryKey = "changed";
        firstUsage.CurrencyCode = "XXX";
        firstUsage.SourceStartDate.Year = 1;
        firstUsage.SourceEndDate.Year = 1;

        TerritoryInfo[] secondTerritories = store.GetTerritoryCatalogSnapshots();
        CurrencyUsage[] secondUsages = store.GetCurrencyUsageCatalogSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(secondTerritories, Is.Not.SameAs(firstTerritories));
            Assert.That(secondTerritories[0], Is.Not.SameAs(firstTerritory));
            Assert.That(secondTerritories[0].CanonicalName, Is.EqualTo(expectedTerritoryName));
            Assert.That(secondUsages, Is.Not.SameAs(firstUsages));
            Assert.That(secondUsages[0], Is.Not.SameAs(firstUsage));
            Assert.That(secondUsages[0].SourceStartDate, Is.Not.SameAs(firstUsage.SourceStartDate));
            Assert.That(secondUsages[0].SourceEndDate, Is.Not.SameAs(firstUsage.SourceEndDate));
            Assert.That(secondUsages[0].TerritoryKey, Is.EqualTo(expectedUsageTerritory));
            Assert.That(secondUsages[0].CurrencyCode, Is.EqualTo(expectedUsageCurrency));
            Assert.That(secondUsages[0].SourceStartDate.Year, Is.EqualTo(expectedStartYear));
            Assert.That(secondUsages[0].SourceEndDate.Year, Is.EqualTo(expectedEndYear));
        });
    }

    private static void AssertPartialDate(PartialDate actual, PartialDate expected, int index)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.IsKnown, Is.EqualTo(expected.IsKnown), index.ToString());
            Assert.That(actual.Year, Is.EqualTo(expected.Year), index.ToString());
            Assert.That(actual.Month, Is.EqualTo(expected.Month), index.ToString());
            Assert.That(actual.Day, Is.EqualTo(expected.Day), index.ToString());
            Assert.That(actual.Precision, Is.EqualTo(expected.Precision), index.ToString());
        });
    }
}
