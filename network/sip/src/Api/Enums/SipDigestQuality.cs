namespace sip;

/// <summary>Identifies a Digest quality-of-protection mode.</summary>
public enum SipDigestQuality
{
    /// <summary>No qop token is used.</summary>
    None = 0,
    /// <summary>Authentication protection.</summary>
    Auth = 1,
    /// <summary>Authentication with entity-body integrity.</summary>
    AuthInt = 2
}
