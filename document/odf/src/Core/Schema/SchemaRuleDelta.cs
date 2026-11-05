namespace odf;

internal enum SchemaRuleDeltaKind
{
    Added = 0,
    Removed = 1,
    Changed = 2
}

internal enum SchemaRuleDeltaCategory
{
    Definition = 0,
    Element = 1,
    Attribute = 2,
    Datatype = 3
}

internal class SchemaRuleDelta
{
    internal string Profile = "";
    internal SchemaRuleDeltaKind Kind;
    internal SchemaRuleDeltaCategory Category;
    internal string NamespaceName = "";
    internal string Name = "";
    internal int[] PreviousIndexes = [];
    internal int[] CurrentIndexes = [];
}

internal class SchemaVersionDelta
{
    internal OdfVersion PreviousVersion;
    internal OdfVersion CurrentVersion;
    internal SchemaRuleDelta[] Changes = [];
}
