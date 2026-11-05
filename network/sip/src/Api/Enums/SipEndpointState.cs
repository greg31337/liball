namespace sip;

/// <summary>Identifies the lifecycle state of a SIP endpoint.</summary>
public enum SipEndpointState
{
    /// <summary>The endpoint has not been started.</summary>
    None = 0,
    /// <summary>The endpoint is starting configured components.</summary>
    Starting = 1,
    /// <summary>The endpoint is accepting operations.</summary>
    Running = 2,
    /// <summary>The endpoint is closing active components.</summary>
    Stopping = 3,
    /// <summary>The endpoint has stopped.</summary>
    Stopped = 4,
    /// <summary>The endpoint encountered a terminal lifecycle failure.</summary>
    Failed = 5
}
