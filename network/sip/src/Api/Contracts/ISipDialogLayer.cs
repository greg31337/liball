namespace sip;

/// <summary>Creates, finds, updates, and terminates SIP dialogs.</summary>
public interface ISipDialogLayer
{
    /// <summary>Creates a dialog from a request and response.</summary>
    /// <param name="request">The dialog-forming request.</param>
    /// <param name="response">The provisional or final response.</param>
    /// <param name="options">The dialog options.</param>
    /// <returns>The created dialog snapshot.</returns>
    SipDialogSnapshot CreateDialog(SipMessage request, SipMessage response, SipDialogOptions options);
    /// <summary>Finds a dialog by identifier.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <returns>The matching snapshot.</returns>
    SipDialogSnapshot FindDialog(SipDialogId id);
    /// <summary>Finds the live dialog matching a message.</summary>
    /// <param name="message">The in-dialog message.</param>
    /// <returns>The matching snapshot.</returns>
    SipDialogSnapshot FindDialogByMessage(SipMessage message);
    /// <summary>Finds the live dialog associated with a transaction.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <returns>The matching snapshot.</returns>
    SipDialogSnapshot FindDialogByTransaction(string transactionId);
    /// <summary>Constructs the next local request within a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="method">The request method token.</param>
    /// <returns>The constructed request.</returns>
    SipMessage CreateRequest(SipDialogId id, string method);
    /// <summary>Constructs a target-refresh request with a new local target.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="method">The INVITE or UPDATE method token.</param>
    /// <param name="localTarget">The local Contact URI to advertise.</param>
    /// <returns>The constructed target-refresh request.</returns>
    SipMessage CreateTargetRefreshRequest(
        SipDialogId id, string method, SipUri localTarget);
    /// <summary>Creates another usage sharing an existing dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <returns>The new application-visible usage identifier.</returns>
    string CreateUsage(SipDialogId id);
    /// <summary>Terminates one usage without affecting sibling usages.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="usageId">The usage identifier.</param>
    /// <param name="cancellation">The termination request.</param>
    void TerminateUsage(
        SipDialogId id, string usageId, SipCancellation cancellation);
    /// <summary>Associates a live transaction with a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    void AssociateTransaction(SipDialogId id, string transactionId);
    /// <summary>Removes a transaction association from a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    void DissociateTransaction(SipDialogId id, string transactionId);
    /// <summary>Associates an authentication context with a dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    void AssociateAuthenticationContext(
        SipDialogId id, string authenticationContextId);
    /// <summary>Clears the dialog authentication context association.</summary>
    /// <param name="id">The dialog identifier.</param>
    void ClearAuthenticationContext(SipDialogId id);
    /// <summary>Processes an in-dialog message.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="message">The in-dialog message.</param>
    /// <returns>The updated snapshot.</returns>
    SipDialogSnapshot UpdateDialog(SipDialogId id, SipMessage message);
    /// <summary>Terminates one dialog.</summary>
    /// <param name="id">The dialog identifier.</param>
    /// <param name="cancellation">The termination request.</param>
    void TerminateDialog(SipDialogId id, SipCancellation cancellation);
    /// <summary>Polls the next dialog event.</summary>
    /// <returns>The next event.</returns>
    SipDialogEvent PollDialogEvent();
    /// <summary>Polls the next queued dialog snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is available.</returns>
    SipDialogSnapshot PollDialogSnapshot();
}
