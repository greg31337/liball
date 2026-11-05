namespace sip;

/// <summary>Holds pager-mode MESSAGE client retry and size policy.</summary>
public class SipInstantMessageOptions
{
    /// <summary>The maximum accepted body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>Allows redirects to be followed automatically.</summary>
    public bool FollowRedirects = true;
    /// <summary>The maximum number of followed redirects.</summary>
    public int MaxRedirects = 3;
    /// <summary>The maximum number of authentication retries.</summary>
    public int MaxAuthenticationAttempts = SipDefaults.MaxAuthenticationAttempts;
}
