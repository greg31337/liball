using System;
namespace token;

/// <summary>
/// Identifies the kind of a decoded CBOR primitive value.
/// </summary>
public enum TokenCborValueKind
{
    /// <summary>
    /// An unsigned integer.
    /// </summary>
    UnsignedInteger = 0,

    /// <summary>
    /// A negative integer represented by the argument <c>n</c> in <c>-1-n</c>.
    /// </summary>
    NegativeInteger = 1,

    /// <summary>
    /// A byte string.
    /// </summary>
    ByteString = 2,

    /// <summary>
    /// A UTF-8 text string.
    /// </summary>
    TextString = 3,

    /// <summary>
    /// A boolean value.
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// A null value.
    /// </summary>
    Null = 5,

    /// <summary>
    /// An undefined value.
    /// </summary>
    Undefined = 6,

    /// <summary>
    /// An unassigned or application-defined simple value.
    /// </summary>
    Simple = 7,

    /// <summary>
    /// A binary floating-point value.
    /// </summary>
    FloatingPoint = 8,

    /// <summary>
    /// An array of CBOR values.
    /// </summary>
    Array = 9,

    /// <summary>
    /// A map of CBOR key and value pairs.
    /// </summary>
    Map = 10,

    /// <summary>
    /// A tagged CBOR value.
    /// </summary>
    Tag = 11
}
