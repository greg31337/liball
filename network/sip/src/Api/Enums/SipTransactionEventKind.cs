namespace sip;

/// <summary>Identifies a transaction-layer event.</summary>
public enum SipTransactionEventKind
{
    /// <summary>No event is available.</summary>
    None = 0,
    /// <summary>A transaction was created.</summary>
    Created = 1,
    /// <summary>The transaction changed state.</summary>
    StateChanged = 2,
    /// <summary>A message was retransmitted.</summary>
    Retransmitted = 3,
    /// <summary>A message was delivered to the transaction user.</summary>
    MessageDelivered = 4,
    /// <summary>A transaction timer fired.</summary>
    TimerFired = 5,
    /// <summary>A transport error occurred.</summary>
    TransportError = 6,
    /// <summary>The transaction terminated.</summary>
    Terminated = 7
}
