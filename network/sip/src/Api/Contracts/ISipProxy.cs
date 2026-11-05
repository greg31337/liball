namespace sip;

/// <summary>Processes stateless and stateful proxy operations.</summary>
public interface ISipProxy
{
    /// <summary>Configures target resolution for proxy routing.</summary>
    /// <param name="resolver">The DNS resolver.</param>
    /// <param name="options">The DNS resolver options.</param>
    void ConfigureProxyResolver(
        ISipDnsResolver resolver, SipDnsOptions options);
    /// <summary>Configures application proxy routing policy.</summary>
    /// <param name="routePolicy">The routing policy.</param>
    void ConfigureProxyRoutePolicy(ISipRoutePolicy routePolicy);
    /// <summary>Configures registered-contact lookup for proxy routing.</summary>
    /// <param name="locationService">The location service.</param>
    void ConfigureProxyLocationService(ISipLocationService locationService);
    /// <summary>Configures proxy authentication and authorization.</summary>
    /// <param name="authenticationServer">The authentication server.</param>
    /// <param name="authorizationPolicy">The authorization policy.</param>
    /// <param name="nonceOptions">The proxy challenge options.</param>
    void ConfigureProxySecurity(
        ISipAuthenticationServer authenticationServer,
        ISipAuthorizationPolicy authorizationPolicy,
        SipNonceOptions nonceOptions);
    /// <summary>Configures proxy admission and overload safeguards.</summary>
    /// <param name="options">The proxy safety options.</param>
    void ConfigureProxySafety(SipProxySafetyOptions options);
    /// <summary>Starts proxy processing.</summary>
    /// <param name="request">The proxy request.</param>
    /// <returns>The current proxy result.</returns>
    SipProxyResult ProcessProxyRequest(SipProxyRequest request);
    /// <summary>Processes a branch response.</summary>
    /// <param name="response">The branch response.</param>
    /// <returns>The current proxy result.</returns>
    SipProxyResult ProcessProxyResponse(SipMessage response);
    /// <summary>Advances monotonic proxy time and expires due branches.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void AdvanceProxyTime(long nowMilliseconds);
    /// <summary>Cancels proxy branches.</summary>
    /// <param name="cancellation">The cancellation request.</param>
    void CancelProxyBranches(SipCancellation cancellation);
    /// <summary>Polls the next proxy event.</summary>
    /// <returns>The next event.</returns>
    SipProxyEvent PollProxyEvent();
}
