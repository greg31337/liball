namespace sip;

/// <summary>Selects Digest challenges, authorizes requests, and verifies server authentication.</summary>
public class SipAuthenticationClient : ISipAuthenticationClient
{
    SipAuthenticationClientCore core;

    /// <summary>Initializes a SIP authentication client.</summary>
    /// <param name="credentialProvider">The realm and target credential provider.</param>
    public SipAuthenticationClient(ISipCredentialProvider credentialProvider)
    {
        core = new SipAuthenticationClientCore(credentialProvider,
            new SipCryptographicRandomSource());
    }

    /// <summary>Authorizes one challenged request.</summary>
    /// <param name="request">The authentication request.</param>
    /// <returns>The authorized message or failure.</returns>
    public SipAuthenticationResult AuthorizeRequest(SipAuthenticationRequest request)
    {
        return core.Authorize(request);
    }

    /// <summary>Verifies Authentication-Info for one response.</summary>
    /// <param name="request">The verification request.</param>
    /// <returns>The verification result.</returns>
    public SipAuthenticationResult VerifyAuthenticationInfo(
        SipAuthenticationRequest request)
    {
        return core.Verify(request);
    }
}
