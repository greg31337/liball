namespace sip;

/// <summary>Holds credentials for one SIP authentication protection space.</summary>
public class SipCredential
{
    /// <summary>The authentication user name.</summary>
    public string? UserName;
    /// <summary>The credential secret bytes.</summary>
    public byte[] Secret = new byte[0];
    /// <summary>The authentication realm.</summary>
    public string? Realm;
    /// <summary>The credential scope.</summary>
    public SipCredentialScope Scope;
}
