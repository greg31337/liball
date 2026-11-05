using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class TerritoryReconciliationData
{
    internal TerritoryReconciliationData(IReadOnlyList<TerritoryReconciliation> reconciliations)
    {
        Reconciliations = reconciliations;
    }

    internal IReadOnlyList<TerritoryReconciliation> Reconciliations { get; }
}
