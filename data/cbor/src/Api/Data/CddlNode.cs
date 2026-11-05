using System.Collections.Generic;
using System.Numerics;

namespace cbor;

/// <summary>Represents one flat, discriminated CDDL source AST node.</summary>
public class CddlNode
{
    /// <summary>The syntactic node kind.</summary>
    public CddlNodeKind Kind = CddlNodeKind.None;
    /// <summary>The literal representation kind when <see cref="Kind"/> is a literal.</summary>
    public CddlLiteralKind LiteralKind = CddlLiteralKind.None;
    /// <summary>The occurrence form applied to a group entry.</summary>
    public CddlOccurrenceKind Occurrence = CddlOccurrenceKind.ExactlyOne;
    /// <summary>A rule, parameter, label, or socket name associated with the node.</summary>
    public string? Name;
    /// <summary>The decoded text value or preserved literal spelling.</summary>
    public string? TextValue;
    /// <summary>The decoded byte-string value.</summary>
    public byte[]? ByteStringValue;
    /// <summary>The arbitrary-precision integer value.</summary>
    public BigInteger IntegerValue;
    /// <summary>Indicates whether <see cref="IntegerValue"/> is present.</summary>
    public bool HasIntegerValue;
    /// <summary>The parsed floating-point value.</summary>
    public double FloatValue;
    /// <summary>Indicates whether <see cref="FloatValue"/> is present.</summary>
    public bool HasFloatValue;
    /// <summary>The control-operator name, including its leading period.</summary>
    public string? ControlName;
    /// <summary>The minimum allowed occurrence count.</summary>
    public int MinimumOccurrence = 1;
    /// <summary>The maximum allowed occurrence count, or negative one when unbounded.</summary>
    public int MaximumOccurrence = 1;
    /// <summary>The represented CBOR major type, or negative one when unspecified.</summary>
    public int MajorType = -1;
    /// <summary>The representation or tag argument value.</summary>
    public BigInteger ArgumentValue;
    /// <summary>Indicates whether <see cref="ArgumentValue"/> is present.</summary>
    public bool HasArgumentValue;
    /// <summary>Indicates whether a range excludes its upper endpoint.</summary>
    public bool IsRangeUpperExclusive;
    /// <summary>Indicates whether a map entry carries the cut operator.</summary>
    public bool IsCut;
    /// <summary>The ordered child nodes used by the discriminated node kind.</summary>
    public List<CddlNode> Children = new List<CddlNode>();
    /// <summary>The exact source range occupied by the node.</summary>
    public CddlSourceSpan Span = new CddlSourceSpan();
    internal int ReferencedRuleIndex = -1;
}
