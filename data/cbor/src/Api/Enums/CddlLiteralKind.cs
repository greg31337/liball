namespace cbor;

/// <summary>Identifies the source representation of a CDDL literal.</summary>
public enum CddlLiteralKind
{
    /// <summary>No literal kind has been assigned.</summary>
    None = 0,
    /// <summary>An unsigned decimal, hexadecimal, octal, or binary integer.</summary>
    UnsignedInteger = 1,
    /// <summary>A negative decimal, hexadecimal, octal, or binary integer.</summary>
    NegativeInteger = 2,
    /// <summary>A decimal floating-point literal.</summary>
    DecimalFloat = 3,
    /// <summary>A hexadecimal floating-point literal.</summary>
    HexadecimalFloat = 4,
    /// <summary>A text-string literal.</summary>
    TextString = 5,
    /// <summary>A UTF-8 byte-string literal.</summary>
    Utf8ByteString = 6,
    /// <summary>A hexadecimal byte-string literal.</summary>
    HexByteString = 7,
    /// <summary>A base64url byte-string literal.</summary>
    Base64UrlByteString = 8
}
