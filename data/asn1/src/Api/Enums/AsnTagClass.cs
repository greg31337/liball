namespace asn1;

/// <summary>
/// Identifies an ASN.1 tag class.
/// </summary>
public enum AsnTagClass
{
    /// <summary>
    /// Represents universal.
    /// </summary>
    Universal = 0,

    /// <summary>
    /// Represents application.
    /// </summary>
    Application = 1,

    /// <summary>
    /// Represents context specific.
    /// </summary>
    ContextSpecific = 2,

    /// <summary>
    /// Represents private.
    /// </summary>
    Private = 3
}
