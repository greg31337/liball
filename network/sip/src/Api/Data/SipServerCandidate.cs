namespace sip;

/// <summary>Holds one ordered resolved SIP server candidate.</summary>
public class SipServerCandidate
{
    /// <summary>The target host or numeric address.</summary>
    public string? Host;
    /// <summary>The target port.</summary>
    public int Port = -1;
    /// <summary>The selected transport.</summary>
    public SipTransportKind Transport;
    /// <summary>The SRV priority.</summary>
    public int Priority;
    /// <summary>The SRV weight.</summary>
    public int Weight;
    /// <summary>The record kind that produced the candidate.</summary>
    public SipDnsRecordKind RecordKind;
    /// <summary>The monotonic expiry timestamp in milliseconds.</summary>
    public long ExpiresAtMilliseconds = -1;
    /// <summary>Indicates a secure target.</summary>
    public bool Secure;
}
