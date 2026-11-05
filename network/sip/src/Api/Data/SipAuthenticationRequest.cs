namespace sip;

/// <summary>Describes one client or server authentication operation.</summary>
public class SipAuthenticationRequest
{
    /// <summary>The SIP message being authenticated.</summary>
    public SipMessage? Message;
    /// <summary>The available challenges.</summary>
    public SipDigestChallenge[] Challenges = new SipDigestChallenge[0];
    /// <summary>The credential supplied for verification.</summary>
    public SipCredential? Credential;
    /// <summary>The external bearer token or bearer challenge policy.</summary>
    public SipBearerToken? BearerToken;
    /// <summary>The target URI.</summary>
    public SipUri? Target;
    /// <summary>The exact entity-body bytes used by auth-int.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The credential scope.</summary>
    public SipCredentialScope Scope;
    /// <summary>The zero-based retry attempt.</summary>
    public int Attempt;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
