using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CanonicalCSharpRow
{
    internal CanonicalCSharpRow(string sortKey, string value)
    {
        SortKey = sortKey;
        Values = new CanonicalCSharpValue[] { CanonicalCSharpValue.String(value) };
    }

    internal CanonicalCSharpRow(string sortKey, IReadOnlyList<CanonicalCSharpValue> values)
    {
        SortKey = sortKey;
        Values = values;
    }

    internal string SortKey { get; }

    internal IReadOnlyList<CanonicalCSharpValue> Values { get; }
}
