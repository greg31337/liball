using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CanonicalCSharpFile
{
    internal CanonicalCSharpFile(
        string namespaceName,
        string className,
        IReadOnlyList<CanonicalCSharpTable> tables)
        : this(
            namespaceName,
            className,
            new CanonicalCSharpField[0],
            tables)
    {
    }

    internal CanonicalCSharpFile(
        string namespaceName,
        string className,
        IReadOnlyList<CanonicalCSharpField> fields,
        IReadOnlyList<CanonicalCSharpTable> tables)
    {
        NamespaceName = namespaceName;
        ClassName = className;
        Fields = fields;
        Tables = tables;
    }

    internal string NamespaceName { get; }

    internal string ClassName { get; }

    internal IReadOnlyList<CanonicalCSharpField> Fields { get; }

    internal IReadOnlyList<CanonicalCSharpTable> Tables { get; }
}
