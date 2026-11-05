namespace whois;

/// <summary>
/// Identifies the protocol scheme advertised by a referral value.
/// </summary>
public enum WhoisReferralScheme
{
    /// <summary>
    /// No explicit protocol scheme was advertised.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The RFC 3912 WHOIS protocol.
    /// </summary>
    Whois = 1,

    /// <summary>
    /// The RFC 2167 RWhois protocol.
    /// </summary>
    RWhois = 2,

    /// <summary>
    /// The unencrypted HTTP protocol.
    /// </summary>
    Http = 3,

    /// <summary>
    /// The encrypted HTTPS protocol.
    /// </summary>
    Https = 4,

    /// <summary>
    /// An explicit scheme not supported by the library.
    /// </summary>
    Unsupported = 5
}
