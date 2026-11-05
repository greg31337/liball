namespace diameter;

/// <summary>
/// Constructs reusable RFC 6733 base-protocol AVPs.
/// </summary>
public interface IDiameterBaseAvpFactory
{
    /// <summary>Creates an Origin-Host AVP.</summary>
    /// <param name="identity">The originating node identity.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateOriginHostAvp(DiameterIdentity? identity);

    /// <summary>Creates an Origin-Realm AVP.</summary>
    /// <param name="realm">The originating node realm.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateOriginRealmAvp(DiameterIdentity? realm);

    /// <summary>Creates an Origin-State-Id AVP.</summary>
    /// <param name="identifier">The originating node state identifier.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateOriginStateIdAvp(uint identifier);

    /// <summary>Creates a Destination-Host AVP.</summary>
    /// <param name="identity">The destination node identity.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateDestinationHostAvp(DiameterIdentity? identity);

    /// <summary>Creates a Destination-Realm AVP.</summary>
    /// <param name="realm">The destination realm.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateDestinationRealmAvp(DiameterIdentity? realm);

    /// <summary>Creates an Auth-Application-Id AVP.</summary>
    /// <param name="applicationId">The authentication application identifier.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateAuthApplicationIdAvp(uint applicationId);

    /// <summary>Creates an Acct-Application-Id AVP.</summary>
    /// <param name="applicationId">The accounting application identifier.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateAcctApplicationIdAvp(uint applicationId);

    /// <summary>Creates a Vendor-Specific-Application-Id grouped AVP.</summary>
    /// <param name="application">The vendor and application identifier.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateVendorSpecificApplicationIdAvp(DiameterApplicationId? application);

    /// <summary>Creates a Session-Id AVP.</summary>
    /// <param name="sessionId">The globally unique session identifier.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateSessionIdAvp(string? sessionId);

    /// <summary>Creates a Route-Record AVP.</summary>
    /// <param name="identity">The traversed node identity.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateRouteRecordAvp(DiameterIdentity? identity);

    /// <summary>Creates a Proxy-Info grouped AVP.</summary>
    /// <param name="information">The proxy identity, state, and extensions.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateProxyInfoAvp(DiameterProxyInformation? information);

    /// <summary>Creates a Redirect-Host AVP.</summary>
    /// <param name="uri">The redirect target URI.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateRedirectHostAvp(DiameterUri? uri);

    /// <summary>Creates a Redirect-Host-Usage AVP.</summary>
    /// <param name="usage">The scope of the redirect route.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateRedirectHostUsageAvp(DiameterRedirectHostUsage usage);

    /// <summary>Creates a Redirect-Max-Cache-Time AVP.</summary>
    /// <param name="seconds">The maximum cache lifetime in seconds.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateRedirectMaxCacheTimeAvp(uint seconds);

    /// <summary>Creates an Experimental-Result grouped AVP.</summary>
    /// <param name="result">The vendor and vendor-assigned result code.</param>
    /// <returns>The constructed AVP.</returns>
    DiameterAvp CreateExperimentalResultAvp(DiameterExperimentalResult? result);
}
