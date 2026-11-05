namespace sip;

/// <summary>Validates and processes REGISTER requests.</summary>
public interface ISipRegistrar
{
    /// <summary>Configures the location service used for registrar bindings.</summary>
    /// <param name="locationService">The location service.</param>
    void ConfigureRegistrarLocationService(ISipLocationService locationService);
    /// <summary>Configures Digest authentication for protected registrations.</summary>
    /// <param name="authenticationServer">The authentication server.</param>
    /// <param name="nonceOptions">The registrar nonce and realm policy.</param>
    void ConfigureRegistrarAuthenticationServer(
        ISipAuthenticationServer authenticationServer,
        SipNonceOptions nonceOptions);
    /// <summary>Processes one REGISTER request.</summary>
    /// <param name="message">The REGISTER request.</param>
    /// <param name="options">The registrar policy.</param>
    /// <returns>The resulting registration snapshot.</returns>
    SipRegistrationSnapshot ProcessRegistration(SipMessage message, SipRegistrarOptions options);
    /// <summary>Expires registrar bindings.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void ExpireRegistrarBindings(long nowMilliseconds);
    /// <summary>Updates the flow and push status of an existing binding.</summary>
    /// <param name="binding">The binding identity and current status metadata.</param>
    /// <returns>The resulting registration snapshot.</returns>
    SipRegistrationSnapshot UpdateRegistrarBindingStatus(
        SipRegistrationBinding binding);
    /// <summary>Polls the next registrar-generated registration event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipRegistrationEvent PollRegistrarEvent();
}
