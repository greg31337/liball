using System.Collections.Generic;
using System.Numerics;

namespace cbor;

internal class CddlNormalizedNode
{
    internal int Index = -1;
    internal CddlNodeKind Kind = CddlNodeKind.None;
    internal CddlLiteralKind LiteralKind = CddlLiteralKind.None;
    internal CddlOccurrenceKind Occurrence = CddlOccurrenceKind.ExactlyOne;
    internal string? Name;
    internal string? TextValue;
    internal byte[]? ByteStringValue;
    internal BigInteger IntegerValue;
    internal bool HasIntegerValue;
    internal double FloatValue;
    internal bool HasFloatValue;
    internal string? ControlName;
    internal int MinimumOccurrence = 1;
    internal int MaximumOccurrence = 1;
    internal int MajorType = -1;
    internal BigInteger ArgumentValue;
    internal bool HasArgumentValue;
    internal bool IsRangeUpperExclusive;
    internal bool IsCut;
    internal List<CddlNormalizedNode> Children =
        new List<CddlNormalizedNode>();
    internal CddlSourceSpan Span = new CddlSourceSpan();
    internal CddlNode SourceNode = new CddlNode();
    internal CddlNormalizedRule? ReferencedRule;
    internal CddlRuleKind ReferenceKind = CddlRuleKind.None;
    internal bool IsEmptySocket;
}
