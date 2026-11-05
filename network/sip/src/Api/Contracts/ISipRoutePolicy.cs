namespace sip;

/// <summary>Selects proxy, redirect, registrar, or back-to-back routing.</summary>
public interface ISipRoutePolicy
{
    /// <summary>Chooses routing for one request.</summary>
    /// <param name="request">The proxy request context.</param>
    /// <returns>The routing decision.</returns>
    SipRouteDecision ChooseRoute(SipProxyRequest request);
}
