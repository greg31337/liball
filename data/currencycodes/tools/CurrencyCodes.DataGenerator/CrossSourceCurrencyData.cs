using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CrossSourceCurrencyData
{
    internal CrossSourceCurrencyData(IReadOnlyList<CrossSourceCurrencyRow> rows)
    {
        Rows = rows;
    }

    internal IReadOnlyList<CrossSourceCurrencyRow> Rows { get; }
}
