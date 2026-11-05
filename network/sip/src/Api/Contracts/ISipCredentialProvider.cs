namespace sip;

/// <summary>Supplies credentials for SIP authentication protection spaces.</summary>
public interface ISipCredentialProvider
{
    /// <summary>Gets credentials for one authentication request.</summary>
    /// <param name="request">The authentication request.</param>
    /// <returns>The matching credential.</returns>
    SipCredential GetCredential(SipAuthenticationRequest request);
}
