namespace cbor;

/// <summary>
/// Identifies a token produced or consumed by the low-level CBOR APIs.
/// </summary>
public enum CborTokenKind
{
    /// <summary>No token is available.</summary>
    None = 0,
    /// <summary>An unsigned integer token.</summary>
    UnsignedInteger = 1,
    /// <summary>A negative integer token.</summary>
    NegativeInteger = 2,
    /// <summary>A complete definite-length byte string token.</summary>
    ByteString = 3,
    /// <summary>A complete definite-length text string token.</summary>
    TextString = 4,
    /// <summary>The start of an indefinite-length byte string.</summary>
    StartByteString = 5,
    /// <summary>A chunk in an indefinite-length byte string.</summary>
    ByteStringChunk = 6,
    /// <summary>The end of an indefinite-length byte string.</summary>
    EndByteString = 7,
    /// <summary>The start of an indefinite-length text string.</summary>
    StartTextString = 8,
    /// <summary>A chunk in an indefinite-length text string.</summary>
    TextStringChunk = 9,
    /// <summary>The end of an indefinite-length text string.</summary>
    EndTextString = 10,
    /// <summary>The start of an array.</summary>
    StartArray = 11,
    /// <summary>The end of an array.</summary>
    EndArray = 12,
    /// <summary>The start of a map.</summary>
    StartMap = 13,
    /// <summary>The end of a map.</summary>
    EndMap = 14,
    /// <summary>A semantic tag.</summary>
    Tag = 15,
    /// <summary>A simple value.</summary>
    SimpleValue = 16,
    /// <summary>A floating-point value.</summary>
    Float = 17
}
