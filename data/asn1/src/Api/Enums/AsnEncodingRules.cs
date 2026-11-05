namespace asn1;

/// <summary>
/// Identifies an ASN.1 encoding rule or encoding-control plan.
/// </summary>
public enum AsnEncodingRules
{
    /// <summary>
    /// Represents ber.
    /// </summary>
    Ber = 0,

    /// <summary>
    /// Represents der.
    /// </summary>
    Der = 1,

    /// <summary>
    /// Represents cer.
    /// </summary>
    Cer = 2,

    /// <summary>
    /// Represents aper.
    /// </summary>
    Aper = 3,

    /// <summary>
    /// Represents uper.
    /// </summary>
    Uper = 4,

    /// <summary>
    /// Represents canonical aligned per.
    /// </summary>
    CanonicalAlignedPer = 5,

    /// <summary>
    /// Represents canonical unaligned per.
    /// </summary>
    CanonicalUnalignedPer = 6,

    /// <summary>
    /// Represents oer.
    /// </summary>
    Oer = 7,

    /// <summary>
    /// Represents coer.
    /// </summary>
    Coer = 8,

    /// <summary>
    /// Represents basic xer.
    /// </summary>
    BasicXer = 9,

    /// <summary>
    /// Represents canonical xer.
    /// </summary>
    CanonicalXer = 10,

    /// <summary>
    /// Represents extended xer.
    /// </summary>
    ExtendedXer = 11,

    /// <summary>
    /// Represents ecn.
    /// </summary>
    Ecn = 12
}
