namespace cbor;

internal class CddlNormalizedRule
{
    internal int Index = -1;
    internal string Name = "";
    internal CddlRuleKind Kind = CddlRuleKind.None;
    internal CddlRule SourceRule = new CddlRule();
    internal CddlNormalizedNode? Value;
    internal bool IsGenericInstantiation;
}
