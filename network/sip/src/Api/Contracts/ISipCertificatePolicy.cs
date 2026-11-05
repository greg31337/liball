namespace sip;

/// <summary>Evaluates peer certificate and SIP domain identity.</summary>
public interface ISipCertificatePolicy
{
    /// <summary>Evaluates one certificate identity.</summary>
    /// <param name="identity">The certificate identity data.</param>
    /// <param name="request">The associated authentication request.</param>
    /// <returns>The authorization decision.</returns>
    SipAuthorizationDecision EvaluateCertificate(SipCertificateIdentity identity, SipAuthenticationRequest request);
}
