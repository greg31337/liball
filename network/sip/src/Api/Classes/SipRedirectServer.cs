namespace sip;

/// <summary>Produces policy-driven SIP redirect responses.</summary>
public class SipRedirectServer : ISipRedirectServer
{
    SipRedirectServerCore core;

    /// <summary>Initializes a redirect server.</summary>
    public SipRedirectServer()
    {
        core = new SipRedirectServerCore();
    }

    /// <summary>Processes one request into a redirect result.</summary>
    /// <param name="request">The request to redirect.</param>
    /// <param name="decision">The routing decision.</param>
    /// <returns>The redirect result.</returns>
    public SipRedirectResult CreateRedirect(
        SipMessage request, SipRouteDecision decision)
    {
        return core.Create(request, decision);
    }
}
