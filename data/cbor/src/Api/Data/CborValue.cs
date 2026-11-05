using System.Collections.Generic;
using System.Numerics;

namespace cbor;

/// <summary>
/// Holds one mutable value in the extended generic CBOR data model.
/// </summary>
public class CborValue
{
    /// <summary>The value's data-model kind.</summary>
    public CborValueKind Kind;
    /// <summary>The arbitrary-precision value when the kind is an integer kind.</summary>
    public BigInteger IntegerValue;
    /// <summary>The bytes when the kind is <see cref="CborValueKind.ByteString"/>.</summary>
    public byte[]? ByteStringValue;
    /// <summary>The text when the kind is <see cref="CborValueKind.TextString"/>.</summary>
    public string? TextStringValue;
    /// <summary>The ordered child values when the kind is <see cref="CborValueKind.Array"/>.</summary>
    public List<CborValue>? ArrayItems;
    /// <summary>The ordered entries when the kind is <see cref="CborValueKind.Map"/>.</summary>
    public List<CborMapEntry>? MapEntries;
    /// <summary>The unsigned tag number when the kind is <see cref="CborValueKind.Tag"/>.</summary>
    public ulong TagNumber;
    /// <summary>The tagged item when the kind is <see cref="CborValueKind.Tag"/>.</summary>
    public CborValue? TaggedValue;
    /// <summary>The floating-point data when the kind is <see cref="CborValueKind.Float"/>.</summary>
    public CborFloatValue? FloatValue;
    /// <summary>The encoded width when the value is a floating-point value.</summary>
    public CborFloatWidth FloatWidth;
    /// <summary>The simple-value number when the kind is <see cref="CborValueKind.SimpleValue"/>.</summary>
    public byte SimpleValue;
    /// <summary>The encoded length form when the value is a string or container.</summary>
    public CborLengthKind LengthKind;
    /// <summary>The optional original representation metadata.</summary>
    public CborRepresentation? Representation;
    /// <summary>Indicates that this value was marked as potentially shared.</summary>
    public bool IsSharedValue;
    /// <summary>The serialization-order index for a shared value or reference, or negative one when absent.</summary>
    public int SharedValueIndex = -1;
    /// <summary>The resolved target of a shared-value reference, which may participate in a cycle.</summary>
    public CborValue? ReferencedValue;
}
