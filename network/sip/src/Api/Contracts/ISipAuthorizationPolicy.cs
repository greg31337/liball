namespace sip;

/// <summary>Authorizes SIP operations after identity establishment.</summary>
public interface ISipAuthorizationPolicy
{
    /// <summary>Authorizes one authenticated operation.</summary>
    /// <param name="request">The authentication and message context.</param>
    /// <param name="certificate">The certificate identity context.</param>
    /// <param name="token">The bearer token context.</param>
    /// <returns>The authorization decision.</returns>
    SipAuthorizationDecision AuthorizeOperation(SipAuthenticationRequest request, SipCertificateIdentity? certificate, SipBearerToken? token);
}
