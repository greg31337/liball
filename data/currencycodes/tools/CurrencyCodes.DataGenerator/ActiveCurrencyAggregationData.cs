using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class ActiveCurrencyAggregationData
{
    internal ActiveCurrencyAggregationData(
        IReadOnlyList<ActiveCurrencyIdentity> currencies,
        IReadOnlyList<ActiveCurrencyUsage> usages)
    {
        Currencies = currencies;
        Usages = usages;
    }

    internal IReadOnlyList<ActiveCurrencyIdentity> Currencies { get; }

    internal IReadOnlyList<ActiveCurrencyUsage> Usages { get; }
}
