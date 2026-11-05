namespace sip;

/// <summary>Identifies a normalized SIP transaction state.</summary>
public enum SipTransactionState
{
    /// <summary>No state is assigned.</summary>
    None = 0,
    /// <summary>The INVITE client Calling state.</summary>
    Calling = 1,
    /// <summary>The non-INVITE Trying state.</summary>
    Trying = 2,
    /// <summary>The Proceeding state.</summary>
    Proceeding = 3,
    /// <summary>The Completed state.</summary>
    Completed = 4,
    /// <summary>The Confirmed state.</summary>
    Confirmed = 5,
    /// <summary>The RFC 6026 Accepted state.</summary>
    Accepted = 6,
    /// <summary>The terminal state.</summary>
    Terminated = 7
}
