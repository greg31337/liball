namespace sip;

/// <summary>Holds an RFC 5626 outbound flow association.</summary>
public class SipFlow
{
    /// <summary>The flow identifier.</summary>
    public string? Id;
    /// <summary>The outbound instance identifier.</summary>
    public string? InstanceId;
    /// <summary>The registration identifier, or negative one when absent.</summary>
    public int RegistrationId = -1;
    /// <summary>The associated reliable connection.</summary>
    public SipConnectionInfo? Connection;
    /// <summary>The stored Path route set associated with the flow.</summary>
    public SipRouteValue[] Path = new SipRouteValue[0];
    /// <summary>Indicates the primary flow.</summary>
    public bool Primary;
    /// <summary>Indicates that the flow failed.</summary>
    public bool Failed;
    /// <summary>The server-provided Flow-Timer interval in seconds, or negative one when absent.</summary>
    public int FlowTimerSeconds = -1;
    /// <summary>The next monotonic keepalive timestamp in milliseconds.</summary>
    public long NextKeepAliveAtMilliseconds = -1;
    /// <summary>The outstanding keepalive response deadline, or negative one when none is pending.</summary>
    public long KeepAliveResponseDeadlineMilliseconds = -1;
    /// <summary>The monotonic recovery timestamp after failure, or negative one when active.</summary>
    public long RetryAtMilliseconds = -1;
    /// <summary>The number of consecutive failures for this flow registration.</summary>
    public int ConsecutiveFailures;
    /// <summary>The monotonic expiry timestamp in milliseconds.</summary>
    public long ExpiresAtMilliseconds = -1;
}
