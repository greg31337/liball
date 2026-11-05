namespace sip;

/// <summary>Holds transaction behavior, timer, and resource options.</summary>
public class SipTransactionOptions
{
    /// <summary>The transaction timer configuration.</summary>
    public SipTimerOptions Timers = new SipTimerOptions();
    /// <summary>Indicates a reliable transport.</summary>
    public bool ReliableTransport;
    /// <summary>Enables bounded RFC 2543 compatibility matching.</summary>
    public bool AllowLegacyMatching = true;
    /// <summary>The maximum live transaction count.</summary>
    public int MaxTransactionCount = SipDefaults.MaxTransactionCount;
    /// <summary>The maximum queued transaction event count.</summary>
    public int MaxEventCount = SipDefaults.MaxQueueCount;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
