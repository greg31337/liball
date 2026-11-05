namespace cbor;

/// <summary>
/// Identifies the encoded width of a CBOR floating-point value.
/// </summary>
public enum CborFloatWidth
{
    /// <summary>No floating-point width has been selected.</summary>
    None = 0,
    /// <summary>IEEE 754 binary16, encoded in 16 bits.</summary>
    Half = 16,
    /// <summary>IEEE 754 binary32, encoded in 32 bits.</summary>
    Single = 32,
    /// <summary>IEEE 754 binary64, encoded in 64 bits.</summary>
    Double = 64
}
