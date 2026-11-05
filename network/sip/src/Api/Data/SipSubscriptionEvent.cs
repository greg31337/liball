namespace sip;

/// <summary>Holds one queued subscription event.</summary>
public class SipSubscriptionEvent
{
    /// <summary>The stable identifier of this subscription branch.</summary>
    public string? SubscriptionId;
    /// <summary>The subscription state.</summary>
    public SipSubscriptionState State;
    /// <summary>The subscription request.</summary>
    public SipSubscriptionRequest? Request;
    /// <summary>The associated SUBSCRIBE or NOTIFY message.</summary>
    public SipMessage? Message;
    /// <summary>The generated response to a NOTIFY request.</summary>
    public SipMessage? Response;
    /// <summary>The generated NOTIFY request for a server operation.</summary>
    public SipMessage? Notification;
    /// <summary>The associated SIP response status code, or zero when absent.</summary>
    public int StatusCode;
    /// <summary>Indicates that the operation or notification was accepted.</summary>
    public bool Accepted;
    /// <summary>Indicates that this subscription branch has terminated.</summary>
    public bool Final;
    /// <summary>Indicates that the branch was created by a forked response or NOTIFY.</summary>
    public bool Forked;
    /// <summary>The remaining expiry in seconds, or negative one when unavailable.</summary>
    public int ExpiresSeconds = -1;
    /// <summary>The termination reason.</summary>
    public string? Reason;
    /// <summary>The dialog identifier.</summary>
    public SipDialogId? DialogId;
    /// <summary>The most recently accepted NOTIFY CSeq, or negative one when absent.</summary>
    public int NotifyCSeq = -1;
    /// <summary>The monotonically increasing accepted-NOTIFY version.</summary>
    public long Version = -1;
    /// <summary>The next monotonic refresh timestamp, or negative one when absent.</summary>
    public long RefreshAtMilliseconds = -1;
    /// <summary>The monotonic expiry timestamp, or negative one when absent.</summary>
    public long ExpiresAtMilliseconds = -1;
    /// <summary>The next monotonic re-subscription timestamp, or negative one when absent.</summary>
    public long RetryAtMilliseconds = -1;
    /// <summary>The requested retry delay in seconds, or negative one when absent.</summary>
    public int RetryAfterSeconds = -1;
    /// <summary>The notification body bytes for a server state update.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The notification body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The application authorization decision, when evaluated.</summary>
    public SipAuthorizationDecision? Authorization;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
