namespace qrcode;

/// <summary>Identifies a QR payload or control encoding mode.</summary>
/// <remarks>The integer values match the four-bit Model 2 mode indicators.</remarks>
public enum QrEncodingMode
{
    /// <summary>Ends the encoded payload.</summary>
    Terminator = 0x0,

    /// <summary>Encodes decimal digits in numeric mode.</summary>
    Numeric = 0x1,

    /// <summary>Encodes the QR 45-character repertoire.</summary>
    Alphanumeric = 0x2,

    /// <summary>Encodes arbitrary bytes.</summary>
    Byte = 0x4,

    /// <summary>Encodes permitted Shift JIS double-byte characters.</summary>
    Kanji = 0x8,

    /// <summary>Encodes the optional GB2312 Hanzi subset.</summary>
    Hanzi = 0xD,

    /// <summary>Changes the active Extended Channel Interpretation assignment.</summary>
    Eci = 0x7,

    /// <summary>Marks FNC1 in first position.</summary>
    Fnc1First = 0x5,

    /// <summary>Marks FNC1 in second position.</summary>
    Fnc1Second = 0x9,

    /// <summary>Supplies a structured-append header.</summary>
    StructuredAppend = 0x3
}
