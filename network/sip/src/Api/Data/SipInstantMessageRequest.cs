namespace sip;

/// <summary>Describes a pager-mode MESSAGE operation.</summary>
public class SipInstantMessageRequest
{
    /// <summary>The local sender identity for an out-of-dialog MESSAGE.</summary>
    public SipUri? Sender;
    /// <summary>The MESSAGE target.</summary>
    public SipUri? Target;
    /// <summary>The existing dialog used for an in-dialog MESSAGE.</summary>
    public SipDialogSnapshot? Dialog;
    /// <summary>The message body bytes.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>Requests a delivery notification when supported.</summary>
    public bool RequestDeliveryNotification;
    /// <summary>The client retry and size policy.</summary>
    public SipInstantMessageOptions Options = new SipInstantMessageOptions();
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The authentication context associated with challenge retries.</summary>
    public string? AuthenticationContextId;
}
