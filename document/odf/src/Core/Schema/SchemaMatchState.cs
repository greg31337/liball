namespace odf;

internal class SchemaMatchState
{
    internal bool[] UsedAttributes = [];
    internal int ChildIndex;
    internal bool AllowMixedText;
}

internal class SchemaScalarState
{
    internal int Position;
}
