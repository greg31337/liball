using System;

namespace fake;

/// <summary>Describes one node in a caller-defined fake-data schema tree.</summary>
/// <remarks>Fields not used by <see cref="Kind"/> remain null or zero. Schema validation occurs before generation.</remarks>
public class SchemaNode
{
    /// <summary>The operation represented by this node.</summary>
    public SchemaNodeKind Kind;

    /// <summary>The callback used by a <see cref="SchemaNodeKind.Scalar"/> node.</summary>
    public Func<Fake, object?>? ScalarFactory;

    /// <summary>The stored value returned by a <see cref="SchemaNodeKind.Constant"/> node.</summary>
    public object? ConstantValue;

    /// <summary>The ordered fields used by a <see cref="SchemaNodeKind.Object"/> node.</summary>
    public SchemaField[] Fields = new SchemaField[0];

    /// <summary>The repeated child used by a <see cref="SchemaNodeKind.Array"/> node.</summary>
    public SchemaNode? Item;

    /// <summary>The inclusive minimum item count for an array node.</summary>
    public int MinimumCount;

    /// <summary>The inclusive maximum item count for an array node.</summary>
    public int MaximumCount;

    /// <summary>The dot-separated field path used by a <see cref="SchemaNodeKind.Reference"/> node.</summary>
    public string? ReferencePath;

    /// <summary>The correlation-context callback used by a <see cref="SchemaNodeKind.Derived"/> node.</summary>
    public Func<object, object?>? Deriver;

    /// <summary>The correlation-context predicate used by a <see cref="SchemaNodeKind.Condition"/> node.</summary>
    public Func<object, bool>? Predicate;

    /// <summary>The child evaluated when <see cref="Predicate"/> returns true.</summary>
    public SchemaNode? WhenTrue;

    /// <summary>The child evaluated when <see cref="Predicate"/> returns false.</summary>
    public SchemaNode? WhenFalse;

    /// <summary>The weighted children used by a <see cref="SchemaNodeKind.Variant"/> node.</summary>
    public SchemaVariant[] Variants = new SchemaVariant[0];

    /// <summary>The state name used by sequence and unique nodes.</summary>
    public string? StateName;

    /// <summary>The initial signed integer returned by a sequence node.</summary>
    public long SequenceStart;

    /// <summary>The nonzero increment used by a sequence node.</summary>
    public long SequenceIncrement;

    /// <summary>The child evaluated by a <see cref="SchemaNodeKind.Unique"/> node.</summary>
    public SchemaNode? Source;

    /// <summary>The optional ordered composite-key selector used by a unique node.</summary>
    public Func<object?, string[]>? UniqueKeySelector;

    /// <summary>The maximum attempts made by a unique node for one value.</summary>
    public int RetryLimit;

    internal SchemaNode(SchemaNodeKind kind)
    {
        Kind = kind;
        ScalarFactory = null;
        ConstantValue = null;
        Fields = new SchemaField[0];
        Item = null;
        MinimumCount = 0;
        MaximumCount = 0;
        ReferencePath = null;
        Deriver = null;
        Predicate = null;
        WhenTrue = null;
        WhenFalse = null;
        Variants = new SchemaVariant[0];
        StateName = null;
        SequenceStart = 0L;
        SequenceIncrement = 0L;
        Source = null;
        UniqueKeySelector = null;
        RetryLimit = 0;
    }
}
