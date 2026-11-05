namespace asn1;

/// <summary>
/// Identifies how an ASN.1 tag is applied.
/// </summary>
public enum AsnTaggingMode
{
    /// <summary>
    /// Represents unspecified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Represents explicit.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// Represents implicit.
    /// </summary>
    Implicit = 2,

    /// <summary>
    /// Represents automatic.
    /// </summary>
    Automatic = 3
}
