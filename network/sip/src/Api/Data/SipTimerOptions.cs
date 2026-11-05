namespace sip;

/// <summary>Holds SIP transaction timer intervals.</summary>
public class SipTimerOptions
{
    /// <summary>The T1 interval in milliseconds.</summary>
    public int T1Milliseconds = SipDefaults.T1Milliseconds;
    /// <summary>The T2 interval in milliseconds.</summary>
    public int T2Milliseconds = SipDefaults.T2Milliseconds;
    /// <summary>The T4 interval in milliseconds.</summary>
    public int T4Milliseconds = SipDefaults.T4Milliseconds;
    /// <summary>The maximum scheduled timer count.</summary>
    public int MaxTimerCount = SipDefaults.MaxTransactionCount;
}
