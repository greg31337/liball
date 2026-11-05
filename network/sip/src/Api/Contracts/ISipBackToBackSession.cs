namespace sip;

/// <summary>Creates and manages two independent SIP dialog legs.</summary>
public interface ISipBackToBackSession
{
    /// <summary>Creates the two-leg session.</summary>
    /// <param name="leftRequest">The incoming leg request.</param>
    /// <param name="rightRequest">The outgoing leg request.</param>
    /// <returns>The initial mapping event.</returns>
    SipBackToBackEvent CreateBackToBackSession(SipInviteRequest leftRequest, SipInviteRequest rightRequest);
    /// <summary>Maps a message between session legs.</summary>
    /// <param name="message">The leg message.</param>
    /// <returns>The mapping event.</returns>
    SipBackToBackEvent MapBackToBackMessage(SipMessage message);
    /// <summary>Applies an application-created message to the opposite independent leg.</summary>
    /// <param name="mapping">The source message, destination message, and source leg.</param>
    /// <returns>The resulting detached two-leg event.</returns>
    SipBackToBackEvent ApplyBackToBackMapping(SipBackToBackMapping mapping);
    /// <summary>Associates authentication state with only one leg.</summary>
    /// <param name="leg">The leg to update.</param>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    void AssociateBackToBackAuthenticationContext(
        SipBackToBackLeg leg, string authenticationContextId);
    /// <summary>Reports a transport failure for only one leg.</summary>
    /// <param name="leg">The failed leg.</param>
    /// <param name="errorMessage">The bounded failure description.</param>
    void ReportBackToBackTransportError(
        SipBackToBackLeg leg, string errorMessage);
    /// <summary>Advances timers independently on both legs.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void AdvanceBackToBackTime(long nowMilliseconds);
    /// <summary>Terminates both session legs.</summary>
    /// <param name="cancellation">The termination request.</param>
    void TerminateBackToBackSession(SipCancellation cancellation);
    /// <summary>Polls the next detached back-to-back session event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipBackToBackEvent PollBackToBackEvent();
    /// <summary>Gets the current detached two-leg session state.</summary>
    /// <returns>The current two-leg event snapshot.</returns>
    SipBackToBackEvent GetBackToBackSession();
}
