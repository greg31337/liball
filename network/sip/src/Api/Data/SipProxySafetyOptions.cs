namespace sip;

/// <summary>Holds proxy admission, capacity, and overload-response policy.</summary>
public class SipProxySafetyOptions
{
    /// <summary>The maximum number of live proxy operations.</summary>
    public int MaxTransactionCount = SipDefaults.MaxTransactionCount;
    /// <summary>The maximum number of branches retained by live operations.</summary>
    public int MaxBranchCount = SipDefaults.MaxProxyBranchCount;
    /// <summary>The maximum number of queued proxy events.</summary>
    public int MaxEventCount = SipDefaults.MaxQueueCount;
    /// <summary>The maximum admitted initial requests in one rate window.</summary>
    public int MaxRequestCount = SipDefaults.MaxTransactionCount;
    /// <summary>The admission-rate window in milliseconds.</summary>
    public int RequestWindowMilliseconds = 1000;
    /// <summary>The Retry-After delay used for overload responses.</summary>
    public int RetryAfterSeconds = 1;
}
