namespace whois;

/// <summary>
/// Contains reusable options for WHOIS lookup processing.
/// </summary>
public class WhoisClientOptions
{
    /// <summary>
    /// Contains stage and overall timeout limits.
    /// </summary>
    public WhoisTimeoutOptions Timeouts = new WhoisTimeoutOptions();

    /// <summary>
    /// Contains request, response, parsing, and traversal limits.
    /// </summary>
    public WhoisLimits Limits = new WhoisLimits();

    /// <summary>
    /// Specifies the preferred order of resolved address families.
    /// </summary>
    public WhoisAddressPreference AddressPreference = WhoisAddressPreference.System;

    /// <summary>
    /// Contains request encoding and response decoding behavior.
    /// </summary>
    public WhoisEncodingOptions Encoding = new WhoisEncodingOptions();

    /// <summary>
    /// Controls whether automatic lookup follows eligible WHOIS referrals.
    /// </summary>
    public bool FollowReferrals = true;

    /// <summary>
    /// Controls whether IANA discovery responses remain in the lookup result.
    /// </summary>
    public bool RetainDiscoveryResponses = true;

    /// <summary>
    /// Controls whether caller-directed exchanges may use non-public endpoints.
    /// </summary>
    public bool AllowNonPublicDirectEndpoints = true;

    /// <summary>
    /// Controls whether automatically selected endpoints may use non-public addresses.
    /// </summary>
    public bool AllowNonPublicAutomaticEndpoints = false;
}
