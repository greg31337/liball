namespace sip;

/// <summary>Holds one REFER or refer-event subscription lifecycle event.</summary>
public class SipReferEvent
{
    /// <summary>The transfer state.</summary>
    public SipReferState State;
    /// <summary>The high-level transfer request.</summary>
    public SipReferRequest? Request;
    /// <summary>The REFER request or received NOTIFY.</summary>
    public SipMessage? Message;
    /// <summary>The REFER response or generated NOTIFY response.</summary>
    public SipMessage? Response;
    /// <summary>The generated refer-event NOTIFY request.</summary>
    public SipMessage? Notification;
    /// <summary>The referred-to resource.</summary>
    public SipNameAddress? ReferTo;
    /// <summary>The associated dialog identifier.</summary>
    public SipDialogId? DialogId;
    /// <summary>The refer subscription identifier.</summary>
    public string? SubscriptionId;
    /// <summary>Indicates that no implicit subscription was created.</summary>
    public bool NoSubscription;
    /// <summary>The latest referred-operation status code.</summary>
    public int StatusCode;
    /// <summary>Indicates successful transfer completion.</summary>
    public bool Success;
    /// <summary>Indicates a final transfer state.</summary>
    public bool Final;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
