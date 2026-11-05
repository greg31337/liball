using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CldrCurrencyUsageData
{
    internal CldrCurrencyUsageData(IReadOnlyList<CldrCurrencyUsage> usages)
    {
        Usages = usages;
    }

    internal IReadOnlyList<CldrCurrencyUsage> Usages { get; }
}
