namespace sip;

/// <summary>Validates REGISTER requests and maintains registrar bindings.</summary>
public class SipRegistrar : ISipRegistrar
{
    SipRegistrarCore core;

    /// <summary>Initializes a registrar with an in-memory location service.</summary>
    public SipRegistrar()
    {
        core = new SipRegistrarCore();
    }

    /// <summary>Configures the location service used for registrar bindings.</summary>
    /// <param name="locationService">The location service.</param>
    public void ConfigureRegistrarLocationService(
        ISipLocationService locationService)
    {
        core.ConfigureLocationService(locationService);
    }

    /// <summary>Configures Digest authentication for protected registrations.</summary>
    /// <param name="authenticationServer">The authentication server.</param>
    /// <param name="nonceOptions">The registrar nonce and realm policy.</param>
    public void ConfigureRegistrarAuthenticationServer(
        ISipAuthenticationServer authenticationServer,
        SipNonceOptions nonceOptions)
    {
        core.ConfigureAuthenticationServer(authenticationServer, nonceOptions);
    }

    /// <summary>Processes one REGISTER request.</summary>
    /// <param name="message">The REGISTER request.</param>
    /// <param name="options">The registrar policy.</param>
    /// <returns>The resulting registration snapshot.</returns>
    public SipRegistrationSnapshot ProcessRegistration(
        SipMessage message, SipRegistrarOptions options)
    {
        return core.Process(message, options);
    }

    /// <summary>Expires registrar bindings.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void ExpireRegistrarBindings(long nowMilliseconds)
    {
        core.Expire(nowMilliseconds);
    }

    /// <summary>Updates the flow and push status of an existing binding.</summary>
    /// <param name="binding">The binding identity and current status metadata.</param>
    /// <returns>The resulting registration snapshot.</returns>
    public SipRegistrationSnapshot UpdateRegistrarBindingStatus(
        SipRegistrationBinding binding)
    {
        return core.UpdateBindingStatus(binding);
    }

    /// <summary>Polls the next registrar-generated registration event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipRegistrationEvent PollRegistrarEvent()
    {
        return core.PollEvent();
    }
}
