namespace whois;

/// <summary>
/// Identifies the role or protocol of an advertised referral.
/// </summary>
public enum WhoisReferralKind
{
    /// <summary>
    /// An IANA authoritative service referral.
    /// </summary>
    Iana = 0,

    /// <summary>
    /// A registry WHOIS service referral.
    /// </summary>
    Registry = 1,

    /// <summary>
    /// A registrar WHOIS service referral.
    /// </summary>
    Registrar = 2,

    /// <summary>
    /// A WHOIS referral without a more specific role.
    /// </summary>
    Generic = 3,

    /// <summary>
    /// An RFC 2167 RWhois referral.
    /// </summary>
    RWhois = 4,

    /// <summary>
    /// An HTTP or HTTPS informational referral.
    /// </summary>
    Web = 5,

    /// <summary>
    /// A referral whose role could not be determined.
    /// </summary>
    Unknown = 6
}
