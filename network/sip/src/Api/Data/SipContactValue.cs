namespace sip;

/// <summary>
/// Holds one Contact header field value, including the wildcard form.
/// </summary>
public class SipContactValue
{
    /// <summary>Indicates that this is the wildcard contact.</summary>
    public bool Wildcard;

    /// <summary>The contact name-address, or null for the wildcard form.</summary>
    public SipNameAddress? Address;

    /// <summary>The contact expiry in seconds, or negative one when absent.</summary>
    public int ExpiresSeconds = -1;

    /// <summary>The quality value in thousandths, or negative one when absent.</summary>
    public int QualityThousandths = -1;

    /// <summary>The SIP instance identifier Contact parameter.</summary>
    public string? InstanceId;

    /// <summary>The outbound registration identifier, or negative one when absent.</summary>
    public int RegistrationId = -1;

    /// <summary>Indicates that the Contact URI contains the outbound <c>ob</c> parameter.</summary>
    public bool Outbound;

    /// <summary>The public GRUU URI text.</summary>
    public string? PublicGruu;

    /// <summary>The temporary GRUU URI text.</summary>
    public string? TemporaryGruu;

    /// <summary>The push-notification provider URI parameter.</summary>
    public string? PushProvider;

    /// <summary>The push-notification provider-specific parameter.</summary>
    public string? PushParameter;

    /// <summary>The push-notification resource identifier.</summary>
    public string? PushResourceId;

    /// <summary>The push-notification proxy-unique registration reference.</summary>
    public string? PushRegistrationReference;
}
