namespace sip;

/// <summary>Selects challenges, authorizes requests, and verifies authentication information.</summary>
public interface ISipAuthenticationClient
{
    /// <summary>Authorizes one challenged request.</summary>
    /// <param name="request">The authentication request.</param>
    /// <returns>The authorized message or failure.</returns>
    SipAuthenticationResult AuthorizeRequest(SipAuthenticationRequest request);
    /// <summary>Verifies Authentication-Info for one response.</summary>
    /// <param name="request">The verification request.</param>
    /// <returns>The verification result.</returns>
    SipAuthenticationResult VerifyAuthenticationInfo(SipAuthenticationRequest request);
}
