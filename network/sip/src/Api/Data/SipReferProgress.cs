namespace sip;

/// <summary>Describes progress of a referred operation.</summary>
public class SipReferProgress
{
    /// <summary>The server-issued refer subscription identifier.</summary>
    public string? SubscriptionId;
    /// <summary>The SIP-style status code representing referred-operation progress.</summary>
    public int StatusCode;
    /// <summary>The SIP-style status reason.</summary>
    public string? ReasonPhrase;
    /// <summary>Marks the final referred-operation status.</summary>
    public bool Final;
    /// <summary>The remaining subscription lifetime in seconds.</summary>
    public int ExpiresSeconds = SipDefaults.SubscriptionExpiresSeconds;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
