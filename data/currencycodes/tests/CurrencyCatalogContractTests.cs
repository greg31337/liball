using System;

namespace currencycodes.Tests;

public class CurrencyCatalogContractTests
{
    [Test]
    public void CatalogOperationsHaveCompleteUniqueNames()
    {
        string[] names = Array.ConvertAll(
            typeof(ICurrencyCatalog).GetMethods(),
            method => method.Name);
        Array.Sort(names, StringComparer.Ordinal);

        Assert.That(names, Is.EqualTo(new[]
        {
            "FindCurrenciesByNumericCode",
            "FindCurrenciesByOfficialName",
            "FindCurrencyByAlphabeticCode",
            "FindTerritoriesByNumericCode",
            "FindTerritoriesByOfficialName",
            "FindTerritoryByAlpha2Code",
            "FindTerritoryByAlpha3Code",
            "GetCurrenciesForTerritoryOnDate",
            "GetCurrencyCatalog",
            "GetCurrencyDataVersion",
            "GetCurrencyFormatMetadata",
            "GetCurrencyUsageCatalog",
            "GetCurrencyUsageHistoryForCurrency",
            "GetCurrencyUsageHistoryForTerritory",
            "GetCurrentCurrenciesForTerritory",
            "GetCurrentTerritoriesForCurrency",
            "GetRequiredCurrencyByAlphabeticCode",
            "GetTerritoryCatalog"
        }));
    }

    [Test]
    public void CatalogOperationsExposePlannedSignatures()
    {
        AssertSignature("GetCurrencyDataVersion", typeof(DataVersionInfo));
        AssertSignature("GetCurrencyCatalog", typeof(CurrencyInfo[]), typeof(CurrencySet));
        AssertSignature("GetTerritoryCatalog", typeof(TerritoryInfo[]));
        AssertSignature("GetCurrencyUsageCatalog", typeof(CurrencyUsage[]));
        AssertSignature(
            "FindCurrencyByAlphabeticCode",
            typeof(CurrencyLookupResult),
            typeof(string),
            typeof(CurrencySet));
        AssertSignature(
            "FindCurrenciesByNumericCode",
            typeof(CurrencyLookupResult),
            typeof(string),
            typeof(CurrencySet));
        AssertSignature(
            "FindCurrenciesByOfficialName",
            typeof(CurrencyLookupResult),
            typeof(string),
            typeof(NameMatchMode),
            typeof(CurrencySet));
        AssertSignature(
            "GetRequiredCurrencyByAlphabeticCode",
            typeof(CurrencyInfo),
            typeof(string),
            typeof(CurrencySet));
        AssertSignature("FindTerritoryByAlpha2Code", typeof(TerritoryLookupResult), typeof(string));
        AssertSignature("FindTerritoryByAlpha3Code", typeof(TerritoryLookupResult), typeof(string));
        AssertSignature("FindTerritoriesByNumericCode", typeof(TerritoryLookupResult), typeof(string));
        AssertSignature(
            "FindTerritoriesByOfficialName",
            typeof(TerritoryLookupResult),
            typeof(string),
            typeof(NameMatchMode));
        AssertSignature(
            "GetCurrentCurrenciesForTerritory",
            typeof(CurrencyInfo[]),
            typeof(string),
            typeof(bool));
        AssertSignature(
            "GetCurrenciesForTerritoryOnDate",
            typeof(CurrencyInfo[]),
            typeof(string),
            typeof(DateOnly),
            typeof(bool));
        AssertSignature(
            "GetCurrencyUsageHistoryForTerritory",
            typeof(CurrencyUsage[]),
            typeof(string));
        AssertSignature(
            "GetCurrentTerritoriesForCurrency",
            typeof(TerritoryInfo[]),
            typeof(string),
            typeof(bool));
        AssertSignature(
            "GetCurrencyUsageHistoryForCurrency",
            typeof(CurrencyUsage[]),
            typeof(string));
        AssertSignature("GetCurrencyFormatMetadata", typeof(CurrencyFormatMetadata), typeof(string));
    }

    private static void AssertSignature(string name, Type returnType, params Type[] parameterTypes)
    {
        System.Reflection.MethodInfo? method = typeof(ICurrencyCatalog).GetMethod(name, parameterTypes);

        Assert.That(method, Is.Not.Null, name);
        Assert.That(method?.ReturnType, Is.EqualTo(returnType), name);
    }
}
