namespace sip;

/// <summary>Indicates that a SIP transaction operation failed.</summary>
public class SipTransactionException : System.Exception
{
    /// <summary>The related transaction identifier, when available.</summary>
    public string? TransactionId;
    /// <summary>The transaction state associated with the failure.</summary>
    public SipTransactionState State;

    /// <summary>Initializes a SIP transaction failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="transactionId">The related transaction identifier, when available.</param>
    /// <param name="state">The transaction state associated with the failure.</param>
    public SipTransactionException(string message, string? transactionId, SipTransactionState state) : base(message)
    {
        TransactionId = transactionId;
        State = state;
    }
}
