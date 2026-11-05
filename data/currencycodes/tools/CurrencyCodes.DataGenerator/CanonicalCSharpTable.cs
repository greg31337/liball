using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CanonicalCSharpTable
{
    internal CanonicalCSharpTable(
        string fieldName,
        string elementTypeName,
        bool constructRows,
        IReadOnlyList<CanonicalCSharpRow> rows)
    {
        FieldName = fieldName;
        ElementTypeName = elementTypeName;
        ConstructRows = constructRows;
        Rows = rows;
    }

    internal string FieldName { get; }

    internal string ElementTypeName { get; }

    internal bool ConstructRows { get; }

    internal IReadOnlyList<CanonicalCSharpRow> Rows { get; }
}
