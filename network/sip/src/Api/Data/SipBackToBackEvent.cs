namespace sip;

/// <summary>Holds one back-to-back session mapping event.</summary>
public class SipBackToBackEvent
{
    /// <summary>The back-to-back session state.</summary>
    public SipBackToBackState State;
    /// <summary>The left-leg dialog.</summary>
    public SipDialogSnapshot? LeftDialog;
    /// <summary>The right-leg dialog.</summary>
    public SipDialogSnapshot? RightDialog;
    /// <summary>The complete left-leg INVITE usage snapshot.</summary>
    public SipInviteSnapshot? LeftInvite;
    /// <summary>The complete right-leg INVITE usage snapshot.</summary>
    public SipInviteSnapshot? RightInvite;
    /// <summary>The latest independent left-leg transaction snapshot.</summary>
    public SipTransactionSnapshot? LeftTransaction;
    /// <summary>The latest independent right-leg transaction snapshot.</summary>
    public SipTransactionSnapshot? RightTransaction;
    /// <summary>The left-leg message.</summary>
    public SipMessage? LeftMessage;
    /// <summary>The right-leg message.</summary>
    public SipMessage? RightMessage;
    /// <summary>The source leg for an application-controlled mapping.</summary>
    public SipBackToBackLeg SourceLeg;
    /// <summary>Indicates that both source and mapped messages were applied.</summary>
    public bool Mapped;
    /// <summary>The active session cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The event reason.</summary>
    public string? Reason;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
