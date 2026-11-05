using System.Collections.Generic;

namespace cbor;

internal class CddlAbnfNode
{
    internal const int None = 0;
    internal const int Alternative = 1;
    internal const int Sequence = 2;
    internal const int Repeat = 3;
    internal const int RuleReference = 4;
    internal const int Literal = 5;
    internal const int Range = 6;
    internal int Index = -1;
    internal int Kind;
    internal string Text = "";
    internal bool CaseSensitive;
    internal int Minimum = 1;
    internal int Maximum = 1;
    internal int Lower;
    internal int Upper;
    internal List<CddlAbnfNode> Children = new List<CddlAbnfNode>();
}

internal class CddlAbnfRule
{
    internal string Name = "";
    internal CddlAbnfNode? Expression;
}

internal class CddlAbnfGrammar
{
    internal CddlAbnfNode? Root;
    internal List<CddlAbnfRule> Rules = new List<CddlAbnfRule>();
    internal int NodeCount;
}
