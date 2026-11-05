namespace asn1;

/// <summary>
/// Identifies the result of checking canonical encoding rules.
/// </summary>
public enum AsnCanonicalityStatus
{
    /// <summary>
    /// Represents canonical.
    /// </summary>
    Canonical = 0,

    /// <summary>
    /// Represents non canonical.
    /// </summary>
    NonCanonical = 1,

    /// <summary>
    /// Represents invalid.
    /// </summary>
    Invalid = 2
}
