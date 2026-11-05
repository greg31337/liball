using System;
namespace token;

/// <summary>
/// Represents a decoded CBOR primitive value.
/// </summary>
public class CborItem
{
    /// <summary>
    /// Identifies which value field is active.
    /// </summary>
    public TokenCborValueKind Kind;

    /// <summary>
    /// Contains an unsigned integer value or the argument <c>n</c> of a negative integer <c>-1-n</c>.
    /// </summary>
    public ulong IntegerArgument;

    /// <summary>
    /// Contains byte string data.
    /// </summary>
    public byte[]? Bytes;

    /// <summary>
    /// Contains decoded text string data.
    /// </summary>
    public string? Text;

    /// <summary>
    /// Contains a boolean value.
    /// </summary>
    public bool BooleanValue;

    /// <summary>
    /// Contains an unassigned or application-defined simple value.
    /// </summary>
    public byte SimpleValue;

    /// <summary>
    /// Contains a floating-point value converted to double precision.
    /// </summary>
    public double FloatingPointValue;

    /// <summary>
    /// Contains the encoded floating-point width in bytes: two, four, or eight.
    /// </summary>
    public int FloatingPointWidth;

    /// <summary>
    /// Contains array items when <see cref="Kind"/> is <see cref="TokenCborValueKind.Array"/>.
    /// </summary>
    public CborItem[]? ArrayValue;

    /// <summary>
    /// Contains map entries when <see cref="Kind"/> is <see cref="TokenCborValueKind.Map"/>.
    /// </summary>
    public CborMapEntry[]? MapValue;

    /// <summary>
    /// Contains the tag number when <see cref="Kind"/> is <see cref="TokenCborValueKind.Tag"/>.
    /// </summary>
    public ulong TagValue;

    /// <summary>
    /// Contains the tagged item when <see cref="Kind"/> is <see cref="TokenCborValueKind.Tag"/>.
    /// </summary>
    public CborItem? TaggedValue;

    /// <summary>
    /// Initializes an empty CBOR item.
    /// </summary>
    public CborItem()
    {
        Kind = TokenCborValueKind.Null;
        IntegerArgument = 0;
        Bytes = null;
        Text = null;
        BooleanValue = false;
        SimpleValue = 0;
        FloatingPointValue = 0;
        FloatingPointWidth = 0;
        ArrayValue = null;
        MapValue = null;
        TagValue = 0;
        TaggedValue = null;
    }
}
