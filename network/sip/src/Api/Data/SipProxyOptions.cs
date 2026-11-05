namespace sip;

/// <summary>Holds proxy routing, forking, and capacity policy.</summary>
public class SipProxyOptions
{
    /// <summary>The proxy processing mode.</summary>
    public SipProxyMode Mode = SipProxyMode.StatefulParallel;
    /// <summary>Inserts Record-Route when true.</summary>
    public bool RecordRoute = true;
    /// <summary>The maximum branch count.</summary>
    public int MaxBranchCount = SipDefaults.MaxProxyBranchCount;
    /// <summary>The maximum redirect recursion count.</summary>
    public int MaxRedirectCount = SipDefaults.MaxRedirectCount;
    /// <summary>The Timer C interval in milliseconds.</summary>
    public int TimerCMilliseconds = 180000;
    /// <summary>Prevents SIPS downgrade.</summary>
    public bool PreventSecureDowngrade = true;
}
