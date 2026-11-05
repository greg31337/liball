namespace sip;

/// <summary>Describes one REFER transfer request.</summary>
public class SipReferRequest
{
    /// <summary>The existing dialog identifier, or null for an out-of-dialog REFER.</summary>
    public SipDialogId? DialogId;
    /// <summary>The REFER recipient for an out-of-dialog request.</summary>
    public SipUri? Target;
    /// <summary>The local identity for an out-of-dialog request.</summary>
    public SipUri? Sender;
    /// <summary>The mandatory referred-to resource.</summary>
    public SipNameAddress? ReferTo;
    /// <summary>The mandatory local Contact URI.</summary>
    public SipUri? Contact;
    /// <summary>The optional referrer identity.</summary>
    public SipNameAddress? ReferredBy;
    /// <summary>Requests suppression of the implicit refer-event subscription.</summary>
    public bool NoSubscription;
    /// <summary>Requires support for suppressing the implicit subscription.</summary>
    public bool RequireNoSubscription;
    /// <summary>The optional REFER body.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The REFER body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The client limits.</summary>
    public SipReferOptions Options = new SipReferOptions();
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The authentication context associated with challenge retries.</summary>
    public string? AuthenticationContextId;
}
