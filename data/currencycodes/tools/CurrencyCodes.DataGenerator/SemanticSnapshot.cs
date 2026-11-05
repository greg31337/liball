namespace CurrencyCodes.DataGenerator;

internal sealed class SemanticSnapshot
{
    internal SemanticSnapshot(
        ActiveCurrencyAggregationData activeCurrencies,
        HistoricalCurrencyAggregationData historicalCurrencies,
        CldrCurrencyUsageData currencyUsages,
        CldrFractionData fractionRules,
        CldrTerritoryCodeData territoryCodes,
        TerritoryReconciliationData territoryReconciliations)
    {
        ActiveCurrencies = activeCurrencies;
        HistoricalCurrencies = historicalCurrencies;
        CurrencyUsages = currencyUsages;
        FractionRules = fractionRules;
        TerritoryCodes = territoryCodes;
        TerritoryReconciliations = territoryReconciliations;
    }

    internal ActiveCurrencyAggregationData ActiveCurrencies { get; }

    internal HistoricalCurrencyAggregationData HistoricalCurrencies { get; }

    internal CldrCurrencyUsageData CurrencyUsages { get; }

    internal CldrFractionData FractionRules { get; }

    internal CldrTerritoryCodeData TerritoryCodes { get; }

    internal TerritoryReconciliationData TerritoryReconciliations { get; }
}
