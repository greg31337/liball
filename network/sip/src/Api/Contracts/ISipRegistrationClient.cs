namespace sip;

/// <summary>Coordinates registration, refresh, recovery, and unregistration.</summary>
public interface ISipRegistrationClient
{
    /// <summary>Starts a registration operation.</summary>
    /// <param name="request">The registration request.</param>
    /// <returns>The current snapshot.</returns>
    SipRegistrationSnapshot RegisterContact(SipRegistrationRequest request);
    /// <summary>Refreshes the active registration.</summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>The current snapshot.</returns>
    SipRegistrationSnapshot RefreshRegistration(SipRegistrationRequest request);
    /// <summary>Removes registered contacts.</summary>
    /// <param name="request">The unregistration request.</param>
    /// <returns>The current snapshot.</returns>
    SipRegistrationSnapshot UnregisterContact(SipRegistrationRequest request);
    /// <summary>Processes a response to the outstanding REGISTER request.</summary>
    /// <param name="message">The received REGISTER response.</param>
    /// <returns>The updated registration snapshot.</returns>
    SipRegistrationSnapshot ReceiveRegistrationResponse(SipMessage message);
    /// <summary>Advances registration refresh, retry, and expiry processing.</summary>
    /// <param name="nowMilliseconds">The current monotonic timestamp.</param>
    void AdvanceRegistrationTime(long nowMilliseconds);
    /// <summary>Stops the active registration state machine.</summary>
    /// <param name="cancellation">The shutdown request.</param>
    void ShutdownRegistration(SipCancellation cancellation);
    /// <summary>Reports a transport or Outbound flow failure.</summary>
    /// <param name="transportEvent">The failure event.</param>
    /// <returns>The updated registration snapshot.</returns>
    SipRegistrationSnapshot ReportRegistrationTransportFailure(
        SipTransportEvent transportEvent);
    /// <summary>Replaces a failed Outbound flow and starts recovery.</summary>
    /// <param name="flow">The replacement flow connection and routing metadata.</param>
    /// <returns>The updated registration snapshot.</returns>
    SipRegistrationSnapshot RecoverRegistrationFlow(SipFlow flow);
    /// <summary>Configures the authentication client used for 401 and 407 retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    void ConfigureRegistrationAuthenticationClient(
        ISipAuthenticationClient authenticationClient);
    /// <summary>Associates an authentication context with the active registration.</summary>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    void AssociateRegistrationAuthenticationContext(string authenticationContextId);
    /// <summary>Polls the next registration event.</summary>
    /// <returns>The next event.</returns>
    SipRegistrationEvent PollRegistrationEvent();
    /// <summary>Polls the next queued registration snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is queued.</returns>
    SipRegistrationSnapshot PollRegistrationSnapshot();
    /// <summary>Gets the current detached registration snapshot.</summary>
    /// <returns>The current snapshot, or an empty snapshot when idle.</returns>
    SipRegistrationSnapshot GetRegistrationSnapshot();
}
