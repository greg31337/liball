namespace sip;

/// <summary>Holds one queued dialog-layer event.</summary>
public class SipDialogEvent
{
    /// <summary>The stable event-kind token.</summary>
    public string? Kind;
    /// <summary>The dialog identifier.</summary>
    public SipDialogId? DialogId;
    /// <summary>The affected dialog usage identifier, when applicable.</summary>
    public string? UsageId;
    /// <summary>The dialog state after the event.</summary>
    public SipDialogState State;
    /// <summary>The dialog snapshot.</summary>
    public SipDialogSnapshot? Snapshot;
    /// <summary>The associated message.</summary>
    public SipMessage? Message;
    /// <summary>The event reason.</summary>
    public string? Reason;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
