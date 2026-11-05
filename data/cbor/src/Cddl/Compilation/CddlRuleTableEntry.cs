using System.Collections.Generic;

namespace cbor;

internal class CddlRuleTableEntry
{
    internal string Name = "";
    internal CddlRuleKind Kind = CddlRuleKind.None;
    internal CddlRuleKind SocketKind = CddlRuleKind.None;
    internal CddlRule? BaseRule;
    internal List<CddlRule> Definitions = new List<CddlRule>();
    internal CddlRule? MergedRule;
    internal bool HasConflict;
}
