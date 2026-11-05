namespace sip;

/// <summary>Describes an Outbound, GRUU, or push-aware Contact to generate.</summary>
public class SipOutboundContactRequest
{
    /// <summary>The SIP or SIPS Contact URI.</summary>
    public SipUri? ContactUri;
    /// <summary>The stable SIP instance identifier.</summary>
    public string? InstanceId;
    /// <summary>The stable Outbound registration identifier, or negative one when unused.</summary>
    public int RegistrationId = -1;
    /// <summary>The Contact expiry in seconds, or negative one when omitted.</summary>
    public int ExpiresSeconds = -1;
    /// <summary>The Contact quality in thousandths, or negative one when omitted.</summary>
    public int QualityThousandths = -1;
    /// <summary>Requests RFC 5626 Outbound Contact and option-tag behavior.</summary>
    public bool UseOutbound;
    /// <summary>Requests RFC 5627 GRUU assignment.</summary>
    public bool RequestGruu;
    /// <summary>The push notification service provider, empty for a provider query.</summary>
    public string? PushProvider;
    /// <summary>The provider-specific push parameter.</summary>
    public string? PushParameter;
    /// <summary>The push resource identifier.</summary>
    public string? PushResourceId;
    /// <summary>Adds the <c>+sip.pnsreg</c> feature tag for a non-push refresh.</summary>
    public bool PushRefreshWithoutNotification;
}
