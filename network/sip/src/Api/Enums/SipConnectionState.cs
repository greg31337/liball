namespace sip;

/// <summary>Identifies the lifecycle state of a reliable SIP connection.</summary>
public enum SipConnectionState
{
    /// <summary>The connection is closed.</summary>
    Closed = 0,
    /// <summary>The connection is being established.</summary>
    Connecting = 1,
    /// <summary>The connection is established.</summary>
    Connected = 2,
    /// <summary>The connection is closing.</summary>
    Closing = 3,
    /// <summary>The connection failed.</summary>
    Failed = 4
}
