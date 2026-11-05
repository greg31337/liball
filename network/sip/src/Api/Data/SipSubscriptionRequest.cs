namespace sip;

/// <summary>Describes a SUBSCRIBE client or server operation.</summary>
public class SipSubscriptionRequest
{
    /// <summary>The incoming SUBSCRIBE message for a server operation.</summary>
    public SipMessage? Message;
    /// <summary>The local subscriber identity for an out-of-dialog request.</summary>
    public SipUri? Sender;
    /// <summary>The subscription target.</summary>
    public SipUri? Target;
    /// <summary>The local Contact URI for an out-of-dialog request.</summary>
    public SipUri? Contact;
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The event identifier.</summary>
    public string? EventId;
    /// <summary>The requested expiry in seconds.</summary>
    public int ExpiresSeconds = SipDefaults.SubscriptionExpiresSeconds;
    /// <summary>The request body bytes.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The request body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The existing dialog identifier.</summary>
    public SipDialogId? DialogId;
    /// <summary>The client lifetime, retry, fork, and body policy.</summary>
    public SipSubscriptionOptions Options = new SipSubscriptionOptions();
    /// <summary>The server acceptance, expiry, authorization, and queue policy.</summary>
    public SipSubscriptionServerOptions ServerOptions =
        new SipSubscriptionServerOptions();
    /// <summary>Indicates that routing identified this as a forked subscription.</summary>
    public bool Forked;
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The authentication context associated with challenge retries.</summary>
    public string? AuthenticationContextId;
}
