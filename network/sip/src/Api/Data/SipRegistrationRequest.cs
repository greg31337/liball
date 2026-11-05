namespace sip;

/// <summary>Describes one registration client operation.</summary>
public class SipRegistrationRequest
{
    /// <summary>The address of record.</summary>
    public SipUri? AddressOfRecord;
    /// <summary>The contacts to register.</summary>
    public SipContactValue[] Contacts = new SipContactValue[0];
    /// <summary>Indicates that all bindings are to be removed with a wildcard Contact.</summary>
    public bool UnregisterAll;
    /// <summary>The reliable outbound flow candidates for these contacts.</summary>
    public SipFlow[] Flows = new SipFlow[0];
    /// <summary>The registration options.</summary>
    public SipRegistrationOptions Options = new SipRegistrationOptions();
    /// <summary>The registrar request target, or null to derive it from the address of record.</summary>
    public SipUri? Target;
    /// <summary>The configured outbound proxy placed before the service route set.</summary>
    public SipUri? OutboundProxy;
    /// <summary>The stored Service-Route set used for the registration request.</summary>
    public SipRouteValue[] ServiceRoute = new SipRouteValue[0];
    /// <summary>The cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The authentication context associated with registration retries.</summary>
    public string? AuthenticationContextId;
}
