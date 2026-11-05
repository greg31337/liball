namespace sip;

/// <summary>Creates, matches, advances, and observes SIP transactions.</summary>
public interface ISipTransactionLayer
{
    /// <summary>Creates a transaction for one request.</summary>
    /// <param name="request">The initiating request.</param>
    /// <param name="options">The transaction options.</param>
    /// <returns>The initial snapshot.</returns>
    SipTransactionSnapshot CreateTransaction(SipMessage request, SipTransactionOptions options);
    /// <summary>Processes an incoming transaction message.</summary>
    /// <param name="message">The received message.</param>
    /// <returns>The updated snapshot.</returns>
    SipTransactionSnapshot ReceiveTransactionMessage(SipTransportMessage message);
    /// <summary>Sends a response through an existing server transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="response">The response to send.</param>
    /// <returns>The updated snapshot.</returns>
    SipTransactionSnapshot SendTransactionResponse(string transactionId, SipMessage response);
    /// <summary>Reports a transport failure to an existing transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="errorMessage">The bounded failure description.</param>
    void ReportTransactionTransportError(string transactionId, string errorMessage);
    /// <summary>Requests transaction cancellation or termination.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellation">The cancellation request.</param>
    void CancelTransaction(string transactionId, SipCancellation cancellation);
    /// <summary>Immediately terminates and removes a transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellation">The termination request.</param>
    void TerminateTransaction(string transactionId, SipCancellation cancellation);
    /// <summary>Advances monotonic transaction time.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void AdvanceTransactionTime(long nowMilliseconds);
    /// <summary>Polls the next transaction event.</summary>
    /// <returns>The next event.</returns>
    SipTransactionEvent PollTransactionEvent();
    /// <summary>Polls the next queued transaction snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is available.</returns>
    SipTransactionSnapshot PollTransactionSnapshot();
    /// <summary>Gets the current snapshot for a live transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <returns>The current snapshot.</returns>
    SipTransactionSnapshot GetTransactionSnapshot(string transactionId);
}
