namespace sip;

/// <summary>Holds one queued registration event.</summary>
public class SipRegistrationEvent
{
    /// <summary>The registration state after the event.</summary>
    public SipRegistrationState State;
    /// <summary>The registration snapshot.</summary>
    public SipRegistrationSnapshot? Snapshot;
    /// <summary>The associated SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The event reason.</summary>
    public string? Reason;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The monotonically increasing RFC 3680 document version.</summary>
    public long Version = -1;
    /// <summary>The RFC 3680 document state, either <c>full</c> or <c>partial</c>.</summary>
    public string? DocumentState;
    /// <summary>The RFC 3680 registration state.</summary>
    public string? RegistrationState;
    /// <summary>The RFC 3680 contact state.</summary>
    public string? ContactState;
    /// <summary>The contact binding affected by the event.</summary>
    public SipRegistrationBinding? Binding;
    /// <summary>Indicates that the associated Outbound flow status changed.</summary>
    public bool FlowChanged;
    /// <summary>Indicates that the associated push-notification data changed.</summary>
    public bool PushChanged;
}
