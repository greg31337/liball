using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class HistoricalCurrencyAggregationData
{
    internal HistoricalCurrencyAggregationData(
        IReadOnlyList<HistoricalCurrencyIdentity> currencies,
        IReadOnlyList<HistoricalCurrencyUsage> usages)
    {
        Currencies = currencies;
        Usages = usages;
    }

    internal IReadOnlyList<HistoricalCurrencyIdentity> Currencies { get; }

    internal IReadOnlyList<HistoricalCurrencyUsage> Usages { get; }
}
