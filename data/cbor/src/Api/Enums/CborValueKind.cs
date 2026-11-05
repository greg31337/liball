namespace cbor;

/// <summary>
/// Identifies the data-model kind represented by a CBOR value.
/// </summary>
public enum CborValueKind
{
    /// <summary>No value kind has been selected.</summary>
    None = 0,
    /// <summary>An unsigned integer.</summary>
    UnsignedInteger = 1,
    /// <summary>A negative integer.</summary>
    NegativeInteger = 2,
    /// <summary>A byte string.</summary>
    ByteString = 3,
    /// <summary>A text string.</summary>
    TextString = 4,
    /// <summary>An array.</summary>
    Array = 5,
    /// <summary>A map.</summary>
    Map = 6,
    /// <summary>A semantically tagged value.</summary>
    Tag = 7,
    /// <summary>A floating-point value.</summary>
    Float = 8,
    /// <summary>A simple value, including false, true, null, and undefined.</summary>
    SimpleValue = 9
}
