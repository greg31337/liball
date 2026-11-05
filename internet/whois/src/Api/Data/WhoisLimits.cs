namespace whois;

/// <summary>
/// Contains resource limits for WHOIS requests, responses, parsing, and traversal.
/// </summary>
public class WhoisLimits
{
    /// <summary>
    /// Contains the maximum encoded query size before the terminating CRLF.
    /// </summary>
    public int MaximumEncodedQueryBytes = WhoisDefaults.MaximumEncodedQueryBytes;

    /// <summary>
    /// Contains the maximum response bytes retained for one exchange.
    /// </summary>
    public int MaximumResponseBytes = WhoisDefaults.MaximumResponseBytes;

    /// <summary>
    /// Contains the maximum number of automatic referral hops.
    /// </summary>
    public int MaximumReferralHops = WhoisDefaults.MaximumReferralHops;

    /// <summary>
    /// Contains the maximum number of derived response lines.
    /// </summary>
    public int MaximumLines = WhoisDefaults.MaximumLines;

    /// <summary>
    /// Contains the maximum number of parsed response fields.
    /// </summary>
    public int MaximumFields = WhoisDefaults.MaximumFields;
}
