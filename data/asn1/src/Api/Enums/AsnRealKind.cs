namespace asn1;

/// <summary>
/// Identifies the normalized form of an ASN.1 REAL value.
/// </summary>
public enum AsnRealKind
{
    /// <summary>
    /// Represents zero.
    /// </summary>
    Zero = 0,

    /// <summary>
    /// Represents minus zero.
    /// </summary>
    MinusZero = 1,

    /// <summary>
    /// Represents finite decimal.
    /// </summary>
    FiniteDecimal = 2,

    /// <summary>
    /// Represents finite binary.
    /// </summary>
    FiniteBinary = 3,

    /// <summary>
    /// Represents positive infinity.
    /// </summary>
    PositiveInfinity = 4,

    /// <summary>
    /// Represents negative infinity.
    /// </summary>
    NegativeInfinity = 5,

    /// <summary>
    /// Represents not a number.
    /// </summary>
    NotANumber = 6
}
