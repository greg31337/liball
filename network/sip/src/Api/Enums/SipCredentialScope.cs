namespace sip;

/// <summary>Identifies the protection space of SIP credentials.</summary>
public enum SipCredentialScope
{
    /// <summary>No credential scope is selected.</summary>
    None = 0,
    /// <summary>Origin-server authentication.</summary>
    Origin = 1,
    /// <summary>Proxy authentication.</summary>
    Proxy = 2
}
