namespace sip;

/// <summary>Describes a PUBLISH client or server operation.</summary>
public class SipPublicationRequest
{
    /// <summary>The incoming PUBLISH message for server processing.</summary>
    public SipMessage? Message;
    /// <summary>The publisher identity.</summary>
    public SipUri? Sender;
    /// <summary>The publication target.</summary>
    public SipUri? Target;
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The SIP entity tag for a conditional operation.</summary>
    public string? EntityTag;
    /// <summary>The requested expiry in seconds.</summary>
    public int ExpiresSeconds = SipDefaults.SubscriptionExpiresSeconds;
    /// <summary>The publication body bytes.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The publication body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The publication lifetime and retry policy.</summary>
    public SipPublicationOptions Options = new SipPublicationOptions();
    /// <summary>The publication-server processing policy.</summary>
    public SipPublicationServerOptions ServerOptions = new SipPublicationServerOptions();
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The authentication context associated with challenge retries.</summary>
    public string? AuthenticationContextId;
}
