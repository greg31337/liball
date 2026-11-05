namespace sip;

/// <summary>Coordinates incoming and outgoing INVITE usages.</summary>
public interface ISipInviteSession
{
    /// <summary>Starts an INVITE or dialog refresh, or sends an incoming response.</summary>
    /// <param name="request">The initial or in-dialog request or response command.</param>
    /// <returns>The initial event.</returns>
    SipInviteEvent StartInvite(SipInviteRequest request);
    /// <summary>Processes an INVITE usage message.</summary>
    /// <param name="message">The received message.</param>
    /// <returns>The resulting event.</returns>
    SipInviteEvent ReceiveInviteMessage(SipMessage message);
    /// <summary>Terminates an INVITE usage.</summary>
    /// <param name="cancellation">The termination request.</param>
    void TerminateInvite(SipCancellation cancellation);
    /// <summary>Advances reliable provisional, PRACK, and session timers.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void AdvanceInviteTime(long nowMilliseconds);
    /// <summary>Associates an authentication context with retries and any live dialog.</summary>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    void AssociateInviteAuthenticationContext(string authenticationContextId);
    /// <summary>Reports a transport failure for the active INVITE usage.</summary>
    /// <param name="errorMessage">The bounded failure description.</param>
    void ReportInviteTransportError(string errorMessage);
    /// <summary>Polls the next INVITE usage event.</summary>
    /// <returns>The next event.</returns>
    SipInviteEvent PollInviteEvent();
    /// <summary>Polls the next queued INVITE usage snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is queued.</returns>
    SipInviteSnapshot PollInviteSnapshot();
    /// <summary>Gets the current detached INVITE usage snapshot.</summary>
    /// <returns>The current snapshot.</returns>
    SipInviteSnapshot GetInviteSnapshot();
}
