namespace sip;

/// <summary>Creates REFER requests and consumes refer-event responses and notifications.</summary>
public interface ISipReferClient
{
    /// <summary>Configures the dialog layer used for in-dialog REFER requests.</summary>
    /// <param name="dialogLayer">The dialog layer.</param>
    void ConfigureReferDialogLayer(ISipDialogLayer dialogLayer);
    /// <summary>Configures authentication used for REFER challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    void ConfigureReferAuthenticationClient(
        ISipAuthenticationClient authenticationClient);
    /// <summary>Creates one REFER request.</summary>
    /// <param name="request">The transfer request.</param>
    /// <returns>The initial transfer event.</returns>
    SipReferEvent SendRefer(SipReferRequest request);
    /// <summary>Processes one REFER response.</summary>
    /// <param name="message">The REFER response.</param>
    /// <returns>The resulting transfer event.</returns>
    SipReferEvent ReceiveReferResponse(SipMessage message);
    /// <summary>Processes one refer-event NOTIFY request.</summary>
    /// <param name="message">The NOTIFY request.</param>
    /// <returns>The resulting transfer event and NOTIFY response.</returns>
    SipReferEvent ReceiveReferNotification(SipMessage message);
    /// <summary>Polls the next queued transfer event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipReferEvent PollReferEvent();
}
