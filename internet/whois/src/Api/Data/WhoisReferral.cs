namespace whois;

/// <summary>
/// Contains source, protocol, endpoint, and validity metadata for an advertised referral.
/// </summary>
public class WhoisReferral
{
    /// <summary>
    /// Contains the field or directive name that advertised the referral.
    /// </summary>
    public string SourceField = string.Empty;

    /// <summary>
    /// Contains the one-based source line number.
    /// </summary>
    public int SourceLine = 0;

    /// <summary>
    /// Contains the complete referral value as advertised by the server.
    /// </summary>
    public string AdvertisedValue = string.Empty;

    /// <summary>
    /// Identifies the advertised referral protocol.
    /// </summary>
    public WhoisReferralScheme Scheme = WhoisReferralScheme.Unspecified;

    /// <summary>
    /// Identifies the inferred referral role.
    /// </summary>
    public WhoisReferralKind Kind = WhoisReferralKind.Unknown;

    /// <summary>
    /// Contains the automatic WHOIS follow priority, where lower non-negative values take precedence, or <c>-1</c> when not eligible.
    /// </summary>
    public int FollowPriority = -1;

    /// <summary>
    /// Contains the parsed referral endpoint, or <c>null</c> when no endpoint is valid.
    /// </summary>
    public WhoisEndpoint? Endpoint = null;

    /// <summary>
    /// Contains an advertised path or authority-area suffix not used for RFC 3912 dialing.
    /// </summary>
    public string PathSuffix = string.Empty;

    /// <summary>
    /// Indicates whether the referral value was parsed successfully.
    /// </summary>
    public bool IsValid = false;

    /// <summary>
    /// Contains the rejection reason for an invalid or unsupported referral.
    /// </summary>
    public string RejectionReason = string.Empty;
}
