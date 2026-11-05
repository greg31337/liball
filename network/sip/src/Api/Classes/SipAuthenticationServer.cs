namespace sip;

/// <summary>Creates and verifies server-side SIP Digest authentication exchanges.</summary>
public class SipAuthenticationServer : ISipAuthenticationServer
{
    SipAuthenticationServerCore core;

    /// <summary>Initializes a SIP authentication server.</summary>
    /// <param name="credentialProvider">The credential verification provider.</param>
    public SipAuthenticationServer(ISipCredentialProvider credentialProvider)
    {
        core = new SipAuthenticationServerCore(credentialProvider,
            new SipCryptographicRandomSource(), new SipSystemUnixTimeSource(),
            new SipSystemMonotonicClock());
    }

    /// <summary>Creates an origin or proxy Digest challenge response.</summary>
    /// <param name="request">The request and challenge policy.</param>
    /// <param name="options">The nonce and protection-space options.</param>
    /// <returns>The generated challenge response.</returns>
    public SipAuthenticationResult CreateAuthenticationChallenge(
        SipAuthenticationRequest request, SipNonceOptions options)
    {
        return core.CreateChallenge(request, options);
    }

    /// <summary>Verifies an origin or proxy Digest authorization.</summary>
    /// <param name="request">The request carrying the authorization.</param>
    /// <returns>The authenticated identity and Authentication-Info data.</returns>
    public SipAuthenticationResult VerifyAuthentication(
        SipAuthenticationRequest request)
    {
        return core.Verify(request);
    }

    /// <summary>Verifies an externally issued Bearer access token.</summary>
    /// <param name="request">The request carrying the Bearer authorization.</param>
    /// <param name="tokenValidator">The application-provided token validator.</param>
    /// <returns>The verification result and validated token metadata.</returns>
    public SipAuthenticationResult VerifyBearerAuthentication(
        SipAuthenticationRequest request, ISipTokenValidator tokenValidator)
    {
        return core.VerifyBearer(request, tokenValidator);
    }

    /// <summary>Verifies a mutually authenticated TLS peer.</summary>
    /// <param name="request">The request received from the peer.</param>
    /// <param name="connection">The authenticated transport connection.</param>
    /// <param name="certificatePolicy">The application certificate trust policy.</param>
    /// <returns>The trusted-peer authentication result.</returns>
    public SipAuthenticationResult VerifyCertificateAuthentication(
        SipAuthenticationRequest request, SipConnectionInfo connection,
        ISipCertificatePolicy certificatePolicy)
    {
        return SipCertificateAuthenticationCore.Verify(
            request, connection, certificatePolicy);
    }

    /// <summary>Authorizes an operation after successful authentication.</summary>
    /// <param name="request">The operation and credential-scope context.</param>
    /// <param name="authentication">The established authentication result.</param>
    /// <param name="authorizationPolicy">The application authorization policy.</param>
    /// <returns>The application authorization decision.</returns>
    public SipAuthorizationDecision AuthorizeAuthenticatedOperation(
        SipAuthenticationRequest request,
        SipAuthenticationResult authentication,
        ISipAuthorizationPolicy authorizationPolicy)
    {
        return SipAuthorizationPolicyCore.Authorize(
            request, authentication, authorizationPolicy);
    }
}
