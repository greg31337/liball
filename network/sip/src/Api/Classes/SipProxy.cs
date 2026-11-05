namespace sip;

/// <summary>Processes stateless and stateful proxy operations.</summary>
public class SipProxy : ISipProxy
{
    SipProxyCore core;

    /// <summary>Initializes a SIP proxy.</summary>
    public SipProxy()
    {
        core = new SipProxyCore();
    }

    /// <summary>Configures target resolution for proxy routing.</summary>
    /// <param name="resolver">The DNS resolver.</param>
    /// <param name="options">The DNS resolver options.</param>
    public void ConfigureProxyResolver(
        ISipDnsResolver resolver, SipDnsOptions options)
    {
        core.ConfigureResolver(resolver, options);
    }

    /// <summary>Configures application proxy routing policy.</summary>
    /// <param name="routePolicy">The routing policy.</param>
    public void ConfigureProxyRoutePolicy(ISipRoutePolicy routePolicy)
    {
        core.ConfigureRoutePolicy(routePolicy);
    }

    /// <summary>Configures registered-contact lookup for proxy routing.</summary>
    /// <param name="locationService">The location service.</param>
    public void ConfigureProxyLocationService(
        ISipLocationService locationService)
    {
        core.ConfigureLocationService(locationService);
    }

    /// <summary>Configures proxy authentication and authorization.</summary>
    /// <param name="authenticationServer">The authentication server.</param>
    /// <param name="authorizationPolicy">The authorization policy.</param>
    /// <param name="nonceOptions">The proxy challenge options.</param>
    public void ConfigureProxySecurity(
        ISipAuthenticationServer authenticationServer,
        ISipAuthorizationPolicy authorizationPolicy,
        SipNonceOptions nonceOptions)
    {
        core.ConfigureSecurity(authenticationServer,
            authorizationPolicy, nonceOptions);
    }

    /// <summary>Configures proxy admission and overload safeguards.</summary>
    /// <param name="options">The proxy safety options.</param>
    public void ConfigureProxySafety(SipProxySafetyOptions options)
    {
        core.ConfigureSafety(options);
    }

    /// <summary>Starts proxy processing.</summary>
    /// <param name="request">The proxy request.</param>
    /// <returns>The current proxy result.</returns>
    public SipProxyResult ProcessProxyRequest(SipProxyRequest request)
    {
        return core.ProcessRequest(request);
    }

    /// <summary>Processes a branch response.</summary>
    /// <param name="response">The branch response.</param>
    /// <returns>The current proxy result.</returns>
    public SipProxyResult ProcessProxyResponse(SipMessage response)
    {
        return core.ProcessResponse(response);
    }

    /// <summary>Advances monotonic proxy time and expires due branches.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void AdvanceProxyTime(long nowMilliseconds)
    {
        core.AdvanceTime(nowMilliseconds);
    }

    /// <summary>Cancels proxy branches.</summary>
    /// <param name="cancellation">The cancellation request.</param>
    public void CancelProxyBranches(SipCancellation cancellation)
    {
        core.CancelBranches(cancellation);
    }

    /// <summary>Polls the next proxy event.</summary>
    /// <returns>The next event.</returns>
    public SipProxyEvent PollProxyEvent()
    {
        return core.PollEvent();
    }
}
