using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteTerritoryUsageTests
{
    [Test]
    public void EveryTerritoryHistoryMatchesEveryPinnedUsageField()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedTerritories = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedCurrencyUsageRows.Rows.Length; index++)
        {
            string territoryKey = GeneratedCurrencyUsageRows.Rows[index].TerritoryKey;
            if (!testedTerritories.Add(territoryKey))
            {
                continue;
            }

            CurrencyUsageRow[] expected = Array.FindAll(
                GeneratedCurrencyUsageRows.Rows,
                row => row.TerritoryKey == territoryKey);
            CurrencyUsage[] actual = catalog.GetCurrencyUsageHistoryForTerritory(
                CreateTerritoryQuery(territoryKey));

            AssertUsageRows(actual, expected, territoryKey);
        }

        Assert.That(testedTerritories, Has.Count.EqualTo(287));
    }

    [Test]
    public void EveryCurrencyHistoryMatchesEveryPinnedUsageField()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedCurrencies = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedCurrencyUsageRows.Rows.Length; index++)
        {
            string currencyCode = GeneratedCurrencyUsageRows.Rows[index].CurrencyCode;
            if (!testedCurrencies.Add(currencyCode))
            {
                continue;
            }

            CurrencyUsageRow[] expected = Array.FindAll(
                GeneratedCurrencyUsageRows.Rows,
                row => row.CurrencyCode == currencyCode);
            CurrencyUsage[] actual = catalog.GetCurrencyUsageHistoryForCurrency(
                " \t" + currencyCode.ToLowerInvariant() + "\u00A0 ");

            AssertUsageRows(actual, expected, currencyCode);
        }

        Assert.That(testedCurrencies, Has.Count.EqualTo(326));
    }

    [Test]
    public void EveryCurrentForwardAndReverseMappingMatchesPinnedUsageRows()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedTerritories = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> testedCurrencies = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedCurrencyUsageRows.Rows.Length; index++)
        {
            CurrencyUsageRow usage = GeneratedCurrencyUsageRows.Rows[index];
            if (testedTerritories.Add(usage.TerritoryKey))
            {
                AssertCurrentCurrencies(catalog, usage.TerritoryKey, false);
                AssertCurrentCurrencies(catalog, usage.TerritoryKey, true);
            }

            if (testedCurrencies.Add(usage.CurrencyCode))
            {
                AssertCurrentTerritories(catalog, usage.CurrencyCode, false);
                AssertCurrentTerritories(catalog, usage.CurrencyCode, true);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(testedTerritories, Has.Count.EqualTo(287));
            Assert.That(testedCurrencies, Has.Count.EqualTo(326));
        });
    }

    [Test]
    public void EveryKnownIntervalBoundaryAndAdjacentDateMatchesPinnedRows()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> tested = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedCurrencyUsageRows.Rows.Length; index++)
        {
            CurrencyUsageRow usage = GeneratedCurrencyUsageRows.Rows[index];
            if (usage.HasInclusiveStartDate)
            {
                AssertDateMapping(catalog, usage.TerritoryKey, usage.InclusiveStartDate, tested);
                if (usage.InclusiveStartDate > DateOnly.MinValue)
                {
                    AssertDateMapping(
                        catalog,
                        usage.TerritoryKey,
                        usage.InclusiveStartDate.AddDays(-1),
                        tested);
                }
            }

            if (usage.HasInclusiveEndDate)
            {
                AssertDateMapping(catalog, usage.TerritoryKey, usage.InclusiveEndDate, tested);
                if (usage.InclusiveEndDate < DateOnly.MaxValue)
                {
                    AssertDateMapping(
                        catalog,
                        usage.TerritoryKey,
                        usage.InclusiveEndDate.AddDays(1),
                        tested);
                }
            }
        }

        Assert.That(tested, Has.Count.GreaterThan(500));
    }

    [Test]
    public void RepresentativeSpecialTransitionAndTenderMappingsRemainExplicit()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        CurrencyInfo[] swissAll = catalog.GetCurrentCurrenciesForTerritory("CH", false);
        CurrencyInfo[] swissTender = catalog.GetCurrentCurrenciesForTerritory("CH", true);
        CurrencyInfo[] noCurrency = catalog.GetCurrentCurrenciesForTerritory(
            "ISO 4217 No Currency",
            false);
        CurrencyInfo[] noCurrencyTender = catalog.GetCurrentCurrenciesForTerritory(
            "ISO 4217 No Currency",
            true);
        CurrencyInfo[] europeanUnion = catalog.GetCurrentCurrenciesForTerritory("EU", true);
        CurrencyInfo[] monetaryFund = catalog.GetCurrentCurrenciesForTerritory(
            "International Monetary Fund (IMF)",
            false);
        TerritoryInfo[] noCurrencyTerritories = catalog.GetCurrentTerritoriesForCurrency(
            "XXX",
            false);
        CurrencyInfo[] beforeEuro = catalog.GetCurrenciesForTerritoryOnDate(
            "BG",
            new DateOnly(2025, 12, 31),
            true);
        CurrencyInfo[] overlap = catalog.GetCurrenciesForTerritoryOnDate(
            "BG",
            new DateOnly(2026, 1, 1),
            true);
        CurrencyInfo[] afterLev = catalog.GetCurrenciesForTerritoryOnDate(
            "BG",
            new DateOnly(2026, 2, 1),
            true);

        Assert.Multiple(() =>
        {
            AssertCurrencyCodes(swissAll, "CHE", "CHF", "CHW");
            AssertCurrencyCodes(swissTender, "CHF");
            AssertCurrencyCodes(noCurrency, "XXX");
            Assert.That(noCurrencyTender, Is.Empty);
            AssertCurrencyCodes(europeanUnion, "EUR");
            AssertCurrencyCodes(monetaryFund, "XDR");
            Assert.That(
                Array.ConvertAll(noCurrencyTerritories, territory => territory.CanonicalName),
                Is.EqualTo(new[]
                {
                    "Antarctica",
                    "Clipperton Island",
                    "ISO 4217 No Currency",
                    "Unknown Region"
                }));
            AssertCurrencyCodes(beforeEuro, "BGN");
            AssertCurrencyCodes(overlap, "BGN", "EUR");
            AssertCurrencyCodes(afterLev, "EUR");
        });
    }

    private static void AssertCurrentCurrencies(
        ICurrencyCatalog catalog,
        string territoryKey,
        bool tenderOnly)
    {
        CurrencyRow[] expected = FindExpectedCurrentCurrencies(territoryKey, tenderOnly);
        CurrencyInfo[] actual = catalog.GetCurrentCurrenciesForTerritory(
            CreateTerritoryQuery(territoryKey),
            tenderOnly);

        Assert.That(actual, Has.Length.EqualTo(expected.Length), territoryKey);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(
                actual[index].AlphabeticCode,
                Is.EqualTo(expected[index].AlphabeticCode),
                territoryKey);
            Assert.That(actual[index].Status, Is.EqualTo(expected[index].Status), territoryKey);
        }
    }

    private static void AssertCurrentTerritories(
        ICurrencyCatalog catalog,
        string currencyCode,
        bool tenderOnly)
    {
        TerritoryRow[] expected = FindExpectedCurrentTerritories(currencyCode, tenderOnly);
        TerritoryInfo[] actual = catalog.GetCurrentTerritoriesForCurrency(
            " \t" + currencyCode.ToLowerInvariant() + "\u00A0 ",
            tenderOnly);

        Assert.That(actual, Has.Length.EqualTo(expected.Length), currencyCode);
        for (int index = 0; index < expected.Length; index++)
        {
            AssertTerritoryMatchesRow(actual[index], expected[index], currencyCode);
        }
    }

    private static void AssertDateMapping(
        ICurrencyCatalog catalog,
        string territoryKey,
        DateOnly date,
        HashSet<string> tested)
    {
        string identity = territoryKey + "\0" + date.DayNumber.ToString();
        if (!tested.Add(identity))
        {
            return;
        }

        CurrencyInfo[] all = catalog.GetCurrenciesForTerritoryOnDate(
            CreateTerritoryQuery(territoryKey),
            date,
            false);
        CurrencyInfo[] tender = catalog.GetCurrenciesForTerritoryOnDate(
            CreateTerritoryQuery(territoryKey),
            date,
            true);
        CurrencyRow[] expectedAll = FindExpectedCurrenciesOnDate(territoryKey, date, false);
        CurrencyRow[] expectedTender = FindExpectedCurrenciesOnDate(territoryKey, date, true);

        AssertCurrencyRows(all, expectedAll, identity);
        AssertCurrencyRows(tender, expectedTender, identity);
    }

    private static CurrencyRow[] FindExpectedCurrentCurrencies(
        string territoryKey,
        bool tenderOnly)
    {
        CurrencyRow[] candidates = Array.FindAll(
            GeneratedCurrencyRows.Rows,
            IsFirstCurrencyDesignation);
        return Array.FindAll(
            candidates,
            currency => Array.Exists(
                GeneratedCurrencyUsageRows.Rows,
                usage => usage.TerritoryKey == territoryKey
                    && usage.CurrencyCode == currency.AlphabeticCode
                    && usage.IsCurrent
                    && (!tenderOnly || usage.IsTender)));
    }

    private static TerritoryRow[] FindExpectedCurrentTerritories(
        string currencyCode,
        bool tenderOnly)
    {
        return Array.FindAll(
            GeneratedTerritoryRows.Rows,
            territory => Array.Exists(
                GeneratedCurrencyUsageRows.Rows,
                usage => usage.CurrencyCode == currencyCode
                    && usage.TerritoryKey == territory.TerritoryKey
                    && usage.IsCurrent
                    && (!tenderOnly || usage.IsTender)));
    }

    private static CurrencyRow[] FindExpectedCurrenciesOnDate(
        string territoryKey,
        DateOnly date,
        bool tenderOnly)
    {
        CurrencyRow[] candidates = Array.FindAll(
            GeneratedCurrencyRows.Rows,
            IsFirstCurrencyDesignation);
        return Array.FindAll(
            candidates,
            currency => Array.Exists(
                GeneratedCurrencyUsageRows.Rows,
                usage => usage.TerritoryKey == territoryKey
                    && usage.CurrencyCode == currency.AlphabeticCode
                    && (!tenderOnly || usage.IsTender)
                    && (!usage.HasInclusiveStartDate || usage.InclusiveStartDate <= date)
                    && (!usage.HasInclusiveEndDate || usage.InclusiveEndDate >= date)));
    }

    private static bool IsFirstCurrencyDesignation(CurrencyRow row)
    {
        int index = Array.IndexOf(GeneratedCurrencyRows.Rows, row);
        return index == 0
            || GeneratedCurrencyRows.Rows[index - 1].AlphabeticCode != row.AlphabeticCode;
    }

    private static void AssertUsageRows(
        CurrencyUsage[] actual,
        CurrencyUsageRow[] expected,
        string message)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length), message);
        for (int index = 0; index < expected.Length; index++)
        {
            AssertUsageMatchesRow(actual[index], expected[index], message);
        }
    }

    private static void AssertUsageMatchesRow(
        CurrencyUsage actual,
        CurrencyUsageRow expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.TerritoryKey, Is.EqualTo(expected.TerritoryKey), message);
            Assert.That(actual.CurrencyCode, Is.EqualTo(expected.CurrencyCode), message);
            AssertPartialDate(actual.SourceStartDate, expected.SourceStartDate, message);
            AssertPartialDate(actual.SourceEndDate, expected.SourceEndDate, message);
            Assert.That(actual.HasInclusiveStartDate, Is.EqualTo(expected.HasInclusiveStartDate), message);
            Assert.That(actual.InclusiveStartDate, Is.EqualTo(expected.InclusiveStartDate), message);
            Assert.That(actual.HasInclusiveEndDate, Is.EqualTo(expected.HasInclusiveEndDate), message);
            Assert.That(actual.InclusiveEndDate, Is.EqualTo(expected.InclusiveEndDate), message);
            Assert.That(actual.IsTender, Is.EqualTo(expected.IsTender), message);
            Assert.That(actual.IsCurrent, Is.EqualTo(expected.IsCurrent), message);
            Assert.That(actual.FromIsoListOne, Is.EqualTo(expected.FromIsoListOne), message);
            Assert.That(actual.FromIsoListThree, Is.EqualTo(expected.FromIsoListThree), message);
            Assert.That(actual.FromCldr, Is.EqualTo(expected.FromCldr), message);
        });
    }

    private static void AssertPartialDate(
        PartialDate actual,
        PartialDate expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.IsKnown, Is.EqualTo(expected.IsKnown), message);
            Assert.That(actual.Year, Is.EqualTo(expected.Year), message);
            Assert.That(actual.Month, Is.EqualTo(expected.Month), message);
            Assert.That(actual.Day, Is.EqualTo(expected.Day), message);
            Assert.That(actual.Precision, Is.EqualTo(expected.Precision), message);
        });
    }

    private static void AssertCurrencyRows(
        CurrencyInfo[] actual,
        CurrencyRow[] expected,
        string message)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length), message);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(
                actual[index].AlphabeticCode,
                Is.EqualTo(expected[index].AlphabeticCode),
                message);
            Assert.That(actual[index].Status, Is.EqualTo(expected[index].Status), message);
        }
    }

    private static void AssertTerritoryMatchesRow(
        TerritoryInfo actual,
        TerritoryRow expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.CanonicalName, Is.EqualTo(expected.CanonicalName), message);
            Assert.That(actual.HasAlpha2Code, Is.EqualTo(expected.HasAlpha2Code), message);
            Assert.That(actual.Alpha2Code, Is.EqualTo(expected.Alpha2Code), message);
            Assert.That(actual.HasAlpha3Code, Is.EqualTo(expected.HasAlpha3Code), message);
            Assert.That(actual.Alpha3Code, Is.EqualTo(expected.Alpha3Code), message);
            Assert.That(actual.HasNumericCode, Is.EqualTo(expected.HasNumericCode), message);
            Assert.That(actual.NumericCode, Is.EqualTo(expected.NumericCode), message);
            Assert.That(actual.NumericCodeValue, Is.EqualTo(expected.NumericCodeValue), message);
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind), message);
        });
    }

    private static string CreateTerritoryQuery(string territoryKey)
    {
        if (territoryKey.Length == 2)
        {
            return " \t" + territoryKey.ToLowerInvariant() + "\u00A0 ";
        }

        return " \t" + territoryKey + "\u00A0 ";
    }

    private static void AssertCurrencyCodes(CurrencyInfo[] actual, params string[] expected)
    {
        Assert.That(
            Array.ConvertAll(actual, currency => currency.AlphabeticCode),
            Is.EqualTo(expected));
    }
}
