namespace image;

/// <summary>Identifies which payload field of a <see cref="MetadataValue"/> contains the value.</summary>
public enum MetadataValueKind
{
    /// <summary>No value is available.</summary>
    Empty = 0,
    /// <summary>The value is stored in <see cref="MetadataValue.IntegerValue"/>.</summary>
    Integer = 1,
    /// <summary>The value is stored in <see cref="MetadataValue.UnsignedIntegerValue"/>.</summary>
    UnsignedInteger = 2,
    /// <summary>The unsigned or non-negative fraction uses the numerator and denominator fields.</summary>
    Rational = 3,
    /// <summary>The signed fraction uses the numerator and denominator fields.</summary>
    SignedRational = 4,
    /// <summary>The value is stored in <see cref="MetadataValue.DoubleValue"/>.</summary>
    Double = 5,
    /// <summary>The value is stored as text in <see cref="MetadataValue.Text"/>.</summary>
    String = 6,
    /// <summary>The string values are stored in <see cref="MetadataValue.TextValues"/>.</summary>
    StringList = 7,
    /// <summary>The value is stored as uninterpreted bytes in <see cref="MetadataValue.Bytes"/>.</summary>
    ByteArray = 8,
    /// <summary>The original date text is stored without forcing a calendar interpretation.</summary>
    DateText = 9,
    /// <summary>The original XML text is stored without reducing it to scalar tags.</summary>
    XmlText = 10,
    /// <summary>The value points to a nested <see cref="MetadataDirectory"/>.</summary>
    DirectoryReference = 11,
    /// <summary>The value is stored in <see cref="MetadataValue.BooleanValue"/>.</summary>
    Boolean = 12,
    /// <summary>The signed integers are stored in <see cref="MetadataValue.IntegerValues"/>.</summary>
    IntegerArray = 13,
    /// <summary>The unsigned integers are stored in <see cref="MetadataValue.UnsignedIntegerValues"/>.</summary>
    UnsignedIntegerArray = 14,
    /// <summary>The unsigned rational pairs are stored in <see cref="MetadataValue.Numerators"/> and <see cref="MetadataValue.Denominators"/>.</summary>
    RationalArray = 15,
    /// <summary>The signed rational pairs are stored in <see cref="MetadataValue.Numerators"/> and <see cref="MetadataValue.Denominators"/>.</summary>
    SignedRationalArray = 16,
    /// <summary>The floating-point values are stored in <see cref="MetadataValue.DoubleValues"/>.</summary>
    DoubleArray = 17
}
