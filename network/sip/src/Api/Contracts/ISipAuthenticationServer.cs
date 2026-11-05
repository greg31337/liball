namespace sip;

/// <summary>Creates challenges and verifies SIP authentication credentials.</summary>
public interface ISipAuthenticationServer
{
    /// <summary>Creates an authentication challenge.</summary>
    /// <param name="request">The authentication request.</param>
    /// <param name="options">The nonce options.</param>
    /// <returns>The challenge result.</returns>
    SipAuthenticationResult CreateAuthenticationChallenge(SipAuthenticationRequest request, SipNonceOptions options);
    /// <summary>Verifies supplied authentication data.</summary>
    /// <param name="request">The authentication request.</param>
    /// <returns>The verification result.</returns>
    SipAuthenticationResult VerifyAuthentication(SipAuthenticationRequest request);
    /// <summary>Verifies an externally issued Bearer access token.</summary>
    /// <param name="request">The request carrying the Bearer authorization.</param>
    /// <param name="tokenValidator">The application-provided token validator.</param>
    /// <returns>The verification result and validated token metadata.</returns>
    SipAuthenticationResult VerifyBearerAuthentication(SipAuthenticationRequest request, ISipTokenValidator tokenValidator);
    /// <summary>Verifies a mutually authenticated TLS peer.</summary>
    /// <param name="request">The request received from the peer.</param>
    /// <param name="connection">The authenticated transport connection.</param>
    /// <param name="certificatePolicy">The application certificate trust policy.</param>
    /// <returns>The trusted-peer authentication result.</returns>
    SipAuthenticationResult VerifyCertificateAuthentication(SipAuthenticationRequest request, SipConnectionInfo connection, ISipCertificatePolicy certificatePolicy);
    /// <summary>Authorizes an operation after successful authentication.</summary>
    /// <param name="request">The operation and credential-scope context.</param>
    /// <param name="authentication">The established authentication result.</param>
    /// <param name="authorizationPolicy">The application authorization policy.</param>
    /// <returns>The application authorization decision.</returns>
    SipAuthorizationDecision AuthorizeAuthenticatedOperation(SipAuthenticationRequest request, SipAuthenticationResult authentication, ISipAuthorizationPolicy authorizationPolicy);
}
