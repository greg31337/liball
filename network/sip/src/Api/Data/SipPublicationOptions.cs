namespace sip;

/// <summary>Holds publication lifetime, body-size, and authentication retry policy.</summary>
public class SipPublicationOptions
{
    /// <summary>The minimum expiry accepted during a 423 retry.</summary>
    public int MinExpiresSeconds = 60;
    /// <summary>The maximum publication expiry.</summary>
    public int MaxExpiresSeconds = 86400;
    /// <summary>The lead time before expiry at which an automatic refresh begins.</summary>
    public int RefreshBeforeSeconds = 60;
    /// <summary>The maximum publication body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>The maximum number of authentication retries.</summary>
    public int MaxAuthenticationAttempts = SipDefaults.MaxAuthenticationAttempts;
}
