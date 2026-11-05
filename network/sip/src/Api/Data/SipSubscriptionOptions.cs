namespace sip;

/// <summary>Holds subscription lifetime, retry, fork, ordering, and body policy.</summary>
public class SipSubscriptionOptions
{
    /// <summary>The minimum expiry accepted for a retry.</summary>
    public int MinExpiresSeconds = 60;
    /// <summary>The maximum expiry accepted for a retry.</summary>
    public int MaxExpiresSeconds = 86400;
    /// <summary>The lead time before expiry at which refresh begins.</summary>
    public int RefreshBeforeSeconds = 60;
    /// <summary>The default retry delay when a termination requests retry without a value.</summary>
    public int RetryDelaySeconds = 5;
    /// <summary>The maximum number of forked subscription branches.</summary>
    public int MaxForks = 16;
    /// <summary>The maximum SUBSCRIBE body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>The maximum number of authentication retries.</summary>
    public int MaxAuthenticationAttempts = SipDefaults.MaxAuthenticationAttempts;
    /// <summary>Rejects duplicate or decreasing NOTIFY CSeq values.</summary>
    public bool StrictNotifyOrdering = true;
}
