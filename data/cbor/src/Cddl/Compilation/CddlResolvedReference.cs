namespace cbor;

internal class CddlResolvedReference
{
    internal CddlNode Node = new CddlNode();
    internal CddlRule? Rule;
    internal CddlRuleKind Kind = CddlRuleKind.None;
    internal string ContainingRule = "";
    internal bool IsEmptySocket;
    internal bool IsGenericParameter;
}
