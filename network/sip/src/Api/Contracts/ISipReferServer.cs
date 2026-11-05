namespace sip;

/// <summary>Validates REFER requests and creates refer-event notifications.</summary>
public interface ISipReferServer
{
    /// <summary>Configures authentication used for protected REFER requests.</summary>
    /// <param name="authenticationServer">The authentication server.</param>
    /// <param name="nonceOptions">The nonce and realm policy.</param>
    void ConfigureReferAuthenticationServer(
        ISipAuthenticationServer authenticationServer,
        SipNonceOptions nonceOptions);
    /// <summary>Processes one incoming REFER request.</summary>
    /// <param name="dialogId">The associated dialog, or null for an out-of-dialog REFER.</param>
    /// <param name="message">The REFER request.</param>
    /// <param name="options">The receiving policy.</param>
    /// <returns>The acceptance event, response, and initial notification.</returns>
    SipReferEvent ReceiveRefer(
        SipDialogId? dialogId, SipMessage message, SipReferServerOptions options);
    /// <summary>Creates a refer-event progress or final notification.</summary>
    /// <param name="progress">The referred operation status.</param>
    /// <returns>The resulting notification event.</returns>
    SipReferEvent ReportReferProgress(SipReferProgress progress);
    /// <summary>Polls the next queued server transfer event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipReferEvent PollReceivedReferEvent();
}
