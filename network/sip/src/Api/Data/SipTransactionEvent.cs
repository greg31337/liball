namespace sip;

/// <summary>Holds one queued transaction-layer event.</summary>
public class SipTransactionEvent
{
    /// <summary>The event kind.</summary>
    public SipTransactionEventKind Kind;
    /// <summary>The transaction identifier.</summary>
    public string? TransactionId;
    /// <summary>The state after the event.</summary>
    public SipTransactionState State;
    /// <summary>The associated message.</summary>
    public SipMessage? Message;
    /// <summary>The timer name.</summary>
    public string? TimerName;
    /// <summary>The transition or failure reason.</summary>
    public string? Reason;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
