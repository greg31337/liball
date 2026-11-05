namespace sip;

/// <summary>Produces standards-compliant redirect responses.</summary>
public interface ISipRedirectServer
{
    /// <summary>Processes one request into a redirect result.</summary>
    /// <param name="request">The request to redirect.</param>
    /// <param name="decision">The routing decision.</param>
    /// <returns>The redirect result.</returns>
    SipRedirectResult CreateRedirect(SipMessage request, SipRouteDecision decision);
}
