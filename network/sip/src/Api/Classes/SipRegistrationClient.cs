namespace sip;

/// <summary>Creates registration requests and maintains their protocol identity.</summary>
public class SipRegistrationClient : ISipRegistrationClient
{
    SipRegistrationClientCore core;

    /// <summary>Initializes an idle registration client.</summary>
    public SipRegistrationClient()
    {
        core = new SipRegistrationClientCore(new SipCryptographicRandomSource());
    }

    /// <summary>Starts a registration operation.</summary>
    /// <param name="request">The registration request.</param>
    /// <returns>The current snapshot.</returns>
    public SipRegistrationSnapshot RegisterContact(SipRegistrationRequest request)
    {
        return core.Register(request);
    }

    /// <summary>Refreshes the active registration.</summary>
    /// <param name="request">The refresh request.</param>
    /// <returns>The current snapshot.</returns>
    public SipRegistrationSnapshot RefreshRegistration(SipRegistrationRequest request)
    {
        return core.Refresh(request);
    }

    /// <summary>Removes registered contacts.</summary>
    /// <param name="request">The unregistration request.</param>
    /// <returns>The current snapshot.</returns>
    public SipRegistrationSnapshot UnregisterContact(SipRegistrationRequest request)
    {
        return core.Unregister(request);
    }

    /// <summary>Processes a response to the outstanding REGISTER request.</summary>
    /// <param name="message">The received REGISTER response.</param>
    /// <returns>The updated registration snapshot.</returns>
    public SipRegistrationSnapshot ReceiveRegistrationResponse(SipMessage message)
    {
        return core.ReceiveResponse(message);
    }

    /// <summary>Advances registration refresh, retry, and expiry processing.</summary>
    /// <param name="nowMilliseconds">The current monotonic timestamp.</param>
    public void AdvanceRegistrationTime(long nowMilliseconds)
    {
        core.AdvanceTime(nowMilliseconds);
    }

    /// <summary>Stops the active registration state machine.</summary>
    /// <param name="cancellation">The shutdown request.</param>
    public void ShutdownRegistration(SipCancellation cancellation)
    {
        core.Shutdown(cancellation);
    }

    /// <summary>Reports a transport or Outbound flow failure.</summary>
    /// <param name="transportEvent">The failure event.</param>
    /// <returns>The updated registration snapshot.</returns>
    public SipRegistrationSnapshot ReportRegistrationTransportFailure(
        SipTransportEvent transportEvent)
    {
        return core.ReportTransportFailure(transportEvent);
    }

    /// <summary>Replaces a failed Outbound flow and starts recovery.</summary>
    /// <param name="flow">The replacement flow connection and routing metadata.</param>
    /// <returns>The updated registration snapshot.</returns>
    public SipRegistrationSnapshot RecoverRegistrationFlow(SipFlow flow)
    {
        return core.RecoverFlow(flow);
    }

    /// <summary>Configures the authentication client used for 401 and 407 retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    public void ConfigureRegistrationAuthenticationClient(
        ISipAuthenticationClient authenticationClient)
    {
        core.ConfigureAuthentication(authenticationClient);
    }

    /// <summary>Associates an authentication context with the active registration.</summary>
    /// <param name="authenticationContextId">The authentication context identifier.</param>
    public void AssociateRegistrationAuthenticationContext(
        string authenticationContextId)
    {
        core.AssociateAuthentication(authenticationContextId);
    }

    /// <summary>Polls the next registration event.</summary>
    /// <returns>The next event.</returns>
    public SipRegistrationEvent PollRegistrationEvent()
    {
        return core.PollEvent();
    }

    /// <summary>Polls the next queued registration snapshot.</summary>
    /// <returns>The next snapshot, or an empty snapshot when none is queued.</returns>
    public SipRegistrationSnapshot PollRegistrationSnapshot()
    {
        return core.PollSnapshot();
    }

    /// <summary>Gets the current detached registration snapshot.</summary>
    /// <returns>The current snapshot, or an empty snapshot when idle.</returns>
    public SipRegistrationSnapshot GetRegistrationSnapshot()
    {
        return core.GetSnapshot();
    }
}
